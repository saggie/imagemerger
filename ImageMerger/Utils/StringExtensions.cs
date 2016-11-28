using System;

namespace ImageMerger
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string str1, string str2)
        {
            return str1.IndexOf(str2, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
