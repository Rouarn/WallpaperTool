using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using WallpaperTool;

namespace WallpaperTool.Core
{
    /// <summary>一次下载的结果，界面据此刷新预览与状态文案。</summary>
    internal sealed class DownloadOutcome
    {
        public string FileName;
        public string FullPath;
        public bool Replaced;
        public bool WallpaperApplied;
        public int Width;
        public int Height;
        public long ByteSize;
        public DateTime ModifiedAt;
    }

    /// <summary>带用户可读原因的下载失败；Message 直接可以显示。</summary>
    internal sealed class DownloadFailure : Exception
    {
        public DownloadFailure(string reason)
            : base(reason)
        {
        }
    }

    /// <summary>下载 → 校验格式与尺寸 → 按开关另存或覆盖 → 按开关设为壁纸。</summary>
    internal static class DownloadService
    {
        private const int MinWidth = 640;
        private const int MinHeight = 480;

        public static DownloadOutcome Run(
            AppConfig config, Logger logger, Action<int> progress, CancellationToken token)
        {
            NetworkSecurity.EnableModernTls();
            logger.Info(Strings.Format(Strings.LogDownloadStart, config.Url));

            string dir = config.DestDir;
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                throw new DownloadFailure(Strings.ErrorDestNotWritable);
            }

            string staging = Path.Combine(dir, ".wallpaper-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (WebClient client = new WebClient())
                {
                    DownloadToFile(client, config.Url, staging, progress, token);
                }

                token.ThrowIfCancellationRequested();

                string extension = ImageSniffer.DetectExtension(staging);
                if (extension == null)
                {
                    throw new DownloadFailure(Strings.ErrorNotImage);
                }

                int width;
                int height;
                if (!ImageSniffer.TryReadDimensions(staging, out width, out height))
                {
                    throw new DownloadFailure(Strings.ErrorNotImage);
                }
                if (width < MinWidth || height < MinHeight)
                {
                    logger.Warn(Strings.Format(Strings.LogImageTooSmall, width, height));
                    throw new DownloadFailure(
                        Strings.Format(Strings.ErrorImageTooSmall, width, height));
                }

                DownloadOutcome outcome = new DownloadOutcome();
                outcome.Width = width;
                outcome.Height = height;

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

                try
                {
                    File.Move(staging, destination);
                }
                catch (Exception error)
                {
                    throw new DownloadFailure(Strings.Format(Strings.ErrorSaveFailed, error.Message));
                }

                outcome.FileName = Path.GetFileName(destination);
                outcome.FullPath = destination;
                try
                {
                    FileInfo info = new FileInfo(destination);
                    outcome.ByteSize = info.Length;
                    outcome.ModifiedAt = info.LastWriteTime;
                }
                catch
                {
                    outcome.ModifiedAt = DateTime.Now;
                }

                if (config.SetWallpaperAfterDownload)
                {
                    if (WallpaperSetter.TryApply(destination))
                    {
                        outcome.WallpaperApplied = true;
                        logger.Info(Strings.LogWallpaperSet);
                    }
                    else
                    {
                        logger.Warn(Strings.Format(Strings.LogWallpaperFailed, Strings.ErrorUnknown));
                    }
                }
                else
                {
                    logger.Info(Strings.LogWallpaperSkipped);
                }

                logger.Info(Strings.Format(Strings.LogSaved, destination));
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

        private static void DownloadToFile(
            WebClient client, string url, string path, Action<int> progress, CancellationToken token)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new DownloadFailure(Strings.ErrorUrlInvalid);
            }

            using (ManualResetEventSlim completed = new ManualResetEventSlim(false))
            {
                Exception failure = null;
                bool cancelled = false;

                client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
                {
                    if (progress != null)
                    {
                        progress(e.ProgressPercentage);
                    }
                };

                client.DownloadFileCompleted += delegate(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
                {
                    if (e.Cancelled)
                    {
                        cancelled = true;
                    }
                    else
                    {
                        failure = e.Error;
                    }
                    completed.Set();
                };

                using (token.Register(delegate { client.CancelAsync(); }))
                {
                    try
                    {
                        client.DownloadFileAsync(uri, path);
                    }
                    catch (Exception error)
                    {
                        throw Translate(error);
                    }
                    completed.Wait();
                }

                if (token.IsCancellationRequested || cancelled)
                {
                    throw new OperationCanceledException(token);
                }
                if (failure != null)
                {
                    throw Translate(failure);
                }
            }
        }

        /// <summary>把网络异常翻译成「结论 + 原因 + 下一步」的中文原因。</summary>
        private static Exception Translate(Exception error)
        {
            if (error is OperationCanceledException || error is DownloadFailure)
            {
                return error;
            }

            WebException web = error as WebException;
            if (web != null)
            {
                HttpWebResponse response = web.Response as HttpWebResponse;
                if (response != null)
                {
                    int status = (int)response.StatusCode;
                    return new DownloadFailure(Strings.Format(Strings.ErrorHttpStatus, status));
                }

                switch (web.Status)
                {
                    case WebExceptionStatus.Timeout:
                        return new DownloadFailure(Strings.ErrorTimeout);
                    case WebExceptionStatus.NameResolutionFailure:
                    case WebExceptionStatus.ProxyNameResolutionFailure:
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ConnectionClosed:
                        return new DownloadFailure(Strings.ErrorNetwork);
                    default:
                        return new DownloadFailure(Strings.ErrorNetwork);
                }
            }

            if (error is UriFormatException)
            {
                return new DownloadFailure(Strings.ErrorUrlInvalid);
            }

            return new DownloadFailure(Strings.ErrorUnknown);
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
    }
}
