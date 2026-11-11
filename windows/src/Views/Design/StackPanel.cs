using System;
using System.Drawing;
using System.Windows.Forms;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 纵向堆叠容器：统一处理间距、拉伸宽度与自适应高度，
    /// 让各卡片不必自己计算兄弟控件的位置。
    /// </summary>
    internal sealed class StackPanel : Panel
    {
        private int _spacing;

        public StackPanel()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Canvas;
        }

        /// <summary>相邻子控件之间的间距（设备像素）。</summary>
        public int Spacing
        {
            get { return _spacing; }
            set
            {
                if (_spacing == value)
                {
                    return;
                }
                _spacing = value;
                PerformLayout();
                Invalidate();
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Reflow();
        }

        private void Reflow()
        {
            int y = Padding.Top;
            int width = Math.Max(0, ClientSize.Width - Padding.Horizontal);
            int count = 0;

            for (int i = 0; i < Controls.Count; i++)
            {
                Control child = Controls[i];
                if (Visibility.IsHidden(child))
                {
                    continue;
                }

                child.Width = width;
                child.PerformLayout();
                child.Location = new Point(Padding.Left, y);
                y += child.Height + _spacing;
                count++;
            }

            if (count > 0)
            {
                y -= _spacing;
            }

            int desired = y + Padding.Bottom;
            if (Height != desired)
            {
                Height = desired;
            }
        }
    }
}
