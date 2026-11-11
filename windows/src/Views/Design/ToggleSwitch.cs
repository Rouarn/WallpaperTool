using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>自绘开关，替代系统 CheckBox，尺寸与配色取自 token。</summary>
    internal sealed class ToggleSwitch : Control
    {
        private bool _checked;

        public ToggleSwitch()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            BackColor = Ui.Palette.Surface;
            Size = new Size(Ui.S(Theme.Size.ToggleWidth), Ui.S(Theme.Size.ToggleHeight));
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value)
                {
                    return;
                }
                _checked = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF track = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = track.Height / 2f;

            if (_checked)
            {
                Painting.FillRounded(g, track, radius, Ui.Palette.Accent);
            }
            else
            {
                Painting.FillRounded(g, track, radius, Ui.Palette.SurfaceSubtle);
                Painting.DrawRounded(g, track, radius, Ui.Palette.StrokeStrong, Ui.Sf(Theme.Size.BorderWidth));
            }

            float inset = Ui.Sf(2);
            float knobSize = Height - inset * 2 - 1f;
            float knobX = _checked ? Width - inset - knobSize - 0.5f : inset + 0.5f;
            RectangleF knob = new RectangleF(knobX, inset + 0.5f, knobSize, knobSize);

            using (SolidBrush brush = new SolidBrush(Ui.Palette.Knob))
            {
                g.FillEllipse(brush, knob);
            }
            using (Pen pen = new Pen(Painting.Blend(Ui.Palette.StrokeStrong, Ui.Palette.Surface, 0.4f), 1f))
            {
                g.DrawEllipse(pen, knob);
            }

            if (Focused)
            {
                Painting.DrawRounded(g, new RectangleF(1f, 1f, Width - 2f, Height - 2f),
                    radius, Ui.Palette.AccentText, Ui.Sf(Theme.Size.FocusBorderWidth) - 1f);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
            {
                Checked = !Checked;
            }
            base.OnMouseUp(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }
    }
}
