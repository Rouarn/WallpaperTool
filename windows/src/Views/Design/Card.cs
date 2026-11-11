using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>圆角卡片容器：标题 + 内容栈，高度随内容自适应。</summary>
    internal class Card : Panel
    {
        private readonly string _title;
        private readonly StackPanel _content;

        public Card(string title, int spacing)
        {
            _title = title;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Canvas;

            _content = new StackPanel();
            _content.BackColor = Ui.Palette.Surface;
            _content.Spacing = spacing;
            Controls.Add(_content);
        }

        public StackPanel Content
        {
            get { return _content; }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_content == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.L);
            int titleHeight = Ui.Fonts.SectionTitle.Height;
            int gap = Ui.S(Theme.Space.M);
            int innerWidth = Math.Max(0, ClientSize.Width - pad * 2);

            _content.Location = new Point(pad, pad + titleHeight + gap);
            _content.Width = innerWidth;
            _content.PerformLayout();

            int desired = _content.Bottom + pad;
            if (Height != desired)
            {
                Height = desired;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Card);

            Painting.FillRounded(g, rect, radius, Ui.Palette.Surface);
            Painting.DrawRounded(g, rect, radius, Ui.Palette.Stroke, Ui.Sf(Theme.Size.BorderWidth));

            int pad = Ui.S(Theme.Space.L);
            if (!string.IsNullOrEmpty(_title))
            {
                Painting.Text(g, _title, Ui.Fonts.SectionTitle, Ui.Palette.TextSecondary,
                    new Rectangle(pad, pad, Width - pad * 2, Ui.Fonts.SectionTitle.Height),
                    Painting.SingleLineLeft);
            }
        }
    }
}
