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
        // Primary colors
        [JsonProperty("primary")]
        public string Primary { get; set; } = "#1E90FF";

        [JsonProperty("primaryHover")]
        public string PrimaryHover { get; set; } = "#1C86EE";

        [JsonProperty("primaryPressed")]
        public string PrimaryPressed { get; set; } = "#1874CD";

        [JsonProperty("secondary")]
        public string Secondary { get; set; } = "#6C757D";

        [JsonProperty("success")]
        public string Success { get; set; } = "#28A745";

        [JsonProperty("warning")]
        public string Warning { get; set; } = "#FFC107";

        [JsonProperty("danger")]
        public string Danger { get; set; } = "#DC3545";

        [JsonProperty("info")]
        public string Info { get; set; } = "#17A2B8";

        // Background colors
        [JsonProperty("background")]
        public string Background { get; set; } = "#1E1E1E";

        [JsonProperty("backgroundAlt")]
        public string BackgroundAlt { get; set; } = "#252526";

        [JsonProperty("backgroundHover")]
        public string BackgroundHover { get; set; } = "#2D2D2D";

        [JsonProperty("surface")]
        public string Surface { get; set; } = "#2D2D2D";

        [JsonProperty("surfaceHover")]
        public string SurfaceHover { get; set; } = "#3C3C3C";

        // Text colors
        [JsonProperty("text")]
        public string Text { get; set; } = "#FFFFFF";

        [JsonProperty("textSecondary")]
        public string TextSecondary { get; set; } = "#AAAAAA";

        [JsonProperty("textDisabled")]
        public string TextDisabled { get; set; } = "#666666";

        [JsonProperty("textInverse")]
        public string TextInverse { get; set; } = "#000000";

        // Border colors
        [JsonProperty("border")]
        public string Border { get; set; } = "#3C3C3C";

        [JsonProperty("borderLight")]
        public string BorderLight { get; set; } = "#4A4A4A";

        [JsonProperty("borderFocus")]
        public string BorderFocus { get; set; } = "#1E90FF";

        // Other
        [JsonProperty("scrollbar")]
        public string Scrollbar { get; set; } = "#4A4A4A";

        [JsonProperty("scrollbarHover")]
        public string ScrollbarHover { get; set; } = "#5A5A5A";

        [JsonProperty("shadow")]
        public string Shadow { get; set; } = "#00000080";
    }

    #endregion

    #region Fonts

    public class ThemeFonts
    {
        [JsonProperty("family")]
        public string Family { get; set; } = "Segoe UI, Microsoft YaHei, sans-serif";

        [JsonProperty("familyMono")]
        public string FamilyMono { get; set; } = "Consolas, Microsoft YaHei Mono, monospace";

        [JsonProperty("sizeSmall")]
        public double SizeSmall { get; set; } = 11;

        [JsonProperty("sizeNormal")]
        public double SizeNormal { get; set; } = 13;

        [JsonProperty("sizeMedium")]
        public double SizeMedium { get; set; } = 15;

        [JsonProperty("sizeLarge")]
        public double SizeLarge { get; set; } = 18;

        [JsonProperty("sizeTitle")]
        public double SizeTitle { get; set; } = 24;

        [JsonProperty("weightNormal")]
        public int WeightNormal { get; set; } = 400;

        [JsonProperty("weightMedium")]
        public int WeightMedium { get; set; } = 500;

        [JsonProperty("weightBold")]
        public int WeightBold { get; set; } = 700;
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
        public string BackgroundDisabled { get; set; } = "${colors.secondary}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.text}";

        [JsonProperty("foregroundDisabled")]
        public string ForegroundDisabled { get; set; } = "${colors.textDisabled}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "transparent";

        [JsonProperty("borderRadius")]
        public string BorderRadius { get; set; } = "${dimensions.borderRadius}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeNormal}";

        [JsonProperty("fontWeight")]
        public string FontWeight { get; set; } = "${fonts.weightMedium}";
    }

    public class ButtonSecondaryStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "transparent";

        [JsonProperty("backgroundHover")]
        public string BackgroundHover { get; set; } = "${colors.backgroundHover}";

        [JsonProperty("backgroundPressed")]
        public string BackgroundPressed { get; set; } = "${colors.surface}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.text}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";
    }

    public class TextBoxStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.backgroundAlt}";

        [JsonProperty("backgroundFocused")]
        public string BackgroundFocused { get; set; } = "${colors.background}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.text}";

        [JsonProperty("foregroundPlaceholder")]
        public string ForegroundPlaceholder { get; set; } = "${colors.textDisabled}";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "${colors.border}";

        [JsonProperty("borderColorFocused")]
        public string BorderColorFocused { get; set; } = "${colors.borderFocus}";

        [JsonProperty("borderRadius")]
        public string BorderRadius { get; set; } = "${dimensions.borderRadius}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeNormal}";
    }

    public class LabelStyle
    {
        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.text}";

        [JsonProperty("foregroundSecondary")]
        public string ForegroundSecondary { get; set; } = "${colors.textSecondary}";

        [JsonProperty("fontSize")]
        public string FontSize { get; set; } = "${fonts.sizeNormal}";
    }

    public class ListBoxStyle
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "${colors.backgroundAlt}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.text}";

        [JsonProperty("itemBackground")]
        public string ItemBackground { get; set; } = "transparent";

        [JsonProperty("itemBackgroundHover")]
        public string ItemBackgroundHover { get; set; } = "${colors.backgroundHover}";

        [JsonProperty("itemBackgroundSelected")]
        public string ItemBackgroundSelected { get; set; } = "${colors.primary}";

        [JsonProperty("itemForegroundSelected")]
        public string ItemForegroundSelected { get; set; } = "${colors.text}";

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
        public string Background { get; set; } = "${colors.background}";

        [JsonProperty("titleBarBackground")]
        public string TitleBarBackground { get; set; } = "${colors.backgroundAlt}";

        [JsonProperty("titleBarForeground")]
        public string TitleBarForeground { get; set; } = "${colors.text}";

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
        public string BackgroundHover { get; set; } = "${colors.backgroundHover}";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "${colors.textSecondary}";

        [JsonProperty("foregroundHover")]
        public string ForegroundHover { get; set; } = "${colors.text}";

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