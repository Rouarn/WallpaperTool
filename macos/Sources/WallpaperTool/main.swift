import AppKit

// 入口分流：静默模式不初始化 SwiftUI，避免菜单栏/Dock 闪现。
if CommandLine.arguments.contains("--silent") {
    exit(HeadlessRunner.run())
}

WallpaperToolApp.main()
