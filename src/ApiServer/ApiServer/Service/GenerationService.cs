﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;
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

        /// <summary>
        /// Get total amount of vouchers generated from all the sources in a period of time
        /// </summary>
        public async Task<List<BsonDocument>> GetTotalAmountOfGeneratedVouchers(DateTime startDate, DateTime endDate) {
            string dateFormat = DateRangeHelper.GetDateFormatForRange(startDate, endDate);
            var pipeline = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument {
                    {
                        "createdAt", new BsonDocument {
                            { "$gte", startDate },
                            { "$lte", endDate }
                        }
                    }
                }),
                new BsonDocument("$group", new BsonDocument {
                    {
                        "_id", new BsonDocument("$dateToString", new BsonDocument {
                            { "format", dateFormat },
                            { "date", "$createdAt" }
                        })
                    },
                    { "totalAmountGenerated", new BsonDocument("$sum", "$amount") }
                }),
                new BsonDocument("$sort", new BsonDocument {
                    { "_id", -1 }
                })
            };

            var result = await GenerationRequestCollection.AggregateAsync<BsonDocument>(pipeline);
            return await result.ToListAsync();
        }

        /// <summary>
        /// Get total amount of vouchers redeemed from all the sources in a period of time
        /// </summary>
        public async Task<List<BsonDocument>> GetTotalAmountOfRedeemedVouchers(DateTime startDate, DateTime endDate) {
            string dateFormat = DateRangeHelper.GetDateFormatForRange(startDate, endDate);
            var pipeline = new BsonDocument[] {
                new BsonDocument("$match",
                    new BsonDocument("timestamp",
                        new BsonDocument {
                            { "$gte", startDate },
                            { "$lte", endDate }
                        })),
                new BsonDocument("$group",
                    new BsonDocument {
                        {
                            "_id",
                            new BsonDocument("$dateToString",
                                new BsonDocument {
                                    { "format", dateFormat },
                                    { "date", "$timestamp" }
                                })
                        }, {
                            "totalAmount",
                            new BsonDocument("$sum", "$count")
                        }
                    }),
                new BsonDocument("$sort",
                    new BsonDocument("_id", -1))
            };
            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
            return await result.ToListAsync();
        }

        /// <summary>
        /// Get the redeemed aim list from most used to least in a period of time
        /// </summary>
        public async Task<List<BsonDocument>> GetRedeemedAimList(DateTime startDate, DateTime endDate) {
            string dateFormat = DateRangeHelper.GetDateFormatForRange(startDate, endDate);
            var pipeline = new BsonDocument[] {
                new BsonDocument("$match",
                    new BsonDocument("timestamp",
                        new BsonDocument {
                            {
                                "$gte",
                                new DateTime(2022, 7, 1, 0, 0, 0)
                            }, {
                                "$lte",
                                new DateTime(2023, 7, 1, 0, 0, 0)
                            }
                        })),
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "GenerationRequests" },
                        { "localField", "generationRequestId" },
                        { "foreignField", "_id" },
                        { "as", "generatedRequest" }
                    }),
                new BsonDocument("$addFields",
                    new BsonDocument("generatedAmount",
                        new BsonDocument("$arrayElemAt",
                            new BsonArray {
                                "$generatedRequest.amount",
                                0
                            }))),
                new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", "$aimCode" }, {
                            "totalAmount",
                            new BsonDocument("$sum", "$generatedAmount")
                        }
                    }),
                new BsonDocument("$sort",
                    new BsonDocument("totalAmount", -1))
            };
            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
            return await result.ToListAsync();
        }

        /// <summary>
        /// Get number of unused vouchers based on the position
        /// </summary>
        public async Task<List<BsonDocument>> GetNumberUnusedVouchers(double latitude, double longitude, int radius) {
            var pipeline = new BsonDocument[] {
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
                    }),
                new BsonDocument("$match",
                    new BsonDocument("$or",
                        new BsonArray {
                            new BsonDocument("count",
                                new BsonDocument("$exists", false)),
                            new BsonDocument("count",
                                new BsonDocument("$gt", 0))
                        })),
                new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", BsonNull.Value }, {
                            "totalUnusedVouchers",
                            new BsonDocument("$sum", "$count")
                        }
                    }),
                new BsonDocument("$project",
                    new BsonDocument {
                        { "_id", 0 },
                        { "totalUnusedVouchers", 1 }
                    })
            };

            var result = await VoucherCollection.AggregateAsync<BsonDocument>(pipeline);
            return await result.ToListAsync();
        }
    }
}
