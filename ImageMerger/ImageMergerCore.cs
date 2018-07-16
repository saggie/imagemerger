using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageMerger
{
    public class ImageMergerCore
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

            sourceImagesManager.refreshSourceImages(settings.sourceImages, workingDirectoryPath, settings.id);

            colorReplacementInfoList.Clear();
            if (settings.colorReplacement != null)
            {
                foreach (var eachColorReplacementSetting in settings.colorReplacement)
                {
                    colorReplacementInfoList.Add(new ColorReplacementInfo(eachColorReplacementSetting));
                }
            }

            CreateMergedImage(sourceImagesManager.sourceImages,
                              sourceImagesManager.GetMaxWidth(),
                              sourceImagesManager.GetMaxHeight());
            UpdateLastUpdateMap();
        }

        internal bool GetAutoSaveAndExitOption()
        {
            return settings.autoSaveAndExit;
        }

        internal string GetOutputFileName()
        {
            var outputFileName = settings.outputFileName;

            // replace "<ID>"
            if (outputFileName.ContainsIgnoreCase("<id>"))
            {
                outputFileName = outputFileName.ToLower().Replace("<id>", settings.id ?? "");
            }

            return outputFileName;
        }

        internal void SaveMergedImage()
        {
            var fileName = GetOutputFileName();
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
                if (!eachImage.isAvailable)
                {
                    continue;
                }

                var sourcePixels = eachImage.pixels;

                // merge images
                for (var yi = 0; yi < eachImage.height; yi++)
                {
                    for (var xi = 0; xi < eachImage.width; xi++)
                    {
                        var drawingPixel = sourcePixels.GetPixelAt(xi, yi, eachImage.width);

                        // position offsetting
                        var px = xi + eachImage.positionX;
                        var py = yi + eachImage.positionY;
                        if (px < 0 || px >= width || py <0 || py >= height)
                        {
                            continue;
                        }

                        // region-mask
                        bool isMaskedPixel = false;
                        if (IsMaskedPixel(eachImage.maskedPixelsInfo, xi, yi, eachImage.width))
                        {
                            drawingPixel = PixelUtil.GetWhite();
                            isMaskedPixel = true;
                        }

                        // skip if nothing to draw
                        if (IsNothingToDraw(drawingPixel, layerNum, isMaskedPixel)) { continue; }

                        // shadowing
                        if (IsShadowingTarget(eachImage, drawingPixel))
                        {
                            var sourcePixel = mergedPixels.GetPixelAt(px, py, width);
                            drawingPixel = PixelUtil.GetShadowedPixel(sourcePixel);
                        }

                        // color replacement
                        foreach (var eachColorReplacementInfo in colorReplacementInfoList)
                        {
                            if (drawingPixel.IsSameRgb(eachColorReplacementInfo.from))
                            {
                                drawingPixel = eachColorReplacementInfo.to;
                            }
                        }

                        // alpha-blending
                        if (eachImage.alphaInfo != null)
                        {
                            var alphaInfo = eachImage.alphaInfo;
                            if (IsAlphaBlendingApplicable(drawingPixel, isMaskedPixel, alphaInfo, layerNum))
                            {
                                var sourcePixel = mergedPixels.GetPixelAt(px, py, width);
                                drawingPixel = drawingPixel.BlendWith(sourcePixel, eachImage.alphaInfo.value);
                            }
                        }

                        mergedPixels[PixelUtil.GetAddressB(px, py, width)] = drawingPixel[0];
                        mergedPixels[PixelUtil.GetAddressG(px, py, width)] = drawingPixel[1];
                        mergedPixels[PixelUtil.GetAddressR(px, py, width)] = drawingPixel[2];
                        mergedPixels[PixelUtil.GetAddressA(px, py, width)] = drawingPixel[3];
                    }
                }
                layerNum++;
            }

            FinalizeImage(mergedPixels);

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

        private bool IsAlphaBlendingApplicable(byte[] drawingPixel, bool isMaskedPixel, AlphaInfo alphaInfo, int layerNum)
        {
            if (layerNum == 0) { return false; }
            if (isMaskedPixel && alphaInfo.excludeMask) { return false; }
            if (alphaInfo.ignoreList.ContainsSameRgb(drawingPixel)) { return false; }

            return true;
        }

        private bool IsShadowingTarget(SourceImageInfo sourceImageInfo, byte[] drawingPixel)
        {
            if (sourceImageInfo.isShadowLayer && !drawingPixel.IsWhite())
            {
                return true;
            }

            if (sourceImageInfo.shadowColor != null &&
                sourceImageInfo.shadowColor.IsSameRgb(drawingPixel))
            {
                return true;
            }

            return false;
        }

        private void FinalizeImage(byte[] mergedPixels)
        {
            if (settings.autoGrayScaling)
            {
                for (var i = 0; i < mergedPixels.Length; i += 4)
                {
                    if (mergedPixels[i] == mergedPixels[i+1] &&
                        mergedPixels[i] == mergedPixels[i+2]) { continue; }

                    var brightness = (mergedPixels[i] + mergedPixels[i+1] + mergedPixels[i+2]) / 3;
                    mergedPixels[i] = (byte)brightness;
                    mergedPixels[i+1] = (byte)brightness;
                    mergedPixels[i+2] = (byte)brightness;
                }
            }
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
