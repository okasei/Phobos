using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

namespace Phobos.Class.Arcusrix
{
    /// <summary>
    /// Dark 主题（不提供样式，仅提供颜色配置）
    /// </summary>
    public class PCDarkTheme : IPhobosTheme, IThemePlugin
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
        private Phobos.Class.Theme.PCConfigBasedTheme? _inner;

        #region IPhobosTheme 实现

        public string Name => _inner?.Name ?? "Dark";
        public string ThemeId => _inner?.ThemeId ?? "dark";
        public string Version => _inner?.Version ?? "2.0.0";
        public string Author => _inner?.Author ?? "Phobos Team";

        public string GetLocalizedName(string languageCode)
        {
            if (_localizedNames.TryGetValue(languageCode, out var name))
                return name;
            return Name;
        }

        public ResourceDictionary GetGlobalStyles()
        {
            if (_inner == null)
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes", "dark.phobos-theme.json");
                var cfg = Phobos.Class.Theme.PCThemeLoader.LoadFromFile(path) ?? new Phobos.Class.Theme.PCThemeConfig();
                _inner = new Phobos.Class.Theme.PCConfigBasedTheme(cfg);
            }
            return _inner.GetGlobalStyles();
        }

        public Style? GetControlStyle(Type controlType)
        {
            return _inner?.GetControlStyle(controlType);
        }

        public ControlAnimationConfig GetControlAnimationConfig(Type controlType)
        {
            if (_inner != null)
            {
                return _inner.GetControlAnimationConfig(controlType);
            }
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
            _inner?.Apply();
        }

        public void Unload()
        {
            _inner?.Unload();
        }

        #endregion

        #region IThemePlugin 实现

        /// <summary>
        /// Dark 主题不提供完整样式，依赖 Arcusrix 或插件提供的样式
        /// </summary>
        public bool ProvidesFullStyles => false;

        /// <summary>
        /// 不提供任何样式键
        /// </summary>
        public IReadOnlyList<string> ProvidedStyleKeys => Array.Empty<string>();

        /// <summary>
        /// 不提供样式资源字典
        /// </summary>
        public ResourceDictionary? GetStylesDictionary() => null;

        /// <summary>
        /// 不加载样式
        /// </summary>
        public bool LoadStyles() => false;

        /// <summary>
        /// 无需卸载样式
        /// </summary>
        public void UnloadStyles() { }

        #endregion
    }
}
