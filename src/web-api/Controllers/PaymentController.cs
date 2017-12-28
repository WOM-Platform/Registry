using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.Models;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api.Controllers 
{
    // api/payment
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {

        protected IConfiguration Configuration { get; private set; }
        protected DatabaseManager DB = new DatabaseManager();

        public PaymentController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        //POST  api/payment/register
        [HttpPost("register")]
        public RegisterResponse Register([FromBody]RegisterPayload payload)
        {

            System.Console.Write("posID :");
            System.Console.WriteLine(payload.PosId);

            //insert new instance of payment in db
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
                RegisterInputPayload sample = new RegisterInputPayload();   //to do : decript the payload into this

                sample.PosId = payload.PosId;

                //insert the payload into the db
                var generatedOTC = DB.PaymentRegister(conn, sample);


                return new RegisterResponse
                {
                    PosId = 332,
                    nonceId = Guid.NewGuid(),
                    nonceTs = "XX:XX:XXXX",
                    OTCpay = generatedOTC
                };
            }
        }

        //GET api/payment/pay
        [HttpGet("pay")]
        public PayGetResponse Get()
        {
            return new PayGetResponse
            {
                amount = 3421,
                //filter = "di quel tipo",
                POS = null
            };

        }

        //POST api/payment/pay
        [HttpPost("pay")]
        public PayPostResponse Pay([FromBody]PayPayload payload)
        {
            return new PayPostResponse
            {
                ackPocket = "confirmed",
                state = "AlreadyPayed"
            };
        }

    }
}