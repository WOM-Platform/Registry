using System;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.InputModels.Generation {
    public class VoucherGenerationSpecification {
        public int Count { get; set; }

        public string Aim { get; set; }

        public GeoCoordsInput Location { get; set; }

        public DateTime Timestamp { get; set; }

        public VoucherCreationMode CreationMode { get; set; }
    }

    public static class VoucherGenerationSpecificationExtensions {
        public static VoucherGenerationSpecification ToSpecification(this VoucherCreatePayload.VoucherInfo voucherInfo) {
            if(voucherInfo == null) {
                throw new ArgumentNullException();
            }

            return new VoucherGenerationSpecification {
                Count = voucherInfo.Count,
                Aim = voucherInfo.Aim,
                Location = new GeoCoordsInput {
                    Latitude = voucherInfo.Latitude,
                    Longitude = voucherInfo.Longitude,
                },
                Timestamp = voucherInfo.Timestamp,
                CreationMode = voucherInfo.CreationMode.ToInternal(),
            };
        }
    }
}
