using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageMerger
{
    public class SourceImagesManager
    {
        public IList<SourceImageInfo> sourceImages = new List<SourceImageInfo>();

        private string workingDirectoryPath;

        internal void refreshSourceImages(IList<SourceImageSettings> sourceImageSettingsList,
                                          string workingDirectoryPath)
        {
            this.workingDirectoryPath = workingDirectoryPath;

            sourceImages.Clear();

            foreach (var eachImageSettings in sourceImageSettingsList)
            {
                // non-versined file
                if (!eachImageSettings.fileName.ToLower().Contains("<ver>"))
                {
                    sourceImages.Add(LoadSourceImage(eachImageSettings));
                    continue;
                }

                var fileNameSplitByVersion = eachImageSettings.fileName.ToLower().Split(new string[] { "<ver>" }, StringSplitOptions.None);
                var fileNameFormerPart = fileNameSplitByVersion.First();
                var fileNameLatterPart = fileNameSplitByVersion.Last();

                // create version list
                var versionList = new List<string>();
                foreach (var eachFilePath in Directory.GetFiles(workingDirectoryPath, fileNameFormerPart + "*"))
                {
                    if (!eachFilePath.EndsWith(fileNameLatterPart)) { continue; }

                    var fileName = Path.GetFileName(eachFilePath);
                    var version = fileName.Replace(fileNameFormerPart, "").Replace(fileNameLatterPart, "");
                    versionList.Add(version);
                }

                var latestVersionFileName = fileNameFormerPart + versionList.Max() + fileNameLatterPart;
                sourceImages.Add(LoadSourceImage(eachImageSettings, latestVersionFileName));
            }
        }

        private SourceImageInfo LoadSourceImage(SourceImageSettings sourceImageSettings, string versionedFileName = null)
        {
            SourceImageInfo ret = new SourceImageInfo();

            ret.fileName = versionedFileName ?? sourceImageSettings.fileName;
            var filePath = Path.Combine(workingDirectoryPath, ret.fileName);

            using (Bitmap sourceBitmap = new Bitmap(Image.FromFile(filePath)))
            {
                ret.width = sourceBitmap.Width;
                ret.height = sourceBitmap.Height;
                ret.pixels = sourceBitmap.ToByteArray();
            }

            ret.alphaInfo = (sourceImageSettings.alpha != null)
                ? new AlphaInfo(sourceImageSettings.alpha)
                : null;
            ret.isShadowLayer = sourceImageSettings.isShadow;
            ret.maskedPixelsInfo = (sourceImageSettings.regionMask != null)
                ? PixelUtil.CreateMaskedPixelsInfo(ret.pixels, ret.width, ret.height, sourceImageSettings.regionMask)
                : null;

            return ret;
        }

        public IEnumerable<string> GetFileNames()
        {
            return sourceImages.Select(img => img.fileName);
        }

        public int GetMaxWidth() { return sourceImages.Select(img => img.width).Max(); }
        public int GetMaxHeight() { return sourceImages.Select(img => img.height).Max(); }
    }
}
