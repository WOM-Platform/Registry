using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.OutputModels {

    public record PosOutput {

        public string Id { get; init; }
        public string Name { get; init; }
        public string Url { get; init; }

    }

    public record PosLoginOutput : PosOutput {

        public string PrivateKey { get; init; }
        public string PublicKey { get; init; }

    }

}
