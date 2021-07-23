using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Conversion;

namespace WomPlatform.Web.Api {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private static string _selfDomain = null;
        public static string SelfDomain {
            get {
                if(_selfDomain == null) {
                    _selfDomain = Environment.GetEnvironmentVariable("SELF_HOST");
                }
                return _selfDomain;
            }
        }

        public static string GetJwtIssuerName() => $"WOM Registry at {SelfDomain}";

        public const string TokenSessionAuthPolicy = "AuthPolicyBearerOnly";
        public const string SimpleAuthPolicy = "AuthPolicyBasicAlso";

        public void ConfigureServices(IServiceCollection services) {
            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.AddCors(options => {
                options.AddDefaultPolicy(builder => {
                    builder
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(origin => {
                            var uri = new Uri(origin);
                            if(uri.Host == "localhost")
                                return true;
                            if(uri.Host == SelfDomain || uri.Host.EndsWith(SelfDomain))
                                return true;
                            return false;
                        })
                        .Build();
                });
            });

            services.AddRouting();

            services.AddControllers()
                .AddMvcOptions(opts => {
                    opts.ModelBinderProviders.Insert(0, new ObjectIdModelBinderProvider());
                    opts.InputFormatters.Add(new PermissiveInputFormatter());
                })
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.AllowTrailingCommas = true;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonWomIdentifierConverter());
                });

            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                    Title = "WOM Registry API",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact {
                        Email = "info@wom.social",
                        Name = "The WOM Platform",
                        Url = new Uri("https://wom.social")
                    },
                    Version = "v1"
                });

                options.OperationFilter<ObjectIdOperationFilter>();

                options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer {
                    Url = $"https://{Environment.GetEnvironmentVariable("SELF_HOST")}{Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH")}",
                    Description = "WOM development server (HTTPS)"
                });
                options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer {
                    Url = $"http://{Environment.GetEnvironmentVariable("SELF_HOST")}{Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH")}",
                    Description = "WOM development server (HTTP)"
                });

                options.IncludeXmlComments(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "ApiServer.xml"));

                options.TagActionsBy(api => {
                    var controller = api.ActionDescriptor as ControllerActionDescriptor;
                    if(controller == null) {
                        return new[] { string.Empty };
                    }

                    // Take tag value from OperationsTagsAttribute, if set
                    var attributes = controller.MethodInfo.GetCustomAttributesData().Concat(controller.ControllerTypeInfo.GetCustomAttributesData());
                    var attr = controller.ControllerTypeInfo.CustomAttributes.Where(a => a.AttributeType == typeof(OperationsTagsAttribute)).FirstOrDefault();
                    if(attr != null) {
                        return ((ReadOnlyCollection<CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value).Select(a => (string)a.Value).ToArray();
                    }
                    else {
                        return new[] { controller.ControllerName };
                    }
                });
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
            })
            .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationSchemeOptions.SchemeName, null);

            services.AddAuthorization(options => {
                options.AddPolicy(TokenSessionAuthPolicy,
                    new AuthorizationPolicyBuilder(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.NameIdentifier)
                    .Build()
                );
                options.AddPolicy(SimpleAuthPolicy,
                    new AuthorizationPolicyBuilder(
                        BasicAuthenticationSchemeOptions.SchemeName,
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireAuthenticatedUser()
                    .Build()
                );
                options.DefaultPolicy = options.GetPolicy(SimpleAuthPolicy);
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
                    PrivateKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pem"),
                    PublicKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pub")
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

            // Use Swagger for documentation
            if(env.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI(conf => {
                    conf.SwaggerEndpoint("v1/swagger.json", "WOM Registry API");
                });
            }

            app.UseStaticFiles();

            app.UseCors();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }

    }

}
