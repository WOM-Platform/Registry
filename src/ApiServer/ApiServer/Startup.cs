using System;
using System.Globalization;
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

            services.AddAuthentication(opt => {
                opt.DefaultAuthenticateScheme = BasicAuthenticationSchemeOptions.DefaultScheme;
                opt.DefaultChallengeScheme = BasicAuthenticationSchemeOptions.DefaultScheme;
            }).AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationSchemeOptions.DefaultScheme, opt => {
                // Noop
            });

            // Add services to dependency registry
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<KeyManager>();
            services.AddSingleton<CryptoProvider>();
            services.AddScoped<DatabaseOperator>();
        }

        private readonly string[] SupportedCultures = new string[] {
            "en-US",
            "it-IT"
        };

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            //app.UseStaticFiles("/payment");
            //app.UseStaticFiles("/vouchers");
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
