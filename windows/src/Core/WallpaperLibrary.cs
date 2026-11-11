using System;
using System.Globalization;
using System.IO;

namespace WallpaperTool.Core
{
    /// <summary>保存目录里最近一次壁纸的查找与预览信息读取。</summary>
    internal static class WallpaperLibrary
    {
        private static readonly string[] Extensions = new string[]
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
        };

        /// <summary>返回保存目录里最新的 wallpaper* 文件，没有则返回 null。</summary>
        public static string FindLatest(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    return null;
                }

                string latest = null;
                DateTime latestTime = DateTime.MinValue;

                foreach (string extension in Extensions)
                {
                    string[] files;
                    try
                    {
                        files = Directory.GetFiles(dir, "wallpaper*" + extension);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string file in files)
                    {
                        DateTime time = File.GetLastWriteTime(file);
                        if (time > latestTime)
                        {
                            latestTime = time;
                            latest = file;
                        }
                    }
                }

                return latest;
            }
            catch
            {
                return null;
            }
        }

        public static WallpaperPreview Read(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            WallpaperPreview preview = new WallpaperPreview();
            preview.FullPath = path;
            preview.FileName = Path.GetFileName(path);

            try
            {
                FileInfo info = new FileInfo(path);
                preview.ByteSize = info.Length;
                preview.ModifiedAt = info.LastWriteTime;
            }
            catch
            {
                preview.ModifiedAt = DateTime.Now;
            }

            int width;
            int height;
            if (ImageSniffer.TryReadDimensions(path, out width, out height))
            {
                preview.Width = width;
                preview.Height = height;
            }

            return preview;
        }

        /// <summary>把路径缩成 ~ 开头的形式，界面更短，tooltip 再给完整路径。</summary>
        public static string Shorten(string path, string home)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(home))
            {
                return path;
            }
            if (path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
            {
                string rest = path.Substring(home.Length).TrimStart('\\', '/');
                return rest.Length == 0 ? "~" : "~\\" + rest;
            }
            return path;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 KB";
            }
            if (bytes < 1024L * 1024L)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0:0} KB", bytes / 1024.0);
            }
            return string.Format(CultureInfo.CurrentCulture, "{0:0.0} MB", bytes / (1024.0 * 1024.0));
        }
    }

    internal sealed class WallpaperPreview
    {
        public string FileName;
        public string FullPath;
        public int Width;
        public int Height;
        public long ByteSize;
        public DateTime ModifiedAt;
    }
}
