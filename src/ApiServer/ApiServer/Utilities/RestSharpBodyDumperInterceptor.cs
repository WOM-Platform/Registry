using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Interceptors;

namespace WomPlatform.Web.Api.Utilities {
    public class RestSharpBodyDumperInterceptor : Interceptor {
        private readonly ILogger _logger;

        public RestSharpBodyDumperInterceptor(ILogger logger) {
            _logger = logger;
        }

        public override async ValueTask BeforeHttpRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken) {
            var payload = await requestMessage.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Request {Method} {Url}: {Payload}", requestMessage.Method, requestMessage.RequestUri, payload);
        }

        public override ValueTask AfterRequest(RestResponse response, CancellationToken cancellationToken) {
            _logger.LogDebug("Response {StatusCode} {1}, {ContentLength} bytes of type {ContentType}: {Payload}",
                response.StatusCode, response.StatusDescription, response.ContentLength ?? 0, response.ContentType, response.Content);

            return ValueTask.CompletedTask;
        }
    }
}
