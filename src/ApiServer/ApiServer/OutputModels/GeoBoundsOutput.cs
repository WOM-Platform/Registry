namespace WomPlatform.Web.Api.OutputModels {
    public class GeoBoundsOutput {
        public double[] LeftTop { get; set; }

        public double[] RightBottom { get; set; }
    }

    public static class GeoBoundsOutputExtensions {
        public static GeoBoundsOutput ToOutput(this DatabaseDocumentModels.Bounds? bounds) {
            if(bounds == null) {
                return null;
            }

            return new GeoBoundsOutput {
                LeftTop = [
                    bounds.LeftTop.Latitude, bounds.LeftTop.Longitude
                ],
                RightBottom = [
                    bounds.RightBottom.Latitude, bounds.RightBottom.Longitude
                ]
            };
        }
    }
}
