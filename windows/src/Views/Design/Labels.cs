using System;
using System.Drawing;
using System.Windows.Forms;

namespace WallpaperTool.Views
{
    /// <summary>标签工厂：统一去掉 AutoSize 抖动，背景色跟随所在表面。</summary>
    internal static class Labels
    {
        public static Label Make(string text, Font font, Color color, Color background)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = font;
            label.ForeColor = color;
            label.BackColor = background;
            label.AutoSize = false;
            label.UseMnemonic = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        public static Label OnSurface(string text, Font font, Color color)
        {
            return Make(text, font, color, Ui.Palette.Surface);
        }

        public static Label OnCanvas(string text, Font font, Color color)
        {
            return Make(text, font, color, Ui.Palette.Canvas);
        }

        /// <summary>按文本实际宽度收紧标签（Row 里子控件不会被拉伸，需要手动量）。</summary>
        public static void Fit(Label label)
        {
            if (label == null)
            {
                return;
            }

            Size measured = Painting.Measure(
                label.Text, label.Font, 4000,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.Left);
            label.Width = measured.Width + 2;
            label.Height = Math.Max(label.Height, label.Font.Height);
        }
    }
}
