using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;

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

        public Task<List<Badge>> GetAllBadges() {
            return BadgeCollection.Find(_ => true).ToListAsync();
        }

        public Task<Badge> GetBadgeById(ObjectId id) {
            var filter = Builders<Badge>.Filter.Eq(m => m.Id, id);
            return BadgeCollection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<Badge?> UpdateBadge(ObjectId id, BadgeDTO badgeToEdit)
        {
            var filter = Builders<Badge>.Filter.Eq(b => b.Id, id);
            var update = Builders<Badge>.Update
                .Set(b => b.Name, badgeToEdit.Name)
                .Set(b => b.Description, badgeToEdit.Description)
                .Set(b => b.IsPublic, badgeToEdit.IsPublic)
                .Set(b => b.InformationUrl, badgeToEdit.InformationUrl)
                .Set(b => b.LastUpdate, DateTime.UtcNow);


            var result = await BadgeCollection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<Badge>
                {
                    ReturnDocument = ReturnDocument.After
                }
            );

            return result;
        }

        public Task<bool> DeleteBadge(ObjectId id)
        {
            return BadgeCollection.DeleteOneAsync(Builders<Badge>.Filter.Eq(b => b.Id, id)).ContinueWith(task => task.Result.DeletedCount > 0);
        }

    }
}
