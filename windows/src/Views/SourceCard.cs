using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;
using WallpaperTool.Core;

namespace WallpaperTool.Views
{
    /// <summary>下载源卡片：图片地址、保存目录与选择按钮，校验失败时字段下方给出原因。</summary>
    internal sealed class SourceCard : Card
    {
        private readonly FieldRow _urlRow;
        private readonly FieldRow _destRow;
        private readonly Label _error;

        public SourceCard()
            : base(Strings.SectionSource, Ui.S(Theme.Space.M))
        {
            _urlRow = new FieldRow(Strings.FieldUrl, AppConfig.DefaultUrl);
            _urlRow.Input.Submit += delegate { RaiseUrlCommitted(); };

            _error = Labels.OnSurface(string.Empty, Ui.Fonts.Caption, Ui.Palette.Danger);
            _error.AutoEllipsis = true;
            _error.Height = Ui.Fonts.Caption.Height;
            Visibility.Set(_error, false);

            _destRow = new FieldRow(Strings.FieldDestDir, string.Empty, Strings.ActionChooseFolder);
            _destRow.Trailing.Click += delegate { RaiseChooseFolder(); };

            Content.Controls.Add(_urlRow);
            Content.Controls.Add(_error);
            Content.Controls.Add(_destRow);
        }

        public TextInput UrlInput
        {
            get { return _urlRow.Input; }
        }

        public TextInput DestInput
        {
            get { return _destRow.Input; }
        }

        public event EventHandler ChooseFolderClicked;

        public event EventHandler UrlCommitted;

        private void RaiseChooseFolder()
        {
            EventHandler handler = ChooseFolderClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseUrlCommitted()
        {
            EventHandler handler = UrlCommitted;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void SetUrlError(string message)
        {
            bool hasError = !string.IsNullOrEmpty(message);
            _error.Text = message ?? string.Empty;
            Visibility.Set(_error, hasError);
            _urlRow.Input.Invalid = hasError;
            Content.PerformLayout();
            PerformLayout();
        }
    }
}
