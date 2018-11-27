using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
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
        public static Source GetSourceById(this DbConnection conn, uint sourceId) {
            return conn.QueryFirstOrDefault<Source>(
                "SELECT * FROM Sources WHERE Id = @Id",
                new { Id = sourceId }
            );
        }

        /// <summary>
        /// Creates a new voucher generation instance.
        /// </summary>
        public static Guid CreateVoucherGeneration(this DbConnection conn, Guid sessionNonce, VoucherCreatePayloadContent creationParameters) {
            var otc = Guid.NewGuid();

            // New generation instance
            var generationRequestId = conn.Insert(new GenerationRequest {
                SourceId = creationParameters.SourceId,
                Amount = (ushort)creationParameters.Vouchers.Length,
                OtcGen = otc,
                CreatedAt = DateTime.UtcNow,
                Performed = false
            });

            foreach (var voucher in creationParameters.Vouchers) {
                var v = new Voucher {
                    Latitude = voucher.Latitude,
                    Longitude = voucher.Longitude,
                    Timestamp = DateTime.UtcNow,
                    SourceId = creationParameters.SourceId,
                    GenerationRequestId = (uint)generationRequestId
                };
                _random.NextBytes(v.Secret);

                conn.Insert(v);
            }

            return otc;
        }

        /// <summary>
        /// Gets a generation request by its unique OTC_gen code.
        /// </summary>
        public static GenerationRequest GetGenerationRequestByOtc(this DbConnection conn, Guid otcGen) {
            return conn.QueryFirstOrDefault<GenerationRequest>(
                "SELECT * FROM GenerationRequests WHERE OtcGen = @OtcGen",
                new { OtcGen = otcGen }
            );
        }

        public static PaymentRequest PaymentParameters(this DbConnection conn, string OTCPay) {
            var instance = conn.QueryFirstOrDefault<PaymentRequest>("select * from Paymentrequests where OTCPay = @otc", new { otc = OTCPay });

            return instance;
        }

        /*
        public void SetPayedRequest(DbConnection conn, string OTCPay) {
            conn.Query("UPDATE `voucherpiattaforma`.`paymentrequests` SET `State`='payed' WHERE `OTCPay`= @otc", new { otc = OTCPay });
        }
        */

    }

}
