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
    public class BackupService {
        private readonly MongoClient _client;
        private readonly StorageClient _storageClient;
        private readonly ILogger<StatsService> _logger;

        private readonly string _googleProjectId;
        private readonly string _googleStorageBucketName;

        public BackupService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<StatsService> logger
        ) {
            _client = client;
            _logger = logger;

            _googleProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
            var migrationConfigSection = configuration.GetSection("Migrations");
            _googleStorageBucketName = migrationConfigSection["GoogleCloudStorageBucket"];

            _storageClient = StorageClient.Create();
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Migration> MigrationCollection {
            get {
                return MainDatabase.GetCollection<Migration>("Migrations");
            }
        }

        public async Task<(Guid id, DateTime expiresOn)> CreateBackup(Stream rawFilePayload, string password) {
            Guid id = Guid.NewGuid();
            _logger.LogDebug("Creating new backup with ID {0}", id);

            var entry = new Migration {
                Id = id,
                Password = password,
                ExpiresOn = DateTime.UtcNow.AddDays(7),
            };
            await MigrationCollection.InsertOneAsync(entry);

            var uploadedObject = await _storageClient.UploadObjectAsync(_googleStorageBucketName, id.ToString("N"), MediaTypeNames.Application.Octet, rawFilePayload);

            return (id, entry.ExpiresOn);
        }
    }
}
