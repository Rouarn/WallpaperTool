using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>预览缩略图：圆角裁切、等比填充；没有图片时显示占位星标。</summary>
    internal sealed class Thumbnail : Control
    {
        private Bitmap _image;

        public Thumbnail()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Size = new Size(Ui.S(220), Ui.S(124));
        }

        public void Show(Bitmap image)
        {
            if (_image != null)
            {
                _image.Dispose();
            }
            _image = image;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Control);

            using (GraphicsPath path = Painting.Rounded(rect, radius))
            {
                g.SetClip(path);

                if (_image != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    float scale = Math.Max(Width / (float)_image.Width, Height / (float)_image.Height);
                    float drawWidth = _image.Width * scale;
                    float drawHeight = _image.Height * scale;
                    g.DrawImage(_image, new RectangleF(
                        (Width - drawWidth) / 2f, (Height - drawHeight) / 2f, drawWidth, drawHeight));
                }
                else
                {
                    using (SolidBrush brush = new SolidBrush(Ui.Palette.SurfaceSubtle))
                    {
                        g.FillRectangle(brush, ClientRectangle);
                    }
                }

                g.ResetClip();
            }

            if (_image == null)
            {
                float size = Ui.Sf(28);
                RectangleF star = new RectangleF(
                    (Width - size) / 2f, (Height - size) / 2f, size, size);
                DrawStar(g, star, Ui.Palette.TextTertiary);
            }

            Painting.DrawRounded(g, rect, radius, Ui.Palette.Stroke, Ui.Sf(Theme.Size.BorderWidth));
        }

        private static void DrawStar(Graphics g, RectangleF bounds, Color color)
        {
            float cx = bounds.X + bounds.Width / 2f;
            float cy = bounds.Y + bounds.Height / 2f;
            float outer = bounds.Width / 2f;
            float inner = outer * 0.22f;

            PointF[] points = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double angle = -Math.PI / 2.0 + Math.PI / 4.0 * i;
                float radius = (i % 2 == 0) ? outer : inner;
                points[i] = new PointF(
                    cx + (float)Math.Cos(angle) * radius,
                    cy + (float)Math.Sin(angle) * radius);
            }

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, points);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _image != null)
            {
                _image.Dispose();
                _image = null;
            }
            base.Dispose(disposing);
        }
    }
}
