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
        public float alphaValue;
        public bool isShadowLayer;
        public ColorReplacementInfo colorReplacementInfo;
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
