import AppKit
import SwiftUI

enum AppInfo {
    static let version = "1.0.0"
    static let bundleIdentifier = "com.local.wallpapertool"
}

struct WallpaperToolApp: App {
    @StateObject private var model = AppModel()

    var body: some Scene {
        Window(Strings.appName, id: "main") {
            MainView(model: model)
                .frame(minWidth: Theme.Size.windowMinWidth,
                       minHeight: Theme.Size.windowMinHeight)
                .onAppear { model.start() }
        }
        .defaultSize(width: Theme.Size.windowWidth, height: Theme.Size.windowHeight)
        .windowResizability(.contentMinSize)
        .commands {
            CommandGroup(replacing: .newItem) {}
        }

        MenuBarExtra {
            MenuBarView(model: model)
        } label: {
            Image(systemName: "photo.on.rectangle")
        }
        .menuBarExtraStyle(.menu)
    }
}

private struct MenuBarView: View {
    @ObservedObject var model: AppModel
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        Text(model.statusText)

        Divider()

        Button(Strings.actionDownloadNow) { model.downloadNow() }
            .disabled(model.isDownloading)

        if model.isDownloading {
            Button(Strings.actionCancel) { model.cancelDownload() }
        }

        Button(Strings.actionOpenMainWindow) { showMainWindow() }

        Divider()

        Button(Strings.actionOpenLogFile) { model.openLogFile() }
        Button(Strings.actionOpenFolder) { model.openFolder() }

        Divider()

        Button(Strings.actionQuit) { NSApp.terminate(nil) }
    }

    private func showMainWindow() {
        openWindow(id: "main")
        NSApp.activate(ignoringOtherApps: true)
    }
}
