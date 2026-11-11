import Foundation

enum UpdateService {
    static func perform(config: AppConfig,
                        logger: Logger,
                        downloader: Downloader) async throws -> UpdateResult {
        guard let url = DownloadTarget.validatedURL(config.url) else {
            throw AppError(kind: .invalidURL, reason: Strings.errorUrlInvalid)
        }

        let destination = DownloadTarget.destinationDirectory(config)
        do {
            try FileManager.default.createDirectory(at: destination, withIntermediateDirectories: true)
        } catch {
            throw AppError(kind: .destNotWritable, reason: Strings.errorDestNotWritable)
        }
        guard FileManager.default.isWritableFile(atPath: destination.path) else {
            throw AppError(kind: .destNotWritable, reason: Strings.errorDestNotWritable)
        }

        logger.log(.info, Strings.logDownloadStart(url.absoluteString))

        var downloaded: DownloadResponse?
        do {
            let response = try await downloader.download(from: url)
            downloaded = response

            let info = try ImageInspector.inspect(url: response.fileURL, mimeType: response.mimeType)
            let installed = try install(response.fileURL, into: destination,
                                        fileExtension: info.fileExtension,
                                        keepHistory: config.keepHistory)
            try? FileManager.default.removeItem(at: response.fileURL)

            var wallpaperApplied = false
            if config.setWallpaperAfterDownload {
                do {
                    try WallpaperSetter.apply(installed.url)
                    wallpaperApplied = true
                    logger.log(.info, Strings.logWallpaperSet)
                } catch {
                    logger.log(.warning, Strings.logWallpaperFailed(error.localizedDescription))
                }
            } else {
                logger.log(.info, Strings.logWallpaperSkipped)
            }

            logger.log(.info, Strings.logSaved(installed.url.path))
            return UpdateResult(fileURL: installed.url, width: info.width, height: info.height,
                                byteSize: info.byteSize, wallpaperApplied: wallpaperApplied,
                                replacedExisting: installed.replaced)
        } catch {
            if let response = downloaded {
                try? FileManager.default.removeItem(at: response.fileURL)
            }
            if let appError = error as? AppError, appError.kind == .cancelled {
                logger.log(.warning, Strings.logCancelled)
            } else {
                logger.log(.error, Strings.logDownloadFailed(message(for: error)))
            }
            throw normalize(error)
        }
    }

    static func message(for error: Error) -> String {
        if let appError = error as? AppError {
            return appError.reason
        }
        return error.localizedDescription
    }

    static func normalize(_ error: Error) -> AppError {
        if let appError = error as? AppError {
            return appError
        }
        return AppError(kind: .fileSystem, reason: error.localizedDescription)
    }

    /// 落盘规则：
    /// - 覆盖模式（默认）：固定 `wallpaper.<ext>`，同名替换，并清掉扩展名变化的旧文件；
    /// - 保留历史：`wallpaper-<yyyyMMdd-HHmmss>.<ext>`，同名冲突时追加 `-2`、`-3`，不删除任何旧文件。
    private static func install(_ source: URL,
                                into directory: URL,
                                fileExtension: String,
                                keepHistory: Bool) throws -> (url: URL, replaced: Bool) {
        let staging = directory.appendingPathComponent(".wallpaper.\(UUID().uuidString).tmp")
        let destination: URL
        var replaced = false

        if keepHistory {
            destination = uniqueHistoryDestination(in: directory, fileExtension: fileExtension)
        } else {
            destination = directory.appendingPathComponent("wallpaper.\(fileExtension)")
            replaced = FileManager.default.fileExists(atPath: destination.path)
        }

        do {
            try FileManager.default.copyItem(at: source, to: staging)
            if replaced {
                _ = try FileManager.default.replaceItemAt(destination, withItemAt: staging)
            } else {
                try FileManager.default.moveItem(at: staging, to: destination)
            }
        } catch {
            try? FileManager.default.removeItem(at: staging)
            throw AppError(kind: .fileSystem, reason: Strings.errorSaveFailed(error.localizedDescription))
        }

        // 覆盖模式才清理扩展名变化留下的旧文件；保留历史模式不动任何历史文件。
        if !keepHistory {
            if let items = try? FileManager.default.contentsOfDirectory(at: directory,
                                                                        includingPropertiesForKeys: nil,
                                                                        options: [.skipsHiddenFiles]) {
                for item in items where item.lastPathComponent.hasPrefix("wallpaper.")
                    && item.lastPathComponent != destination.lastPathComponent {
                    try? FileManager.default.removeItem(at: item)
                }
            }
        }

        return (destination, replaced)
    }

    private static func uniqueHistoryDestination(in directory: URL, fileExtension: String) -> URL {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyyMMdd-HHmmss"
        let stamp = formatter.string(from: Date())

        var index = 1
        while true {
            let suffix = index == 1 ? "" : "-\(index)"
            let candidate = directory.appendingPathComponent("wallpaper-\(stamp)\(suffix).\(fileExtension)")
            if !FileManager.default.fileExists(atPath: candidate.path) {
                return candidate
            }
            index += 1
        }
    }
}
