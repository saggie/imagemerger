using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageMerger
{
    public class ImagesMerger
    {
        internal Bitmap mergedImage;

        private string settingFilePath;
        private string workingDirectoryPath;

        private ImageSettingsManager imageSettingsManager = new ImageSettingsManager();
        private ImageSettings settings;

        // Map: "image path" to "last write time"
        internal IDictionary<string, long> lastUpdateMap = new Dictionary<string, long>();

        internal void Initialize(string settingFilePath)
        {
            this.settingFilePath = settingFilePath;
            Refresh();
        }

        internal void Refresh()
        {
            settings = imageSettingsManager.ReadSettings(settingFilePath);
            workingDirectoryPath = System.IO.Path.GetDirectoryName(settingFilePath);

            var sourceImages = new List<SourceImageInfo>();
            foreach (var eachSettings in settings.sourceImages)
            {
                sourceImages.Add(LoadSourceImage(eachSettings));
            }

            var maxWidth = sourceImages.Select(i => i.width).Max();
            var maxHeight = sourceImages.Select(i => i.height).Max();

            CreateMergedImage(sourceImages, maxWidth, maxHeight);
        }

        internal string GetFileName()
        {
            return settings.outputFileName;
        }

        private SourceImageInfo LoadSourceImage(SourceImageSettings sourceImageInfo)
        {
            SourceImageInfo ret = new SourceImageInfo();

            var filePath = Path.Combine(workingDirectoryPath, sourceImageInfo.fileName);
            using (Bitmap sourceBitmap = new Bitmap(Image.FromFile(filePath)))
            {
                ret.width = sourceBitmap.Width;
                ret.height = sourceBitmap.Height;
                ret.imageFormat = GetImageFormatFromFileExtension(filePath);
                ret.pixels = sourceBitmap.ToByteArray();
            }

            ret.alphaValue = sourceImageInfo.alphaValue;
            ret.isShadowLayer = sourceImageInfo.isShadow;
            ret.maskedPixelsInfo = (sourceImageInfo.maskInfo != null)
                ? PixelUtil.CreateMaskedPixelsInfo(ret.pixels, ret.width, ret.height, sourceImageInfo.maskInfo)
                : null;

            return ret;
        }

        internal void SaveMergedImage()
        {
            var fileName = settings.outputFileName;
            var filePath = Path.Combine(workingDirectoryPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            var fileFormat = GetImageFormatFromFileExtension(fileName);
            mergedImage.Save(filePath, fileFormat);
        }

        private ImageFormat GetImageFormatFromFileExtension(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLower()) {
                case ".bmp": return ImageFormat.Bmp;
                case ".gif": return ImageFormat.Gif;
                case ".jpg":
                case ".jpeg": return ImageFormat.Jpeg;
                case ".png":
                default: return ImageFormat.Png;
            }
        }

        private void CreateMergedImage(IList<SourceImageInfo> sourceImages, int width, int height)
        {
            var mergedPixels = new byte[width * height * 4];
            bool isNotTheFirstLayer = false;
            foreach (var eachImage in sourceImages.Reverse())
            {
                var sourcePixels = eachImage.pixels;

                // merge images
                for (var yi = 0; yi < eachImage.height; yi++)
                {
                    for (var xi = 0; xi < eachImage.width; xi++)
                    {
                        // process region-mask
                        if (eachImage.maskedPixelsInfo != null &&
                            eachImage.maskedPixelsInfo[xi + yi * eachImage.width])
                        {
                            mergedPixels[PixelUtil.GetAddressB(xi, yi, width)] = 0xFF;
                            mergedPixels[PixelUtil.GetAddressG(xi, yi, width)] = 0xFF;
                            mergedPixels[PixelUtil.GetAddressR(xi, yi, width)] = 0xFF;
                            mergedPixels[PixelUtil.GetAddressA(xi, yi, width)] = 0xFF;
                            continue;
                        }

                        var drawingPixel = sourcePixels.GetPixelAt(xi, yi, eachImage.width);

                        // skip if nothing to draw
                        if (drawingPixel.IsWhite() && isNotTheFirstLayer) { continue; }

                        // process shadowing
                        if (eachImage.isShadowLayer && !drawingPixel.IsWhite())
                        {
                            var sourcePixel = mergedPixels.GetPixelAt(xi, yi, width);
                            drawingPixel = PixelUtil.GetShadowedPixel(sourcePixel);
                        }

                        // process alpha-blending
                        if (eachImage.alphaValue < 1 || eachImage.alphaValue > 0)
                        {
                            var sourcePixel = mergedPixels.GetPixelAt(xi, yi, width);
                            drawingPixel = drawingPixel.BlendWith(sourcePixel, eachImage.alphaValue);
                        }

                        mergedPixels[PixelUtil.GetAddressB(xi, yi, width)] = drawingPixel[0];
                        mergedPixels[PixelUtil.GetAddressG(xi, yi, width)] = drawingPixel[1];
                        mergedPixels[PixelUtil.GetAddressR(xi, yi, width)] = drawingPixel[2];
                        mergedPixels[PixelUtil.GetAddressA(xi, yi, width)] = drawingPixel[3];
                    }
                }
                isNotTheFirstLayer = true;
            }

            mergedImage = mergedPixels.ToBitmap(width, height);
        }

        internal bool IsImageFileUpdated()
        {
            var latestMap = CreateLastUpdateMap();
            foreach (var eachImage in latestMap.Keys)
            {
                if (!lastUpdateMap.ContainsKey(eachImage) ||
                    lastUpdateMap[eachImage] != latestMap[eachImage])
                {
                    return true;
                }
            }
            return false;
        }

        internal void UpdateLastUpdateMap()
        {
            lastUpdateMap = CreateLastUpdateMap();
        }

        private IDictionary<string, long> CreateLastUpdateMap() {
            var ret = new Dictionary<string, long>();
            foreach (var eachSourceImage in settings.sourceImages)
            {
                var filePath = Path.Combine(workingDirectoryPath, eachSourceImage.fileName);
                ret[filePath] = File.GetLastWriteTime(filePath).Ticks;
            }
            return ret;
        }
    }
}
