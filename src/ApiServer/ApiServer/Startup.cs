using System;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace WomPlatform.Web.Api {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public const string UserLoginCookieScheme = "UserLoginCookieScheme";
        public const string UserLoginPolicy = "UserLoginPolicy";

        public void ConfigureServices(IServiceCollection services) {
            services.AddRouting(options => {
                options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });

            services.AddControllersWithViews()
                .AddMvcOptions(opts => {
                    opts.AllowEmptyInputInBodyModelBinding = true;
                    opts.InputFormatters.Add(new PermissiveInputFormatter());
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

            services.AddAuthentication()
                .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationSchemeOptions.DefaultScheme, opt => {
                    // Noop
                })
                .AddCookie(UserLoginCookieScheme, options => {
                    options.LoginPath = "/user/login";
                    options.LogoutPath = "/user/logout";
                    options.ReturnUrlParameter = "return";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.Cookie = new CookieBuilder {
                        Domain = "wom.social",
                        IsEssential = true,
                        Name = "WOM Login",
                        SecurePolicy = CookieSecurePolicy.Always,
                        SameSite = SameSiteMode.None,
                        HttpOnly = true
                    };
                })
            ;
            services.AddAuthorization(options => {
                options.AddPolicy(
                    UserLoginPolicy,
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(UserLoginCookieScheme)
                        .Build()
                );
            });

            // Add services to dependency registry
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<KeyManager>();
            services.AddSingleton<CryptoProvider>();
            services.AddScoped<DatabaseOperator>();
            services.AddSingleton<MongoDatabase>();
        }

        private readonly string[] SupportedCultures = new string[] {
            "en-US",
            "it-IT"
        };

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRequestLocalization(o => {
                o.AddSupportedCultures(SupportedCultures);
                o.AddSupportedUICultures(SupportedCultures);
                o.DefaultRequestCulture = new RequestCulture(SupportedCultures[0]);
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
