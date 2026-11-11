import Foundation

private final class ExitCodeBox {
    var value: Int32 = 1
}

enum HeadlessRunner {
    /// 静默入口：下载 → 设壁纸 → 写日志 → 返回退出码，不创建任何界面。
    static func run() -> Int32 {
        let store = ConfigStore()
        let config = store.load()
        let logger = Logger(fileURL: store.logURL(for: config))
        logger.log(.info, Strings.logSilentStart)

        let box = ExitCodeBox()
        let semaphore = DispatchSemaphore(value: 0)

        Task {
            do {
                let downloader = Downloader(progress: { _ in })
                _ = try await UpdateService.perform(config: config, logger: logger, downloader: downloader)
                var updated = config
                updated.lastRunAt = Formatters.isoString(Date())
                updated.lastResult = RunResult.success.rawValue
                store.save(updated)
                logger.log(.info, Strings.logSilentDone)
                box.value = 0
            } catch let error as AppError where error.isCancellation {
                var updated = config
                updated.lastRunAt = Formatters.isoString(Date())
                updated.lastResult = RunResult.cancelled.rawValue
                store.save(updated)
                box.value = 0
            } catch {
                var updated = config
                updated.lastRunAt = Formatters.isoString(Date())
                updated.lastResult = RunResult.failed.rawValue
                store.save(updated)
                logger.log(.error, Strings.logSilentFailed(UpdateService.message(for: error)))
                box.value = 1
            }
            semaphore.signal()
        }

        semaphore.wait()
        return box.value
    }
}
