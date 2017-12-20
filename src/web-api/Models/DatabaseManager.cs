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

        public Source GetSourceById(DbConnection conn, int sourceId)
        {
            return conn.QueryFirstOrDefault<Source>("select * from Sources where Id = @Id", new { Id = sourceId });
        }

        public string CreateVoucherGeneration(DbConnection conn, CreatePayloadContent creationParameters)
        {
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
                    ID_GenerationRequest = generationRequestId,
                });
            }

            // ...
            //var risultato = conn.QueryFirstOrDefault<Voucher>("select * from vouchers where Id = @Id", new { Id = tmpg });

            // Return OTC code of generation request
            return "";
        }
    }
}
