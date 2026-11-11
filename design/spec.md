# 壁纸工具 · 共享设计规范

本文件是 Windows 与 macOS 两端的实现规则。具体数值只存在于 `design/tokens.json` 与 `design/copy.zh-CN.json`，界面代码不得写死色值、字号、圆角、间距和中文文案。

## 1. 唯一来源与生成物

| 来源 | 生成物 | 消费方 |
|---|---|---|
| `design/tokens.json` | `windows/src/Theme.Generated.cs`、`macos/Sources/WallpaperTool/Generated/Theme.swift` | 两端主题层 |
| `design/copy.zh-CN.json` | `windows/src/Strings.Generated.cs`、`macos/Sources/WallpaperTool/Generated/Strings.swift` | 两端界面与服务层 |

规则：

1. 生成文件带 `AUTO-GENERATED ... DO NOT EDIT` 头，禁止手改；改动会在 `--check` 时被识别。
2. 修改流程：改 `design/` → `python3 tools/gen_tokens.py` → 提交 2 个源文件 + 4 个生成文件。
3. 日常构建不依赖 Python：Windows/macOS 编译只读已提交的生成文件。
4. 构建脚本在生成文件缺失时直接报错，不静默回退。

## 2. 命名与结构

- token 路径：`类别.角色[.变体]`，语义命名（`accent`、`surface`），禁用外观命名（`blue`、`gray-100`）。
- 颜色叶子：`...light` / `...dark` 成对出现，缺一即校验失败。
- 平台差异：叶子名为 `windows` / `macos`，生成器为目标平台取值；共享 token 不带平台叶子。
- Windows 生成 PascalCase（`Theme.Palette.BgCanvasLight`）；macOS 生成 lowerCamelCase（`Theme.Palette.bgCanvas`）。
- macOS 的表面/文字色走系统语义色（`macos.semantic` 声明），其余色值用共享十六进制并随外观切换。

## 3. 颜色使用规则

| 层级 | Token | 用途 |
|---|---|---|
| 画布 | `color.bg.canvas` | 窗口内容底色 |
| 表面 | `color.bg.surface` | 卡片、输入框 |
| 弱表面 | `color.bg.surfaceSubtle` | 日志、只读区 |
| 描边 | `color.stroke.default` / `stroke.strong` | 卡片描边 / 输入框描边 |
| 文字 | `color.text.primary` / `secondary` / `tertiary` | 标题与值 / 标签与说明 / 时间戳 |
| 强调 | `color.accent.default` / `onAccent` / `text` / `subtle` / `hover` / `pressed` | 主按钮、选中态、进度 / 浅底上的文字链与焦点描边 |
| 状态 | `color.status.*` / `.*Subtle` | 仅表达成功/警告/失败 |

硬性要求：

- 正文与标签对比度 ≥ 4.5:1，由生成器自动校验，不靠肉眼。
- 状态不只靠颜色表达，必须同时有图标/形状和文案。
- 强调色只出现在主操作、选中态、进度；状态色不得用于装饰。
- 深色模式不是反色：surface 比 canvas 亮一档，描边用白色低透明度。
- 禁止在视图代码出现 `#RRGGBB`、`rgba(...)`、`Color.FromArgb(...)`、`NSColor(...)` 字面量（`ThemeColor` 适配器除外）。

## 4. 字号角色

只允许使用这些语义角色，不逐处调字号：

| 角色 | 用途 |
|---|---|
| `type.windowTitle` | 窗口主标题 |
| `type.sectionTitle` | 卡片标题 |
| `type.body` | 正文、输入值 |
| `type.bodyStrong` | 关键数值、按钮文字 |
| `type.caption` | 字段说明、时间戳、错误提示 |
| `type.mono` | 日志、路径、URL |

Windows 使用 `Segoe UI Variable, Segoe UI`，macOS 使用系统字体（SF Pro / SF Mono）；数值按平台 ramp 生成。

## 5. 间距、圆角与尺寸

- 间距只取 `Theme.Space.*`（4 的倍数）；页面左右边距 `xl(20)`，卡片内边距 `l(16)`，卡片间距 `l(16)`，组内间距 `m(12)`，标签与字段 `s(8)`。
- 圆角：容器 `radius.card`，控件 `radius.control`，开关 `radius.pill`。禁止手写圆角数值。
- 控件高度取 `size.controlHeight` / `size.primaryButtonHeight`；图标取 `size.icon` / `size.iconLarge`。
- 窗口尺寸取 `size.window.*`，最小尺寸必须生效；窗口可缩放，内容布局随宽度自适应。
- Windows 圆角 4/8，macOS 6/10：数值不同，层级规则一致（容器 > 控件）。

## 6. 状态表达

| 状态 | 呈现 | 文案键 |
|---|---|---|
| 空态 | 预览区虚线框 + 图标 + 说明 + 主操作 | `empty.previewTitle` / `empty.previewBody` |
| 进行中 | 主按钮禁用显示百分比、4px 进度条、可取消 | `status.downloading` / `action.cancel` |
| 成功 | 预览刷新 + 状态条短暂提示，随后收敛为更新时间 | `status.success` → `status.updatedAt` |
| 失败 | 对应卡片内 danger 淡底 banner：结论 + 原因 + 动作 | `error.downloadFailed` + `error.*` + `action.retry` |
| 校验失败 | 字段下方 caption 提示 + 红色描边 | `error.urlEmpty` / `error.urlInvalid` |
| 静默失败 | 不弹窗；写日志；下次打开主窗口状态条可见 | `status.lastAutoFail` |

禁止事项：

- 用系统消息框（`MessageBox` / `NSAlert`）提示可恢复错误或成功结果。
- 只显示错误码（“错误: 404”）而不解释原因与下一步。
- 静默模式失败后不留任何可见痕迹。

## 7. 文案规则

- 键名：`域.动作/状态`，例如 `action.downloadNow`、`error.httpStatus`。
- 占位符：`{name}`，生成时按出现顺序转为 C# `{0}` / Swift 插值；同名占位符禁止重复。
- 按钮：动词短语 2–4 字（`立即下载`、`选择…`、`重试`）。
- 状态：事实 + 时间，不用感叹号（`已更新 · 11-11 14:32`）。
- 错误：结论 + 原因 + 下一步（`下载失败` / `服务器返回 404，链接可能已失效。` / `重试`）。
- 时间：界面 `MM-dd HH:mm`，日志 `yyyy-MM-dd HH:mm:ss`。
- 路径：界面用 `~` 缩写，完整路径放 tooltip；日志写完整路径。
- 代码中不得拼接中文句子；变量一律走占位符。

## 8. 配置 schema（两端一致）

路径：Windows `%APPDATA%\WallpaperTool\config.json`；macOS `~/Library/Application Support/WallpaperTool/config.json`。

| 键 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `schemaVersion` | int | `1` | 迁移用 |
| `url` | string | `https://picsum.photos/1920/1080` | 下载地址 |
| `destDir` | string | `~/Pictures/WallpaperTool` | 保存目录，支持 `~` |
| `logFile` | string | 平台默认 | 日志路径 |
| `autoUpdate` | bool | `true` | 登录后自动更新 |
| `setWallpaperAfterDownload` | bool | `true` | 下载后立即设为壁纸 |
| `lastRunAt` | string | `""` | ISO 8601 时间 |
| `lastResult` | string | `""` | `success` / `failed` / `cancelled` |

写入采用“临时文件 + 原子替换”；未知键忽略；`schemaVersion` 高于当前版本时备份原文件并用默认值。

## 9. 日志格式

```
[2026-11-11 14:32:05] INFO 已下载并设为桌面壁纸
[2026-11-11 14:33:10] ERROR 下载失败：服务器返回 404，链接可能已失效。
```

- 级别固定 `INFO` / `WARN` / `ERROR`，正文来自文案表。
- 轮转：单文件 1 MB，保留 3 份（`wallpaper_log.txt.1` … `.3`）。
- 位置：Windows `%LOCALAPPDATA%\WallpaperTool\`；macOS `~/Library/Logs/WallpaperTool/`。

## 10. 验收清单（每个阶段自检）

1. `python3 tools/gen_tokens.py --check` 退出码 0。
2. 界面代码全文搜索无十六进制色值、无裸字号、无中文字符串字面量（文案表生成物除外）。
3. 空态/进行中/成功/失败四种状态均可通过操作触发，且不依赖弹窗。
4. 浅色/深色下文字与背景对比度均 ≥ 4.5:1。
5. 窗口在最小尺寸下不重叠、不截断；路径过长时省略并保留 tooltip。
