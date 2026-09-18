using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class CampaignSubscriberService : BaseService {
        public CampaignSubscriberService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {
        }

        public async Task<CampaignSubscriber> RegisterSubscriber(
            ObjectId campaignId
        ) {
            var subscriber = new CampaignSubscriber {
                CampaignId = campaignId,
                Token = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32)
                ),
                RegisteredAt = DateTime.UtcNow,
                IsRevoked = false,
            };

            await CampaignSubscriberCollection.InsertOneAsync(subscriber);

            return subscriber;
        }

        public Task<List<CampaignSubscriber>> GetSubscribers(
            ObjectId campaignId
        ) {
            var filter = Builders<CampaignSubscriber>.Filter.Eq(
                s => s.CampaignId,
                campaignId
            );

            return CampaignSubscriberCollection
                .Find(filter)
                .Sort(
                    Builders<CampaignSubscriber>.Sort.Ascending(
                        s => s.RegisteredAt
                    )
                )
                .ToListAsync();
        }

        public Task<CampaignSubscriber?> GetSubscriber(
            ObjectId campaignId,
            string token
        ) {
            var filter = Builders<CampaignSubscriber>.Filter.And(
                Builders<CampaignSubscriber>.Filter.Eq(s => s.CampaignId, campaignId),
                Builders<CampaignSubscriber>.Filter.Eq(s => s.Token, token)
            );

            return CampaignSubscriberCollection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<CampaignSubscriber?> UpdateCampaignSubscriber(
            ObjectId campaignId,
            string token,
            bool isRevoked
        ) {
            var filter = Builders<CampaignSubscriber>.Filter.And(
                Builders<CampaignSubscriber>.Filter.Eq(s => s.CampaignId, campaignId),
                Builders<CampaignSubscriber>.Filter.Eq(s => s.Token, token)
            );

            var update = Builders<CampaignSubscriber>.Update
                .Set(s => s.IsRevoked, isRevoked);

            return await CampaignSubscriberCollection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<CampaignSubscriber, CampaignSubscriber> {
                    ReturnDocument = ReturnDocument.After
                }
            );
        }
    }
}
