﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Encloses common voucher operations.
    /// </summary>
    public class Operator {

        private readonly MongoDatabase Mongo;
        private readonly IConfiguration Configuration;
        private readonly ILogger<Operator> Logger;

        private readonly Random Random = new Random();

        private readonly int DefaultAutogeneratedPasswordLength;
        private readonly int DefaultSecretLength;
        private readonly int RequestInitialAttempts;

        public Operator(
            MongoDatabase mongo,
            IConfiguration configuration,
            ILogger<Operator> logger
        ) {
            Mongo = mongo;
            Configuration = configuration;
            Logger = logger;

            var confSection = Configuration.GetSection("PasswordSecurity");
            DefaultAutogeneratedPasswordLength = Convert.ToInt32(confSection["AutogeneratedLength"]);
            DefaultSecretLength = Convert.ToInt32(confSection["SecretLength"]);
            RequestInitialAttempts = Convert.ToInt32(confSection["RequestInitialAttempts"]);
        }

        private byte[] GenerateSecret() {
            byte[] buffer = new byte[DefaultSecretLength];
            Random.NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public async Task<(Guid Otc, string Password, int VoucherCount)> CreateGenerationRequest(
            Source source,
            VoucherCreatePayload.Content creationParameters,
            bool isPreVerified = false
        ) {
            Voucher CreateVoucher(Guid otc, VoucherCreatePayload.VoucherInfo info) {
                var secret = GenerateSecret().ToBase64();

                return info.CreationMode switch {
                    VoucherCreatePayload.VoucherCreationMode.SetLocationOnRedeem => new Voucher {
                        Secret = secret,
                        AimCode = info.Aim,
                        Timestamp = info.Timestamp,
                        Count = info.Count,
                        InitialCount = info.Count,
                        GenerationRequestId = otc,
                        Mode = VoucherCreationMode.SetLocationOnRedeem
                    },
                    _ => new Voucher {
                        Secret = secret,
                        AimCode = info.Aim,
                        Position = GeoJson.Point(GeoJson.Geographic(info.Longitude, info.Latitude)),
                        Timestamp = info.Timestamp,
                        Count = info.Count,
                        InitialCount = info.Count,
                        GenerationRequestId = otc,
                        Mode = VoucherCreationMode.Standard
                    }
                };
            }

            if(!creationParameters.SourceId.Equals(source.Id.ToString())) {
                throw new ArgumentException($"Incoherent POS IDs {creationParameters.SourceId} != {source.Id}");
            }

            // TODO: validate whether source is allowed to generate vouchers (check aims)

            var otc = Guid.NewGuid();
            var password = creationParameters.Password ?? Random.GeneratePassword(DefaultAutogeneratedPasswordLength);

            var genRequest = new GenerationRequest {
                Otc = otc,
                Amount = creationParameters.Vouchers.Length,
                TotalVoucherCount = (from v in creationParameters.Vouchers select v.Count).Sum(),
                CreatedAt = DateTime.UtcNow,
                Verified = isPreVerified,
                Attempts = RequestInitialAttempts,
                SourceId = source.Id,
                Nonce = creationParameters.Nonce,
                Password = password
            };
            await Mongo.AddGenerationRequest(genRequest);
            Logger.LogDebug("Generation request {0} persisted", otc);

            var vouchers = from v in creationParameters.Vouchers select CreateVoucher(otc, v);
            await Mongo.AddVouchers(vouchers);

            var voucherCount = vouchers.Sum(v => v.InitialCount);
            Logger.LogDebug("{0} voucher documents stored for {1} vouchers in request {2}", vouchers.Count(), voucherCount, otc);

            return (otc, password, voucherCount);
        }

        /// <summary>
        /// Verifies a voucher generation request.
        /// </summary>
        public async Task VerifyGenerationRequest(Guid otcGen) {
            var request = await Mongo.GetGenerationRequestByOtc(otcGen);
            if(request == null) {
                throw new ArgumentException("OTC code not valid");
            }

            if(!request.Verified) {
                request.Verified = true;
                await Mongo.UpdateGenerationRequest(request);
            }
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
            var request = await Mongo.GetGenerationRequestByOtc(otcGen);

            if(request == null) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} not found", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/otc-not-valid",
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound
                );
            }
            if(!request.Verified) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} not verified, cannot be performed", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/otc-not-valid",
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound
                );
            }
            if(request.PerformedAt.HasValue) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} already redeemed", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/operation-already-performed",
                    "Operation already performed",
                    StatusCodes.Status400BadRequest
                );
            }
            if(request.Void) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} has been voided", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/request-void",
                    "Request instance is void",
                    StatusCodes.Status410Gone
                );
            }
            if(request.Attempts <= 0) {
                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation {0} has no more attempts available", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/request-void",
                    "Request instance is void",
                    StatusCodes.Status410Gone
                );
            }
            if(!request.Password.Equals(password, StringComparison.Ordinal)) {
                request.Attempts -= 1;
                await Mongo.UpdateGenerationRequest(request);

                Logger.LogInformation(LoggingEvents.Operations, "Voucher generation password does not match");
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/wrong-password",
                    "Wrong password",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            var source = await Mongo.GetSourceById(request.SourceId);
            var vouchers = await Mongo.GetVouchersByGenerationRequest(otcGen);
            var vouchersRequiringLocation = (from v in vouchers where v.Mode == VoucherCreationMode.SetLocationOnRedeem select v).ToList();
            if(vouchersRequiringLocation.Count > 0) {
                // We require the user's location in order to redeem these vouchers
                if(!userLocation.HasValue) {
                    Logger.LogInformation(LoggingEvents.Operations, "Vouchers require user location on redemption");
                    throw new ServiceProblemException(
                        "https://wom.social/api/problems/location-not-provided",
                        "User location not provided",
                        StatusCodes.Status400BadRequest
                    );
                }

                var geoPoint = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(userLocation.Value.Longitude, userLocation.Value.Latitude));
                await Mongo.UpdateVoucherLocation(vouchersRequiringLocation, geoPoint);
                vouchersRequiringLocation.ForEach(v => { v.Position = geoPoint; });

                Logger.LogDebug("Update {0} voucher records with user location on redemption", vouchersRequiringLocation.Count);
            }

            request.PerformedAt = DateTime.UtcNow;
            await Mongo.UpdateGenerationRequest(request);

            return (source, vouchers);
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public async Task<(Guid Otc, string Password)> CreatePaymentRequest(
            Pos pointOfService,
            PaymentRegisterPayload.Content creationParameters,
            bool isPreVerified = false
        ) {
            if(!creationParameters.PosId.Equals(pointOfService.Id.ToString())) {
                throw new ArgumentException("Incoherent POS IDs");
            }

            var otc = Guid.NewGuid();
            var password = creationParameters.Password ?? Random.GeneratePassword(DefaultAutogeneratedPasswordLength);

            var payRequest = new PaymentRequest {
                Otc = otc,
                Amount = creationParameters.Amount,
                CreatedAt = DateTime.UtcNow,
                Verified = isPreVerified,
                IsPersistent = creationParameters.Persistent,
                PosId = pointOfService.Id,
                Nonce = creationParameters.Nonce,
                Password = password,
                AckUrlPocket = creationParameters.PocketAckUrl,
                AckUrlPos = creationParameters.PosAckUrl
            };
            if(creationParameters.SimpleFilter != null) {
                payRequest.Filter = new Filter {
                    Aims = creationParameters.SimpleFilter.Aim,
                    Bounds = creationParameters.SimpleFilter.GetBounds(),
                    MaxAge = creationParameters.SimpleFilter.MaxAge
                };
            }
            await Mongo.AddPaymentRequest(payRequest);

            return (otc, password);
        }

        /// <summary>
        /// Verifies a payment creation request.
        /// </summary>
        public async Task VerifyPaymentRequest(Guid otcPay) {
            var request = await Mongo.GetPaymentRequestByOtc(otcPay);
            if(request == null) {
                throw new ArgumentException("OTC code not valid");
            }

            if(!request.Verified) {
                request.Verified = true;
                await Mongo.UpdatePaymentRequest(request);
            }
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
                Logger.LogInformation(LoggingEvents.Operations, "Voucher {0} is older than {1} days (age {2})", vi.Id, filter.MaxAge.Value, DateTime.UtcNow.Subtract(voucher.Timestamp));
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
            var vouchers = (await Mongo.GetVouchersWithIds(voucherIds)).ToDictionary(v => v.Id);
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
            await Mongo.ReplaceVouchers(vouchers.Values);

            return vi.Count();
        }

        private async Task<int> ProcessPaymentLegacyVouchers(IEnumerable<PaymentConfirmPayload.VoucherInfo> vi, Filter filter) {
            if(vi.Count() == 0) {
                return 0;
            }

            var voucherIds = vi.Select(v => v.Id.ToLong());

            var vouchers = (await Mongo.GetLegacyVouchersWithIds(voucherIds)).ToDictionary(v => v.Id);
            if(voucherIds.Count() != vouchers.Count) {
                // One or more IDs did not load
                Logger.LogError(LoggingEvents.Operations, "One or more V1 vouchers does not exist ({0} expected, {1} found in DB)", voucherIds.Count(), vouchers.Count);
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
            await Mongo.ReplaceLegacyVouchers(vouchers.Values);

            return vouchers.Count;
        }

        public async Task<PaymentRequest> ProcessPayment(PaymentConfirmPayload.Content request) {
            var payment = await Mongo.GetPaymentRequestByOtc(request.Otc);
            if(payment == null) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not found", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/otc-not-valid",
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound
                );
            }
            if(!payment.Verified) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not verified, cannot be performed", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/otc-not-valid",
                    "OTC code does not exist",
                    StatusCodes.Status404NotFound
                );
            }
            if(!payment.IsPersistent && payment.Confirmations?.Count > 0) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment {0} not persistent and already performed", request.Otc);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/operation-already-performed",
                    "Operation already performed",
                    StatusCodes.Status400BadRequest
                );
            }
            if(!payment.Password.Equals(request.Password, StringComparison.Ordinal)) {
                Logger.LogInformation(LoggingEvents.Operations, "Payment password does not match");
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/wrong-password",
                    "Wrong password",
                    StatusCodes.Status422UnprocessableEntity
                );
            }
            if(request.Vouchers.Length != payment.Amount) {
                Logger.LogInformation(LoggingEvents.Operations, "{0} vouchers given instead of {1}", request.Vouchers.Length, payment.Amount);
                throw new ServiceProblemException(
                    "https://wom.social/api/problems/wrong-number-of-vouchers",
                    "Wrong number of vouchers supplied",
                    StatusCodes.Status400BadRequest,
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
                    "https://wom.social/api/problems/insufficient-valid-vouchers",
                    "Insufficient valid vouchers supplied",
                    StatusCodes.Status400BadRequest,
                    "required", payment.Amount.ToString(),
                    "supplied", (v2Count + v1Count).ToString()
                );
            }

            Logger.LogDebug(LoggingEvents.Operations, "Payment confirmed, vouchers updated");

            if(payment.Confirmations == null) {
                payment.Confirmations = new List<PaymentConfirmation>();
            }
            payment.Confirmations.Add(new PaymentConfirmation {
                PerformedAt = DateTime.UtcNow
            });
            await Mongo.UpdatePaymentRequest(payment);

            Logger.LogDebug(LoggingEvents.Operations, "Payment confirmation stored");

            return payment;
        }

    }

}
