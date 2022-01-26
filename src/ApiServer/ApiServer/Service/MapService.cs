using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class MapService {

        private readonly MongoClient _client;
        private readonly ILogger<StatsService> _logger;

        public MapService(
            MongoClient client,
            ILogger<StatsService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Pos> PosCollection {
            get {
                return MainDatabase.GetCollection<Pos>("Pos");
            }
        }

        public Task<List<Pos>> FetchPosWithin(double lowerLeftLong, double lowerLeftLat, double upperRightLong, double upperRightLat) {
            var geoFilter = Builders<Pos>.Filter.GeoWithinBox(p => p.Position,
                lowerLeftLong, lowerLeftLat,
                upperRightLong, upperRightLat
            );

            return PosCollection.Find(geoFilter).ToListAsync();
        }

    }

}
