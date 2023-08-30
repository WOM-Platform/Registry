﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private IList<FilterDefinition<Pos>> GetBasicPosFilter(bool filterDummy = true, bool filterInactive = true) {
            List<FilterDefinition<Pos>> filters = new();
            if(filterDummy)
                filters.Add(Builders<Pos>.Filter.Ne(p => p.IsDummy, true));
            if(filterInactive)
                filters.Add(Builders<Pos>.Filter.Ne(p => p.IsActive, false));

            return filters;
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
            var filters = GetBasicPosFilter();
            filters.Add(Builders<Pos>.Filter.Exists(pos => pos.Position, true));
            filters.Add(Builders<Pos>.Filter.Ne(pos => pos.Position, null));

            return PosCollection.Find(Builders<Pos>.Filter.And(filters)).ToListAsync();
        }

        /// <summary>
        /// Gets list of Merchants and POS that the user controls.
        /// </summary>
        public async Task<IReadOnlyDictionary<Merchant, Pos[]>> GetMerchantsAndPosByUser(ObjectId userId) {
            // Get all merchants with control
            var merchants = await _merchantService.GetMerchantsWithUserControl(userId);

            // Get all matching POS
            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, from m in merchants select m.Id);
            var pos = await PosCollection.Find(posFilter).ToListAsync();

            return merchants.ToImmutableDictionary(
                m => m,
                m => pos.Where(p => p.MerchantId == m.Id).ToArray()
            );
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
            var filters = GetBasicPosFilter();
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
            var basicFilter = Builders<Pos>.Filter.And(GetBasicPosFilter());

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

        public Task DeletePos(ObjectId posId) {
            return PosCollection.DeleteOneAsync(Builders<Pos>.Filter.Eq(p => p.Id, posId));
        }

    }

}
