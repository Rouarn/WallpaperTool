import Foundation

struct AppConfig: Codable, Equatable {
    var schemaVersion: Int
    var url: String
    var destDir: String
    var logFile: String
    var autoUpdate: Bool
    var setWallpaperAfterDownload: Bool
    var keepHistory: Bool
    var lastRunAt: String
    var lastResult: String

    static let currentSchemaVersion = 2

    static func defaults() -> AppConfig {
        AppConfig(
            schemaVersion: currentSchemaVersion,
            url: "https://picsum.photos/1920/1080",
            destDir: "~/Pictures/WallpaperTool",
            logFile: "~/Library/Logs/WallpaperTool/wallpaper_log.txt",
            autoUpdate: false,
            setWallpaperAfterDownload: false,
            keepHistory: false,
            lastRunAt: "",
            lastResult: ""
        )
    }

    init(schemaVersion: Int, url: String, destDir: String, logFile: String,
         autoUpdate: Bool, setWallpaperAfterDownload: Bool, keepHistory: Bool,
         lastRunAt: String, lastResult: String) {
        self.schemaVersion = schemaVersion
        self.url = url
        self.destDir = destDir
        self.logFile = logFile
        self.autoUpdate = autoUpdate
        self.setWallpaperAfterDownload = setWallpaperAfterDownload
        self.keepHistory = keepHistory
        self.lastRunAt = lastRunAt
        self.lastResult = lastResult
    }

    // 容忍旧配置缺键：缺失字段回落到默认值，避免升级后无法启动。
    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        let fallback = AppConfig.defaults()
        let storedVersion = try container.decodeIfPresent(Int.self, forKey: .schemaVersion) ?? 0
        // v1 曾把「下载后设为壁纸」默认为 true；升级到 v2 时强制关闭，避免沿用旧默认继续改桌面。
        schemaVersion = AppConfig.currentSchemaVersion
        url = try container.decodeIfPresent(String.self, forKey: .url) ?? fallback.url
        destDir = try container.decodeIfPresent(String.self, forKey: .destDir) ?? fallback.destDir
        logFile = try container.decodeIfPresent(String.self, forKey: .logFile) ?? fallback.logFile
        autoUpdate = try container.decodeIfPresent(Bool.self, forKey: .autoUpdate) ?? fallback.autoUpdate
        setWallpaperAfterDownload = storedVersion >= 2
            ? (try container.decodeIfPresent(Bool.self, forKey: .setWallpaperAfterDownload) ?? false)
            : false
        // 新增字段：旧配置缺键时回落到 false，不影响其余已保存设置。
        keepHistory = try container.decodeIfPresent(Bool.self, forKey: .keepHistory) ?? false
        lastRunAt = try container.decodeIfPresent(String.self, forKey: .lastRunAt) ?? ""
        lastResult = try container.decodeIfPresent(String.self, forKey: .lastResult) ?? ""
    }
}

struct PreviewInfo {
    let url: URL
    let width: Int
    let height: Int
    let byteSize: Int64
    let modifiedAt: Date?
}

struct UpdateResult {
    let fileURL: URL
    let width: Int
    let height: Int
    let byteSize: Int64
    /// 是否真正改动了桌面壁纸；关闭开关时为 false。
    let wallpaperApplied: Bool
    /// 是否覆盖了同名旧文件；保留历史模式或首次保存时为 false。
    let replacedExisting: Bool
}

enum RunResult: String {
    case success
    case failed
    case cancelled
}

struct AppError: Error {
    enum Kind {
        case invalidURL
        case httpStatus
        case network
        case timeout
        case notImage
        case tooSmall
        case destNotWritable
        case cancelled
        case fileSystem
    }

    let kind: Kind
    let reason: String

    var isCancellation: Bool { kind == .cancelled }
}
