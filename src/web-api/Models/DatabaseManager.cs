using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Web.Api.DatabaseModels;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.Models;
using Dapper;
using Dapper.Contrib;
using System.Data.Common;
using Dapper.Contrib.Extensions;

namespace WomPlatform.Web.Api.Models
{
    public class DatabaseManager 
    {

        protected IConfiguration Configuration { get; private set; }
        /*
        public DatabaseManager()
        {
            Configuration = config;                  
        }*/


        
        // Start the connection with DB
        public DbConnection OpenConnection(IConfiguration config)
        {
            Configuration = config;
            var host = Configuration.GetSection("Database")["Host"];
            var port = Convert.ToInt32(Configuration.GetSection("Database")["Port"]);
            var username = Configuration.GetSection("Database")["Username"];
            var password = Configuration.GetSection("Database")["Password"];
            var schema = Configuration.GetSection("Database")["Schema"];

            Console.WriteLine("Connection opened");

            var connString = string.Format("server={0};port={1};uid={2};pwd={3};database={4}", host, port, username, password, schema);

            DbConnection conn = new MySqlConnection(connString);
            conn.Open();

            return conn;
        }

        //Execute a given sql string in a given connection
        public /*IEnumerable<Aim>*/ void ExecuteQuery(string connString, string sql)
        {
            using (DbConnection conn = new MySqlConnection(connString))
            {
                var res = conn.Query<Aim>(sql);

                //return res.Select(a => a.Id);
            }
        }

        //Controls if the sourceID matches with an instance in the db
        public Source GetSourceById(DbConnection conn, int sourceId)
        {
            return conn.QueryFirstOrDefault<Source>("select * from Sources where Id = @Id", new { Id = sourceId });
        }


       
        public string CreateVoucherGeneration(DbConnection conn, CreatePayloadContent creationParameters)
        {
            //new instance in the generation request table
            var generationRequestId = conn.Insert(new GenerationRequest
            {
                Source_Id = creationParameters.SourceId,
                Amount = creationParameters.Vouchers.Length,
                OTC = "",
                CreatedAt = DateTime.UtcNow,
                State = ""
            });

            // insert vouchers
            foreach (var voucher in creationParameters.Vouchers)
            {
                conn.Insert(new Voucher
                {
                    Id = Guid.NewGuid(),
                    Latitude = voucher.Latitude,
                    Longitude = voucher.Longitude,
                    Timestamp = DateTime.UtcNow,
                    Nonce = "",
                    OTC = "",
                    ID_Source = creationParameters.SourceId,
                    Type = "",
                    ID_GenerationRequest = generationRequestId
                });
            }

            // ...
            //var risultato = conn.QueryFirstOrDefault<Voucher>("select * from vouchers where Id = @Id", new { Id = tmpg });

            // Return OTC code of generation request
            return "";
        }

        public string PaymentRegister(DbConnection conn, RegisterInputPayload instance)
        {
            var insertPayment = conn.Insert(new PaymentRequest
            {
                POS_Id = instance.PosId,
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
        
        public IEnumerable<Voucher> GetVoucherById(DbConnection conn, Guid nonceId)
        {
            //doesn't work
            var results = conn.Query<Voucher>("select * from Vouchers where Nonce = '@nonce'", new { nonce = nonceId });
            //var results = conn.Query<Voucher>("select * from Vouchers");
            return results;
        }
        
    }
}
