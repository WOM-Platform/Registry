using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ApiTester {

    class ConsoleLoggerFactory : ILoggerFactory {

        public void AddProvider(ILoggerProvider provider) {
            
        }

        public ILogger CreateLogger(string categoryName) {
            return new ConsoleLogger();
        }

        public void Dispose() {
            
        }

    }

}
