using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class BackupService : BaseService {
        private readonly StorageClient _storageClient;

        private readonly string _googleStorageBucketName;

        public BackupService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
            var migrationConfigSection = configuration.GetSection("Migrations");
            _googleStorageBucketName = migrationConfigSection["GoogleCloudStorageBucket"];

            _storageClient = StorageClient.Create();
        }

        private IMongoCollection<Migration> MigrationCollection {
            get {
                return MainDatabase.GetCollection<Migration>("Migrations");
            }
        }

        public async Task<(Guid id, DateTime expiresOn)> CreateBackup(Stream rawFilePayload, string password) {
            if(string.IsNullOrWhiteSpace(password)) {
                throw new ArgumentNullException(nameof(password));
            }

            Guid id = Guid.NewGuid();
            Logger.LogInformation("Creating new backup with ID {0}", id);

            var entry = new Migration {
                Id = id,
                Password = password,
                AccessCount = 0,
                ExpiresOn = DateTime.UtcNow.AddDays(31),
                IsCompleted = false,
            };
            await MigrationCollection.InsertOneAsync(entry);
            Logger.LogDebug("Migration will be valid until {0}", entry.ExpiresOn);

            await _storageClient.UploadObjectAsync(_googleStorageBucketName, id.ToString("N"), MediaTypeNames.Application.Octet, rawFilePayload);
            Logger.LogDebug("Storage object uploaded at {0}/{1}", _googleStorageBucketName, id.ToString("N"));

            return (id, entry.ExpiresOn);
        }

        public async Task<(bool Exists, bool AccessGranted, Stream fileStream)> RetrieveBackup(Guid id, string password) {
            if(string.IsNullOrWhiteSpace(password)) {
                throw new ArgumentNullException(nameof(password));
            }

            Logger.LogInformation("Retrieving backup with ID {0}", id);

            var entry = await MigrationCollection.FindOneAndUpdateAsync(
                Builders<Migration>.Filter.And(
                    Builders<Migration>.Filter.Eq(m => m.Id, id),
                    Builders<Migration>.Filter.Not(Builders<Migration>.Filter.Eq(m => m.IsCompleted, true))
                ),
                Builders<Migration>.Update.Inc(m => m.AccessCount, 1)
            );
            if(entry == null) {
                Logger.LogInformation("Backup not found");
                return (false, false, null);
            }
            if(!entry.Password.Equals(password)) {
                Logger.LogInformation("Password does not match");
                return (true, false, null);
            }

            var output = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_googleStorageBucketName, id.ToString("N"), output);
            output.Seek(0, SeekOrigin.Begin);
            Logger.LogDebug("Storage object downloaded from {0}/{1}", _googleStorageBucketName, id.ToString("N"));

            return (true, true, output);
        }

        public async Task<bool> MarkBackupAsCompleted(Guid id, string password) {
            Logger.LogInformation("Marking backup with ID {0} as completed", id);

            var entry = await MigrationCollection.FindOneAndUpdateAsync(
                Builders<Migration>.Filter.And(
                    Builders<Migration>.Filter.Eq(m => m.Id, id),
                    Builders<Migration>.Filter.Eq(m => m.Password, password)
                ),
                Builders<Migration>.Update.Set(m => m.IsCompleted, true)
            );

            return entry != null;
        }

        public async Task<(bool Exists, bool AccessGranted, Migration Migration)> GetBackupInformation(Guid id, string password) {
            if(string.IsNullOrWhiteSpace(password)) {
                throw new ArgumentNullException(nameof(password));
            }

            Logger.LogInformation("Getting information about migration with ID {0}", id);

            var entry = await MigrationCollection.Find(Builders<Migration>.Filter.Eq(m => m.Id, id)).FirstOrDefaultAsync();
            if(entry == null) {
                return (false, false, null);
            }
            if(!entry.Password.Equals(password)) {
                return (true, false, null);
            }

            return (true, true, entry);
        }
    }
}
