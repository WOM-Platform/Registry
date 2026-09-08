using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class CampaignContributionService : BaseService {
        public CampaignContributionService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {
        }

        public Task RegisterContribution(
            CampaignContribution contribution
        ) {
            ArgumentNullException.ThrowIfNull(contribution);

            return CampaignContributionCollection.InsertOneAsync(
                contribution
            );
        }

        public Task<List<CampaignContribution>> GetContributions(
            ObjectId campaignId
        ) {
            var filter = Builders<CampaignContribution>.Filter.Eq(
                c => c.CampaignId,
                campaignId
            );

            return CampaignContributionCollection
                .Find(filter)
                .SortBy(c => c.ContributedAt)
                .ToListAsync();
        }
        public void EnsureIndexes()
        {
            var indexKeys = Builders<CampaignContribution>.IndexKeys
                .Ascending(c => c.Token);

            var indexModel = new CreateIndexModel<CampaignContribution>(
                indexKeys
            );

            CampaignContributionCollection
                .Indexes
                .CreateOne(indexModel);
        }
    }
}
