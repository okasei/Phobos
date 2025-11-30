using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

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

        // 兼容属性
        public string ThemeName => _config.Metadata.Name;
        public Color PrimaryColor => GetColor(_config.Colors.Primary);
        public Color SecondaryColor => GetColor(_config.Colors.Secondary);
        public Color BackgroundColor => GetColor(_config.Colors.Background);
        public Color SurfaceColor => GetColor(_config.Colors.Surface);
        public Color TextColor => GetColor(_config.Colors.Text);
        public Color TextSecondaryColor => GetColor(_config.Colors.TextSecondary);
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

            // 添加颜色资源
            _resourceDictionary["PrimaryColor"] = PrimaryColor;
            _resourceDictionary["SecondaryColor"] = SecondaryColor;
            _resourceDictionary["BackgroundColor"] = BackgroundColor;
            _resourceDictionary["SurfaceColor"] = SurfaceColor;
            _resourceDictionary["TextColor"] = TextColor;
            _resourceDictionary["BorderColor"] = BorderColor;

            _resourceDictionary["PrimaryBrush"] = GetBrush(_config.Colors.Primary);
            _resourceDictionary["SecondaryBrush"] = GetBrush(_config.Colors.Secondary);
            _resourceDictionary["BackgroundBrush"] = GetBrush(_config.Colors.Background);
            _resourceDictionary["SurfaceBrush"] = GetBrush(_config.Colors.Surface);
            _resourceDictionary["TextBrush"] = GetBrush(_config.Colors.Text);
            _resourceDictionary["TextSecondaryBrush"] = GetBrush(_config.Colors.TextSecondary);
            _resourceDictionary["BorderBrush"] = GetBrush(_config.Colors.Border);

            // 添加样式
            foreach (var kvp in _controlStyles)
            {
                var key = kvp.Key.Name + "Style";
                _resourceDictionary[key] = kvp.Value;
            }

            return _resourceDictionary;
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
                // 移除旧的主题资源
                var toRemove = new List<ResourceDictionary>();
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    if (dict.Contains("PhobosThemeMarker"))
                    {
                        toRemove.Add(dict);
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

        private double ResolveDouble(string value)
        {
            var resolved = ResolveValue(value);
            return double.TryParse(resolved, out var result) ? result : 0;
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
            style.Setters.Add(new Setter(Control.FontSizeProperty, ResolveDouble(btn.FontSize)));
            style.Setters.Add(new Setter(Control.FontWeightProperty, PCThemeLoader.ParseFontWeight(ResolveValue(btn.FontWeight))));
            style.Setters.Add(new Setter(Control.PaddingProperty, PCThemeLoader.ParseThickness(_config.Dimensions.ButtonPadding)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, _config.Dimensions.ButtonHeight));
            style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));

            // Template with rounded corners
            var template = CreateButtonTemplate(btn);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));

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
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(ResolveDouble(btn.BorderRadius)));
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
            style.Setters.Add(new Setter(Control.FontSizeProperty, ResolveDouble(tb.FontSize)));
            style.Setters.Add(new Setter(TextBox.CaretBrushProperty, GetBrush(_config.Colors.Text)));
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
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, ResolveDouble(lbl.FontSize)));
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, PCThemeLoader.ParseFontFamily(_config.Fonts.Family)));

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
            style.Setters.Add(new Setter(Panel.BackgroundProperty, GetBrush(_config.Colors.Background)));
            return style;
        }

        private Style CreateBorderStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, GetBrush(_config.Colors.Surface)));
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