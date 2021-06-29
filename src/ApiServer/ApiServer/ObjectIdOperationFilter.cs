using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Filters out ObjectId components from Swagger.
    /// </summary>
    class ObjectIdOperationFilter : IOperationFilter {
        private readonly IEnumerable<string> objectIdIgnoreParameters = new[] {
            nameof(ObjectId.Timestamp),
            nameof(ObjectId.Machine),
            nameof(ObjectId.Pid),
            nameof(ObjectId.Increment),
            nameof(ObjectId.CreationTime)
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            operation.Parameters = operation.Parameters.Where(
                x => !objectIdIgnoreParameters.Contains(x.Name)
            ).ToList();
        }
    }
}
