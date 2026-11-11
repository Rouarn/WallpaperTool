using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>1px 分隔线，用于卡片内分组。</summary>
    internal sealed class Separator : Control
    {
        public Separator()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Height = Math.Max(1, Ui.S(Theme.Size.BorderWidth));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Pen pen = new Pen(Ui.Palette.Stroke, 1f))
            {
                float y = Height / 2f;
                e.Graphics.DrawLine(pen, 0f, y, Width, y);
            }
        }
    }

    /// <summary>行内弹簧：吃掉剩余宽度，把后面的控件推到右侧。</summary>
    internal sealed class Spacer : Control
    {
        public Spacer()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Ui.Palette.Surface;
            Height = 1;
            Width = 0;
            TabStop = false;
        }
    }
}
