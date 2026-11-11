using System;
using System.Drawing;
using System.IO;

namespace WallpaperTool.Core
{
    /// <summary>按文件头识别真实图片格式，不信任 URL 扩展名。</summary>
    internal static class ImageSniffer
    {
        /// <summary>返回含点扩展名；无法识别返回 null。</summary>
        public static string DetectExtension(string path)
        {
            byte[] head = new byte[12];
            int read;
            using (FileStream stream = File.OpenRead(path))
            {
                read = stream.Read(head, 0, head.Length);
            }

            if (read >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
            {
                return ".png";
            }
            if (read >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
            {
                return ".jpg";
            }
            if (read >= 6 && head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46)
            {
                return ".gif";
            }
            if (read >= 2 && head[0] == 0x42 && head[1] == 0x4D)
            {
                return ".bmp";
            }
            if (read >= 12 && head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46
                && head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50)
            {
                return ".webp";
            }
            return null;
        }

        /// <summary>读取像素尺寸；文件损坏或格式不支持时返回 false。</summary>
        public static bool TryReadDimensions(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                using (FileStream stream = File.OpenRead(path))
                using (Image image = Image.FromStream(stream, false, false))
                {
                    width = image.Width;
                    height = image.Height;
                }
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
