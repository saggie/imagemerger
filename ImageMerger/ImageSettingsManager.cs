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
            return (ImageSettings)serializer.ReadObject(streamedContent);
        }

        private void ValidateFile(string settingFilePath)
        {
            if (Path.GetExtension(settingFilePath).ToLower() != ".json")
            {
                throw new Exception(); // FIXME
            }

            return; // TODO add more strict validation
        }
    }
}
