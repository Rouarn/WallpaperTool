using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    internal enum StatusKind
    {
        Ready,
        Working,
        Success,
        Failure
    }

    /// <summary>状态条上的小圆点，颜色只表达四种语义状态。</summary>
    internal sealed class StatusDot : Control
    {
        private StatusKind _kind = StatusKind.Ready;

        public StatusDot()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Size = new Size(Ui.S(8), Ui.S(8));
        }

        public StatusKind Kind
        {
            get { return _kind; }
            set
            {
                if (_kind == value)
                {
                    return;
                }
                _kind = value;
                Invalidate();
            }
        }

        public Color CurrentColor
        {
            get
            {
                switch (_kind)
                {
                    case StatusKind.Working:
                        return Ui.Palette.Accent;
                    case StatusKind.Success:
                        return Ui.Palette.Success;
                    case StatusKind.Failure:
                        return Ui.Palette.Danger;
                    default:
                        return Ui.Palette.Success;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Painting.Dot(e.Graphics, ClientRectangle, CurrentColor);
        }
    }
}
