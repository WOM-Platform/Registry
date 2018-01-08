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
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/voucher")]
    public class VoucherController : Controller {

        protected readonly IConfiguration _configuration;
        protected readonly DatabaseManager _database;
        protected readonly CryptoProvider _crypto;
        protected readonly ILogger<VoucherController> _logger;

        public VoucherController(IConfiguration configuration, DatabaseManager database, CryptoProvider cryptoProvider, ILogger<VoucherController> logger) {
            this._configuration = configuration;
            this._database = database;
            this._crypto = cryptoProvider;
            this._logger = logger;
        }

        // GET api/voucher
        //percorso di prova
        [HttpGet]
        public Source Get() {
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

            return this._database.Connection.GetSourceById(1);
        }

        //POST api/voucher/create
        [HttpPost("create")]
        public CreateResponse Create([FromBody]CreatePayload payload) {
            this._logger.LogInformation(LoggingEvents.VoucherCreation, "Received create request from Source ID {0}, nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = this._database.Connection.GetSourceById(payload.SourceId);
            if(source == null) {
                this._logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                // TODO: correct error handling
                return null;
            }

            // Conversion from crypto-payload
            var payloadContent = this._crypto.DecryptPayload<CreatePayloadContent>(payload.Payload, KeyManager.LoadKeyFromString<Org.BouncyCastle.Crypto.AsymmetricKeyParameter>(source.Key));
            if(payload.SourceId != payloadContent.SourceId) {
                this._logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                // TODO
                return null;
            }
            // TODO: additional verification

            this._logger.LogInformation(LoggingEvents.VoucherCreation, "Processing voucher generation for source {0}, nonce {1}", payload.SourceId, payload.Nonce);

            var otc = this._database.Connection.CreateVoucherGeneration(payload.Nonce, payloadContent);

            this._logger.LogTrace(LoggingEvents.VoucherCreation, "Voucher generation instance created with OTC {0}", otc);

            // TODO: this must be encrypted
            return new CreateResponse {
                Id = payloadContent.Id,
                OtcGen = otc,
                Timestamp = DateTime.UtcNow
            };
        }

        // POST api/voucher/redeem
        [HttpPost("redeem/{otcPart}")]
        public RedeemResponse Redeem([FromRoute]string otcPart, [FromBody]RedeemPayload payload) {
            this._logger.LogInformation(LoggingEvents.VoucherRedemption, "Received redeem request with nonce {0}",
                payload.Nonce
            );

            return null;
        }

    }

}
