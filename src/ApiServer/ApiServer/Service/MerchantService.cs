using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class MerchantService : BaseService {

        public MerchantService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {

        }

        public Task CreateMerchant(Merchant merchant) {
            return MerchantCollection.InsertOneAsync(merchant);
        }

        private IList<FilterDefinition<Merchant>> GetBasicMerchantFilter() {
            List<FilterDefinition<Merchant>> filters = [];

            return filters;
        }

        public enum MerchantListOrder {
            Name,
            CreatedOn,
        }

        /// <summary>
        /// Retrieves a list of paged merchants.
        /// </summary>
        /// <param name="controlledBy">Search for merchants controlled by user ID.</param>
        /// <param name="textSearch">Search by text in name or description.</param>
        public Task<(List<Merchant>, long Total)> ListMerchants(ObjectId? controlledBy, string textSearch, int page, int pageSize, MerchantListOrder orderBy) {
            var filters = GetBasicMerchantFilter();
            if(controlledBy.HasValue) {
                filters.Add(Builders<Merchant>.Filter.Eq($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.UserId)}", controlledBy.Value));
            }
            if(!string.IsNullOrWhiteSpace(textSearch)) {
                filters.Add(Builders<Merchant>.Filter.Text(textSearch, new TextSearchOptions { CaseSensitive = false, DiacriticSensitive = false }));
            }

            return MerchantCollection.FilteredPagedListAsync(
                filters,
                orderBy switch {
                    MerchantListOrder.Name => Builders<Merchant>.Sort.Ascending(p => p.Name),
                    MerchantListOrder.CreatedOn => Builders<Merchant>.Sort.Descending(p => p.CreatedOn),
                    _ => throw new ArgumentException("Unsupported order clause"),
                },
                page, pageSize
            );
        }

        public Task<Merchant> GetMerchantById(ObjectId id) {
            var filter = Builders<Merchant>.Filter.Eq(m => m.Id, id);
            return MerchantCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<Merchant> GetMerchantByFiscalCode(string fiscalCode) {
            var filter = Builders<Merchant>.Filter.Eq(m => m.FiscalCode, fiscalCode);
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return MerchantCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as an administrator.
        /// </summary>
        public Task<List<Merchant>> GetMerchantsWithAdminControl(ObjectId userId) {
            return MerchantCollection.Find(
                Builders<Merchant>.Filter.And(
                    Builders<Merchant>.Filter.Eq($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.UserId)}", userId),
                    Builders<Merchant>.Filter.Eq($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.Role)}", MerchantRole.Admin)
                )
            ).ToListAsync();
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as a POS user.
        /// </summary>
        public Task<List<Merchant>> GetMerchantsWithUserControl(ObjectId userId) {
            return MerchantCollection.Find(
                Builders<Merchant>.Filter.And(
                    Builders<Merchant>.Filter.Eq($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.UserId)}", userId),
                    Builders<Merchant>.Filter.In($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.Role)}", new MerchantRole[] {
                        MerchantRole.User, MerchantRole.Admin
                    })
                )
            ).ToListAsync();
        }

        /// <summary>
        /// Replace an existing merchant, by ID.
        /// </summary>
        public async Task<bool> ReplaceMerchant(Merchant merchant) {
            var filter = Builders<Merchant>.Filter.Eq(u => u.Id, merchant.Id);
            var result = await MerchantCollection.ReplaceOneAsync(filter, merchant);
            return (result.ModifiedCount == 1);
        }

        public class MerchantWithAdmins : Merchant {
            [BsonElement("adminUsers")]
            public User[] Administrators { get; set; }
        }

        /// <summary>
        /// Fetches a list of all merchants with associated admin users.
        /// </summary>
        public Task<List<MerchantWithAdmins>> GetAllMerchantsWithUsers() {
            var pipeline = new EmptyPipelineDefinition<Merchant>()
                .AppendStage<Merchant, Merchant, MerchantWithAdmins>(BsonDocument.Parse(@"{
                    $lookup: {
                        from: 'Users',
                        localField: 'adminUserIds',
                        foreignField: '_id',
                        as: 'adminUsers'
                    }
                }"))
                .AppendStage<Merchant, MerchantWithAdmins, MerchantWithAdmins>(BsonDocument.Parse(@"{
                    $match: {
                        'adminUsers.0': {
                            $exists: true
                        }
                    }
                }"))
                .Sort(Builders<MerchantWithAdmins>.Sort.Ascending(m => m.Name))
            ;

            return MerchantCollection.Aggregate(pipeline).ToListAsync();
        }

        public async Task AddUserAsManager(IClientSessionHandle session, ObjectId merchantId, User user, MerchantRole role) {
            var results = await MerchantCollection.UpdateOneAsync(
                session,
                Builders<Merchant>.Filter.Eq(s => s.Id, merchantId),
                Builders<Merchant>.Update.AddToSet(s => s.Access, new AccessControlEntry<MerchantRole> {
                    UserId = user.Id,
                    Role = role,
                })
            );

            if(results.MatchedCount != 1) {
                throw new ServiceProblemException($"Merchant with ID {merchantId} not found", statusCode: StatusCodes.Status400BadRequest);
            }
            if(results.ModifiedCount != 1) {
                throw new ServiceProblemException($"Failed to add user as administrator of merchant {merchantId} (modified {results.ModifiedCount})", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        public Task DeleteMerchant(ObjectId merchantId) {
            return MerchantCollection.DeleteOneAsync(Builders<Merchant>.Filter.Eq(m => m.Id, merchantId));
        }

    }

}
