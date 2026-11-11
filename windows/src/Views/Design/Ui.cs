using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using WallpaperTool;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 设计 token 的运行时适配层：DPI 缩放、浅色/深色解析、字体缓存。
    /// 视图代码只从这里取色与字号，不出现字面量，对应 design/spec.md 第 3、4 节。
    /// </summary>
    internal static class Ui
    {
        private static float _scale = 1f;
        private static bool _dark;

        public static void Initialize(float dpi)
        {
            if (dpi <= 0f)
            {
                dpi = 96f;
            }
            _scale = dpi / 96f;
            _dark = DetectDark();
        }

        public static float Scale
        {
            get { return _scale; }
        }

        public static bool IsDark
        {
            get { return _dark; }
        }

        /// <summary>设计值 → 设备像素。</summary>
        public static int S(double dip)
        {
            return (int)Math.Round(dip * _scale);
        }

        public static float Sf(double dip)
        {
            return (float)(dip * _scale);
        }

        public static Color Col(uint light, uint dark)
        {
            return Color.FromArgb(unchecked((int)(_dark ? dark : light)));
        }

        private static bool DetectDark()
        {
            string forced = Environment.GetEnvironmentVariable("WALLPAPERTOOL_THEME");
            if (!string.IsNullOrEmpty(forced))
            {
                return string.Equals(forced, "dark", StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value is int)
                        {
                            return (int)value == 0;
                        }
                    }
                }
            }
            catch
            {
                // 读不到系统偏好时按浅色处理
            }
            return false;
        }

        /// <summary>语义色板，名字与 tokens.json 的 color.* 一一对应。</summary>
        public static class Palette
        {
            public static Color Canvas
            {
                get { return Col(Theme.Palette.BgCanvasLight, Theme.Palette.BgCanvasDark); }
            }

            public static Color Surface
            {
                get { return Col(Theme.Palette.BgSurfaceLight, Theme.Palette.BgSurfaceDark); }
            }

            public static Color SurfaceSubtle
            {
                get { return Col(Theme.Palette.BgSurfaceSubtleLight, Theme.Palette.BgSurfaceSubtleDark); }
            }

            public static Color Stroke
            {
                get { return Col(Theme.Palette.StrokeDefaultLight, Theme.Palette.StrokeDefaultDark); }
            }

            public static Color StrokeStrong
            {
                get { return Col(Theme.Palette.StrokeStrongLight, Theme.Palette.StrokeStrongDark); }
            }

            public static Color TextPrimary
            {
                get { return Col(Theme.Palette.TextPrimaryLight, Theme.Palette.TextPrimaryDark); }
            }

            public static Color TextSecondary
            {
                get { return Col(Theme.Palette.TextSecondaryLight, Theme.Palette.TextSecondaryDark); }
            }

            public static Color TextTertiary
            {
                get { return Col(Theme.Palette.TextTertiaryLight, Theme.Palette.TextTertiaryDark); }
            }

            public static Color Accent
            {
                get { return Col(Theme.Palette.AccentDefaultLight, Theme.Palette.AccentDefaultDark); }
            }

            public static Color AccentHover
            {
                get { return Col(Theme.Palette.AccentHoverLight, Theme.Palette.AccentHoverDark); }
            }

            public static Color AccentPressed
            {
                get { return Col(Theme.Palette.AccentPressedLight, Theme.Palette.AccentPressedDark); }
            }

            public static Color OnAccent
            {
                get { return Col(Theme.Palette.AccentOnAccentLight, Theme.Palette.AccentOnAccentDark); }
            }

            public static Color AccentText
            {
                get { return Col(Theme.Palette.AccentTextLight, Theme.Palette.AccentTextDark); }
            }

            public static Color AccentSubtle
            {
                get { return Col(Theme.Palette.AccentSubtleLight, Theme.Palette.AccentSubtleDark); }
            }

            public static Color Success
            {
                get { return Col(Theme.Palette.StatusSuccessLight, Theme.Palette.StatusSuccessDark); }
            }

            public static Color SuccessSubtle
            {
                get { return Col(Theme.Palette.StatusSuccessSubtleLight, Theme.Palette.StatusSuccessSubtleDark); }
            }

            public static Color Warning
            {
                get { return Col(Theme.Palette.StatusWarningLight, Theme.Palette.StatusWarningDark); }
            }

            public static Color WarningSubtle
            {
                get { return Col(Theme.Palette.StatusWarningSubtleLight, Theme.Palette.StatusWarningSubtleDark); }
            }

            public static Color Danger
            {
                get { return Col(Theme.Palette.StatusDangerLight, Theme.Palette.StatusDangerDark); }
            }

            public static Color DangerSubtle
            {
                get { return Col(Theme.Palette.StatusDangerSubtleLight, Theme.Palette.StatusDangerSubtleDark); }
            }

            /// <summary>开关滑块：两种外观都接近纸白，不参与主题切换。</summary>
            public static Color Knob
            {
                get { return Col(0xFFFFFFFF, 0xFFFAF9F5); }
            }
        }

        /// <summary>语义字号：只用 token 里的角色，不逐处调字号。</summary>
        public static class Fonts
        {
            private static Font _title;
            private static Font _sectionTitle;
            private static Font _body;
            private static Font _bodyStrong;
            private static Font _caption;
            private static Font _mono;

            public static Font Title
            {
                get
                {
                    if (_title == null)
                    {
                        _title = Make(Theme.Typography.WindowTitleFontFamily,
                            Theme.Typography.WindowTitleFontSize,
                            Theme.Typography.WindowTitleFontWeight);
                    }
                    return _title;
                }
            }

            public static Font SectionTitle
            {
                get
                {
                    if (_sectionTitle == null)
                    {
                        _sectionTitle = Make(Theme.Typography.SectionTitleFontFamily,
                            Theme.Typography.SectionTitleFontSize,
                            Theme.Typography.SectionTitleFontWeight);
                    }
                    return _sectionTitle;
                }
            }

            public static Font Body
            {
                get
                {
                    if (_body == null)
                    {
                        _body = Make(Theme.Typography.BodyFontFamily,
                            Theme.Typography.BodyFontSize,
                            Theme.Typography.BodyFontWeight);
                    }
                    return _body;
                }
            }

            public static Font BodyStrong
            {
                get
                {
                    if (_bodyStrong == null)
                    {
                        _bodyStrong = Make(Theme.Typography.BodyStrongFontFamily,
                            Theme.Typography.BodyStrongFontSize,
                            Theme.Typography.BodyStrongFontWeight);
                    }
                    return _bodyStrong;
                }
            }

            public static Font Caption
            {
                get
                {
                    if (_caption == null)
                    {
                        _caption = Make(Theme.Typography.CaptionFontFamily,
                            Theme.Typography.CaptionFontSize,
                            Theme.Typography.CaptionFontWeight);
                    }
                    return _caption;
                }
            }

            public static Font Mono
            {
                get
                {
                    if (_mono == null)
                    {
                        _mono = Make(Theme.Typography.MonoFontFamily,
                            Theme.Typography.MonoFontSize,
                            Theme.Typography.MonoFontWeight);
                    }
                    return _mono;
                }
            }

            private static Font Make(string familyList, double px, int weight)
            {
                string family = familyList;
                int comma = family.IndexOf(',');
                if (comma > 0)
                {
                    family = family.Substring(0, comma).Trim();
                }

                FontStyle style = weight >= 550 ? FontStyle.Bold : FontStyle.Regular;
                float size = Sf(px);

                try
                {
                    return new Font(family, size, style, GraphicsUnit.Pixel);
                }
                catch
                {
                    return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel);
                }
            }
        }
    }
}
