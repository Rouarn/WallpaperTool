using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WallpaperTool.Views
{
    /// <summary>圆角路径、描边与文本绘制的公共实现，避免每个控件各写一套。</summary>
    internal static class Painting
    {
        public const TextFormatFlags SingleLineLeft =
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        public const TextFormatFlags SingleLineCentered =
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        public const TextFormatFlags Wrapped =
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak
            | TextFormatFlags.NoPadding;

        public static GraphicsPath Rounded(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0.5f || rect.Width <= 0f || rect.Height <= 0f)
            {
                path.AddRectangle(rect);
                return path;
            }

            float diameter = radius * 2f;
            if (diameter > rect.Width)
            {
                diameter = rect.Width;
            }
            if (diameter > rect.Height)
            {
                diameter = rect.Height;
            }

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180f, 90f);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270f, 90f);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, RectangleF rect, float radius, Color color)
        {
            using (GraphicsPath path = Rounded(rect, radius))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRounded(Graphics g, RectangleF rect, float radius, Color color, float width)
        {
            using (GraphicsPath path = Rounded(rect, radius))
            using (Pen pen = new Pen(color, width))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawPath(pen, path);
            }
        }

        public static void DashedRounded(Graphics g, RectangleF rect, float radius, Color color, float width)
        {
            using (GraphicsPath path = Rounded(rect, radius))
            using (Pen pen = new Pen(color, width))
            {
                pen.DashStyle = DashStyle.Dash;
                pen.DashPattern = new float[] { 4f, 3f };
                pen.Alignment = PenAlignment.Inset;
                g.DrawPath(pen, path);
            }
        }

        public static void Text(Graphics g, string text, Font font, Color color, Rectangle bounds, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, bounds, color, flags);
        }

        public static Size Measure(string text, Font font, int width, TextFormatFlags flags)
        {
            return TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), flags);
        }

        /// <summary>垂直居中绘制一个 8px 的状态圆点。</summary>
        public static void Dot(Graphics g, Rectangle bounds, Color color)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, bounds);
            }
        }

        /// <summary>把前景色按比例混到背景色上，用于禁用态与弱填充，避免写死透明度颜色。</summary>
        public static Color Blend(Color foreground, Color background, float ratio)
        {
            if (ratio <= 0f)
            {
                return background;
            }
            if (ratio >= 1f)
            {
                return foreground;
            }

            int red = (int)Math.Round(foreground.R * ratio + background.R * (1f - ratio));
            int green = (int)Math.Round(foreground.G * ratio + background.G * (1f - ratio));
            int blue = (int)Math.Round(foreground.B * ratio + background.B * (1f - ratio));
            return Color.FromArgb(255, red, green, blue);
        }
    }
}
