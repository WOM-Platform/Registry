﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;
using WomPlatform.Web.Api.InputModels.Generation;
using WomPlatform.Web.Api.Utilities;

namespace WomPlatform.Web.Api.Service {
    public class GenerationService : BaseService {
        private readonly SourceService _sourceService;

        private readonly int DefaultAutogeneratedPasswordLength;
        private readonly int DefaultSecretLength;
        private readonly int RequestInitialAttempts;

        public GenerationService(
            MongoClient client,
            SourceService sourceService,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
            _sourceService = sourceService;

            var voucherSecuritySection = configuration.GetRequiredSection("Security").GetRequiredSection("Vouchers");
            DefaultAutogeneratedPasswordLength = Convert.ToInt32(voucherSecuritySection["AutogeneratedLength"]);
            DefaultSecretLength = Convert.ToInt32(voucherSecuritySection["SecretLength"]);
            RequestInitialAttempts = Convert.ToInt32(voucherSecuritySection["RequestInitialAttempts"]);
        }

        public Task<GenerationRequest> GetGenerationRequestByOtc(Guid otc) {
            var filter = Builders<GenerationRequest>.Filter.Eq(r => r.Otc, otc);
            return GenerationRequestCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<List<Voucher>> GetVouchersByGenerationRequest(Guid otcGen) {
            var filter = Builders<Voucher>.Filter.Eq(v => v.GenerationRequestId, otcGen);
            return VoucherCollection.Find(filter).ToListAsync();
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
        }

        /// <summary>
        /// Create a new voucher generation request.
        /// </summary>
        /// <param name="source">Source ID.</param>
        /// <param name="voucherSpecifications">Specifications of the vouchers to generate.</param>
        /// <param name="password">Optional user-set password.</param>
        /// <param name="nonce">Optional nonce value.</param>
        /// <param name="isPreVerified">Whether the generation is verified upon creation.</param>
        public async Task<(GenerationRequest Request, Voucher[] Vouchers)> CreateGenerationRequest(
            Source source,
            VoucherGenerationSpecification[] voucherSpecifications,
            string password = null,
            string nonce = null,
            bool isPreVerified = false
        ) {
            if(source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if(voucherSpecifications == null || voucherSpecifications.Length == 0) {
                throw new ArgumentException("Voucher specifications must be set", nameof(voucherSpecifications));
            }

            var otc = Guid.NewGuid();
            password ??= Random.GenerateNumericCode(DefaultAutogeneratedPasswordLength);

            var totalVouchers = voucherSpecifications.Sum(spec => spec.Count);
            var vouchers = voucherSpecifications.Select(spec => {
                if(spec.Count <= 0) {
                    throw new ArgumentException(
                        "Voucher specification cannot generate 0 or negative amount of vouchers");
                }

                if(spec.CreationMode == VoucherCreationMode.Standard && spec.Location == null) {
                    throw new ArgumentException("Location required for vouchers with standard creation mode");
                }

                if(spec.Location != null) {
                    if(spec.Location.Latitude < -90 || spec.Location.Latitude > 90) {
                        throw new ArgumentException($"Latitude ({spec.Location.Latitude}) is not valid");
                    }

                    if(spec.Location.Longitude < -180 || spec.Location.Longitude > 180) {
                        throw new ArgumentException($"Longitude ({spec.Location.Longitude}) is not valid");
                    }
                }

                var secret = Random.GenerateSecret(DefaultSecretLength).ToBase64();

                return spec.CreationMode switch {
                    VoucherCreationMode.SetLocationOnRedeem => new Voucher {
                        Secret = secret,
                        AimCode = spec.Aim,
                        Timestamp = spec.Timestamp,
                        Count = spec.Count,
                        InitialCount = spec.Count,
                        GenerationRequestId = otc,
                        Mode = VoucherCreationMode.SetLocationOnRedeem
                    },
                    _ => new Voucher {
                        Secret = secret,
                        AimCode = spec.Aim,
                        Position = GeoJson.Point(GeoJson.Geographic(spec.Location.Longitude, spec.Location.Latitude)),
                        Timestamp = spec.Timestamp,
                        Count = spec.Count,
                        InitialCount = spec.Count,
                        GenerationRequestId = otc,
                        Mode = VoucherCreationMode.Standard
                    }
                };
            });
            await VoucherCollection.InsertManyAsync(vouchers);

            var generationRequest = new GenerationRequest {
                Otc = otc,
                Amount = voucherSpecifications.Length,
                TotalVoucherCount = totalVouchers,
                CreatedAt = DateTime.UtcNow,
                Verified = isPreVerified,
                // PerformedAt
                // Void
                Attempts = RequestInitialAttempts,
                SourceId = source.Id,
                Nonce = nonce ?? string.Concat(source.Id, Guid.NewGuid()),
                Password = password,
            };
            await GenerationRequestCollection.InsertOneAsync(generationRequest);

            return (generationRequest, vouchers.ToArray());
        }

        /// <summary>
        /// Verifies a voucher generation request.
        /// </summary>
        public async Task VerifyGenerationRequest(Guid otcGen) {
            (await GenerationRequestCollection.UpdateOneAsync(
                Builders<GenerationRequest>.Filter.Eq(gr => gr.Otc, otcGen),
                Builders<GenerationRequest>.Update.Set(gr => gr.Verified, true)
            )).Verify(expectedMatchesAndUpdates: 1);
        }

        /// <summary>
        /// Redeems vouchers tied to a given OTC_gen code and marks
        /// the generation request instance as completed.
        /// </summary>
        public async Task<(Source Source, IEnumerable<Voucher> Vouchers)> GenerateVouchers(
            Guid otcGen,
            string password,
            (double Latitude, double Longitude)? userLocation = null
        ) {
            var request = await GetGenerationRequestByOtc(otcGen);

            if(request == null) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} not found", request.Otc);
                throw new ServiceProblemException(
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound,
                    type: "https://wom.social/api/problems/otc-not-valid"
                );
            }

            if(!request.Verified) {
                Logger.LogInformation(LoggingEvents.Operations,
                    "Voucher generation {0} not verified, cannot be performed", request.Otc);
                throw new ServiceProblemException(
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound,
                    type: "https://wom.social/api/problems/otc-not-valid"
                );
            }

            if(request.PerformedAt.HasValue) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} already redeemed", request.Otc);
                throw new ServiceProblemException(
                    "Operation already performed",
                    StatusCodes.Status400BadRequest,
                    type: "https://wom.social/api/problems/operation-already-performed"
                );
            }

            if(request.Void) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} has been voided", request.Otc);
                throw new ServiceProblemException(
                    "Request instance is void",
                    StatusCodes.Status410Gone,
                    type: "https://wom.social/api/problems/request-void"
                );
            }

            if(request.Attempts <= 0) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} has no more attempts available",
                    request.Otc);
                throw new ServiceProblemException(
                    "Request instance is void",
                    StatusCodes.Status410Gone,
                    type: "https://wom.social/api/problems/request-void"
                );
            }

            if(!request.Password.Equals(password, StringComparison.Ordinal)) {
                (await GenerationRequestCollection.UpdateOneAsync(
                    Builders<GenerationRequest>.Filter.Eq(gr => gr.Otc, otcGen),
                    Builders<GenerationRequest>.Update.Inc(gr => gr.Attempts, -1)
                )).Verify(expectedMatchesAndUpdates: 1);

                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation password does not match");
                throw new ServiceProblemException(
                    "Wrong password",
                    StatusCodes.Status422UnprocessableEntity,
                    type: "https://wom.social/api/problems/wrong-password"
                );
            }

            var source = await _sourceService.GetSourceById(request.SourceId);
            var vouchers = await GetVouchersByGenerationRequest(otcGen);

            // Update vouchers requiring user location
            var vouchersRequiringLocation =
                (from v in vouchers where v.Mode == VoucherCreationMode.SetLocationOnRedeem select v).ToArray();
            if(vouchersRequiringLocation.Length > 0) {
                // We require the user's location in order to redeem these vouchers
                if(!userLocation.HasValue) {
                    Logger.LogInformation(LoggingEvents.Operations, "Vouchers require user location on redemption");
                    throw new ServiceProblemException(
                        "User location not provided",
                        StatusCodes.Status400BadRequest,
                        type: "https://wom.social/api/problems/location-not-provided"
                    );
                }

                var geoPoint =
                    GeoJson.Point(GeoJson.Geographic(userLocation.Value.Longitude, userLocation.Value.Latitude));
                Array.ForEach(vouchersRequiringLocation, v => { v.Position = geoPoint; });
                await VoucherCollection.UpdateManyAsync(
                    Builders<Voucher>.Filter.In(v => v.Id, from v in vouchersRequiringLocation select v.Id),
                    Builders<Voucher>.Update.Set(v => v.Position, geoPoint)
                );

                Logger.LogDebug("Updated {0} voucher records with user location on redemption",
                    vouchersRequiringLocation.Length);
            }

            (await GenerationRequestCollection.UpdateOneAsync(
                Builders<GenerationRequest>.Filter.Eq(gr => gr.Otc, otcGen),
                Builders<GenerationRequest>.Update.Set(gr => gr.PerformedAt, DateTime.UtcNow)
            )).Verify(expectedMatchesAndUpdates: 1);

            return (source, vouchers);
        }

        public async Task<VoucherGenerationRedemptionStatsResponse> FetchTotalVouchersGeneratedAndRedeemedStats(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? sourceId,
            string[] aimListFilter,
            double? latitude,
            double? longitude,
            int? radius
        ) {
            var (generatedVouchers, redeemedVouchers) =
                await FetchTotalVouchersGeneratedAndRedeemed(startDate, endDate, sourceId, aimListFilter);
            List<VoucherByAimDTO> voucherByAim =
                await FetchTotalVouchersGeneratedByAim(startDate, endDate, sourceId, aimListFilter);
            List<TotalGeneratedAndRedeemedOverTimeDto> totalGeneratedRedeemedVouchersOverTime =
                await GetTotalGeneratedRedeemedVouchersOverTime(startDate, endDate, sourceId, aimListFilter);
            List<SourceRankDTO> sourceRank = await GetSourceRank(startDate, endDate, sourceId, aimListFilter);

            return new VoucherGenerationRedemptionStatsResponse {
                TotalGenerated = generatedVouchers,
                TotalRedeemed = redeemedVouchers,
                VoucherByAim = voucherByAim,
                TotalGeneratedAndRedeemedOverTime = totalGeneratedRedeemedVouchersOverTime,
                SourceRank = sourceRank,
            };
        }

        /// <summary>
        /// Get total amount of vouchers generated from all the sources in a period of time
        /// </summary>
        public async Task<(int TotalCount, int RedeemedCount)> FetchTotalVouchersGeneratedAndRedeemed(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? sourceId,
            string[] aimListFilter
        ) {
            var pipeline = new List<BsonDocument>();

            pipeline.Add(new BsonDocument("$match",
                new BsonDocument("$and",
                    new BsonArray(MongoQueryHelper.DateMatchCondition(startDate, endDate, "timestamp")))));

            if(aimListFilter != null && aimListFilter.Any()) {
                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("aimCode",
                            new BsonDocument("$in",
                                new BsonArray(aimListFilter.Select(x => x.Trim())))))
                );
            }

            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "GenerationRequests" },
                        { "localField", "generationRequestId" },
                        { "foreignField", "_id" },
                        { "as", "generationRequest" }
                    })
            );
            pipeline.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$generationRequest" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", true }
                    })
            );


            // Add the instrumentName condition if filter applied
            pipeline.AddRange(MongoQueryHelper.SourceMatchFromVouchersCondition(sourceId));

            pipeline.Add(new BsonDocument("$project",
                new BsonDocument {
                    { "source", 1 },
                    { "initialCount", 1 },
                    { "generationRequest.performedAt", 1 }
                }));

            // Add the $group stage
            pipeline.Add(
                new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", BsonNull.Value }, {
                            "totalCount",
                            new BsonDocument("$sum", "$initialCount")
                        }, {
                            "redeemedCount",
                            new BsonDocument("$sum",
                                new BsonDocument("$cond",
                                    new BsonDocument {
                                        { "if", "$generationRequest.performedAt" },
                                        { "then", "$initialCount" },
                                        { "else", 0 }
                                    }))
                        }
                    })
            );

            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
            var totalAmountGeneratedDoc = await result.FirstOrDefaultAsync();


            // If no data was found
            if(totalAmountGeneratedDoc == null) {
                return (0, 0);
            }

            // if data found
            return (totalAmountGeneratedDoc["totalCount"].AsInt32, totalAmountGeneratedDoc["redeemedCount"].AsInt32);
        }

        /// <summary>
        /// Get total amount of vouchers generated grouped by aims
        /// </summary>
        public async Task<List<VoucherByAimDTO>> FetchTotalVouchersGeneratedByAim(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? sourceId,
            string[] aimListFilter
        ) {
            try {
                // Create the list to hold match conditions for the voucher collection
                List<BsonDocument> matchConditions =
                    MongoQueryHelper.DateMatchCondition(startDate, endDate, "timestamp");

                // Initialize the aggregation pipeline
                var pipeline = new List<BsonDocument>();


                // Add the date match conditions
                if(matchConditions.Count > 0) {
                    pipeline.Add(new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchConditions))));
                }

                if(aimListFilter != null && aimListFilter.Any()) {
                    pipeline.Add(
                        new BsonDocument("$match",
                            new BsonDocument("aimCode",
                                new BsonDocument("$in",
                                    new BsonArray(aimListFilter.Select(x => x.Trim())))))
                    );
                }

                if(sourceId.HasValue) {
                    pipeline.Add(
                        new BsonDocument("$lookup",
                            new BsonDocument {
                                { "from", "GenerationRequests" },
                                { "localField", "generationRequestId" },
                                { "foreignField", "_id" },
                                { "as", "generationRequest" }
                            })
                    );
                    pipeline.Add(
                        new BsonDocument("$unwind",
                            new BsonDocument {
                                { "path", "$generationRequest" },
                                { "includeArrayIndex", "string" },
                                { "preserveNullAndEmptyArrays", true }
                            })
                    );
                    // If instrumentName is provided, add the lookup and match conditions
                    var sourceMatchConditions = MongoQueryHelper.SourceMatchFromVouchersCondition(sourceId);
                    pipeline.AddRange(sourceMatchConditions);
                }

                // Add the grouping and sorting stages
                pipeline.Add(new BsonDocument {
                    {
                        "$group", new BsonDocument {
                            { "_id", "$aimCode" },
                            { "amount", new BsonDocument("$sum", "$initialCount") }
                        }
                    }
                });

                pipeline.Add(new BsonDocument {
                    { "$sort", new BsonDocument("amount", -1) }
                });

                // Execute the aggregation pipeline
                var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
                var generatedVouchersByAim = await result.ToListAsync();

                // Map to a strongly-typed model
                var vouchersByAim = generatedVouchersByAim.Select(doc => new VoucherByAimDTO() {
                    AimCode = doc["_id"].AsString,
                    Amount = doc["amount"].AsInt32
                }).ToList();

                return vouchersByAim;
            }
            catch(Exception ex) {
                // Handle exceptions as needed
                throw new ApplicationException("Error fetching voucher totals by aim", ex);
            }
        }


        /// <summary>
        /// Get number of unused vouchers based on the position
        /// </summary>
        public async Task<int> FetchVouchersAvailable(double? latitude, double? longitude, int? radius) {
            var pipeline = new List<BsonDocument>();
            if(radius.HasValue && latitude.HasValue && longitude.HasValue) {
                pipeline.Add(
                    new BsonDocument("$geoNear",
                        new BsonDocument {
                            {
                                "near",
                                new BsonDocument {
                                    { "type", "Point" },
                                    { "coordinates", new BsonArray { latitude, longitude } }
                                }
                            },
                            { "distanceField", "distance" },
                            { "maxDistance", radius * 100 },
                            { "spherical", true }
                        })
                );
            }

            pipeline.Add(new BsonDocument("$match",
                new BsonDocument("$or",
                    new BsonArray {
                        new BsonDocument("count",
                            new BsonDocument("$exists", false)),
                        new BsonDocument("count",
                            new BsonDocument("$gt", 0))
                    }))
            );
            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "GenerationRequests" },
                        { "localField", "generationRequestId" },
                        { "foreignField", "_id" },
                        { "as", "generationRequest" }
                    })
            );

            pipeline.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$generationRequest" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", true }
                    })
            );

            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("generationRequest.performedAt",
                        new BsonDocument {
                            { "$exists", true },
                            { "$ne", BsonNull.Value }
                        }))
            );
            pipeline.Add(
                new BsonDocument("$project",
                    new BsonDocument {
                        { "source", 1 },
                        { "initialCount", 1 },
                        { "count", 1 },
                        { "generationRequest.performedAt", 1 }
                    })
            );
            pipeline.Add(
                new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", BsonNull.Value }, {
                            "totalUnusedVouchers",
                            new BsonDocument("$sum", "$count")
                        }
                    })
            );

            pipeline.Add(
                new BsonDocument("$project",
                    new BsonDocument {
                        { "_id", 0 },
                        { "totalUnusedVouchers", 1 }
                    })
            );

            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);

            var document = await result.FirstOrDefaultAsync();
            if(document != null && document.Contains("totalUnusedVouchers")) {
                return document["totalUnusedVouchers"].ToInt32();
            }

            return 0;
        }

        public async Task<List<TotalGeneratedAndRedeemedOverTimeDto>> GetTotalGeneratedRedeemedVouchersOverTime(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId? sourceId,
            string[] aimListFilter
        ) {
            var pipeline = new List<BsonDocument>();

            // set calculation on last year if period of time is not specified
            if(!startDate.HasValue && !endDate.HasValue) {
                endDate = DateTime.Today; // Set to today
                startDate = DateTime.Today.AddYears(-1); // One year ago
            }

            var formatDate = DateRangeHelper.GetDateFormatForRange(startDate.Value, endDate.Value);

            startDate = startDate.Value.Date; // Truncate to midnight
            endDate = endDate.Value.Date;

            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("timestamp",
                        new BsonDocument {
                            {
                                "$gte", startDate
                            }, {
                                "$lte", endDate
                            }
                        }))
            );

            if(aimListFilter != null && aimListFilter.Any()) {
                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("aimCode",
                            new BsonDocument("$in",
                                new BsonArray(aimListFilter.Select(x => x.Trim())))))
                );
            }

            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "GenerationRequests" },
                        { "localField", "generationRequestId" },
                        { "foreignField", "_id" },
                        { "as", "generationRequest" }
                    })
            );

            pipeline.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$generationRequest" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", true }
                    })
            );

            // Add the instrumentName condition if filter applied
            pipeline.AddRange(MongoQueryHelper.SourceMatchFromVouchersCondition(sourceId));

            pipeline.Add(new BsonDocument("$project",
                new BsonDocument {
                    { "source", 1 },
                    { "initialCount", 1 },
                    { "timestamp", 1 },
                    { "generationRequest.performedAt", 1 }
                }));

            pipeline.Add(new BsonDocument("$group",
                new BsonDocument {
                    {
                        "_id",
                        new BsonDocument("$dateToString",
                            new BsonDocument {
                                { "format", formatDate },
                                { "date", "$timestamp" }
                            })
                    }, {
                        "generatedCount",
                        new BsonDocument("$sum", "$initialCount")
                    }, {
                        "redeemedCount",
                        new BsonDocument("$sum",
                            new BsonDocument("$cond",
                                new BsonDocument {
                                    { "if", "$generationRequest.performedAt" },
                                    { "then", "$initialCount" },
                                    { "else", 0 }
                                }))
                    }
                }));

            pipeline.Add(
                new BsonDocument("$sort",
                    new BsonDocument("_id", 1))
            );

            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
            var generatedRedeemedOverTime = await result.ToListAsync();

            // transform format date from MongoDB to .NET
            var netFormatDate = formatDate.Replace("%Y", "yyyy").Replace("%m", "MM").Replace("%d", "dd");

            // Determine the increment unit based on the date format
            Func<DateTime, DateTime> incrementDate = DateRangeHelper.setDateIncrement(netFormatDate);

            // Get the list of all dates between startDate and endDate
            var allDates = new List<string>();

            var currentDate = startDate.Value.Date; // Start with the initial date

            // While currentDate is less than or equal to endDate
            while(currentDate <= endDate.Value.Date) {
                allDates.Add(currentDate.ToString(netFormatDate));

                // Increment the date using the appropriate logic based on netFormatDate
                currentDate = incrementDate(currentDate); // Increment date accordingly
            }

            // Map MongoDB results to DTO and create a dictionary by date
            var vouchersByAimDict = generatedRedeemedOverTime
                .ToDictionary(
                    doc => doc["_id"].AsString,
                    doc => new TotalGeneratedAndRedeemedOverTimeDto {
                        Date = doc["_id"].AsString,
                        TotalGenerated = doc["generatedCount"].AsInt32,
                        TotalRedeemed = doc["redeemedCount"].AsInt32
                    }
                );

            // Create the final list with missing dates filled with 0
            var vouchersByAim = allDates.Select(date => {
                if(vouchersByAimDict.ContainsKey(date)) {
                    return vouchersByAimDict[date];
                }

                return new TotalGeneratedAndRedeemedOverTimeDto {
                    Date = date,
                    TotalGenerated = 0,
                    TotalRedeemed = 0
                };
            }).ToList();

            return vouchersByAim;
        }

        public async Task<List<SourceRankDTO>> GetSourceRank(DateTime? startDate, DateTime? endDate,
            ObjectId? sourceId, string[] aimListFilter) {
            var pipeline = new List<BsonDocument>();

            // Create the list to hold match conditions for the voucher collection
            List<BsonDocument> matchConditions =
                MongoQueryHelper.DateMatchCondition(startDate, endDate, "timestamp");


            // Add the date match conditions
            if(matchConditions.Count > 0) {
                pipeline.Add(new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchConditions))));
            }

            if(aimListFilter != null && aimListFilter.Any()) {
                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("aimCode",
                            new BsonDocument("$in",
                                new BsonArray(aimListFilter.Select(x => x.Trim())))))
                );
            }

            pipeline.Add(new BsonDocument("$sort",
                new BsonDocument("totalRedeemedAmount", -1)));

            // If instrumentName is provided, add the lookup and match conditions
            if(sourceId.HasValue) {
                var sourceMatchConditions = MongoQueryHelper.SourceMatchFromVouchersCondition(sourceId);
                pipeline.AddRange(sourceMatchConditions);
            }



            // $lookup: GenerationRequests
            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument
                    {
                        { "from", "GenerationRequests" },
                        { "localField", "generationRequestId" },
                        { "foreignField", "_id" },
                        { "as", "gen" }
                    }
                )
            );

            // $unwind: gen
            pipeline.Add(
                new BsonDocument("$unwind",
                    new BsonDocument
                    {
                        { "path", "$gen" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", false }
                    }
                )
            );

            // $lookup: Sources
            pipeline.Add(
                new BsonDocument("$lookup",
                    new BsonDocument
                    {
                        { "from", "Sources" },
                        { "localField", "gen.sourceId" },
                        { "foreignField", "_id" },
                        { "as", "source" }
                    }
                )
            );

            // $unwind: source
            pipeline.Add(
                new BsonDocument("$unwind",
                    new BsonDocument
                    {
                        { "path", "$source" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", false }
                    }
                )
            );

            // $group: Aggregate totals
            pipeline.Add(
                new BsonDocument("$group",
                    new BsonDocument
                    {
                        { "_id", "$source._id" },
                        { "name", new BsonDocument("$first", "$source.name") },
                        { "totalGeneratedAmount", new BsonDocument("$sum", "$initialCount") },
                        { "totalRedeemedAmount",
                            new BsonDocument("$sum",
                                new BsonDocument("$cond",
                                    new BsonDocument
                                    {
                                        { "if",
                                            new BsonDocument("$gt",
                                                new BsonArray
                                                {
                                                    "$gen.performedAt",
                                                    BsonNull.Value
                                                }
                                            )
                                        },
                                        { "then", "$initialCount" },
                                        { "else", 0 }
                                    }
                                )
                            )
                        }
                    }
                )
            );

            pipeline.Add(
                new BsonDocument(
                    "$unionWith",
                    new BsonDocument
                    {
                        { "coll", "Sources" },
                        { "pipeline",
                            new BsonArray
                            {
                                new BsonDocument("$project",
                                    new BsonDocument
                                    {
                                        { "_id", "$_id" },
                                        { "name", "$name" },
                                        { "totalGeneratedAmount",
                                            new BsonDocument("$literal", 0) },
                                        { "totalRedeemedAmount",
                                            new BsonDocument("$literal", 0) }
                                    })
                            } }
                    }
                    )
                );

            pipeline.Add(
                new BsonDocument("$group",
                    new BsonDocument
                    {
                        { "_id", "$_id" },
                        { "name",
                            new BsonDocument("$first", "$name") },
                        { "totalGeneratedAmount",
                            new BsonDocument("$max", "$totalGeneratedAmount") },
                        { "totalRedeemedAmount",
                            new BsonDocument("$max", "$totalRedeemedAmount") }
                    })
                );

            // $sort: totalRedeemedAmount descending
            pipeline.Add(
                new BsonDocument("$setWindowFields",
                    new BsonDocument {
                        {
                            "sortBy",
                            new BsonDocument("totalGeneratedAmount", -1)
                        }, {
                            "output",
                            new BsonDocument("rank",
                                new BsonDocument("$denseRank",
                                    new BsonDocument()))
                        }
                    }));

            try {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
                var sourceRankList = await result.ToListAsync();
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                Logger.LogInformation($"Rank Aggregation pipeline executed in {elapsedMilliseconds} ms");

                // Map to a strongly-typed model
                var sourceRank = sourceRankList.Select(doc => new SourceRankDTO() {
                    Id = doc["_id"].IsBsonNull ? ObjectId.Empty : doc["_id"].AsObjectId,
                    Name = doc["name"].AsString,
                    TotalGeneratedAmount = doc["totalGeneratedAmount"].AsInt32,
                    TotalRedeemedAmount = doc["totalRedeemedAmount"].AsInt32,
                    Rank = doc["rank"].AsInt32
                }).ToList();

                return sourceRank;
            }
            catch(Exception ex) {
                Logger.LogError($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
