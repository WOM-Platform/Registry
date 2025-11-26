using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Authentication {
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions> {
        private readonly ApiKeyService _apiKeyService;
        private readonly SourceService _sourceService;

        public ApiKeyAuthenticationHandler(
            ApiKeyService apiKeyService,
            SourceService sourceService,
            IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        ) : base(options, logger, encoder) {
            _apiKeyService = apiKeyService;
            _sourceService = sourceService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if(!Request.Headers.TryGetValue("X-WOM-ApiKey", out var apiKeyHeader)) {
                return AuthenticateResult.NoResult();
            }

            var apiKey = apiKeyHeader.ToString();
            if(string.IsNullOrWhiteSpace(apiKey)) {
                return AuthenticateResult.NoResult();
            }

            var entry = await _apiKeyService.RetrieveApiKey(apiKey);
            if(entry == null || entry.Expired) {
                return AuthenticateResult.Fail("API key not valid");
            }

            if(entry.Kind != DatabaseDocumentModels.ApiKey.KindOfKey.SourceAdministrator) {
                return AuthenticateResult.Fail("API key kind unsupported");
            }

            var source = await _sourceService.GetSourceById(entry.ControlledEntityId);
            if(source == null || source.IsDeleted) {
                return AuthenticateResult.Fail("API key not active");
            }

            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new SourceAdministratorIdentity(source)),
                BasicAuthenticationSchemeOptions.SchemeName
            ));
        }
    }
}
