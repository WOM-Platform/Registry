using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Blurhash.ImageSharp;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using WomPlatform.Web.Api.OutputModels;

namespace WomPlatform.Web.Api.Service {
    public class PicturesService {

        private readonly StorageClient _storageClient;
        private readonly ILogger<PicturesService> _logger;

        private readonly string _googleStorageBucketName;
        private readonly int _resolutionFullMaxLength;
        private readonly string _resolutionFullPartPath;
        private readonly int _resolutionMidMaxLength;
        private readonly string _resolutionMidPartPath;
        private readonly int _resolutionHighMaxLength;
        private readonly string _resolutionHighPartPath;
        private readonly int _resolutionThumbMaxLength;
        private readonly string _resolutionThumbPartPath;
        private readonly int _blurHashComponentsNumber;
        private readonly string _baseUrl;

        public readonly PictureOutput DefaultPosCover;

        private const string JpegPathPart = ".jpg";

        public PicturesService(
            IConfiguration configuration,
            ILogger<PicturesService> logger
        ) {
            _logger = logger;

            var picturesConfigSection = configuration.GetSection("Pictures");
            _googleStorageBucketName = picturesConfigSection["GoogleCloudStorageBucket"];
            _resolutionFullMaxLength = Convert.ToInt32(picturesConfigSection["FullSizeMaxLength"]);
            _resolutionFullPartPath = picturesConfigSection["FullSizePathPart"];
            _resolutionMidMaxLength = Convert.ToInt32(picturesConfigSection["MidDensityMaxLength"]);
            _resolutionMidPartPath = picturesConfigSection["MidDensityPathPart"];
            _resolutionHighMaxLength = Convert.ToInt32(picturesConfigSection["HighDensityMaxLength"]);
            _resolutionHighPartPath = picturesConfigSection["HighDensityPathPart"];
            _resolutionThumbMaxLength = Convert.ToInt32(picturesConfigSection["SquareThumbnailMaxLength"]);
            _resolutionThumbPartPath = picturesConfigSection["SquareThumbnailPathPart"];
            _blurHashComponentsNumber = Convert.ToInt32(picturesConfigSection["BlurHashComponentsNumber"]);
            _baseUrl = picturesConfigSection["BaseUrl"];

            DefaultPosCover = GetPictureOutput(
                picturesConfigSection["DefaultPosCoverPath"],
                picturesConfigSection["DefaultPosCoverBlurHash"]
            );

            _storageClient = StorageClient.Create();
        }

        /// <summary>
        /// Creates a square image, resizing and cropping the original.
        /// </summary>
        private Image<SixLabors.ImageSharp.PixelFormats.Rgb24> CreateThumbnail(Image original, int maxLength) {
            var clone = original.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgb24>();

            clone.Mutate(x => x
                .AutoOrient()
                .Resize(new ResizeOptions { Mode = ResizeMode.Crop, Size = new Size(maxLength) })
            );

            return clone;
        }

        /// <summary>
        /// Create a scaled version of an image, cropping it to a square and downscaling/upscaling if necessary.
        /// </summary>
        private Image<SixLabors.ImageSharp.PixelFormats.Rgb24> CreateScaled(Image original, int maxLength) {
            var clone = original.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgb24>();

            if(original.Width <= maxLength && original.Height <= maxLength) {
                clone.Mutate(x => x
                    .AutoOrient()
                );
            }
            else {
                clone.Mutate(x => x
                    .AutoOrient()
                    .Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxLength) })
                );
            }

            return clone;
        }

        private async Task StoreJpeg(Image image, string path) {
            using var buffer = new MemoryStream(3 * 1024 * 1024); // preallocate 3 MiB

            await image.SaveAsJpegAsync(buffer, new JpegEncoder { Quality = 60 });

            buffer.Seek(0, SeekOrigin.Begin);
            await _storageClient.UploadObjectAsync(_googleStorageBucketName, path, MediaTypeNames.Image.Jpeg, buffer);

            _logger.LogDebug("Written JPEG image as {0} bytes and uploaded with path {1}", buffer.Length, path);
        }

        private async Task<(string Url, string BlurHash)> ProcessAndUploadThumbnail(Image image, string basePath) {
            var targetPath = basePath + _resolutionThumbPartPath + JpegPathPart;

            using(var thumbnail = CreateThumbnail(image, _resolutionThumbMaxLength)) {
                _logger.LogDebug("Resized thumbnail image to {0}×{1}", thumbnail.Width, thumbnail.Height);
                await StoreJpeg(thumbnail, targetPath);

                return (
                    targetPath,
                    Blurhasher.Encode(thumbnail, _blurHashComponentsNumber, _blurHashComponentsNumber)
                );
            }
        }

        private async Task<string> ProcessAndUploadPicture(Image image, string basePath, string pathPart, int maxLength) {
            var targetPath = basePath + pathPart + JpegPathPart;

            using(var resized = CreateScaled(image, maxLength)) {
                _logger.LogDebug("Resized image to {0}×{1}", resized.Width, resized.Height);
                await StoreJpeg(resized, targetPath);

                return targetPath;
            }
        }

        public enum PictureUsage {
            PosCover,
        }

        private static string GenerateNewPath(string basePath, PictureUsage usage) {
            if(string.IsNullOrWhiteSpace(basePath)) {
                throw new ArgumentException("Picture base path cannot be empty");
            }

            return usage switch {
                PictureUsage.PosCover => $"pos-covers/{basePath}/{Guid.NewGuid():N}",
                _ => throw new ArgumentException("Unsupported picture usage"),
            };
        }

        public async Task<(string PicturePath, string BlurHash)> ProcessAndUploadPicture(Stream pictureData, string basePath, PictureUsage usage) {
            var picturePath = GenerateNewPath(basePath, usage);

            pictureData.Seek(0, SeekOrigin.Begin);

            using var original = Image.Load(pictureData);

            // Aspect ratio check
            double aspectRatio = original.Height / (double)original.Width;
            _logger.LogDebug("Loaded image {0}×{1} px (ratio: {2})", original.Width, original.Height, aspectRatio);
            if(aspectRatio < 0.25 || aspectRatio > 4) {
                throw new ArgumentException($"Picture aspect ratio too extreme ({Math.Round(aspectRatio, 2)})", nameof(pictureData));
            }

            // Prep and upload scaled images
            var taskThumbnail = ProcessAndUploadThumbnail(original, picturePath);
            var taskFull = ProcessAndUploadPicture(original, picturePath, _resolutionFullPartPath, _resolutionFullMaxLength);
            var taskMid = ProcessAndUploadPicture(original, picturePath, _resolutionMidPartPath, _resolutionMidMaxLength);
            var taskHigh = ProcessAndUploadPicture(original, picturePath, _resolutionHighPartPath, _resolutionHighMaxLength);
            await Task.WhenAll(taskThumbnail, taskFull, taskMid, taskHigh);

            return (picturePath, taskThumbnail.Result.BlurHash);
        }

        public PictureOutput GetPictureOutput(string basePath, string blurHash, string pathSuffix = JpegPathPart) {
            if(string.IsNullOrWhiteSpace(basePath)) {
                return null;
            }

            return new PictureOutput {
                FullSizeUrl = _baseUrl + basePath + _resolutionFullPartPath + pathSuffix,
                MidDensityFullWidthUrl = _baseUrl + basePath + _resolutionMidPartPath + pathSuffix,
                HighDensityFullWidthUrl = _baseUrl + basePath + _resolutionHighPartPath + pathSuffix,
                SquareThumbnailUrl = _baseUrl + basePath + _resolutionThumbPartPath + pathSuffix,
                BlurHash = blurHash,
            };
        }

    }
}
