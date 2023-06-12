using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Net.Codecrete.QrCodeGenerator;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("render")]
    [RequireHttpsInProd]
    public class RenderController : BaseRegistryController {

        public RenderController(
            IServiceProvider serviceProvider,
            ILogger<RenderController> logger
        ) : base(serviceProvider, logger) {
        }

        [HttpGet("qrcode")]
        [Produces("image/png")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult QrCode(
            [FromQuery] string url
        ) {
            var domain = Environment.GetEnvironmentVariable("SELF_HOST");
            if(url == null || !url.StartsWith($"https://{domain}/")) {
                return StatusCode(400);
            }

            var qrCodeData = QRCoder.PngByteQRCodeHelper.GetQRCode(url, QRCoder.QRCodeGenerator.ECCLevel.M, 15);
            return File(qrCodeData, "image/png");
        }

        private static FontCollection _customFontCollection;

        private static FontCollection CustomFontCollection {
            get {
                if(_customFontCollection == null) {
                    FontCollection fonts = new();
                    foreach(var fontName in new[] { "WomPlatform.Web.Api.Resources.Raleway-Regular.ttf", "WomPlatform.Web.Api.Resources.Raleway-Bold.ttf" }) {
                        using var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fontName);
                        fonts.Add(fontStream);
                    }

                    _customFontCollection = fonts;
                }

                return _customFontCollection;
            }
        }

        private static Color WomBlue {
            get {
                return Color.FromRgb(42, 104, 246);
            }
        }

        private static Color ContrastOrange {
            get {
                return Color.FromRgb(255, 192, 42);
            }
        }

        private const float PaddingTopSizeFraction = 0.12f;
        private const float PaddingBottomSizeFraction = 0.1f;
        private const float PaddingAmountSizeFraction = 0.035f;

        private const float MarginBelowTitle = 8f;
        private const float QrCodeMargin = 24f;
        private const float QrCodePadding = 24f;

        private const float TitleFontSizeFraction = 0.075f;
        private const float DescriptionFontSizeFraction = 0.038f;
        private const float AmountFontSizeFraction = 0.1f;

        private void DrawQrCode(IImageProcessingContext ctx, QrCode qrCode, Color background, Color foreground, Vector2 origin, Vector2 size, float backgroundPadding = 0) {
            if(size.X != size.Y) {
                throw new ArgumentException("QR Code must be drawn in a squared format");
            }

            ctx.Fill(background, new RectangleF(origin.X, origin.Y, size.X, size.Y));

            var blockSize = (size.X - (backgroundPadding * 2)) / qrCode.Size;
            for(int r = 0; r < qrCode.Size; ++r) {
                for(int c = 0; c < qrCode.Size; ++c) {
                    if(qrCode.GetModule(c, r)) {
                        ctx.Fill(foreground, new RectangleF(origin.X + backgroundPadding + (c * blockSize), origin.Y + backgroundPadding + (r * blockSize), blockSize, blockSize));
                    }
                }
            }
        }

        private (float Width, float Height) MeasureAndDraw(IImageProcessingContext ctx, string text, Color color, TextOptions textOptions) {
            var measures = TextMeasurer.Measure(text, textOptions);
            ctx.DrawText(textOptions, text, color);

            return (measures.Width, measures.Height);
        }

        private float DrawLeft(IImageProcessingContext ctx, Font font, string text, Color color, float imageWidth, float originHeight) {
            var options = new TextOptions(font) {
                TextAlignment = TextAlignment.Start,
                HorizontalAlignment = HorizontalAlignment.Left,
                WrappingLength = imageWidth * (1 - PaddingTopSizeFraction - PaddingTopSizeFraction),
                Origin = new Vector2(imageWidth * PaddingTopSizeFraction, originHeight),
            };

            var measures = TextMeasurer.Measure(text, options);
            ctx.DrawText(options, text, color);

            return measures.Height;
        }

        private float DrawCenteredFromBottom(IImageProcessingContext ctx, Font font, string text, Color color, float imageWidth, float originHeight) {
            var options = new TextOptions(font) {
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Origin = new Vector2(imageWidth / 2 , originHeight),
            };

            // Shift text up from bottom
            var measures = TextMeasurer.Measure(text, options);
            options.Origin = new Vector2(imageWidth / 2, originHeight - measures.Height);

            ctx.DrawText(options, text, color);

            return measures.Height;
        }

        private (float Width, float Height) DrawLeftFromBottom(IImageProcessingContext ctx, Font font, string text, Color color, Vector2 origin) {
            var options = new TextOptions(font) {
                TextAlignment = TextAlignment.Start,
                HorizontalAlignment = HorizontalAlignment.Left,
                Origin = origin,
            };

            // Shift text up from bottom
            var measures = TextMeasurer.Measure(text, options);
            options.Origin = new Vector2(origin.X, origin.Y - measures.Height);

            ctx.DrawText(options, text, color);

            return (measures.Width, measures.Height);
        }

        [HttpGet("offer/{offerId}")]
        [Produces("image/jpeg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RenderOffer(ObjectId offerId, string style) {
            var offer = await OfferService.GetOfferById(offerId);
            if(offer == null) {
                return NotFound();
            }

            var qrCode = Net.Codecrete.QrCodeGenerator.QrCode.EncodeText($"https://{SelfLinkDomain}/payment/{offer.Payment.Otc:D}", Net.Codecrete.QrCodeGenerator.QrCode.Ecc.Medium);

            var outputImage = await (style switch {
                "print" => GenerateOfferImage(offer, qrCode, "WomPlatform.Web.Api.Resources.base-offer-bw.jpg", Color.Black),
                _ => GenerateOfferImage(offer, qrCode, "WomPlatform.Web.Api.Resources.base-offer.jpg", WomBlue),
            });

            var outputStream = new MemoryStream();
            await outputImage.SaveAsJpegAsync(outputStream);
            outputStream.Position = 0;

            return File(outputStream, "image/jpeg");
        }

        private async Task<Image> GenerateOfferImage(Offer offer, QrCode qrCode, string backgroundResourceName, Color titleColor) {
            var backgroundStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(backgroundResourceName);
            var backgroundImage = await Image.LoadAsync(backgroundStream);

            var ralewayFont = CustomFontCollection.Get("Raleway");
            var titleFont = ralewayFont.CreateFont(backgroundImage.Width * TitleFontSizeFraction, FontStyle.Bold);
            var descriptionFont = ralewayFont.CreateFont(backgroundImage.Width * DescriptionFontSizeFraction, FontStyle.Regular);
            var amountFont = ralewayFont.CreateFont(backgroundImage.Width * AmountFontSizeFraction, FontStyle.Bold);

            backgroundImage.Mutate(ctx => {
                var paddingTop = backgroundImage.Height * PaddingTopSizeFraction;
                var paddingBottom = backgroundImage.Height * PaddingBottomSizeFraction;
                var paddingAmount = backgroundImage.Height * PaddingAmountSizeFraction;

                (_, var titleHeight) = MeasureAndDraw(ctx, offer.Title, titleColor, new TextOptions(titleFont) {
                    Origin = new Vector2(paddingTop, paddingTop),
                    WrappingLength = backgroundImage.Width - (paddingTop * 2),
                });

                float descriptionOffsetFromTop = paddingTop + titleHeight + MarginBelowTitle;
                if(!string.IsNullOrEmpty(offer.Description)) {
                    var descriptionHeight = DrawLeft(ctx, descriptionFont, offer.Description, Color.Black, backgroundImage.Width, descriptionOffsetFromTop);
                    descriptionOffsetFromTop += descriptionHeight;
                }

                var passwordHeight = DrawCenteredFromBottom(ctx, descriptionFont, "Password: " + offer.Payment.Password, Color.Black, backgroundImage.Width, backgroundImage.Height - paddingBottom);

                var qrCodeHeight = backgroundImage.Height - descriptionOffsetFromTop - paddingBottom - passwordHeight - (QrCodeMargin * 2);
                float qrCodeX = (backgroundImage.Width - qrCodeHeight) / 2f;
                float qrCodeY = descriptionOffsetFromTop + QrCodeMargin;
                DrawQrCode(ctx, qrCode, Color.White, Color.Black, new Vector2(qrCodeX, qrCodeY), new Vector2(qrCodeHeight, qrCodeHeight), QrCodePadding);

                var amountString = $"{offer.Payment.Cost}W";
                MeasureAndDraw(ctx, amountString, Color.Black, new TextOptions(amountFont) {
                    Origin = new Vector2(paddingAmount, backgroundImage.Height - paddingAmount - 130),
                    TextRuns = new[] { new TextRun { Start = amountString.Length - 1, End = amountString.Length, Font = descriptionFont } }
                });
            });

            return backgroundImage;
        }

    }

}
