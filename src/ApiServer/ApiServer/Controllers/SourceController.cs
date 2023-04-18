using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Source;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/source")]
    [OperationsTags("Instrument")]
    [RequireHttpsInProd]
    public class SourceController : BaseRegistryController 
    {
        public SourceController(
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
        }

        [HttpPost]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> CreateSource(
            [FromBody] CreateSourceInput input
        ) {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            var keys = CryptoHelper.CreateKeyPair();
            var source = await SourceService.CreateNewSource(input.Name, input.Url, keys);

            Logger.LogInformation("Source {0} created with ID {1}", input.Name, source.Id);

            return Ok(source.ToOutput());
        }

        [HttpGet("{sourceId}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> GetSource(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            return Ok(source.ToOutput());
        }

        [HttpGet("generated/{sourceId}")]
        [Obsolete]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceGeneratedCountOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public Task<ActionResult> GetSourceGeneratedVouchersCountLegacy([FromRoute] ObjectId sourceId) {
            return GetSourceGeneratedVouchersCount(sourceId);
        }

        /// <summary>
        /// Provides a count of vouchers produced by a given source.
        /// Request must be authorized by a user who is an administrator of the source.
        /// </summary>
        [HttpGet("{sourceId}/generated")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceGeneratedCountOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetSourceGeneratedVouchersCount([FromRoute] ObjectId sourceId)
        {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            if(!User.GetUserId(out var loggedUserId) || !source.AdministratorUserIds.Contains(loggedUserId)) {
                return Forbid();
            }
            
            var result = await SourceService.GetGeneratedVouchersBySource(sourceId);
            return Ok(new SourceGeneratedCountOutput {
                Total = (int)result?.Total
            });
        }

        [HttpGet("{sourceId}/custom-generator")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> GetCustomGenerator(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            await VerifyUserIsAdminOfSource(source);

            if(source.CustomGenerator == null) {
                return NoContent();
            }

            return Ok(source.CustomGenerator.ToOutput(PicturesService.GetPictureOutput(source.CustomGenerator.LogoPath, source.CustomGenerator.LogoBlurHash)));
        }

        [HttpPut("{sourceId}/custom-generator")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> SetCustomGenerator(
            [FromRoute] ObjectId sourceId,
            [FromBody] SourceCustomGeneratorInput input
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            await VerifyUserIsAdminOfSource(source);

            source.CustomGenerator = new SourceCustomGenerator {
                Title = input.Title,
                LogoPath = source.CustomGenerator?.LogoPath,
                LogoBlurHash = source.CustomGenerator?.LogoBlurHash,
                EnableCustomGeneration = input.EnableCustomGeneration,
                Templates = (from t in input.Templates
                             select new SourceCustomGenerator.TemplateInfo {
                                 Name = t.Name,
                                 Description = t.Description,
                                 Guide = t.Guide,
                                 PresetWomCount = t.PresetWomCount,
                                 PresetWomAim = t.PresetWomAim,
                                 PresetWomLocation = t.PresetWomLocation.ToGeoJson(),
                             }).ToArray(),
            };
            await SourceService.ReplaceSource(source);

            return Ok(source.CustomGenerator.ToOutput(PicturesService.GetPictureOutput(source.CustomGenerator.LogoPath, source.CustomGenerator.LogoBlurHash)));
        }

        [HttpDelete("{sourceId}/custom-generator")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> DeleteCustomGenerator(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            await VerifyUserIsAdminOfSource(source);

            source.CustomGenerator = null;
            await SourceService.ReplaceSource(source);

            return NoContent();
        }

        [HttpPut("{sourceId}/custom-generator/logo")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> SetCustomGeneratorLogo(
            [FromRoute] ObjectId sourceId,
            [FromForm] [Required] IFormFile image
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            await VerifyUserIsAdminOfSource(source);

            if(source.CustomGenerator == null) {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Source has no custom generator");
            }

            // Safety checks on uploaded file
            if(image == null || image.Length == 0) {
                Logger.LogError("Image field null or empty");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image field null or empty");
            }
            if(image.Length > 4 * 1024 * 1024) {
                Logger.LogError("Image too large ({0} bytes)", image.Length);
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image too large");
            }

            try {
                var sourceUrl = source.Name.ToCleanUrl();

                // Process and upload image
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                (var picturePath, var pictureBlurHash) = await PicturesService.ProcessAndUploadPicture(stream, sourceUrl, PicturesService.PictureUsage.SourceLogo);

                source.CustomGenerator.LogoPath = picturePath;
                source.CustomGenerator.LogoBlurHash = pictureBlurHash;
                await SourceService.ReplaceSource(source);

                var picSourceLogo = PicturesService.GetPosCoverOutput(picturePath, pictureBlurHash);
                return Ok(picSourceLogo);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update source logo");
                throw;
            }
        }
    }    
}
