# design/ · 设计源文件

本目录是 Windows 与 macOS 两端的唯一设计来源。界面代码只消费生成物，不直接读这里的 JSON。

## 文件

| 文件 | 作用 |
|---|---|
| `tokens.json` | 色值、间距、圆角、尺寸、字号、动效、macOS 语义色映射 |
| `copy.zh-CN.json` | 界面与日志的全部中文文案（含占位符） |
| `spec.md` | 使用规则：命名、状态表达、配置 schema、日志格式、验收清单 |
| `README.md` | 本文件 |

## 生成物（禁止手改）

| 文件 | 平台 |
|---|---|
| `src/Theme.Generated.cs` | Windows（WPF） |
| `src/Strings.Generated.cs` | Windows（WPF） |
| `macos/Sources/WallpaperTool/Generated/Theme.swift` | macOS（SwiftUI） |
| `macos/Sources/WallpaperTool/Generated/Strings.swift` | macOS（SwiftUI） |

## 日常流程

```bash
# 1. 修改 design/tokens.json 或 design/copy.zh-CN.json
# 2. 生成
python3 tools/gen_tokens.py
# 3. 校验（对比度、占位符、生成物一致性、源码引用）
python3 tools/gen_tokens.py --check
# 4. 提交：2 个源文件 + 4 个生成文件
```

`--check` 失败时不会写文件；修复后重新生成。构建脚本（`build_app.ps1` / `scripts/build_macos.sh`）会在生成文件缺失时直接报错。

## 新增一个 token

1. 在 `tokens.json` 对应类别下加节点，颜色必须同时给 `light` / `dark`；平台差异用 `windows` / `macos` 叶子。
2. 运行生成器，确认 C# 与 Swift 都出现对应常量。
3. 在界面代码里通过 `Theme.*` 使用，不写字面量。

## 新增一条文案

1. 在 `copy.zh-CN.json` 加 `域.名称` 键，变量写成 `{name}`。
2. 生成后使用 `Strings.ActionXxx`（C#）或 `Strings.actionXxx`（Swift）；带占位符的键在 Swift 侧生成同名函数。
3. `--check` 会扫描源码中的 `Strings.xxx` 引用，引用不存在的键会失败。

## 平台差异约定

- 尺寸/字号按平台取值（Windows 与 macOS 各一套 ramp），语义角色共用。
- macOS 的 `bg` / `stroke` / `text` 映射到系统语义色（见 `tokens.json` 的 `macos.semantic`），自动适配浅色/深色/提高对比度。
- 品牌色与状态色两端使用同一组十六进制值，保证识别一致。

## 依赖

Python 3 标准库即可，无需安装包。生成动作只在设计变更时执行；日常编译不依赖 Python。
