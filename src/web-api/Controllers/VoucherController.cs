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
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
                /*
                conn.Open();
                Console.WriteLine(conn.State);
                */
                //var aim = conn.Query<Aim>("select * from aim");
                //var aim = conn.Query<Aim>("select * from aim where Id < @FiltroId", new { FiltroId = 1 });
                //var aim = conn.Query<Aim>("select * from aim where Id = @Id", new { Id = 1 }

                //return aim.Select(a => a.Description); // Linq

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


                var source = DB.GetSourceById(conn, 123);

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

            //open the DB connection
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
                //sourceId parameter validation
                var source = DB.GetSourceById(conn, payload.SourceId);
                Console.WriteLine(source);

                //Conversion from crypto-payload
                CreatePayloadContent content = null; // TODO
                // to remove
                content = new CreatePayloadContent
                {
                    Id = Guid.NewGuid(),
                    SourceId = payload.SourceId,
                    Vouchers = new VoucherRequestInfo[]
                    {
                        new VoucherRequestInfo
                        {
                            Latitude = 123,
                            Longitude = 123,
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };

                //Voucher Creation in DB
                var otc = DB.CreateVoucherGeneration(conn, content);

                //Response POST
                return new CreateResponse
                {
                    Id = content.Id,
                    OtcGen = otc,
                    Timestamp = DateTime.UtcNow
                };
            }


            //return string.Format("ID {0}", payload.SourceId);

            //acquisisco i valori e li memorizzo su delle variabili
            //li inserisco nel database

            //restituisco le info necessarie per la risposta


        }

        // POST api/voucher/redeem
        [HttpPost("redeem")]
        public RedeemResponse Redeem([FromBody]RedeemPayload payload)
        {
            System.Console.Write("nonceID :");
            System.Console.WriteLine(payload.nonceId);
            System.Console.Write("nonceTs :");
            System.Console.WriteLine(payload.nonceTs);

            //connect to db
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {

                //nonce_ID validation, get the valid instance of vouchers
                var selectedVouchers = DB.GetVoucherById(conn, payload.nonceId); //TO DO 

                //System.Console.WriteLine("voucher : ", selectedVouchers.Count());
                return new RedeemResponse
                {
                    Payload = "crypted payload" //todo
                };
            }
    
        }
                
            
        

    }
}
