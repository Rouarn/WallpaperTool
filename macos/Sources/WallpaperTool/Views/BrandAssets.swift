import AppKit
import SwiftUI

/// 品牌资源接入层。图片随 .app 的 Contents/Resources 分发，来源为 VaKao 品牌视觉资源包
/// （logo/star-mono-ink.png、logo/star-mono-white.png）。缺失时回退到系统符号，不崩。
enum BrandAssets {
    static func star(for scheme: ColorScheme) -> NSImage? {
        let name = scheme == .dark ? "BrandStarWhite" : "BrandStarInk"
        guard let url = Bundle.main.url(forResource: name, withExtension: "png") else {
            return nil
        }
        return NSImage(contentsOf: url)
    }
}

/// 四芒星标记。isSpinning 对应品牌动效 star-spin（时长取 Theme.Motion.spin）。
struct BrandStarMark: View {
    var size: CGFloat = Theme.Size.iconLarge
    var isSpinning = false
    var tint: Color = Theme.Palette.accentDefault

    @Environment(\.colorScheme) private var scheme

    var body: some View {
        Group {
            if let image = BrandAssets.star(for: scheme) {
                Image(nsImage: image)
                    .resizable()
                    .interpolation(.high)
                    .aspectRatio(contentMode: .fit)
            } else {
                Image(systemName: "sparkle")
                    .resizable()
                    .aspectRatio(contentMode: .fit)
                    .foregroundColor(tint)
            }
        }
        .frame(width: size, height: size)
        .rotationEffect(.degrees(isSpinning ? 360 : 0))
        .animation(spinAnimation, value: isSpinning)
    }

    private var spinAnimation: Animation {
        isSpinning
            ? .linear(duration: Theme.Motion.spin).repeatForever(autoreverses: false)
            : .easeOut(duration: Theme.Motion.fast)
    }
}
