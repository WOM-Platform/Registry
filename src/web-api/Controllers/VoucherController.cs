using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers
{
    [Route("api/[controller]")]
    public class VoucherController : Controller
    {
        // GET api/voucher
        //percorso di prova
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "prova1", "value2" };
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
                Timestamp = "XX:XX:XXXX"
                //voucher?!?
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
