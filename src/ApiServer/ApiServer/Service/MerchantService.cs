using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;

namespace WomPlatform.Web.Api.Service {

    public class MerchantService : BaseService {

        public MerchantService(
            MongoClient client,
            ILogger<BaseService> logger
        ) : base(client, logger) {

        }

        public Task CreateMerchant(Merchant merchant) {
            ArgumentNullException.ThrowIfNull(merchant);

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
        public Task<(List<Merchant>, long Total)> ListMerchants(ObjectId? controlledBy, string textSearch, int page, int pageSize, MerchantListOrder orderBy, bool? enabled) {

            var filters = GetBasicMerchantFilter();
            if(controlledBy.HasValue) {
                filters.Add(Builders<Merchant>.Filter.Eq($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.UserId)}", controlledBy.Value));
            }
            if(!string.IsNullOrWhiteSpace(textSearch)) {
                filters.Add(Builders<Merchant>.Filter.Regex(m => m.Name, regex: new BsonRegularExpression(Regex.Escape(textSearch), "i")));
            }

            if (enabled.HasValue)
            {
                filters.Add(Builders<Merchant>.Filter.Eq(m => m.Enabled, enabled.Value));
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

        public async Task<Merchant> GetMerchantByName(string merchantName) {
            var filter = Builders<Merchant>.Filter.Eq(m => m.Name, merchantName);
            var merchant = await MerchantCollection.Find(filter).FirstOrDefaultAsync();

            return merchant ?? throw new ApplicationException($"Merchant with name '{merchantName}' not found.");
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
                    Builders<Merchant>.Filter.In($"{nameof(Merchant.Access)}.{nameof(AccessControlEntry<MerchantRole>.Role)}", [
                        MerchantRole.User, MerchantRole.Admin
                    ])
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
        public Task<List<MerchantWithAdmins>> GetAllMerchantsAndUsers() {
            var pipeline = new EmptyPipelineDefinition<Merchant>()
                .AppendStage<Merchant, Merchant, MerchantWithAdmins>(new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", UserCollectionName },
                    { "localField", "access.userId" },
                    { "foreignField", "_id" },
                    { "as", "adminUsers" }
                }))
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

        /// <summary>
        /// Soft-deletes a merchant by setting the "enabled" flag to false.
        /// </summary>
        public Task DeleteMerchant(ObjectId merchantId) {
            return MerchantCollection.UpdateOneAsync(
                Builders<Merchant>.Filter.Eq(m => m.Id, merchantId),
                Builders<Merchant>.Update.Set(m => m.Enabled, false)
            );
        }

        public async Task<List<MerchantReportDto>> GetMerchantReportDataAsync() {
            var pipeline = new BsonDocument[] {
                // 1. Filter disabled merchants
                new("$match", new BsonDocument {
                    { "enabled", new BsonDocument{
                        { "$ne", false }
                    }},
                }),

                // 2. Load user details and unwind for each user
                new("$lookup", new BsonDocument {
                    { "from", UserCollectionName },
                    { "localField", "access.userId" },
                    { "foreignField", "_id" },
                    { "as", "userDetails" }
                }),
                new("$unwind", new BsonDocument
                {
                    { "path", "$userDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                // 3. Load offers and unwind
                new("$lookup", new BsonDocument {
                    { "from", OfferCollectionName },
                    { "localField", "_id" },
                    { "foreignField", "merchant._id" },
                    { "as", "offer" }
                }),
                new("$unwind", new BsonDocument
                {
                    { "path", "$offer" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                // 4. Proiezione finale
                new("$project", new BsonDocument {
                    { "_id", 0 },
                    { "MerchantId", "$_id" },
                    { "MerchantName", "$name" },
                    { "MerchantFiscalCode", "$fiscalCode" },
                    { "MerchantLastUpdate", "$lastUpdate" },
                    { "UserEmail", "$userDetails.email" },
                    { "UserName", "$userDetails.name" },
                    { "UserSurname", "$userDetails.surname" },
                    { "OfferTitle", "$offer.title" },
                    { "OfferCost", "$offer.payment.cost" },
                    { "OfferCreation", "$offer.createdOn" },
                    { "OfferLastUpdate", "$offer.lastUpdate" },
                })
            };
            return await MerchantCollection.Aggregate<MerchantReportDto>(pipeline).ToListAsync();

        }

    }

}
