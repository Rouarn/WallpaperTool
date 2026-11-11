using System;
using System.Threading;
using WallpaperTool;

namespace WallpaperTool.Core
{
    /// <summary>开机静默更新入口：不弹窗，只写日志与配置，返回进程退出码。</summary>
    internal static class SilentRunner
    {
        public static int Run(AppConfig config, Logger logger)
        {
            logger.Info(Strings.LogSilentStart);
            try
            {
                DownloadOutcome outcome = DownloadService.Run(
                    config, logger, null, CancellationToken.None);

                config.MarkRun("success");
                config.Save();
                logger.Info(Strings.LogSilentDone);
                return outcome == null ? 1 : 0;
            }
            catch (Exception error)
            {
                config.MarkRun("failed");
                config.Save();
                logger.Error(Strings.Format(Strings.LogSilentFailed, error.Message));
                return 1;
            }
        }
    }
}
