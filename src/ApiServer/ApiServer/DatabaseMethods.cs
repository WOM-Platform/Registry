using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WomPlatform.Web.Api.DatabaseModels;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Extensions methods that operate on a database connection.
    /// </summary>
    public static class DatabaseMethods {

        private static Random _random = new Random();

        private static JsonSerializerSettings DatabaseSerializerSettings { get; } = new JsonSerializerSettings {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Gets a source by its primary ID or null if not found.
        /// </summary>
        public static Source GetSourceById(this DataContext data, long sourceId) {
            return (from s in data.Sources
                    where s.Id == sourceId
                    select s).SingleOrDefault();
        }

        /// <summary>
        /// Gets a POS by its primary ID or null if not found.
        /// </summary>
        public static POS GetPosById(this DataContext data, long posId) {
            return (from p in data.POS
                    where p.Id == posId
                    select p).SingleOrDefault();
        }

        /// <summary>
        /// Gets all aims.
        /// </summary>
        public static IEnumerable<Aim> GetAims(this DataContext data) {
            return from a in data.Aims
                   orderby a.Code
                   select a;
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public static Guid CreateVoucherGeneration(this DataContext data, VoucherCreatePayload.Content creationParameters) {
            // TODO: validate whether source is allowed to generate vouchers (check aims)

            var otc = Guid.NewGuid();

            var genRequest = new GenerationRequest {
                Amount = (ushort)creationParameters.Vouchers.Length,
                OtcGen = otc,
                CreatedAt = DateTime.UtcNow,
                Verified = false,
                PerformedAt = null,
                Void = false,
                SourceId = creationParameters.SourceId,
                Nonce = creationParameters.Nonce.FromBase64(),
                Password = creationParameters.Password
            };
            data.GenerationRequests.Add(genRequest);
            data.SaveChanges();

            foreach (var voucher in creationParameters.Vouchers) {
                var v = new Voucher {
                    AimCode = voucher.Aim,
                    Latitude = voucher.Latitude,
                    Longitude = voucher.Longitude,
                    Timestamp = voucher.Timestamp,
                    GenerationRequestId = genRequest.Id,
                    Spent = false
                };
                _random.NextBytes(v.Secret);

                data.Vouchers.Add(v);
            }
            data.SaveChanges();

            return otc;
        }

        /// <summary>
        /// Verifies a voucher generation request.
        /// </summary>
        public static void VerifyGenerationRequest(this DataContext data, Guid otcGen) {
            var request = (from g in data.GenerationRequests
                           where g.OtcGen == otcGen
                           select g).SingleOrDefault();

            if(request == null) {
                throw new ArgumentException("OTC code not valid");
            }

            if(!request.Verified) {
                request.Verified = true;
                data.SaveChanges();
            }
        }

        /// <summary>
        /// Redeems vouchers tied to a given OTC_gen code and marks
        /// the generation request instance as completed.
        /// </summary>
        public static IEnumerable<Voucher> GenerateVouchers(this DataContext data, Guid otcGen, string password) {
            var request = (from g in data.GenerationRequests
                           where g.OtcGen == otcGen
                           select g).SingleOrDefault();

            if(request == null) {
                throw new ArgumentException("OTC code not valid");
            }
            if (!request.Verified) {
                throw new ArgumentException("OTC code not verified");
            }
            if (request.PerformedAt.HasValue) {
                throw new InvalidOperationException("Vouchers already redeemed");
            }
            if (request.Void) {
                throw new InvalidOperationException("Voucher generation request has been voided");
            }
            if (!request.Password.Equals(password, StringComparison.Ordinal)) {
                request.Void = true;
                data.SaveChanges();

                throw new ArgumentException("Password does not match, request has been voided");
            }

            var vouchers = from v in data.Vouchers
                           where v.GenerationRequestId == request.Id
                           select v;

            request.PerformedAt = DateTime.UtcNow;
            data.SaveChanges();

            return vouchers;
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public static Guid CreatePaymentRequest(this DataContext data, PaymentRegisterPayload.Content creationParameters) {
            var otc = Guid.NewGuid();

            var payRequest = new PaymentRequest {
                Amount = creationParameters.Amount,
                JsonFilter = JsonConvert.SerializeObject(
                    new Filter {
                        Simple = creationParameters.SimpleFilter
                    },
                    DatabaseSerializerSettings
                ),
                OtcPay = otc,
                UrlAckPocket = creationParameters.PocketAckUrl,
                UrlAckPos = creationParameters.PosAckUrl,
                CreatedAt = DateTime.UtcNow,
                Verified = false,
                PerformedAt = null,
                Void = false,
                PosId = creationParameters.PosId,
                Nonce = creationParameters.Nonce.FromBase64(),
                Password = creationParameters.Password
            };
            data.PaymentRequests.Add(payRequest);
            data.SaveChanges();

            return otc;
        }

        /// <summary>
        /// Verifies a payment creation request.
        /// </summary>
        public static void VerifyPaymentRequest(this DataContext data, Guid otcPay) {
            var request = (from g in data.PaymentRequests
                           where g.OtcPay == otcPay
                           select g).SingleOrDefault();

            if (request == null) {
                throw new ArgumentException("OTC code not valid");
            }

            if (!request.Verified) {
                request.Verified = true;
                data.SaveChanges();
            }
        }

        public static (PaymentRequest payment, Filter filter) GetPaymentRequestInfo(this DataContext data, Guid otcPay, string password) {
            var request = (from g in data.PaymentRequests
                           where g.OtcPay == otcPay
                           select g)
                           .Include(nameof(PaymentRequest.Pos))
                           .SingleOrDefault();

            if (request == null) {
                throw new ArgumentException("OTC code not valid");
            }
            if (!request.Verified) {
                throw new ArgumentException("OTC code not verified");
            }
            if (request.PerformedAt.HasValue) {
                throw new InvalidOperationException("Payment already confirmed");
            }
            if (request.Void) {
                throw new InvalidOperationException("Payment has been voided");
            }
            if (!request.Password.Equals(password, StringComparison.Ordinal)) {
                request.Void = true;
                data.SaveChanges();

                throw new ArgumentException("Password does not match, payment has been voided");
            }

            Filter f = null;
            if(request.JsonFilter != null) {
                f = JsonConvert.DeserializeObject<Filter>(request.JsonFilter, DatabaseSerializerSettings);
            }

            return (request, f);
        }

        public static PaymentRequest ProcessPayment(this DataContext data, PaymentConfirmPayload.Content request) {
            (var payment, var filter) = GetPaymentRequestInfo(data, request.Otc, request.Password);

            if(request.Vouchers.Length != payment.Amount) {
                throw new ArgumentException("Wrong number of vouchers");
            }

            // Fetch non-spent vouchers from DB
            var voucherIds = request.Vouchers.Select(v => v.Id).ToArray();
            var voucherMap = (from v in data.Vouchers
                              where voucherIds.Contains(v.Id)
                              where !v.Spent
                              select v).ToDictionary(v => v.Id);

            bool CheckVoucher(PaymentConfirmPayload.VoucherInfo vi) {
                if (!voucherMap.ContainsKey(vi.Id)) {
                    // Voucher not found or already spent
                    return false;
                }
                var voucher = voucherMap[vi.Id];

                if(!vi.Secret.FromBase64().SequenceEqual(voucher.Secret)) {
                    // Secret does not match
                    return false;
                }

                if(filter?.Simple?.Aim != null && !voucher.AimCode.StartsWith(filter.Simple.Aim)) {
                    // Voucher does not match aim filter
                    return false;
                }

                if(filter?.Simple?.GeoBounds != null) {
                    // TODO: implement geo filtering
                }

                if(filter?.Simple?.MaxAge != null && DateTime.UtcNow.Subtract(voucher.Timestamp) > TimeSpan.FromDays(filter.Simple.MaxAge.Value)) {
                    // Voucher too old
                    return false;
                }

                return true;
            }

            // Check all voucher secrets
            if(!request.Vouchers.All(CheckVoucher)) {
                throw new ArgumentException("One or more vouchers not valid for payment");
            }

            // Update payment status
            foreach(var v in voucherMap.Values) {
                v.Spent = true;
            }
            payment.Void = true;
            data.SaveChanges();

            return payment;
        }

    }

}
