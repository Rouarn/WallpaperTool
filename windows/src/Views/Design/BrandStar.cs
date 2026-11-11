using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>品牌四芒星标记，下载中通过 Angle 旋转，对应 macOS 端的 star-spin 动效。</summary>
    internal sealed class BrandStar : Control
    {
        private float _angle;
        private Color _tint = Color.Empty;

        public BrandStar()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Size = new Size(Ui.S(Theme.Size.IconLarge), Ui.S(Theme.Size.IconLarge));
        }

        public float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                Invalidate();
            }
        }

        public Color Tint
        {
            get { return _tint; }
            set
            {
                _tint = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = Width / 2f;
            float cy = Height / 2f;
            float outer = Math.Min(Width, Height) / 2f - 1f;
            float inner = outer * 0.22f;
            double baseAngle = -Math.PI / 2.0 + _angle * Math.PI / 180.0;

            PointF[] points = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double angle = baseAngle + Math.PI / 4.0 * i;
                float radius = (i % 2 == 0) ? outer : inner;
                points[i] = new PointF(
                    cx + (float)Math.Cos(angle) * radius,
                    cy + (float)Math.Sin(angle) * radius);
            }

            using (SolidBrush brush = new SolidBrush(_tint.IsEmpty ? Ui.Palette.Accent : _tint))
            {
                g.FillPolygon(brush, points);
            }
        }
    }
}
