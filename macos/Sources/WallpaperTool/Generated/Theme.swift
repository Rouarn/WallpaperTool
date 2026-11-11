// AUTO-GENERATED FROM design/tokens.json - DO NOT EDIT.
// 重新生成: python3 tools/gen_tokens.py
import SwiftUI
import AppKit

enum Theme {
    enum Palette {
        static let bgCanvas = ThemeColor.semantic(.windowBackgroundColor)
        static let bgSurface = ThemeColor.semantic(.controlBackgroundColor)
        static let bgSurfaceSubtle = ThemeColor.semantic(.underPageBackgroundColor)
        static let strokeDefault = ThemeColor.semantic(.separatorColor)
        static let strokeStrong = ThemeColor.semantic(.gridColor)
        static let textPrimary = ThemeColor.semantic(.labelColor)
        static let textSecondary = ThemeColor.semantic(.secondaryLabelColor)
        static let textTertiary = ThemeColor.semantic(.tertiaryLabelColor)
        static let accentDefault = ThemeColor.adaptive(light: 0xFF0F6CBD, dark: 0xFF60CDFF)
        static let accentOnAccent = ThemeColor.adaptive(light: 0xFFFFFFFF, dark: 0xFF101010)
        static let accentSubtle = ThemeColor.adaptive(light: 0x1A0F6CBD, dark: 0x2460CDFF)
        static let accentHover = ThemeColor.adaptive(light: 0xFF0C5A9E, dark: 0xFF7BD7FF)
        static let accentPressed = ThemeColor.adaptive(light: 0xFF0A4C86, dark: 0xFF9EE2FF)
        static let statusSuccess = ThemeColor.adaptive(light: 0xFF0F7B0F, dark: 0xFF6CCB5F)
        static let statusSuccessSubtle = ThemeColor.adaptive(light: 0x1A0F7B0F, dark: 0x246CCB5F)
        static let statusWarning = ThemeColor.adaptive(light: 0xFF9D5D00, dark: 0xFFFCE100)
        static let statusWarningSubtle = ThemeColor.adaptive(light: 0x1A9D5D00, dark: 0x24FCE100)
        static let statusDanger = ThemeColor.adaptive(light: 0xFFC42B1C, dark: 0xFFFF99A4)
        static let statusDangerSubtle = ThemeColor.adaptive(light: 0x1AC42B1C, dark: 0x24FF99A4)
    }

    enum Space {
        static let xs: CGFloat = 4
        static let s: CGFloat = 8
        static let m: CGFloat = 12
        static let l: CGFloat = 16
        static let xl: CGFloat = 20
        static let xxl: CGFloat = 32
    }

    enum Radius {
        static let control: CGFloat = 6
        static let card: CGFloat = 10
        static let pill: CGFloat = 999
    }

    enum Size {
        static let borderWidth: CGFloat = 1
        static let focusBorderWidth: CGFloat = 2
        static let controlHeight: CGFloat = 28
        static let primaryButtonHeight: CGFloat = 32
        static let icon: CGFloat = 16
        static let iconLarge: CGFloat = 20
        static let progressBar: CGFloat = 4
        static let headerHeight: CGFloat = 56
        static let statusBarHeight: CGFloat = 32
        static let logPanelHeight: CGFloat = 200
        static let rowHeight: CGFloat = 20
        static let toggleWidth: CGFloat = 38
        static let toggleHeight: CGFloat = 22
        static let windowWidth: CGFloat = 540
        static let windowHeight: CGFloat = 680
        static let windowMinWidth: CGFloat = 500
        static let windowMinHeight: CGFloat = 600
    }

    enum Opacity {
        static let disabled: Double = 0.6
    }

    enum Typography {
        static let windowTitleFontSize: CGFloat = 17
        static let windowTitleFontWeight: Int = 600
        static let windowTitleLineHeight: CGFloat = 22
        static let windowTitleFont: Font = .system(size: windowTitleFontSize, weight: themeFontWeight(windowTitleFontWeight))

        static let sectionTitleFontSize: CGFloat = 13
        static let sectionTitleFontWeight: Int = 600
        static let sectionTitleLineHeight: CGFloat = 16
        static let sectionTitleFont: Font = .system(size: sectionTitleFontSize, weight: themeFontWeight(sectionTitleFontWeight))

        static let bodyFontSize: CGFloat = 13
        static let bodyFontWeight: Int = 400
        static let bodyLineHeight: CGFloat = 16
        static let bodyFont: Font = .system(size: bodyFontSize, weight: themeFontWeight(bodyFontWeight))

        static let bodyStrongFontSize: CGFloat = 13
        static let bodyStrongFontWeight: Int = 600
        static let bodyStrongLineHeight: CGFloat = 16
        static let bodyStrongFont: Font = .system(size: bodyStrongFontSize, weight: themeFontWeight(bodyStrongFontWeight))

        static let captionFontSize: CGFloat = 11
        static let captionFontWeight: Int = 400
        static let captionLineHeight: CGFloat = 14
        static let captionFont: Font = .system(size: captionFontSize, weight: themeFontWeight(captionFontWeight))

        static let monoFontSize: CGFloat = 11
        static let monoFontWeight: Int = 400
        static let monoLineHeight: CGFloat = 16
        static let monoFont: Font = .system(size: monoFontSize, weight: themeFontWeight(monoFontWeight), design: .monospaced)

    }

    enum Motion {
        static let fast: Double = 0.15
        static let normal: Double = 0.2
        static let standardEasing: [Double] = [0.2, 0, 0, 1]
    }
}

enum ThemeColor {
    static func semantic(_ color: NSColor) -> Color {
        Color(nsColor: color)
    }

    static func adaptive(light: UInt32, dark: UInt32) -> Color {
        Color(nsColor: NSColor(name: nil) { appearance in
            let isDark = appearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua
            return NSColor(argb: isDark ? dark : light)
        })
    }
}

private func themeFontWeight(_ value: Int) -> Font.Weight {
    switch value {
    case ..<350: return .light
    case 350..<450: return .regular
    case 450..<550: return .medium
    case 550..<650: return .semibold
    default: return .bold
    }
}

private extension NSColor {
    convenience init(argb: UInt32) {
        self.init(srgbRed: CGFloat((argb >> 16) & 0xFF) / 255.0,
                  green: CGFloat((argb >> 8) & 0xFF) / 255.0,
                  blue: CGFloat(argb & 0xFF) / 255.0,
                  alpha: CGFloat((argb >> 24) & 0xFF) / 255.0)
    }
}
