// AUTO-GENERATED FROM design/copy.zh-CN.json - DO NOT EDIT.
// 重新生成: python3 tools/gen_tokens.py
namespace WallpaperTool
{
    public static class Strings
    {
        // 占位符: version
        public const string AboutVersion = "版本 {0}";
        public const string ActionCancel = "取消";
        public const string ActionChooseFolder = "选择…";
        public const string ActionClose = "关闭";
        public const string ActionCopyLog = "复制日志";
        public const string ActionDownloadNow = "立即下载";
        // 占位符: percent
        public const string ActionDownloading = "下载中 {0}%";
        public const string ActionHideLog = "收起日志";
        public const string ActionOpenFolder = "打开文件夹";
        public const string ActionOpenLog = "查看日志";
        public const string ActionOpenLogFile = "打开日志文件";
        public const string ActionReapply = "重新应用";
        public const string ActionResetUrl = "恢复默认";
        public const string ActionRetry = "重试";
        public const string ActionTestNow = "立即测试";
        public const string AppName = "壁纸工具";
        public const string AppTagline = "登录时自动更新桌面壁纸";
        public const string EmptyPreviewBody = "添加图片地址后下载，完成后会自动设为桌面壁纸。";
        public const string EmptyPreviewTitle = "还没有下载过壁纸";
        public const string ErrorDestNotWritable = "保存目录不可写，请更换目录。";
        public const string ErrorDownloadFailed = "下载失败";
        // 占位符: status
        public const string ErrorHttpStatus = "服务器返回 {0}，链接可能已失效。";
        // 占位符: width, height
        public const string ErrorImageTooSmall = "图片尺寸过小（{0} × {1}），至少需要 640 × 480。";
        public const string ErrorNetwork = "无法连接服务器，请检查网络后重试。";
        public const string ErrorNotImage = "下载内容不是图片，可能链接已失效或需要登录。";
        // 占位符: reason
        public const string ErrorOpenFolderFailed = "无法打开文件夹：{0}";
        // 占位符: reason
        public const string ErrorOpenLogFailed = "无法打开日志文件：{0}";
        public const string ErrorRetryHint = "可更换地址或稍后重试。";
        public const string ErrorTimeout = "连接超时，请稍后重试。";
        public const string ErrorUnknown = "发生未知错误，请查看日志。";
        public const string ErrorUrlEmpty = "请输入图片地址。";
        public const string ErrorUrlInvalid = "请输入以 http:// 或 https:// 开头的地址。";
        public const string FieldAutoUpdate = "登录时自动更新";
        public const string FieldDestDir = "保存到";
        public const string FieldLastAutoUpdate = "上次自动更新";
        public const string FieldLogFile = "日志文件";
        public const string FieldUrl = "图片地址";
        public const string HintDesktopSpace = "macOS 的桌面壁纸按空间生效，切换桌面空间可能仍显示旧图。";
        public const string HintLaunchAgentApproval = "首次开启时，系统可能要求你在“系统设置 → 通用 → 登录项”中允许后台项目。";
        public const string HintLogCollapsed = "查看运行日志";
        public const string LogAppStarted = "应用启动";
        public const string LogAutoUpdateDisabled = "已关闭登录时自动更新";
        public const string LogAutoUpdateEnabled = "已开启登录时自动更新";
        public const string LogCancelled = "下载已取消";
        public const string LogConfigMigrated = "已迁移旧版配置";
        // 占位符: path
        public const string LogDownloadDone = "下载完成，已保存到 {0}";
        // 占位符: reason
        public const string LogDownloadFailed = "下载失败：{0}";
        // 占位符: url
        public const string LogDownloadStart = "开始下载 {0}";
        public const string LogEmpty = "暂无日志";
        // 占位符: width, height
        public const string LogImageTooSmall = "图片尺寸过小（{0} × {1}）";
        public const string LogInvalidImage = "下载内容不是有效图片";
        // 占位符: url
        public const string LogInvalidUrl = "下载地址无效：{0}";
        // 占位符: reason
        public const string LogLaunchAgentFailed = "更新登录项失败：{0}";
        // 占位符: path
        public const string LogSaved = "已保存到 {0}";
        public const string LogSilentDone = "静默更新完成";
        // 占位符: reason
        public const string LogSilentFailed = "静默更新失败：{0}";
        public const string LogSilentStart = "静默更新开始";
        public const string LogStartupCreated = "已创建登录启动项";
        public const string LogStartupRemoved = "已移除登录启动项";
        public const string LogTitle = "运行日志";
        // 占位符: reason
        public const string LogWallpaperFailed = "设置桌面壁纸失败：{0}";
        public const string LogWallpaperSet = "已设置桌面壁纸";
        // 占位符: width, height
        public const string MetaDimensions = "{0} × {1}";
        // 占位符: size
        public const string MetaFileSize = "{0}";
        public const string MetaNever = "从未更新";
        public const string MetaUnknownSize = "尺寸未知";
        // 占位符: time
        public const string MetaUpdatedAt = "更新于 {0}";
        public const string SectionAutoUpdate = "自动更新";
        public const string SectionPreview = "预览";
        public const string SectionSource = "下载源";
        public const string StatusCancelled = "已取消下载";
        // 占位符: percent
        public const string StatusDownloading = "正在下载 {0}%";
        public const string StatusDownloadingUnknown = "正在下载…";
        public const string StatusFailed = "下载失败";
        public const string StatusLastAutoFail = "上次自动更新失败 · 查看日志";
        public const string StatusNeverUpdated = "尚未更新";
        public const string StatusReady = "就绪";
        public const string StatusSuccess = "已下载并设为桌面壁纸";
        // 占位符: time
        public const string StatusUpdatedAt = "已更新 · {0}";
        public const string TimeNever = "从未";
        public const string TimeUnknown = "未知";
        public const string TooltipRevealFile = "在文件管理器中显示";

        public static string Format(string template, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args);
        }
    }
}
