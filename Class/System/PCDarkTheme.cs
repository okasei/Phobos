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
    /// Dark 主题
    /// </summary>
    public class PCDarkTheme : IPhobosTheme
    {
        private readonly Dictionary<string, string> _localizedNames = new()
        {
            { "en-US", "Dark" },
            { "zh-CN", "深色" },
            { "zh-TW", "深色" },
            { "ja-JP", "ダーク" },
            { "ko-KR", "다크" }
        };

        private readonly Dictionary<Type, ControlAnimationConfig> _animationConfigs = new();
        private ResourceDictionary? _resources;

        public string Name => "Dark";
        public string ThemeId => "dark";
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
            _resources["PrimaryBackground"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            _resources["SecondaryBackground"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            _resources["PrimaryForeground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            _resources["SecondaryForeground"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            _resources["AccentColor"] = new SolidColorBrush(Color.FromRgb(0, 150, 255));
            _resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
            _resources["HoverBackground"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            _resources["SelectedBackground"] = new SolidColorBrush(Color.FromRgb(0, 100, 180));

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
            textBoxStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, _resources["PrimaryForeground"]));
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