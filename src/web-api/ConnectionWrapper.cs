using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Simple connection wrapper to keep track of a <see cref="System.Data.Common.DbConnection"/> instance.
    /// </summary>
    public class ConnectionWrapper : IDisposable {

        protected readonly DbConnection _innerConnection;
        protected readonly ILogger<ConnectionWrapper> _logger;

        public ConnectionWrapper(DbConnection connection, ILogger<ConnectionWrapper> logger) {
            this._innerConnection = connection;
            this._logger = logger;
        }

        public static implicit operator DbConnection(ConnectionWrapper wrapper) {
            return wrapper._innerConnection;
        }

        public DbConnection Content {
            get {
                return this._innerConnection;
            }
        }

        public void Dispose() {
            this._innerConnection.Dispose();
            this._logger.LogDebug(LoggingEvents.DatabaseConnection, "Database connection disposed");
        }

    }

}
