using System;
using System.Runtime.InteropServices;

namespace WallpaperTool.Core
{
    /// <summary>仅在用户显式开启开关时调用，默认不会执行。</summary>
    internal static class WallpaperSetter
    {
        private const int SpiSetDeskWallpaper = 20;
        private const int SpifUpdateIniFile = 0x01;
        private const int SpifSendChange = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(int action, int param, string value, int winIni);

        public static bool TryApply(string path)
        {
            try
            {
                int result = SystemParametersInfo(
                    SpiSetDeskWallpaper, 0, path, SpifUpdateIniFile | SpifSendChange);
                return result != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
