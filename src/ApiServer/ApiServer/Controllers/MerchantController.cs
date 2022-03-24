using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/merchant")]
    [RequireHttps]
    [OperationsTags("Merchant")]
    public class MerchantController : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public MerchantController(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<MerchantController> logger
        ) : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
        }

        /// <summary>
        /// Merchant registration payload.
        /// </summary>
        public record MerchantRegisterInput(
            [Required]
            [MinLength(8)]
            string Name,
            [Required]
            [MinLength(11)] // Length of Partita IVA
            [MaxLength(16)] // Length of Codice Fiscale
            string FiscalCode,
            [Required]
            MerchantActivityType PrimaryActivity,
            [Required]
            string Address,
            [Required]
            string ZipCode,
            [Required]
            string City,
            [Required]
            string Country,
            string Description,
            [Url]
            string Url
        );

        /// <summary>
        /// Registers a new merchant to the service.
        /// </summary>
        /// <param name="input">Merchant registration payload.</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(MerchantRegisterInput input) {
            var existingMerchant = await _mongo.GetMerchantByFiscalCode(input.FiscalCode);
            if(existingMerchant != null) {
                return this.ProblemParameter("Supplied fiscal code is already registered");
            }

            if(!User.GetUserId(out var loggedUserId)) {
                return Forbid();
            }

            try {
                var merchant = new Merchant {
                    Name = input.Name,
                    FiscalCode = input.FiscalCode.ToUpperInvariant(),
                    PrimaryActivityType = input.PrimaryActivity,
                    Address = input.Address,
                    ZipCode = input.ZipCode,
                    City = input.City,
                    Country = input.Country,
                    Description = input.Description,
                    WebsiteUrl = input.Url,
                    CreatedOn = DateTime.UtcNow,
                    AdministratorIds = new ObjectId[] {
                        loggedUserId
                    }
                };
                await _mongo.CreateMerchant(merchant);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = merchant.Id
                    },
                    merchant.ToOutput()
                );
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register new merchant with fiscal code {0}", input.FiscalCode);
                throw;
            }
        }

        /// <summary>
        /// Retrieves information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator or POS user.
        /// </remarks>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var existingMerchant = await _mongo.GetMerchantById(id);
            if(existingMerchant == null) {
                return NotFound();
            }

            // Forbid if logged user is not in admin list OR POS user list
            if(!User.GetUserId(out var loggedUserId) ||
               !(
                   existingMerchant.AdministratorIds.Contains(loggedUserId) || existingMerchant.PosUserIds.Contains(loggedUserId)
               )) {
                return Forbid();
            }

            return Ok(existingMerchant.ToOutput());
        }

        /// <summary>
        /// Merchant update payload.
        /// </summary>
        public record MerchantUpdateInput(
            [MinLength(8)]
            string Name,
            MerchantActivityType? PrimaryActivity,
            string Address,
            string ZipCode,
            string City,
            string Country,
            string Description,
            [Url]
            string Url
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator.
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateMerchant(
            [FromRoute] ObjectId id,
            MerchantUpdateInput input
        ) {
            var existingMerchant = await _mongo.GetMerchantById(id);
            if(existingMerchant == null) {
                return NotFound();
            }

            if(!User.GetUserId(out var loggedUserId) || !existingMerchant.AdministratorIds.Contains(loggedUserId)) {
                return Forbid();
            }

            try {
                if(input.Name != null) {
                    existingMerchant.Name = input.Name;
                }
                if(input.PrimaryActivity.HasValue) {
                    existingMerchant.PrimaryActivityType = input.PrimaryActivity.Value;
                }
                if(input.Address != null) {
                    existingMerchant.Address = input.Address;
                }
                if(input.ZipCode != null) {
                    existingMerchant.ZipCode = input.ZipCode;
                }
                if(input.City != null) {
                    existingMerchant.City = input.City;
                }
                if(input.Country != null) {
                    existingMerchant.Country = input.Country;
                }
                if(input.Description != null) {
                    existingMerchant.Description = input.Description;
                }
                if(input.Url != null) {
                    existingMerchant.WebsiteUrl = input.Url;
                }
                existingMerchant.LastUpdate = DateTime.UtcNow;

                await _mongo.ReplaceMerchant(existingMerchant);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update merchant {0}", id);
                throw;
            }

            return Ok(existingMerchant.ToOutput());
        }

    }

}
