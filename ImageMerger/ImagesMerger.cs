using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageMerger
{
    public class ImagesMerger
    {
        public Bitmap margedImage;

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
            var imageFormat = sourceImages.First().imageFormat; // FIXME

            CreateMergedImage(sourceImages, maxWidth, maxHeight);
            SaveMergedImage(settings.outputFileName, imageFormat);
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

                ret.pixels = new Color[sourceBitmap.Width * sourceBitmap.Height];
                for (var yi = 0; yi < sourceBitmap.Height; yi++)
                {
                    for (var xi = 0; xi < sourceBitmap.Width; xi++)
                    {
                        ret.pixels[xi + yi * sourceBitmap.Width] = sourceBitmap.GetPixel(xi, yi); // FIXME
                    }
                }
            }

            ret.alpha = sourceImageInfo.alpha;
            ret.isShadow = sourceImageInfo.isShadow;
            ret.margin = sourceImageInfo.margin;

            return ret;
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
            Bitmap bitmap = new Bitmap(width, height);
            bool isFirstLayer = true;
            foreach (var eachImage in sourceImages.Reverse())
            {
                for (var yi = 0; yi < eachImage.height; yi++)
                {
                    for (var xi = 0; xi < eachImage.width; xi++)
                    {
                        var pixel = eachImage.pixels[xi + yi * width];

                        if (IsWhite(pixel) && !isFirstLayer) { continue; }

                        bitmap.SetPixel(xi, yi, pixel); // TODO
                    }
                }
                isFirstLayer = false;
            }
            margedImage = bitmap;
        }

        private bool IsWhite(Color pixel)
        {
            return (pixel.R == 0xff) && (pixel.G == 0xff) && (pixel.B == 0xff);
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
