using WallpaperTool;

namespace WallpaperTool.Core
{
    /// <summary>应用元信息。版本号同时出现在状态栏与日志里。</summary>
    internal static class AppInfo
    {
        public const string Version = "1.1.0";

        public static string VersionText
        {
            get { return Strings.Format(Strings.AboutVersion, Version); }
        }
    }
}
