using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace GuidEncodingConversion {
    public class ImportService<T> : IHostedService {

        private readonly IConfiguration _configuration;

        public ImportService(
            IConfiguration configuration
        ) {
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            List<T> data = new();
            using(var fs = new FileStream("dump.json", FileMode.Open)) {
                data = await JsonSerializer.DeserializeAsync<List<T>>(fs, cancellationToken: cancellationToken);
            }

            Console.WriteLine("Read {0} payment requests from file", data.Count);

            var mongoConnectionString = _configuration["mongoConnectionString"];
            Console.WriteLine("Mongo connection: {0}", mongoConnectionString);

            var mongo = new MongoClient(mongoConnectionString);
            var database = mongo.GetDatabase("Wom");
            var collection = database.GetCollection<T>(MongoDatabase.GetCollectionName<T>());

            await collection.DeleteManyAsync(Builders<T>.Filter.Empty);

            await collection.InsertManyAsync(data);

            Console.WriteLine("Import completed");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
