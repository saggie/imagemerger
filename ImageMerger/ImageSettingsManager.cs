using ImageMerger.Exceptions;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ImageMerger
{
    public class ImageSettingsManager
    {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ImageSettings));

        public ImageSettings ReadSettings(string settingFilePath)
        {
            ValidateFile(settingFilePath);

            string fileContent = null;
            using (StreamReader sr = File.OpenText(settingFilePath))
            {
                fileContent = sr.ReadToEnd();
            }

            var streamedContent = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            ImageSettings ret = null;
            try
            {
                ret = (ImageSettings)serializer.ReadObject(streamedContent);
            }
            catch (Exception)
            {
                throw new InvalidSettingsFileException();
            }

            // Completing the ID (if not provided) from the settings file name
            if (ret.id == null)
            {
                ret.id = Path.GetFileNameWithoutExtension(settingFilePath);
            }

            return ret;
        }

        private void ValidateFile(string settingFilePath)
        {
            if (Path.GetExtension(settingFilePath).ToLower() != ".json")
            {
                throw new InvalidSettingsFileException();
            }

            return;
        }
    }
}
