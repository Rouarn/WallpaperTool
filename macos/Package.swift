// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "WallpaperTool",
    platforms: [
        .macOS(.v13)
    ],
    targets: [
        .executableTarget(
            name: "WallpaperTool",
            path: "Sources/WallpaperTool"
        )
    ]
)
