import SwiftUI

struct Card<Content: View>: View {
    let title: String
    @ViewBuilder let content: Content

    var body: some View {
        VStack(alignment: .leading, spacing: Theme.Space.m) {
            Text(title)
                .font(Theme.Typography.sectionTitleFont)
                .foregroundColor(Theme.Palette.textSecondary)
            content
        }
        .padding(Theme.Space.l)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .fill(Theme.Palette.bgSurface)
        )
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .stroke(Theme.Palette.strokeDefault, lineWidth: Theme.Size.borderWidth)
        )
    }
}

struct TokenTextField: View {
    @Binding var text: String
    var isInvalid = false
    var onSubmit: (() -> Void)?

    @FocusState private var focused: Bool

    var body: some View {
        TextField("", text: $text)
            .textFieldStyle(.plain)
            .font(Theme.Typography.bodyFont)
            .foregroundColor(Theme.Palette.textPrimary)
            .padding(.horizontal, Theme.Space.s)
            .frame(height: Theme.Size.controlHeight)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .fill(Theme.Palette.bgSurfaceSubtle)
            )
            .overlay(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .stroke(borderColor, lineWidth: focused || isInvalid ? Theme.Size.focusBorderWidth : Theme.Size.borderWidth)
            )
            .focused($focused)
            .onSubmit { onSubmit?() }
    }

    private var borderColor: Color {
        if isInvalid {
            return Theme.Palette.statusDanger
        }
        return focused ? Theme.Palette.accentText : Theme.Palette.strokeStrong
    }
}

struct FieldLabel: View {
    let text: String

    var body: some View {
        Text(text)
            .font(Theme.Typography.captionFont)
            .foregroundColor(Theme.Palette.textSecondary)
    }
}

struct PrimaryButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(Theme.Typography.bodyStrongFont)
            .foregroundColor(Theme.Palette.accentOnAccent)
            .padding(.horizontal, Theme.Space.l)
            .frame(height: Theme.Size.primaryButtonHeight)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .fill(configuration.isPressed ? Theme.Palette.accentPressed : Theme.Palette.accentDefault)
            )
            .contentShape(RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous))
    }
}

struct SecondaryButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(Theme.Typography.bodyFont)
            .foregroundColor(Theme.Palette.textPrimary)
            .padding(.horizontal, Theme.Space.m)
            .frame(height: Theme.Size.controlHeight)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .fill(configuration.isPressed ? Theme.Palette.bgSurfaceSubtle : Color.clear)
            )
            .overlay(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .stroke(Theme.Palette.strokeStrong, lineWidth: Theme.Size.borderWidth)
            )
            .contentShape(RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous))
    }
}

struct TextButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(Theme.Typography.bodyFont)
            .foregroundColor(Theme.Palette.accentText)
            .opacity(configuration.isPressed ? 0.7 : 1)
            .contentShape(Rectangle())
    }
}

struct BannerView: View {
    let banner: AppModel.Banner
    let onRetry: () -> Void
    let onDismiss: () -> Void

    var body: some View {
        HStack(alignment: .top, spacing: Theme.Space.s) {
            Image(systemName: "exclamationmark.triangle.fill")
                .font(.system(size: Theme.Size.icon))
                .foregroundColor(Theme.Palette.statusDanger)

            VStack(alignment: .leading, spacing: Theme.Space.xs) {
                Text(banner.title)
                    .font(Theme.Typography.bodyStrongFont)
                    .foregroundColor(Theme.Palette.textPrimary)
                Text(banner.message)
                    .font(Theme.Typography.captionFont)
                    .foregroundColor(Theme.Palette.textSecondary)
                    .fixedSize(horizontal: false, vertical: true)
                if banner.canRetry {
                    Button(Strings.actionRetry, action: onRetry)
                        .buttonStyle(TextButtonStyle())
                }
            }

            Spacer(minLength: Theme.Space.s)

            Button(action: onDismiss) {
                Image(systemName: "xmark")
                    .font(.system(size: 10, weight: .semibold))
                    .foregroundColor(Theme.Palette.textTertiary)
            }
            .buttonStyle(.plain)
        }
        .padding(Theme.Space.m)
        .background(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .fill(Theme.Palette.statusDangerSubtle)
        )
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .stroke(Theme.Palette.strokeDefault, lineWidth: Theme.Size.borderWidth)
        )
    }
}

struct StatusDot: View {
    let kind: AppModel.StatusKind

    var body: some View {
        Circle()
            .fill(color)
            .frame(width: 8, height: 8)
    }

    private var color: Color {
        switch kind {
        case .ready:
            return Theme.Palette.statusSuccess
        case .working:
            return Theme.Palette.accentDefault
        case .success:
            return Theme.Palette.statusSuccess
        case .failure:
            return Theme.Palette.statusDanger
        }
    }
}
