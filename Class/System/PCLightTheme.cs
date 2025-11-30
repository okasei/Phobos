using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

namespace Phobos.Class.System
{
    /// <summary>
    /// Light 主题
    /// </summary>
    public class PCLightTheme : IPhobosTheme
    {
        private readonly Dictionary<string, string> _localizedNames = new()
        {
            { "en-US", "Light" },
            { "zh-CN", "浅色" },
            { "zh-TW", "淺色" },
            { "ja-JP", "ライト" },
            { "ko-KR", "라이트" }
        };

        private readonly Dictionary<Type, ControlAnimationConfig> _animationConfigs = new();
        private ResourceDictionary? _resources;

        public string Name => "Light";
        public string ThemeId => "light";
        public string Version => "1.0.0";
        public string Author => "Phobos Team";

        public string GetLocalizedName(string languageCode)
        {
            if (_localizedNames.TryGetValue(languageCode, out var name))
                return name;
            return Name;
        }

        public ResourceDictionary GetGlobalStyles()
        {
            if (_resources != null)
                return _resources;

            _resources = new ResourceDictionary();

            // 定义颜色
            _resources["PrimaryBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            _resources["SecondaryBackground"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            _resources["PrimaryForeground"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            _resources["SecondaryForeground"] = new SolidColorBrush(Color.FromRgb(117, 117, 117));
            _resources["AccentColor"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
            _resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            _resources["HoverBackground"] = new SolidColorBrush(Color.FromRgb(229, 229, 229));
            _resources["SelectedBackground"] = new SolidColorBrush(Color.FromRgb(204, 228, 247));

            // 按钮样式
            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, _resources["SecondaryBackground"]));
            buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, _resources["PrimaryForeground"]));
            buttonStyle.Setters.Add(new Setter(Control.BorderBrushProperty, _resources["BorderColor"]));
            buttonStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            buttonStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 12, 6)));
            buttonStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            _resources[typeof(Button)] = buttonStyle;

            // TextBox 样式
            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, _resources["PrimaryBackground"]));
            textBoxStyle.Setters.Add(new Setter(Control.ForegroundProperty, _resources["PrimaryForeground"]));
            textBoxStyle.Setters.Add(new Setter(Control.BorderBrushProperty, _resources["BorderColor"]));
            textBoxStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            textBoxStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
            _resources[typeof(TextBox)] = textBoxStyle;

            // Label 样式
            var labelStyle = new Style(typeof(Label));
            labelStyle.Setters.Add(new Setter(Control.ForegroundProperty, _resources["PrimaryForeground"]));
            _resources[typeof(Label)] = labelStyle;

            // Grid 样式
            var gridStyle = new Style(typeof(Grid));
            gridStyle.Setters.Add(new Setter(Panel.BackgroundProperty, _resources["PrimaryBackground"]));
            _resources[typeof(Grid)] = gridStyle;

            return _resources;
        }

        public Style? GetControlStyle(Type controlType)
        {
            var resources = GetGlobalStyles();
            if (resources.Contains(controlType))
                return resources[controlType] as Style;
            return null;
        }

        public ControlAnimationConfig GetControlAnimationConfig(Type controlType)
        {
            if (_animationConfigs.TryGetValue(controlType, out var config))
                return config;
            return GetDefaultAnimationConfig();
        }

        public ControlAnimationConfig GetDefaultAnimationConfig()
        {
            return new ControlAnimationConfig
            {
                OnLoad = new AnimationConfig
                {
                    Types = AnimationType.FadeIn,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                },
                OnRestore = new AnimationConfig
                {
                    Types = AnimationType.FadeIn,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                }
            };
        }

        public void Apply()
        {
            var resources = GetGlobalStyles();
            Application.Current.Resources.MergedDictionaries.Add(resources);
        }

        public void Unload()
        {
            if (_resources != null && Application.Current.Resources.MergedDictionaries.Contains(_resources))
            {
                Application.Current.Resources.MergedDictionaries.Remove(_resources);
            }
        }
    }
}