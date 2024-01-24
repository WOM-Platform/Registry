using System;
using System.IO;
using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api {
    public static class KnownEntitiesManagementExtensions {

        public static IApplicationBuilder SetupDevelopmentEntities(this IApplicationBuilder app) {
            using var scope = app.ApplicationServices.CreateScope();

            var conf = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var srv = scope.ServiceProvider.GetRequiredService<SetupService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();

            if(env.IsDevelopment()) {
                logger.LogInformation("Setup known entities in development mode");

                var devSection = conf.GetSection("DevelopmentSetup");

                var devUserSection = devSection.GetSection("AdminUser");
                var devUserEntity = new DatabaseDocumentModels.User {
                    Id = new ObjectId(devUserSection["Id"]),
                    Email = devUserSection["Email"],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(devUserSection["Password"]),
                    Name = devUserSection["Name"],
                    Surname = devUserSection["Surname"],
                    Role = PlatformRole.User,
                    RegisteredOn = DateTime.UtcNow
                };
                srv.UpsertUserSync(devUserEntity);

                var devSourceSection = devSection.GetSection("Source");
                var devSourceId = devSourceSection["Id"];
                srv.UpsertSourceSync(new DatabaseDocumentModels.Source {
                    Id = new ObjectId(devSourceId),
                    Name = "Development source",
                    PrivateKey = File.ReadAllText(devSourceSection["KeyPathBase"] + ".pem"),
                    PublicKey = File.ReadAllText(devSourceSection["KeyPathBase"] + ".pub"),
                    AdministratorUserIds = new ObjectId[] { devUserEntity.Id },
                });
                logger.LogDebug("Configured development source #{0}", devSourceId);

                var devPosSection = devSection.GetSection("Pos");
                var devPosId = devPosSection["Id"];
                srv.UpsertPosSync(new DatabaseDocumentModels.Pos {
                    Id = new ObjectId(devPosId),
                    Name = "Development POS",
                    Description = "This is a dummy virtual POS which is available only on a development setup.",
                    PrivateKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pem"),
                    PublicKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pub"),
                    Url = "https://example.org/store"
                });
                logger.LogDebug("Configured development POS #{0}", devPosId);
            }

            return app;
        }

        public static IApplicationBuilder SetupKnownEntities(this IApplicationBuilder app) {
            using var scope = app.ApplicationServices.CreateScope();

            var conf = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var srv = scope.ServiceProvider.GetRequiredService<SetupService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();

            var devSection = conf.GetSection("KnownEntities");

            var anonymousPosId = devSection["AnonymousPosId"];
            var anonymousKeyPair = CryptoHelper.CreateKeyPair();
            srv.UpsertPosSync(new DatabaseDocumentModels.Pos {
                Id = new ObjectId(anonymousPosId),
                Name = "Anonymous POS",
                Description = "Anonymous POS that can be used by any guest WOM user without authentication.",
                PublicKey = anonymousKeyPair.Public.ToPemString(),
                PrivateKey = anonymousKeyPair.Private.ToPemString(),
                IsDummy = true,
            });
            logger.LogDebug("Configured anonymous POS #{0}", anonymousPosId);

            var exchangeSourceId = devSection["ExchangeSourceId"];
            var exchangeKeyPair = CryptoHelper.CreateKeyPair();
            srv.UpsertSourceSync(new DatabaseDocumentModels.Source {
                Id = new ObjectId(exchangeSourceId),
                Name = "Exchange source",
                PublicKey = exchangeKeyPair.Public.ToPemString(),
                PrivateKey = exchangeKeyPair.Private.ToPemString(),
            });
            logger.LogDebug("Configured exchange source #{0}", exchangeSourceId);

            return app;
        }

    }
}
