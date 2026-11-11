# Wallpaper Tool (壁纸自动更新工具)

一个壁纸下载与自动更新工具，Windows 与 macOS 两端共用一套设计 token 与中文文案，各自提供原生界面与开机静默更新能力。

## ✨ 主要功能

- **原生双端应用**：Windows 端为 .NET Framework（C# WinForms，Windows 自带运行时）；macOS 端为 SwiftUI + AppKit（SwiftPM）。
- **图形化配置界面**：设置保存目录、下载 URL 和日志文件名。
- **开机静默更新**：
  - Windows 端作为启动项运行时在后台静默下载并退出，支持 `-silent` 参数手动触发。
  - macOS 端通过 LaunchAgent（`~/Library/LaunchAgents/com.local.wallpapertool.plist`）登录后静默运行。
- **高 DPI 适配**：Windows 端随屏幕分辨率调整布局；macOS 端使用原生自适应布局。
- **中文支持**：全中文界面与日志，文案集中在 `design/copy.zh-CN.json`。
- **日志记录**：详细记录每次运行的时间和结果，方便排查问题。

## 📂 项目结构

```
wallpaper-tool/
├── README.md           # 本文件
├── design/             # 唯一设计来源：tokens、文案、规范（禁止在界面代码里写死）
├── tools/
│   └── gen_tokens.py   # 由 design/ 生成两端常量，并做 --check 一致性校验
├── macos/              # macOS 端（SwiftUI + AppKit）
│   ├── Package.swift
│   ├── Sources/WallpaperTool/
│   │   ├── main.swift
│   │   ├── App/        # 应用入口与状态模型
│   │   ├── Core/       # 配置、下载、壁纸设置、日志、LaunchAgent
│   │   ├── Views/      # 界面
│   │   └── Generated/  # Theme.swift / Strings.swift（生成物，禁止手改）
│   ├── Resources/      # AppIcon.icns 与品牌星芒图（来自品牌资源包）
│   └── scripts/
│       └── build_macos.sh
└── windows/            # Windows 端（C# WinForms）
    ├── build_app.bat   # 编译入口（双击运行）
    ├── build_app.ps1   # 调用 csc.exe 编译
    ├── bin/            # 编译产物 WallpaperTool.exe 与日志（不提交）
    ├── src/
    │   ├── WallpaperApp.cs
    │   ├── app.ico
    │   ├── app.manifest
    │   ├── Theme.Generated.cs    # 生成物，禁止手改
    │   └── Strings.Generated.cs  # 生成物，禁止手改
    └── scripts/
        ├── download_wallpaper.ps1 # PowerShell 下载逻辑（旧版核心）
        ├── install_startup.ps1    # 安装开机启动项
        └── run_now.bat            # 快速运行脚本
```

## 🎨 设计源与生成物

界面中的色值、间距、圆角、字号与中文文案全部来自 `design/`，由 `tools/gen_tokens.py` 生成到两端目录：

| 来源 | 生成物 |
|---|---|
| `design/tokens.json` | `windows/src/Theme.Generated.cs`、`macos/Sources/WallpaperTool/Generated/Theme.swift` |
| `design/copy.zh-CN.json` | `windows/src/Strings.Generated.cs`、`macos/Sources/WallpaperTool/Generated/Strings.swift` |

```bash
python3 tools/gen_tokens.py          # 重新生成
python3 tools/gen_tokens.py --check  # 校验（对比度、占位符、生成物是否漂移、源码引用）
```

`--check` 失败时不写文件。日常编译不依赖 Python，只读已提交的生成文件。

## 🚀 Windows 端

### 1. 编译（首次使用）

双击运行 `windows\build_app.bat`，或在该目录执行 `powershell -ExecutionPolicy Bypass -File build_app.ps1`。

编译成功后，可执行文件生成在 `windows\bin\WallpaperTool.exe`。

### 2. 正常运行

双击 `windows\bin\WallpaperTool.exe` 打开图形界面。

- **保存目录**：默认为 “Saved Pictures”，可点击“浏览”修改。
- **下载地址**：默认为 `https://picsum.photos/1920/1080`，可改为任意图片 URL。
- **立即下载**：点击按钮即可测试下载。

### 3. 设置开机自动更新

1. 进入 `windows\scripts` 目录。
2. 右键 `install_startup.ps1`，选择 **“使用 PowerShell 运行”**。

脚本会在启动文件夹创建指向 `WallpaperTool.exe -silent` 的快捷方式，下次开机自动在后台更新。

### 4. 手动脚本运行

运行 `windows\scripts\run_now.bat` 可快速触发旧版 PowerShell 下载逻辑（主要用于测试）。

## 🍎 macOS 端

```bash
python3 tools/gen_tokens.py --check     # 确认生成物一致
macos/scripts/build_macos.sh            # swift build + 组装 .app + ad-hoc 签名
```

产物位于 `dist/WallpaperTool.app`。静默运行：`dist/WallpaperTool.app/Contents/MacOS/WallpaperTool --silent`。

## 🛠️ 技术细节

- **语言**：C#（WinForms）、Swift（SwiftUI / AppKit）、PowerShell、Batch、Bash
- **编译器**：`csc.exe`（Windows 内置 .NET Framework）、`swift build`（SwiftPM，macOS 13+）
- **依赖**：Windows 端无需额外安装；macOS 端无需第三方库

## 📝 常见问题

**Q: 运行时出现中文乱码？**
A: Windows 端已在代码中强制使用 UTF-8，并配置控制台输出编码，确保中文环境正常显示。

**Q: 如何修改默认下载源？**
A: 在图形界面中修改 URL 即可。Windows 端的硬编码默认值在 `windows/src/WallpaperApp.cs` 的 `defaultUrl` 变量中。

**Q: 开机启动没有反应？**
A: 开机启动模式为**静默运行**，不显示窗口。Windows 端查看 `windows/bin/wallpaper_log.txt`，macOS 端查看 `~/Library/Logs/WallpaperTool/`。
