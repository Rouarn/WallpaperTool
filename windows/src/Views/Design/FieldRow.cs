using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>「字段说明 + 输入框（+ 可选尾部按钮）」的一行，卡片里复用。</summary>
    internal sealed class FieldRow : Panel
    {
        private readonly Label _caption;
        private readonly TextInput _input;
        private readonly ChromeButton _trailing;

        public FieldRow(string caption, string value)
            : this(caption, value, null)
        {
        }

        public FieldRow(string caption, string value, string trailingText)
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;

            _caption = Labels.OnSurface(caption, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _input = new TextInput(value);
            Controls.Add(_caption);
            Controls.Add(_input);

            if (trailingText != null)
            {
                _trailing = new ChromeButton(trailingText, ButtonKind.Secondary);
                Controls.Add(_trailing);
            }
        }

        public TextInput Input
        {
            get { return _input; }
        }

        public ChromeButton Trailing
        {
            get { return _trailing; }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            int width = ClientSize.Width;
            int captionHeight = Ui.Fonts.Caption.Height;
            int gap = Ui.S(Theme.Space.Xs);

            _caption.Location = new Point(0, 0);
            _caption.Size = new Size(width, captionHeight);

            int top = captionHeight + gap;
            _input.Location = new Point(0, top);

            if (_trailing != null)
            {
                int spacing = Ui.S(Theme.Space.S);
                _input.Width = Math.Max(0, width - _trailing.Width - spacing);
                _trailing.Location = new Point(width - _trailing.Width, top);
            }
            else
            {
                _input.Width = width;
            }

            int desired = top + _input.Height;
            if (Height != desired)
            {
                Height = desired;
            }
        }
    }
}
