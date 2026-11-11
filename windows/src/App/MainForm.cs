using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WallpaperTool.Core;
using WallpaperTool.Views;
using Timer = System.Windows.Forms.Timer;

namespace WallpaperTool.App
{
    /// <summary>
    /// 主窗口：顶部应用栏 + 可滚动的卡片区 + 底部状态条。
    /// 负责把配置、下载任务与各卡片的状态串起来，卡片本身不碰业务。
    /// </summary>
    internal sealed class MainForm : Form
    {
        private const string ResultSuccess = "success";
        private const string ResultFailed = "failed";
        private const string ResultCancelled = "cancelled";

        private readonly AppConfig _config;
        private readonly Logger _logger;

        private readonly HeaderBar _header;
        private readonly WallpaperTool.Views.StatusBar _statusBar;
        private readonly ScrollView _scroll;
        private readonly StackPanel _stack;
        private readonly PreviewCard _previewCard;
        private readonly SourceCard _sourceCard;
        private readonly BehaviorCard _behaviorCard;
        private readonly LogCard _logCard;
        private readonly Timer _successTimer;

        private BackgroundWorker _worker;
        private CancellationTokenSource _cancellation;
        private BannerView _banner;
        private string _currentWallpaper;
        private bool _busy;

        public MainForm(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;

            using (Graphics graphics = CreateGraphics())
            {
                Ui.Initialize(graphics.DpiX);
            }

            Text = Strings.AppName;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Ui.Palette.Canvas;
            ForeColor = Ui.Palette.TextPrimary;
            Font = Ui.Fonts.Body;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.S(Theme.Size.WindowWidth), Ui.S(Theme.Size.WindowHeight));
            MinimumSize = SizeFromClientSize(new Size(
                Ui.S(Theme.Size.WindowMinWidth), Ui.S(Theme.Size.WindowMinHeight)));

            _header = new HeaderBar();
            _header.DownloadClicked += delegate { StartDownload(); };
            _header.CancelClicked += delegate { CancelDownload(); };

            _statusBar = new WallpaperTool.Views.StatusBar();

            _stack = new StackPanel();
            _stack.BackColor = Ui.Palette.Canvas;
            _stack.Spacing = Ui.S(Theme.Space.L);

            _previewCard = new PreviewCard();
            _previewCard.OpenFolderClicked += delegate { OpenFolder(); };
            _previewCard.ReapplyClicked += delegate { ReapplyWallpaper(); };

            _sourceCard = new SourceCard();
            _sourceCard.ChooseFolderClicked += delegate { ChooseFolder(); };
            _sourceCard.UrlCommitted += delegate { ValidateUrlField(); };

            _behaviorCard = new BehaviorCard();
            _behaviorCard.SetWallpaperChanged += delegate { OnSetWallpaperToggled(); };
            _behaviorCard.KeepHistoryChanged += delegate { OnKeepHistoryToggled(); };
            _behaviorCard.TestNowClicked += delegate { StartDownload(); };

            _logCard = new LogCard();
            _logCard.OpenLogFileClicked += delegate { OpenLogFile(); };
            _logCard.CopyLogClicked += delegate { CopyLog(); };
            _logCard.ExpandedChanged += delegate { OnLogExpandedChanged(); };

            _stack.Controls.Add(_previewCard);
            _stack.Controls.Add(_sourceCard);
            _stack.Controls.Add(_behaviorCard);
            _stack.Controls.Add(_logCard);

            _scroll = new ScrollView(_stack, Ui.S(Theme.Space.Xl));
            _scroll.Dock = DockStyle.Fill;

            Controls.Add(_scroll);
            Controls.Add(_statusBar);
            Controls.Add(_header);

            _successTimer = new Timer();
            _successTimer.Interval = 3000;
            _successTimer.Tick += delegate
            {
                _successTimer.Stop();
                SetStatus(StatusKind.Ready, UpdatedAtText());
            };

            _logger.Info(Strings.LogAppStarted);
            LoadConfigIntoUi();
            RefreshPreview();
            RestoreStatus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _scroll.PerformLayout();
            ApplyDarkTitleBar();
            _sourceCard.UrlInput.FocusInput();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDarkTitleBar();
        }

        /// <summary>深色模式下把系统标题栏也切成深色，避免「白框 + 黑内容」的割裂感。</summary>
        private void ApplyDarkTitleBar()
        {
            if (!Ui.IsDark)
            {
                return;
            }

            try
            {
                int enabled = 1;
                // Windows 10 20H1 起是 20，1809 是 19；都不支持时静默忽略
                if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
                }
            }
            catch
            {
                // 旧系统没有 dwmapi 时不影响使用
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
            }
            _config.Save();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _successTimer.Stop();
                _successTimer.Dispose();
                if (_cancellation != null)
                {
                    _cancellation.Dispose();
                    _cancellation = null;
                }
            }
            base.Dispose(disposing);
        }

        // ---------------------------------------------------------------- 配置

        private void LoadConfigIntoUi()
        {
            _sourceCard.UrlInput.Value = _config.Url;
            _sourceCard.DestInput.Value = _config.DestDir;
            _behaviorCard.SetValues(_config.SetWallpaperAfterDownload, _config.KeepHistory);
            _behaviorCard.SetLastUpdate(UpdatedAtText());
            RefreshLog();

            if (_config.Migrated)
            {
                _logger.Info(Strings.LogConfigMigrated);
                _config.Save();
            }
        }

        private void RestoreStatus()
        {
            if (string.Equals(_config.LastResult, ResultFailed, StringComparison.Ordinal))
            {
                SetStatus(StatusKind.Failure, FailStatusText());
                return;
            }
            SetStatus(StatusKind.Ready, UpdatedAtText());
        }

        private string FailStatusText()
        {
            DateTime time;
            if (AppConfig.TryParseTime(_config.LastRunAt, out time))
            {
                return Strings.StatusLastAutoFail + " · " + time.ToString("MM-dd HH:mm");
            }
            return Strings.StatusLastAutoFail;
        }

        private string UpdatedAtText()
        {
            DateTime time;
            if (AppConfig.TryParseTime(_config.LastRunAt, out time))
            {
                return Strings.Format(Strings.StatusUpdatedAt, time.ToString("MM-dd HH:mm"));
            }
            return Strings.StatusReady;
        }

        private void SetStatus(StatusKind kind, string text)
        {
            _statusBar.SetStatus(kind, text);
        }

        // ---------------------------------------------------------------- 下载

        private void StartDownload()
        {
            if (_busy)
            {
                return;
            }

            _config.Url = _sourceCard.UrlInput.Value.Trim();
            _config.DestDir = _sourceCard.DestInput.Value.Trim();

            string error = ValidateUrl(_config.Url);
            _sourceCard.SetUrlError(error);
            if (error != null)
            {
                SetStatus(StatusKind.Failure, error);
                return;
            }

            _config.Save();
            ClearBanner();
            SetStatus(StatusKind.Working, Strings.StatusDownloadingUnknown);

            _cancellation = new CancellationTokenSource();
            _busy = true;
            _header.SetBusy(true);

            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += OnWork;
            _worker.ProgressChanged += OnProgress;
            _worker.RunWorkerCompleted += OnCompleted;
            _worker.RunWorkerAsync(_cancellation.Token);
        }

        private void CancelDownload()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
            }
        }

        private void OnWork(object sender, DoWorkEventArgs e)
        {
            CancellationToken token = (CancellationToken)e.Argument;
            BackgroundWorker worker = (BackgroundWorker)sender;

            e.Result = DownloadService.Run(_config, _logger, delegate(int percent)
            {
                worker.ReportProgress(percent);
            }, token);
        }

        private void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            _header.SetProgress(e.ProgressPercentage);
            SetStatus(StatusKind.Working, Strings.Format(Strings.StatusDownloading, e.ProgressPercentage));
        }

        private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _busy = false;
            _header.SetBusy(false);

            if (_cancellation != null)
            {
                _cancellation.Dispose();
                _cancellation = null;
            }

            RefreshLog();
            _behaviorCard.SetLastUpdate(UpdatedAtText());

            if (e.Error is OperationCanceledException)
            {
                _config.MarkRun(ResultCancelled);
                _config.Save();
                SetStatus(StatusKind.Ready, Strings.StatusCancelled);
                return;
            }

            DownloadFailure failure = e.Error as DownloadFailure;
            if (failure != null)
            {
                MarkFailure();
                ShowBanner(Strings.ErrorDownloadFailed, failure.Message, true);
                SetStatus(StatusKind.Failure, Strings.StatusFailed);
                return;
            }

            if (e.Error != null)
            {
                MarkFailure();
                ShowBanner(Strings.ErrorDownloadFailed, Strings.ErrorUnknown, true);
                SetStatus(StatusKind.Failure, Strings.StatusFailed);
                return;
            }

            DownloadOutcome outcome = (DownloadOutcome)e.Result;
            if (outcome == null)
            {
                MarkFailure();
                ShowBanner(Strings.ErrorDownloadFailed, Strings.ErrorUnknown, true);
                SetStatus(StatusKind.Failure, Strings.StatusFailed);
                return;
            }

            _config.MarkRun(ResultSuccess);
            _config.Save();
            RefreshPreview();
            _behaviorCard.SetLastUpdate(UpdatedAtText());
            SetStatus(StatusKind.Success, DownloadService.StatusText(outcome));
            _successTimer.Stop();
            _successTimer.Start();
        }

        private void MarkFailure()
        {
            _config.MarkRun(ResultFailed);
            _config.Save();
        }

        // ---------------------------------------------------------------- 预览与文件

        private void OnLogExpandedChanged()
        {
            if (!_logCard.Expanded)
            {
                return;
            }

            // 展开后把日志卡片滚进可视区，避免用户以为没反应
            _scroll.PerformLayout();
            _scroll.BringIntoView(_logCard);
        }

        private void RefreshPreview()
        {
            string latest = WallpaperLibrary.FindLatest(_config.DestDir);
            _currentWallpaper = latest;
            WallpaperPreview preview = WallpaperLibrary.Read(latest);
            Bitmap image = latest == null
                ? null
                : ImageLoader.LoadScaled(latest, Ui.S(440), Ui.S(248));
            _previewCard.Update(preview, image);
        }

        private void OpenFolder()
        {
            try
            {
                string dir = _config.DestDir;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!string.IsNullOrEmpty(_currentWallpaper) && File.Exists(_currentWallpaper))
                {
                    System.Diagnostics.Process.Start(
                        "explorer.exe", "/select,\"" + _currentWallpaper + "\"");
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
                }
            }
            catch (Exception error)
            {
                ShowBanner(Strings.ErrorDownloadFailed,
                    Strings.Format(Strings.ErrorOpenFolderFailed, error.Message), false);
            }
        }

        private void ReapplyWallpaper()
        {
            if (string.IsNullOrEmpty(_currentWallpaper) || !File.Exists(_currentWallpaper))
            {
                return;
            }

            if (WallpaperSetter.TryApply(_currentWallpaper))
            {
                _logger.Info(Strings.LogWallpaperSet);
                RefreshLog();
                SetStatus(StatusKind.Success,
                    Strings.Format(Strings.StatusAppliedFromPreview, Path.GetFileName(_currentWallpaper)));
                _successTimer.Stop();
                _successTimer.Start();
            }
            else
            {
                _logger.Warn(Strings.Format(Strings.LogWallpaperFailed, Strings.ErrorUnknown));
                RefreshLog();
                SetStatus(StatusKind.Failure, Strings.StatusFailed);
            }
        }

        private void ChooseFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = Strings.FieldDestDir;
                string current = _sourceCard.DestInput.Value.Trim();
                if (Directory.Exists(current))
                {
                    dialog.SelectedPath = current;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _sourceCard.DestInput.Value = dialog.SelectedPath;
                    _config.DestDir = dialog.SelectedPath;
                    _config.Save();
                    RefreshPreview();
                    SetStatus(StatusKind.Ready, UpdatedAtText());
                }
            }
        }

        // ---------------------------------------------------------------- 行为开关

        private void OnSetWallpaperToggled()
        {
            bool value = _behaviorCard.SetWallpaperChecked;
            _config.SetWallpaperAfterDownload = value;
            _logger.Info(value ? Strings.LogWallpaperSettingOn : Strings.LogWallpaperSettingOff);
            _behaviorCard.SetWallpaperState(value);
            _config.Save();
            RefreshLog();
        }

        private void OnKeepHistoryToggled()
        {
            bool value = _behaviorCard.KeepHistoryChecked;
            _config.KeepHistory = value;
            _logger.Info(value ? Strings.LogKeepHistoryOn : Strings.LogKeepHistoryOff);
            _behaviorCard.SetHistoryState(value);
            _config.Save();
            RefreshLog();
        }

        // ---------------------------------------------------------------- 日志

        private void RefreshLog()
        {
            string text = _logger.ReadTail();
            _logCard.SetLogText(string.IsNullOrEmpty(text) ? Strings.LogEmpty : text);
        }

        private void OpenLogFile()
        {
            try
            {
                _logger.EnsureFile();
                System.Diagnostics.Process.Start(_logger.Path);
            }
            catch (Exception error)
            {
                ShowBanner(Strings.StatusFailed,
                    Strings.Format(Strings.ErrorOpenLogFailed, error.Message), false);
            }
        }

        private void CopyLog()
        {
            try
            {
                string text = _logCard.LogText;
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    SetStatus(StatusKind.Ready, Strings.ActionCopyLog);
                }
            }
            catch
            {
                // 剪贴板被占用时忽略
            }
        }

        // ---------------------------------------------------------------- 横幅

        private void ShowBanner(string title, string message, bool canRetry)
        {
            ClearBanner();

            _banner = new BannerView(title, message, canRetry);
            _banner.Retry += delegate { StartDownload(); };
            _banner.Dismissed += delegate { ClearBanner(); };

            _stack.Controls.Add(_banner);
            _stack.Controls.SetChildIndex(_banner, 0);
            _stack.PerformLayout();
            _scroll.PerformLayout();
        }

        private void ClearBanner()
        {
            if (_banner == null)
            {
                return;
            }

            _stack.Controls.Remove(_banner);
            _banner.Dispose();
            _banner = null;
            _stack.PerformLayout();
            _scroll.PerformLayout();
        }

        // ---------------------------------------------------------------- 校验

        private void ValidateUrlField()
        {
            string error = ValidateUrl(_sourceCard.UrlInput.Value.Trim());
            _sourceCard.SetUrlError(error);
            if (error == null)
            {
                _config.Url = _sourceCard.UrlInput.Value.Trim();
                _config.Save();
            }
        }

        private static string ValidateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return Strings.ErrorUrlEmpty;
            }

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Strings.ErrorUrlInvalid;
            }
            return null;
        }
    }
}
