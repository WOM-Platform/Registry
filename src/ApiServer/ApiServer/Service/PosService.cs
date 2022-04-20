using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class PosService {

        private readonly MongoClient _client;
        private readonly ILogger<PosService> _logger;

        public PosService(
            MongoClient client,
            ILogger<PosService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Pos> PosCollection {
            get {
                return MainDatabase.GetCollection<Pos>("Pos");
            }
        }

        private IMongoCollection<Merchant> MerchantCollection {
            get {
                return MainDatabase.GetCollection<Merchant>("Merchants");
            }
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as a POS user.
        /// </summary>
        private Task<List<Merchant>> GetMerchantsWithPosControl(ObjectId userId) {
            var merchFilter = Builders<Merchant>.Filter.Or(
                Builders<Merchant>.Filter.AnyEq(m => m.AdministratorIds, userId),
                Builders<Merchant>.Filter.AnyEq(m => m.PosUserIds, userId)
            );
            return MerchantCollection.Find(merchFilter).ToListAsync();
        }

        public Task CreatePos(Pos pos) {
            return PosCollection.InsertOneAsync(pos);
        }

        public Task<Pos> GetPosById(ObjectId id) {
            var filter = Builders<Pos>.Filter.Eq(u => u.Id, id);
            return PosCollection.Find(filter).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets list of POS that the user controls.
        /// </summary>
        public async Task<List<Pos>> GetPosByUser(ObjectId userId) {
            var merchantIds = from m in await GetMerchantsWithPosControl(userId)
                              select m.Id;

            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, merchantIds);
            return await PosCollection.Find(posFilter).ToListAsync();
        }

        /// <summary>
        /// Get list of POS with position.
        /// </summary>
        public Task<List<Pos>> GetPosWithPosition() {
            var filter = Builders<Pos>.Filter.And(
                Builders<Pos>.Filter.Ne(pos => pos.IsDummy, true),
                Builders<Pos>.Filter.Exists(pos => pos.Position, true),
                Builders<Pos>.Filter.Ne(pos => pos.Position, null)
            );
            return PosCollection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Gets list of Merchants and POS that the user controls.
        /// </summary>
        public async Task<List<(Merchant, List<Pos>)>> GetMerchantsAndPosByUser(ObjectId userId) {
            // Get all merchants with control
            var merchants = await GetMerchantsWithPosControl(userId);

            // Get all matching POS
            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, from m in merchants select m.Id);
            var pos = await PosCollection.Find(posFilter).ToListAsync();

            // Build nested list
            var ret = new List<(Merchant, List<Pos>)>(merchants.Count);
            foreach(var merchant in merchants) {
                ret.Add((merchant, pos.Where(p => p.MerchantId == merchant.Id).ToList()));
            }

            return ret;
        }

        public Task<List<Pos>> GetPosByMerchant(ObjectId merchantId) {
            var posFilter = Builders<Pos>.Filter.Eq(p => p.MerchantId, merchantId);
            return PosCollection.Find(posFilter).ToListAsync();
        }

        /// <summary>
        /// Replace an existing POS, by ID.
        /// </summary>
        public Task ReplacePos(Pos pos) {
            var filter = Builders<Pos>.Filter.Eq(p => p.Id, pos.Id);
            return PosCollection.ReplaceOneAsync(filter, pos);
        }

    }

}
