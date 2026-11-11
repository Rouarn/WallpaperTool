import AppKit

enum WallpaperSetter {
    static func apply(_ imageURL: URL) throws {
        let options: [NSWorkspace.DesktopImageOptionKey: Any] = [
            .imageScaling: NSImageScaling.scaleProportionallyUpOrDown.rawValue
        ]

        var lastError: Error?
        for screen in NSScreen.screens {
            do {
                try NSWorkspace.shared.setDesktopImageURL(imageURL, for: screen, options: options)
            } catch {
                lastError = error
            }
        }
        if let error = lastError {
            throw error
        }
    }
}
