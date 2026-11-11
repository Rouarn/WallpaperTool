import Foundation
import ImageIO
import UniformTypeIdentifiers

struct DownloadResponse {
    let fileURL: URL
    let statusCode: Int
    let mimeType: String?
}

final class Downloader: NSObject {
    private let progressHandler: (Double?) -> Void
    private var session: URLSession?
    private var continuation: CheckedContinuation<DownloadResponse, Error>?
    private var downloadedURL: URL?
    private var httpResponse: HTTPURLResponse?
    private var finished = false

    init(progress: @escaping (Double?) -> Void) {
        self.progressHandler = progress
    }

    func download(from url: URL, timeout: TimeInterval = 30) async throws -> DownloadResponse {
        try await withCheckedThrowingContinuation { continuation in
            self.continuation = continuation

            let configuration = URLSessionConfiguration.ephemeral
            configuration.timeoutIntervalForRequest = timeout
            configuration.timeoutIntervalForResource = timeout * 4
            configuration.requestCachePolicy = .reloadIgnoringLocalCacheData

            let session = URLSession(configuration: configuration, delegate: self, delegateQueue: nil)
            self.session = session

            var request = URLRequest(url: url)
            request.timeoutInterval = timeout
            request.setValue("WallpaperTool/1.0 (macOS)", forHTTPHeaderField: "User-Agent")
            session.downloadTask(with: request).resume()
        }
    }

    func cancel() {
        session?.invalidateAndCancel()
    }

    private func finish(error: Error?) {
        guard !finished else { return }
        finished = true
        session?.finishTasksAndInvalidate()

        if let error = error {
            continuation?.resume(throwing: Downloader.mapError(error))
            continuation = nil
            return
        }

        guard let response = httpResponse, let fileURL = downloadedURL else {
            continuation?.resume(throwing: AppError(kind: .network, reason: Strings.errorNetwork))
            continuation = nil
            return
        }

        guard (200..<300).contains(response.statusCode) else {
            try? FileManager.default.removeItem(at: fileURL)
            continuation?.resume(throwing: AppError(
                kind: .httpStatus,
                reason: Strings.errorHttpStatus(String(response.statusCode))
            ))
            continuation = nil
            return
        }

        continuation?.resume(returning: DownloadResponse(
            fileURL: fileURL,
            statusCode: response.statusCode,
            mimeType: response.mimeType
        ))
        continuation = nil
    }

    private static func mapError(_ error: Error) -> AppError {
        if let urlError = error as? URLError {
            switch urlError.code {
            case .cancelled:
                return AppError(kind: .cancelled, reason: Strings.statusCancelled)
            case .timedOut:
                return AppError(kind: .timeout, reason: Strings.errorTimeout)
            default:
                return AppError(kind: .network, reason: Strings.errorNetwork)
            }
        }
        if let appError = error as? AppError {
            return appError
        }
        return AppError(kind: .network, reason: Strings.errorNetwork)
    }
}

extension Downloader: URLSessionDownloadDelegate {
    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask,
                    didWriteData bytesWritten: Int64, totalBytesWritten: Int64,
                    totalBytesExpectedToWrite: Int64) {
        if totalBytesExpectedToWrite > 0 {
            progressHandler(Double(totalBytesWritten) / Double(totalBytesExpectedToWrite))
        } else {
            progressHandler(nil)
        }
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask,
                    didFinishDownloadingTo location: URL) {
        httpResponse = downloadTask.response as? HTTPURLResponse

        // 系统在代理返回后会删除 location，必须先复制到自己的临时目录。
        let directory = FileManager.default.temporaryDirectory
            .appendingPathComponent("WallpaperToolDownloads", isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            let target = directory.appendingPathComponent(UUID().uuidString)
            try? FileManager.default.removeItem(at: target)
            try FileManager.default.copyItem(at: location, to: target)
            downloadedURL = target
        } catch {
            downloadedURL = nil
        }
    }

    func urlSession(_ session: URLSession, task: URLSessionTask, didCompleteWithError error: Error?) {
        finish(error: error)
    }
}

enum ImageInspector {
    struct Info {
        let width: Int
        let height: Int
        let fileExtension: String
        let byteSize: Int64
    }

    static func dimensions(url: URL) -> (width: Int, height: Int)? {
        guard let source = CGImageSourceCreateWithURL(url as CFURL, nil),
              let properties = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as NSDictionary?,
              let width = (properties[kCGImagePropertyPixelWidth] as? NSNumber)?.intValue,
              let height = (properties[kCGImagePropertyPixelHeight] as? NSNumber)?.intValue else {
            return nil
        }
        return (width, height)
    }

    static func inspect(url: URL, mimeType: String?) throws -> Info {
        if let mime = mimeType?.lowercased(),
           mime.hasPrefix("text/") || mime.hasPrefix("application/json") || mime.hasPrefix("application/xhtml") {
            throw AppError(kind: .notImage, reason: Strings.errorNotImage)
        }

        guard let source = CGImageSourceCreateWithURL(url as CFURL, nil),
              let typeIdentifier = CGImageSourceGetType(source),
              let contentType = UTType(typeIdentifier as String),
              contentType.conforms(to: .image),
              let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
            throw AppError(kind: .notImage, reason: Strings.errorNotImage)
        }

        let width = image.width
        let height = image.height
        guard width >= DownloadTarget.minimumWidth, height >= DownloadTarget.minimumHeight else {
            throw AppError(
                kind: .tooSmall,
                reason: Strings.errorImageTooSmall(String(width), String(height))
            )
        }

        let attributes = try? FileManager.default.attributesOfItem(atPath: url.path)
        let size = (attributes?[.size] as? NSNumber)?.int64Value ?? 0

        return Info(
            width: width,
            height: height,
            fileExtension: contentType.preferredFilenameExtension ?? "jpg",
            byteSize: size
        )
    }
}
