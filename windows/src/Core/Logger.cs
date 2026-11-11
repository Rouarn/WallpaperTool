using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace WallpaperTool.Core
{
    /// <summary>单文件 1 MB 轮转的运行日志，写失败不影响主流程。</summary>
    internal sealed class Logger
    {
        private const long MaxBytes = 1024 * 1024;
        private const int MaxViewLines = 400;

        private readonly string _path;
        private readonly object _gate = new object();

        public Logger(string path)
        {
            _path = path;
        }

        public string Path
        {
            get { return _path; }
        }

        public void Log(string level, string message)
        {
            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                + "] " + level + " " + message + Environment.NewLine;

            lock (_gate)
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(_path);
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

        public void Info(string message)
        {
            Log("INFO", message);
        }

        public void Warn(string message)
        {
            Log("WARN", message);
        }

        public void Error(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>读取日志末尾若干行，供界面日志面板显示；读不到时返回空串。</summary>
        public string ReadTail()
        {
            lock (_gate)
            {
                try
                {
                    if (!File.Exists(_path))
                    {
                        return string.Empty;
                    }

                    string[] lines = File.ReadAllLines(_path, Encoding.UTF8);
                    int start = Math.Max(0, lines.Length - MaxViewLines);
                    StringBuilder builder = new StringBuilder();
                    for (int i = start; i < lines.Length; i++)
                    {
                        if (i > start)
                        {
                            builder.Append(Environment.NewLine);
                        }
                        builder.Append(lines[i]);
                    }
                    return builder.ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>确保日志文件存在，供「打开日志文件」使用。</summary>
        public bool EnsureFile()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(_path))
                {
                    File.WriteAllText(_path, string.Empty, new UTF8Encoding(false));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
