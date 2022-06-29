using System;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace WomPlatform.Web.Api.Service {
    public class BackupService {

        private readonly MongoClient _client;
        private readonly StorageClient _storageClient;
        private readonly ILogger<StatsService> _logger;

        private readonly string _googleProjectId;

        public BackupService(
            MongoClient client,
            ILogger<StatsService> logger
        ) {
            _client = client;
            _logger = logger;

            _googleProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");

            _storageClient = StorageClient.Create();
        }

    }
}
