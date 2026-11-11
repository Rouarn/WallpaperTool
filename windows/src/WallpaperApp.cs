using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WallpaperTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool silent = Array.IndexOf(args, "-silent") >= 0;

            AppConfig config = AppConfig.Load();
            Logger logger = new Logger(config.ResolveLogPath());

            if (silent)
            {
                Environment.Exit(SilentRunner.Run(config, logger));
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(config, logger));
        }
    }

    /// <summary>用户配置。key=value 文本，缺键回落默认值，未知键忽略，不清空已有设置。</summary>
    internal sealed class AppConfig
    {
        public const string DefaultUrl = "https://picsum.photos/1920/1080";

        public string Url = DefaultUrl;
        public string DestDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WallpaperTool");
        public string LogFile = "wallpaper_log.txt";
        public bool SetWallpaperAfterDownload;
        public bool KeepHistory;

        /// <summary>本次加载是否有新增字段缺失（用于提示配置已升级）。</summary>
        public bool Migrated;

        public static string ConfigDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperTool");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigDir, "config.ini"); }
        }

        public string ResolveLogPath()
        {
            if (Path.IsPathRooted(LogFile))
            {
                return LogFile;
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFile);
        }

        public static AppConfig Load()
        {
            AppConfig config = new AppConfig();
            if (!File.Exists(ConfigPath))
            {
                return config;
            }

            bool sawSetWallpaper = false;
            bool sawKeepHistory = false;

            try
            {
                foreach (string raw in File.ReadAllLines(ConfigPath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    int split = line.IndexOf('=');
                    if (split <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, split).Trim();
                    string value = line.Substring(split + 1).Trim();

                    switch (key)
                    {
                        case "url":
                            if (value.Length > 0) config.Url = value;
                            break;
                        case "destDir":
                            if (value.Length > 0) config.DestDir = value;
                            break;
                        case "logFile":
                            if (value.Length > 0) config.LogFile = value;
                            break;
                        case "setWallpaperAfterDownload":
                            sawSetWallpaper = true;
                            config.SetWallpaperAfterDownload = ParseBool(value);
                            break;
                        case "keepHistory":
                            sawKeepHistory = true;
                            config.KeepHistory = ParseBool(value);
                            break;
                    }
                }
            }
            catch
            {
                // 读失败时保持默认值，不阻断启动
            }

            config.Migrated = !sawSetWallpaper || !sawKeepHistory;
            return config;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# WallpaperTool 配置，缺失的键会回落默认值");
                builder.AppendLine("url=" + Url);
                builder.AppendLine("destDir=" + DestDir);
                builder.AppendLine("logFile=" + LogFile);
                builder.AppendLine("setWallpaperAfterDownload=" + (SetWallpaperAfterDownload ? "true" : "false"));
                builder.AppendLine("keepHistory=" + (KeepHistory ? "true" : "false"));
                File.WriteAllText(ConfigPath, builder.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                // 配置写入失败不影响当前会话
            }
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class Logger
    {
        private const long MaxBytes = 1024 * 1024;
        private readonly string _path;
        private readonly object _gate = new object();

        public Logger(string path)
        {
            _path = path;
        }

        public void Log(string level, string message)
        {
            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                + "] " + level + " " + message + Environment.NewLine;

            lock (_gate)
            {
                try
                {
                    string dir = Path.GetDirectoryName(_path);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (File.Exists(_path) && new FileInfo(_path).Length > MaxBytes)
                    {
                        string backup = _path + ".1";
                        if (File.Exists(backup))
                        {
                            File.Delete(backup);
                        }
                        File.Move(_path, backup);
                    }

                    File.AppendAllText(_path, line, new UTF8Encoding(false));
                }
                catch
                {
                    // 日志失败不影响主流程
                }
            }
        }
    }

    internal static class ImageSniffer
    {
        /// <summary>按文件头判断真实图片格式，返回含点扩展名；无法识别返回 null。</summary>
        public static string DetectExtension(string path)
        {
            byte[] head = new byte[12];
            int read;
            using (FileStream stream = File.OpenRead(path))
            {
                read = stream.Read(head, 0, head.Length);
            }

            if (read >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
            {
                return ".png";
            }
            if (read >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
            {
                return ".jpg";
            }
            if (read >= 6 && head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46)
            {
                return ".gif";
            }
            if (read >= 2 && head[0] == 0x42 && head[1] == 0x4D)
            {
                return ".bmp";
            }
            if (read >= 12 && head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46
                && head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50)
            {
                return ".webp";
            }
            return null;
        }
    }

    /// <summary>仅在用户显式开启开关时调用，默认不会执行。</summary>
    internal static class WallpaperSetter
    {
        private const int SpiSetDeskWallpaper = 20;
        private const int SpifUpdateIniFile = 0x01;
        private const int SpifSendChange = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(int action, int param, string value, int winIni);

        public static bool TryApply(string path)
        {
            try
            {
                int result = SystemParametersInfo(
                    SpiSetDeskWallpaper, 0, path, SpifUpdateIniFile | SpifSendChange);
                return result != 0;
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class DownloadOutcome
    {
        public string FileName;
        public string FullPath;
        public bool Replaced;
        public bool WallpaperApplied;
    }

    internal static class DownloadService
    {
        /// <summary>下载 → 校验格式 → 按开关决定另存或覆盖 → 按开关决定是否设为壁纸。</summary>
        public static DownloadOutcome Run(AppConfig config, Logger logger, Action<int> progress)
        {
            logger.Log("INFO", Strings.Format(Strings.LogDownloadStart, config.Url));

            string dir = config.DestDir;
            Directory.CreateDirectory(dir);

            string staging = Path.Combine(dir, ".wallpaper-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (WebClient client = new WebClient())
                {
                    if (progress != null)
                    {
                        client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                        {
                            progress(e.ProgressPercentage);
                        };
                    }
                    client.DownloadFile(config.Url, staging);
                }

                string extension = ImageSniffer.DetectExtension(staging);
                if (extension == null)
                {
                    throw new InvalidDataException(Strings.ErrorNotImage);
                }

                DownloadOutcome outcome = new DownloadOutcome();
                string destination;

                if (config.KeepHistory)
                {
                    destination = UniqueHistoryDestination(dir, extension);
                }
                else
                {
                    destination = Path.Combine(dir, "wallpaper" + extension);
                    if (File.Exists(destination))
                    {
                        outcome.Replaced = true;
                        File.Delete(destination);
                    }
                    RemoveOtherWallpaperFiles(dir, Path.GetFileName(destination));
                }

                File.Move(staging, destination);
                outcome.FileName = Path.GetFileName(destination);
                outcome.FullPath = destination;

                if (config.SetWallpaperAfterDownload)
                {
                    if (WallpaperSetter.TryApply(destination))
                    {
                        outcome.WallpaperApplied = true;
                        logger.Log("INFO", Strings.LogWallpaperSet);
                    }
                    else
                    {
                        logger.Log("WARN", Strings.Format(Strings.LogWallpaperFailed, Strings.ErrorUnknown));
                    }
                }
                else
                {
                    logger.Log("INFO", Strings.LogWallpaperSkipped);
                }

                logger.Log("INFO", Strings.Format(Strings.LogSaved, destination));
                return outcome;
            }
            finally
            {
                if (File.Exists(staging))
                {
                    try
                    {
                        File.Delete(staging);
                    }
                    catch
                    {
                        // 清理失败可忽略
                    }
                }
            }
        }

        private static string UniqueHistoryDestination(string dir, string extension)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            int index = 1;
            while (true)
            {
                string suffix = index == 1 ? string.Empty : "-" + index.ToString(CultureInfo.InvariantCulture);
                string candidate = Path.Combine(dir, "wallpaper-" + stamp + suffix + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
                index++;
            }
        }

        /// <summary>覆盖模式才清理扩展名变化留下的旧文件；保留历史模式不动任何历史文件。</summary>
        private static void RemoveOtherWallpaperFiles(string dir, string keepFileName)
        {
            try
            {
                foreach (string item in Directory.GetFiles(dir, "wallpaper.*"))
                {
                    if (!string.Equals(Path.GetFileName(item), keepFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(item);
                        }
                        catch
                        {
                            // 单个文件删除失败不阻断
                        }
                    }
                }
            }
            catch
            {
                // 目录枚举失败不阻断
            }
        }

        public static string StatusText(DownloadOutcome outcome)
        {
            if (outcome.WallpaperApplied)
            {
                return outcome.Replaced
                    ? Strings.Format(Strings.StatusSavedOverwriteAndSet, outcome.FileName)
                    : Strings.Format(Strings.StatusSavedNewAndSet, outcome.FileName);
            }
            return outcome.Replaced
                ? Strings.Format(Strings.StatusSavedOverwriteOnly, outcome.FileName)
                : Strings.Format(Strings.StatusSavedNewOnly, outcome.FileName);
        }
    }

    internal static class SilentRunner
    {
        public static int Run(AppConfig config, Logger logger)
        {
            logger.Log("INFO", Strings.LogSilentStart);
            try
            {
                DownloadOutcome outcome = DownloadService.Run(config, logger, null);
                logger.Log("INFO", Strings.LogSilentDone);
                config.Save();
                return outcome == null ? 1 : 0;
            }
            catch (Exception error)
            {
                logger.Log("ERROR", Strings.Format(Strings.LogSilentFailed, error.Message));
                return 1;
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;

        private readonly TextBox _urlBox = new TextBox();
        private readonly TextBox _destBox = new TextBox();
        private readonly TextBox _logFileBox = new TextBox();
        private readonly CheckBox _setWallpaperCheck = new CheckBox();
        private readonly Label _setWallpaperHint = new Label();
        private readonly CheckBox _keepHistoryCheck = new CheckBox();
        private readonly Label _keepHistoryHint = new Label();
        private readonly Button _downloadButton = new Button();
        private readonly Label _statusLabel = new Label();
        private readonly TextBox _logView = new TextBox();
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private bool _running;

        public MainForm(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;

            BuildUi();
            LoadConfigIntoUi();

            _logger.Log("INFO", Strings.LogAppStarted);
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += OnWork;
            _worker.ProgressChanged += OnProgress;
            _worker.RunWorkerCompleted += OnCompleted;
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = Strings.AppName;
            BackColor = TokenColor(Theme.Palette.BgCanvasLight);
            ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            Font = BodyFont();
            ClientSize = new Size((int)Theme.Size.WindowWidth, (int)Theme.Size.WindowHeight);
            MinimumSize = new Size((int)Theme.Size.WindowMinWidth + 16, (int)Theme.Size.WindowMinHeight + 39);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            int pad = (int)Theme.Space.Xl;
            int gap = (int)Theme.Space.M;

            // 标题区
            Label title = new Label();
            title.Text = Strings.AppName;
            title.Font = MakeFont(Theme.Typography.WindowTitleFontFamily,
                Theme.Typography.WindowTitleFontSize, FontStyle.Bold);
            title.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            title.AutoSize = true;
            title.Location = new Point(pad, pad);
            Controls.Add(title);

            Label tagline = new Label();
            tagline.Text = Strings.AppTagline;
            tagline.Font = CaptionFont();
            tagline.ForeColor = TokenColor(Theme.Palette.TextSecondaryLight);
            tagline.AutoSize = true;
            tagline.Location = new Point(pad, title.Bottom + (int)Theme.Space.Xs);
            Controls.Add(tagline);

            int y = tagline.Bottom + gap;

            y = AddTextField(FieldUrlLabel(), _urlBox, pad, y, true);
            y = AddTextField(FieldDestLabel(), _destBox, pad, y, false);

            Button chooseButton = new Button();
            chooseButton.Text = Strings.ActionChooseFolder;
            chooseButton.FlatStyle = FlatStyle.Flat;
            chooseButton.FlatAppearance.BorderColor = TokenColor(Theme.Palette.StrokeStrongLight);
            chooseButton.FlatAppearance.BorderSize = 1;
            chooseButton.BackColor = TokenColor(Theme.Palette.BgSurfaceLight);
            chooseButton.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            chooseButton.Font = CaptionFont();
            chooseButton.Height = _destBox.Height;
            chooseButton.Width = 84;
            chooseButton.Location = new Point(ClientSize.Width - pad - 84, _destBox.Top);
            chooseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chooseButton.Click += OnChooseFolder;
            Controls.Add(chooseButton);

            y = AddTextField(FieldLogFileLabel(), _logFileBox, pad, y, true);

            y += (int)Theme.Space.Xs;
            y = AddToggle(FieldSetWallpaper(), _setWallpaperCheck, _setWallpaperHint, pad, y);
            _setWallpaperCheck.CheckedChanged += OnSetWallpaperChanged;

            y += (int)Theme.Space.S;
            y = AddToggle(FieldKeepHistory(), _keepHistoryCheck, _keepHistoryHint, pad, y);
            _keepHistoryCheck.CheckedChanged += OnKeepHistoryChanged;

            y += (int)Theme.Space.M;

            _downloadButton.Text = Strings.ActionDownloadNow;
            _downloadButton.Font = MakeFont(Theme.Typography.BodyStrongFontFamily,
                Theme.Typography.BodyStrongFontSize, FontStyle.Bold);
            _downloadButton.BackColor = TokenColor(Theme.Palette.AccentDefaultLight);
            _downloadButton.ForeColor = TokenColor(Theme.Palette.AccentOnAccentLight);
            _downloadButton.FlatStyle = FlatStyle.Flat;
            _downloadButton.FlatAppearance.BorderSize = 0;
            _downloadButton.FlatAppearance.MouseOverBackColor = TokenColor(Theme.Palette.AccentHoverLight);
            _downloadButton.Height = (int)Theme.Size.PrimaryButtonHeight;
            _downloadButton.Width = 132;
            _downloadButton.Location = new Point(pad, y);
            _downloadButton.Click += OnDownloadClicked;
            Controls.Add(_downloadButton);

            Button openFolder = new Button();
            openFolder.Text = Strings.ActionOpenFolder;
            openFolder.Font = CaptionFont();
            openFolder.FlatStyle = FlatStyle.Flat;
            openFolder.FlatAppearance.BorderColor = TokenColor(Theme.Palette.StrokeDefaultLight);
            openFolder.FlatAppearance.BorderSize = 1;
            openFolder.BackColor = TokenColor(Theme.Palette.BgSurfaceLight);
            openFolder.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            openFolder.Height = (int)Theme.Size.PrimaryButtonHeight;
            openFolder.Width = 104;
            openFolder.Location = new Point(_downloadButton.Right + (int)Theme.Space.S, y);
            openFolder.Click += OnOpenFolder;
            Controls.Add(openFolder);

            _statusLabel.Text = Strings.StatusReady;
            _statusLabel.Font = CaptionFont();
            _statusLabel.ForeColor = TokenColor(Theme.Palette.TextSecondaryLight);
            _statusLabel.AutoSize = false;
            _statusLabel.Height = 18;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Location = new Point(pad, _downloadButton.Bottom + (int)Theme.Space.S);
            _statusLabel.Width = ClientSize.Width - pad * 2;
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_statusLabel);

            // 日志区
            Label logTitle = new Label();
            logTitle.Text = Strings.LogTitle;
            logTitle.Font = MakeFont(Theme.Typography.SectionTitleFontFamily,
                Theme.Typography.SectionTitleFontSize, FontStyle.Bold);
            logTitle.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            logTitle.AutoSize = true;
            logTitle.Location = new Point(pad, _statusLabel.Bottom + gap);
            Controls.Add(logTitle);

            Button copyLog = new Button();
            copyLog.Text = Strings.ActionCopyLog;
            copyLog.Font = CaptionFont();
            copyLog.FlatStyle = FlatStyle.Flat;
            copyLog.FlatAppearance.BorderColor = TokenColor(Theme.Palette.StrokeDefaultLight);
            copyLog.FlatAppearance.BorderSize = 1;
            copyLog.BackColor = TokenColor(Theme.Palette.BgSurfaceLight);
            copyLog.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            copyLog.Height = 26;
            copyLog.Width = 88;
            copyLog.Location = new Point(ClientSize.Width - pad - 88, logTitle.Top - 3);
            copyLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            copyLog.Click += OnCopyLog;
            Controls.Add(copyLog);

            _logView.Multiline = true;
            _logView.ReadOnly = true;
            _logView.ScrollBars = ScrollBars.Vertical;
            _logView.BackColor = TokenColor(Theme.Palette.BgSurfaceSubtleLight);
            _logView.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            _logView.BorderStyle = BorderStyle.FixedSingle;
            _logView.Font = MakeFont(Theme.Typography.MonoFontFamily,
                Theme.Typography.MonoFontSize, FontStyle.Regular);
            _logView.WordWrap = false;
            _logView.Location = new Point(pad, logTitle.Bottom + (int)Theme.Space.S);
            _logView.Size = new Size(ClientSize.Width - pad * 2,
                (int)Theme.Size.LogPanelHeight);
            _logView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_logView);

            ResumeLayout(false);
            PerformLayout();
        }

        private int AddTextField(string caption, TextBox box, int pad, int y, bool required)
        {
            Label label = new Label();
            label.Text = caption;
            label.Font = CaptionFont();
            label.ForeColor = TokenColor(Theme.Palette.TextSecondaryLight);
            label.AutoSize = true;
            label.Location = new Point(pad, y);
            Controls.Add(label);

            box.Font = BodyFont();
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = TokenColor(Theme.Palette.BgSurfaceLight);
            box.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            box.Location = new Point(pad, label.Bottom + (int)Theme.Space.Xs);
            box.Width = ClientSize.Width - pad * 2 - (required ? 0 : 96);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            box.Height = (int)Theme.Size.ControlHeight;
            Controls.Add(box);

            return box.Bottom + (int)Theme.Space.S;
        }

        private int AddToggle(string caption, CheckBox check, Label hint, int pad, int y)
        {
            check.Text = caption;
            check.Font = BodyFont();
            check.ForeColor = TokenColor(Theme.Palette.TextPrimaryLight);
            check.AutoSize = true;
            check.Location = new Point(pad, y);
            Controls.Add(check);

            hint.Font = CaptionFont();
            hint.ForeColor = TokenColor(Theme.Palette.TextSecondaryLight);
            hint.AutoSize = true;
            hint.Location = new Point(pad + 20, check.Bottom + 2);
            Controls.Add(hint);

            return hint.Bottom + (int)Theme.Space.Xs;
        }

        private void LoadConfigIntoUi()
        {
            _urlBox.Text = _config.Url;
            _destBox.Text = _config.DestDir;
            _logFileBox.Text = _config.LogFile;

            _setWallpaperCheck.Checked = _config.SetWallpaperAfterDownload;
            _keepHistoryCheck.Checked = _config.KeepHistory;
            UpdateHints();

            if (_config.Migrated)
            {
                _logger.Log("INFO", Strings.LogConfigMigrated);
                _config.Save();
            }
        }

        private void UpdateHints()
        {
            _setWallpaperHint.Text = _config.SetWallpaperAfterDownload
                ? Strings.HintSetWallpaperOn
                : Strings.HintSetWallpaperOff;
            _keepHistoryHint.Text = _config.KeepHistory
                ? Strings.HintKeepHistoryOn
                : Strings.HintKeepHistoryOff;
        }

        private void OnChooseFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = Strings.FieldDestDir;
                if (Directory.Exists(_destBox.Text))
                {
                    dialog.SelectedPath = _destBox.Text;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _destBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOpenFolder(object sender, EventArgs e)
        {
            try
            {
                string dir = _destBox.Text.Trim();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception error)
            {
                _statusLabel.Text = Strings.Format(Strings.ErrorOpenFolderFailed, error.Message);
            }
        }

        private void OnSetWallpaperChanged(object sender, EventArgs e)
        {
            _config.SetWallpaperAfterDownload = _setWallpaperCheck.Checked;
            _logger.Log("INFO", _config.SetWallpaperAfterDownload
                ? Strings.LogWallpaperSettingOn
                : Strings.LogWallpaperSettingOff);
            UpdateHints();
            _config.Save();
        }

        private void OnKeepHistoryChanged(object sender, EventArgs e)
        {
            _config.KeepHistory = _keepHistoryCheck.Checked;
            _logger.Log("INFO", _config.KeepHistory
                ? Strings.LogKeepHistoryOn
                : Strings.LogKeepHistoryOff);
            UpdateHints();
            _config.Save();
        }

        private void OnCopyLog(object sender, EventArgs e)
        {
            try
            {
                if (_logView.TextLength > 0)
                {
                    Clipboard.SetText(_logView.Text);
                }
            }
            catch
            {
                // 剪贴板被占用时忽略
            }
        }

        private void OnDownloadClicked(object sender, EventArgs e)
        {
            if (_running)
            {
                return;
            }

            _config.Url = _urlBox.Text.Trim();
            _config.DestDir = _destBox.Text.Trim();
            _config.LogFile = _logFileBox.Text.Trim();

            if (_config.Url.Length == 0)
            {
                _statusLabel.Text = Strings.ErrorUrlEmpty;
                return;
            }

            _config.Save();

            _running = true;
            _downloadButton.Enabled = false;
            _downloadButton.Text = Strings.StatusDownloadingUnknown;
            _statusLabel.Text = Strings.StatusDownloadingUnknown;
            _statusLabel.ForeColor = TokenColor(Theme.Palette.TextSecondaryLight);

            AppConfig snapshot = _config;
            _worker.RunWorkerAsync(snapshot);
        }

        private void OnWork(object sender, DoWorkEventArgs e)
        {
            AppConfig config = (AppConfig)e.Argument;
            BackgroundWorker worker = (BackgroundWorker)sender;

            try
            {
                e.Result = DownloadService.Run(config, _logger, delegate(int percent)
                {
                    worker.ReportProgress(percent);
                });
            }
            catch (Exception error)
            {
                e.Result = error;
            }
        }

        private void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            string text = Strings.Format(Strings.StatusDownloading, e.ProgressPercentage);
            _statusLabel.Text = text;
            _downloadButton.Text = text;
        }

        private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _running = false;
            _downloadButton.Enabled = true;
            _downloadButton.Text = Strings.ActionDownloadNow;

            if (e.Result is Exception)
            {
                Exception error = (Exception)e.Result;
                _statusLabel.Text = Strings.Format(Strings.LogDownloadFailed, error.Message);
                _statusLabel.ForeColor = TokenColor(Theme.Palette.StatusDangerLight);
                AppendLogView(Strings.Format(Strings.LogDownloadFailed, error.Message));
                return;
            }

            DownloadOutcome outcome = (DownloadOutcome)e.Result;
            _statusLabel.Text = DownloadService.StatusText(outcome);
            _statusLabel.ForeColor = TokenColor(Theme.Palette.StatusSuccessLight);
            AppendLogView(Strings.Format(Strings.LogSaved, outcome.FullPath));
        }

        private void AppendLogView(string text)
        {
            _logView.AppendText("[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                + "] " + text + Environment.NewLine);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _config.Save();
            base.OnFormClosed(e);
        }

        private static string FieldUrlLabel()
        {
            return Strings.FieldUrl;
        }

        private static string FieldDestLabel()
        {
            return Strings.FieldDestDir;
        }

        private static string FieldLogFileLabel()
        {
            return Strings.FieldLogFile;
        }

        private static string FieldSetWallpaper()
        {
            return Strings.FieldSetWallpaper;
        }

        private static string FieldKeepHistory()
        {
            return Strings.FieldKeepHistory;
        }

        private static Color TokenColor(uint argb)
        {
            return Color.FromArgb(unchecked((int)argb));
        }

        private static Font BodyFont()
        {
            return MakeFont(Theme.Typography.BodyFontFamily,
                Theme.Typography.BodyFontSize, FontStyle.Regular);
        }

        private static Font CaptionFont()
        {
            return MakeFont(Theme.Typography.CaptionFontFamily,
                Theme.Typography.CaptionFontSize, FontStyle.Regular);
        }

        private static Font MakeFont(string familyList, double size, FontStyle style)
        {
            string family = familyList;
            int comma = family.IndexOf(',');
            if (comma > 0)
            {
                family = family.Substring(0, comma).Trim();
            }

            try
            {
                return new Font(family, (float)size, style);
            }
            catch
            {
                return new Font(FontFamily.GenericSansSerif, (float)size, style);
            }
        }
    }
}
