using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers
{
    [Route("voucher")]
    public class VoucherController : Controller
    {

        [HttpPost("create")]
        public CreateResponse Create([FromBody]CreatePayload payload)
        {
            //return string.Format("ID {0}", payload.SourceId);

            return new CreateResponse
            {
                Id = Guid.NewGuid(),
                OtcGen = "Prova"
            };
        }

        [HttpPost("redeem")]
        public void Redeem()
        {

        }

    }
}
