using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace GuidEncodingConversion {
    public class ExportService<T> : IHostedService {

        private readonly IConfiguration _configuration;

        public ExportService(
            IConfiguration configuration
        ) {
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            var mongoConnectionString = _configuration["mongoConnectionString"];
            Console.WriteLine("Mongo connection: {0}", mongoConnectionString);

            var mongo = new MongoClient(mongoConnectionString);
            var database = mongo.GetDatabase("Wom");
            var collection = database.GetCollection<T>(MongoDatabase.GetCollectionName<T>());

            var paymentCursor = await collection.FindAsync(Builders<T>.Filter.Empty);
            var sourcePayments = await paymentCursor.ToListAsync();

            Console.WriteLine("Read {0} elements", sourcePayments.Count);

            using(var fs = new FileStream("dump.json", FileMode.Create)) {
                await JsonSerializer.SerializeAsync(fs, sourcePayments, cancellationToken: cancellationToken);
                await fs.FlushAsync();
            }

            Console.WriteLine("Export completed");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
