namespace WomPlatform.Web.Api.OutputModels {
    public class GeoBounds {
        public double[] LeftTop { get; set; }

        public double[] RightBottom { get; set; }
    }

    public static class GeoBoundsExtensions {
        public static GeoBounds ToOutput(this DatabaseDocumentModels.Bounds bounds) {
            return (bounds == null) ? null : new GeoBounds {
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
