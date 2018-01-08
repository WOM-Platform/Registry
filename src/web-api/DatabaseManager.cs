using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace WomPlatform.Web.Api {

    public class DatabaseManager {

        protected readonly IConfiguration _configuration;
        protected readonly IHttpContextAccessor _contextAccessor;
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger<DatabaseManager> _logger;

        /// <summary>
        /// Gets the connection string used to connecto to the database.
        /// </summary>
        public string ConnectionString { get; private set; }

        public DatabaseManager(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILoggerFactory loggerFactory) {
            this._configuration = configuration;
            this._contextAccessor = contextAccessor;
            this._loggerFactory = loggerFactory;
            this._logger = loggerFactory.CreateLogger<DatabaseManager>();

            // Build connection string once
            var dbSection = this._configuration.GetSection("Database");
            var host = dbSection["Host"];
            var port = Convert.ToInt32(dbSection["Port"]);
            var username = dbSection["Username"];
            var password = dbSection["Password"];
            var schema = dbSection["Schema"];
            this.ConnectionString = string.Format("server={0};port={1};uid={2};pwd={3};database={4}",
                host, port, username, password, schema);

            this._logger.LogTrace(LoggingEvents.Database, "Database connection string: {0}", this.ConnectionString);
        }

        private ConnectionWrapper _connection = null;

        /// <summary>
        /// Gets an open connection to the database.
        /// </summary>
        public DbConnection Connection {
            get {
                if(this._connection == null) {
                    this._connection = new ConnectionWrapper(
                        new MySqlConnection(this.ConnectionString),
                        this._loggerFactory.CreateLogger<ConnectionWrapper>()
                    );
                    this._connection.Content.Open();

                    this._contextAccessor.HttpContext.Response.RegisterForDispose(this._connection);

                    this._logger.LogDebug(LoggingEvents.DatabaseConnection, "Database connection opened");
                }

                return this._connection;
            }
        }

    }
}
