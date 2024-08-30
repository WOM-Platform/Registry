using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class SourceService {

        private readonly MongoClient _client;
        private readonly ILogger<SourceService> _logger;

        public SourceService(
            MongoClient client,
            ILogger<SourceService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
        }

        private IMongoCollection<GenerationRequest> GenerationRequestCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        private IList<FilterDefinition<Source>> GetBasicSourceFilter() {
            List<FilterDefinition<Source>> filters = new() {
                Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
            };

            return filters;
        }

        public async Task<Source> CreateNewSource(string name, string url, AsymmetricCipherKeyPair keys,
            string[] enabledAimCodes = null, bool enableAllAims = true,
            GeoCoords? location = null, bool locationIsFixed = false
        ) {
            var source = new Source {
                Name = name,
                PublicKey = keys.Public.ToPemString(),
                PrivateKey = keys.Private.ToPemString(),
                Url = url,
                CreatedOn = DateTime.UtcNow,
                IsDeleted = false,
                Aims = new Source.SourceAims {
                    EnableAll = enableAllAims,
                    Enabled = enableAllAims ? null : (enabledAimCodes ?? []),
                },
                Location = location.HasValue ?
                    new Source.SourceLocation {
                        Position = location.Value.ToGeoJson(),
                        IsFixed = locationIsFixed,
                    } :
                    null,
            };

            await SourceCollection.InsertOneAsync(source);

            return source;
        }

        public enum SourceListOrder {
            Name,
            CreatedOn,
        }

        public Task<(List<Source>, long Total)> ListSources(ObjectId? controlledBy, string textSearch, string aim, int page, int pageSize, SourceListOrder orderBy) {
            var filters = GetBasicSourceFilter();
            if(controlledBy.HasValue) {
                filters.Add(Builders<Source>.Filter.AnyEq(s => s.AdministratorUserIds, controlledBy.Value));
            }
            if(!string.IsNullOrWhiteSpace(textSearch)) {
                filters.Add(Builders<Source>.Filter.Text(textSearch, new TextSearchOptions { CaseSensitive = false, DiacriticSensitive = false }));
            }
            if(!string.IsNullOrWhiteSpace(aim)) {
                filters.Add(Builders<Source>.Filter.Or(
                    Builders<Source>.Filter.Eq(s => s.Aims.EnableAll, true),
                    Builders<Source>.Filter.AnyEq(s => s.Aims.Enabled, aim.Trim().ToUpperInvariant())
                ));
            }

            return SourceCollection.FilteredPagedListAsync(
                filters,
                orderBy switch {
                    SourceListOrder.Name => Builders<Source>.Sort.Ascending(p => p.Name),
                    SourceListOrder.CreatedOn => Builders<Source>.Sort.Descending(p => p.CreatedOn),
                    _ => throw new ArgumentException("Unsupported order clause"),
                },
                page, pageSize
            );
        }

        public Task<Source> GetSourceById(ObjectId id) {
            var filter = Builders<Source>.Filter.And(
                Builders<Source>.Filter.Eq(s => s.Id, id),
                Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
            );
            return SourceCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<List<Source>> GetSourcesByUser(ObjectId userId) {
            var filter = Builders<Source>.Filter.And(
                Builders<Source>.Filter.AnyEq(s => s.AdministratorUserIds, userId),
                Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
            );
            return SourceCollection.Find(filter).ToListAsync();
        }

        public async Task<bool> ReplaceSource(Source source) {
            var filter = Builders<Source>.Filter.And(
                Builders<Source>.Filter.Eq(s => s.Id, source.Id),
                Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
            );
            var result = await SourceCollection.ReplaceOneAsync(filter, source);
            return (result.ModifiedCount == 1);
        }

        public async Task<bool> DeleteSource(ObjectId sourceId) {
            var filter = Builders<Source>.Filter.And(
                Builders<Source>.Filter.Eq(s => s.Id, sourceId),
                Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
            );
            var result = await SourceCollection.UpdateOneAsync(filter, Builders<Source>.Update.Set(s => s.IsDeleted, true));
            return result.ModifiedCount == 1;
        }

        public async Task AddUserAsAdministrator(IClientSessionHandle session, ObjectId sourceId, User user) {
            var results = await SourceCollection.UpdateOneAsync(
                session,
                Builders<Source>.Filter.And(
                    Builders<Source>.Filter.Eq(s => s.Id, sourceId),
                    Builders<Source>.Filter.Ne(s => s.IsDeleted, true)
                ),
                Builders<Source>.Update.AddToSet(s => s.AdministratorUserIds, user.Id)
            );

            if(results.MatchedCount != 1) {
                throw new ServiceProblemException($"Source with ID {sourceId} not found", statusCode: StatusCodes.Status400BadRequest);
            }
            if(results.ModifiedCount != 1) {
                throw new ServiceProblemException($"Failed to add user as administrator of source {sourceId} (modified {results.ModifiedCount})", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public class GeneratedVouchersCountBySourceResult {
            [BsonId]
            public int? Id;

            [BsonElement("total")]
            public long Total;
        }

        /// <summary>
        /// Returns the sum of vouchers that have been generated by the selected Source.
        /// </summary>
        public async Task<GeneratedVouchersCountBySourceResult> GetGeneratedVouchersBySource(ObjectId sourceId) {
            PipelineDefinition<GenerationRequest, GeneratedVouchersCountBySourceResult> pipeline = new BsonDocument[] {
                BsonDocument.Parse(@"{
                    $project: {
                        'GenerationRequests': '$$ROOT', '_id': 0
                    }
                }"),
                BsonDocument.Parse(@"{
                    $lookup: {
                        localField: 'GenerationRequests._id',
                        from: 'Vouchers',
                        foreignField: 'generationRequestId',
                        as: 'Vouchers'
                    }
                }"),
                BsonDocument.Parse(@"{
                    $unwind: {
                        path: '$Vouchers',
                        preserveNullAndEmptyArrays: true
                    }
                }"),
                BsonDocument.Parse(@"{
                    $addFields: {
                        'gr_id': '$GenerationRequests._id',
                        'gr_sourceId': '$GenerationRequests.sourceId',
                        'gr_amount': '$GenerationRequests.amount',
                        'vo_id': '$Vouchers.position',
                        'vo_initialCount': '$Vouchers.initialCount',
                        'vo_count': '$Vouchers.count',
                        'total_amount': {
                            $multiply: ['$Vouchers.initialCount', '$GenerationRequests.amount']
                        }
                    }
                }"),
                BsonDocument.Parse("{$match: {'GenerationRequests.sourceId': {$eq: new ObjectId('" + sourceId + "')}}}"),
                BsonDocument.Parse(@"{
                    $group: {
                        _id: null,
                        total: {$sum: '$total_amount'}
                    }
                }")
            };

            return await GenerationRequestCollection.Aggregate(pipeline).SingleOrDefaultAsync();
        }

        public async Task<Source> GetInstrumentByNameAsync(string instrumentName)
        {

            var filter = Builders<Source>.Filter.Eq(s => s.Name, instrumentName);
            var source = await SourceCollection.Find(filter).FirstOrDefaultAsync();

            if (source == null)
            {
                throw new ApplicationException($"Instrument with name '{instrumentName}' not found.");
            }

            return source;
        }
    }

}
