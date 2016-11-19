using System.Collections.Generic;
using System.Drawing.Imaging;

namespace ImageMerger
{
    public class SourceImageInfo
    {
        public string fileName;
        public byte[] pixels;
        public int width;
        public int height;
        public bool[] maskedPixelsInfo;
        public AlphaInfo alphaInfo;
        public bool isShadowLayer;
        public ColorReplacementInfo colorReplacementInfo;
    }

    public class AlphaInfo
    {
        public float value;
        public IList<byte[]> ignoreList;
        public bool excludeMask;

        public AlphaInfo(AlphaSettings alphaSettings)
        {
            value = alphaSettings.value;
            ignoreList = new List<byte[]>();
            if (alphaSettings.ignoreList != null)
            {
                foreach (var ignoringColor in alphaSettings.ignoreList)
                {
                    ignoreList.Add(ignoringColor.ToPixelData());
                }
            }
            excludeMask = alphaSettings.excludeMask;
        }
    }

    public class ColorReplacementInfo
    {
        public byte[] from;
        public byte[] to;

        public ColorReplacementInfo(ColorReplacementSettigns colorReplacementSettings)
        {
            from = colorReplacementSettings.from.ToPixelData();
            to = colorReplacementSettings.to.ToPixelData();
        }
    }
}
