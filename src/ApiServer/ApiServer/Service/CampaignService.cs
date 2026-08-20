using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class CampaignService : BaseService {
        public CampaignService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {

        }

        public Task<List<Campaign>> GetCampaigns(bool? isPublic = null) {
            List<FilterDefinition<Campaign>>? filters = new List<FilterDefinition<Campaign>> {
                Builders<Campaign>.Filter.Ne(c => c.IsDeleted, true)
            };
            if(isPublic != null) {
                filters.Add(Builders<Campaign>.Filter.Eq(c => c.IsPublic, isPublic));
            }

            return CampaignCollection
                .Find(Builders<Campaign>.Filter.And(filters))
                .Sort(Builders<Campaign>.Sort.Combine(
                    Builders<Campaign>.Sort.Ascending(c => c.Name),
                    Builders<Campaign>.Sort.Ascending(c => c.CreatedAt)
                ))
                .ToListAsync();
        }

        public Task<Campaign> GetCampaignById(ObjectId id) {
            FilterDefinition<Campaign>? filter = Builders<Campaign>.Filter.And(
                Builders<Campaign>.Filter.Eq(c => c.Id, id),
                Builders<Campaign>.Filter.Ne(m => m.IsDeleted, true)
            );

            return CampaignCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task RegisterCampaign(Campaign campaign) {
            ArgumentNullException.ThrowIfNull(campaign);

            return CampaignCollection.InsertOneAsync(campaign);
        }

        public async Task<bool> ReplaceCampaign(Campaign replacement) {
            ArgumentNullException.ThrowIfNull(replacement);

            FilterDefinition<Campaign>? filter = Builders<Campaign>.Filter.And(
                Builders<Campaign>.Filter.Eq(c => c.Id, replacement.Id),
                Builders<Campaign>.Filter.Ne(c => c.IsDeleted, true)
            );

            ReplaceOneResult? result = await CampaignCollection.ReplaceOneAsync(
                filter,
                replacement
            );

            return result.IsAcknowledged && result.MatchedCount == 1;
        }

        public async Task<bool> DeleteCampaign(ObjectId campaignId) {
            var filter = Builders<Campaign>.Filter.Eq(c => c.Id, campaignId);

            var result = await CampaignCollection.UpdateOneAsync(
                filter,
                Builders<Campaign>.Update.Set(c => c.IsDeleted, true)
            );

            return result.IsAcknowledged && result.MatchedCount == 1;
        }
    }
}
