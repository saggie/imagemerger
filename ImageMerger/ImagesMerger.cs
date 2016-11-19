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

        private string settingsFilePath;
        private string workingDirectoryPath;

        private ImageSettingsManager imageSettingsManager = new ImageSettingsManager();
        private ImageSettings settings;

        private SourceImagesManager sourceImagesManager = new SourceImagesManager();

        private IList<ColorReplacementInfo> colorReplacementInfoList = new List<ColorReplacementInfo>();

        // Map: "image/settings file path" -> "last write time"
        internal IDictionary<string, long> lastUpdateMap = new Dictionary<string, long>();

        internal void Initialize(string settingsFilePath)
        {
            this.settingsFilePath = settingsFilePath;
            Refresh();
        }

        internal void Refresh()
        {
            settings = imageSettingsManager.ReadSettings(settingsFilePath);
            workingDirectoryPath = Path.GetDirectoryName(settingsFilePath);

            sourceImagesManager.refreshSourceImages(settings.sourceImages, workingDirectoryPath);

            colorReplacementInfoList.Clear();
            foreach (var eachColorReplacementSetting in settings.colorReplacement)
            {
                colorReplacementInfoList.Add(new ColorReplacementInfo(eachColorReplacementSetting));
            }

            CreateMergedImage(sourceImagesManager.sourceImages,
                              sourceImagesManager.GetMaxWidth(),
                              sourceImagesManager.GetMaxHeight());
        }

        internal string GetOutputFileName()
        {
            return settings.outputFileName;
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
            int layerNum = 0;
            foreach (var eachImage in sourceImages.Reverse())
            {
                var sourcePixels = eachImage.pixels;

                // merge images
                for (var yi = 0; yi < eachImage.height; yi++)
                {
                    for (var xi = 0; xi < eachImage.width; xi++)
                    {
                        var drawingPixel = sourcePixels.GetPixelAt(xi, yi, eachImage.width);

                        // process region-mask
                        bool isMaskedPixel = false;
                        if (IsMaskedPixel(eachImage.maskedPixelsInfo, xi, yi, eachImage.width))
                        {
                            drawingPixel = PixelUtil.GetWhite();
                            isMaskedPixel = true;
                        }

                        // skip if nothing to draw
                        if (IsNothingToDraw(drawingPixel, layerNum, isMaskedPixel)) { continue; }

                        // process shadowing
                        if (eachImage.isShadowLayer && !drawingPixel.IsWhite())
                        {
                            var sourcePixel = mergedPixels.GetPixelAt(xi, yi, width);
                            drawingPixel = PixelUtil.GetShadowedPixel(sourcePixel);
                        }

                        // process color replacement
                        foreach (var eachColorReplacementInfo in colorReplacementInfoList)
                        {
                            if (drawingPixel.IsSameRgb(eachColorReplacementInfo.from))
                            {
                                drawingPixel = eachColorReplacementInfo.to;
                            }
                        }

                        // process alpha-blending
                        if (eachImage.alphaInfo != null)
                        {
                            var alphaInfo = eachImage.alphaInfo;
                            if (IsAlphaBlendingApplicable(isMaskedPixel, alphaInfo.excludeMask))
                            {
                                var sourcePixel = mergedPixels.GetPixelAt(xi, yi, width);
                                if (!eachImage.alphaInfo.ignoreList.Contains(sourcePixel))
                                {
                                    drawingPixel = drawingPixel.BlendWith(sourcePixel, eachImage.alphaInfo.value);
                                }
                            }
                        }

                        mergedPixels[PixelUtil.GetAddressB(xi, yi, width)] = drawingPixel[0];
                        mergedPixels[PixelUtil.GetAddressG(xi, yi, width)] = drawingPixel[1];
                        mergedPixels[PixelUtil.GetAddressR(xi, yi, width)] = drawingPixel[2];
                        mergedPixels[PixelUtil.GetAddressA(xi, yi, width)] = drawingPixel[3];
                    }
                }
                layerNum++;
            }

            mergedImage = mergedPixels.ToBitmap(width, height);
        }

        private bool IsMaskedPixel(bool[] maskedPixelInfo, int xi, int yi, int width)
        {
            if (maskedPixelInfo != null && maskedPixelInfo[xi + yi * width])
            {
                return true;
            }
            return false;
        }

        private bool IsNothingToDraw(byte[] drawingPixel, int layerNum, bool isMaskedPixel)
        {
            if (drawingPixel.IsNotWhite()) { return false; }
            if (layerNum == 0) { return false; }
            if (isMaskedPixel) { return false; }

            return true;
        }

        private bool IsAlphaBlendingApplicable(bool isMaskedPixel, bool excludeMask)
        {
            if (isMaskedPixel && excludeMask) { return false; }
            return true;
        }

        internal bool IsFileUpdated()
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

            // for settings file
            ret[settingsFilePath] = File.GetLastWriteTime(settingsFilePath).Ticks;

            // for image files
            foreach (var eachFileName in sourceImagesManager.GetFileNames())
            {
                var filePath = Path.Combine(workingDirectoryPath, eachFileName);
                ret[filePath] = File.GetLastWriteTime(filePath).Ticks;
            }

            return ret;
        }
    }
}
