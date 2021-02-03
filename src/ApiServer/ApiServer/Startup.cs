using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public const string ApiLoginPolicy = "APILoginPolicy";
        public const string UserLoginPolicy = "UserLoginPolicy";

        public const string ActiveMerchantClaimType = "ActiveMerchantClaim";

        public static string GetJwtIssuerName() => $"WOM Registry at {Environment.GetEnvironmentVariable("SELF_HOST")}";

        public void ConfigureServices(IServiceCollection services) {
            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.AddCors(options => {
                options.AddDefaultPolicy(builder => {
                    builder.WithOrigins(
                            "https://localhost",
                            "https://*.wom.social",
                            "https://wom.social"
                        )
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            services.AddRouting();

            services.AddApiVersioning(o => {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = false;
            });

            services.AddControllers()
                .AddMvcOptions(opts => {
                    opts.ModelBinderProviders.Insert(0, new ObjectIdModelBinderProvider());
                    opts.AllowEmptyInputInBodyModelBinding = true;
                    opts.InputFormatters.Add(new PermissiveInputFormatter());
                })
                .AddNewtonsoftJson(setup => {
                    var cs = Client.JsonSettings;
                    setup.SerializerSettings.ContractResolver = cs.ContractResolver;
                    setup.SerializerSettings.Culture = cs.Culture;
                    setup.SerializerSettings.DateFormatHandling = cs.DateFormatHandling;
                    setup.SerializerSettings.DateTimeZoneHandling = cs.DateTimeZoneHandling;
                    setup.SerializerSettings.DateParseHandling = cs.DateParseHandling;
                    setup.SerializerSettings.Formatting = cs.Formatting;
                    setup.SerializerSettings.NullValueHandling = cs.NullValueHandling;
                });

            services.AddDbContext<DataContext>(o => {
                var dbSection = Configuration.GetSection("Database");
                var host = dbSection["Host"];
                var port = Convert.ToInt32(dbSection["Port"]);
                var username = dbSection["Username"];
                var password = dbSection["Password"];
                var schema = dbSection["Schema"];
                var connectionString = string.Format(
                    "server={0};port={1};uid={2};pwd={3};database={4};Old Guids=false",
                    host, port, username, password, schema
                );

                o.UseMySQL(connectionString);
            });

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_USER_TOKEN_SECRET"))),
                    ValidateIssuer = true,
                    ValidIssuer = GetJwtIssuerName(),
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Add services to dependency registry
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<KeyManager>();
            services.AddTransient<CryptoProvider>();
            services.AddScoped<DatabaseOperator>(); // To be removed
            services.AddScoped<Operator>();
            services.AddSingleton<MongoDatabase>();
            services.AddMailComposer();
        }

        private readonly string[] SupportedCultures = new string[] {
            "en-US",
        };

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            MongoDatabase mongo,
            ILogger<Startup> logger
        ) {
            if (env.IsDevelopment()) {
                logger.LogInformation("Setup in development mode");

                app.UseDeveloperExceptionPage();

                // Refresh development setup
                var devSection = Configuration.GetSection("DevelopmentSetup");

                var devSourceSection = devSection.GetSection("Source");
                var devSourceId = devSourceSection["Id"];
                mongo.UpsertSourceSync(new DatabaseDocumentModels.Source {
                    Id = new MongoDB.Bson.ObjectId(devSourceId),
                    Name = "Development source",
                    PrivateKey = System.IO.File.ReadAllText(devSourceSection["KeyPathBase"] + ".pem"),
                    PublicKey = System.IO.File.ReadAllText(devSourceSection["KeyPathBase"] + ".pub")
                });
                logger.LogDebug("Configured development source #{0}", devSourceId);

                var devPosSection = devSection.GetSection("Pos");
                var devPosId = devPosSection["Id"];
                mongo.UpsertPosSync(new DatabaseDocumentModels.Pos {
                    Id = new MongoDB.Bson.ObjectId(devPosId),
                    Name = "Development POS",
                    PrivateKey = System.IO.File.ReadAllText(devPosSection["KeyPathBase"] + ".pem"),
                    PublicKey = System.IO.File.ReadAllText(devPosSection["KeyPathBase"] + ".pub")
                });
                logger.LogDebug("Configured development POS #{0}", devPosId);
            }

            // Fix incoming base path for hosting behind proxy
            string basePath = Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH");
            if(!string.IsNullOrWhiteSpace(basePath)) {
                logger.LogInformation("Configuring server to run under base path '{0}'", basePath);

                app.UsePathBase(new PathString(basePath));
                app.Use(async (context, next) => {
                    context.Request.PathBase = basePath;
                    await next.Invoke();
                });
            }

            // Enable forwarded headers within Docker local networks
            var forwardOptions = new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto
            };
            forwardOptions.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.20.0.1"), 2));
            app.UseForwardedHeaders(forwardOptions);

            app.UseStaticFiles();

            app.UseRequestLocalization(o => {
                o.AddSupportedCultures(SupportedCultures);
                o.AddSupportedUICultures(SupportedCultures);
                o.DefaultRequestCulture = new RequestCulture(SupportedCultures[0]);
            });

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }

    }

}
