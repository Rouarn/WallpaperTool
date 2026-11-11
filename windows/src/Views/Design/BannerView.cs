using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>可关闭的错误横幅：结论 + 原因 + 「重试」，不使用系统消息框。</summary>
    internal sealed class BannerView : Panel
    {
        private readonly string _title;
        private readonly string _message;
        private readonly ChromeButton _retry;
        private readonly GlyphButton _close;

        public BannerView(string title, string message, bool canRetry)
        {
            _title = title;
            _message = message;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Canvas;

            if (canRetry)
            {
                _retry = new ChromeButton(Strings.ActionRetry, ButtonKind.Text);
                _retry.Click += delegate { OnRetry(EventArgs.Empty); };
                Controls.Add(_retry);
            }

            _close = new GlyphButton();
            _close.Click += delegate { OnDismissed(EventArgs.Empty); };
            Controls.Add(_close);
        }

        public event EventHandler Retry;

        public event EventHandler Dismissed;

        private void OnRetry(EventArgs e)
        {
            EventHandler handler = Retry;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnDismissed(EventArgs e)
        {
            EventHandler handler = Dismissed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_close == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.M);
            int icon = Ui.S(Theme.Size.Icon);
            int gap = Ui.S(Theme.Space.S);
            int textX = pad + icon + gap;
            int closeSize = Ui.S(22);
            int textWidth = Math.Max(0, ClientSize.Width - textX - pad - closeSize - gap);

            int titleHeight = Ui.Fonts.BodyStrong.Height;
            int messageHeight = Painting.Measure(
                _message, Ui.Fonts.Caption, textWidth, Painting.Wrapped).Height;

            int y = pad + titleHeight + Ui.S(Theme.Space.Xs) + messageHeight;

            if (_retry != null)
            {
                y += Ui.S(Theme.Space.Xs);
                _retry.Location = new Point(textX, y);
                y += _retry.Height;
            }

            int desired = y + pad;

            _close.Location = new Point(ClientSize.Width - pad - closeSize, pad - Ui.S(2));
            _close.Size = new Size(closeSize, closeSize);

            if (Height != desired)
            {
                Height = desired;
            }

            _textRect = new Rectangle(textX, pad, textWidth, titleHeight + messageHeight);
            _messageRect = new Rectangle(
                textX, pad + titleHeight + Ui.S(Theme.Space.Xs), textWidth, messageHeight);
        }

        private Rectangle _textRect;
        private Rectangle _messageRect;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pad = Ui.S(Theme.Space.M);
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Card);

            Painting.FillRounded(g, rect, radius, Ui.Palette.DangerSubtle);
            Painting.DrawRounded(g, rect, radius, Ui.Palette.Stroke, Ui.Sf(Theme.Size.BorderWidth));

            DrawWarningIcon(g, new Rectangle(pad, pad, Ui.S(Theme.Size.Icon), Ui.S(Theme.Size.Icon)));

            int titleHeight = Ui.Fonts.BodyStrong.Height;
            Painting.Text(g, _title, Ui.Fonts.BodyStrong, Ui.Palette.TextPrimary,
                new Rectangle(_textRect.X, _textRect.Y, _textRect.Width, titleHeight),
                Painting.SingleLineLeft);
            Painting.Text(g, _message, Ui.Fonts.Caption, Ui.Palette.TextSecondary,
                _messageRect, Painting.Wrapped);
        }

        private static void DrawWarningIcon(Graphics g, Rectangle bounds)
        {
            float w = bounds.Width;
            float h = bounds.Height;
            PointF[] triangle = new PointF[]
            {
                new PointF(bounds.X + w / 2f, bounds.Y + 1f),
                new PointF(bounds.X + w - 1f, bounds.Y + h - 1f),
                new PointF(bounds.X + 1f, bounds.Y + h - 1f)
            };

            using (SolidBrush brush = new SolidBrush(Ui.Palette.Danger))
            {
                g.FillPolygon(brush, triangle);
            }

            float barWidth = Math.Max(1.5f, w * 0.09f);
            float barTop = bounds.Y + h * 0.36f;
            float barHeight = h * 0.34f;
            using (SolidBrush brush = new SolidBrush(Ui.Palette.Surface))
            {
                g.FillRectangle(brush, bounds.X + w / 2f - barWidth / 2f, barTop, barWidth, barHeight);
                g.FillEllipse(brush, bounds.X + w / 2f - barWidth / 2f,
                    barTop + barHeight + w * 0.08f, barWidth, barWidth);
            }
        }
    }

    /// <summary>横幅右上角的关闭 ×。</summary>
    internal sealed class GlyphButton : Control
    {
        private bool _hover;

        public GlyphButton()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable, true);
            BackColor = Ui.Palette.Canvas;
            Cursor = Cursors.Hand;
            TabStop = false;
            Size = new Size(Ui.S(22), Ui.S(22));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)
            {
                Painting.FillRounded(g, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f),
                    Ui.Sf(Theme.Radius.Control), Ui.Palette.SurfaceSubtle);
            }

            Color color = _hover ? Ui.Palette.TextPrimary : Ui.Palette.TextTertiary;
            float inset = Width * 0.34f;
            using (Pen pen = new Pen(color, Math.Max(1.3f, Ui.Sf(1.2))))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, inset, inset, Width - inset, Height - inset);
                g.DrawLine(pen, Width - inset, inset, inset, Height - inset);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
            {
                OnClick(EventArgs.Empty);
            }
            base.OnMouseUp(e);
        }
    }
}
