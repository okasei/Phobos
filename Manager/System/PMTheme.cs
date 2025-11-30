using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Interface.System;
using Phobos.Shared.Interface;

namespace Phobos.Manager.System
{
    /// <summary>
    /// 主题管理器实现
    /// </summary>
    public class PMTheme : PIThemeManager
    {
        private static PMTheme? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, IPhobosTheme> _themes = new(StringComparer.OrdinalIgnoreCase);
        private IPhobosTheme? _currentTheme;

        public static PMTheme Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMTheme();
                    }
                }
                return _instance;
            }
        }

        public IPhobosTheme? CurrentTheme => _currentTheme;
        public string CurrentThemeId => _currentTheme?.ThemeId ?? string.Empty;
        public IReadOnlyList<IPhobosTheme> AvailableThemes => _themes.Values.ToList();

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        private PMTheme()
        {
            // 注册默认主题
            RegisterTheme(new Class.System.PCLightTheme());
            RegisterTheme(new Class.System.PCDarkTheme());
        }

        public async Task<bool> LoadTheme(string themeId)
        {
            return await Task.Run(() =>
            {
                if (!_themes.TryGetValue(themeId, out var theme))
                    return false;

                var oldThemeId = CurrentThemeId;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 卸载旧主题
                    _currentTheme?.Unload();

                    // 应用新主题
                    theme.Apply();
                    _currentTheme = theme;

                    // 触发事件
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                    {
                        OldThemeId = oldThemeId,
                        NewThemeId = themeId,
                        NewTheme = theme
                    });
                });

                return true;
            });
        }

        public void RegisterTheme(IPhobosTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            _themes[theme.ThemeId] = theme;
        }

        public void UnregisterTheme(string themeId)
        {
            if (string.Equals(themeId, CurrentThemeId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot unregister the current theme");

            _themes.Remove(themeId);
        }

        public IPhobosTheme? GetTheme(string themeId)
        {
            _themes.TryGetValue(themeId, out var theme);
            return theme;
        }

        public void ApplyThemeToWindow(Window window)
        {
            if (_currentTheme == null || window == null)
                return;

            var resources = _currentTheme.GetGlobalStyles();

            // 确保不重复添加
            if (!window.Resources.MergedDictionaries.Contains(resources))
            {
                window.Resources.MergedDictionaries.Add(resources);
            }
        }
    }
}