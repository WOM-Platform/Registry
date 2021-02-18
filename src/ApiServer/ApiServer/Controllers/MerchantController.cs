using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
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

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/merchant")]
    [RequireHttps]
    [OperationsTags("Merchant")]
    public class MerchantController : BaseRegistryController {

        public MerchantController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<MerchantController> logger
        ) : base(configuration, crypto, keyManager, mongo, @operator, logger) {

        }

        /// <summary>
        /// User registration payload.
        /// </summary>
        public record MerchantRegisterInput(
            [Required]
            [MinLength(8)]
            string Name,
            [Required]
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
            string WebsiteUrl
        );

        /// <summary>
        /// Register a new merchant to the service.
        /// </summary>
        /// <param name="input">Merchant registration payload.</param>
        [HttpPost("register")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(MerchantRegisterInput input) {
            var existingMerchant = await Mongo.GetMerchantByFiscalCode(input.FiscalCode);
            if(existingMerchant != null) {
                return this.ProblemParameter("Supplied fiscal code is already registered");
            }

            var loggedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try {
                var merchant = new Merchant {
                    Name = input.Name,
                    FiscalCode = input.FiscalCode,
                    PrimaryActivityType = input.PrimaryActivity,
                    Address = input.Address,
                    ZipCode = input.ZipCode,
                    City = input.City,
                    Country = input.Country,
                    Description = input.Description,
                    WebsiteUrl = input.WebsiteUrl,
                    CreatedOn = DateTime.UtcNow,
                    AdministratorIds = new ObjectId[] {
                        new ObjectId(loggedUserId)
                    }
                };
                await Mongo.CreateMerchant(merchant);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = merchant.Id
                    },
                    MerchantToInfoOutput(merchant)
                );
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register new merchant with fiscal code {0}", input.FiscalCode);
                throw;
            }
        }

        public record MerchantInformationOutput(
            string Id,
            string Name,
            string FiscalCode,
            MerchantActivityType PrimaryActivity,
            string Address,
            string ZipCode,
            string City,
            string Country,
            string Description,
            string WebsiteUrl
        );

        /// <summary>
        /// Retrieves information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var existingMerchant = await Mongo.GetMerchantById(id);
            if(existingMerchant == null) {
                return NotFound();
            }

            return Ok(MerchantToInfoOutput(existingMerchant));
        }

        private static MerchantInformationOutput MerchantToInfoOutput(Merchant m) {
            return new MerchantInformationOutput(
                m.Id.ToString(),
                m.Name,
                m.FiscalCode,
                m.PrimaryActivityType,
                m.Address,
                m.ZipCode,
                m.City,
                m.Country,
                m.Description,
                m.WebsiteUrl
            );
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
            string WebsiteUrl
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        [HttpPatch("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateMerchant(
            [FromRoute] ObjectId id,
            MerchantUpdateInput input
        ) {
            var existingMerchant = await Mongo.GetMerchantById(id);
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
                if(input.WebsiteUrl != null) {
                    existingMerchant.WebsiteUrl = input.WebsiteUrl;
                }
                existingMerchant.LastUpdate = DateTime.UtcNow;

                await Mongo.ReplaceMerchant(existingMerchant);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update merchant {0}", id);
                throw;
            }

            return Ok();
        }

    }

}
