using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers 
{
    // api/payment
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        //POST  api/payment/register
        [HttpPost("register")]
        public RegisterResponse Register([FromBody]RegisterPayload payload)
        {

            System.Console.Write("posID :");
            System.Console.WriteLine(payload.PosId);

            //

            return new RegisterResponse
            {
                PosId = 332,
                nonceId = Guid.NewGuid(),
                nonceTs = "XX:XX:XXXX",
                OTCpay = "wwww.ddadsad68454sad.com"
            };
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