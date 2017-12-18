using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using WomPlatform.Web.Api.Models;
using Dapper;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api.Controllers
{
    [Route("api/[controller]")]
    public class VoucherController : Controller
    {
        
        protected IConfiguration Configuration { get; private set; }
        protected DatabaseManager DB = new DatabaseManager();

        public VoucherController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        // GET api/voucher
        //percorso di prova
        [HttpGet]
        public IEnumerable<string> Get()
        {
            /*
            var host = Configuration.GetSection("Database")["Host"];
            var port = Convert.ToInt32(Configuration.GetSection("Database")["Port"]);
            var username = Configuration.GetSection("Database")["Username"];
            var password = Configuration.GetSection("Database")["Password"];
            var schema = Configuration.GetSection("Database")["Schema"];
            var connString = string.Format("server={0};port={1};uid={2};pwd={3};database={4}", host, port, username, password, schema);*/


            var connString = DB.OpenConnection(Configuration);
                Console.WriteLine("Connection: {0}", connString);
            //}
            using (DbConnection conn = new MySqlConnection(connString))
            {
                /*
                conn.Open();
                Console.WriteLine(conn.State);
                */
                //var aim = conn.Query<Aim>("select * from aim");
                var aim = conn.Query<Aim>("select * from aim where Id < @FiltroId", new { FiltroId = 1 });
                //var aim = conn.Query<Aim>("select * from aim where Id = @Id", new { Id = 1 }

                return aim.Select(a => a.Description); // Linq

                /*var response = new List<string>();
                foreach(var a in aim)
                {
                    response.Add(a.Description);
                }
                return response;*/

                /*var com = conn.CreateCommand();
                com.CommandType = System.Data.CommandType.Text;
                com.CommandText = "SELECT count(*) FROM Voucher";
                var count = com.ExecuteScalar();
                Console.WriteLine("Count: " + count);*/


            }

            // Sintassi finale:
           // using(var conn = DatabaseManager.Open(Configuration))
            {
                // Crea i voucher
                //...

                // ritorna i dati finali
            }

            return null; //new string[] { Configuration.GetSection("Database")["Host"], Configuration.GetSection("Database")["Port"] };
        }

        //POST api/voucher/create
        [HttpPost("create")]
        public CreateResponse Create([FromBody]CreatePayload payload)
        {
            //stampo i dati inviati nella richiesta POST, verranno poi inseriti nel database
            System.Console.Write("Source id: ");
            System.Console.WriteLine(payload.SourceId);
            System.Console.Write("payload :");
            System.Console.WriteLine(payload.Payload);

            //return string.Format("ID {0}", payload.SourceId);

            //acquisisco i valori e li memorizzo su delle variabili
            //li inserisco nel database

            //restituisco le info necessarie per la risposta

            return new CreateResponse
            {
                Id = Guid.NewGuid(),
                OtcGen = "Prova",
                Timestamp = "XX:XX:XXXX",
                nVoucher = new VoucherInfo[]
                {
                    new VoucherInfo()
                    {
                        VoucherID = 1,
                        Longitude = 123
                    },
                    new VoucherInfo()
                    {
                        VoucherID = 2,
                        Longitude = 124
                    }
                }
            };
        }

        // POST api/voucher/redeem
        [HttpPost("redeem")]
        public RedeemResponse Redeem([FromBody]RedeemPayload payload)
        {
            System.Console.Write("nonceTs :");
            System.Console.WriteLine(payload.nonceTs);
            System.Console.Write("nonceID :");
            System.Console.WriteLine(payload.nonceId);



            return new RedeemResponse
            {
                //da aggiungere il payload giusto
                Payload = null/*new RedeemResponsePayload {
                    nonceId = Guid.NewGuid(),
                    nonceTs = "xx:xx:xxxx"
                };*/
            };
        }

    }
}
