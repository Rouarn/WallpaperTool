using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>日志卡片：默认收起，展开后显示圆角日志面板与复制/打开操作。</summary>
    internal sealed class LogCard : Card
    {
        private readonly ChromeButton _toggle;
        private readonly ChromeButton _openFile;
        private readonly ChromeButton _copy;
        private readonly LogPanel _panel;
        private readonly Spacer _spring;
        private bool _expanded;

        public LogCard()
            : base(Strings.LogTitle, Ui.S(Theme.Space.M))
        {
            _toggle = new ChromeButton(Strings.HintLogCollapsed, ButtonKind.Text);
            _toggle.Click += delegate { SetExpanded(!_expanded); };

            _openFile = new ChromeButton(Strings.ActionOpenLogFile, ButtonKind.Text);
            _openFile.Click += delegate { RaiseOpenLogFile(); };

            _copy = new ChromeButton(Strings.ActionCopyLog, ButtonKind.Text);
            _copy.Click += delegate { RaiseCopyLog(); };

            _spring = new Spacer();
            Row header = new Row();
            header.Spacing = Ui.S(Theme.Space.S);
            header.Controls.Add(_toggle);
            header.Controls.Add(_spring);
            header.Controls.Add(_openFile);
            header.Controls.Add(_copy);
            header.Spring = _spring;

            _panel = new LogPanel();
            Visibility.Set(_panel, false);

            Content.Controls.Add(header);
            Content.Controls.Add(_panel);
        }

        public event EventHandler OpenLogFileClicked;

        public event EventHandler CopyLogClicked;

        public event EventHandler ExpandedChanged;

        public bool Expanded
        {
            get { return _expanded; }
        }

        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            Visibility.Set(_panel, expanded);
            _toggle.Text = expanded ? Strings.ActionHideLog : Strings.HintLogCollapsed;
            Content.PerformLayout();
            PerformLayout();

            EventHandler handler = ExpandedChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void SetLogText(string text)
        {
            _panel.Value = text;
        }

        public string LogText
        {
            get { return _panel.Value; }
        }

        private void RaiseOpenLogFile()
        {
            EventHandler handler = OpenLogFileClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseCopyLog()
        {
            EventHandler handler = CopyLogClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
