using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public enum VoucherCreationMode {
        Standard,
        SetLocationOnRedeem
    }

    public static class VoucherCreationModeExtensions {
        public static VoucherCreationMode ToInternal(this VoucherCreatePayload.VoucherCreationMode mode) {
            return mode switch {
                VoucherCreatePayload.VoucherCreationMode.SetLocationOnRedeem => VoucherCreationMode.SetLocationOnRedeem,
                _ => VoucherCreationMode.Standard,
            };
        }
    }
}
