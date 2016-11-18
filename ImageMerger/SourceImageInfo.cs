using System.Collections.Generic;
using System.Drawing.Imaging;

namespace ImageMerger
{
    public class SourceImageInfo
    {
        public byte[] pixels;
        public int width;
        public int height;
        public ImageFormat imageFormat;
        public bool[] maskedPixelsInfo;
        public AlphaInfo alphaInfo;
        public bool isShadowLayer;
        public ColorReplacementInfo colorReplacementInfo;
    }

    public class AlphaInfo
    {
        public float value;
        public IList<byte[]> ignoreList;

        public AlphaInfo(AlphaSettings alphaSettings)
        {
            value = alphaSettings.value;
            ignoreList = new List<byte[]>();
            foreach(var ignoringColor in alphaSettings.ignoreList)
            {
                ignoreList.Add(ignoringColor.ToPixelData());
            }
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
