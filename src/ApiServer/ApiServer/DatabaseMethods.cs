using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Web.Api.DatabaseModels;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Extensions methods that operate on a database connection.
    /// </summary>
    public static class DatabaseMethods {

        private static Random _random = new Random();

        /// <summary>
        /// Gets a source by its primary ID or null if not found.
        /// </summary>
        public static Source GetSourceById(this DataContext data, long sourceId) {
            return (from s in data.Sources
                    where s.Id == sourceId
                    select s).SingleOrDefault();
        }

        /// <summary>
        /// Gets all aims.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static IEnumerable<Aim> GetAims(this DataContext data) {
            return from a in data.Aims
                   orderby a.Code
                   select a;
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public static Guid CreateVoucherGeneration(this DataContext data, VoucherCreatePayload.Content creationParameters) {
            // TODO: validate whether source is allowed to generate vouchers

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
        /// Gets a generation request by its unique OTC_gen code.
        /// </summary>
        public static GenerationRequest GetGenerationRequestByOtc(this DataContext data, Guid otcGen) {
            return (from g in data.GenerationRequests
                    where g.OtcGen == otcGen
                    select g).SingleOrDefault();
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
                throw new ArgumentException("OTC code matches no voucher generation request");
            }
            if (!request.Verified) {
                throw new InvalidOperationException("Voucher generation request not verified");
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

    }

}
