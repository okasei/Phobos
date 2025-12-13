using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Interface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Phobos.Class.Theme
{
    /// <summary>
    /// 从 PCThemeConfig 创建 IPhobosTheme 实现
    /// </summary>
    public class PCConfigBasedTheme : IPhobosTheme
    {
        private readonly PCThemeConfig _config;
        private readonly Dictionary<Type, Style> _controlStyles = new();
        private readonly Dictionary<string, SolidColorBrush> _brushCache = new();
        private ResourceDictionary? _resourceDictionary;

        public string ThemeId => _config.Metadata.Id;
        public string Name => _config.Metadata.Name;
        public string Version => _config.Metadata.Version;
        public string Author => _config.Metadata.Author;

        // 兼容属性 (使用新的5级前景/背景色)
        public string ThemeName => _config.Metadata.Name;
        public Color PrimaryColor => GetColor(_config.Colors.Primary);
        public Color BackgroundColor => GetColor(_config.Colors.Background1);
        public Color SurfaceColor => GetColor(_config.Colors.Background3);
        public Color TextColor => GetColor(_config.Colors.Foreground1);
        public Color TextSecondaryColor => GetColor(_config.Colors.Foreground3);
        public Color BorderColor => GetColor(_config.Colors.Border);
        public Color AccentColor => GetColor(_config.Colors.Primary);
        public Color ErrorColor => GetColor(_config.Colors.Danger);
        public Color WarningColor => GetColor(_config.Colors.Warning);
        public Color SuccessColor => GetColor(_config.Colors.Success);

        public PCConfigBasedTheme(PCThemeConfig config)
        {
            _config = config;
            BuildControlStyles();
        }

        #region IPhobosTheme Implementation

        public string GetLocalizedName(string languageCode)
        {
            return _config.Metadata.GetLocalizedName(languageCode);
        }

        public ResourceDictionary GetGlobalStyles()
        {
            if (_resourceDictionary != null)
                return _resourceDictionary;

            _resourceDictionary = new ResourceDictionary();

            // ============ 前景色 (5级) ============
            _resourceDictionary["Foreground1Color"] = GetColor(_config.Colors.Foreground1);
            _resourceDictionary["Foreground2Color"] = GetColor(_config.Colors.Foreground2);
            _resourceDictionary["Foreground3Color"] = GetColor(_config.Colors.Foreground3);
            _resourceDictionary["Foreground4Color"] = GetColor(_config.Colors.Foreground4);
            _resourceDictionary["Foreground5Color"] = GetColor(_config.Colors.Foreground5);

            _resourceDictionary["Foreground1Brush"] = GetBrush(_config.Colors.Foreground1);
            _resourceDictionary["Foreground2Brush"] = GetBrush(_config.Colors.Foreground2);
            _resourceDictionary["Foreground3Brush"] = GetBrush(_config.Colors.Foreground3);
            _resourceDictionary["Foreground4Brush"] = GetBrush(_config.Colors.Foreground4);
            _resourceDictionary["Foreground5Brush"] = GetBrush(_config.Colors.Foreground5);

            // ============ 背景色 (5级) ============
            _resourceDictionary["Background1Color"] = GetColor(_config.Colors.Background1);
            _resourceDictionary["Background2Color"] = GetColor(_config.Colors.Background2);
            _resourceDictionary["Background3Color"] = GetColor(_config.Colors.Background3);
            _resourceDictionary["Background4Color"] = GetColor(_config.Colors.Background4);
            _resourceDictionary["Background5Color"] = GetColor(_config.Colors.Background5);

            _resourceDictionary["Background1Brush"] = GetBrush(_config.Colors.Background1);
            _resourceDictionary["Background2Brush"] = GetBrush(_config.Colors.Background2);
            _resourceDictionary["Background3Brush"] = GetBrush(_config.Colors.Background3);
            _resourceDictionary["Background4Brush"] = GetBrush(_config.Colors.Background4);
            _resourceDictionary["Background5Brush"] = GetBrush(_config.Colors.Background5);

            // ============ 主题色 ============
            _resourceDictionary["PrimaryColor"] = GetColor(_config.Colors.Primary);
            _resourceDictionary["PrimaryHoverColor"] = GetColor(_config.Colors.PrimaryHover);
            _resourceDictionary["PrimaryPressedColor"] = GetColor(_config.Colors.PrimaryPressed);
            _resourceDictionary["PrimaryDisabledColor"] = GetColor(_config.Colors.PrimaryDisabled);

            _resourceDictionary["PrimaryBrush"] = GetBrush(_config.Colors.Primary);
            _resourceDictionary["PrimaryHoverBrush"] = GetBrush(_config.Colors.PrimaryHover);
            _resourceDictionary["PrimaryPressedBrush"] = GetBrush(_config.Colors.PrimaryPressed);
            _resourceDictionary["PrimaryDisabledBrush"] = GetBrush(_config.Colors.PrimaryDisabled);

            // ============ 状态色 ============
            _resourceDictionary["SuccessColor"] = GetColor(_config.Colors.Success);
            _resourceDictionary["WarningColor"] = GetColor(_config.Colors.Warning);
            _resourceDictionary["DangerColor"] = GetColor(_config.Colors.Danger);
            _resourceDictionary["InfoColor"] = GetColor(_config.Colors.Info);

            _resourceDictionary["SuccessBrush"] = GetBrush(_config.Colors.Success);
            _resourceDictionary["WarningBrush"] = GetBrush(_config.Colors.Warning);
            _resourceDictionary["DangerBrush"] = GetBrush(_config.Colors.Danger);
            _resourceDictionary["InfoBrush"] = GetBrush(_config.Colors.Info);

            // ============ 备选色 (6个) ============
            _resourceDictionary["Accent1Color"] = GetColor(_config.Colors.Accent1);
            _resourceDictionary["Accent2Color"] = GetColor(_config.Colors.Accent2);
            _resourceDictionary["Accent3Color"] = GetColor(_config.Colors.Accent3);
            _resourceDictionary["Accent4Color"] = GetColor(_config.Colors.Accent4);
            _resourceDictionary["Accent5Color"] = GetColor(_config.Colors.Accent5);
            _resourceDictionary["Accent6Color"] = GetColor(_config.Colors.Accent6);

            _resourceDictionary["Accent1Brush"] = GetBrush(_config.Colors.Accent1);
            _resourceDictionary["Accent2Brush"] = GetBrush(_config.Colors.Accent2);
            _resourceDictionary["Accent3Brush"] = GetBrush(_config.Colors.Accent3);
            _resourceDictionary["Accent4Brush"] = GetBrush(_config.Colors.Accent4);
            _resourceDictionary["Accent5Brush"] = GetBrush(_config.Colors.Accent5);
            _resourceDictionary["Accent6Brush"] = GetBrush(_config.Colors.Accent6);

            // ============ 边框色 ============
            _resourceDictionary["BorderColor"] = GetColor(_config.Colors.Border);
            _resourceDictionary["BorderLightColor"] = GetColor(_config.Colors.BorderLight);
            _resourceDictionary["BorderFocusColor"] = GetColor(_config.Colors.BorderFocus);

            _resourceDictionary["BorderBrush"] = GetBrush(_config.Colors.Border);
            _resourceDictionary["BorderLightBrush"] = GetBrush(_config.Colors.BorderLight);
            _resourceDictionary["BorderFocusBrush"] = GetBrush(_config.Colors.BorderFocus);

            // ============ 其他颜色 ============
            _resourceDictionary["ScrollbarBrush"] = GetBrush(_config.Colors.Scrollbar);
            _resourceDictionary["ScrollbarHoverBrush"] = GetBrush(_config.Colors.ScrollbarHover);
            _resourceDictionary["ShadowBrush"] = GetBrush(_config.Colors.Shadow);
            _resourceDictionary["OverlayBrush"] = GetBrush(_config.Colors.Overlay);

            // ============ 字体族 (3套) ============
            _resourceDictionary["FontPrimary"] = PCThemeLoader.ParseFontFamily(_config.Fonts.Primary);
            _resourceDictionary["FontSecondary"] = PCThemeLoader.ParseFontFamily(_config.Fonts.Secondary);
            _resourceDictionary["FontMono"] = PCThemeLoader.ParseFontFamily(_config.Fonts.Mono);

            // ============ 字号 ============
            _resourceDictionary["FontSizeXs"] = _config.Fonts.SizeXs;
            _resourceDictionary["FontSizeSm"] = _config.Fonts.SizeSm;
            _resourceDictionary["FontSizeMd"] = _config.Fonts.SizeMd;
            _resourceDictionary["FontSizeLg"] = _config.Fonts.SizeLg;
            _resourceDictionary["FontSizeXl"] = _config.Fonts.SizeXl;
            _resourceDictionary["FontSize2xl"] = _config.Fonts.Size2xl;
            _resourceDictionary["FontSize3xl"] = _config.Fonts.Size3xl;

            // ============ 字重 ============
            _resourceDictionary["FontWeightLight"] = FontWeightFromInt(_config.Fonts.WeightLight);
            _resourceDictionary["FontWeightNormal"] = FontWeightFromInt(_config.Fonts.WeightNormal);
            _resourceDictionary["FontWeightMedium"] = FontWeightFromInt(_config.Fonts.WeightMedium);
            _resourceDictionary["FontWeightSemibold"] = FontWeightFromInt(_config.Fonts.WeightSemibold);
            _resourceDictionary["FontWeightBold"] = FontWeightFromInt(_config.Fonts.WeightBold);

            // ============ 行高 ============
            _resourceDictionary["LineHeightTight"] = _config.Fonts.LineHeightTight;
            _resourceDictionary["LineHeightNormal"] = _config.Fonts.LineHeightNormal;
            _resourceDictionary["LineHeightRelaxed"] = _config.Fonts.LineHeightRelaxed;

            // ============ 尺寸资源 ============
            _resourceDictionary["BorderRadius"] = new CornerRadius(_config.Dimensions.BorderRadius);
            _resourceDictionary["BorderRadiusLarge"] = new CornerRadius(_config.Dimensions.BorderRadiusLarge);
            _resourceDictionary["BorderWidth"] = _config.Dimensions.BorderWidth;
            _resourceDictionary["BorderThickness"] = new Thickness(_config.Dimensions.BorderWidth);

            _resourceDictionary["Spacing"] = _config.Dimensions.Spacing;
            _resourceDictionary["SpacingSmall"] = _config.Dimensions.SpacingSmall;
            _resourceDictionary["SpacingLarge"] = _config.Dimensions.SpacingLarge;

            _resourceDictionary["ButtonHeight"] = _config.Dimensions.ButtonHeight;
            _resourceDictionary["ButtonHeightSmall"] = _config.Dimensions.ButtonHeightSmall;
            _resourceDictionary["ButtonHeightLarge"] = _config.Dimensions.ButtonHeightLarge;
            _resourceDictionary["ButtonPadding"] = PCThemeLoader.ParseThickness(_config.Dimensions.ButtonPadding);

            _resourceDictionary["InputHeight"] = _config.Dimensions.InputHeight;
            _resourceDictionary["InputPadding"] = PCThemeLoader.ParseThickness(_config.Dimensions.InputPadding);

            _resourceDictionary["TitleBarHeight"] = _config.Dimensions.TitleBarHeight;

            // ============ 控件样式 ============
            foreach (var kvp in _controlStyles)
            {
                var key = kvp.Key.Name + "Style"; // e.g., "ButtonStyle"
                _resourceDictionary[key] = kvp.Value;
                // add typed key as well, so resources[typeof(Button)] is available
                _resourceDictionary[kvp.Key] = kvp.Value;
            }

            // Also add some common named resources used across XAML that may expect named keys
            if (_controlStyles.TryGetValue(typeof(Button), out var buttonStyle))
            {
                _resourceDictionary["PrimaryButtonStyle"] = buttonStyle;
                _resourceDictionary["PhobosButton"] = buttonStyle;
                _resourceDictionary["DialogButtonStyle"] = buttonStyle; // safe default
            }

            // Secondary button style (named only)
            var secondaryBtnStyle = CreateSecondaryButtonStyle();
            if (secondaryBtnStyle != null)
            {
                _resourceDictionary["SecondaryButtonStyle"] = secondaryBtnStyle;
                _resourceDictionary["PhobosButtonSecondary"] = secondaryBtnStyle;
            }

            // Toggle / CheckBox style fallback
            var toggleStyle = CreateToggleSwitchStyle();
            if (toggleStyle != null)
            {
                _resourceDictionary["PhobosToggleSwitch"] = toggleStyle;
                _resourceDictionary[typeof(CheckBox)] = toggleStyle;
            }

            // Additional named button variants (danger/success/ghost/icon/sizes)
            if (_resourceDictionary.Contains("PhobosButton"))
            {
                var baseBtn = _resourceDictionary["PhobosButton"] as Style;
                if (baseBtn != null)
                {
                    // Danger
                    var danger = new Style(typeof(Button), baseBtn);
                    danger.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(_config.Colors.Danger)));
                    danger.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(_config.Colors.Background1)));
                    _resourceDictionary["PhobosButtonDanger"] = danger;

                    // Success
                    var success = new Style(typeof(Button), baseBtn);
                    success.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(_config.Colors.Success)));
                    success.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(_config.Colors.Background1)));
                    _resourceDictionary["PhobosButtonSuccess"] = success;

                    // Ghost (transparent)
                    var ghost = new Style(typeof(Button), baseBtn);
                    ghost.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                    ghost.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(_config.Colors.Border)));
                    _resourceDictionary["PhobosButtonGhost"] = ghost;

                    // Icon (keep base but smaller padding)
                    var icon = new Style(typeof(Button), baseBtn);
                    icon.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6)));
                    _resourceDictionary["PhobosButtonIcon"] = icon;

                    // Small / Large
                    var small = new Style(typeof(Button), baseBtn);
                    small.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.ButtonHeightSmall));
                    _resourceDictionary["PhobosButtonSmall"] = small;

                    var large = new Style(typeof(Button), baseBtn);
                    large.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.ButtonHeightLarge));
                    _resourceDictionary["PhobosButtonLarge"] = large;
                }
            }

            return _resourceDictionary;
        }

        private FontWeight FontWeightFromInt(int weight)
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

        public Style? GetControlStyle(Type controlType)
        {
            return _controlStyles.GetValueOrDefault(controlType);
        }

        public ControlAnimationConfig GetControlAnimationConfig(Type controlType)
        {
            if (!_config.Animations.EnableAnimations)
                return new ControlAnimationConfig();

            // 窗口动画
            if (typeof(Window).IsAssignableFrom(controlType))
            {
                return new ControlAnimationConfig
                {
                    OnLoad = ParseAnimationConfig(_config.Animations.WindowOpenAnimation),
                    OnRestore = ParseAnimationConfig(_config.Animations.WindowCloseAnimation)
                };
            }

            return GetDefaultAnimationConfig();
        }

        public ControlAnimationConfig GetDefaultAnimationConfig()
        {
            return new ControlAnimationConfig
            {
                OnLoad = new Shared.Interface.AnimationConfig
                {
                    Types = AnimationType.FadeIn,
                    Duration = TimeSpan.FromMilliseconds(_config.Animations.DefaultDuration),
                    EasingFunction = GetEasingFunction(_config.Animations.Easing)
                }
            };
        }

        public void Apply()
        {
            var resources = GetGlobalStyles();

            if (Application.Current != null)
            {
                // Determine whether the new theme provides styles (full theme) or just colors
                bool newHasStyles = resources.Values.OfType<Style>().Any() || resources.Values.OfType<ControlTemplate>().Any();

                // Remove only appropriate old theme resource dictionaries.
                // If new theme is a full theme (contains styles), then remove all previously marked theme dictionaries.
                // If new theme is colors-only, remove only old theme dictionaries that are also colors-only (keep style dictionaries as base fallback).
                var toRemove = new List<ResourceDictionary>();
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    if (!dict.Contains("PhobosThemeMarker"))
                        continue;

                    bool dictHasStyles = dict.Values.OfType<Style>().Any() || dict.Values.OfType<ControlTemplate>().Any();
                    if (newHasStyles)
                    {
                        // If the incoming theme contains styles, we will remove all previous theme dictionaries
                        toRemove.Add(dict);
                    }
                    else
                    {
                        // New theme has only colors: preserve any dict which contains styles (act as base), remove only those that are colors-only
                        if (!dictHasStyles)
                        {
                            toRemove.Add(dict);
                        }
                    }
                }
                foreach (var dict in toRemove)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                }

                // 添加标记
                resources["PhobosThemeMarker"] = true;

                // 添加新主题资源
                Application.Current.Resources.MergedDictionaries.Add(resources);
            }
        }

        void DiagnoseFontSizeSetters(ResourceDictionary resources)
        {
            var s = "";
            foreach (var a in resources.Keys)
            {
                s += $"{a}: {resources[a]}{Environment.NewLine}";
            }
            foreach (var key in resources.Keys)
            {
                var value = resources[key];
                // 只检查 Style 类型的资源（还有控件模板/其它样式可能也包含 Setter）
                if (value is Style style)
                {
                    foreach (Setter setter in style.Setters.OfType<Setter>())
                    {
                        // 检查是否是 TextElement.FontSize 或 FrameworkElement.FontSize（有时 TextBlock 用的是 FrameworkElement.FontSize）
                        if (Equals(setter.Property, new TextBlock().FontSize) || Equals(setter.Property, Control.FontSizeProperty))
                        {
                            // 打印信息帮助定位
                            s += $"Resource key: {key}  Style.TargetType: {style.TargetType?.FullName ?? "null"}{Environment.NewLine}";
                            s += $"  Setter.Property: {setter.Property}{Environment.NewLine}";
                            s += $"  Setter.Value type: {setter.Value?.GetType().FullName ?? "null"}  value: {setter.Value ?? "null"}{Environment.NewLine}";

                            // 如果 value 是 DynamicResource/StaticResource 标记扩展，需要额外解析，尝试处理常见情况：
                            if (setter.Value is StaticResourceExtension sExt)
                            {
                                s += $"    StaticResource key: {sExt.ResourceKey}{Environment.NewLine}";
                            }
                            else if (setter.Value is DynamicResourceExtension dExt)
                            {
                                s += $"    DynamicResource key: {dExt.ResourceKey}{Environment.NewLine}";
                            }
                        }
                    }
                }
            }

            // 额外：检查是否存在名为 "FontSize" 的资源并输出其值
            if (resources.Contains("FontSize"))
            {
                s += $"Resource 'FontSize' exists with value: {resources["FontSize"]} ({resources["FontSize"]?.GetType()}){Environment.NewLine}";
            }
            // 以及一些常见命名
            string[] commonKeys = { "FontSizeSm", "FontSizeMd", "FontSizeLg", "FontSizeXl", "FontSize2xl", "FontSize3xl" };
            foreach (var k in commonKeys)
            {
                if (resources.Contains(k))
                {
                    s += $"Resource '{k}' = {resources[k]} ({resources[k]?.GetType()}){Environment.NewLine}";
                }
            }
            PCLoggerPlugin.Debug("Phobos.Theme.JsonDeserializer", s);
        }

        public void Unload()
        {
            if (Application.Current != null && _resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_resourceDictionary);
            }
        }

        #endregion

        #region Color Helpers

        private Color GetColor(string hex)
        {
            return PCThemeLoader.ParseColor(ResolveValue(hex));
        }

        private SolidColorBrush GetBrush(string hex)
        {
            var resolved = ResolveValue(hex);
            if (!_brushCache.TryGetValue(resolved, out var brush))
            {
                brush = PCThemeLoader.ParseBrush(resolved);
                brush.Freeze();
                _brushCache[resolved] = brush;
            }
            return brush;
        }

        private string ResolveValue(string value)
        {
            return PCThemeLoader.ResolveVariable(value, _config);
        }

        private double ResolveDouble(string value, double defaultValue = 14)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            var resolved = ResolveValue(value);

            if (double.TryParse(resolved, out var result))
                return result;

            // 解析失败，输出调试信息
            PCLoggerPlugin.Warning("Phobos.Theme.ValueParser", $"Failed to resolve double: '{value}' -> '{resolved}', using default: {defaultValue}");
            return defaultValue;
        }

        private int ResolveInt(string value, int defaultValue = 400)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            var resolved = ResolveValue(value);

            if (int.TryParse(resolved, out var result))
                return result;

            PCLoggerPlugin.Warning("Phobos.Theme.ValueParser", $"[Theme] Failed to resolve int: '{value}' -> '{resolved}', using default: {defaultValue}");
            return defaultValue;
        }

        #endregion

        #region Style Building

        private void BuildControlStyles()
        {
            _controlStyles[typeof(Button)] = CreateButtonStyle();
            _controlStyles[typeof(TextBox)] = CreateTextBoxStyle();
            _controlStyles[typeof(TextBlock)] = CreateLabelStyle();
            _controlStyles[typeof(ListBox)] = CreateListBoxStyle();
            _controlStyles[typeof(ListBoxItem)] = CreateListBoxItemStyle();
            _controlStyles[typeof(ScrollViewer)] = CreateScrollViewerStyle();
            _controlStyles[typeof(Grid)] = CreateGridStyle();
            _controlStyles[typeof(Border)] = CreateBorderStyle();
        }

        private Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));
            var btn = _config.Controls.Button;

            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(btn.Background)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(btn.Foreground)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(btn.BorderColor)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, _config.Fonts.SizeMd));
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeightFromInt(ResolveInt(btn.FontWeight, _config.Fonts.WeightMedium))));
            style.Setters.Add(new Setter(Control.PaddingProperty, PCThemeLoader.ParseThickness(_config.Dimensions.ButtonPadding)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.ButtonHeight));
            style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));

            // Template with rounded corners
            var template = CreateButtonTemplate(btn);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));

            return style;
        }

        private Style CreateSecondaryButtonStyle()
        {
            var style = new Style(typeof(Button));
            var btn = _config.Controls.ButtonSecondary;

            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(btn.Background)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(btn.Foreground)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(btn.BorderColor)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, _config.Fonts.SizeMd));
            style.Setters.Add(new Setter(Control.PaddingProperty, PCThemeLoader.ParseThickness(_config.Dimensions.ButtonPadding)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.ButtonHeight));
            return style;
        }

        private ControlTemplate CreateButtonTemplate(ButtonStyle btn)
        {
            var template = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, GetBrush(btn.Background));
            border.SetValue(Border.BorderBrushProperty, GetBrush(btn.BorderColor));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(ResolveDouble(btn.BorderRadius, _config.Dimensions.BorderRadius)));
            border.SetValue(Border.PaddingProperty, PCThemeLoader.ParseThickness(_config.Dimensions.ButtonPadding));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // Triggers
            var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, GetBrush(btn.BackgroundHover), "border"));
            template.Triggers.Add(mouseOverTrigger);

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, GetBrush(btn.BackgroundPressed), "border"));
            template.Triggers.Add(pressedTrigger);

            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty, GetBrush(btn.BackgroundDisabled), "border"));
            disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(btn.ForegroundDisabled)));
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        private Style CreateTextBoxStyle()
        {
            var style = new Style(typeof(TextBox));
            var tb = _config.Controls.TextBox;

            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(tb.Background)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(tb.Foreground)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(tb.BorderColor)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth)));
            style.Setters.Add(new Setter(Control.PaddingProperty, PCThemeLoader.ParseThickness(_config.Dimensions.InputPadding)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.InputHeight));
            style.Setters.Add(new Setter(Control.FontSizeProperty, ResolveDouble(tb.FontSize, _config.Fonts.SizeMd)));
            style.Setters.Add(new Setter(TextBox.CaretBrushProperty, GetBrush(_config.Colors.Foreground1)));
            style.Setters.Add(new Setter(TextBox.SelectionBrushProperty, GetBrush(_config.Colors.Primary)));
            style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));

            // Focus trigger
            var focusTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            focusTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(tb.BorderColorFocused)));
            focusTrigger.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(tb.BackgroundFocused)));
            style.Triggers.Add(focusTrigger);

            return style;
        }

        private Style CreateLabelStyle()
        {
            var style = new Style(typeof(TextBlock));
            var lbl = _config.Controls.Label;

            style.Setters.Add(new Setter(TextBlock.ForegroundProperty, GetBrush(lbl.Foreground)));
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, ResolveDouble(lbl.FontSize, _config.Fonts.SizeMd)));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, PCThemeLoader.ParseFontFamily(_config.Fonts.Primary)));

            return style;
        }

        private Style CreateListBoxStyle()
        {
            var style = new Style(typeof(ListBox));
            var lb = _config.Controls.ListBox;

            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(lb.Background)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(lb.Foreground)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, GetBrush(lb.BorderColor)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth)));

            return style;
        }

        private Style CreateListBoxItemStyle()
        {
            var style = new Style(typeof(ListBoxItem));
            var lb = _config.Controls.ListBox;

            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(lb.ItemBackground)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(lb.Foreground)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(_config.Dimensions.Spacing)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(lb.ItemBackgroundHover)));
            style.Triggers.Add(mouseOverTrigger);

            var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(lb.ItemBackgroundSelected)));
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(lb.ItemForegroundSelected)));
            style.Triggers.Add(selectedTrigger);

            return style;
        }

        private Style CreateScrollViewerStyle()
        {
            var style = new Style(typeof(ScrollViewer));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            return style;
        }

        private Style CreateGridStyle()
        {
            var style = new Style(typeof(Grid));
            //style.Setters.Add(new Setter(Panel.BackgroundProperty, GetBrush(_config.Colors.Background1)));
            return style;
        }

        private Style CreateToggleSwitchStyle()
        {
            var style = new Style(typeof(CheckBox));
            // Provide simple visual change, using a Border background and content placement for simplicity
            style.Setters.Add(new Setter(Control.ForegroundProperty, GetBrush(_config.Colors.Foreground1)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, GetBrush(_config.Colors.Background2)));
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 28.0));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 0, 6, 0)));
            return style;
        }

        private Style CreateBorderStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, GetBrush(_config.Colors.Background2)));
            style.Setters.Add(new Setter(Border.BorderBrushProperty, GetBrush(_config.Colors.Border)));
            style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(_config.Dimensions.BorderWidth)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(_config.Dimensions.BorderRadius)));
            return style;
        }

        #endregion

        #region Animation Helpers

        private Shared.Interface.AnimationConfig ParseAnimationConfig(Theme.AnimationConfig config)
        {
            var animType = AnimationType.None;

            foreach (var type in config.Type.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<AnimationType>(type.Trim(), true, out var parsed))
                {
                    animType |= parsed;
                }
            }

            return new Shared.Interface.AnimationConfig
            {
                Types = animType,
                Duration = TimeSpan.FromMilliseconds(config.Duration),
                EasingFunction = GetEasingFunction(config.Easing)
            };
        }

        private IEasingFunction? GetEasingFunction(string easing)
        {
            return easing?.ToLowerInvariant() switch
            {
                "linear" => null,
                "cubiceasein" => new CubicEase { EasingMode = EasingMode.EaseIn },
                "cubiceaseout" => new CubicEase { EasingMode = EasingMode.EaseOut },
                "cubiceaseinout" => new CubicEase { EasingMode = EasingMode.EaseInOut },
                "quadraticeasein" => new QuadraticEase { EasingMode = EasingMode.EaseIn },
                "quadraticeaseout" => new QuadraticEase { EasingMode = EasingMode.EaseOut },
                "bounceeasein" => new BounceEase { EasingMode = EasingMode.EaseIn },
                "bounceeaseout" => new BounceEase { EasingMode = EasingMode.EaseOut },
                "elasticeasein" => new ElasticEase { EasingMode = EasingMode.EaseIn },
                "elasticeaseout" => new ElasticEase { EasingMode = EasingMode.EaseOut },
                _ => new CubicEase { EasingMode = EasingMode.EaseOut }
            };
        }

        #endregion

        #region Additional Methods

        /// <summary>
        /// 获取窗口样式配置
        /// </summary>
        public WindowStyle GetWindowConfig() => _config.Controls.Window;

        /// <summary>
        /// 获取标题栏按钮样式配置
        /// </summary>
        public TitleBarButtonStyle GetTitleBarButtonConfig() => _config.Controls.TitleBarButton;

        /// <summary>
        /// 获取完整主题配置
        /// </summary>
        public PCThemeConfig GetConfig() => _config;

        #endregion
    }
}