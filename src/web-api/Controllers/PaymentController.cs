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
            System.Console.Write("payload :");
            System.Console.WriteLine(payload.Payload);

            //to do : decrypt the payload from RegisterPayload to RegisterPayloadContent

            //insert new instance of payment in db
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
                RegisterInputPayload sample = new RegisterInputPayload();   //to do : decript the payload into this

                //this data are included in the decrypted input payload
                sample.PosId = payload.PosId;
                sample.ackPocket = "";
                sample.amount = 1;
                sample.ackPos = "";
            

                //insert the payload into the db
                var generatedOTC = DB.PaymentRegister(conn, sample);


                return new RegisterResponse
                {
                    PosId = payload.PosId,
                    nonceId = Guid.NewGuid(),
                    nonceTs = "XX:XX:XXXX",
                    OTCpay = generatedOTC
                };
            }
        }

        //GET api/payment/{
        [HttpGet("{OTCPay}")]
        public PayGetResponse Get(string OTCpay)
        {
            //check in the DB if the request can be satisfied
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
                //check if exixst the parameters of the payment
                var payment = DB.PaymentParameters(conn, OTCpay);

                //get the pos info for the response
                var pos = DB.GetPosInfoById(conn, payment.ID_POS);

                return new PayGetResponse
                {
                    amount = payment.Amount,
                    //filter = "di quel tipo",
                    POS = new PosInfo
                    {
                        name = pos.Name,
                        description = pos.Description,
                        URL = pos.URLPOS
                    }
                };
            }

        }

        //POST api/payment/pay
        [HttpPost("pay")]
        public PayPostResponse Pay([FromBody]PayPayload payload)
        {
            using (DbConnection conn = DB.OpenConnection(Configuration))
            {
            //payload.vouchers
                //foreach ()
                return new PayPostResponse
                {
                    ackPocket = "confirmed",
                    state = "AlreadyPayed"
                };
            }
        }

    }
}