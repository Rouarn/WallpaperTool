import Foundation

final class ConfigStore {
    let fileURL: URL

    init(fileURL: URL = ConfigStore.defaultFileURL) {
        self.fileURL = fileURL
    }

    static var defaultFileURL: URL {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        return base.appendingPathComponent("WallpaperTool", isDirectory: true)
            .appendingPathComponent("config.json")
    }

    static func expand(_ path: String) -> String {
        (path as NSString).expandingTildeInPath
    }

    func load() -> AppConfig {
        guard let data = try? Data(contentsOf: fileURL) else {
            let config = AppConfig.defaults()
            save(config)
            return config
        }

        do {
            let config = try JSONDecoder().decode(AppConfig.self, from: data)
            if config.schemaVersion > AppConfig.currentSchemaVersion {
                try? backupBrokenConfig()
                let fallback = AppConfig.defaults()
                save(fallback)
                return fallback
            }
            return config
        } catch {
            // 配置损坏时保留原文件，避免用户数据无声丢失。
            try? backupBrokenConfig()
            let fallback = AppConfig.defaults()
            save(fallback)
            return fallback
        }
    }

    @discardableResult
    func save(_ config: AppConfig) -> Bool {
        let directory = fileURL.deletingLastPathComponent()
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            let data = try encoder.encode(config)
            let temporary = directory.appendingPathComponent(".config.\(UUID().uuidString).tmp")
            try data.write(to: temporary, options: .atomic)
            if FileManager.default.fileExists(atPath: fileURL.path) {
                _ = try FileManager.default.replaceItemAt(fileURL, withItemAt: temporary)
            } else {
                try FileManager.default.moveItem(at: temporary, to: fileURL)
            }
            return true
        } catch {
            return false
        }
    }

    func logURL(for config: AppConfig) -> URL {
        let path = config.logFile.trimmingCharacters(in: .whitespacesAndNewlines)
        let resolved = path.isEmpty ? AppConfig.defaults().logFile : path
        return URL(fileURLWithPath: ConfigStore.expand(resolved))
    }

    private func backupBrokenConfig() throws {
        guard FileManager.default.fileExists(atPath: fileURL.path) else { return }
        let stamp = Int(Date().timeIntervalSince1970)
        let backup = fileURL.deletingLastPathComponent()
            .appendingPathComponent("config.broken.\(stamp).json")
        try FileManager.default.copyItem(at: fileURL, to: backup)
    }
}

enum DownloadTarget {
    static let minimumWidth = 640
    static let minimumHeight = 480

    static func validatedURL(_ raw: String) -> URL? {
        let text = raw.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty,
              let url = URL(string: text),
              let scheme = url.scheme?.lowercased(),
              scheme == "http" || scheme == "https",
              url.host?.isEmpty == false else {
            return nil
        }
        return url
    }

    static func destinationDirectory(_ config: AppConfig) -> URL {
        URL(fileURLWithPath: ConfigStore.expand(config.destDir), isDirectory: true)
    }

    static func findExistingWallpaper(in directory: URL) -> URL? {
        guard let items = try? FileManager.default.contentsOfDirectory(
            at: directory,
            includingPropertiesForKeys: [.contentModificationDateKey, .fileSizeKey],
            options: [.skipsHiddenFiles]
        ) else {
            return nil
        }
        return items
            .filter { $0.lastPathComponent.hasPrefix("wallpaper.") }
            .sorted { $0.lastPathComponent < $1.lastPathComponent }
            .first
    }
}
