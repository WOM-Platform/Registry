// See https://aka.ms/new-console-template for more information
using GuidEncodingConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WomPlatform.Web.Api.DatabaseDocumentModels;

Console.WriteLine("Hello, World!");

Console.WriteLine("Running host...");

IHost host = Host.CreateDefaultBuilder(args)
#if DEBUG
    .UseEnvironment("Development")
#endif
    .ConfigureServices(services => {
        services.AddHostedService<ImportService<PaymentRequest>>();
    })
    .Build();
host.Run();

Console.WriteLine("Host stopped.");
