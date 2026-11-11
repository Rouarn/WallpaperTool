using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WallpaperTool;
using WallpaperTool.Core;

namespace WallpaperTool.Views
{
    /// <summary>预览卡片：有壁纸时显示缩略图与元信息，无壁纸时显示空态。</summary>
    internal sealed class PreviewCard : Card
    {
        private readonly Thumbnail _thumb;
        private readonly Label _dimensions;
        private readonly Label _size;
        private readonly Label _updated;
        private readonly StackPanel _info;
        private readonly Row _filledRow;
        private readonly Row _emptyRow;
        private readonly DashedPlaceholder _placeholder;
        private readonly StackPanel _emptyText;
        private readonly Label _emptyTitle;
        private readonly Label _emptyBody;
        private readonly ChromeButton _openFolder;
        private readonly ChromeButton _reapply;

        public PreviewCard()
            : base(Strings.SectionPreview, Ui.S(Theme.Space.M))
        {
            _thumb = new Thumbnail();

            _dimensions = Labels.OnSurface(Strings.MetaUnknownSize, Ui.Fonts.BodyStrong, Ui.Palette.TextPrimary);
            _size = Labels.OnSurface(string.Empty, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _updated = Labels.OnSurface(string.Empty, Ui.Fonts.Caption, Ui.Palette.TextTertiary);

            _openFolder = new ChromeButton(Strings.ActionOpenFolder, ButtonKind.Secondary);
            _openFolder.Click += delegate { OnOpenFolder(EventArgs.Empty); };

            _reapply = new ChromeButton(Strings.ActionReapply, ButtonKind.Secondary);
            _reapply.Click += delegate { OnReapply(EventArgs.Empty); };

            Row actions = new Row();
            actions.Spacing = Ui.S(Theme.Space.S);
            actions.Controls.Add(_openFolder);
            actions.Controls.Add(_reapply);

            _info = new StackPanel();
            _info.BackColor = Ui.Palette.Surface;
            _info.Spacing = Ui.S(Theme.Space.Xs);
            _info.Controls.Add(_dimensions);
            _info.Controls.Add(_size);
            _info.Controls.Add(_updated);
            _info.Controls.Add(actions);

            _filledRow = new Row();
            _filledRow.BackColor = Ui.Palette.Surface;
            _filledRow.Spacing = Ui.S(Theme.Space.L);
            _filledRow.CenterVertically = false;
            _filledRow.Controls.Add(_thumb);
            _filledRow.Controls.Add(_info);

            _placeholder = new DashedPlaceholder();
            _emptyTitle = Labels.OnSurface(Strings.EmptyPreviewTitle, Ui.Fonts.BodyStrong, Ui.Palette.TextPrimary);
            _emptyBody = Labels.OnSurface(Strings.EmptyPreviewBody, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _emptyBody.TextAlign = ContentAlignment.TopLeft;

            _emptyText = new StackPanel();
            _emptyText.BackColor = Ui.Palette.Surface;
            _emptyText.Spacing = Ui.S(Theme.Space.Xs);
            _emptyText.Controls.Add(_emptyTitle);
            _emptyText.Controls.Add(_emptyBody);

            _emptyRow = new Row();
            _emptyRow.BackColor = Ui.Palette.Surface;
            _emptyRow.Spacing = Ui.S(Theme.Space.M);
            _emptyRow.Controls.Add(_placeholder);
            _emptyRow.Controls.Add(_emptyText);

            Content.Controls.Add(_filledRow);
            Content.Controls.Add(_emptyRow);

            Visibility.Set(_filledRow, false);
        }

        public event EventHandler OpenFolderClicked;

        public event EventHandler ReapplyClicked;

        private void OnOpenFolder(EventArgs e)
        {
            EventHandler handler = OpenFolderClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnReapply(EventArgs e)
        {
            EventHandler handler = ReapplyClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Update(WallpaperPreview preview, Bitmap image)
        {
            bool hasPreview = preview != null;
            Visibility.Set(_filledRow, hasPreview);
            Visibility.Set(_emptyRow, !hasPreview);

            if (hasPreview)
            {
                _thumb.Show(image);
                _dimensions.Text = preview.Width > 0 && preview.Height > 0
                    ? Strings.Format(Strings.MetaDimensions, preview.Width, preview.Height)
                    : Strings.MetaUnknownSize;
                _size.Text = WallpaperLibrary.FormatBytes(preview.ByteSize);
                _updated.Text = Strings.Format(
                    Strings.MetaUpdatedAt,
                    preview.ModifiedAt.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture));
            }
            else
            {
                _thumb.Show(null);
            }

            Content.PerformLayout();
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            // 基类构造期间会触发一次布局，此时派生字段还没赋值
            if (_info != null && _thumb != null && _emptyText != null && _emptyBody != null)
            {
                int pad = Ui.S(Theme.Space.L);
                int contentWidth = Math.Max(0, ClientSize.Width - pad * 2);

                if (contentWidth > 0)
                {
                    // 窗口变窄时缩略图跟着收，给右侧元信息与按钮留出空间
                    int thumbWidth = Math.Min(
                        Ui.S(220), Math.Max(Ui.S(132), (int)Math.Round(contentWidth * 0.42)));
                    int thumbHeight = (int)Math.Round(thumbWidth * 124.0 / 220.0);
                    if (_thumb.Size.Width != thumbWidth || _thumb.Size.Height != thumbHeight)
                    {
                        _thumb.Size = new Size(thumbWidth, thumbHeight);
                    }

                    _info.Width = Math.Max(0, contentWidth - thumbWidth - Ui.S(Theme.Space.L));
                    _info.PerformLayout();

                    int emptyWidth = Math.Max(0, contentWidth - _placeholder.Width - Ui.S(Theme.Space.M));
                    _emptyText.Width = emptyWidth;
                    _emptyBody.Size = new Size(
                        emptyWidth,
                        Painting.Measure(_emptyBody.Text, Ui.Fonts.Caption, emptyWidth, Painting.Wrapped).Height);
                    _emptyText.PerformLayout();
                }
            }

            base.OnLayout(levent);
        }
    }

    /// <summary>空态预览占位：虚线圆角框 + 星标。</summary>
    internal sealed class DashedPlaceholder : Control
    {
        public DashedPlaceholder()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Palette.Surface;
            Size = new Size(Ui.S(96), Ui.S(54));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Ui.Sf(Theme.Radius.Control);
            Painting.DashedRounded(g, rect, radius, Ui.Palette.StrokeStrong, Ui.Sf(Theme.Size.BorderWidth));

            float size = Ui.Sf(24);
            float cx = Width / 2f;
            float cy = Height / 2f;
            float outer = size / 2f;
            float inner = outer * 0.22f;
            PointF[] points = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double angle = -Math.PI / 2.0 + Math.PI / 4.0 * i;
                float r = (i % 2 == 0) ? outer : inner;
                points[i] = new PointF(
                    cx + (float)Math.Cos(angle) * r,
                    cy + (float)Math.Sin(angle) * r);
            }

            using (SolidBrush brush = new SolidBrush(Ui.Palette.TextTertiary))
            {
                g.FillPolygon(brush, points);
            }
        }
    }
}
