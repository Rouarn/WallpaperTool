import AppKit
import Foundation
import SwiftUI

@MainActor
final class AppModel: ObservableObject {
    enum StatusKind {
        case ready
        case working
        case success
        case failure
    }

    struct Banner: Identifiable {
        let id = UUID()
        let title: String
        let message: String
        let canRetry: Bool
    }

    @Published private(set) var config: AppConfig
    @Published private(set) var isDownloading = false
    @Published private(set) var progress: Double?
    @Published private(set) var statusText: String
    @Published private(set) var statusKind: StatusKind
    @Published private(set) var banner: Banner?
    @Published private(set) var preview: PreviewInfo?
    @Published private(set) var previewImage: NSImage?
    @Published private(set) var logs: [String]
    @Published var isLogExpanded = false
    @Published private(set) var launchAgentEnabled: Bool
    @Published private(set) var autoUpdateError: String?
    @Published var urlError: String?

    private let store: ConfigStore
    private let logger: Logger
    private var activeDownloader: Downloader?
    private var saveTask: Task<Void, Never>?
    private var successResetTask: Task<Void, Never>?
    private var didStart = false

    init(store: ConfigStore = ConfigStore()) {
        self.store = store
        let loaded = store.load()
        self.config = loaded

        let logger = Logger(fileURL: store.logURL(for: loaded))
        self.logger = logger
        self.logs = logger.currentLines()
        self.launchAgentEnabled = LaunchAgentManager.isEnabled()

        let existing = AppModel.loadPreview(for: loaded)
        self.preview = existing
        self.previewImage = existing.flatMap { NSImage(contentsOf: $0.url) }

        if loaded.lastResult == RunResult.failed.rawValue {
            self.statusKind = .failure
            if let date = Formatters.date(fromISO: loaded.lastRunAt) {
                self.statusText = Strings.statusLastAutoFail + " · " + Formatters.short(date)
            } else {
                self.statusText = Strings.statusLastAutoFail
            }
        } else if let existing = existing {
            self.statusKind = .ready
            self.statusText = Strings.statusUpdatedAt(Formatters.short(existing.modifiedAt))
        } else {
            self.statusKind = .ready
            self.statusText = Strings.statusReady
        }

        logger.onChange = { [weak self] lines in
            self?.logs = lines
        }
    }

    func start() {
        guard !didStart else { return }
        didStart = true
        logger.log(.info, Strings.logAppStarted)
    }

    // MARK: - 配置

    var urlBinding: Binding<String> {
        Binding(get: { self.config.url }, set: { self.updateURL($0) })
    }

    var destDirBinding: Binding<String> {
        Binding(get: { self.config.destDir }, set: { self.updateDestDir($0) })
    }

    var logFileBinding: Binding<String> {
        Binding(get: { self.config.logFile }, set: { self.updateLogFile($0) })
    }

    var updatedAtText: String {
        guard let date = Formatters.date(fromISO: config.lastRunAt) else {
            return Strings.timeNever
        }
        return Formatters.short(date)
    }

    func updateURL(_ value: String) {
        config.url = value
        if urlError != nil {
            urlError = nil
        }
        scheduleSave()
    }

    func commitURL() {
        let text = config.url.trimmingCharacters(in: .whitespacesAndNewlines)
        if text.isEmpty {
            urlError = Strings.errorUrlEmpty
            return
        }
        guard DownloadTarget.validatedURL(text) != nil else {
            urlError = Strings.errorUrlInvalid
            return
        }
        urlError = nil
        config.url = text
        persist()
    }

    func updateDestDir(_ value: String) {
        config.destDir = value
        scheduleSave()
    }

    func chooseFolder() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = false
        panel.canChooseDirectories = true
        panel.allowsMultipleSelection = false
        panel.canCreateDirectories = true
        panel.prompt = Strings.actionChooseFolder
        if panel.runModal() == .OK, let url = panel.url {
            config.destDir = (url.path as NSString).abbreviatingWithTildeInPath
            persist()
            refreshPreview()
        }
    }

    func updateLogFile(_ value: String) {
        config.logFile = value
        logger.updateFileURL(store.logURL(for: config))
        logs = logger.currentLines()
        scheduleSave()
    }

    func resetURL() {
        config.url = AppConfig.defaults().url
        urlError = nil
        persist()
    }

    // MARK: - 自动更新

    func setAutoUpdate(_ enabled: Bool) {
        autoUpdateError = nil
        if enabled {
            switch LaunchAgentManager.enable() {
            case .success:
                config.autoUpdate = true
                launchAgentEnabled = true
                logger.log(.info, Strings.logAutoUpdateEnabled)
                logger.log(.info, Strings.hintLaunchAgentApproval)
            case .failure(let error):
                autoUpdateError = Strings.logLaunchAgentFailed(error.localizedDescription)
                config.autoUpdate = false
                launchAgentEnabled = false
            }
        } else {
            switch LaunchAgentManager.disable() {
            case .success:
                config.autoUpdate = false
                launchAgentEnabled = false
                logger.log(.info, Strings.logAutoUpdateDisabled)
            case .failure(let error):
                autoUpdateError = Strings.logLaunchAgentFailed(error.localizedDescription)
            }
        }
        persist()
    }

    func openLoginItemsSettings() {
        LaunchAgentManager.openLoginItemsSettings()
    }

    // MARK: - 设为壁纸开关

    /// 只记录用户意图，不触发任何下载或设置动作。
    func setAutoSetWallpaper(_ enabled: Bool) {
        config.setWallpaperAfterDownload = enabled
        logger.log(.info, enabled ? Strings.logWallpaperSettingOn : Strings.logWallpaperSettingOff)
        persist()
    }

    // MARK: - 保留历史开关

    /// 只记录用户意图，不触发任何下载动作。
    func setKeepHistory(_ enabled: Bool) {
        config.keepHistory = enabled
        logger.log(.info, enabled ? Strings.logKeepHistoryOn : Strings.logKeepHistoryOff)
        persist()
    }

    // MARK: - 下载

    func downloadNow() {
        guard !isDownloading else { return }
        guard let url = DownloadTarget.validatedURL(config.url) else {
            urlError = config.url.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
                ? Strings.errorUrlEmpty
                : Strings.errorUrlInvalid
            return
        }
        urlError = nil
        banner = nil
        isDownloading = true
        progress = nil
        statusKind = .working
        statusText = Strings.statusDownloadingUnknown
        _ = url

        let downloader = Downloader { [weak self] value in
            Task { @MainActor in
                self?.progress = value
                if let value = value {
                    self?.statusText = Strings.statusDownloading(String(Int(value * 100)))
                }
            }
        }
        activeDownloader = downloader

        Task { [weak self] in
            guard let self = self else { return }
            do {
                let result = try await UpdateService.perform(config: self.config,
                                                             logger: self.logger,
                                                             downloader: downloader)
                self.handleSuccess(result)
            } catch {
                self.handleFailure(error)
            }
        }
    }

    func cancelDownload() {
        activeDownloader?.cancel()
    }

    func retry() {
        downloadNow()
    }

    func reapplyWallpaper() {
        guard let preview = preview else { return }
        do {
            try WallpaperSetter.apply(preview.url)
            logger.log(.info, Strings.logWallpaperSet)
            statusKind = .success
            statusText = Strings.statusAppliedFromPreview(preview.url.lastPathComponent)
            scheduleSuccessReset()
        } catch {
            logger.log(.warning, Strings.logWallpaperFailed(error.localizedDescription))
            banner = Banner(title: Strings.errorDownloadFailed,
                            message: error.localizedDescription,
                            canRetry: false)
            statusKind = .failure
            statusText = Strings.statusFailed
        }
    }

    // MARK: - 文件与日志

    func openFolder() {
        let folder = DownloadTarget.destinationDirectory(config)
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        } catch {
            banner = Banner(title: Strings.errorOpenFolderFailed(error.localizedDescription),
                            message: folder.path,
                            canRetry: false)
            return
        }
        if let target = preview?.url {
            NSWorkspace.shared.activateFileViewerSelecting([target])
        } else {
            NSWorkspace.shared.open(folder)
        }
    }

    func openLogFile() {
        let url = logger.logFilePath()
        if !FileManager.default.fileExists(atPath: url.path) {
            try? FileManager.default.createDirectory(at: url.deletingLastPathComponent(),
                                                     withIntermediateDirectories: true)
            FileManager.default.createFile(atPath: url.path, contents: nil)
        }
        if !NSWorkspace.shared.open(url) {
            banner = Banner(title: Strings.errorOpenLogFailed(url.path), message: "", canRetry: false)
        }
    }

    func copyLogs() {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(logs.joined(separator: "\n"), forType: .string)
    }

    func clearBanner() {
        banner = nil
    }

    func refreshPreview() {
        preview = AppModel.loadPreview(for: config)
        previewImage = preview.flatMap { NSImage(contentsOf: $0.url) }
    }

    // MARK: - 内部

    private func handleSuccess(_ result: UpdateResult) {
        activeDownloader = nil
        isDownloading = false
        progress = 1
        banner = nil

        config.lastRunAt = Formatters.isoString(Date())
        config.lastResult = RunResult.success.rawValue
        persist()

        preview = PreviewInfo(url: result.fileURL,
                              width: result.width,
                              height: result.height,
                              byteSize: result.byteSize,
                              modifiedAt: Date())
        previewImage = NSImage(contentsOf: result.fileURL)

        statusKind = .success
        let name = result.fileURL.lastPathComponent
        if result.wallpaperApplied {
            statusText = result.replacedExisting
                ? Strings.statusSavedOverwriteAndSet(name)
                : Strings.statusSavedNewAndSet(name)
        } else {
            statusText = result.replacedExisting
                ? Strings.statusSavedOverwriteOnly(name)
                : Strings.statusSavedNewOnly(name)
        }
        scheduleSuccessReset()
    }

    private func handleFailure(_ error: Error) {
        activeDownloader = nil
        isDownloading = false
        progress = nil

        let appError = UpdateService.normalize(error)
        config.lastRunAt = Formatters.isoString(Date())

        if appError.isCancellation {
            config.lastResult = RunResult.cancelled.rawValue
            statusKind = .ready
            statusText = Strings.statusCancelled
            banner = nil
        } else {
            config.lastResult = RunResult.failed.rawValue
            statusKind = .failure
            statusText = Strings.statusFailed
            banner = Banner(title: Strings.errorDownloadFailed,
                            message: appError.reason,
                            canRetry: true)
        }
        persist()
    }

    private func scheduleSuccessReset() {
        successResetTask?.cancel()
        successResetTask = Task { [weak self] in
            try? await Task.sleep(nanoseconds: 3_000_000_000)
            guard !Task.isCancelled else { return }
            guard let self = self else { return }
            self.statusKind = .ready
            self.statusText = Strings.statusUpdatedAt(Formatters.short(Date()))
        }
    }

    private func scheduleSave() {
        saveTask?.cancel()
        saveTask = Task { [weak self] in
            try? await Task.sleep(nanoseconds: 400_000_000)
            guard !Task.isCancelled else { return }
            self?.persist()
        }
    }

    private func persist() {
        _ = store.save(config)
    }

    private static func loadPreview(for config: AppConfig) -> PreviewInfo? {
        let folder = DownloadTarget.destinationDirectory(config)
        guard let url = DownloadTarget.findExistingWallpaper(in: folder) else {
            return nil
        }
        let attributes = try? FileManager.default.attributesOfItem(atPath: url.path)
        let size = (attributes?[.size] as? NSNumber)?.int64Value ?? 0
        let modified = attributes?[.modificationDate] as? Date
        let dimensions = ImageInspector.dimensions(url: url)
        return PreviewInfo(url: url,
                           width: dimensions?.width ?? 0,
                           height: dimensions?.height ?? 0,
                           byteSize: size,
                           modifiedAt: modified)
    }
}
