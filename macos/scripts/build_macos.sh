#!/usr/bin/env bash
# 构建 macOS 应用：swift build + 组装 .app + ad-hoc 签名。
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APP_NAME="WallpaperTool"
# 产物放在 macos/ 自己目录下，与 Windows 端的 windows/bin/ 对称
BUILD_DIR="${ROOT}/macos/dist"
APP="${BUILD_DIR}/${APP_NAME}.app"
SOURCE_FILE="${ROOT}/macos/Sources/WallpaperTool/App/WallpaperToolApp.swift"

if [[ ! -f "${ROOT}/macos/Sources/WallpaperTool/Generated/Theme.swift" ]]; then
    echo "缺少生成文件：请先运行 python3 tools/gen_tokens.py" >&2
    exit 1
fi

VERSION="$(/usr/bin/grep -m1 'static let version' "${SOURCE_FILE}" | /usr/bin/sed -E 's/.*"([^"]+)".*/\1/')"
BUNDLE_ID="$(/usr/bin/grep -m1 'static let bundleIdentifier' "${SOURCE_FILE}" | /usr/bin/sed -E 's/.*"([^"]+)".*/\1/')"

echo "==> swift build (release)"
/usr/bin/swift build -c release --package-path "${ROOT}/macos"

BINARY="${ROOT}/macos/.build/release/${APP_NAME}"
if [[ ! -x "${BINARY}" ]]; then
    echo "构建产物不存在：${BINARY}" >&2
    exit 1
fi

echo "==> 组装 ${APP}"
/bin/rm -rf "${APP}"
/bin/mkdir -p "${APP}/Contents/MacOS" "${APP}/Contents/Resources"
/bin/cp "${BINARY}" "${APP}/Contents/MacOS/${APP_NAME}"

/bin/cat > "${APP}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>${BUNDLE_ID}</string>
    <key>CFBundleExecutable</key>
    <string>${APP_NAME}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>LSMinimumSystemVersion</key>
    <string>13.0</string>
    <key>LSUIElement</key>
    <true/>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSAppTransportSecurity</key>
    <dict>
        <key>NSAllowsArbitraryLoads</key>
        <true/>
    </dict>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
PLIST

ICON="${ROOT}/macos/Resources/AppIcon.icns"
if [[ -f "${ICON}" ]]; then
    /bin/cp "${ICON}" "${APP}/Contents/Resources/AppIcon.icns"
    /usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string AppIcon" "${APP}/Contents/Info.plist" >/dev/null
fi

# 品牌图片资源（四芒星标记），运行时经 Bundle.main 读取
for asset in BrandStarInk.png BrandStarWhite.png; do
    if [[ -f "${ROOT}/macos/Resources/${asset}" ]]; then
        /bin/cp "${ROOT}/macos/Resources/${asset}" "${APP}/Contents/Resources/${asset}"
    fi
done

echo "==> ad-hoc 签名"
/usr/bin/codesign --force --deep --sign - --timestamp=none "${APP}"
/usr/bin/codesign --verify --verbose=2 "${APP}"

echo "==> 完成：${APP} (${VERSION})"
