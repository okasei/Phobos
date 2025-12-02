using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Class.Theme;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Interface.Arcusrix;
using Phobos.Shared.Interface;

namespace Phobos.Manager.Arcusrix
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
        private string _themesDirectory = string.Empty;

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
        public string ThemesDirectory => _themesDirectory;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        private PMTheme()
        {
            // 注册代码定义的默认主题（作为回退）
            RegisterTheme(new Class.Arcusrix.PCLightTheme());
            RegisterTheme(new Class.Arcusrix.PCDarkTheme());
        }

        #region 初始化

        /// <summary>
        /// 初始化主题管理器
        /// </summary>
        /// <param name="themesDirectory">主题目录路径</param>
        public async Task Initialize(string? themesDirectory = null)
        {
            // 设置主题目录
            if (string.IsNullOrEmpty(themesDirectory))
            {
                _themesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Themes");
            }
            else
            {
                _themesDirectory = themesDirectory;
            }

            // 加载内置主题
            await RegisterDefaultThemes();

            // 加载外部主题
            await LoadExternalThemes();
        }

        /// <summary>
        /// 注册默认主题（从 Assets/Themes 加载 JSON）
        /// </summary>
        private async Task RegisterDefaultThemes()
        {
            var darkThemePath = Path.Combine(_themesDirectory, "dark.phobos-theme.json");
            var lightThemePath = Path.Combine(_themesDirectory, "light.phobos-theme.json");

            // 尝试加载 JSON 主题文件
            if (File.Exists(darkThemePath))
            {
                var theme = await LoadThemeFromFileInternal(darkThemePath);
                if (theme != null)
                {
                    _themes[theme.ThemeId] = theme;
                }
            }

            if (File.Exists(lightThemePath))
            {
                var theme = await LoadThemeFromFileInternal(lightThemePath);
                if (theme != null)
                {
                    _themes[theme.ThemeId] = theme;
                }
            }
        }

        /// <summary>
        /// 加载外部主题（用户自定义主题）
        /// </summary>
        private async Task LoadExternalThemes()
        {
            // 用户主题目录
            var userThemesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Phobos", "Themes");
            if (!Directory.Exists(userThemesDir))
            {
                Directory.CreateDirectory(userThemesDir);
                return;
            }

            // 扫描 *.phobos-theme.json 文件
            var themeFiles = Directory.GetFiles(userThemesDir, "*.phobos-theme.json");
            foreach (var file in themeFiles)
            {
                try
                {
                    var theme = await LoadThemeFromFileInternal(file);
                    if (theme != null && !_themes.ContainsKey(theme.ThemeId))
                    {
                        _themes[theme.ThemeId] = theme;
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Phobos.Theme.Loader", $"Failed to load theme from {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新主题列表
        /// </summary>
        public async Task RefreshThemes()
        {
            // 保留代码定义的主题
            var codeDefined = _themes.Values
                .Where(t => t is not PCConfigBasedTheme)
                .ToList();

            _themes.Clear();

            foreach (var theme in codeDefined)
            {
                _themes[theme.ThemeId] = theme;
            }

            // 重新加载文件主题
            await RegisterDefaultThemes();
            await LoadExternalThemes();
        }

        #endregion

        #region 加载主题

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

        /// <summary>
        /// 从文件加载并注册主题
        /// </summary>
        /// <param name="filePath">主题文件路径</param>
        public async Task<IPhobosTheme?> LoadThemeFromFile(string filePath)
        {
            var theme = await LoadThemeFromFileInternal(filePath);
            if (theme != null)
            {
                _themes[theme.ThemeId] = theme;
            }
            return theme;
        }

        /// <summary>
        /// 从 JSON 字符串加载并注册主题
        /// </summary>
        /// <param name="json">JSON 内容</param>
        public IPhobosTheme? LoadThemeFromJson(string json)
        {
            try
            {
                var config = PCThemeLoader.LoadFromJson(json);
                if (config != null)
                {
                    var theme = new PCConfigBasedTheme(config);
                    _themes[theme.ThemeId] = theme;
                    return theme;
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Loader.Json", $"Failed to load theme from JSON: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 内部方法：从文件加载主题
        /// </summary>
        private async Task<IPhobosTheme?> LoadThemeFromFileInternal(string filePath)
        {
            try
            {
                var config = await Task.Run(() => PCThemeLoader.LoadFromFile(filePath));
                if (config != null)
                {
                    return new PCConfigBasedTheme(config);
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Loader.Internal", $"Failed to load theme from {filePath}: {ex.Message}");
            }
            return null;
        }

        #endregion

        #region 注册/注销

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

        #endregion

        #region 应用主题

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

        #endregion

        #region 主题信息

        /// <summary>
        /// 获取所有可用主题的信息
        /// </summary>
        public List<ThemeInfo> GetAvailableThemeInfos()
        {
            return _themes.Values.Select(t => new ThemeInfo
            {
                ThemeId = t.ThemeId,
                Name = t.Name,
                Version = t.Version,
                Author = t.Author,
                IsCurrent = t.ThemeId == CurrentThemeId,
                IsFromFile = t is PCConfigBasedTheme
            }).ToList();
        }

        #endregion
    }

    /// <summary>
    /// 主题信息
    /// </summary>
    public class ThemeInfo
    {
        public string ThemeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public bool IsFromFile { get; set; }
    }
}