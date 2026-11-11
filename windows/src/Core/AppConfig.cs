using System;
using System.Globalization;
using System.IO;
using System.Text;
using WallpaperTool;

namespace WallpaperTool.Core
{
    /// <summary>用户配置。key=value 文本，缺键回落默认值，未知键忽略，不清空已有设置。</summary>
    internal sealed class AppConfig
    {
        public const string DefaultUrl = "https://picsum.photos/1920/1080";
        public const string DefaultLogFile = "wallpaper_log.txt";

        public string Url = DefaultUrl;
        public string DestDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallpaperTool");
        public string LogFile = DefaultLogFile;
        public bool SetWallpaperAfterDownload;
        public bool KeepHistory;

        /// <summary>上次运行时间，ISO 8601；界面用它显示「已更新 · …」。</summary>
        public string LastRunAt = string.Empty;

        /// <summary>上次运行结果：success / failed / cancelled / 空。</summary>
        public string LastResult = string.Empty;

        /// <summary>本次加载是否缺少旧版字段（用于记录一次「已迁移配置」）。</summary>
        public bool Migrated;

        public static string ConfigDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperTool");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigDir, "config.ini"); }
        }

        public static AppConfig Load()
        {
            AppConfig config = new AppConfig();
            if (!File.Exists(ConfigPath))
            {
                return config;
            }

            bool sawSetWallpaper = false;
            bool sawKeepHistory = false;

            try
            {
                foreach (string raw in File.ReadAllLines(ConfigPath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    int split = line.IndexOf('=');
                    if (split <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, split).Trim();
                    string value = line.Substring(split + 1).Trim();

                    switch (key)
                    {
                        case "url":
                            if (value.Length > 0) config.Url = value;
                            break;
                        case "destDir":
                            if (value.Length > 0) config.DestDir = value;
                            break;
                        case "logFile":
                            if (value.Length > 0) config.LogFile = value;
                            break;
                        case "setWallpaperAfterDownload":
                            sawSetWallpaper = true;
                            config.SetWallpaperAfterDownload = ParseBool(value);
                            break;
                        case "keepHistory":
                            sawKeepHistory = true;
                            config.KeepHistory = ParseBool(value);
                            break;
                        case "lastRunAt":
                            config.LastRunAt = value;
                            break;
                        case "lastResult":
                            config.LastResult = value;
                            break;
                    }
                }
            }
            catch
            {
                // 读失败时保持默认值，不阻断启动
            }

            config.Migrated = !sawSetWallpaper || !sawKeepHistory;
            return config;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# WallpaperTool 配置，缺失的键会回落默认值");
                builder.AppendLine("url=" + Url);
                builder.AppendLine("destDir=" + DestDir);
                builder.AppendLine("logFile=" + LogFile);
                builder.AppendLine("setWallpaperAfterDownload=" + (SetWallpaperAfterDownload ? "true" : "false"));
                builder.AppendLine("keepHistory=" + (KeepHistory ? "true" : "false"));
                builder.AppendLine("lastRunAt=" + LastRunAt);
                builder.AppendLine("lastResult=" + LastResult);
                File.WriteAllText(ConfigPath, builder.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                // 配置写入失败不影响当前会话
            }
        }

        /// <summary>记录一次运行结果，供下次启动时恢复状态栏。</summary>
        public void MarkRun(string result)
        {
            LastRunAt = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            LastResult = result;
        }

        public static bool TryParseTime(string iso, out DateTime value)
        {
            return DateTime.TryParseExact(
                iso, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out value);
        }

        public string ResolveLogPath()
        {
            if (Path.IsPathRooted(LogFile))
            {
                return LogFile;
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFile);
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
