using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Text
    }

    /// <summary>
    /// 统一的按钮控件，三种角色对应 macOS 端的 Primary / Secondary / TextButton 样式，
    /// 自绘圆角、悬停、按下与焦点态。
    /// </summary>
    internal sealed class ChromeButton : Control
    {
        private readonly ButtonKind _kind;
        private string _text;
        private bool _hover;
        private bool _pressed;

        public ChromeButton(string text, ButtonKind kind)
        {
            _kind = kind;
            _text = text;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Ui.Palette.Surface;
            Size = MeasureSize();
        }

        public ButtonKind Kind
        {
            get { return _kind; }
        }

        public override string Text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                {
                    return;
                }
                _text = value;
                Size = MeasureSize();
                Invalidate();
            }
        }

        public Size MeasureSize()
        {
            Font font = FontFor();
            Size measured = TextRenderer.MeasureText(
                _text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            int padH;
            int height;
            if (_kind == ButtonKind.Primary)
            {
                padH = Ui.S(Theme.Space.L);
                height = Ui.S(Theme.Size.PrimaryButtonHeight);
            }
            else if (_kind == ButtonKind.Secondary)
            {
                padH = Ui.S(Theme.Space.M);
                height = Ui.S(Theme.Size.ControlHeight);
            }
            else
            {
                padH = Ui.S(2);
                height = Math.Max(font.Height + Ui.S(4), Ui.S(24));
            }

            int extra = _kind == ButtonKind.Secondary ? Ui.S(2) : 0;
            return new Size(measured.Width + padH * 2 + extra, height);
        }

        private Font FontFor()
        {
            return _kind == ButtonKind.Primary ? Ui.Fonts.BodyStrong : Ui.Fonts.Body;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Control);
            Color textColor;

            if (_kind == ButtonKind.Primary)
            {
                Color background = _hover ? Ui.Palette.AccentHover : Ui.Palette.Accent;
                if (_pressed)
                {
                    background = Ui.Palette.AccentPressed;
                }
                if (!Enabled)
                {
                    background = Painting.Blend(Ui.Palette.Accent, Ui.Palette.Surface, 0.45f);
                }
                Painting.FillRounded(g, rect, radius, background);
                textColor = Enabled ? Ui.Palette.OnAccent : Ui.Palette.TextTertiary;
            }
            else if (_kind == ButtonKind.Secondary)
            {
                if (_hover || _pressed)
                {
                    Color fill = _pressed
                        ? Ui.Palette.SurfaceSubtle
                        : Painting.Blend(Ui.Palette.SurfaceSubtle, Ui.Palette.Surface, 0.45f);
                    Painting.FillRounded(g, rect, radius, fill);
                }
                Painting.DrawRounded(g, rect, radius, Ui.Palette.StrokeStrong, Ui.Sf(Theme.Size.BorderWidth));
                textColor = Enabled ? Ui.Palette.TextPrimary : Ui.Palette.TextTertiary;
            }
            else
            {
                if (_pressed)
                {
                    textColor = Ui.Palette.AccentPressed;
                }
                else if (_hover)
                {
                    textColor = Ui.Palette.AccentHover;
                }
                else
                {
                    textColor = Ui.Palette.AccentText;
                }
                if (!Enabled)
                {
                    textColor = Ui.Palette.TextTertiary;
                }
            }

            Painting.Text(g, _text, FontFor(), textColor, ClientRectangle, Painting.SingleLineCentered);

            if (Focused && !_pressed)
            {
                RectangleF ring = new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f);
                Painting.DrawRounded(g, ring, Math.Max(1f, radius - 1f),
                    Ui.Palette.AccentText, Ui.Sf(Theme.Size.FocusBorderWidth) - 1f);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
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
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _pressed = true;
                Focus();
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool fire = _pressed && e.Button == MouseButtons.Left
                && ClientRectangle.Contains(e.Location) && Enabled;
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
            if (fire)
            {
                OnClick(EventArgs.Empty);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                _pressed = true;
                Invalidate();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_pressed && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                _pressed = false;
                Invalidate();
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }
    }
}
