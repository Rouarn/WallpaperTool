using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>下载行为卡片：两个开关 + 上次更新时间与「立即测试」。</summary>
    internal sealed class BehaviorCard : Card
    {
        private readonly ToggleRow _wallpaper;
        private readonly ToggleRow _history;
        private readonly Label _lastCaption;
        private readonly Label _lastValue;
        private readonly ChromeButton _testNow;

        /// <summary>程序化同步开关状态时置位，避免把「加载配置」误当成用户操作。</summary>
        private bool _syncing;

        public BehaviorCard()
            : base(Strings.SectionBehavior, Ui.S(Theme.Space.M))
        {
            _wallpaper = new ToggleRow(Strings.FieldSetWallpaper, Strings.HintSetWallpaperOff, false);
            _wallpaper.CheckedChanged += delegate
            {
                if (!_syncing)
                {
                    RaiseSetWallpaperChanged();
                }
            };

            _history = new ToggleRow(Strings.FieldKeepHistory, Strings.HintKeepHistoryOff, false);
            _history.CheckedChanged += delegate
            {
                if (!_syncing)
                {
                    RaiseKeepHistoryChanged();
                }
            };

            _lastCaption = Labels.OnSurface(Strings.FieldLastAutoUpdate, Ui.Fonts.Caption, Ui.Palette.TextSecondary);
            _lastValue = Labels.OnSurface(Strings.TimeNever, Ui.Fonts.Caption, Ui.Palette.TextPrimary);
            Labels.Fit(_lastCaption);
            Labels.Fit(_lastValue);

            _testNow = new ChromeButton(Strings.ActionTestNow, ButtonKind.Text);
            _testNow.Click += delegate { RaiseTestNow(); };

            Spacer spring = new Spacer();
            Row lastRow = new Row();
            lastRow.Spacing = Ui.S(Theme.Space.S);
            lastRow.Controls.Add(_lastCaption);
            lastRow.Controls.Add(_lastValue);
            lastRow.Controls.Add(spring);
            lastRow.Controls.Add(_testNow);
            lastRow.Spring = spring;

            Content.Controls.Add(_wallpaper);
            Content.Controls.Add(new Separator());
            Content.Controls.Add(_history);
            Content.Controls.Add(new Separator());
            Content.Controls.Add(lastRow);
        }

        public bool SetWallpaperChecked
        {
            get { return _wallpaper.Checked; }
        }

        public bool KeepHistoryChecked
        {
            get { return _history.Checked; }
        }

        public event EventHandler SetWallpaperChanged;

        public event EventHandler KeepHistoryChanged;

        public event EventHandler TestNowClicked;

        private void RaiseSetWallpaperChanged()
        {
            EventHandler handler = SetWallpaperChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseKeepHistoryChanged()
        {
            EventHandler handler = KeepHistoryChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseTestNow()
        {
            EventHandler handler = TestNowClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void SetValues(bool setWallpaper, bool keepHistory)
        {
            _syncing = true;
            try
            {
                _wallpaper.Checked = setWallpaper;
                _history.Checked = keepHistory;
                _wallpaper.SetHint(setWallpaper ? Strings.HintSetWallpaperOn : Strings.HintSetWallpaperOff);
                _history.SetHint(keepHistory ? Strings.HintKeepHistoryOn : Strings.HintKeepHistoryOff);
            }
            finally
            {
                _syncing = false;
            }
        }

        public void SetWallpaperState(bool value)
        {
            _syncing = true;
            try
            {
                _wallpaper.Checked = value;
                _wallpaper.SetHint(value ? Strings.HintSetWallpaperOn : Strings.HintSetWallpaperOff);
            }
            finally
            {
                _syncing = false;
            }
        }

        public void SetHistoryState(bool value)
        {
            _syncing = true;
            try
            {
                _history.Checked = value;
                _history.SetHint(value ? Strings.HintKeepHistoryOn : Strings.HintKeepHistoryOff);
            }
            finally
            {
                _syncing = false;
            }
        }

        public void SetLastUpdate(string text)
        {
            _lastValue.Text = text;
            Labels.Fit(_lastValue);
            PerformLayout();
        }
    }
}
