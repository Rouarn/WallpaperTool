using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;
using WallpaperTool.Core;

namespace WallpaperTool.Views
{
    /// <summary>底部状态条：状态圆点 + 当前状态 + 版本号，不随内容滚动。</summary>
    internal sealed class StatusBar : Panel
    {
        private readonly StatusDot _dot;
        private readonly Label _text;
        private readonly Label _version;

        public StatusBar()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Bottom;
            BackColor = Ui.Palette.Surface;
            Height = Ui.S(Theme.Size.StatusBarHeight);

            _dot = new StatusDot();
            _text = Labels.OnSurface(Strings.StatusReady, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _version = Labels.OnSurface(AppInfo.VersionText, Ui.Fonts.Caption, Ui.Palette.TextTertiary);
            _version.TextAlign = ContentAlignment.MiddleRight;

            Controls.Add(_dot);
            Controls.Add(_text);
            Controls.Add(_version);
        }

        public void SetStatus(StatusKind kind, string text)
        {
            _dot.Kind = kind;
            _text.Text = text ?? string.Empty;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_dot == null || _text == null || _version == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.Xl);
            int dotSize = Ui.S(8);

            _dot.Size = new Size(dotSize, dotSize);
            _dot.Location = new Point(pad, (Height - dotSize) / 2);

            int versionWidth = Painting.Measure(
                AppInfo.VersionText, Ui.Fonts.Caption, 600, Painting.SingleLineLeft).Width + 2;
            _version.Location = new Point(ClientSize.Width - pad - versionWidth, 0);
            _version.Size = new Size(versionWidth, Height);

            int textX = _dot.Right + Ui.S(Theme.Space.S);
            _text.Location = new Point(textX, 0);
            _text.Size = new Size(
                Math.Max(0, _version.Left - Ui.S(Theme.Space.S) - textX), Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Pen pen = new Pen(Ui.Palette.Stroke, 1f))
            {
                e.Graphics.DrawLine(pen, 0f, 0.5f, Width, 0.5f);
            }
        }
    }
}
