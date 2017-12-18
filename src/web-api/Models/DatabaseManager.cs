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
using System.Data.Common;
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
        public string OpenConnection(IConfiguration config)
        {
            Configuration = config;
            var host = Configuration.GetSection("Database")["Host"];
            var port = Convert.ToInt32(Configuration.GetSection("Database")["Port"]);
            var username = Configuration.GetSection("Database")["Username"];
            var password = Configuration.GetSection("Database")["Password"];
            var schema = Configuration.GetSection("Database")["Schema"];

            System.Console.WriteLine("Connection opened");

            var connString = string.Format("server={0};port={1};uid={2};pwd={3};database={4}", host, port, username, password, schema);

            using (DbConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                Console.WriteLine(conn.State);
            }
            return connString;
        }

        //Execute a given sql string in a given connection
        public /*IEnumerable<Aim>*/ void ExecuteQuery(string connString, string sql)
        {
            using (DbConnection conn = new MySqlConnection(connString))
            {
                var res = conn.Query<Aim>(sql);

                //return res.Select(a => a.Description);
            }
        }
    }
}
