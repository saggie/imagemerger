using ImageMerger;
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

        private IList<string> supportedImageFormats = new List<string> { "bmp", "png", "gif", "jpg", "jpeg" };

        private string workingDirectoryPath;

        internal void refreshSourceImages(IList<SourceImageSettings> sourceImageSettingsList,
                                          string workingDirectoryPath,
                                          string id = null)
        {
            this.workingDirectoryPath = workingDirectoryPath;

            sourceImages.Clear();

            foreach (var eachImageSettings in sourceImageSettingsList)
            {
                var eachFileName = eachImageSettings.fileName;

                // replace "<ID>"
                if (eachFileName.ContainsIgnoreCase("<id>"))
                {
                    eachFileName = eachFileName.ToLower().Replace("<id>", id ?? "");
                }

                // replace "<EXT>"
                if (eachFileName.ContainsIgnoreCase(".<ext>"))
                {
                    var fileNameSearchString = eachFileName.ToLower().Replace(".<ext>", ".*");
                    fileNameSearchString = fileNameSearchString.ToLower().Replace("<ver>", "*");

                    bool isFileFound = false;
                    foreach (var candidateFilePath in Directory.GetFiles(workingDirectoryPath, fileNameSearchString))
                    {
                        foreach (var eachImageFormat in supportedImageFormats)
                        {
                            if (candidateFilePath.EndsWith(eachImageFormat))
                            {
                                eachFileName = eachFileName.ToLower().Replace("<ext>", eachImageFormat);
                                isFileFound = true;
                                break;
                            }
                        }

                        if (isFileFound) { break; }
                    }
                    if (!isFileFound) { throw new InvalidSettingsFileException(); }
                }

                // replace "<VER>"
                if (eachFileName.ContainsIgnoreCase("<ver>"))
                {
                    var fileNameSplitByVersion = eachFileName.ToLower().Split(new string[] { "<ver>" }, StringSplitOptions.None);
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

                    // choose latest one
                    eachFileName = fileNameFormerPart + versionList.Max() + fileNameLatterPart;
                }

                sourceImages.Add(LoadSourceImage(eachImageSettings, eachFileName));
            }
        }

        private SourceImageInfo LoadSourceImage(SourceImageSettings sourceImageSettings, string fileName)
        {
            SourceImageInfo ret = new SourceImageInfo();

            ret.fileName = fileName;
            var filePath = Path.Combine(workingDirectoryPath, ret.fileName);

            using (Image image = Image.FromFile(filePath))
            using (Bitmap sourceBitmap = new Bitmap(image)) // TODO handle the case when the file is missing
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
            ret.shadowColor = (sourceImageSettings.shadowColor != null)
                ? sourceImageSettings.shadowColor.ToPixelData()
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
