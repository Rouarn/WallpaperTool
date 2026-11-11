using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 纵向滚动容器：视口裁剪 + 右侧细滚动条。
    /// 不用 Panel.AutoScroll，因为它会按 DisplayRectangle 自行加横向滚动条，
    /// 在本布局里会出现「内容没超宽却横向可滚动」的抖动。
    /// </summary>
    internal sealed class ScrollView : Panel
    {
        private readonly Panel _viewport;
        private readonly ScrollThumb _bar;
        private readonly Control _content;
        private readonly int _padding;
        private int _offset;
        private bool _layingOut;

        public ScrollView(Control content, int padding)
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Canvas;

            _content = content;
            _padding = padding;

            _viewport = new Panel();
            _viewport.BackColor = Ui.Palette.Canvas;
            _viewport.Controls.Add(_content);

            _bar = new ScrollThumb();
            _bar.ScrollTo += delegate(int value) { SetOffset(value); };

            Controls.Add(_viewport);
            Controls.Add(_bar);

            _content.SizeChanged += delegate { PerformLayout(); };
        }

        public void ScrollBy(int delta)
        {
            SetOffset(_offset + delta);
        }

        /// <summary>把某个子控件滚进可视区（用于展开日志卡片）。</summary>
        public void BringIntoView(Control control)
        {
            if (control == null)
            {
                return;
            }

            int extent = ExtentHeight;
            int maxOffset = Math.Max(0, extent - ClientSize.Height);
            int target = control.Bottom + _padding + Ui.S(Theme.Space.M) - ClientSize.Height;
            SetOffset(Math.Max(0, Math.Min(maxOffset, target)));
        }

        private int ExtentHeight
        {
            get { return _content.Height + _padding * 2; }
        }

        private void SetOffset(int value)
        {
            int maxOffset = Math.Max(0, ExtentHeight - ClientSize.Height);
            int clamped = Math.Max(0, Math.Min(maxOffset, value));
            if (clamped == _offset)
            {
                return;
            }

            _offset = clamped;
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (_layingOut)
            {
                return;
            }

            _layingOut = true;
            try
            {
                int barWidth = Ui.S(10);
                int viewportWidth = Math.Max(0, ClientSize.Width - barWidth);

                _viewport.Bounds = new Rectangle(0, 0, viewportWidth, ClientSize.Height);
                _content.Width = Math.Max(0, viewportWidth - _padding * 2);
                _content.PerformLayout();

                int extent = ExtentHeight;
                int maxOffset = Math.Max(0, extent - ClientSize.Height);
                _offset = Math.Max(0, Math.Min(_offset, maxOffset));

                _content.Location = new Point(_padding, _padding - _offset);

                _bar.Bounds = new Rectangle(
                    ClientSize.Width - barWidth, 0, barWidth, ClientSize.Height);
                _bar.Visible = maxOffset > 0;
                _bar.SetMetrics(ClientSize.Height, extent, _offset, maxOffset);
            }
            finally
            {
                _layingOut = false;
            }
        }
    }

    /// <summary>右侧细滚动条，只在需要时出现，悬停/拖动时加深。</summary>
    internal sealed class ScrollThumb : Control
    {
        private int _viewportHeight;
        private int _extent;
        private int _offset;
        private int _maxOffset;

        private bool _hover;
        private bool _dragging;
        private int _dragStartY;
        private int _dragStartOffset;

        public ScrollThumb()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Canvas;
            TabStop = false;
        }

        public event Action<int> ScrollTo;

        public void SetMetrics(int viewportHeight, int extent, int offset, int maxOffset)
        {
            _viewportHeight = viewportHeight;
            _extent = extent;
            _offset = offset;
            _maxOffset = maxOffset;
            Invalidate();
        }

        private int ThumbHeight()
        {
            if (_extent <= _viewportHeight || _extent <= 0)
            {
                return Height;
            }

            float ratio = _viewportHeight / (float)_extent;
            return Math.Max(Ui.S(36), (int)Math.Round(Height * ratio));
        }

        private int ThumbTop()
        {
            int thumbHeight = ThumbHeight();
            if (_maxOffset <= 0)
            {
                return 0;
            }

            float ratio = _offset / (float)_maxOffset;
            return (int)Math.Round((Height - thumbHeight) * ratio);
        }

        private Rectangle ThumbBounds()
        {
            int width = Ui.S(4);
            int thumbHeight = ThumbHeight();
            return new Rectangle((Width - width) / 2, ThumbTop(), width, thumbHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle thumb = ThumbBounds();
            int alpha = (_hover || _dragging) ? 150 : 95;
            Color color = Color.FromArgb(alpha, Ui.Palette.TextTertiary);

            Painting.FillRounded(g,
                new RectangleF(thumb.X, thumb.Y, thumb.Width, thumb.Height),
                thumb.Width / 2f, color);
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _maxOffset <= 0)
            {
                return;
            }

            Rectangle thumb = ThumbBounds();
            if (thumb.Contains(e.Location))
            {
                _dragging = true;
                _dragStartY = e.Y;
                _dragStartOffset = _offset;
                Capture = true;
            }
            else
            {
                // 点击轨道：向上/向下翻页
                int page = Math.Max(Ui.S(40), _viewportHeight - Ui.S(60));
                RaiseScroll(_offset + (e.Y < thumb.Y ? -page : page));
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                int travel = Math.Max(1, Height - ThumbHeight());
                int delta = e.Y - _dragStartY;
                RaiseScroll(_dragStartOffset + (int)((long)delta * _maxOffset / travel));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        private void RaiseScroll(int value)
        {
            Action<int> handler = ScrollTo;
            if (handler != null)
            {
                handler(Math.Max(0, Math.Min(_maxOffset, value)));
            }
        }
    }

    /// <summary>把滚轮消息路由给鼠标下方的滚动容器（不管焦点在哪个子控件）。</summary>
    internal sealed class WheelRouter : IMessageFilter
    {
        private const int WmMouseWheel = 0x020A;
        private const int WmMouseHWheel = 0x020E;

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmMouseWheel && m.Msg != WmMouseHWheel)
            {
                return false;
            }

            IntPtr handle = WindowFromPoint(Cursor.Position);
            Control control = handle == IntPtr.Zero ? null : Control.FromHandle(handle);

            while (control != null)
            {
                ScrollView view = control as ScrollView;
                if (view != null)
                {
                    int delta = (short)((long)m.WParam >> 16);
                    view.ScrollBy(-delta / 120 * Ui.S(72));
                    return true;
                }
                control = control.Parent;
            }

            return false;
        }
    }
}
