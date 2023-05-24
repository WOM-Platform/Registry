using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class AimService : BaseService {

        public AimService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {

        }

        /// <summary>
        /// Get all aims sorted by code.
        /// </summary>
        public Task<List<Aim>> GetAllAims() {
            return AimCollection
                .Find(Builders<Aim>.Filter.Empty)
                .SortBy(aim => aim.Code)
                .ToListAsync();
        }

        /// <summary>
        /// Get all root aims.
        /// </summary>
        public Task<List<Aim>> GetRootAims() {
            return AimCollection.Aggregate()
                // Projects and adds length and root (first code character)
                .Project<BsonDocument>((ProjectionDefinition<Aim>)@"{
                    ""titles"": 1,
                    ""order"": 1,
                    ""hidden"": 1,
                    ""length"": {$strLenCP: ""$_id""},
                    ""root"": { $substrCP:[ ""$_id"", 0, 1 ] }
                }")
                // Match only aims with length == 1
                .Match(@"{
                    ""length"": 1
                }")
                // Sort by root aim
                .Sort(@"{
                    ""_id"": 1
                }")
                .As<Aim>()
                .ToListAsync();
        }

        /// <summary>
        /// Get all root aim codes.
        /// </summary>
        public async Task<string[]> GetRootAimCodes() {
            return (from a in await GetRootAims() select a.Code).ToArray();
        }

        public Task<Aim> GetAimByCode(string code) {
            var filter = Builders<Aim>.Filter.Eq(a => a.Code, code);
            return AimCollection.Find(filter).SingleOrDefaultAsync();
        }

    }
}
