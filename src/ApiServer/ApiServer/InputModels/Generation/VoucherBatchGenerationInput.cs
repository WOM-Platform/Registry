using System;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.InputModels.Generation {
    public class VoucherBatchGenerationInput {
        public ObjectId SourceId { get; set; }

        public string Title { get; set; } 

        public class VoucherBatchSpecification {
            public string Email { get; set; }

            public int Count { get; set; }

            public string Aim { get; set; }

            public GeoCoordsInput Location { get; set; }

            public DateTime Timestamp { get; set; }

            public VoucherCreationMode CreationMode { get; set; }
        }

        public VoucherBatchSpecification[] Users { get; set; }
    }
}
