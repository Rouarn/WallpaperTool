using System;
using System.Windows.Forms;
using WallpaperTool.Core;
using WallpaperTool.Views;

namespace WallpaperTool.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            NetworkSecurity.EnableModernTls();

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
            Application.AddMessageFilter(new WheelRouter());
            using (MainForm form = new MainForm(config, logger))
            {
                Application.Run(form);
            }
        }
    }
}
