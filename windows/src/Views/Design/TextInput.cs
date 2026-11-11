using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 圆角输入框：外层自绘表面与描边，内层是无边框 TextBox，
    /// 焦点态用强调色描边，非法态用危险色描边。
    /// </summary>
    internal sealed class TextInput : Panel
    {
        private readonly TextBox _box;
        private bool _invalid;

        public TextInput(string value)
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Height = Ui.S(Theme.Size.ControlHeight);

            _box = new TextBox();
            _box.BorderStyle = BorderStyle.None;
            _box.BackColor = Ui.Palette.SurfaceSubtle;
            _box.ForeColor = Ui.Palette.TextPrimary;
            _box.Font = Ui.Fonts.Body;
            _box.Text = value ?? string.Empty;
            _box.GotFocus += delegate { Invalidate(); };
            _box.LostFocus += delegate { Invalidate(); };
            _box.TextChanged += delegate { OnValueChanged(EventArgs.Empty); };
            _box.KeyDown += OnBoxKeyDown;
            Controls.Add(_box);
        }

        public TextBox Inner
        {
            get { return _box; }
        }

        public string Value
        {
            get { return _box.Text; }
            set { _box.Text = value ?? string.Empty; }
        }

        public bool Invalid
        {
            get { return _invalid; }
            set
            {
                if (_invalid == value)
                {
                    return;
                }
                _invalid = value;
                Invalidate();
            }
        }

        public event EventHandler ValueChanged;

        public event EventHandler Submit;

        public void FocusInput()
        {
            _box.Focus();
            _box.SelectionStart = _box.TextLength;
            _box.SelectionLength = 0;
        }

        private void OnValueChanged(EventArgs e)
        {
            EventHandler handler = ValueChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EventHandler handler = Submit;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_box == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.S);
            int width = Math.Max(0, ClientSize.Width - pad * 2);
            _box.Width = width;
            _box.Location = new Point(pad, Math.Max(0, (ClientSize.Height - _box.Height) / 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Control);

            Painting.FillRounded(g, rect, radius, Ui.Palette.SurfaceSubtle);

            bool focused = _box.Focused;
            Color border;
            float width;
            if (_invalid)
            {
                border = Ui.Palette.Danger;
                width = Ui.Sf(Theme.Size.FocusBorderWidth);
            }
            else if (focused)
            {
                border = Ui.Palette.AccentText;
                width = Ui.Sf(Theme.Size.FocusBorderWidth);
            }
            else
            {
                border = Ui.Palette.StrokeStrong;
                width = Ui.Sf(Theme.Size.BorderWidth);
            }

            Painting.DrawRounded(g, rect, radius, border, width);
        }
    }
}
