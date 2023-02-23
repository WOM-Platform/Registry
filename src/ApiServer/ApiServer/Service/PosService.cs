using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class PosService {

        private readonly MongoClient _client;
        private readonly MerchantService _merchantService;
        private readonly ILogger<PosService> _logger;

        public PosService(
            MongoClient client,
            MerchantService merchantService,
            ILogger<PosService> logger
        ) {
            _client = client;
            _merchantService = merchantService;
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
            var merchantIds = from m in await _merchantService.GetMerchantsWithUserControl(userId)
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
            var merchants = await _merchantService.GetMerchantsWithUserControl(userId);

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

        public enum PosListOrder {
            Name,
            CreatedOn,
        }

        public async Task<(List<Pos>, long Total)> ListPos(string textSearch, int page, int pageSize, PosListOrder orderBy, bool? mustHavePosition = null) {
            List<FilterDefinition<Pos>> filters = new() {
                Builders<Pos>.Filter.Ne(p => p.IsDummy, true)
            };
            if(mustHavePosition.HasValue) {
                filters.Add(Builders<Pos>.Filter.Exists(p => p.Position, mustHavePosition.Value));
            }
            if(!string.IsNullOrWhiteSpace(textSearch)) {
                filters.Add(Builders<Pos>.Filter.Text(textSearch, new TextSearchOptions { CaseSensitive = false, DiacriticSensitive = false }));
            }

            var count = await PosCollection.CountDocumentsAsync(Builders<Pos>.Filter.And(filters));

            var query = PosCollection.Find(Builders<Pos>.Filter.And(filters));
            query = orderBy switch {
                PosListOrder.Name => query.SortBy(p => p.Name),
                PosListOrder.CreatedOn => query.SortByDescending(p => p.CreatedOn),
                _ => throw new ArgumentException("Unsupported order clause"),
            };
            query = query.Skip((page, pageSize).GetSkip()).Limit(pageSize);

            var results = await query.ToListAsync();

            return (results, count);
        }

        public async Task<(List<Pos> Results, long Total)> ListPosByDistance(double latitude, double longitude, int page, int pageSize) {
            var basicFilter = Builders<Pos>.Filter.Ne(p => p.IsDummy, true);

            var count = await PosCollection.CountDocumentsAsync(basicFilter);

            var pipeline = new EmptyPipelineDefinition<Pos>()
                .AppendStage<Pos, Pos, Pos>(BsonDocument.Parse(string.Format(CultureInfo.InvariantCulture, @"{{
                        $geoNear: {{
                            'near': {{ 'type': 'Point', 'coordinates': [ {0}, {1} ] }},
                            'distanceField': 'distance',
                            'spherical': true,
                        }}
                    }}", longitude, latitude)))
                .Match(basicFilter)
                .AppendStage<Pos, Pos, Pos>(BsonDocument.Parse(@"{
                        $sort: {
                            'distance': -1,
                        }
                    }"))
                .Skip((page, pageSize).GetSkip()).Limit(pageSize);

            var results = await PosCollection.Aggregate(pipeline).ToListAsync();

            return (results, count);
        }

        /// <summary>
        /// Replace an existing POS, by ID.
        /// </summary>
        public Task ReplacePos(Pos pos) {
            var filter = Builders<Pos>.Filter.Eq(p => p.Id, pos.Id);
            return PosCollection.ReplaceOneAsync(filter, pos);
        }

        /// <summary>
        /// Sets cover path and blur hash on an existing POS.
        /// </summary>
        public Task UpdatePosCover(ObjectId posId, string coverPath, string coverBlurHash) {
            return PosCollection.UpdateOneAsync(
                Builders<Pos>.Filter.Eq(pos => pos.Id, posId),
                Builders<Pos>.Update.Set(pos => pos.CoverPath, coverPath).Set(pos => pos.CoverBlurHash, coverBlurHash)
            );
        }

    }

}
