using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace WallpaperTool
{
    public class MainForm : Form
    {
        private TextBox txtDestDir;
        private Button btnBrowse;
        private TextBox txtUrl;
        private TextBox txtLogFile;
        private Button btnDownload;
        private TextBox txtLog;
        private Label lblStatus;
        
        // 默认值
        private string defaultUrl = "https://picsum.photos/1920/1080";
        private string defaultLogFile = "wallpaper_log.txt";

        // 自动运行标志
        private bool isAutoRun = false;

        public MainForm()
        {
            CheckIfAutoRun();
            InitializeComponent();
            LoadDefaultSettings();
            
            if (isAutoRun)
            {
                this.Shown += (s, e) => {
                    this.Hide();
                    Log("检测到自动运行。正在开始静默下载...");
                    PerformSilentDownload();
                };
            }
        }

        private void CheckIfAutoRun()
        {
            // 检查应用程序是否从启动文件夹运行
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            
            // 同时检查命令行参数 "-silent" 或是否从启动目录运行
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.Equals("-silent", StringComparison.OrdinalIgnoreCase))
                {
                    isAutoRun = true;
                    return;
                }
            }

            // 注意：由于快捷方式的原因，精确的路径匹配可能比较棘手，所以依赖参数更安全。
            // 但是，用户要求基于启动文件夹启动进行检测。
            // 如果 EXE 本身在启动文件夹中（不仅仅是快捷方式），这行得通：
            if (currentPath.TrimEnd('\\').Equals(startupPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                isAutoRun = true;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "壁纸下载工具";
            
            // 基于屏幕分辨率的动态尺寸
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int width = (int)(screen.Width * 0.4); 
            int height = (int)(screen.Height * 0.5);
            
            // 设置最小和最大限制，避免窗口过小或过大
            width = Math.Max(500, Math.Min(width, 800));
            height = Math.Max(600, Math.Min(height, 900));
            
            this.Size = new Size(width, height);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            int padding = 20;
            int contentWidth = width - (padding * 3); // 控件的大致宽度

            // 保存目录
            Label lblDir = new Label();
            lblDir.Text = "保存目录:";
            lblDir.Location = new Point(padding, 20);
            lblDir.AutoSize = true;
            this.Controls.Add(lblDir);

            txtDestDir = new TextBox();
            txtDestDir.Location = new Point(padding, 45);
            txtDestDir.Size = new Size(contentWidth - 90, 25);
            this.Controls.Add(txtDestDir);

            btnBrowse = new Button();
            btnBrowse.Text = "浏览";
            btnBrowse.Location = new Point(padding + contentWidth - 80, 44);
            btnBrowse.Size = new Size(80, 27);
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // 壁纸 URL
            Label lblUrl = new Label();
            lblUrl.Text = "壁纸下载地址 (URL):";
            lblUrl.Location = new Point(padding, 80);
            lblUrl.AutoSize = true;
            this.Controls.Add(lblUrl);

            txtUrl = new TextBox();
            txtUrl.Location = new Point(padding, 105);
            txtUrl.Size = new Size(contentWidth, 25);
            this.Controls.Add(txtUrl);

            // 日志文件名
            Label lblLogFile = new Label();
            lblLogFile.Text = "日志文件名:";
            lblLogFile.Location = new Point(padding, 140);
            lblLogFile.AutoSize = true;
            this.Controls.Add(lblLogFile);

            txtLogFile = new TextBox();
            txtLogFile.Location = new Point(padding, 165);
            txtLogFile.Size = new Size(contentWidth, 25);
            this.Controls.Add(txtLogFile);

            // 下载按钮
            btnDownload = new Button();
            btnDownload.Text = "立即下载壁纸";
            btnDownload.Location = new Point(padding, 210);
            btnDownload.Size = new Size(contentWidth, 50); // 稍大的按钮
            btnDownload.Font = new Font(this.Font, FontStyle.Bold);
            btnDownload.Click += BtnDownload_Click;
            this.Controls.Add(btnDownload);

            // 日志输出
            Label lblLog = new Label();
            lblLog.Text = "运行日志:";
            lblLog.Location = new Point(padding, 280);
            lblLog.AutoSize = true;
            this.Controls.Add(lblLog);

            txtLog = new TextBox();
            txtLog.Location = new Point(padding, 305);
            txtLog.Size = new Size(contentWidth, height - 380); // 动态高度
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            this.Controls.Add(txtLog);

            // 状态栏
            lblStatus = new Label();
            lblStatus.Text = "就绪";
            lblStatus.Location = new Point(padding, height - 60);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);
        }

        private void LoadDefaultSettings()
        {
            string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string savedPictures = Path.Combine(picturesPath, "Saved Pictures");
            txtDestDir.Text = savedPictures;
            
            // 设置 URL 和日志文件的默认值
            txtUrl.Text = defaultUrl;
            txtLogFile.Text = defaultLogFile;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = txtDestDir.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDestDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            DownloadWallpaper(false);
        }

        private void PerformSilentDownload()
        {
            DownloadWallpaper(true);
        }

        private void DownloadWallpaper(bool isSilent)
        {
            if (!isSilent)
            {
                btnDownload.Enabled = false;
                lblStatus.Text = "正在下载...";
            }
            
            Log("开始下载...");

            string destDir = txtDestDir.Text;
            string destFile = Path.Combine(destDir, "wallpaper_Terminal.jpg");
            string downloadUrl = txtUrl.Text.Trim();

            if (string.IsNullOrEmpty(downloadUrl))
            {
                if (!isSilent) MessageBox.Show("请输入有效的壁纸下载地址。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (!isSilent) btnDownload.Enabled = true;
                return;
            }

            try
            {
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                using (WebClient client = new WebClient())
                {
                    // 使用标准 TLS 协议
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    
                    client.DownloadFileCompleted += (s, args) =>
                    {
                        if (args.Error != null)
                        {
                            HandleError(args.Error, isSilent);
                        }
                        else
                        {
                            string msg = string.Format("成功: 壁纸已更新至 {0}", destFile);
                            Log(msg);
                            if (!isSilent)
                            {
                                lblStatus.Text = "完成!";
                                MessageBox.Show("壁纸下载成功!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                btnDownload.Enabled = true;
                            }
                            else
                            {
                                // 静默下载后退出应用程序
                                Application.Exit();
                            }
                        }
                    };

                    client.DownloadFileAsync(new Uri(downloadUrl), destFile);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, isSilent);
                if (!isSilent) btnDownload.Enabled = true;
                else Application.Exit();
            }
        }

        private void HandleError(Exception ex, bool isSilent)
        {
            string errorMsg = string.Format("错误: {0}", ex.Message);
            Log(errorMsg);
            if (!isSilent)
            {
                lblStatus.Text = "错误";
                MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Log(string message)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = string.Format("[{0}] {1}", date, message);
            
            // UI 更新
            if (txtLog != null && !txtLog.IsDisposed)
            {
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => txtLog.AppendText(logEntry + Environment.NewLine)));
                }
                else
                {
                    txtLog.AppendText(logEntry + Environment.NewLine);
                }
            }

            // 文件日志记录
            try
            {
                string logFileName = txtLogFile.Text.Trim();
                if (string.IsNullOrEmpty(logFileName))
                {
                    logFileName = defaultLogFile;
                }
                
                // 如果从 bin 运行，日志在 bin 中。如果从 src (开发) 运行，日志在 src 中。
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch { /* 忽略日志文件错误 */ }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
