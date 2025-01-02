using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api {

    public class Program {

        public static void Main(string[] args) {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var host = WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configuration)
                .ConfigureLogging((context, logging) => {
                    logging.ClearProviders();
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            var confSentry = configuration.GetSection("Sentry");
            if(confSentry != null && Convert.ToBoolean(confSentry["Enabled"])) {
                host = host.UseSentry(sentry => {
                    sentry.Dsn = confSentry["Dns"]!;
                    sentry.Debug = true;
                    sentry.DiagnosticLevel = Sentry.SentryLevel.Warning;
                });
            }

            host = host.UseStartup<Startup>();

            return host.Build();
        }

    }

}
