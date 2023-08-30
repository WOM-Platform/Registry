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
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Conversion;
using WomPlatform.Web.Api.Mail;
using WomPlatform.Web.Api.Service;
using static System.Net.Mime.MediaTypeNames;

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
            services.AddCors(options => {
                options.AddDefaultPolicy(builder => {
                    builder
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(origin => true)
                        .Build();
                });
            });

            services.AddRouting();

            services.AddControllers()
                .AddMvcOptions(options => {
                    options.ModelBinderProviders.Insert(0, new ObjectIdModelBinderProvider());
                    options.InputFormatters.Add(new PermissiveInputFormatter());
                })
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.AllowTrailingCommas = true;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonWomIdentifierConverter());
                })
                .ConfigureApiBehaviorOptions(options => {
                    var builtInFactory = options.InvalidModelStateResponseFactory;

                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Model binding error: {0}",
                            string.Join(", ", from modelState in context.ModelState
                                              select string.Format("{0} ({1})", modelState.Key, string.Join(", ", from error in modelState.Value.Errors
                                                                                                                  select error.ErrorMessage)))
                        );

                        // Proceed with default built-in response factory
                        return builtInFactory(context);
                    };
                })
            ;
            services.AddProblemDetails();

            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                    Title = "WOM Registry API",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact {
                        Email = "info@wom.social",
                        Name = "The WOM Platform",
                        Url = new Uri("https://wom.social")
                    }
                });

                options.OperationFilter<ObjectIdOperationFilter>();

                options.MapType<TimeSpan>(() => new OpenApiSchema {
                    Type = "string",
                    Example = new OpenApiString("12:34:56"),
                });

                options.CustomSchemaIds(type => type.ToString().Replace('+', '_'));

                options.AddServer(new OpenApiServer {
                    Url = $"https://{Environment.GetEnvironmentVariable("SELF_HOST")}{Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH")}",
                    Description = "WOM development server (HTTPS)"
                });
                options.AddServer(new OpenApiServer {
                    Url = $"http://{Environment.GetEnvironmentVariable("SELF_HOST")}{Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH")}",
                    Description = "WOM development server (HTTP)"
                });

                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "ApiServer.xml"));

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
            services.AddSingleton(provider => {
                BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
                BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

                var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_URI");
                if(connectionString == null) {
                    var username = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_USERNAME");
                    var password = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD");
                    var host = Environment.GetEnvironmentVariable("MONGO_CONNECTION_HOST");
                    var port = Environment.GetEnvironmentVariable("MONGO_CONNECTION_PORT");

                    connectionString = string.Format("mongodb://{0}:{1}@{2}:{3}", username, password, host, port);
                }

                return new MongoClient(connectionString);
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<KeyManager>();
            services.AddTransient<CryptoProvider>();

            services.AddScoped<MongoDatabase>();

            services.AddScoped<AimService>();
            services.AddScoped<ApiKeyService>();
            services.AddScoped<BackupService>();
            services.AddScoped<GenerationService>();
            services.AddScoped<MapService>();
            services.AddScoped<MerchantService>();
            services.AddScoped<OfferService>();
            services.AddScoped<PaymentService>();
            services.AddScoped<PicturesService>();
            services.AddScoped<PosService>();
            services.AddScoped<SetupService>();
            services.AddScoped<SourceService>();
            services.AddScoped<StatsService>();
            services.AddScoped<UserService>();

            services.AddMailComposer();
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            SetupService setupService,
            ILogger<Startup> logger
        ) {
            if (env.IsDevelopment()) {
                logger.LogInformation("Setup in development mode");

                // Refresh development setup
                var devSection = Configuration.GetSection("DevelopmentSetup");

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
                setupService.UpsertUserSync(devUserEntity);
                logger.LogDebug("Admin user #{0} created for development/testing purposes", devUserEntity.Id);

                var devSourceSection = devSection.GetSection("Source");
                var devSourceId = devSourceSection["Id"];
                setupService.UpsertSourceSync(new DatabaseDocumentModels.Source {
                    Id = new ObjectId(devSourceId),
                    Name = "Development source",
                    PrivateKey = File.ReadAllText(devSourceSection["KeyPathBase"] + ".pem"),
                    PublicKey = File.ReadAllText(devSourceSection["KeyPathBase"] + ".pub"),
                    AdministratorUserIds = new ObjectId[] { devUserEntity.Id },
                });
                logger.LogDebug("Development source #{0} configured", devSourceId);

                var devPosSection = devSection.GetSection("Pos");
                var devPosId = devPosSection["Id"];
                setupService.UpsertPosSync(new DatabaseDocumentModels.Pos {
                    Id = new ObjectId(devPosId),
                    Name = "Development POS",
                    Description = "This is a dummy virtual POS which is available only on a development setup.",
                    PrivateKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pem"),
                    PublicKey = File.ReadAllText(devPosSection["KeyPathBase"] + ".pub"),
                });
                logger.LogDebug("Development POS #{0} configured", devPosId);
            }

            app.UseStatusCodePages();
            app.UseExceptionHandler(new ExceptionHandlerOptions {
                AllowStatusCode404Response = true,
                ExceptionHandler = async httpContext => {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
                    if(exceptionHandlerPathFeature?.Error is ServiceProblemException) {
                        var serviceException = (ServiceProblemException)exceptionHandlerPathFeature.Error;

                        logger.LogError("Service problem “{0}” with status code {1} (code {2})", serviceException.Title, serviceException.HttpStatus, serviceException.Type);

                        httpContext.Response.StatusCode = serviceException.HttpStatus;

                        var problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();
                        await problemDetailsService.WriteAsync(new ProblemDetailsContext {
                            HttpContext = httpContext,
                            ProblemDetails = serviceException.ToProblemDetails(),
                        });
                    }
                    else {
                        httpContext.Response.ContentType = Text.Plain;
                        await httpContext.Response.WriteAsync(exceptionHandlerPathFeature?.Error?.ToString());
                    }
                },
            });

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
