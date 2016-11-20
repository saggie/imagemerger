using System.Runtime.InteropServices;
using System.Text;

namespace ImageMerger
{
    public class IniFileManager
    {
        [DllImport("KERNEL32.DLL")]
        public static extern uint GetPrivateProfileString(string lpAppName,
                                                          string lpKeyName,
                                                          string lpDefault,
                                                          StringBuilder lpReturnedString,
                                                          uint nSize,
                                                          string lpFileName);

        [DllImport("KERNEL32.DLL")]
        public static extern uint WritePrivateProfileString(string lpAppName,
                                                            string lpKeyName,
                                                            string lpString,
                                                            string lpFileName);

        public static string ReadFromFile(string iniFilePath, string appName, string key)
        {
            StringBuilder sb = new StringBuilder(1024);

            GetPrivateProfileString(appName, key, null, sb, (uint)sb.Capacity, iniFilePath);

            return sb.ToString();
        }

        public static void WriteToFile(string iniFilePath, string appName, string key, string value)
        {
            WritePrivateProfileString(appName, key, value, iniFilePath);
        }
    }
}
