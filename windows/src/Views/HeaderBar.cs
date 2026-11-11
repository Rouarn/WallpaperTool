using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 顶部应用栏：品牌星标 + 标题 + 说明，右侧主操作/取消，
    /// 下载时在底边绘制 4px 进度条（对应 macOS 的 header + progressBar）。
    /// </summary>
    internal sealed class HeaderBar : Panel
    {
        private readonly BrandStar _star;
        private readonly Label _title;
        private readonly Label _tagline;
        private readonly ChromeButton _primary;
        private readonly ChromeButton _cancel;
        private readonly Timer _spin;
        private readonly int _barHeight;

        private bool _busy;
        private int _progress = -1;
        private float _angle;

        public HeaderBar()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            BackColor = Ui.Palette.Surface;
            Height = Ui.S(Theme.Size.HeaderHeight);
            _barHeight = Ui.S(Theme.Size.ProgressBar);

            _star = new BrandStar();
            _star.Size = new Size(Ui.S(Theme.Size.IconLarge), Ui.S(Theme.Size.IconLarge));
            _star.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _title = Labels.OnSurface(Strings.AppName, Ui.Fonts.Title, Ui.Palette.TextPrimary);
            _tagline = Labels.OnSurface(Strings.AppTagline, Ui.Fonts.Caption, Ui.Palette.TextSecondary);

            _primary = new ChromeButton(Strings.ActionDownloadNow, ButtonKind.Primary);
            _primary.Click += delegate { OnDownloadClicked(EventArgs.Empty); };

            _cancel = new ChromeButton(Strings.ActionCancel, ButtonKind.Secondary);
            Visibility.Set(_cancel, false);
            _cancel.Click += delegate { OnCancelClicked(EventArgs.Empty); };

            Controls.Add(_star);
            Controls.Add(_title);
            Controls.Add(_tagline);
            Controls.Add(_primary);
            Controls.Add(_cancel);

            // 标题标签是整行宽的，按钮必须压在它上面，否则会被盖住
            _primary.BringToFront();
            _cancel.BringToFront();

            _spin = new Timer();
            _spin.Interval = 40;
            _spin.Tick += delegate
            {
                _angle += 6f;
                if (_angle >= 360f)
                {
                    _angle -= 360f;
                }
                _star.Angle = _angle;
            };
        }

        public event EventHandler DownloadClicked;

        public event EventHandler CancelClicked;

        private void OnDownloadClicked(EventArgs e)
        {
            EventHandler handler = DownloadClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnCancelClicked(EventArgs e)
        {
            EventHandler handler = CancelClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            Visibility.Set(_cancel, busy);
            _primary.Enabled = !busy;

            if (busy)
            {
                _primary.Text = Strings.StatusDownloadingUnknown;
                _spin.Start();
            }
            else
            {
                _primary.Text = Strings.ActionDownloadNow;
                _progress = -1;
                _spin.Stop();
                _angle = 0f;
                _star.Angle = 0f;
            }

            PerformLayout();
            Invalidate();
        }

        public void SetProgress(int percent)
        {
            _progress = percent;
            _primary.Text = Strings.Format(Strings.ActionDownloading, percent);
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // 构造函数里设置 Height 会先触发一次布局，此时子控件尚未创建
            if (_star == null || _title == null || _tagline == null
                || _primary == null || _cancel == null)
            {
                return;
            }

            int pad = Ui.S(Theme.Space.Xl);
            int gap = Ui.S(Theme.Space.M);

            _star.Location = new Point(pad, (Height - _barHeight - _star.Height) / 2);
            _star.Size = new Size(Ui.S(Theme.Size.IconLarge), Ui.S(Theme.Size.IconLarge));

            int titleX = _star.Right + gap;
            int titleHeight = Ui.Fonts.Title.Height;
            int taglineHeight = Ui.Fonts.Caption.Height;
            int blockHeight = titleHeight + 2 + taglineHeight;
            int blockTop = (Height - _barHeight - blockHeight) / 2;

            int buttonsLeft = ClientSize.Width - pad - _primary.Width;
            int textWidth = Math.Max(0, buttonsLeft - gap - titleX);

            _title.Location = new Point(titleX, blockTop);
            _title.Size = new Size(textWidth, titleHeight);
            _tagline.Location = new Point(titleX, blockTop + titleHeight + 2);
            _tagline.Size = new Size(textWidth, taglineHeight);

            _primary.Location = new Point(buttonsLeft, (Height - _barHeight - _primary.Height) / 2);
            _cancel.Location = new Point(
                _primary.Left - Ui.S(Theme.Space.S) - _cancel.Width,
                (Height - _barHeight - _cancel.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_busy)
            {
                int track = _barHeight;
                Rectangle bar = new Rectangle(0, Height - track, Width, track);
                using (SolidBrush brush = new SolidBrush(Ui.Palette.SurfaceSubtle))
                {
                    g.FillRectangle(brush, bar);
                }

                if (_progress >= 0)
                {
                    int filled = (int)Math.Round(Width * Math.Min(100, _progress) / 100.0);
                    if (filled > 0)
                    {
                        using (SolidBrush brush = new SolidBrush(Ui.Palette.Accent))
                        {
                            g.FillRectangle(brush, new Rectangle(0, Height - track, filled, track));
                        }
                    }
                }
            }

            using (Pen pen = new Pen(Ui.Palette.Stroke, 1f))
            {
                g.DrawLine(pen, 0f, Height - 0.5f, Width, Height - 0.5f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spin.Stop();
                _spin.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
