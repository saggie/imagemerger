using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageMerger
{
    public static class PixelUtil
    {
        #region consts

        public const byte Gray0 = 0xEB;
        public const byte Gray1 = 0xDC;
        public const byte Gray2 = 0xB4;
        public const byte Gray3 = 0x78;
        public const byte Gray4 = 0x46;
        public const byte Gray5 = 0x28;

        #endregion

        public static byte[] ToByteArray(this Bitmap bitmap)
        {
            var ret = new byte[bitmap.Width * bitmap.Height * 4];

            BitmapData data = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

            Marshal.Copy(data.Scan0, ret, 0, ret.Length);

            bitmap.UnlockBits(data);

            return ret;
        }

        public static Bitmap ToBitmap(this byte[] byteArray, int width, int height)
        {
            Bitmap ret = new Bitmap(width, height);

            BitmapData data = ret.LockBits(
                        new Rectangle(0, 0, ret.Width, ret.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

            Marshal.Copy(byteArray, 0, data.Scan0, byteArray.Length);

            ret.UnlockBits(data);

            return ret;
        }

        public static int GetAddressR(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 0; }
        public static int GetAddressG(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 1; }
        public static int GetAddressB(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 2; }
        public static int GetAddressA(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 3; }

        public static byte[] GetPixel(byte[] sourcePixels, int addrX, int addrY, int width)
        {
            return new byte[]
            {
                sourcePixels[GetAddressR(addrX, addrY, width)],
                sourcePixels[GetAddressG(addrX, addrY, width)],
                sourcePixels[GetAddressB(addrX, addrY, width)],
                sourcePixels[GetAddressA(addrX, addrY, width)]
            };
        }

        internal static byte[] GetShadowedPixel(byte[] pixel)
        {
            if (pixel.IsWhite()) { return GetGray1(); }
            if (pixel.IsGray0()) { return GetGray1(); }
            if (pixel.IsGray1()) { return GetGray2(); }
            if (pixel.IsGray2()) { return GetGray3(); }
            if (pixel.IsGray3()) { return GetGray4(); }
            if (pixel.IsGray4()) { return GetGray5(); }
            if (pixel.IsGray5()) { return GetBlack(); }

            return GetBlack();
        }

        public static bool IsWhite(this byte[] pixel) { return pixel[0] ==  0xFF && pixel[1] ==  0xFF && pixel[2] ==  0xFF; }
        public static bool IsGray0(this byte[] pixel) { return pixel[0] == Gray0 && pixel[1] == Gray0 && pixel[2] == Gray0; }
        public static bool IsGray1(this byte[] pixel) { return pixel[0] == Gray1 && pixel[1] == Gray1 && pixel[2] == Gray1; }
        public static bool IsGray2(this byte[] pixel) { return pixel[0] == Gray2 && pixel[1] == Gray2 && pixel[2] == Gray2; }
        public static bool IsGray3(this byte[] pixel) { return pixel[0] == Gray3 && pixel[1] == Gray3 && pixel[2] == Gray3; }
        public static bool IsGray4(this byte[] pixel) { return pixel[0] == Gray4 && pixel[1] == Gray4 && pixel[2] == Gray4; }
        public static bool IsGray5(this byte[] pixel) { return pixel[0] == Gray5 && pixel[1] == Gray5 && pixel[2] == Gray5; }

        private static byte[] GetGray0() { return new byte[] { Gray0, Gray0, Gray0, 0xFF }; }
        private static byte[] GetGray1() { return new byte[] { Gray1, Gray1, Gray1, 0xFF }; }
        private static byte[] GetGray2() { return new byte[] { Gray2, Gray2, Gray2, 0xFF }; }
        private static byte[] GetGray3() { return new byte[] { Gray3, Gray3, Gray3, 0xFF }; }
        private static byte[] GetGray4() { return new byte[] { Gray4, Gray4, Gray4, 0xFF }; }
        private static byte[] GetGray5() { return new byte[] { Gray5, Gray5, Gray5, 0xFF }; }
        private static byte[] GetBlack() { return new byte[] { 0x00, 0x00, 0x00, 0xFF }; }
    }
}
