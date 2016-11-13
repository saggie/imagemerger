using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageMerger
{
    public class ImagesMerger
    {
        public Bitmap margedImage;
        private ImageFormat outputImageFormat;

        private string settingFilePath;
        private string workingDirectoryPath;

        private ImageSettingsManager imageSettingsManager = new ImageSettingsManager();
        private ImageSettings settings;

        public void Init(string settingFilePath)
        {
            this.settingFilePath = settingFilePath;
            Refresh();
        }

        public void Refresh()
        {
            settings = imageSettingsManager.ReadSettings(settingFilePath);
            workingDirectoryPath = System.IO.Path.GetDirectoryName(settingFilePath);

            var sourceImages = new List<SourceImage>();
            foreach (var eachSettings in settings.sourceImages)
            {
                sourceImages.Add(LoadSourceImage(eachSettings));
            }

            var maxWidth = sourceImages.Select(i => i.width).Max();
            var maxHeight = sourceImages.Select(i => i.height).Max();
            outputImageFormat = sourceImages.First().imageFormat; // FIXME

            CreateMergedImage(sourceImages, maxWidth, maxHeight);
        }

        internal string GetFileName()
        {
            return settings.outputFileName;
        }

        private SourceImage LoadSourceImage(SourceImageInfo sourceImageInfo)
        {
            SourceImage ret = new SourceImage();

            var filePath = System.IO.Path.Combine(workingDirectoryPath, sourceImageInfo.fileName);
            using (Bitmap sourceBitmap = new Bitmap(Image.FromFile(filePath)))
            {
                ret.width = sourceBitmap.Width;
                ret.height = sourceBitmap.Height;
                ret.imageFormat = GetImageFormatFromFileExtension(filePath);
                ret.pixels = sourceBitmap.ToByteArray();
            }

            ret.alpha = sourceImageInfo.alpha;
            ret.isShadow = sourceImageInfo.isShadow;
            ret.margin = sourceImageInfo.margin;

            return ret;
        }

        internal void Save()
        {
            SaveMergedImage(settings.outputFileName, outputImageFormat);
        }

        private ImageFormat GetImageFormatFromFileExtension(string fileName)
        {
            switch (System.IO.Path.GetExtension(fileName).ToLower()) {
                case ".bmp": return ImageFormat.Bmp;
                case ".gif": return ImageFormat.Gif;
                case ".jpg":
                case ".jpeg": return ImageFormat.Jpeg;
                case ".png":
                default: return ImageFormat.Png;
            }
        }

        private void CreateMergedImage(IList<SourceImage> sourceImages, int width, int height)
        {
            var mergedPixels = new byte[width * height * 4];
            bool isFirstLayer = true;
            foreach (var eachImage in sourceImages.Reverse())
            {
                for (var yi = 0; yi < eachImage.height; yi++)
                {
                    for (var xi = 0; xi < eachImage.width; xi++)
                    {
                        var pixel = PixelUtil.GetPixel(eachImage.pixels, xi, yi, eachImage.width);

                        // skip if nothing to draw (from 2nd layer)
                        if (!isFirstLayer && pixel.IsWhite()) { continue; }

                        // process shadowing
                        if (eachImage.isShadow && !pixel.IsWhite())
                        {
                            var sourcePixel = PixelUtil.GetPixel(mergedPixels, xi, yi, width);
                            pixel = PixelUtil.GetShadowedPixel(sourcePixel);
                        }

                        mergedPixels[PixelUtil.GetAddressR(xi, yi, width)] = pixel[0];
                        mergedPixels[PixelUtil.GetAddressG(xi, yi, width)] = pixel[1];
                        mergedPixels[PixelUtil.GetAddressB(xi, yi, width)] = pixel[2];
                        mergedPixels[PixelUtil.GetAddressA(xi, yi, width)] = pixel[3];
                    }
                }
                isFirstLayer = false;
            }

            margedImage = mergedPixels.ToBitmap(width, height);
        }

        private void SaveMergedImage(string fileName, ImageFormat imageFormat)
        {
            var filePath = System.IO.Path.Combine(workingDirectoryPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            margedImage.Save(filePath, imageFormat);
        }
    }
}
