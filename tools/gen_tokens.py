#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 design/ 生成两端主题与文案常量，并做一致性校验。

用法:
    python3 tools/gen_tokens.py            # 生成 4 个文件
    python3 tools/gen_tokens.py --check    # 只校验，不写文件

校验内容:
    1. 生成文件与 design/ 源文件一致（无手改漂移）
    2. 文本/背景对比度达到 WCAG AA（正文 4.5:1）
    3. 文案占位符格式正确、无重复、无空值
    4. 源码中引用的 Strings.xxx 键都存在于文案表
"""

import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOKENS_PATH = ROOT / "design" / "tokens.json"
COPY_PATH = ROOT / "design" / "copy.zh-CN.json"

GENERATED = {
    "cs_theme": ROOT / "windows" / "src" / "Theme.Generated.cs",
    "cs_strings": ROOT / "windows" / "src" / "Strings.Generated.cs",
    "swift_theme": ROOT / "macos" / "Sources" / "WallpaperTool" / "Generated" / "Theme.swift",
    "swift_strings": ROOT / "macos" / "Sources" / "WallpaperTool" / "Generated" / "Strings.swift",
}

PLACEHOLDER_RE = re.compile(r"\{([a-zA-Z][a-zA-Z0-9]*)\}")
KEY_RE = re.compile(r"^[a-z][A-Za-z0-9]*(\.[a-zA-Z][A-Za-z0-9]*)+$")
REF_RE = re.compile(r"Strings\.([A-Za-z_][A-Za-z0-9_]*)")

SWIFT_RESERVED = {
    "associatedtype", "class", "deinit", "enum", "extension", "fileprivate", "func",
    "import", "init", "inout", "internal", "let", "open", "operator", "private",
    "protocol", "public", "rethrows", "static", "struct", "subscript", "typealias",
    "var", "break", "case", "continue", "default", "defer", "do", "else",
    "fallthrough", "for", "guard", "if", "in", "repeat", "return", "switch",
    "where", "while", "as", "catch", "false", "is", "nil", "super", "self",
    "throw", "throws", "true", "try",
}


# ----------------------------- 基础工具 -----------------------------

def pascal(parts):
    return "".join(p[:1].upper() + p[1:] for p in parts if p)


def camel(parts):
    head = parts[0] if parts else ""
    return head[:1].lower() + head[1:] + "".join(p[:1].upper() + p[1:] for p in parts[1:])


def swift_ident(name):
    return "`%s`" % name if name in SWIFT_RESERVED else name


def dim_value(value):
    """'4px' / '13pt' -> 4.0 ; 纯数字直接返回。"""
    if isinstance(value, (int, float)):
        return float(value)
    text = str(value).strip().lower()
    for unit in ("px", "pt", "ms"):
        if text.endswith(unit):
            return float(text[: -len(unit)])
    return float(text)


def num(value):
    """输出稳定的小数格式：4 -> '4', 0.6 -> '0.6'。"""
    number = float(value)
    if number == int(number):
        return str(int(number))
    return ("%.4f" % number).rstrip("0").rstrip(".")


def parse_color(value):
    """支持 #RRGGBB / #AARRGGBB / rgba(r,g,b,a)。返回 (r,g,b,a[0-1])。"""
    text = str(value).strip()
    if text.startswith("#"):
        hex_part = text[1:]
        if len(hex_part) == 6:
            return (int(hex_part[0:2], 16), int(hex_part[2:4], 16), int(hex_part[4:6], 16), 1.0)
        if len(hex_part) == 8:
            return (int(hex_part[2:4], 16), int(hex_part[4:6], 16), int(hex_part[6:8], 16),
                    int(hex_part[0:2], 16) / 255.0)
        raise ValueError("无法解析颜色: %s" % value)
    match = re.match(r"^rgba?\(([^)]+)\)$", text)
    if match:
        parts = [p.strip() for p in match.group(1).split(",")]
        r, g, b = (int(parts[0]), int(parts[1]), int(parts[2]))
        a = float(parts[3]) if len(parts) > 3 else 1.0
        return (r, g, b, a)
    raise ValueError("无法解析颜色: %s" % value)


def argb_hex(value):
    r, g, b, a = parse_color(value)
    alpha = int(round(a * 255))
    return "0x%02X%02X%02X%02X" % (alpha, r, g, b)


def composite(color, base):
    """把带透明度的颜色合成到不透明底色上。"""
    r, g, b, a = color
    br, bg, bb, _ = base
    return (
        int(round(r * a + br * (1 - a))),
        int(round(g * a + bg * (1 - a))),
        int(round(b * a + bb * (1 - a))),
        1.0,
    )


def luminance(color):
    r, g, b, _ = color

    def channel(c):
        c = c / 255.0
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4

    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)


def contrast(fg, bg):
    fg = composite(fg, bg)
    l1, l2 = luminance(fg), luminance(bg)
    if l1 < l2:
        l1, l2 = l2, l1
    return (l1 + 0.05) / (l2 + 0.05)


def collect_leaves(node, path=None):
    """深度优先收集所有带 $value 的叶子。"""
    path = path or []
    if isinstance(node, dict):
        if "$value" in node:
            yield path, node
            return
        for key, value in node.items():
            if key.startswith("$"):
                continue
            yield from collect_leaves(value, path + [key])


def split_platform(path):
    if path and path[-1] in ("windows", "macos"):
        return path[:-1], path[-1]
    return path, None


# ----------------------------- 数据加载 -----------------------------

def load_json(path):
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


class Data(object):
    def __init__(self, tokens, copy):
        self.tokens = tokens
        self.copy = copy
        self.colors = {}       # base tuple -> {variant: value}
        self.semantic = {}
        self._build()

    def _build(self):
        for path, leaf in collect_leaves(self.tokens, []):
            if path[0] == "macos":
                continue
            if path[0] == "color":
                if path[-1] in ("light", "dark"):
                    base, variant = path[:-1], path[-1]
                else:
                    base, variant = split_platform(path)
                    variant = variant or "base"
                self.colors.setdefault(tuple(base), {})[variant] = leaf["$value"]

        semantic_map = self.tokens.get("macos", {}).get("semantic", {})
        self.semantic = dict(semantic_map)
        for key in self.semantic:
            base = tuple(key.split("."))
            if base not in self.colors:
                raise ValueError("macos.semantic 指向不存在的颜色 token: %s" % key)

        for base, variants in self.colors.items():
            has_light = "light" in variants
            has_dark = "dark" in variants
            if has_light != has_dark:
                raise ValueError("颜色 token %s 必须同时提供 light/dark" % ".".join(base))

    def dimension_group(self, category):
        """按平台解析 space/radius/size/opacity 等叶子。"""
        items = []
        for path, leaf in collect_leaves(self.tokens.get(category, {}), [category]):
            base, platform = split_platform(path)
            items.append((base[1:], platform, leaf))
        return items


# ----------------------------- C# 生成 -----------------------------

def cs_string(text):
    escaped = text.replace("\\", "\\\\").replace('"', '\\"')
    escaped = escaped.replace("\n", "\\n").replace("\r", "").replace("\t", "\\t")
    return '"%s"' % escaped


def cs_copy_constant(value):
    """把 {name} 依次替换为 {0} {1}，并转义字面花括号。"""
    names = []

    def repl(match):
        names.append(match.group(1))
        return "\x00%d\x00" % (len(names) - 1)

    replaced = PLACEHOLDER_RE.sub(repl, value)
    replaced = replaced.replace("{", "{{").replace("}", "}}")
    for index in range(len(names)):
        replaced = replaced.replace("\x00%d\x00" % index, "{%d}" % index)
    return replaced, names


def build_cs_theme(data):
    lines = [
        "// AUTO-GENERATED FROM design/tokens.json - DO NOT EDIT.",
        "// 重新生成: python3 tools/gen_tokens.py",
        "namespace WallpaperTool",
        "{",
        "    public static class Theme",
        "    {",
    ]

    # Palette
    lines.append("        public static class Palette")
    lines.append("        {")
    for base, variants in data.colors.items():
        name = pascal(base[1:])
        for variant in ("light", "dark"):
            if variant in variants:
                lines.append("            public const uint %s%s = %s;" % (
                    name, variant.capitalize(), argb_hex(variants[variant])))
    lines.append("        }")

    # 维度类 token
    for category, class_name in (("space", "Space"), ("radius", "Radius"), ("size", "Size"), ("opacity", "Opacity")):
        entries = []
        for base, platform, leaf in data.dimension_group(category):
            if platform == "macos":
                continue
            entries.append((pascal(base), dim_value(leaf["$value"])))
        if not entries:
            continue
        lines.append("")
        lines.append("        public static class %s" % class_name)
        lines.append("        {")
        for name, value in entries:
            lines.append("            public const double %s = %sd;" % (name, num(value)))
        lines.append("        }")

    # Typography
    entries = []
    for path, leaf in collect_leaves(data.tokens.get("type", {}), ["type"]):
        base, platform = split_platform(path)
        if platform == "macos":
            continue
        entries.append((pascal(base[1:]), leaf["$value"]))
    if entries:
        lines.append("")
        lines.append("        public static class Typography")
        lines.append("        {")
        for name, value in entries:
            lines.append("            public const string %sFontFamily = %s;" % (name, cs_string(value["fontFamily"])))
            lines.append("            public const double %sFontSize = %sd;" % (name, num(dim_value(value["fontSize"]))))
            lines.append("            public const int %sFontWeight = %d;" % (name, int(value["fontWeight"])))
            lines.append("            public const double %sLineHeight = %sd;" % (name, num(dim_value(value["lineHeight"]))))
        lines.append("        }")

    # Motion
    motion = data.tokens.get("motion", {})
    lines.append("")
    lines.append("        public static class Motion")
    lines.append("        {")
    for key, value in motion.items():
        if key == "standardEasing" or value.get("$type") != "duration":
            continue
        lines.append("            public const double %s = %sd;" % (
            key.capitalize(), num(dim_value(value["$value"]))))
    easing = motion.get("standardEasing", {}).get("$value")
    if easing:
        values = ", ".join("%sd" % num(v) for v in easing)
        lines.append("            public static readonly double[] StandardEasing = new double[] { %s };" % values)
    lines.append("        }")

    lines.extend(["    }", "}", ""])
    return "\n".join(lines)


def build_cs_strings(copy):
    lines = [
        "// AUTO-GENERATED FROM design/copy.zh-CN.json - DO NOT EDIT.",
        "// 重新生成: python3 tools/gen_tokens.py",
        "namespace WallpaperTool",
        "{",
        "    public static class Strings",
        "    {",
    ]
    for key in sorted(copy):
        value = copy[key]
        name = pascal(key.split("."))
        constant, names = cs_copy_constant(value)
        if names:
            lines.append("        // 占位符: %s" % ", ".join(names))
        lines.append("        public const string %s = %s;" % (name, cs_string(constant)))
    lines.extend([
        "",
        "        public static string Format(string template, params object[] args)",
        "        {",
        "            return string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args);",
        "        }",
        "    }",
        "}",
        "",
    ])
    return "\n".join(lines)


# ----------------------------- Swift 生成 -----------------------------

def swift_string(text, placeholders):
    """把 {name} 转为 Swift 插值；placeholders 为占位符到参数名的映射。"""
    escaped = text.replace("\\", "\\\\").replace('"', '\\"')
    escaped = escaped.replace("\n", "\\n").replace("\t", "\\t")

    def repl(match):
        name = match.group(1)
        return "\\(" + swift_ident(placeholders[name]) + ")"

    return PLACEHOLDER_RE.sub(repl, escaped)


def build_swift_theme(data):
    lines = [
        "// AUTO-GENERATED FROM design/tokens.json - DO NOT EDIT.",
        "// 重新生成: python3 tools/gen_tokens.py",
        "import SwiftUI",
        "import AppKit",
        "",
        "enum Theme {",
        "    enum Palette {",
    ]
    for base, variants in data.colors.items():
        name = camel(base[1:])
        key = ".".join(base)
        if key in data.semantic:
            lines.append("        static let %s = ThemeColor.semantic(.%s)" % (name, data.semantic[key]))
        elif "light" in variants:
            lines.append("        static let %s = ThemeColor.adaptive(light: %s, dark: %s)" % (
                name, argb_hex(variants["light"]), argb_hex(variants["dark"])))
        else:
            lines.append("        static let %s = ThemeColor.adaptive(light: %s, dark: %s)" % (
                name, argb_hex(variants["base"]), argb_hex(variants["base"])))
    lines.extend(["    }", ""])

    for category, type_name, class_name in (
            ("space", "CGFloat", "Space"),
            ("radius", "CGFloat", "Radius"),
            ("size", "CGFloat", "Size"),
            ("opacity", "Double", "Opacity")):
        entries = []
        for base, platform, leaf in data.dimension_group(category):
            if platform == "windows":
                continue
            entries.append((camel(base), num(dim_value(leaf["$value"]))))
        if not entries:
            continue
        lines.append("    enum %s {" % class_name)
        for name, value in entries:
            lines.append("        static let %s: %s = %s" % (name, type_name, value))
        lines.append("    }")
        lines.append("")

    # Typography
    entries = []
    for path, leaf in collect_leaves(data.tokens.get("type", {}), ["type"]):
        base, platform = split_platform(path)
        if platform == "windows":
            continue
        entries.append((camel(base[1:]), leaf["$value"]))
    if entries:
        lines.append("    enum Typography {")
        for name, value in entries:
            lines.append("        static let %sFontSize: CGFloat = %s" % (name, num(dim_value(value["fontSize"]))))
            lines.append("        static let %sFontWeight: Int = %d" % (name, int(value["fontWeight"])))
            lines.append("        static let %sLineHeight: CGFloat = %s" % (name, num(dim_value(value["lineHeight"]))))
            if name == "mono":
                lines.append("        static let %sFont: Font = .system(size: %sFontSize, weight: themeFontWeight(%sFontWeight), design: .monospaced)" % (name, name, name))
            else:
                lines.append("        static let %sFont: Font = .system(size: %sFontSize, weight: themeFontWeight(%sFontWeight))" % (name, name, name))
            lines.append("")
        lines.append("    }")
        lines.append("")

    # Motion
    motion = data.tokens.get("motion", {})
    lines.append("    enum Motion {")
    for key, value in motion.items():
        if key == "standardEasing" or value.get("$type") != "duration":
            continue
        lines.append("        static let %s: Double = %s" % (
            key, num(dim_value(value["$value"]) / 1000.0)))
    easing = motion.get("standardEasing", {}).get("$value")
    if easing:
        lines.append("        static let standardEasing: [Double] = [%s]" % ", ".join(num(v) for v in easing))
    lines.append("    }")
    lines.append("}")

    lines.extend([
        "",
        "enum ThemeColor {",
        "    static func semantic(_ color: NSColor) -> Color {",
        "        Color(nsColor: color)",
        "    }",
        "",
        "    static func adaptive(light: UInt32, dark: UInt32) -> Color {",
        "        Color(nsColor: NSColor(name: nil) { appearance in",
        "            let isDark = appearance.bestMatch(from: [.aqua, .darkAqua]) == .darkAqua",
        "            return NSColor(argb: isDark ? dark : light)",
        "        })",
        "    }",
        "}",
        "",
        "private func themeFontWeight(_ value: Int) -> Font.Weight {",
        "    switch value {",
        "    case ..<350: return .light",
        "    case 350..<450: return .regular",
        "    case 450..<550: return .medium",
        "    case 550..<650: return .semibold",
        "    default: return .bold",
        "    }",
        "}",
        "",
        "private extension NSColor {",
        "    convenience init(argb: UInt32) {",
        "        self.init(srgbRed: CGFloat((argb >> 16) & 0xFF) / 255.0,",
        "                  green: CGFloat((argb >> 8) & 0xFF) / 255.0,",
        "                  blue: CGFloat(argb & 0xFF) / 255.0,",
        "                  alpha: CGFloat((argb >> 24) & 0xFF) / 255.0)",
        "    }",
        "}",
        "",
    ])
    return "\n".join(lines)


def build_swift_strings(copy):
    lines = [
        "// AUTO-GENERATED FROM design/copy.zh-CN.json - DO NOT EDIT.",
        "// 重新生成: python3 tools/gen_tokens.py",
        "enum Strings {",
    ]
    for key in sorted(copy):
        value = copy[key]
        name = camel(key.split("."))
        names = PLACEHOLDER_RE.findall(value)
        if names:
            params = ", ".join("_ %s: String" % swift_ident(n) for n in names)
            body = swift_string(value, {n: n for n in names})
            lines.append("    static func %s(%s) -> String {" % (name, params))
            lines.append('        return "%s"' % body)
            lines.append("    }")
        else:
            lines.append('    static let %s = "%s"' % (name, swift_string(value, {})))
    lines.append("}")
    lines.append("")
    return "\n".join(lines)


# ----------------------------- 校验 -----------------------------

def validate_copy(copy):
    errors = []
    if not copy:
        errors.append("文案表为空")
    for key, value in copy.items():
        if not KEY_RE.match(key):
            errors.append("文案键格式不合法: %s" % key)
        if not isinstance(value, str) or not value.strip():
            errors.append("文案值为空: %s" % key)
            continue
        names = PLACEHOLDER_RE.findall(value)
        if len(names) != len(set(names)):
            errors.append("占位符重复: %s" % key)
        leftover = PLACEHOLDER_RE.sub("", value)
        if "{" in leftover or "}" in leftover:
            errors.append("存在未闭合或格式错误的占位符: %s" % key)
        for name in names:
            if not re.match(r"^[a-z][a-zA-Z0-9]*$", name):
                errors.append("占位符命名不合法: %s (%s)" % (key, name))
    return errors


def resolve_color_pair(data, name):
    base = tuple(name.split("."))
    variants = data.colors.get(base)
    if not variants:
        raise ValueError("缺少颜色 token: %s" % name)
    return variants


def validate_contrast(data):
    """返回 (报告行列表, 失败列表)。"""
    report = []
    failures = []
    checks = []

    for scheme in ("light", "dark"):
        canvas = parse_color(resolve_color_pair(data, "color.bg.canvas")[scheme])
        surface = parse_color(resolve_color_pair(data, "color.bg.surface")[scheme])
        for text_name in ("color.text.primary", "color.text.secondary", "color.text.tertiary"):
            fg = parse_color(resolve_color_pair(data, text_name)[scheme])
            for bg_name, bg in (("canvas", canvas), ("surface", surface)):
                checks.append(("%s / %s(%s)" % (text_name, bg_name, scheme), fg, bg))
        accent = parse_color(resolve_color_pair(data, "color.accent.default")[scheme])
        on_accent = parse_color(resolve_color_pair(data, "color.accent.onAccent")[scheme])
        checks.append(("accent.onAccent / accent(%s)" % scheme, on_accent, accent))
        for status_name in ("color.status.success", "color.status.warning", "color.status.danger"):
            fg = parse_color(resolve_color_pair(data, status_name)[scheme])
            for bg_name, bg in (("canvas", canvas), ("surface", surface)):
                checks.append(("%s / %s(%s)" % (status_name, bg_name, scheme), fg, bg))

    for label, fg, bg in checks:
        ratio = contrast(fg, bg)
        report.append("  %-42s %5.2f : 1" % (label, ratio))
        if ratio < 4.5:
            failures.append("%s 对比度仅 %.2f:1（要求 >= 4.5:1）" % (label, ratio))
    return report, failures


def min_contrast(report):
    """从报告行里找出最低对比度。"""
    best = None
    for line in report:
        match = re.search(r"([0-9]+\.[0-9]+) : 1$", line.strip())
        if match:
            value = float(match.group(1))
            if best is None or value < best[0]:
                best = (value, line.split(":", 1)[0].strip())
    return best


def validate_source_references(copy):
    """扫描源码里引用的 Strings.xxx，确认文案键存在。"""
    errors = []
    allowed = {"Format"}
    known = {pascal(key.split(".")) for key in copy}
    known |= {camel(key.split(".")) for key in copy}

    search_roots = [ROOT / "windows" / "src", ROOT / "macos" / "Sources"]
    for root in search_roots:
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.suffix not in (".cs", ".swift"):
                continue
            text = path.read_text(encoding="utf-8", errors="ignore")
            for name in REF_RE.findall(text):
                if name in allowed or name in known:
                    continue
                errors.append("%s 引用了不存在的文案键: Strings.%s" % (path.relative_to(ROOT), name))
    return errors


def diff_summary(expected, actual):
    exp_lines = expected.splitlines()
    act_lines = actual.splitlines() if actual is not None else []
    for index in range(max(len(exp_lines), len(act_lines))):
        exp = exp_lines[index] if index < len(exp_lines) else "<缺失>"
        act = act_lines[index] if index < len(act_lines) else "<缺失>"
        if exp != act:
            return "第 %d 行不一致\n    期望: %s\n    实际: %s" % (index + 1, exp, act)
    return "内容一致"


# ----------------------------- 主流程 -----------------------------

def main():
    args = sys.argv[1:]
    check_only = "--check" in args

    try:
        tokens = load_json(TOKENS_PATH)
        copy = load_json(COPY_PATH)
    except (OSError, ValueError) as error:
        print("读取设计源文件失败: %s" % error)
        return 1

    if not isinstance(copy, dict):
        print("design/copy.zh-CN.json 必须是对象")
        return 1

    try:
        data = Data(tokens, copy)
    except ValueError as error:
        print("设计 token 结构错误: %s" % error)
        return 1

    outputs = {
        "cs_theme": build_cs_theme(data),
        "cs_strings": build_cs_strings(copy),
        "swift_theme": build_swift_theme(data),
        "swift_strings": build_swift_strings(copy),
    }

    errors = []
    errors.extend(validate_copy(copy))
    report, contrast_failures = validate_contrast(data)
    errors.extend(contrast_failures)
    errors.extend(validate_source_references(copy))

    if contrast_failures:
        print("对比度校验失败:")
        for line in report:
            print(line)
    else:
        lowest = min_contrast(report)
        if lowest:
            print("对比度校验通过（正文/标签 >= 4.5:1，最低 %.2f:1，%s）" % lowest)
        else:
            print("对比度校验通过（正文/标签 >= 4.5:1）")

    drift = []
    for name, expected in outputs.items():
        path = GENERATED[name]
        actual = path.read_text(encoding="utf-8") if path.exists() else None
        if actual != expected:
            drift.append((path, expected, actual))

    if check_only:
        if drift:
            for path, expected, actual in drift:
                print("生成文件已过期: %s（%s）" % (path.relative_to(ROOT), diff_summary(expected, actual)))
            errors.append("生成文件与 design/ 不一致，请运行 python3 tools/gen_tokens.py")
        if errors:
            print("校验失败:")
            for error in errors:
                print("  - %s" % error)
            return 1
        print("tokens OK, contrast OK, copy keys OK")
        return 0

    # 写入模式
    if errors:
        print("校验未通过，已停止生成:")
        for error in errors:
            print("  - %s" % error)
        return 1

    for name, content in outputs.items():
        path = GENERATED[name]
        path.parent.mkdir(parents=True, exist_ok=True)
        old = path.read_text(encoding="utf-8") if path.exists() else None
        if old == content:
            print("未变化: %s" % path.relative_to(ROOT))
            continue
        with path.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)
        print("已生成: %s" % path.relative_to(ROOT))

    print("完成：4 个生成文件已与 design/ 同步")
    return 0


if __name__ == "__main__":
    sys.exit(main())
