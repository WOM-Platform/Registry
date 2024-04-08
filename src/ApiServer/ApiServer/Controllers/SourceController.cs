using System;
using System.ComponentModel;
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
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.OutputModels.User;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/source")]
    [OperationsTags("Instrument")]
    [RequireHttpsInProd]
    public class SourceController : BaseRegistryController {
        public SourceController(
            IServiceProvider serviceProvider,
            ILogger<SourceController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Creates a new source.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceOutput), StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateSource(
            [FromBody] CreateSourceInput input
        ) {
            await VerifyUserIsAdmin();

            var keys = CryptoHelper.CreateKeyPair();
            var source = await SourceService.CreateNewSource(input.Name, input.Url, keys);

            Logger.LogInformation("Source {0} created with ID {1}", input.Name, source.Id);

            return Ok(new SourceOutput(source));
        }

        /// <summary>
        /// Retrieves a list of sources.
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Paged<SourceOutput>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListSources(
            [FromQuery] string search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(SourceService.SourceListOrder.Name)] SourceService.SourceListOrder orderBy = SourceService.SourceListOrder.Name
        ) {
            await VerifyUserIsAdmin();

            (var results, var count) = await SourceService.ListSources(search, page, pageSize, orderBy);

            return Ok(Paged<SourceOutput>.FromPage(
                (from s in results select new SourceOutput(s)).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Retrieves information about a given source.
        /// </summary>
        [HttpGet("{sourceId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceOutput), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSource(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            return Ok(new SourceOutput(source));
        }

        /// <summary>
        /// Deletes a source.
        /// </summary>
        [HttpDelete("{sourceId}")]
        [Authorize]
        [ProducesResponseType(typeof(SourceOutput), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteSource(
            [FromRoute] ObjectId sourceId
        ) {
            await VerifyUserIsAdminOfSource(sourceId);

            if(!await SourceService.DeleteSource(sourceId)) {
                return this.DeleteFailed();
            }

            return this.NoContent();
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
        public async Task<ActionResult> GetSourceGeneratedVouchersCount([FromRoute] ObjectId sourceId) {
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
            var source = await VerifyUserIsAdminOfSource(sourceId);

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
            var source = await VerifyUserIsAdminOfSource(sourceId);

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
                                 BatchGeneration = t.BatchGeneration,
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
            var source = await VerifyUserIsAdminOfSource(sourceId);

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
            var source = await VerifyUserIsAdminOfSource(sourceId);

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
            catch(Exception) {
                Logger.LogError("Failed to update source logo");
                throw;
            }
        }

        private async Task<SourceAccessOutput.UserAccessInformation> GetInfo(ObjectId id) {
            var user = await UserService.GetUserById(id);
            return new SourceAccessOutput.UserAccessInformation {
                UserId = user.Id,
                Email = user.Email.ConcealEmail(),
                Name = user.Name.Conceal(),
                Surname = user.Surname.Conceal(),
                Role = SourceRole.Admin,
            };
        }

        [HttpGet("{sourceId}/access")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetSourceAccess(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            var userTasks = source.AdministratorUserIds.Select(async (ObjectId id) => {
                var user = await UserService.GetUserById(id);
                return new SourceAccessOutput.UserAccessInformation {
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Surname = user.Surname,
                    Role = SourceRole.Admin,
                };
            });
            var users = await Task.WhenAll(userTasks);

            return Ok(new SourceAccessOutput {
                SourceId = source.Id,
                Users = users,
            });
        }

        [HttpPost("{sourceId}/access")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GrantSourceAccess(
            [FromRoute] ObjectId sourceId,
            [FromQuery] ObjectId userId,
            [FromQuery] SourceRole role = SourceRole.Admin
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            var user = await UserService.GetUserById(userId);
            if(user == null) {
                return this.UserNotFound();
            }

            source.AdministratorUserIds = (from accessUserId in source.AdministratorUserIds.ToSafeList()
                                           where accessUserId != userId
                                           select accessUserId)
                                           .Concat([userId])
                                           .ToArray();

            if(!await SourceService.ReplaceSource(source)) {
                return this.WriteFailed("Failed to update source");
            }

            return this.NoContent();
        }

        [HttpDelete("{sourceId}/access/{userId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RevokeSourceAccess(
            [FromRoute] ObjectId sourceId,
            [FromRoute] ObjectId userId
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            source.AdministratorUserIds = (from accessUserId in source.AdministratorUserIds
                                           where accessUserId != userId
                                           select accessUserId)
                                           .ToArray();

            if(!await SourceService.ReplaceSource(source)) {
                return this.WriteFailed("Failed to update source");
            }

            return this.NoContent();
        }
    }
}
