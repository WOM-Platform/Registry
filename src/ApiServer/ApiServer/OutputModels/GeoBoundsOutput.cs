namespace WomPlatform.Web.Api.OutputModels {
    public class GeoBoundsOutput {
        public double[] LeftTop { get; set; }

        public double[] RightBottom { get; set; }
    }

    public static class GeoBoundsOutputExtensions {
        public static GeoBoundsOutput ToOutput(this DatabaseDocumentModels.Bounds bounds) {
            return (bounds == null) ? null : new GeoBoundsOutput {
                LeftTop = new double[] {
                    bounds.LeftTop.Latitude, bounds.LeftTop.Longitude
                },
                RightBottom = new double[] {
                    bounds.RightBottom.Latitude, bounds.RightBottom.Longitude
                }
            };
        }
    }
}
