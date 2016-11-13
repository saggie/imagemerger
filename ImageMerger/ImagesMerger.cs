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
                ret.pixels = ConvertBitmapToByteArray(sourceBitmap);
            }

            ret.alpha = sourceImageInfo.alpha;
            ret.isShadow = sourceImageInfo.isShadow;
            ret.margin = sourceImageInfo.margin;

            return ret;
        }

        private byte[] ConvertBitmapToByteArray(Bitmap bitmap)
        {
            var ret = new byte[bitmap.Width * bitmap.Height * 4];

            BitmapData data = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

            Marshal.Copy(data.Scan0, ret, 0, ret.Length);

            bitmap.UnlockBits(data);

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
                        var pixel = GetPixel(eachImage.pixels, xi, yi, eachImage.width);

                        if (!isFirstLayer && IsWhitePixel(pixel)) { continue; }

                        mergedPixels[GetAddressR(xi, yi, width)] = pixel[0];
                        mergedPixels[GetAddressG(xi, yi, width)] = pixel[1];
                        mergedPixels[GetAddressB(xi, yi, width)] = pixel[2];
                        mergedPixels[GetAddressA(xi, yi, width)] = pixel[3];
                    }
                }
                isFirstLayer = false;
            }

            margedImage = ConvertByteArrayToBitmap(mergedPixels, width, height);
        }

        private byte[] GetPixel(byte[] sourcePixels, int addrX, int addrY, int width)
        {
            return new byte[]
            {
                sourcePixels[GetAddressR(addrX, addrY, width)],
                sourcePixels[GetAddressG(addrX, addrY, width)],
                sourcePixels[GetAddressB(addrX, addrY, width)],
                0xFF
            };
        }

        private int GetAddressR(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 0; }
        private int GetAddressG(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 1; }
        private int GetAddressB(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 2; }
        private int GetAddressA(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 3; }

        private Bitmap ConvertByteArrayToBitmap(byte[] byteArray, int width, int height)
        {
            Bitmap ret = new Bitmap(width, height);

            BitmapData data = ret.LockBits(
                        new Rectangle(0, 0, ret.Width, ret.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

            Marshal.Copy(byteArray, 0, data.Scan0, byteArray.Length);

            ret.UnlockBits(data);

            return ret;
        }

        private bool IsWhitePixel(byte[] pixel)
        {
            return (pixel[0] == 0xff) && (pixel[1] == 0xff) && (pixel[2] == 0xff);
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
