// AUTO-GENERATED FROM design/copy.zh-CN.json - DO NOT EDIT.
// 重新生成: python3 tools/gen_tokens.py
enum Strings {
    static func aboutVersion(_ version: String) -> String {
        return "版本 \(version)"
    }
    static let actionCancel = "取消"
    static let actionChooseFolder = "选择…"
    static let actionClose = "关闭"
    static let actionCopyLog = "复制日志"
    static let actionDownloadNow = "立即下载"
    static func actionDownloading(_ percent: String) -> String {
        return "下载中 \(percent)%"
    }
    static let actionHideLog = "收起日志"
    static let actionOpenFolder = "打开文件夹"
    static let actionOpenLog = "查看日志"
    static let actionOpenLogFile = "打开日志文件"
    static let actionOpenLoginItems = "打开登录项设置"
    static let actionOpenMainWindow = "打开主窗口"
    static let actionQuit = "退出"
    static let actionReapply = "设为桌面壁纸"
    static let actionResetUrl = "恢复默认"
    static let actionRetry = "重试"
    static let actionTestNow = "立即测试"
    static let appName = "壁纸工具"
    static let appTagline = "按需下载壁纸，是否设为桌面由你决定"
    static let emptyPreviewBody = "添加图片地址后下载，文件会保存到本地文件夹。"
    static let emptyPreviewTitle = "还没有下载过壁纸"
    static let errorDestNotWritable = "保存目录不可写，请更换目录。"
    static let errorDownloadFailed = "下载失败"
    static func errorHttpStatus(_ status: String) -> String {
        return "服务器返回 \(status)，链接可能已失效。"
    }
    static func errorImageTooSmall(_ width: String, _ height: String) -> String {
        return "图片尺寸过小（\(width) × \(height)），至少需要 640 × 480。"
    }
    static let errorNetwork = "无法连接服务器，请检查网络后重试。"
    static let errorNotImage = "下载内容不是图片，可能链接已失效或需要登录。"
    static func errorOpenFolderFailed(_ reason: String) -> String {
        return "无法打开文件夹：\(reason)"
    }
    static func errorOpenLogFailed(_ reason: String) -> String {
        return "无法打开日志文件：\(reason)"
    }
    static let errorRetryHint = "可更换地址或稍后重试。"
    static func errorSaveFailed(_ reason: String) -> String {
        return "保存图片失败：\(reason)"
    }
    static let errorTimeout = "连接超时，请稍后重试。"
    static let errorUnknown = "发生未知错误，请查看日志。"
    static let errorUrlEmpty = "请输入图片地址。"
    static let errorUrlInvalid = "请输入以 http:// 或 https:// 开头的地址。"
    static let fieldAutoUpdate = "登录时自动更新"
    static let fieldDestDir = "保存到"
    static let fieldKeepHistory = "保留历史壁纸"
    static let fieldLastAutoUpdate = "上次自动更新"
    static let fieldLogFile = "日志文件"
    static let fieldSetWallpaper = "下载后自动设为壁纸"
    static let fieldUrl = "图片地址"
    static let hintDesktopSpace = "macOS 的桌面壁纸按空间生效，切换桌面空间可能仍显示旧图。"
    static let hintKeepHistoryOff = "每次下载覆盖保存目录里的同名文件。"
    static let hintKeepHistoryOn = "每次下载另存为新文件，旧壁纸不会被覆盖。"
    static let hintLaunchAgentApproval = "首次开启时，系统可能要求你在“系统设置 → 通用 → 登录项”中允许后台项目。"
    static let hintLogCollapsed = "查看运行日志"
    static let hintSetWallpaperOff = "关闭时只保存文件，不会改动你的桌面。"
    static let hintSetWallpaperOn = "下载完成后会替换你的桌面壁纸。"
    static let logAppStarted = "应用启动"
    static let logAutoUpdateDisabled = "已关闭登录时自动更新"
    static let logAutoUpdateEnabled = "已开启登录时自动更新"
    static let logCancelled = "下载已取消"
    static let logConfigMigrated = "已迁移旧版配置"
    static func logDownloadDone(_ path: String) -> String {
        return "下载完成，已保存到 \(path)"
    }
    static func logDownloadFailed(_ reason: String) -> String {
        return "下载失败：\(reason)"
    }
    static func logDownloadStart(_ url: String) -> String {
        return "开始下载 \(url)"
    }
    static let logEmpty = "暂无日志"
    static func logImageTooSmall(_ width: String, _ height: String) -> String {
        return "图片尺寸过小（\(width) × \(height)）"
    }
    static let logInvalidImage = "下载内容不是有效图片"
    static func logInvalidUrl(_ url: String) -> String {
        return "下载地址无效：\(url)"
    }
    static let logKeepHistoryOff = "已关闭「保留历史壁纸」"
    static let logKeepHistoryOn = "已开启「保留历史壁纸」"
    static func logLaunchAgentFailed(_ reason: String) -> String {
        return "更新登录项失败：\(reason)"
    }
    static func logSaved(_ path: String) -> String {
        return "已保存到 \(path)"
    }
    static let logSilentDone = "静默更新完成"
    static func logSilentFailed(_ reason: String) -> String {
        return "静默更新失败：\(reason)"
    }
    static let logSilentStart = "静默更新开始"
    static let logStartupCreated = "已创建登录启动项"
    static let logStartupRemoved = "已移除登录启动项"
    static let logTitle = "运行日志"
    static func logWallpaperFailed(_ reason: String) -> String {
        return "设置桌面壁纸失败：\(reason)"
    }
    static let logWallpaperSet = "已设置桌面壁纸"
    static let logWallpaperSettingOff = "已关闭「下载后自动设为壁纸」"
    static let logWallpaperSettingOn = "已开启「下载后自动设为壁纸」"
    static let logWallpaperSkipped = "已跳过设置桌面壁纸（开关关闭）"
    static func metaDimensions(_ width: String, _ height: String) -> String {
        return "\(width) × \(height)"
    }
    static func metaFileSize(_ size: String) -> String {
        return "\(size)"
    }
    static let metaNever = "从未更新"
    static let metaUnknownSize = "尺寸未知"
    static func metaUpdatedAt(_ time: String) -> String {
        return "更新于 \(time)"
    }
    static let sectionAutoUpdate = "自动更新"
    static let sectionBehavior = "下载与保存"
    static let sectionPreview = "预览"
    static let sectionSource = "下载源"
    static func statusAppliedFromPreview(_ name: String) -> String {
        return "已将 \(name) 设为桌面壁纸"
    }
    static let statusCancelled = "已取消下载"
    static func statusDownloading(_ percent: String) -> String {
        return "正在下载 \(percent)%"
    }
    static let statusDownloadingUnknown = "正在下载…"
    static let statusFailed = "下载失败"
    static let statusLastAutoFail = "上次自动更新失败 · 查看日志"
    static let statusNeverUpdated = "尚未更新"
    static let statusReady = "就绪"
    static func statusSavedNewAndSet(_ name: String) -> String {
        return "已另存为 \(name) 并设为桌面壁纸"
    }
    static func statusSavedNewOnly(_ name: String) -> String {
        return "已另存为 \(name)（未改动桌面壁纸）"
    }
    static func statusSavedOverwriteAndSet(_ name: String) -> String {
        return "已覆盖 \(name) 并设为桌面壁纸"
    }
    static func statusSavedOverwriteOnly(_ name: String) -> String {
        return "已覆盖 \(name)（未改动桌面壁纸）"
    }
    static func statusUpdatedAt(_ time: String) -> String {
        return "已更新 · \(time)"
    }
    static let timeNever = "从未"
    static let timeUnknown = "未知"
    static let tooltipRevealFile = "在文件管理器中显示"
}
