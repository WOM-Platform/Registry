﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;
using WomPlatform.Web.Api.Utilities;

namespace WomPlatform.Web.Api.Service {
    public class PaymentService : BaseService {
        private readonly int DefaultAutogeneratedPasswordLength;
        private readonly int DefaultSecretLength;
        private readonly int RequestInitialAttempts;

        public PaymentService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
            var voucherSecuritySection = configuration.GetRequiredSection("Security").GetRequiredSection("Vouchers");
            DefaultAutogeneratedPasswordLength = Convert.ToInt32(voucherSecuritySection["AutogeneratedLength"]);
            DefaultSecretLength = Convert.ToInt32(voucherSecuritySection["SecretLength"]);
            RequestInitialAttempts = Convert.ToInt32(voucherSecuritySection["RequestInitialAttempts"]);
        }

        private Task<List<Voucher>> GetVouchersWithIds(IEnumerable<ObjectId> ids) {
            var filter = Builders<Voucher>.Filter.In(v => v.Id, ids);
            return VoucherCollection.Find(filter).ToListAsync();
        }

        private Task ReplaceVouchers(IEnumerable<Voucher> vouchers) {
            var replaces = from v in vouchers
                let filter = Builders<Voucher>.Filter.Eq(vf => vf.Id, v.Id)
                select new ReplaceOneModel<Voucher>(filter, v);
            return VoucherCollection.BulkWriteAsync(replaces);
        }

        private Task<List<LegacyVoucher>> GetLegacyVouchersWithIds(IEnumerable<long> ids) {
            var filter = Builders<LegacyVoucher>.Filter.In(v => v.Id, ids);
            return LegacyVoucherCollection.Find(filter).ToListAsync();
        }

        private Task ReplaceLegacyVouchers(IEnumerable<LegacyVoucher> vouchers) {
            var replaces = from v in vouchers
                let filter = Builders<LegacyVoucher>.Filter.Eq(vf => vf.Id, v.Id)
                select new ReplaceOneModel<LegacyVoucher>(filter, v);
            return LegacyVoucherCollection.BulkWriteAsync(replaces);
        }

        /// <summary>
        /// Retrieves a list of all persistent payments.
        /// </summary>
        public Task<List<PaymentRequest>> GetPersistentPayments() {
            var filter = Builders<PaymentRequest>.Filter.And(
                Builders<PaymentRequest>.Filter.Eq(pr => pr.Verified, true),
                Builders<PaymentRequest>.Filter.Eq(pr => pr.IsPersistent, true)
            );

            return PaymentRequestCollection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Create a new payment request.
        /// </summary>
        /// <param name="pointOfService">Owning POS instance.</param>
        /// <param name="amount">Amount of WOM vouchers request for the payment.</param>
        /// <param name="filter">Filter that WOM vouchers must satisfy.</param>
        /// <param name="password">Optional user-set password.</param>
        /// <param name="nonce">Optional nonce value.</param>
        /// <param name="ackUrlPocket">Optional ack URL for the Pocket app.</param>
        /// <param name="ackUrlPos">Optional ack URL for the POS.</param>
        /// <param name="isPersistent">Whether the payment is persistent.</param>
        /// <param name="isPreVerified">Whether the payment is verified upon creation.</param>
        public async Task<PaymentRequest> CreatePaymentRequest(
            Pos pointOfService,
            int amount,
            Filter filter,
            string password = null,
            string nonce = null,
            string ackUrlPocket = null,
            string ackUrlPos = null,
            bool isPersistent = false,
            bool isPreVerified = false
        ) {
            if(pointOfService == null) {
                throw new ArgumentNullException(nameof(pointOfService));
            }

            var otc = Guid.NewGuid();
            password ??= Random.GenerateNumericCode(DefaultAutogeneratedPasswordLength);

            var payRequest = new PaymentRequest {
                Otc = otc,
                Amount = amount,
                Filter = filter,
                CreatedAt = DateTime.UtcNow,
                Verified = isPreVerified,
                IsPersistent = isPersistent,
                PosId = pointOfService.Id,
                Nonce = nonce ?? string.Concat(pointOfService.Id, Guid.NewGuid()),
                Password = password,
                AckUrlPocket = ackUrlPocket,
                AckUrlPos = ackUrlPos,
            };
            await PaymentRequestCollection.InsertOneAsync(payRequest);

            return payRequest;
        }

        /// <summary>
        /// Verifies a payment creation request.
        /// </summary>
        public async Task VerifyPaymentRequest(Guid otcPay) {
            var results = await PaymentRequestCollection.UpdateOneAsync(
                Builders<PaymentRequest>.Filter.Eq(p => p.Otc, otcPay),
                Builders<PaymentRequest>.Update.Set(pr => pr.Verified, true)
            );

            if(results.MatchedCount != 1 || results.ModifiedCount != 1) {
                throw new Exception("Payment request does not exist or was already verified");
            }
        }

        public Task<PaymentRequest> GetPaymentRequestByOtc(Guid otcPay) {
            var filter = Builders<PaymentRequest>.Filter.Eq(p => p.Otc, otcPay);
            return PaymentRequestCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task UpdatePaymentRequest(PaymentRequest request) {
            var filter = Builders<PaymentRequest>.Filter.Eq(r => r.Otc, request.Otc);
            return PaymentRequestCollection.ReplaceOneAsync(filter, request);
        }

        /// <summary>
        /// Verifies single vouchers, checks filter satisfaction, and reduces count.
        /// </summary>
        private bool UpdateAndVerifyVouchers(
            PaymentConfirmPayload.VoucherInfo vi,
            IDictionary<ObjectId, Voucher> voucherMap,
            Filter filter
        ) {
            var objId = new ObjectId(vi.Id.GetBaseId());
            if(!voucherMap.ContainsKey(objId)) {
                Logger.LogError(LoggingEvents.Operations, "Looking for voucher {0} in Mongo vouchers, not found", vi.Id);
                return false;
            }

            var voucher = voucherMap[objId];
            if(!voucher.Secret.Equals(vi.Secret, StringComparison.InvariantCulture)) {
                Logger.LogInformation(LoggingEvents.Operations, "Secret for voucher {0} does not match", vi.Id);
                return false;
            }

            // Match filter
            if(filter?.Aims == null && voucher.AimCode.StartsWith("0")) {
                // No aim filter, but voucher is a "demo" voucher
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} matches 0 demo aim filter", vi.Id);
                return false;
            }

            if(filter?.Aims != null && !voucher.AimCode.StartsWith(filter.Aims)) {
                // Voucher does not match aim filter
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} does not match aim filter '{1}'", vi.Id, filter.Aims);
                return false;
            }

            if(filter?.Bounds != null && !filter.Bounds.Contains(voucher.Position.Coordinates)) {
                // Voucher not contained in geographical bounds
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} is outside geographical bounds", vi.Id);
                return false;
            }

            if(filter?.MaxAge != null && DateTime.UtcNow.Subtract(voucher.Timestamp) > TimeSpan.FromDays(filter.MaxAge.Value)) {
                // Voucher too old
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} is older than {1} days (age {2})",
                    vi.Id, filter.MaxAge.Value, DateTime.UtcNow.Subtract(voucher.Timestamp));
                return false;
            }

            // Update voucher count for update
            if(voucher.Count <= 0) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} already spent", vi.Id);
                return false;
            }

            voucher.Count -= 1;

            return true;
        }

        private async Task<int> ProcessPaymentVouchers(IEnumerable<PaymentConfirmPayload.VoucherInfo> vi, Filter filter) {
            if(vi.Count() == 0) {
                return 0;
            }

            // Extract distinct base IDs
            var voucherIds = vi.Select(v => v.Id.GetBaseId()).Distinct().Select(s => new ObjectId(s));

            // Retrieve distinct voucher instances from Mongo
            var vouchers = (await GetVouchersWithIds(voucherIds)).ToDictionary(v => v.Id);
            if(voucherIds.Count() != vouchers.Count) {
                // One or more distinct IDs did not load
                Logger.LogError(LoggingEvents.Operations, "One or more vouchers does not exist ({0} expected, {1} found in DB)", voucherIds.Count(), vouchers.Count);
                throw new ArgumentException("One or more voucher(s) not found");
            }

            if(!vi.All(v => UpdateAndVerifyVouchers(v, vouchers, filter))) {
                Logger.LogError(LoggingEvents.Operations, "One or more vouchers did not satisfy constraints");
                throw new ArgumentException("Invalid voucher(s)");
            }

            // TODO: this must be done in a two-step process
            await ReplaceVouchers(vouchers.Values);

            return vi.Count();
        }

        private async Task<int> ProcessPaymentLegacyVouchers(IEnumerable<PaymentConfirmPayload.VoucherInfo> vi, Filter filter) {
            if(vi.Count() == 0) {
                return 0;
            }

            var voucherIds = vi.Select(v => v.Id.ToLong());

            var vouchers = (await GetLegacyVouchersWithIds(voucherIds)).ToDictionary(v => v.Id);
            if(voucherIds.Count() != vouchers.Count) {
                // One or more IDs did not load
                Logger.LogError(LoggingEvents.Operations,
                    "One or more V1 vouchers does not exist ({0} expected, {1} found in DB)", voucherIds.Count(),
                    vouchers.Count);
                throw new ArgumentException("One or more voucher(s) not found");
            }

            if(!vi.All(expected => {
                   if(!vouchers.ContainsKey(expected.Id.ToLong())) {
                       Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} not found", expected.Id);
                       return false;
                   }

                   var voucher = vouchers[expected.Id.ToLong()];

                   if(voucher.Spent) {
                       Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} spent", voucher.Id);
                       return false;
                   }

                   if(!voucher.Secret.Equals(expected.Secret, StringComparison.InvariantCulture)) {
                       Logger.LogInformation(LoggingEvents.Operations, "Secret of voucher {0} not valid (is '{1}', expected '{2}')", voucher.Id, voucher.Secret, expected.Secret);
                       return false;
                   }

                   // Trust client-side validation for old vouchers 🤞

                   return true;
               })) {
                Logger.LogError(LoggingEvents.Operations, "One or more V1 vouchers did not satisfy constraints");
                throw new ArgumentException("Invalid voucher(s)");
            }

            // TODO: this must be done in a two-step process
            foreach(var v in vouchers.Values) {
                v.Spent = true;
            }

            await ReplaceLegacyVouchers(vouchers.Values);

            return vouchers.Count;
        }

        public async Task<PaymentRequest> ProcessPayment(PaymentConfirmPayload.Content request) {
            var payment = await GetPaymentRequestByOtc(request.Otc);
            if(payment == null) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not found", request.Otc);
                throw new ServiceProblemException(
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound,
                    type: "https://wom.social/api/problems/otc-not-valid"
                );
            }

            if(!payment.Verified) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not verified, cannot be performed", request.Otc);
                throw new ServiceProblemException(
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound,
                    type: "https://wom.social/api/problems/otc-not-valid"
                );
            }

            if(!payment.IsPersistent && payment.Confirmations?.Count > 0) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not persistent and already performed", request.Otc);
                throw new ServiceProblemException(
                    "Operation already performed",
                    StatusCodes.Status400BadRequest,
                    type: "https://wom.social/api/problems/operation-already-performed"
                );
            }

            if(!payment.Password.Equals(request.Password, StringComparison.Ordinal)) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment password does not match");
                throw new ServiceProblemException(
                    "Wrong password",
                    StatusCodes.Status422UnprocessableEntity,
                    type: "https://wom.social/api/problems/wrong-password"
                );
            }

            if(request.Vouchers.Length != payment.Amount) {
                Logger.LogInformation(LoggingEvents.Operations, "{0} vouchers given instead of {1}", request.Vouchers.Length, payment.Amount);
                throw new ServiceProblemException(
                    "Wrong number of vouchers supplied",
                    StatusCodes.Status400BadRequest,
                    type: "https://wom.social/api/problems/wrong-number-of-vouchers",
                    "required", payment.Amount.ToString(),
                    "supplied", request.Vouchers.Length.ToString()
                );
            }

            int v1Count = await ProcessPaymentLegacyVouchers(request.Vouchers.Where(v => !v.Id.ToString().Contains('/')), payment.Filter);
            int v2Count = await ProcessPaymentVouchers(request.Vouchers.Where(v => v.Id.Id.Contains('/')), payment.Filter);
            Logger.LogDebug("V1 vouchers spent {0}, V2 vouchers spent {1}", v1Count, v2Count);
            if(v1Count + v2Count < payment.Amount) {
                Logger.LogInformation(LoggingEvents.Operations, "Found {0} valid vouchers, less than requested ({1})", v2Count + v1Count, payment.Amount);
                throw new ServiceProblemException(
                    "Insufficient valid vouchers supplied",
                    StatusCodes.Status400BadRequest,
                    type: "https://wom.social/api/problems/insufficient-valid-vouchers",
                    "required", payment.Amount.ToString(),
                    "supplied", (v2Count + v1Count).ToString()
                );
            }

            Logger.LogDebug(LoggingEvents.Operations, "Payment confirmed, vouchers updated");

            if(payment.Confirmations == null) {
                payment.Confirmations = [];
            }

            payment.Confirmations.Add(new PaymentConfirmation {
                PerformedAt = DateTime.UtcNow
            });
            await UpdatePaymentRequest(payment);

            Logger.LogDebug(LoggingEvents.Operations, "Payment confirmation stored");

            return payment;
        }

        /// <summary>
        /// Marks vouchers as spent for a voucher transfer.
        /// </summary>
        public async Task<int> MarkVouchersForTransfer(VoucherTransferPayload.Content request) {
            int v1Count = await ProcessPaymentLegacyVouchers(request.Vouchers.Where(v => !v.Id.ToString().Contains('/')).Select(v => new PaymentConfirmPayload.VoucherInfo { Id = v.Id, Secret = v.Secret }), null);
            int v2Count = await ProcessPaymentVouchers(request.Vouchers.Where(v => v.Id.Id.Contains('/')).Select(v => new PaymentConfirmPayload.VoucherInfo { Id = v.Id, Secret = v.Secret }), null);
            Logger.LogDebug("V1 vouchers spent {0}, V2 vouchers spent {1}", v1Count, v2Count);

            return v1Count + v2Count;
        }


        public async Task<VoucherConsumptionStatsResponse> FetchTotalVouchersConsumedStats(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? merchantId
        ) {
            VoucherConsumptionStatsResponse response = new VoucherConsumptionStatsResponse();
            int totalConsumed = await FetchTotalVouchersConsumed(startDate, endDate, merchantId);
            List<VoucherByAimDTO> voucherByAims = await FetchTotalVouchersConsumedByAim(startDate, endDate, merchantId);
            List<MerchantRankDTO> merchantRankDtos = await GetMerchantRank(startDate, endDate, merchantId);
            List<TotalConsumedOverTimeDto> totalConsumedOverTimeDtos = await GetTotalConsumedVouchersOverTime(startDate, endDate, merchantId);

            return new VoucherConsumptionStatsResponse {
                TotalConsumed = totalConsumed,
                VoucherByAims = voucherByAims,
                MerchantRanks = merchantRankDtos,
                TotalConsumedOverTime = totalConsumedOverTimeDtos
            };
        }


        /// <summary>
        /// Get total amount of vouchers consumed from all the merchants in a period of time
        /// </summary>
        public async Task<int> FetchTotalVouchersConsumed(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? merchantId
        ) {
            var pipeline = new List<BsonDocument>();

            // execute a different aggregation pipeline if the user is filtering or not for date
            // if date filter
            if(startDate.HasValue && endDate.HasValue) {
                pipeline.AddRange(MongoQueryHelper.DatePaymentConfirmationCondition(startDate, endDate, "confirmations.performedAt"));
                // check if user is filtering for merchant name
                pipeline.AddRange(MongoQueryHelper.MerchantMatchFromPaymentRequestsCondition(merchantId));
                pipeline.Add(new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", BsonNull.Value }, {
                            "totalAmount",
                            new BsonDocument("$sum", "$amount")
                        }
                    }));
            }
            else {
                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("confirmations",
                            new BsonDocument {
                                { "$exists", true }, {
                                    "$ne",
                                    new BsonArray()
                                }
                            }))
                );
                // check if user is filtering for merchant name
                pipeline.AddRange(MongoQueryHelper.MerchantMatchFromPaymentRequestsCondition(merchantId));
                pipeline.Add(
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id", BsonNull.Value }, {
                                "totalAmount",
                                new BsonDocument("$sum",
                                    new BsonDocument("$multiply",
                                        new BsonArray {
                                            "$amount",
                                            new BsonDocument("$size", "$confirmations")
                                        }))
                            }
                        })
                );
            }

            var result = await PaymentRequestCollection.AggregateAsync<BsonDocument>(pipeline);
            var totalAmountConsumedDoc = await result.FirstOrDefaultAsync();

            // If no data was found
            if(totalAmountConsumedDoc == null) {
                return 0;
            }

            // if data found
            return totalAmountConsumedDoc["totalAmount"].AsInt32;
        }


        /// <summary>
        /// Get the consumed aim list from most used to least in a period of time
        /// </summary>
        public async Task<List<VoucherByAimDTO>> FetchTotalVouchersConsumedByAim(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? merchantId
        ) {
            var pipeline = new List<BsonDocument>();

            // Unwind confirmations array
            pipeline.Add(new BsonDocument("$unwind",
                new BsonDocument {
                    { "path", "$confirmations" },
                    { "includeArrayIndex", "string" },
                    { "preserveNullAndEmptyArrays", false }
                }));

            // check if user is filtering for merchant name
            pipeline.AddRange(MongoQueryHelper.MerchantMatchFromPaymentRequestsCondition(merchantId));

            // Project relevant fields
            pipeline.Add(
                new BsonDocument("$project",
                    new BsonDocument {
                        { "_id", 1 },
                        { "performedAt", "$confirmations.performedAt" },
                        { "posId", 1 },
                        { "amount", 1 }
                    }));

            // Match date range if provided
            if(startDate.HasValue && endDate.HasValue) {
                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("confirmations.performedAt",
                            new BsonDocument {
                                {
                                    "$gte",
                                    startDate
                                }, {
                                    "$lte",
                                    endDate
                                }
                            }))
                );
            }

            // Group by aim and calculate total amount
            pipeline.Add(new BsonDocument("$group",
                new BsonDocument {
                    {
                        "_id",
                        new BsonDocument("aim",
                            new BsonDocument("$ifNull",
                                new BsonArray {
                                    "$filter.aims",
                                    "NoAim"
                                }))
                    }, {
                        "totalAmount",
                        new BsonDocument("$sum", "$amount")
                    }
                }));

            // Project final output
            pipeline.Add(new BsonDocument("$project",
                new BsonDocument {
                    { "_id", 0 },
                    { "aimCode", "$_id.aim" },
                    { "totalAmount", 1 }
                }));

            // Sort by total amount descending
            pipeline.Add(new BsonDocument("$sort",
                new BsonDocument("totalAmount", -1)));

            var result = await PaymentRequestCollection.AggregateAsync<BsonDocument>(pipeline);
            var consumedVouchersByAim = await result.ToListAsync();

            // Map to a strongly-typed model
            var vouchersByAim = consumedVouchersByAim.Select(doc => new VoucherByAimDTO() {
                AimCode = doc["aimCode"].AsString,
                Amount = doc["totalAmount"].AsInt32
            }).ToList();

            return vouchersByAim;
        }

        /// <summary>
        /// Get merchant rank in a period of time
        /// </summary>
        public async Task<List<MerchantRankDTO>> GetMerchantRank(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? merchantId
        ) {
            var pipeline = new List<BsonDocument>();

            // filter date
            pipeline.AddRange(
                MongoQueryHelper.DatePaymentConfirmationCondition(startDate, endDate, "confirmations.performedAt"));
            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "Pos" },
                        { "localField", "posId" },
                        { "foreignField", "_id" },
                        { "as", "posData" }
                    }));

            pipeline.Add(
                new BsonDocument("$addFields",
                    new BsonDocument("merchantId",
                        new BsonDocument("$arrayElemAt",
                            new BsonArray {
                                "$posData.merchantId",
                                0
                            })))
            );

            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "Merchants" },
                        { "localField", "merchantId" },
                        { "foreignField", "_id" },
                        { "as", "merchant" }
                    }));
            // group sum if filter date
            if(startDate.HasValue && endDate.HasValue) {
                pipeline.Add(
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id", "$merchantId" }, {
                                "totalAmount",
                                new BsonDocument("$sum", "$amount")
                            }, {
                                "name",
                                new BsonDocument("$first", "$merchant.name")
                            }
                        })
                );
            }
            // group multiplication if no filter date
            else {
                pipeline.Add(
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id", "$merchantId" }, {
                                "totalAmount",
                                new BsonDocument("$sum",
                                    new BsonDocument("$multiply",
                                        new BsonArray {
                                            "$amount",
                                            new BsonDocument("$cond",
                                                new BsonDocument {
                                                    {
                                                        "if",
                                                        new BsonDocument("$isArray", "$confirmations")
                                                    }, {
                                                        "then",
                                                        new BsonDocument("$size", "$confirmations")
                                                    },
                                                    { "else", 0 }
                                                })
                                        }))
                            }, {
                                "name",
                                new BsonDocument("$first", "$merchant.name")
                            }
                        })
                );
            }

            pipeline.Add(
                new BsonDocument("$project",
                    new BsonDocument {
                        { "_id", 1 }, {
                            "name",
                            new BsonDocument("$arrayElemAt",
                                new BsonArray {
                                    "$name",
                                    0
                                })
                        },
                        { "totalAmount", 1 }
                    }));

            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("name",
                        new BsonDocument("$ne", BsonNull.Value))));

            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("totalAmount",
                        new BsonDocument("$gt", 0))));
            pipeline.Add(
                new BsonDocument("$setWindowFields",
                    new BsonDocument {
                        {
                            "sortBy",
                            new BsonDocument("totalAmount", -1)
                        }, {
                            "output",
                            new BsonDocument("rank",
                                new BsonDocument("$denseRank",
                                    new BsonDocument()))
                        }
                    }));
            if(merchantId.HasValue) {
                pipeline.Add(new BsonDocument("$match", new BsonDocument("_id", merchantId.Value)));
            }

            try {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await PaymentRequestCollection.AggregateAsync<BsonDocument>(pipeline);
                var merchantRankList = await result.ToListAsync();
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                Logger.LogInformation($"Rank Aggregation pipeline executed in {elapsedMilliseconds} ms");

                // Map to a strongly-typed model
                var merchantRank = merchantRankList.Select(doc => new MerchantRankDTO() {
                    Id = doc["_id"].AsObjectId,
                    Name = doc["name"].AsString,
                    Amount = doc["totalAmount"].AsInt32,
                    Rank = doc["rank"].AsInt32
                }).ToList();

                return merchantRank;
            }
            catch(Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");

                throw;
            }
        }

        public async Task<List<TotalConsumedOverTimeDto>> GetTotalConsumedVouchersOverTime(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? merchantId
        ) {
            var pipeline = new List<BsonDocument>();

            // if not specified a period of time set calculation on last year
            if(!startDate.HasValue && !endDate.HasValue) {
                endDate = DateTime.Today;
                startDate = DateTime.Today.AddYears(-1); // One year ago
            }

            // check if user is filtering for merchant name
            pipeline.AddRange(MongoQueryHelper.MerchantMatchFromPaymentRequestsCondition(merchantId));

            pipeline.Add(new BsonDocument("$unwind",
                new BsonDocument {
                    { "path", "$confirmations" },
                    { "includeArrayIndex", "string" },
                    { "preserveNullAndEmptyArrays", true }
                })
            );

            pipeline.Add(
                new BsonDocument("$addFields",
                    new BsonDocument("confirmations", "$confirmations.performedAt"))
            );

            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("confirmations",
                        new BsonDocument {
                            {
                                "$gte", startDate
                            }, {
                                "$lte", endDate
                            }
                        }))
            );
            var formatDate = DateRangeHelper.GetDateFormatForRange(startDate.Value, endDate.Value);

            pipeline.Add(
                new BsonDocument("$group",
                    new BsonDocument {
                        {
                            "_id",
                            new BsonDocument("$dateToString",
                                new BsonDocument {
                                    { "format", formatDate },
                                    { "date", "$confirmations" }
                                })
                        }, {
                            "totalAmount",
                            new BsonDocument("$sum", "$amount")
                        }
                    })
            );

            pipeline.Add(new BsonDocument("$sort",
                new BsonDocument("_id", 1))
            );

            var result = PaymentRequestCollection.Aggregate<BsonDocument>(pipeline);
            var consumedOverTime = await result.ToListAsync();

            // transform format date from MongoDB to .NET
            var netFormatDate = formatDate.Replace("%Y", "yyyy").Replace("%m", "MM").Replace("%d", "dd");

            // Determine the increment unit based on the date format
            Func<DateTime, DateTime> incrementDate = DateRangeHelper.setDateIncrement(netFormatDate);

            // Get the list of all dates between startDate and endDate
            var allDates = new List<string>();
            for(var date = startDate.Value.Date; date <= endDate.Value.Date; date = incrementDate(date)) {
                allDates.Add(date.ToString(netFormatDate));
            }

            // Map MongoDB results to DTO and create a dictionary by date
            var vouchersByAimDict = consumedOverTime
                .ToDictionary(
                    doc => doc["_id"].AsString,
                    doc => new TotalConsumedOverTimeDto {
                        Date = doc["_id"].AsString,
                        Total = doc["totalAmount"].AsInt32
                    }
                );

            // Create the final list with missing dates filled with 0
            var vouchersByAim = allDates.Select(date => {
                if(vouchersByAimDict.ContainsKey(date)) {
                    return vouchersByAimDict[date];
                }

                return new TotalConsumedOverTimeDto() {
                    Date = date,
                    Total = 0
                };
            }).ToList();

            return vouchersByAim;
        }
    }
}
