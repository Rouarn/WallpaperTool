using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>日志面板：圆角弱表面容器 + 无边框只读多行文本框。</summary>
    internal sealed class LogPanel : Panel
    {
        private readonly TextBox _view;

        public LogPanel()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Height = Ui.S(Theme.Size.LogPanelHeight);

            _view = new TextBox();
            _view.Multiline = true;
            _view.ReadOnly = true;
            _view.ScrollBars = ScrollBars.Vertical;
            _view.BorderStyle = BorderStyle.None;
            _view.BackColor = Ui.Palette.SurfaceSubtle;
            _view.ForeColor = Ui.Palette.TextSecondary;
            _view.Font = Ui.Fonts.Mono;
            _view.WordWrap = false;
            _view.ShortcutsEnabled = true;
            Controls.Add(_view);
        }

        public string Value
        {
            get { return _view.Text; }
            set
            {
                _view.Text = value ?? string.Empty;
                _view.SelectionStart = _view.TextLength;
                _view.ScrollToCaret();
            }
        }

        public TextBox View
        {
            get { return _view; }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_view == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.M);
            _view.Location = new Point(pad, pad);
            _view.Size = new Size(
                Math.Max(0, ClientSize.Width - pad * 2),
                Math.Max(0, ClientSize.Height - pad * 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Control);

            Painting.FillRounded(g, rect, radius, Ui.Palette.SurfaceSubtle);
            Painting.DrawRounded(g, rect, radius, Ui.Palette.Stroke, Ui.Sf(Theme.Size.BorderWidth));
        }
    }
}
