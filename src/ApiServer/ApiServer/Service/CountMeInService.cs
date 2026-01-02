using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RestSharp;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.Utilities;
using static WomPlatform.Web.Api.Controllers.SourceCheckInController;

namespace WomPlatform.Web.Api.Service {
    public class CountMeInService : BaseService {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CountMeInService> _logger;

        private readonly bool _cmiEnabled = false;
        private readonly RestClient _cmiClient;

        public CountMeInService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<CountMeInService> logger
        ) : base(client, logger) {
            _configuration = configuration;
            _logger = logger;

            var section = _configuration.GetSection("CountMeIn");
            var cmiApiKey = section.GetValue<string>("ApiKey");
            var cmiBaseUrl = section.GetValue<string>("BaseUrl");

            if(string.IsNullOrEmpty(cmiApiKey) || string.IsNullOrEmpty(cmiBaseUrl)) {
                _logger.LogInformation("Count Me In is not enabled");
            }
            else {
                _logger.LogInformation("Count Me In enabled, connected to {BaseUrl}", cmiBaseUrl);

                _cmiClient = new RestClient(new RestClientOptions {
                    BaseUrl = new Uri(cmiBaseUrl),
                    Interceptors = [new RestSharpBodyDumperInterceptor(_logger)]
                }, configureDefaultHeaders: (headers) => {
                    headers.Add("X-WOM-auth-key", cmiApiKey);
                });

                _cmiEnabled = true;
            }
        }

        /// <summary>
        /// Perform preliminary checks on Source and system, to confirm that Count Me In can be used.
        /// </summary>
        private void CheckSource(Source source) {
            if(!_cmiEnabled) {
                throw ServiceProblemException.CmiNotEnabled;
            }

            if(source.CountMeInProvider == null) {
                Logger.LogError("Cannot manage check-in totems for source {SourceId} because it is not linked to any Count Me In provider", source.Id);

                throw ServiceProblemException.SourceHasNoCmiProvider;
            }
        }

        private record CreateEventRequest(
            string Title,
            string ProviderId,
            DateTime Start,
            DateTime End,
            CreateEventLocationRequest? Location,
            string? TotemId,
            CreateEventWomRequest WomGeneration
        );

        private record CreateEventLocationRequest(
            string Name,
            GeoCoords Coords,
            int Radius
        );

        private record CreateEventWomRequest(
            string Aim,
            int Count
        );

        private record GeoCoords(double Lat, double Lng);

        public record CreateEventResponse(
            string ProviderId,
            string EventId,
            string TotemId
        );

        /// <summary>
        /// Creates a new check-in totem for a given provider.
        /// </summary>
        public async Task<CheckInTotem> CreateEvent(Source source, CreateCheckinInput input) {
            CheckSource(source);

            _logger.LogInformation("Creating CountMeIn event for source {SourceId} and {ProviderId}", source.Id, source.CountMeInProvider);

            var payload = new CreateEventRequest(
                input.EventTitle,
                source.CountMeInProvider,
                input.EventStart,
                input.EventEnd,
                input.Location == null ? new CreateEventLocationRequest(
                    "Fixed totem",
                    new GeoCoords(0, 0),
                    1000
                ) : new CreateEventLocationRequest(
                    input.Location.Name,
                    new GeoCoords(input.Location.Coords.Latitude, input.Location.Coords.Longitude),
                    1000
                ),
                input.TotemId,
                new CreateEventWomRequest(
                    input.WomGeneration.Aim,
                    input.WomGeneration.Count
                )
            );

            var request = new RestRequest("womRegistry-createEvent", Method.Post);
            request.AddJsonBody(payload);

            var response = await _cmiClient.ExecutePostAsync<CreateEventResponse>(request);
            if(!response.IsSuccessStatusCode || response.Data == null) {
                _logger.LogError("Event creation request failed with status {StatusCode}. Response: {Payload}", response.StatusCode, response.Content);

                throw new Exception("CountMeIn event creation failed");
            }

            _logger.LogDebug("Event creation succeeded with ID {EventId} and totem {TotemId}", response.Data.EventId, response.Data.TotemId);

            var totem = new CheckInTotem {
                EventId = response.Data.EventId,
                ProviderId = response.Data.ProviderId,
                TotemId = response.Data.TotemId,
                SourceId = source.Id,
                CreatedOn = DateTime.UtcNow,
            };
            await CheckInTotemCollection.InsertOneAsync(totem, new InsertOneOptions { });

            return totem;
        }

        public Task<CheckInTotem> GetEventById(Source source, string eventId) {
            CheckSource(source);

            var filter = Builders<CheckInTotem>.Filter.And(
                Builders<CheckInTotem>.Filter.Eq(totem => totem.EventId, eventId),
                Builders<CheckInTotem>.Filter.Eq(totem => totem.SourceId, source.Id),
                Builders<CheckInTotem>.Filter.Ne(totem => totem.Deleted, true)
            );
            return CheckInTotemCollection.Find(filter).SingleOrDefaultAsync();
        }

        private record DeleteEventRequest(
            string EventId,
            string ProviderId
        );

        public async Task<bool> DeleteEvent(Source source, string eventId) {
            CheckSource(source);

            _logger.LogInformation("Deleting CountMeIn event {EventId} for source {SourceId} and {ProviderId}", eventId, source.Id, source.CountMeInProvider);

            // Try to update existing document, if any
            var filter = Builders<CheckInTotem>.Filter.And(
                Builders<CheckInTotem>.Filter.Eq(totem => totem.EventId, eventId),
                Builders<CheckInTotem>.Filter.Eq(totem => totem.SourceId, source.Id),
                Builders<CheckInTotem>.Filter.Ne(totem => totem.Deleted, true)
            );
            var updateResults = await CheckInTotemCollection.UpdateManyAsync(filter, Builders<CheckInTotem>.Update.Combine(
                Builders<CheckInTotem>.Update.Set(totem => totem.Deleted, true),
                Builders<CheckInTotem>.Update.Set(totem => totem.LastUpdate, DateTime.UtcNow)
            ), new UpdateOptions { IsUpsert = false });

            if(!updateResults.IsModifiedCountAvailable || updateResults.ModifiedCount < 1) {
                _logger.LogDebug("No event {EventId} for source {SourceId} found to be deleted", eventId, source.Id);

                return false;
            }

            var request = new RestRequest("womRegistry-deleteEvent", Method.Delete);
            request.AddJsonBody(new DeleteEventRequest(eventId, source.CountMeInProvider));

            var response = await _cmiClient.ExecuteAsync(request);
            if(!response.IsSuccessStatusCode) {
                _logger.LogError("Event deletion request failed with status {StatusCode}. Response: {Payload}", response.StatusCode, response.Content);

                // We ignore the failure and proceed
                return true;
            }

            _logger.LogDebug("Event {EventId} deleted successfully", eventId);

            return true;
        }
    }
}
