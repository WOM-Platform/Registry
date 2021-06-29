using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.OutputModels {

    public record UserOutput {

        public string Id { get; init; }
        public string Email { get; init; }
        public string Name { get; init; }
        public string Surname { get; init; }

    }

}
