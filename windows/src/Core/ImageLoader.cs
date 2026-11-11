using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace WallpaperTool.Core
{
    /// <summary>读取图片并缩放成缩略图，读完立即释放文件句柄，不锁住用户文件。</summary>
    internal static class ImageLoader
    {
        public static Bitmap LoadScaled(string path, int maxWidth, int maxHeight)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            if (maxWidth < 1) maxWidth = 1;
            if (maxHeight < 1) maxHeight = 1;

            try
            {
                using (FileStream stream = File.OpenRead(path))
                using (Image source = Image.FromStream(stream))
                {
                    float scale = Math.Min(
                        maxWidth / (float)source.Width,
                        maxHeight / (float)source.Height);
                    if (scale > 1f)
                    {
                        scale = 1f;
                    }

                    int width = Math.Max(1, (int)Math.Round(source.Width * scale));
                    int height = Math.Max(1, (int)Math.Round(source.Height * scale));

                    Bitmap bitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(source, 0, 0, width, height);
                    }
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
