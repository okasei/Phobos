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
using Phobos.Manager.Database;

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
        private IThemePlugin? _activeStylePlugin;
        private bool _arcusrixStylesLoaded = false;

        /// <summary>
        /// 数据库中保存主题设置的 key
        /// </summary>
        public const string ThemeSettingKey = "Theme";

        /// <summary>
        /// 默认主题 ID（代码定义的 DarkTheme，作为最终回退）
        /// </summary>
        public const string DefaultThemeId = "dark";

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

        /// <summary>
        /// 当前活跃的样式提供插件
        /// </summary>
        public IThemePlugin? ActiveStylePlugin => _activeStylePlugin;

        /// <summary>
        /// 是否由插件提供样式
        /// </summary>
        public bool HasPluginStyles => _activeStylePlugin != null && _activeStylePlugin.ProvidesFullStyles;

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

            await LoadExternalThemes(_themesDirectory);

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
        private async Task LoadExternalThemes(string? DirectoryPath = null)
        {
            // 用户主题目录
            var userThemesDir = DirectoryPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Phobos", "Themes");
            if (!Directory.Exists(userThemesDir))
            {
                Utils.IO.PUFileSystem.Instance.CreateFullFolders(userThemesDir);
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

        /// <summary>
        /// 从数据库读取已保存的主题设置
        /// </summary>
        /// <returns>保存的主题 ID，如果不存在则返回 null</returns>
        public async Task<string?> GetSavedThemeIdAsync()
        {
            try
            {
                return await PMDatabase.Instance.GetSystemConfig(ThemeSettingKey);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Settings", $"Failed to read theme setting: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存主题设置到数据库
        /// </summary>
        /// <param name="themeId">要保存的主题 ID</param>
        public async Task<bool> SaveThemeSettingAsync(string themeId)
        {
            try
            {
                return await PMDatabase.Instance.SetSystemConfig(ThemeSettingKey, themeId);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Settings", $"Failed to save theme setting: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载主题并可选保存到数据库
        /// </summary>
        /// <param name="themeId">主题 ID</param>
        /// <param name="saveToDatabase">是否保存到数据库（默认 true）</param>
        public async Task<bool> LoadThemeAndSaveAsync(string themeId, bool saveToDatabase = true)
        {
            var success = await LoadTheme(themeId);
            if (success && saveToDatabase)
            {
                await SaveThemeSettingAsync(themeId);
            }
            return success;
        }

        /// <summary>
        /// 加载已保存的主题或使用默认主题
        /// </summary>
        public async Task<bool> LoadSavedThemeAsync()
        {
            var savedThemeId = await GetSavedThemeIdAsync();

            // 如果有保存的主题且存在，则加载
            if (!string.IsNullOrEmpty(savedThemeId) && _themes.ContainsKey(savedThemeId))
            {
                PCLoggerPlugin.Debug("Phobos.Theme.Loader", $"Loading saved theme: {savedThemeId}");
                return await LoadTheme(savedThemeId);
            }

            // 否则回退到默认主题（代码定义的 DarkTheme）
            PCLoggerPlugin.Debug("Phobos.Theme.Loader", $"Saved theme not found, falling back to: {DefaultThemeId}");
            return await LoadTheme(DefaultThemeId);
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

                    // 检查是否有插件提供样式
                    if (HasPluginStyles)
                    {
                        // 插件提供了样式，不需要加载 Arcusrix 样式
                        PCLoggerPlugin.Debug("Phobos.Theme.Loader", "Using plugin-provided styles");
                    }
                    else
                    {
                        // 确保 Arcusrix 样式已加载
                        EnsureArcusrixStylesLoaded();
                    }

                    // 应用新主题（这会更新颜色 tokens）
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
        /// 注册样式提供插件
        /// </summary>
        /// <param name="plugin">实现 IThemePlugin 的插件</param>
        /// <returns>是否成功注册</returns>
        public bool RegisterStylePlugin(IThemePlugin plugin)
        {
            if (plugin == null)
                return false;

            if (!plugin.ProvidesFullStyles)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Plugin", "Plugin does not provide full styles");
                return false;
            }

            // 检查是否提供了必需的样式
            var providedKeys = plugin.ProvidedStyleKeys;
            var missingKeys = PhobosRequiredStyleKeys.Core.Where(k => !providedKeys.Contains(k)).ToList();
            if (missingKeys.Count > 0)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Plugin", $"Plugin missing required styles: {string.Join(", ", missingKeys)}");
                return false;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 卸载旧的插件样式
                _activeStylePlugin?.UnloadStyles();

                // 移除 Arcusrix 样式
                RemoveArcusrixStyles();

                // 加载新的插件样式
                if (plugin.LoadStyles())
                {
                    _activeStylePlugin = plugin;
                    PCLoggerPlugin.Info("Phobos.Theme.Plugin", $"Style plugin registered with {providedKeys.Count} styles");
                }
                else
                {
                    // 加载失败，回退到 Arcusrix 样式
                    EnsureArcusrixStylesLoaded();
                    PCLoggerPlugin.Error("Phobos.Theme.Plugin", "Failed to load plugin styles, falling back to Arcusrix");
                }
            });

            return _activeStylePlugin == plugin;
        }

        /// <summary>
        /// 注销样式提供插件
        /// </summary>
        public void UnregisterStylePlugin()
        {
            if (_activeStylePlugin == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _activeStylePlugin.UnloadStyles();
                _activeStylePlugin = null;

                // 回退到 Arcusrix 样式
                EnsureArcusrixStylesLoaded();
                PCLoggerPlugin.Info("Phobos.Theme.Plugin", "Style plugin unregistered, using Arcusrix styles");
            });
        }

        /// <summary>
        /// 确保 Arcusrix 样式已加载
        /// </summary>
        private void EnsureArcusrixStylesLoaded()
        {
            if (Application.Current == null) return;

            // 如果有插件提供样式，不加载 Arcusrix
            if (HasPluginStyles) return;

            // 检查是否已加载
            if (_arcusrixStylesLoaded)
            {
                bool stillExists = Application.Current.Resources.MergedDictionaries
                    .Any(d => d.Source?.OriginalString?.Contains("PhobosStyles.xaml") == true ||
                              d.Contains("PhobosArcusrixStylesMarker"));
                if (stillExists) return;
            }

            try
            {
                var stylesDict = new ResourceDictionary
                {
                    Source = new Uri("/Phobos;component/UI/Arcusrix/PhobosStyles.xaml", UriKind.Relative)
                };
                stylesDict["PhobosArcusrixStylesMarker"] = true;
                Application.Current.Resources.MergedDictionaries.Insert(0, stylesDict);
                _arcusrixStylesLoaded = true;
                PCLoggerPlugin.Debug("Phobos.Theme.Loader", "Arcusrix styles loaded");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Warning("Phobos.Theme.Loader", $"Failed to load Arcusrix styles: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除已加载的 Arcusrix 样式
        /// </summary>
        private void RemoveArcusrixStyles()
        {
            if (Application.Current == null) return;

            var toRemove = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source?.OriginalString?.Contains("PhobosStyles.xaml") == true ||
                            d.Source?.OriginalString?.Contains("UI/Arcusrix/") == true ||
                            d.Contains("PhobosArcusrixStylesMarker"))
                .ToList();

            foreach (var dict in toRemove)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dict);
                PCLoggerPlugin.Debug("Phobos.Theme.Loader", $"Removed Arcusrix style: {dict.Source?.OriginalString ?? "inline"}");
            }

            _arcusrixStylesLoaded = false;
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

        /// <summary>
        /// 安装主题（从文件导入并注册到数据库）
        /// </summary>
        /// <param name="filePath">主题文件路径</param>
        /// <param name="copyToThemesFolder">是否复制到用户主题目录</param>
        /// <returns>安装的主题，失败返回 null</returns>
        public async Task<IPhobosTheme?> InstallThemeAsync(string filePath, bool copyToThemesFolder = true)
        {
            try
            {
                var theme = await LoadThemeFromFileInternal(filePath);
                if (theme == null)
                {
                    PCLoggerPlugin.Error("Phobos.Theme.Install", $"Failed to load theme from {filePath}");
                    return null;
                }

                // 目标路径
                var targetPath = filePath;
                if (copyToThemesFolder)
                {
                    var userThemesDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Phobos", "Themes");
                    Utils.IO.PUFileSystem.Instance.CreateFullFolders(userThemesDir);

                    var fileName = $"{theme.ThemeId}.phobos-theme.json";
                    targetPath = Path.Combine(userThemesDir, fileName);

                    // 复制文件
                    if (!string.Equals(filePath, targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(filePath, targetPath, true);
                    }
                }

                // 注册到内存
                _themes[theme.ThemeId] = theme;

                // 注册到数据库
                var configTheme = theme as PCConfigBasedTheme;
                var description = configTheme?.GetConfig()?.Metadata?.Description ?? string.Empty;
                await PMDatabase.Instance.RegisterTheme(
                    theme.ThemeId,
                    theme.Name,
                    theme.Author,
                    description,
                    theme.Version,
                    targetPath,
                    isBuiltIn: false);

                PCLoggerPlugin.Info("Phobos.Theme.Install", $"Theme installed: {theme.Name} ({theme.ThemeId})");
                return theme;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Install", $"Failed to install theme: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 卸载主题
        /// </summary>
        /// <param name="themeId">要卸载的主题 ID</param>
        /// <param name="deleteFile">是否删除主题文件</param>
        /// <returns>是否成功卸载</returns>
        public async Task<bool> UninstallThemeAsync(string themeId, bool deleteFile = false)
        {
            try
            {
                // 检查是否是内置主题
                var themeRecord = await PMDatabase.Instance.GetThemeRecord(themeId);
                if (themeRecord?.IsBuiltIn == true)
                {
                    PCLoggerPlugin.Warning("Phobos.Theme.Uninstall", $"Cannot uninstall built-in theme: {themeId}");
                    return false;
                }

                // 检查是否是代码定义的主题
                var theme = GetTheme(themeId);
                if (theme != null && theme is not PCConfigBasedTheme)
                {
                    PCLoggerPlugin.Warning("Phobos.Theme.Uninstall", $"Cannot uninstall code-defined theme: {themeId}");
                    return false;
                }

                // 如果当前正在使用这个主题，回退到默认主题
                if (string.Equals(CurrentThemeId, themeId, StringComparison.OrdinalIgnoreCase))
                {
                    PCLoggerPlugin.Info("Phobos.Theme.Uninstall", $"Current theme is being uninstalled, falling back to: {DefaultThemeId}");
                    await LoadThemeAndSaveAsync(DefaultThemeId);
                }

                // 从内存中移除
                _themes.Remove(themeId);

                // 从数据库中移除
                await PMDatabase.Instance.UnregisterTheme(themeId);

                // 删除文件（如果需要）
                if (deleteFile && themeRecord != null && !string.IsNullOrEmpty(themeRecord.FilePath) && File.Exists(themeRecord.FilePath))
                {
                    try
                    {
                        File.Delete(themeRecord.FilePath);
                        PCLoggerPlugin.Debug("Phobos.Theme.Uninstall", $"Deleted theme file: {themeRecord.FilePath}");
                    }
                    catch (Exception ex)
                    {
                        PCLoggerPlugin.Warning("Phobos.Theme.Uninstall", $"Failed to delete theme file: {ex.Message}");
                    }
                }

                PCLoggerPlugin.Info("Phobos.Theme.Uninstall", $"Theme uninstalled: {themeId}");
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Theme.Uninstall", $"Failed to uninstall theme: {ex.Message}");
                return false;
            }
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