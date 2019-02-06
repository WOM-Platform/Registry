using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace WomPlatform.Web.Api {

    public class DatabaseManager {

        protected IConfiguration Configuration { get; }
        protected IHttpContextAccessor ContextAccessor { get; }
        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger<DatabaseManager> Logger { get; }

        /// <summary>
        /// Gets the connection string used to connecto to the database.
        /// </summary>
        public string ConnectionString { get; private set; }

        public DatabaseManager(
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor,
            ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            ContextAccessor = contextAccessor;
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger<DatabaseManager>();

            // Build connection string once
            var dbSection = Configuration.GetSection("Database");
            var host = dbSection["Host"];
            var port = Convert.ToInt32(dbSection["Port"]);
            var username = dbSection["Username"];
            var password = dbSection["Password"];
            var schema = dbSection["Schema"];
            ConnectionString = string.Format(
                "server={0};port={1};uid={2};pwd={3};database={4}",
                host, port, username, password, schema
            );

            Logger.LogDebug(LoggingEvents.Database, "Database connection string: {0}", ConnectionString);
        }

        private DataContext _context = null;

        /// <summary>
        /// Gets an open connection to the database.
        /// </summary>
        public DataContext Context {
            get {
                if(_context == null) {
                    _context = new DataContext(
                        ConnectionString,
                        LoggerFactory.CreateLogger<DataContext>()
                    );
                    ContextAccessor.HttpContext.Response.RegisterForDispose(_context);

                    Logger.LogTrace(LoggingEvents.DatabaseConnection, "Opening DataContext");
                }

                return _context;
            }
        }

    }
}
