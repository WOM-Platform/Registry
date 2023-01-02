namespace WomPlatform.Web.Api.OutputModels {
    public class PictureOutput {
        /// <summary>
        /// URL to full-sized version of the image.
        /// </summary>
        /// <remarks>
        /// This image is usually too large to be used in a mobile setting.
        /// </remarks>
        public string FullSizeUrl { get; init; }

        /// <summary>
        /// URL to image with full mobile width for mid-density screens.
        /// </summary>
        /// <remarks>
        /// Assumes 640 dp for compact screens × medium density (mdpi) = 640 pixels.
        /// </remarks>
        public string MidDensityFullWidthUrl { get; init; }

        /// <summary>
        /// URL to image with full mobile width for high-density screens.
        /// </summary>
        /// <remarks>
        /// Assumes 640 dp for compact screens × extra-high density (xhdpi) = 1280 pixels.
        /// </remarks>
        public string HighDensityFullWidthUrl { get; init; }

        /// <summary>
        /// URL to square thumbnail of image, with high-density.
        /// </summary>
        /// <remarks>
        /// Assumes 300×300 dp with extra-high density (xhdpi) = 600×600 pixels.
        /// </remarks>
        public string SquareThumbnailUrl { get; init; }
    }
}
