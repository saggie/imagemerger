using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageMerger
{
    public static class PixelUtil
    {
        #region Constants

        private const byte Gray0 = 0xEB;
        private const byte Gray1 = 0xDC;
        private const byte Gray2 = 0xB4;
        private const byte Gray3 = 0x78;
        private const byte Gray4 = 0x46;
        private const byte Gray5 = 0x28;

        private const byte CyanR = 0x00;
        private const byte CyanG = 0xFF;
        private const byte CyanB = 0xFF;

        private const byte YellowR = 0xFF;
        private const byte YellowG = 0xFF;
        private const byte YellowB = 0x00;

        #endregion

        #region Converter

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

        #endregion

        public static int GetAddressB(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 0; }
        public static int GetAddressG(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 1; }
        public static int GetAddressR(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 2; }
        public static int GetAddressA(int addrX, int addrY, int width) { return (addrX + addrY * width) * 4 + 3; }

        public static byte[] GetPixelAt(this byte[] sourcePixels, int addrX, int addrY, int width)
        {
            return new byte[]
            {
                sourcePixels[GetAddressB(addrX, addrY, width)],
                sourcePixels[GetAddressG(addrX, addrY, width)],
                sourcePixels[GetAddressR(addrX, addrY, width)],
                sourcePixels[GetAddressA(addrX, addrY, width)]
            };
        }

        public static byte[] BlendWith(this byte[] pixel1, byte[] pixel2, float alphaValue)
        {
            if (alphaValue >= 1) { return pixel1; }
            if (alphaValue <= 0) { return pixel2; }

            return new byte[]
            {
                (byte) (pixel1[0] * alphaValue + pixel2[0] * (1 - alphaValue)),
                (byte) (pixel1[1] * alphaValue + pixel2[1] * (1 - alphaValue)),
                (byte) (pixel1[2] * alphaValue + pixel2[2] * (1 - alphaValue)),
                0xFF
            };
        }

        public static byte[] GetShadowedPixel(byte[] pixel)
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

        public static bool[] CreateMaskedPixelsInfo(byte[] sourcePixels, int width, int height, IList<MaskInfo> maskInfoList)
        {
            var ret = new bool[width * height];

            for (var yi = 0; yi < height; yi++)
            {
                for (var xi = 0; xi < width; xi++)
                {
                    byte[] targetPixel = sourcePixels.GetPixelAt(xi, yi, width);
                    if (targetPixel.IsWhite() ||
                        targetPixel.IsNotMaskTargetColor(maskInfoList.Select(m => m.targetColor))) { continue; }

                    var margin = GetMargin(targetPixel, maskInfoList);

                    // move window from upper-left to lower-right...
                    //   <-----margin(2)---->
                    //     0   1   2   3   4
                    // 0 [00][01][02][03][04] ^
                    // 1 [05][06][07][08][09] |
                    // 2 [10][11][12][13][14] margin(2)
                    // 3 [15][16][17][18][19] |
                    // 4 [20][21][22][23][24] v
                    for (var yj = (yi - margin > 0) ? (yi - margin) : 0; yj <= (yi + margin); yj++)
                    {
                        if (yj >= height) { break; } // bottom-edge case

                        for (var xj = (xi - margin > 0) ? (xi - margin) : 0; xj <= (xi + margin); xj++)
                        {
                            if (xj >= width) { break; } // right-edge case

                            var pixelToMask = sourcePixels.GetPixelAt(xj, yj, width);
                            if (pixelToMask.IsWhite())
                            {
                                ret[xj + yj * width] = true;
                            }
                        }
                    }
                }
            }

            return ret;
        }

        private static byte[] Copy(byte[] source)
        {
            var ret = new byte[source.Length];
            Buffer.BlockCopy(source, 0, ret, 0, source.Length * sizeof(byte));
            return ret;
        }

        private static bool IsNotMaskTargetColor(this byte[] pixel, IEnumerable<string> maskTargetColors)
        {
            return !pixel.IsMaskTargetColor(maskTargetColors);
        }

        private static bool IsMaskTargetColor(this byte[] pixel, IEnumerable<string> maskTargetColors)
        {
            foreach (var eachMaskTargetColor in maskTargetColors)
            {
                if (pixel.IsSameRgb(eachMaskTargetColor.ToPixelData()))
                {
                    return true; // hit (at least 1)
                }
            }
            return false; // not hit at all
        }

        private static int GetMargin(byte[] pixel, IList<MaskInfo> maskInfoList)
        {
            foreach (var eachMaskInfo in maskInfoList)
            {
                if (pixel.IsSameRgb(eachMaskInfo.targetColor.ToPixelData()))
                {
                    return eachMaskInfo.margin;
                }
            }
            return 0;
        }

        private static byte[] ToPixelData(this string colorString)
        {
            var strR = colorString.Substring(colorString.Length - 6, 2);
            var strG = colorString.Substring(colorString.Length - 4, 2);
            var strB = colorString.Substring(colorString.Length - 2, 2);
            return new byte[]
            {
                (byte) int.Parse(strB, System.Globalization.NumberStyles.HexNumber),
                (byte) int.Parse(strG, System.Globalization.NumberStyles.HexNumber),
                (byte) int.Parse(strR, System.Globalization.NumberStyles.HexNumber),
                0xFF
            };
        }

        private static bool IsSameRgb(this byte[] pixel1, byte[] pixel2)
        {
            return pixel1[0] == pixel2[0] && pixel1[1] == pixel2[1] && pixel1[2] == pixel2[2];
        }

        public static bool IsWhite(this byte[] pixel) { return pixel[0] ==  0xFF && pixel[1] ==  0xFF && pixel[2] ==  0xFF; }
        public static bool IsGray0(this byte[] pixel) { return pixel[0] == Gray0 && pixel[1] == Gray0 && pixel[2] == Gray0; }
        public static bool IsGray1(this byte[] pixel) { return pixel[0] == Gray1 && pixel[1] == Gray1 && pixel[2] == Gray1; }
        public static bool IsGray2(this byte[] pixel) { return pixel[0] == Gray2 && pixel[1] == Gray2 && pixel[2] == Gray2; }
        public static bool IsGray3(this byte[] pixel) { return pixel[0] == Gray3 && pixel[1] == Gray3 && pixel[2] == Gray3; }
        public static bool IsGray4(this byte[] pixel) { return pixel[0] == Gray4 && pixel[1] == Gray4 && pixel[2] == Gray4; }
        public static bool IsGray5(this byte[] pixel) { return pixel[0] == Gray5 && pixel[1] == Gray5 && pixel[2] == Gray5; }

        public static bool IsYellow(this byte[] pixel){ return pixel[0] == YellowB && pixel[1] == YellowG && pixel[2] == YellowR; }

        private static byte[] GetGray0() { return new byte[] { Gray0, Gray0, Gray0, 0xFF }; }
        private static byte[] GetGray1() { return new byte[] { Gray1, Gray1, Gray1, 0xFF }; }
        private static byte[] GetGray2() { return new byte[] { Gray2, Gray2, Gray2, 0xFF }; }
        private static byte[] GetGray3() { return new byte[] { Gray3, Gray3, Gray3, 0xFF }; }
        private static byte[] GetGray4() { return new byte[] { Gray4, Gray4, Gray4, 0xFF }; }
        private static byte[] GetGray5() { return new byte[] { Gray5, Gray5, Gray5, 0xFF }; }
        private static byte[] GetBlack() { return new byte[] { 0x00, 0x00, 0x00, 0xFF }; }

        public static byte[] GetCyan() { return new byte[] { CyanB, CyanG, CyanR, 0xFF }; }
    }
}
