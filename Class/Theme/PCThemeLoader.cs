using Microsoft.Win32;
using Newtonsoft.Json;
using Phobos.Class.Plugin.BuiltIn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace Phobos.Class.Theme
{
    /// <summary>
    /// 主题配置文件加载器
    /// </summary>
    public class PCThemeLoader
    {
        // 标准变量: ${category.property}
        private static readonly Regex StandardVariablePattern = new(@"\$\{(\w+)\.(\w+)\}", RegexOptions.Compiled);

        // Phobos 特殊变量: ${Phobos.SystemAccent}
        private static readonly Regex PhobosSimplePattern = new(@"\$\{Phobos\.(\w+)\}", RegexOptions.Compiled);

        // Phobos 带参数变量: ${Phobos.AdaptiveColor:参数}
        private static readonly Regex PhobosParamPattern = new(@"\$\{Phobos\.(\w+):([^}]+)\}", RegexOptions.Compiled);

        // 缓存系统强调色
        private static Color? _cachedSystemAccent;
        private static DateTime _lastAccentCheck = DateTime.MinValue;
        private static readonly TimeSpan AccentCacheTimeout = TimeSpan.FromSeconds(5);

        #region File Operations

        /// <summary>
        /// 从文件加载主题配置
        /// </summary>
        public static PCThemeConfig? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader", $"Theme file not found: {filePath}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<PCThemeConfig>(json);
                return config;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader.Load", $"Failed to load theme: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从 JSON 字符串加载主题配置
        /// </summary>
        public static PCThemeConfig? LoadFromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<PCThemeConfig>(json);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader.Json.Parser", $"Failed to parse theme JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存主题配置到文件
        /// </summary>
        public static bool SaveToFile(PCThemeConfig config, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Utils.IO.PUFileSystem.Instance.CreateFullFolders(directory);
                }

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader.Save", $"Failed to save theme: {ex.Message}");
                return false;
            }
        }

        #endregion

            /// <summary>
            /// Create a minimal ThemeControls instance referencing color variables, so a colors-only theme still contains
            /// controls definitions for compatibility across the app (XAML expects named style keys).
            /// This produces simple defaults that refer to colors and dimensions from the config.
            /// </summary>
            public static ThemeControls GenerateControlsFromColors(PCThemeConfig config)
            {
                var controls = new ThemeControls();

                controls.Button = new ButtonStyle
                {
                    Background = "${colors.primary}",
                    BackgroundHover = "${colors.primaryHover}",
                    BackgroundPressed = "${colors.primaryPressed}",
                    BackgroundDisabled = "${colors.primaryDisabled}",
                    Foreground = "${colors.background1}",
                    ForegroundDisabled = "${colors.foreground4}",
                    BorderColor = "transparent",
                    BorderRadius = "${dimensions.borderRadius}",
                    FontSize = "${fonts.sizeMd}",
                    FontWeight = "${fonts.weightMedium}"
                };

                controls.ButtonSecondary = new ButtonSecondaryStyle
                {
                    Background = "transparent",
                    BackgroundHover = "${colors.background4}",
                    BackgroundPressed = "${colors.background3}",
                    Foreground = "${colors.foreground1}",
                    BorderColor = "${colors.border}"
                };

                controls.TextBox = new TextBoxStyle
                {
                    Background = "${colors.background2}",
                    BackgroundFocused = "${colors.background1}",
                    Foreground = "${colors.foreground1}",
                    ForegroundPlaceholder = "${colors.foreground4}",
                    BorderColor = "${colors.border}",
                    BorderColorFocused = "${colors.borderFocus}",
                    BorderRadius = "${dimensions.borderRadius}",
                    FontSize = "${fonts.sizeMd}"
                };

                controls.Label = new LabelStyle
                {
                    Foreground = "${colors.foreground1}",
                    FontSize = "${fonts.sizeMd}" 
                };

                controls.ListBox = new ListBoxStyle
                {
                    Background = "${colors.background2}",
                    Foreground = "${colors.foreground1}",
                    ItemBackground = "transparent",
                    ItemBackgroundHover = "${colors.background4}",
                    ItemBackgroundSelected = "${colors.primary}",
                    ItemForegroundSelected = "${colors.background1}",
                    BorderColor = "${colors.border}"
                };

                controls.ScrollBar = new ScrollBarStyle
                {
                    Background = "transparent",
                    ThumbBackground = "${colors.scrollbar}",
                    ThumbBackgroundHover = "${colors.scrollbarHover}",
                    Width = 10,
                    ThumbRadius = 5
                };

                controls.Window = new WindowStyle
                {
                    Background = "${colors.background1}",
                    TitleBarBackground = "${colors.background2}",
                    TitleBarForeground = "${colors.foreground1}",
                    BorderColor = "${colors.border}",
                    ShadowColor = "${colors.shadow}",
                    ShadowBlurRadius = 20
                };

                controls.TitleBarButton = new TitleBarButtonStyle
                {
                    Background = "transparent",
                    BackgroundHover = "${colors.background4}",
                    Foreground = "${colors.foreground3}",
                    ForegroundHover = "${colors.foreground1}",
                    CloseBackgroundHover = "#F85149",
                    CloseForegroundHover = "#FFFFFF"
                };

                return controls;
            }

        #region Variable Resolution

        /// <summary>
        /// 解析变量引用
        /// 支持:
        /// - ${colors.primary} - 标准配置变量
        /// - ${fonts.sizeMd} - 字体变量
        /// - ${dimensions.borderRadius} - 尺寸变量
        /// - ${Phobos.SystemAccent} - 系统强调色
        /// - ${Phobos.AdaptiveColor:${colors.background1}} - 自适应前景色
        /// </summary>
        public static string ResolveVariable(string value, PCThemeConfig config)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains("${"))
                return value;

            var result = value;

            // 1. 首先解析 Phobos 带参数变量 (需要先解析内部变量)
            result = PhobosParamPattern.Replace(result, match =>
            {
                var funcName = match.Groups[1].Value;
                var parameter = match.Groups[2].Value;

                // 递归解析参数中的变量
                var resolvedParam = ResolveVariable(parameter, config);

                return ResolvePhobosFunction(funcName, resolvedParam, config);
            });

            // 2. 解析 Phobos 简单变量
            result = PhobosSimplePattern.Replace(result, match =>
            {
                var funcName = match.Groups[1].Value;
                return ResolvePhobosFunction(funcName, null, config);
            });

            // 3. 解析标准变量
            result = StandardVariablePattern.Replace(result, match =>
            {
                var category = match.Groups[1].Value.ToLowerInvariant();
                var property = match.Groups[2].Value;

                var resolved = category switch
                {
                    "colors" => GetColorValue(config.Colors, property),
                    "fonts" => GetFontValue(config.Fonts, property),
                    "dimensions" => GetDimensionValue(config.Dimensions, property),
                    _ => match.Value
                };

                if (string.IsNullOrEmpty(resolved))
                {
                    PCLoggerPlugin.Warning("Phobos.Theme.Loader.Resolver", $"Warning: Failed to resolve ${{{category}.{property}}}");
                }

                return resolved;
            });

            return result;
        }

        /// <summary>
        /// 解析 Phobos 特殊函数
        /// </summary>
        private static string ResolvePhobosFunction(string funcName, string? parameter, PCThemeConfig config)
        {
            return funcName.ToLowerInvariant() switch
            {
                "systemaccent" => GetSystemAccentColor(),
                "adaptivecolor" => GetAdaptiveColor(parameter),
                "adaptiveforeground" => GetAdaptiveColor(parameter), // 别名
                "contrastcolor" => GetAdaptiveColor(parameter), // 别名
                _ => $"${{Phobos.{funcName}}}" // 未知函数，保持原样
            };
        }

        #endregion

        #region System Accent Color

        /// <summary>
        /// 获取 Windows 系统强调色
        /// </summary>
        public static string GetSystemAccentColor()
        {
            var color = GetSystemAccentColorValue();
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// 获取系统强调色 Color 值
        /// </summary>
        public static Color GetSystemAccentColorValue()
        {
            // 检查缓存
            if (_cachedSystemAccent.HasValue &&
                DateTime.Now - _lastAccentCheck < AccentCacheTimeout)
            {
                return _cachedSystemAccent.Value;
            }

            Color accentColor = Color.FromRgb(0, 120, 212); // 默认 Windows 蓝

            try
            {
                // 方法1: 通过 DWM API (Windows 10/11)
                if (TryGetDwmAccentColor(out var dwmColor))
                {
                    accentColor = dwmColor;
                }
                // 方法2: 通过注册表
                else if (TryGetRegistryAccentColor(out var regColor))
                {
                    accentColor = regColor;
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader.AccentColor", $"Failed to get system accent color: {ex.Message}");
            }

            _cachedSystemAccent = accentColor;
            _lastAccentCheck = DateTime.Now;

            return accentColor;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

        private static bool TryGetDwmAccentColor(out Color color)
        {
            color = default;

            try
            {
                var result = DwmGetColorizationColor(out uint colorization, out _);
                if (result == 0)
                {
                    // ARGB 格式
                    color = Color.FromArgb(
                        (byte)((colorization >> 24) & 0xFF),
                        (byte)((colorization >> 16) & 0xFF),
                        (byte)((colorization >> 8) & 0xFF),
                        (byte)(colorization & 0xFF));
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TryGetRegistryAccentColor(out Color color)
        {
            color = default;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\DWM");

                if (key?.GetValue("AccentColor") is int accentValue)
                {
                    // ABGR 格式 (注意顺序)
                    color = Color.FromArgb(
                        (byte)((accentValue >> 24) & 0xFF),
                        (byte)(accentValue & 0xFF),
                        (byte)((accentValue >> 8) & 0xFF),
                        (byte)((accentValue >> 16) & 0xFF));
                    return true;
                }

                // 尝试 ColorizationColor
                if (key?.GetValue("ColorizationColor") is int colorizationValue)
                {
                    color = Color.FromArgb(
                        (byte)((colorizationValue >> 24) & 0xFF),
                        (byte)((colorizationValue >> 16) & 0xFF),
                        (byte)((colorizationValue >> 8) & 0xFF),
                        (byte)(colorizationValue & 0xFF));
                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 清除系统强调色缓存 (用于响应系统设置变化)
        /// </summary>
        public static void InvalidateSystemAccentCache()
        {
            _cachedSystemAccent = null;
            _lastAccentCheck = DateTime.MinValue;
        }

        #endregion

        #region Adaptive Color

        /// <summary>
        /// 根据背景色计算适合的前景色 (黑或白)
        /// </summary>
        /// <param name="backgroundHex">背景色十六进制值</param>
        /// <returns>适合的前景色 (#FFFFFF 或 #000000 或带透明度变体)</returns>
        public static string GetAdaptiveColor(string? backgroundHex)
        {
            if (string.IsNullOrEmpty(backgroundHex))
                return "#FFFFFF";

            var bgColor = ParseColor(backgroundHex);
            return GetAdaptiveColorForBackground(bgColor);
        }

        /// <summary>
        /// 根据背景色计算适合的前景色
        /// 使用 WCAG 2.0 相对亮度算法
        /// </summary>
        public static string GetAdaptiveColorForBackground(Color backgroundColor)
        {
            var luminance = GetRelativeLuminance(backgroundColor);

            // WCAG 建议: 亮度 > 0.179 使用深色文字，否则使用浅色文字
            // 这里使用 0.5 作为阈值，可以根据需要调整
            if (luminance > 0.5)
            {
                return "#1A1A1A"; // 深色前景
            }
            else
            {
                return "#FFFFFF"; // 浅色前景
            }
        }

        /// <summary>
        /// 获取适合的前景色 Color 值
        /// </summary>
        public static Color GetAdaptiveColorValue(Color backgroundColor)
        {
            var luminance = GetRelativeLuminance(backgroundColor);
            return luminance > 0.5
                ? Color.FromRgb(0x1A, 0x1A, 0x1A)
                : Color.FromRgb(0xFF, 0xFF, 0xFF);
        }

        /// <summary>
        /// 计算颜色的相对亮度 (WCAG 2.0)
        /// </summary>
        public static double GetRelativeLuminance(Color color)
        {
            // sRGB 到线性 RGB 的转换
            double R = SrgbToLinear(color.R / 255.0);
            double G = SrgbToLinear(color.G / 255.0);
            double B = SrgbToLinear(color.B / 255.0);

            // 相对亮度公式 (ITU-R BT.709)
            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }

        private static double SrgbToLinear(double value)
        {
            return value <= 0.03928
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// 计算两个颜色之间的对比度 (WCAG 2.0)
        /// </summary>
        public static double GetContrastRatio(Color color1, Color color2)
        {
            var l1 = GetRelativeLuminance(color1);
            var l2 = GetRelativeLuminance(color2);

            var lighter = Math.Max(l1, l2);
            var darker = Math.Min(l1, l2);

            return (lighter + 0.05) / (darker + 0.05);
        }

        /// <summary>
        /// 检查对比度是否满足 WCAG AA 标准 (4.5:1)
        /// </summary>
        public static bool MeetsWcagAA(Color foreground, Color background)
        {
            return GetContrastRatio(foreground, background) >= 4.5;
        }

        /// <summary>
        /// 检查对比度是否满足 WCAG AAA 标准 (7:1)
        /// </summary>
        public static bool MeetsWcagAAA(Color foreground, Color background)
        {
            return GetContrastRatio(foreground, background) >= 7.0;
        }

        #endregion

        #region Property Accessors

        private static string GetColorValue(ThemeColors colors, string property)
        {
            var prop = FindProperty(typeof(ThemeColors), property);
            var value = prop?.GetValue(colors)?.ToString() ?? string.Empty;
            return value;
        }

        private static string GetFontValue(ThemeFonts fonts, string property)
        {
            var prop = FindProperty(typeof(ThemeFonts), property);
            var value = prop?.GetValue(fonts)?.ToString() ?? string.Empty;
            return value;
        }

        private static string GetDimensionValue(ThemeDimensions dimensions, string property)
        {
            var prop = FindProperty(typeof(ThemeDimensions), property);
            var value = prop?.GetValue(dimensions)?.ToString() ?? string.Empty;
            return value;
        }

        /// <summary>
        /// 不区分大小写地查找属性
        /// </summary>
        private static PropertyInfo? FindProperty(Type type, string propertyName)
        {
            // 先尝试精确匹配
            var prop = type.GetProperty(propertyName);
            if (prop != null)
                return prop;

            // 不区分大小写查找
            foreach (var p in type.GetProperties())
            {
                if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }

            return null;
        }

        #endregion

        #region Parse Helpers

        /// <summary>
        /// 将十六进制颜色字符串转换为 WPF Color
        /// </summary>
        public static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.Transparent;

            // 处理 transparent 关键字
            if (hex.Equals("transparent", StringComparison.OrdinalIgnoreCase))
                return Colors.Transparent;

            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 6)
                {
                    return Color.FromRgb(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16));
                }
                else if (hex.Length == 8)
                {
                    return Color.FromArgb(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16),
                        Convert.ToByte(hex.Substring(6, 2), 16));
                }
                else if (hex.Length == 3)
                {
                    // 支持简写格式 #RGB
                    return Color.FromRgb(
                        Convert.ToByte(new string(hex[0], 2), 16),
                        Convert.ToByte(new string(hex[1], 2), 16),
                        Convert.ToByte(new string(hex[2], 2), 16));
                }
                else if (hex.Length == 4)
                {
                    // 支持简写格式 #ARGB
                    return Color.FromArgb(
                        Convert.ToByte(new string(hex[0], 2), 16),
                        Convert.ToByte(new string(hex[1], 2), 16),
                        Convert.ToByte(new string(hex[2], 2), 16),
                        Convert.ToByte(new string(hex[3], 2), 16));
                }
            }
            catch { }

            return Colors.Transparent;
        }

        /// <summary>
        /// 将十六进制颜色字符串转换为 SolidColorBrush
        /// </summary>
        public static SolidColorBrush ParseBrush(string hex)
        {
            var color = ParseColor(hex);
            return new SolidColorBrush(color);
        }

        /// <summary>
        /// 将 Color 转换为十六进制字符串
        /// </summary>
        public static string ColorToHex(Color color, bool includeAlpha = false)
        {
            if (includeAlpha || color.A < 255)
            {
                return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// 解析 Thickness 字符串（如 "8,16" 或 "8,16,8,16"）
        /// </summary>
        public static Thickness ParseThickness(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new Thickness(0);

            var parts = value.Split(',');
            try
            {
                return parts.Length switch
                {
                    1 => new Thickness(double.Parse(parts[0])),
                    2 => new Thickness(double.Parse(parts[0]), double.Parse(parts[1]), double.Parse(parts[0]), double.Parse(parts[1])),
                    4 => new Thickness(double.Parse(parts[0]), double.Parse(parts[1]), double.Parse(parts[2]), double.Parse(parts[3])),
                    _ => new Thickness(0)
                };
            }
            catch
            {
                return new Thickness(0);
            }
        }

        /// <summary>
        /// 解析 CornerRadius
        /// </summary>
        public static CornerRadius ParseCornerRadius(string value)
        {
            if (double.TryParse(value, out var radius))
                return new CornerRadius(radius);
            return new CornerRadius(0);
        }

        /// <summary>
        /// 解析 FontFamily
        /// </summary>
        public static FontFamily ParseFontFamily(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new FontFamily("Segoe UI");
            return new FontFamily(value);
        }

        /// <summary>
        /// 解析 FontWeight
        /// </summary>
        public static FontWeight ParseFontWeight(string value)
        {
            if (int.TryParse(value, out var weight))
            {
                return weight switch
                {
                    100 => FontWeights.Thin,
                    200 => FontWeights.ExtraLight,
                    300 => FontWeights.Light,
                    400 => FontWeights.Normal,
                    500 => FontWeights.Medium,
                    600 => FontWeights.SemiBold,
                    700 => FontWeights.Bold,
                    800 => FontWeights.ExtraBold,
                    900 => FontWeights.Black,
                    _ => FontWeights.Normal
                };
            }
            return FontWeights.Normal;
        }

        #endregion
    }
}