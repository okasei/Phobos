using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Phobos.Class.Theme
{
    /// <summary>
    /// 主题配置文件数据结构
    /// </summary>
    public class PCThemeConfig
    {
        [JsonProperty("$schema")]
        public string? Schema { get; set; }

        [JsonProperty("metadata")]
        public ThemeMetadata Metadata { get; set; } = new();

        [JsonProperty("colors")]
        public ThemeColors Colors { get; set; } = new();

        [JsonProperty("fonts")]
        public ThemeFonts Fonts { get; set; } = new();

        [JsonProperty("dimensions")]
        public ThemeDimensions Dimensions { get; set; } = new();

        [JsonProperty("controls")]
        public ThemeControls Controls { get; set; } = new();

        [JsonProperty("animations")]
        public ThemeAnimations Animations { get; set; } = new();
    }

    #region Metadata

    public class ThemeMetadata
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("localizedNames")]
        public Dictionary<string, string> LocalizedNames { get; set; } = new();

        public string GetLocalizedName(string languageCode)
        {
            if (LocalizedNames.TryGetValue(languageCode, out var name))
                return name;
            if (LocalizedNames.TryGetValue("en-US", out var defaultName))
                return defaultName;
            return Name;
        }
    }

    #endregion

    #region Colors

    public class ThemeColors
    {
        // ============ 前景色 (5级) ============
        [JsonProperty("foreground1")]
        public string Foreground1 { get; set; } = "#FFFFFF";  // 最亮/主要文字

        [JsonProperty("foreground2")]
        public string Foreground2 { get; set; } = "#E0E0E0";  // 次要文字

        [JsonProperty("foreground3")]
        public string Foreground3 { get; set; } = "#AAAAAA";  // 辅助文字

        [JsonProperty("foreground4")]
        public string Foreground4 { get; set; } = "#777777";  // 禁用/占位文字

        [JsonProperty("foreground5")]
        public string Foreground5 { get; set; } = "#444444";  // 最暗/提示文字

        // ============ 背景色 (5级) ============
        [JsonProperty("background1")]
        public string Background1 { get; set; } = "#0A0A0A";  // 最深/主背景

        [JsonProperty("background2")]
        public string Background2 { get; set; } = "#1A1A1A";  // 次级背景

        [JsonProperty("background3")]
        public string Background3 { get; set; } = "#2A2A2A";  // 卡片/表面

        [JsonProperty("background4")]
        public string Background4 { get; set; } = "#3A3A3A";  // 悬停背景

        [JsonProperty("background5")]
        public string Background5 { get; set; } = "#4A4A4A";  // 最浅/边框背景

        // ============ 主题色 ============
        [JsonProperty("primary")]
        public string Primary { get; set; } = "#1E90FF";

        [JsonProperty("primaryHover")]
        public string PrimaryHover { get; set; } = "#1C86EE";

        [JsonProperty("primaryPressed")]
        public string PrimaryPressed { get; set; } = "#1874CD";

        [JsonProperty("primaryDisabled")]
        public string PrimaryDisabled { get; set; } = "#1E90FF80";

        // ============ 状态色 ============
        [JsonProperty("success")]
        public string Success { get; set; } = "#28A745";

        [JsonProperty("warning")]
        public string Warning { get; set; } = "#FFC107";

        [JsonProperty("danger")]
        public string Danger { get; set; } = "#DC3545";

        [JsonProperty("info")]
        public string Info { get; set; } = "#17A2B8";

        // ============ 备选色 (6个) ============
        [JsonProperty("accent1")]
        public string Accent1 { get; set; } = "#FF6B6B";  // 红色系

        [JsonProperty("accent2")]
        public string Accent2 { get; set; } = "#4ECDC4";  // 青色系

        [JsonProperty("accent3")]
        public string Accent3 { get; set; } = "#45B7D1";  // 蓝色系

        [JsonProperty("accent4")]
        public string Accent4 { get; set; } = "#96CEB4";  // 绿色系

        [JsonProperty("accent5")]
        public string Accent5 { get; set; } = "#FFEAA7";  // 黄色系

        [JsonProperty("accent6")]
        public string Accent6 { get; set; } = "#DDA0DD";  // 紫色系

        // ============ 边框色 ============
        [JsonProperty("border")]
        public string Border { get; set; } = "#3C3C3C";

        [JsonProperty("borderLight")]
        public string BorderLight { get; set; } = "#4A4A4A";

        [JsonProperty("borderFocus")]
        public string BorderFocus { get; set; } = "#1E90FF";

        // ============ 其他 ============
        [JsonProperty("scrollbar")]
        public string Scrollbar { get; set; } = "#4A4A4A";

        [JsonProperty("scrollbarHover")]
        public string ScrollbarHover { get; set; } = "#5A5A5A";

        [JsonProperty("shadow")]
        public string Shadow { get; set; } = "#00000080";

        [JsonProperty("overlay")]
        public string Overlay { get; set; } = "#00000060";
    }

    #endregion

    #region Fonts

    public class ThemeFonts
    {
        // ============ 字体族 (3套) ============
        [JsonProperty("primary")]
        public string Primary { get; set; } = "Segoe UI, Microsoft YaHei, sans-serif";  // 主字体

        [JsonProperty("secondary")]
        public string Secondary { get; set; } = "Arial, Helvetica, sans-serif";  // 备用字体

        [JsonProperty("mono")]
        public string Mono { get; set; } = "Consolas, Microsoft YaHei Mono, monospace";  // 等宽字体

        // ============ 字号 ============
        [JsonProperty("sizeXs")]
        public double SizeXs { get; set; } = 10;

        [JsonProperty("sizeSm")]
        public double SizeSm { get; set; } = 12;

        [JsonProperty("sizeMd")]
        public double SizeMd { get; set; } = 14;

        [JsonProperty("sizeLg")]
        public double SizeLg { get; set; } = 16;

        [JsonProperty("sizeXl")]
        public double SizeXl { get; set; } = 20;

        [JsonProperty("size2xl")]
        public double Size2xl { get; set; } = 24;

        [JsonProperty("size3xl")]
        public double Size3xl { get; set; } = 32;

        // ============ 字重 ============
        [JsonProperty("weightLight")]
        public int WeightLight { get; set; } = 300;

        [JsonProperty("weightNormal")]
        public int WeightNormal { get; set; } = 400;

        [JsonProperty("weightMedium")]
        public int WeightMedium { get; set; } = 500;

        [JsonProperty("weightSemibold")]
        public int WeightSemibold { get; set; } = 600;

        [JsonProperty("weightBold")]
        public int WeightBold { get; set; } = 700;

        // ============ 行高 ============
        [JsonProperty("lineHeightTight")]
        public double LineHeightTight { get; set; } = 1.25;

        [JsonProperty("lineHeightNormal")]
        public double LineHeightNormal { get; set; } = 1.5;

        [JsonProperty("lineHeightRelaxed")]
        public double LineHeightRelaxed { get; set; } = 1.75;
    }

    #endregion

    #region Dimensions

    public class ThemeDimensions
    {
        [JsonProperty("borderRadius")]
        public double BorderRadius { get; set; } = 4;

        [JsonProperty("borderRadiusLarge")]
        public double BorderRadiusLarge { get; set; } = 8;

        [JsonProperty("borderWidth")]
        public double BorderWidth { get; set; } = 1;

        [JsonProperty("spacing")]
        public double Spacing { get; set; } = 8;

        [JsonProperty("spacingSmall")]
        public double SpacingSmall { get; set; } = 4;

        [JsonProperty("spacingLarge")]
        public double SpacingLarge { get; set; } = 16;

        [JsonProperty("buttonHeight")]
        public double ButtonHeight { get; set; } = 32;

        [JsonProperty("buttonHeightSmall")]
        public double ButtonHeightSmall { get; set; } = 24;

        [JsonProperty("buttonHeightLarge")]
        public double ButtonHeightLarge { get; set; } = 40;

        [JsonProperty("buttonPadding")]
        public string ButtonPadding { get; set; } = "8,16";

        [JsonProperty("inputHeight")]
        public double InputHeight { get; set; } = 32;

        [JsonProperty("inputPadding")]
        public string InputPadding { get; set; } = "6,10";

        [JsonProperty("titleBarHeight")]
        public double TitleBarHeight { get; set; } = 32;

        [JsonProperty("windowMinWidth")]
        public double WindowMinWidth { get; set; } = 400;

        [JsonProperty("windowMinHeight")]
        public double WindowMinHeight { get; set; } = 300;
    }

    #endregion

    #region Controls

    public class ThemeControls
    {
        [JsonProperty("button")]
        public ButtonStyle Button { get; set; } = new();

        [JsonProperty("buttonSecondary")]
        public ButtonSecondaryStyle ButtonSecondary { get; set; } = new();

        [JsonProperty("textBox")]
        public TextBoxStyle TextBox { get; set; } = new();

        [JsonProperty("label")]
        public LabelStyle Label { get; set; } = new();

        [JsonProperty("listBox")]
        public ListBoxStyle ListBox { get; set; } = new();

        [JsonProperty("scrollBar")]
        public ScrollBarStyle ScrollBar { get; set; } = new();

        [JsonProperty("window")]
        public WindowStyle Window { get; set; } = new();

        [JsonProperty("titleBarButton")]
        public TitleBarButtonStyle TitleBarButton { get; set; } = new();
    }

    public class ButtonStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.primary}";

        [JsonProperty("backgroundHover")]
        public string BackgroundHover { get; set; } = "${colors.primaryHover}";

        [JsonProperty("backgroundPressed")]
        public string BackgroundPressed { get; set; } = "${colors.primaryPressed}";

        [JsonProperty("backgroundDisabled")]
        public string BackgroundDisabled { get; set; } = "${colors.primaryDisabled}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.background1}";

        [JsonProperty("foregroundDisabled")]
        public string ForegroundDisabled { get; set; } = "${colors.foreground4}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "transparent";

        [JsonProperty("borderRadius")]
        public string BorderRadius { get; set; } = "${dimensions.borderRadius}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeMd}";

        [JsonProperty("fontWeight")]
        public string FontWeight { get; set; } = "${fonts.weightMedium}";
    }

    public class ButtonSecondaryStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "transparent";

        [JsonProperty("backgroundHover")]
        public string BackgroundHover { get; set; } = "${colors.background4}";

        [JsonProperty("backgroundPressed")]
        public string BackgroundPressed { get; set; } = "${colors.background3}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.foreground1}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";
    }

    public class TextBoxStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.background2}";

        [JsonProperty("backgroundFocused")]
        public string BackgroundFocused { get; set; } = "${colors.background1}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.foreground1}";

        [JsonProperty("foregroundPlaceholder")]
        public string ForegroundPlaceholder { get; set; } = "${colors.foreground4}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";

        [JsonProperty("borderColorFocused")]
        public string BorderColorFocused { get; set; } = "${colors.borderFocus}";

        [JsonProperty("borderRadius")]
        public string BorderRadius { get; set; } = "${dimensions.borderRadius}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeMd}";
    }

    public class LabelStyle
    {
        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.foreground1}";

        [JsonProperty("foregroundSecondary")]
        public string ForegroundSecondary { get; set; } = "${colors.foreground3}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeMd}";
    }

    public class ListBoxStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.background2}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.foreground1}";

        [JsonProperty("itemBackground")]
        public string ItemBackground { get; set; } = "transparent";

        [JsonProperty("itemBackgroundHover")]
        public string ItemBackgroundHover { get; set; } = "${colors.background4}";

        [JsonProperty("itemBackgroundSelected")]
        public string ItemBackgroundSelected { get; set; } = "${colors.primary}";

        [JsonProperty("itemForegroundSelected")]
        public string ItemForegroundSelected { get; set; } = "${colors.background1}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";
    }

    public class ScrollBarStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "transparent";

        [JsonProperty("thumbBackground")]
        public string ThumbBackground { get; set; } = "${colors.scrollbar}";

        [JsonProperty("thumbBackgroundHover")]
        public string ThumbBackgroundHover { get; set; } = "${colors.scrollbarHover}";

        [JsonProperty("width")]
        public double Width { get; set; } = 10;

        [JsonProperty("thumbRadius")]
        public double ThumbRadius { get; set; } = 5;
    }

    public class WindowStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.background1}";

        [JsonProperty("titleBarBackground")]
        public string TitleBarBackground { get; set; } = "${colors.background2}";

        [JsonProperty("titleBarForeground")]
        public string TitleBarForeground { get; set; } = "${colors.foreground1}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";

        [JsonProperty("shadowColor")]
        public string ShadowColor { get; set; } = "${colors.shadow}";

        [JsonProperty("shadowBlurRadius")]
        public double ShadowBlurRadius { get; set; } = 20;
    }

    public class TitleBarButtonStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "transparent";

        [JsonProperty("backgroundHover")]
        public string BackgroundHover { get; set; } = "${colors.background4}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.foreground3}";

        [JsonProperty("foregroundHover")]
        public string ForegroundHover { get; set; } = "${colors.foreground1}";

        [JsonProperty("closeBackgroundHover")]
        public string CloseBackgroundHover { get; set; } = "#E81123";

        [JsonProperty("closeForegroundHover")]
        public string CloseForegroundHover { get; set; } = "#FFFFFF";
    }

    #endregion

    #region Animations

    public class ThemeAnimations
    {
        [JsonProperty("defaultDuration")]
        public int DefaultDuration { get; set; } = 200;

        [JsonProperty("fastDuration")]
        public int FastDuration { get; set; } = 100;

        [JsonProperty("slowDuration")]
        public int SlowDuration { get; set; } = 400;

        [JsonProperty("easing")]
        public string Easing { get; set; } = "CubicEaseOut";

        [JsonProperty("enableAnimations")]
        public bool EnableAnimations { get; set; } = true;

        [JsonProperty("windowOpenAnimation")]
        public AnimationConfig WindowOpenAnimation { get; set; } = new();

        [JsonProperty("windowCloseAnimation")]
        public AnimationConfig WindowCloseAnimation { get; set; } = new();

        [JsonProperty("buttonHoverAnimation")]
        public AnimationConfig ButtonHoverAnimation { get; set; } = new();
    }

    public class AnimationConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "FadeIn";

        [JsonProperty("duration")]
        public int Duration { get; set; } = 200;

        [JsonProperty("easing")]
        public string Easing { get; set; } = "CubicEaseOut";
    }

    #endregion
}