using System;
using System.Drawing;
using System.Windows.Forms;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 横向排列一行控件：子控件保持自身宽度，按内容自适应高度；
    /// 设置 Spring 后该控件吃掉剩余宽度，实现右对齐。
    /// </summary>
    internal sealed class Row : Panel
    {
        private int _spacing;
        private bool _centerVertically = true;
        private Control _spring;

        public Row()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
        }

        public int Spacing
        {
            get { return _spacing; }
            set
            {
                _spacing = value;
                PerformLayout();
            }
        }

        public bool CenterVertically
        {
            get { return _centerVertically; }
            set
            {
                _centerVertically = value;
                PerformLayout();
            }
        }

        public Control Spring
        {
            get { return _spring; }
            set
            {
                _spring = value;
                PerformLayout();
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            int height = 0;
            int fixedWidth = 0;
            int count = 0;

            for (int i = 0; i < Controls.Count; i++)
            {
                Control child = Controls[i];
                if (Visibility.IsHidden(child))
                {
                    continue;
                }

                height = Math.Max(height, child.Height);
                if (child != _spring)
                {
                    fixedWidth += child.Width;
                    count++;
                }
            }

            if (count > 1)
            {
                fixedWidth += _spacing * (count - 1);
            }

            if (_spring != null && !Visibility.IsHidden(_spring))
            {
                int remaining = ClientSize.Width - fixedWidth - (count > 0 ? _spacing : 0);
                _spring.Width = Math.Max(0, remaining);
            }

            int x = 0;
            for (int i = 0; i < Controls.Count; i++)
            {
                Control child = Controls[i];
                if (Visibility.IsHidden(child))
                {
                    continue;
                }

                int top = _centerVertically ? Math.Max(0, (height - child.Height) / 2) : 0;
                child.Location = new Point(x, top);
                x += child.Width + _spacing;
            }

            if (Height != height)
            {
                Height = height;
            }
        }
    }
}
