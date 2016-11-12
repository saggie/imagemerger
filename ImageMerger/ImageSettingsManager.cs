using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ImageMerger
{
    public class ImageSettingsManager
    {
        public static ImageSettings ReadSettings(string settingFilePath)
        {
            string fileContent = null;
            using (StreamReader sr = File.OpenText(settingFilePath))
            {
                fileContent = sr.ReadToEnd();
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ImageSettings));
            MemoryStream streamedContent = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            return (ImageSettings)serializer.ReadObject(streamedContent);
        }
    }
}
