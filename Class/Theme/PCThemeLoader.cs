using Newtonsoft.Json;
using Phobos.Class.Plugin.BuiltIn;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
/**
 * 
 * ## 🔄 主题加载流程
```
1. PMTheme.Initialize(themesDirectory)
   ↓
2. RegisterDefaultThemes()  → 加载内置主题 (Assets/Themes/)
   ↓
3. LoadExternalThemes()     → 加载外部主题 (%AppData%/Phobos/Themes/)
   ↓
4. LoadTheme("dark")        → 应用主题
   ↓
5. Apply()                  → 注入到 Application.Resources

// 初始化
PMTheme.Instance.Initialize(themesPath);

// 加载主题
await PMTheme.Instance.LoadTheme("com.phobos.theme.dark");

// 从文件加载
await PMTheme.Instance.LoadThemeFromFile("path/to/custom.phobos-theme.json");

// 应用到窗口
PMTheme.Instance.ApplyThemeToWindow(myWindow);
 */

namespace Phobos.Class.Theme
{
    /// <summary>
    /// 主题配置文件加载器
    /// </summary>
    public class PCThemeLoader
    {
        private static readonly Regex VariablePattern = new(@"\$\{(\w+)\.(\w+)\}", RegexOptions.Compiled);

        /// <summary>
        /// 从文件加载主题配置
        /// </summary>
        public static PCThemeConfig? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                PCLoggerPlugin.Error("Phobos.Theme.Loader", $"Theme file not found: {filePath}");
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
                PCLoggerPlugin.Error("Phobos.Theme.Loader", $"Failed to load theme: {ex.Message}");
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
                PCLoggerPlugin.Error("Phobos.Theme.Loader", $"Failed to parse theme JSON: {ex.Message}");
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
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Loader", $"Failed to save theme: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析变量引用（如 ${colors.primary}）
        /// </summary>
        public static string ResolveVariable(string value, PCThemeConfig config)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains("${"))
                return value;

            return VariablePattern.Replace(value, match =>
            {
                var category = match.Groups[1].Value.ToLowerInvariant();
                var property = match.Groups[2].Value;

                return category switch
                {
                    "colors" => GetColorValue(config.Colors, property),
                    "fonts" => GetFontValue(config.Fonts, property),
                    "dimensions" => GetDimensionValue(config.Dimensions, property),
                    _ => match.Value
                };
            });
        }

        private static string GetColorValue(ThemeColors colors, string property)
        {
            var prop = typeof(ThemeColors).GetProperty(property);
            return prop?.GetValue(colors)?.ToString() ?? string.Empty;
        }

        private static string GetFontValue(ThemeFonts fonts, string property)
        {
            var prop = typeof(ThemeFonts).GetProperty(property);
            return prop?.GetValue(fonts)?.ToString() ?? string.Empty;
        }

        private static string GetDimensionValue(ThemeDimensions dimensions, string property)
        {
            var prop = typeof(ThemeDimensions).GetProperty(property);
            return prop?.GetValue(dimensions)?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 将十六进制颜色字符串转换为 WPF Color
        /// </summary>
        public static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
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
                    2 => new Thickness(double.Parse(parts[1]), double.Parse(parts[0]), double.Parse(parts[1]), double.Parse(parts[0])),
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
    }
}