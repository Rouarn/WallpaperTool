import Foundation

final class Logger {
    enum Level: String {
        case info = "INFO"
        case warning = "WARN"
        case error = "ERROR"
    }

    static let shared = Logger()

    private let lock = NSLock()
    private let maximumBytes: Int64 = 1024 * 1024
    private let maximumBackups = 3
    private let maximumRecentLines = 300
    private var fileURL: URL
    private var recentLines: [String] = []
    private let formatter: DateFormatter

    var onChange: (([String]) -> Void)?

    init(fileURL: URL? = nil) {
        self.fileURL = fileURL ?? Logger.defaultFileURL
        formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyy-MM-dd HH:mm:ss"
        reloadRecentLines()
    }

    static var defaultFileURL: URL {
        let base = FileManager.default.urls(for: .libraryDirectory, in: .userDomainMask)[0]
        return base.appendingPathComponent("Logs", isDirectory: true)
            .appendingPathComponent("WallpaperTool", isDirectory: true)
            .appendingPathComponent("wallpaper_log.txt")
    }

    func updateFileURL(_ url: URL) {
        lock.lock()
        let changed = url != fileURL
        if changed {
            fileURL = url
        }
        lock.unlock()
        if changed {
            reloadRecentLines()
        }
    }

    func log(_ level: Level, _ message: String) {
        let clean = message.replacingOccurrences(of: "\n", with: " ")

        lock.lock()
        let line = "[\(formatter.string(from: Date()))] \(level.rawValue) \(clean)"
        recentLines.append(line)
        if recentLines.count > maximumRecentLines {
            recentLines.removeFirst(recentLines.count - maximumRecentLines)
        }
        let snapshot = recentLines
        writeLocked(line)
        lock.unlock()

        DispatchQueue.main.async { [weak self] in
            self?.onChange?(snapshot)
        }
    }

    func currentLines() -> [String] {
        lock.lock()
        defer { lock.unlock() }
        return recentLines
    }

    func logFilePath() -> URL {
        lock.lock()
        defer { lock.unlock() }
        return fileURL
    }

    private func writeLocked(_ line: String) {
        let directory = fileURL.deletingLastPathComponent()
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            rotateIfNeededLocked()
            let data = Data((line + "\n").utf8)
            if FileManager.default.fileExists(atPath: fileURL.path) {
                let handle = try FileHandle(forWritingTo: fileURL)
                defer { try? handle.close() }
                try handle.seekToEnd()
                try handle.write(contentsOf: data)
            } else {
                try data.write(to: fileURL, options: .atomic)
            }
        } catch {
            // 日志失败不影响主流程。
        }
    }

    private func rotateIfNeededLocked() {
        guard let attributes = try? FileManager.default.attributesOfItem(atPath: fileURL.path),
              let size = attributes[.size] as? NSNumber,
              size.int64Value >= maximumBytes else {
            return
        }
        let directory = fileURL.deletingLastPathComponent()
        let baseName = fileURL.lastPathComponent
        for index in stride(from: maximumBackups - 1, through: 1, by: -1) {
            let from = directory.appendingPathComponent("\(baseName).\(index)")
            let to = directory.appendingPathComponent("\(baseName).\(index + 1)")
            if FileManager.default.fileExists(atPath: from.path) {
                try? FileManager.default.removeItem(at: to)
                try? FileManager.default.moveItem(at: from, to: to)
            }
        }
        let first = directory.appendingPathComponent("\(baseName).1")
        try? FileManager.default.removeItem(at: first)
        try? FileManager.default.moveItem(at: fileURL, to: first)
    }

    private func reloadRecentLines() {
        lock.lock()
        defer { lock.unlock() }
        guard let data = try? Data(contentsOf: fileURL),
              let text = String(data: data, encoding: .utf8) else {
            recentLines = []
            return
        }
        let all = text.split(separator: "\n", omittingEmptySubsequences: true).map(String.init)
        recentLines = Array(all.suffix(maximumRecentLines))
    }
}
