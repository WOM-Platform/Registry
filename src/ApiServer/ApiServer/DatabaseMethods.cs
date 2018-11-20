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

        /// <summary>
        /// Gets a source by its primary ID or null if not found.
        /// </summary>
        public static Source GetSourceById(this DbConnection conn, int sourceId) {
            return conn.QueryFirstOrDefault<Source>(
                "select * from Sources where Id = @Id",
                new { Id = sourceId }
            );
        }

        public static string CreateVoucherGeneration(this DbConnection conn, Guid sessionNonce, CreatePayloadContent creationParameters) {
            //new instance in the generation request table
            var generationRequestId = conn.Insert(new GenerationRequest {
                SourceId = creationParameters.SourceId,
                Amount = creationParameters.Vouchers.Length,
                Otc = "",
                CreatedAt = DateTime.UtcNow,
                State = ""
            });

            // insert vouchers
            foreach (var voucher in creationParameters.Vouchers) {
                conn.Insert(new Voucher {
                    Id = Guid.NewGuid(),
                    Latitude = voucher.Latitude,
                    Longitude = voucher.Longitude,
                    Timestamp = DateTime.UtcNow,
                    Nonce = sessionNonce, // TODO: move this to GenerationRequest
                    OTC = "",
                    SourceId = creationParameters.SourceId,
                    Type = "",
                    GenerationRequestId = generationRequestId
                });
            }

            // ...
            //var risultato = conn.QueryFirstOrDefault<Voucher>("select * from vouchers where Id = @Id", new { Id = tmpg });

            // Return OTC code of generation request
            return "";
        }

        public static string PaymentRegister(this DbConnection conn, RegisterInputPayload instance) {
            var insertPayment = conn.Insert(new PaymentRequest {
                PosId = instance.PosId,
                URLAckPocket = instance.ackPocket,
                URLAckPOS = instance.ackPos,
                Amount = instance.amount,
                OTCPay = "",
                CreatedAt = DateTime.UtcNow,
                State = "not payed"/*,
                JsonFilter = new FilterInfo
                {
                    position = null,
                    type = "that type"
                };*/
            });

            //returns the OTCpay to the payment request
            return "";
        }

        public static IEnumerable<Voucher> GetVoucherById(this DbConnection conn, Guid nonceId) {
            //doesn't work
            var results = conn.Query<Voucher>("select * from Vouchers where Nonce = @nonce", new { nonce = nonceId });
            //var results = conn.Query<Voucher>("select * from Vouchers");
            return results;
        }

        public static PaymentRequest PaymentParameters(this DbConnection conn, string OTCPay) {
            var instance = conn.QueryFirstOrDefault<PaymentRequest>("select * from Paymentrequests where OTCPay = @otc", new { otc = OTCPay });

            return instance;
        }

        /*
        public POS GetPosInfoById(DbConnection conn, long PosId) {
            var pos = conn.QueryFirstOrDefault<POS>("select * from Pos where ID = @id", new { id = PosId });
            return pos;
        }

        //return true if the voucher is not used yet, false otherwise
        public bool VerifyVoucher(DbConnection conn, Guid voucherId) {
            bool result = false;

            var voucher = conn.QueryFirstOrDefault<Voucher>("select * from Vouchers where ID =@id", new { id = voucherId });
            //if (voucher.ID_PaymentRequest == null)
            //the voucher is not spent yet
            result = true;
            return result;
        }

        public void SpendVoucher(DbConnection conn, Guid voucherId, string OTCPay) {
            //get the id of the payment request
            var payment = conn.QueryFirstOrDefault<PaymentRequest>("select * from Paymentrequests where OTCPay = @otc", new { otc = OTCPay });

            //write the payment id in the voucher (now is spent)
            conn.Query("UPDATE `voucherpiattaforma`.`vouchers` SET `ID_PaymentRequest`= @iDPay WHERE `ID`= @iDVoucher", new { idPay = payment.Id, iDVoucher = voucherId });

            //this query works, doesn't read the GUID in Voucher.ID
            //conn.Query("UPDATE `voucherpiattaforma`.`vouchers` SET `ID_PaymentRequest`= @iDPay WHERE `Latitude` = 123" ,new { idPay = payment.Id });
        }


        public void SetPayedRequest(DbConnection conn, string OTCPay) {
            conn.Query("UPDATE `voucherpiattaforma`.`paymentrequests` SET `State`='payed' WHERE `OTCPay`= @otc", new { otc = OTCPay });
        }
        */

        /*
        //sperimental
        public IEnumerable<Voucher> GETID (DbConnection conn)
        {
            var result = conn.Query<Voucher>("select * from Vouchers");
            return result;
        }*/

    }

}
