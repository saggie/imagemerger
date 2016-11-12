using System.Drawing;
using System.Drawing.Imaging;

namespace ImageMerger
{
    public class SourceImage
    {
        public Color[] pixels;
        public int width;
        public int height;
        public ImageFormat imageFormat;
        public int margin;
        public float alpha;
        public bool isShadow;
    }
}
