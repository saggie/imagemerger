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
        public IList<MaskInfo> maskInfoList;
        public float alphaValue;
        public bool isShadow;
    }
}
