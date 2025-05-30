using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class BadgeService : BaseService {
        public BadgeService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {

        }

        public Task RegisterBadge(Badge badge) {
            ArgumentNullException.ThrowIfNull(badge);

            return BadgeCollection.InsertOneAsync(badge);
        }

        public async Task UpdateBadgeImage(ObjectId badgeId, string imagePath, string imageBlurHash) {
            var filter = Builders<Badge>.Filter.Eq(m => m.Id, badgeId);
            var update = Builders<Badge>.Update
                .Set(m => m.ImagePath, imagePath)
                .Set(m => m.ImageBlurHash, imageBlurHash);

            var result = await BadgeCollection.UpdateOneAsync(filter, update);
            if(result.MatchedCount == 0) {
                throw ServiceProblemException.BadgeNotFound;
            }
            if(!result.IsAcknowledged || result.ModifiedCount == 0) {
                throw new InvalidOperationException("Badge update not performed");
            }
        }

        public Task<Badge> GetBadgeById(ObjectId id) {
            var filter = Builders<Badge>.Filter.Eq(m => m.Id, id);
            return BadgeCollection.Find(filter).SingleOrDefaultAsync();
        }
    }
}
