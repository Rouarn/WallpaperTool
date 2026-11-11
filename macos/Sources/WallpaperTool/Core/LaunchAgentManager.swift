import AppKit
import Foundation

enum LaunchAgentManager {
    static let label = AppInfo.bundleIdentifier

    static var plistURL: URL {
        let base = FileManager.default.urls(for: .libraryDirectory, in: .userDomainMask)[0]
        return base.appendingPathComponent("LaunchAgents", isDirectory: true)
            .appendingPathComponent("\(label).plist")
    }

    static var executablePath: String {
        if let path = Bundle.main.executablePath {
            return path
        }
        return CommandLine.arguments[0]
    }

    static func isEnabled() -> Bool {
        FileManager.default.fileExists(atPath: plistURL.path)
    }

    static func isLoaded() -> Bool {
        runLaunchctl(["print", "gui/\(getuid())/\(label)"]) == 0
    }

    @discardableResult
    static func enable() -> Result<Void, Error> {
        do {
            try FileManager.default.createDirectory(at: plistURL.deletingLastPathComponent(),
                                                    withIntermediateDirectories: true)
            try plistContents().write(to: plistURL, atomically: true, encoding: .utf8)
            let code = runLaunchctl(["bootstrap", "gui/\(getuid())", plistURL.path])
            // 37/5 表示已经加载；此时保留 plist 即可。
            if code != 0 && !isLoaded() {
                return .failure(LaunchAgentError.commandFailed(code))
            }
            return .success(())
        } catch {
            return .failure(error)
        }
    }

    @discardableResult
    static func disable() -> Result<Void, Error> {
        _ = runLaunchctl(["bootout", "gui/\(getuid())", plistURL.path])
        do {
            if FileManager.default.fileExists(atPath: plistURL.path) {
                try FileManager.default.removeItem(at: plistURL)
            }
            return .success(())
        } catch {
            return .failure(error)
        }
    }

    static func openLoginItemsSettings() {
        guard let url = URL(string: "x-apple.systempreferences:com.apple.LoginItems-Settings.extension") else {
            return
        }
        NSWorkspace.shared.open(url)
    }

    private static func plistContents() -> String {
        let executable = xmlEscape(executablePath)
        return """
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
            <key>Label</key>
            <string>\(label)</string>
            <key>ProgramArguments</key>
            <array>
                <string>\(executable)</string>
                <string>--silent</string>
            </array>
            <key>RunAtLoad</key>
            <true/>
            <key>ProcessType</key>
            <string>Background</string>
            <key>LimitLoadToSessionType</key>
            <string>Aqua</string>
        </dict>
        </plist>
        """
    }

    private static func xmlEscape(_ text: String) -> String {
        text.replacingOccurrences(of: "&", with: "&amp;")
            .replacingOccurrences(of: "<", with: "&lt;")
            .replacingOccurrences(of: ">", with: "&gt;")
    }

    private static func runLaunchctl(_ arguments: [String]) -> Int32 {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/bin/launchctl")
        process.arguments = arguments
        process.standardOutput = FileHandle.nullDevice
        process.standardError = FileHandle.nullDevice
        do {
            try process.run()
            process.waitUntilExit()
            return process.terminationStatus
        } catch {
            return -1
        }
    }
}

enum LaunchAgentError: LocalizedError {
    case commandFailed(Int32)

    var errorDescription: String? {
        switch self {
        case .commandFailed(let code):
            return "launchctl exit \(code)"
        }
    }
}
