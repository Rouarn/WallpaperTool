using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>「标题 + 说明 + 开关」的一行，说明文字会随宽度换行。</summary>
    internal sealed class ToggleRow : Panel
    {
        private readonly Label _title;
        private readonly Label _hint;
        private readonly ToggleSwitch _toggle;

        public ToggleRow(string title, string hint, bool value)
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;

            _title = Labels.OnSurface(title, Ui.Fonts.Body, Ui.Palette.TextPrimary);
            _hint = Labels.OnSurface(hint, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _hint.TextAlign = ContentAlignment.TopLeft;
            _toggle = new ToggleSwitch();
            _toggle.Checked = value;
            _toggle.CheckedChanged += delegate { OnCheckedChanged(EventArgs.Empty); };

            Controls.Add(_title);
            Controls.Add(_hint);
            Controls.Add(_toggle);
        }

        public ToggleSwitch Toggle
        {
            get { return _toggle; }
        }

        public bool Checked
        {
            get { return _toggle.Checked; }
            set { _toggle.Checked = value; }
        }

        public event EventHandler CheckedChanged;

        private void OnCheckedChanged(EventArgs e)
        {
            EventHandler handler = CheckedChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void SetHint(string text)
        {
            if (_hint.Text == text)
            {
                return;
            }
            _hint.Text = text;
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            int spacing = Ui.S(Theme.Space.Xs);
            int titleHeight = Ui.Fonts.Body.Height;
            int textWidth = Math.Max(0, ClientSize.Width - _toggle.Width - Ui.S(Theme.Space.M));

            _title.Location = new Point(0, 0);
            _title.Size = new Size(textWidth, titleHeight);

            int hintHeight = 0;
            if (!string.IsNullOrEmpty(_hint.Text))
            {
                hintHeight = Painting.Measure(
                    _hint.Text, Ui.Fonts.Caption, textWidth, Painting.Wrapped).Height;
            }

            int hintTop = titleHeight + spacing;
            _hint.Location = new Point(0, hintTop);
            _hint.Size = new Size(textWidth, hintHeight);

            int contentHeight = titleHeight + (hintHeight > 0 ? spacing + hintHeight : 0);
            int height = Math.Max(contentHeight, _toggle.Height);

            _toggle.Location = new Point(
                ClientSize.Width - _toggle.Width, Math.Max(0, (height - _toggle.Height) / 2));

            if (Height != height)
            {
                Height = height;
            }
        }
    }
}
