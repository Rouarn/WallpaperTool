import SwiftUI

struct MainView: View {
    @ObservedObject var model: AppModel

    var body: some View {
        VStack(spacing: 0) {
            header
            Divider().overlay(Theme.Palette.strokeDefault)

            ScrollView {
                VStack(spacing: Theme.Space.l) {
                    if let banner = model.banner {
                        BannerView(banner: banner,
                                   onRetry: model.retry,
                                   onDismiss: model.clearBanner)
                    }
                    PreviewCard(model: model)
                    SourceCard(model: model)
                    AutoUpdateCard(model: model)
                    LogSection(model: model)
                }
                .padding(Theme.Space.xl)
                .frame(maxWidth: .infinity)
            }

            StatusBar(model: model)
        }
        .background(Theme.Palette.bgCanvas)
    }

    private var header: some View {
        VStack(spacing: 0) {
            HStack(alignment: .center, spacing: Theme.Space.m) {
                BrandStarMark(size: Theme.Size.iconLarge, isSpinning: model.isDownloading)

                VStack(alignment: .leading, spacing: 2) {
                    Text(Strings.appName)
                        .font(Theme.Typography.windowTitleFont)
                        .foregroundColor(Theme.Palette.textPrimary)
                    Text(Strings.appTagline)
                        .font(Theme.Typography.captionFont)
                        .foregroundColor(Theme.Palette.textSecondary)
                }

                Spacer()

                if model.isDownloading {
                    Button(Strings.actionCancel) { model.cancelDownload() }
                        .buttonStyle(SecondaryButtonStyle())
                    Button(downloadingLabel) {}
                        .buttonStyle(PrimaryButtonStyle())
                        .disabled(true)
                        .opacity(Theme.Opacity.disabled)
                } else {
                    Button(Strings.actionDownloadNow) { model.downloadNow() }
                        .buttonStyle(PrimaryButtonStyle())
                }
            }
            .padding(.horizontal, Theme.Space.xl)
            .frame(height: Theme.Size.headerHeight)

            if model.isDownloading {
                progressBar
            }
        }
        .background(Theme.Palette.bgSurface)
    }

    private var downloadingLabel: String {
        if let progress = model.progress {
            return Strings.actionDownloading(String(Int(progress * 100)))
        }
        return Strings.statusDownloadingUnknown
    }

    private var progressBar: some View {
        Group {
            if let progress = model.progress {
                ProgressView(value: progress)
                    .progressViewStyle(.linear)
                    .tint(Theme.Palette.accentDefault)
            } else {
                ProgressView()
                    .progressViewStyle(.linear)
                    .tint(Theme.Palette.accentDefault)
            }
        }
        .frame(height: Theme.Size.progressBar)
    }
}

private struct PreviewCard: View {
    @ObservedObject var model: AppModel

    var body: some View {
        Card(title: Strings.sectionPreview) {
            if let preview = model.preview {
                HStack(alignment: .top, spacing: Theme.Space.l) {
                    thumbnail
                    VStack(alignment: .leading, spacing: Theme.Space.xs) {
                        Text(dimensionsText(preview))
                            .font(Theme.Typography.bodyStrongFont)
                            .foregroundColor(Theme.Palette.textPrimary)
                        Text(Formatters.bytes(preview.byteSize))
                            .font(Theme.Typography.captionFont)
                            .foregroundColor(Theme.Palette.textSecondary)
                        Text(Strings.metaUpdatedAt(Formatters.short(preview.modifiedAt)))
                            .font(Theme.Typography.captionFont)
                            .foregroundColor(Theme.Palette.textTertiary)

                        Spacer(minLength: Theme.Space.s)

                        HStack(spacing: Theme.Space.s) {
                            Button(Strings.actionOpenFolder) { model.openFolder() }
                                .buttonStyle(SecondaryButtonStyle())
                            Button(Strings.actionReapply) { model.reapplyWallpaper() }
                                .buttonStyle(SecondaryButtonStyle())
                        }
                    }
                    Spacer(minLength: 0)
                }
            } else {
                emptyState
            }
        }
    }

    private var thumbnail: some View {
        Group {
            if let image = model.previewImage {
                Image(nsImage: image)
                    .resizable()
                    .aspectRatio(contentMode: .fill)
            } else {
                Rectangle()
                    .fill(Theme.Palette.bgSurfaceSubtle)
                    .overlay(
                        Image(systemName: "photo")
                            .font(.system(size: Theme.Size.iconLarge))
                            .foregroundColor(Theme.Palette.textTertiary)
                    )
            }
        }
        .frame(width: 220, height: 124)
        .clipped()
        .clipShape(RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous))
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                .stroke(Theme.Palette.strokeDefault, lineWidth: Theme.Size.borderWidth)
        )
    }

    private var emptyState: some View {
        HStack(spacing: Theme.Space.m) {
            RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                .strokeBorder(Theme.Palette.strokeStrong, style: StrokeStyle(lineWidth: 1, dash: [4, 4]))
                .frame(width: 96, height: 54)
                .overlay(
                    BrandStarMark(size: 28)
                )

            VStack(alignment: .leading, spacing: Theme.Space.xs) {
                Text(Strings.emptyPreviewTitle)
                    .font(Theme.Typography.bodyStrongFont)
                    .foregroundColor(Theme.Palette.textPrimary)
                Text(Strings.emptyPreviewBody)
                    .font(Theme.Typography.captionFont)
                    .foregroundColor(Theme.Palette.textSecondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
            Spacer(minLength: 0)
        }
    }

    private func dimensionsText(_ preview: PreviewInfo) -> String {
        guard preview.width > 0, preview.height > 0 else {
            return Strings.metaUnknownSize
        }
        return Strings.metaDimensions(String(preview.width), String(preview.height))
    }
}

private struct SourceCard: View {
    @ObservedObject var model: AppModel

    var body: some View {
        Card(title: Strings.sectionSource) {
            VStack(alignment: .leading, spacing: Theme.Space.xs) {
                FieldLabel(text: Strings.fieldUrl)
                TokenTextField(text: model.urlBinding,
                               isInvalid: model.urlError != nil,
                               onSubmit: model.commitURL)
                if let error = model.urlError {
                    Text(error)
                        .font(Theme.Typography.captionFont)
                        .foregroundColor(Theme.Palette.statusDanger)
                }
            }

            VStack(alignment: .leading, spacing: Theme.Space.xs) {
                FieldLabel(text: Strings.fieldDestDir)
                HStack(spacing: Theme.Space.s) {
                    TokenTextField(text: model.destDirBinding)
                    Button(Strings.actionChooseFolder) { model.chooseFolder() }
                        .buttonStyle(SecondaryButtonStyle())
                }
            }
        }
    }
}

private struct AutoUpdateCard: View {
    @ObservedObject var model: AppModel

    var body: some View {
        Card(title: Strings.sectionAutoUpdate) {
            HStack(alignment: .top, spacing: Theme.Space.m) {
                VStack(alignment: .leading, spacing: Theme.Space.xs) {
                    Text(Strings.fieldAutoUpdate)
                        .font(Theme.Typography.bodyFont)
                        .foregroundColor(Theme.Palette.textPrimary)
                    if model.launchAgentEnabled {
                        Text(Strings.hintLaunchAgentApproval)
                            .font(Theme.Typography.captionFont)
                            .foregroundColor(Theme.Palette.textSecondary)
                            .fixedSize(horizontal: false, vertical: true)
                    }
                }
                Spacer(minLength: 0)
                Toggle("", isOn: Binding(
                    get: { model.launchAgentEnabled },
                    set: { model.setAutoUpdate($0) }
                ))
                .labelsHidden()
                .toggleStyle(.switch)
                .tint(Theme.Palette.accentDefault)
            }

            if let error = model.autoUpdateError {
                Text(error)
                    .font(Theme.Typography.captionFont)
                    .foregroundColor(Theme.Palette.statusDanger)
                    .fixedSize(horizontal: false, vertical: true)
            }

            Divider().overlay(Theme.Palette.strokeDefault)

            HStack(alignment: .top, spacing: Theme.Space.m) {
                VStack(alignment: .leading, spacing: Theme.Space.xs) {
                    Text(Strings.fieldSetWallpaper)
                        .font(Theme.Typography.bodyFont)
                        .foregroundColor(Theme.Palette.textPrimary)
                    Text(model.config.setWallpaperAfterDownload
                         ? Strings.hintSetWallpaperOn
                         : Strings.hintSetWallpaperOff)
                        .font(Theme.Typography.captionFont)
                        .foregroundColor(Theme.Palette.textSecondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
                Spacer(minLength: 0)
                Toggle("", isOn: Binding(
                    get: { model.config.setWallpaperAfterDownload },
                    set: { model.setAutoSetWallpaper($0) }
                ))
                .labelsHidden()
                .toggleStyle(.switch)
                .tint(Theme.Palette.accentDefault)
            }

            Divider().overlay(Theme.Palette.strokeDefault)

            HStack(alignment: .top, spacing: Theme.Space.m) {
                VStack(alignment: .leading, spacing: Theme.Space.xs) {
                    Text(Strings.fieldKeepHistory)
                        .font(Theme.Typography.bodyFont)
                        .foregroundColor(Theme.Palette.textPrimary)
                    Text(model.config.keepHistory
                         ? Strings.hintKeepHistoryOn
                         : Strings.hintKeepHistoryOff)
                        .font(Theme.Typography.captionFont)
                        .foregroundColor(Theme.Palette.textSecondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
                Spacer(minLength: 0)
                Toggle("", isOn: Binding(
                    get: { model.config.keepHistory },
                    set: { model.setKeepHistory($0) }
                ))
                .labelsHidden()
                .toggleStyle(.switch)
                .tint(Theme.Palette.accentDefault)
            }

            Divider().overlay(Theme.Palette.strokeDefault)

            HStack(spacing: Theme.Space.s) {
                FieldLabel(text: Strings.fieldLastAutoUpdate)
                Text(model.updatedAtText)
                    .font(Theme.Typography.captionFont)
                    .foregroundColor(Theme.Palette.textPrimary)
                Spacer()
                Button(Strings.actionTestNow) { model.downloadNow() }
                    .buttonStyle(TextButtonStyle())
                    .disabled(model.isDownloading)
                Button(Strings.actionOpenLoginItems) { model.openLoginItemsSettings() }
                    .buttonStyle(TextButtonStyle())
            }
        }
    }
}

private struct LogSection: View {
    @ObservedObject var model: AppModel

    var body: some View {
        Card(title: Strings.logTitle) {
            HStack(spacing: Theme.Space.s) {
                Button(model.isLogExpanded ? Strings.actionHideLog : Strings.hintLogCollapsed) {
                    withAnimation(.easeOut(duration: Theme.Motion.fast)) {
                        model.isLogExpanded.toggle()
                    }
                }
                .buttonStyle(TextButtonStyle())

                Spacer()

                Button(Strings.actionOpenLogFile) { model.openLogFile() }
                    .buttonStyle(TextButtonStyle())
            }

            if model.isLogExpanded {
                FieldLabel(text: Strings.fieldLogFile)
                HStack(spacing: Theme.Space.s) {
                    TokenTextField(text: model.logFileBinding, onSubmit: model.openLogFile)
                    Button(Strings.actionCopyLog) { model.copyLogs() }
                        .buttonStyle(SecondaryButtonStyle())
                }

                logContent
            }
        }
    }

    private var logContent: some View {
        ScrollViewReader { proxy in
            ScrollView {
                Text(model.logs.isEmpty ? Strings.logEmpty : model.logs.joined(separator: "\n"))
                    .font(Theme.Typography.monoFont)
                    .foregroundColor(Theme.Palette.textSecondary)
                    .textSelection(.enabled)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(Theme.Space.m)

                Color.clear.frame(height: 1).id("logBottom")
            }
            .frame(height: Theme.Size.logPanelHeight)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .fill(Theme.Palette.bgSurfaceSubtle)
            )
            .overlay(
                RoundedRectangle(cornerRadius: Theme.Radius.control, style: .continuous)
                    .stroke(Theme.Palette.strokeDefault, lineWidth: Theme.Size.borderWidth)
            )
            .onChange(of: model.logs.count) { _ in
                withAnimation(.easeOut(duration: Theme.Motion.fast)) {
                    proxy.scrollTo("logBottom", anchor: .bottom)
                }
            }
        }
    }
}

private struct StatusBar: View {
    @ObservedObject var model: AppModel

    var body: some View {
        HStack(spacing: Theme.Space.s) {
            StatusDot(kind: model.statusKind)
            Text(model.statusText)
                .font(Theme.Typography.captionFont)
                .foregroundColor(Theme.Palette.textSecondary)
                .lineLimit(1)
            Spacer()
            Text(Strings.aboutVersion(AppInfo.version))
                .font(Theme.Typography.captionFont)
                .foregroundColor(Theme.Palette.textTertiary)
        }
        .padding(.horizontal, Theme.Space.xl)
        .frame(height: Theme.Size.statusBarHeight)
        .background(Theme.Palette.bgSurface)
        .overlay(alignment: .top) {
            Rectangle()
                .fill(Theme.Palette.strokeDefault)
                .frame(height: 1)
        }
        .animation(.easeOut(duration: Theme.Motion.fast), value: model.statusText)
    }
}
