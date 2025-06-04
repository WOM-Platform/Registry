using System;
using System.Collections.Generic;
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

        public Task<List<Badge>> GetBadges(
            ObjectId? challengeId = null,
            bool? isPublic = null
        ) {
            var filters = new List<FilterDefinition<Badge>> {
                Builders<Badge>.Filter.Ne(b => b.IsDeleted, true)
            };
            if(challengeId != null) {
                filters.Add(Builders<Badge>.Filter.Eq(b => b.ChallengeId, challengeId));
            }
            if(isPublic != null) {
                filters.Add(Builders<Badge>.Filter.Eq(b => b.IsPublic, isPublic));
            }

            return BadgeCollection.Find(Builders<Badge>.Filter.And(filters)).ToListAsync();
        }

        public Task<Badge> GetBadgeById(ObjectId id) {
            var filter = Builders<Badge>.Filter.And(
                Builders<Badge>.Filter.Eq(m => m.Id, id),
                Builders<Badge>.Filter.Ne(m => m.IsDeleted, true)
            );

            return BadgeCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<Badge> UpdateBadge(
            ObjectId badgeId,
            ObjectId? challengeId = null,
            Dictionary<string, string>? name = null,
            Dictionary<string, string>? description = null,
            bool? isPublic = null,
            string? informationUrl = null
        ) {
            var filter = Builders<Badge>.Filter.And(
                Builders<Badge>.Filter.Eq(b => b.Id, badgeId),
                Builders<Badge>.Filter.Ne(b => b.IsDeleted, true)
            );

            var chain = Builders<Badge>.Update.Chain();
            if(challengeId.HasValue) {
                chain.Set(b => b.ChallengeId, challengeId.Value);
            }
            if(name != null) {
                chain.Set(b => b.Name, name);
            }
            if(description != null) {
                chain.Set(b => b.Description, description);
            }
            if(isPublic != null) {
                chain.Set(b => b.IsPublic, isPublic);
            }
            if(informationUrl != null) {
                chain.Set(b => b.InformationUrl, informationUrl);
            }
            chain.Set(u => u.LastUpdate, DateTime.UtcNow);

            return BadgeCollection.FindOneAndUpdateAsync(filter, chain.End(), new FindOneAndUpdateOptions<Badge, Badge> { ReturnDocument = ReturnDocument.After });
        }

        public async Task<bool> ReplaceBadge(Badge replacement) {
            var filter = Builders<Badge>.Filter.Eq(s => s.Id, replacement.Id);
            var result = await BadgeCollection.ReplaceOneAsync(filter, replacement);
            return result.IsAcknowledged && result.MatchedCount == 1;
        }

        public async Task<bool> DeleteBadge(ObjectId badgeId) {
            var filter = Builders<Badge>.Filter.Eq(s => s.Id, badgeId);
            var result = await BadgeCollection.UpdateOneAsync(filter, Builders<Badge>.Update.Set(s => s.IsDeleted, true));
            return result.IsAcknowledged && result.MatchedCount == 1;
        }

        public Task RegisterBadgeChallenge(BadgeChallenge challenge) {
            ArgumentNullException.ThrowIfNull(challenge);

            return BadgeChallengeCollection.InsertOneAsync(challenge);
        }

        public Task<List<BadgeChallenge>> GetBadgeChallenges(
            bool? isPublic = null
        ) {
            var filters = new List<FilterDefinition<BadgeChallenge>> {
                Builders<BadgeChallenge>.Filter.Ne(b => b.IsDeleted, true)
            };
            if(isPublic != null) {
                filters.Add(Builders<BadgeChallenge>.Filter.Eq(b => b.IsPublic, isPublic));
            }

            return BadgeChallengeCollection.Find(Builders<BadgeChallenge>.Filter.And(filters)).ToListAsync();

        }

        public Task<BadgeChallenge> GetBadgeChallengeById(ObjectId id) {
            var filter = Builders<BadgeChallenge>.Filter.And(
                Builders<BadgeChallenge>.Filter.Eq(m => m.Id, id),
                Builders<BadgeChallenge>.Filter.Ne(m => m.IsDeleted, true)
            );

            return BadgeChallengeCollection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<bool> ReplaceBadgeChallenge(BadgeChallenge replacement) {
            var filter = Builders<BadgeChallenge>.Filter.Eq(s => s.Id, replacement.Id);
            var result = await BadgeChallengeCollection.ReplaceOneAsync(filter, replacement);
            return result.IsAcknowledged && result.MatchedCount == 1;
        }

        public async Task<bool> DeleteBadgeChallenge(ObjectId badgeId) {
            var filter = Builders<BadgeChallenge>.Filter.Eq(s => s.Id, badgeId);
            var result = await BadgeChallengeCollection.UpdateOneAsync(filter, Builders<BadgeChallenge>.Update.Set(s => s.IsDeleted, true));
            return result.IsAcknowledged && result.MatchedCount == 1;
        }
    }
}
