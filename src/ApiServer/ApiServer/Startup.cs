using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
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
using WomPlatform.Web.Api.Authentication;
using WomPlatform.Web.Api.Conversion;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api {

    public class Startup {

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment env

        ) {
            _configuration = configuration;
            _env = env;
        }

        private static string _selfDomain = null;

        public static string SelfDomain {
            get {
                _selfDomain ??= Environment.GetEnvironmentVariable("SELF_HOST");
                return _selfDomain;
            }
        }

        public static string GetJwtIssuerName() => $"WOM Registry at {SelfDomain}";

        public const string TokenSessionAuthPolicy = "AuthPolicyBearerOnly";
        public const string SimpleAuthPolicy = "AuthPolicyBasicAlso";

        public void ConfigureServices(IServiceCollection services) {
            services.AddGoogleDiagnosticsForAspNetCore(
                projectId: Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID"),
                serviceName: _env.IsDevelopment() ? "Registry-Dev" : "Registry-Prod",
                loggingOptions: LoggingOptions.Create(logLevel: LogLevel.Information)
            );

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
                    options.InputFormatters.Add(new RawStreamInputFormatter());
                })
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.AllowTrailingCommas = true;
                    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonPermissiveNumericConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonWomIdentifierConverter());
                })
                .ConfigureApiBehaviorOptions(options => {
                    var builtInFactory = options.InvalidModelStateResponseFactory;

                    options.InvalidModelStateResponseFactory = context => {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogWarning("Model binding error on action {Action}: {1}",
                            context.ActionDescriptor.DisplayName,
                            string.Join(", ", from modelState in context.ModelState
                                              where modelState.Value.Errors.Any()
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
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "WOM Registry API",
                    Contact = new OpenApiContact {
                        Email = "info@wom.social",
                        Name = "The WOM Platform",
                        Url = new Uri("https://wom.social")
                    }
                });

                options.OperationFilter<ObjectIdOperationFilter>();
                options.SchemaFilter<ObjectIdSchemaFilter>();

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
                        return [controller.ControllerName];
                    }
                });
            });

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                var securitySection = _configuration.GetRequiredSection("Security");

                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securitySection["JwtTokenSigningKey"])),
                    ValidateIssuer = true,
                    ValidIssuer = GetJwtIssuerName(),
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationSchemeOptions.SchemeName, null)
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationSchemeOptions.SchemeName, null);

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
                var configuration = provider.GetRequiredService<IConfiguration>();

                BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;
                BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

                var confMongo = configuration.GetRequiredSection("Database").GetRequiredSection("Mongo");

                var connectionString = confMongo["ConnectionUri"];
                if(connectionString == null) {
                    var username = confMongo["RootUsername"];
                    var password = confMongo["RootPassword"];
                    var host = confMongo["ConnectionHost"];
                    var port = confMongo["ConnectionPort"];

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
            services.AddScoped<BadgeService>();
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
            services.AddScoped<CountMeInService>();

            services.AddMailComposer();
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILogger<Startup> logger
        ) {
            // Registry setup
            app.SetupDevelopmentEntities();
            app.SetupKnownEntities();

            // Pipeline setup
            app.UseStatusCodePages();
            app.UseExceptionHandler(new ExceptionHandlerOptions {
                AllowStatusCode404Response = true,
                ExceptionHandler = async httpContext => {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
                    if(exceptionHandlerPathFeature?.Error != null) {
                        logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception");
                    }

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
                    else if(env.IsDevelopment()) {
                        httpContext.Response.ContentType = System.Net.Mime.MediaTypeNames.Text.Plain;
                        await httpContext.Response.WriteAsync(exceptionHandlerPathFeature?.Error?.ToString());
                    }
                    else {
                        // Write nothing
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
            forwardOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.20.0.1"), 2));
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
