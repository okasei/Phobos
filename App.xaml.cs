using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Components.Arcusrix.Desktop;
using Phobos.Components.Arcusrix.Dialog;
using Phobos.Components.Plugin;
using Phobos.Manager.Arcusrix;
using Phobos.Manager.Plugin;
using Phobos.Service.Arcusrix;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Shared.Manager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Phobos
{
    /// <summary>
    /// Phobos Application Entry Point
    /// </summary>
    public partial class App : Application
    {
        private string _appDataPath = string.Empty;
        private string _pluginsPath = string.Empty;
        private string _databasePath = string.Empty;
        private readonly PluginResourceManager _resourceManager = new();

        /// <summary>
        /// 获取数据库实例
        /// </summary>
        private static PCSqliteDatabase? Database => Manager.Database.PMDatabase.Instance.Database;

        #region i18n Error Messages

        /// <summary>
        /// 获取错误消息的本地化文本
        /// </summary>
        private static string GetErrorMessage(string key)
        {
            return LocalizationManager.Instance.Get(key);
        }

        private static async Task ShowErrorDialogAsync(string titleKey, string messageKey, string? details = null)
        {
            var title = GetErrorMessage(titleKey);
            var message = GetErrorMessage(messageKey);
            if (!string.IsNullOrEmpty(details))
            {
                message += $"\n\n{GetErrorMessage("error.details")}\n{details}";
            }

            var config = new DialogConfig
            {
                Title = title,
                ContentText = message,
                ContentMode = DialogContentMode.LeftAlignedText,
                Width = 450,
                MinHeight = 180,
                MaxHeight = 400,
                ShowCancelButton = false,
                CloseOnEscape = true,
                IsDraggable = true,
                Buttons = new List<DialogButton>
                {
                    new DialogButton
                    {
                        Text = GetErrorMessage("button.ok"),
                        Tag = "ok",
                        ButtonType = DialogButtonType.Primary,
                        CloseOnClick = true
                    }
                }
            };

            await PCOPhobosDialog.ShowAsync(config);
        }

        private static void ShowFatalError(string titleKey, string messageKey, string? details = null)
        {
            var title = GetErrorMessage(titleKey);
            var message = GetErrorMessage(messageKey);
            if (!string.IsNullOrEmpty(details))
            {
                message += $"\n\n{GetErrorMessage("error.details")}\n{details}";
            }
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        /// <summary>
        /// 应用程序启动入口
        /// </summary>
        private async void Phobos_Awake(object sender, StartupEventArgs e)
        {
            try
            {
                LocalizationManager.Instance.CurrentLanguage = CultureInfo.CurrentCulture.IetfLanguageTag;

                // 初始化路径
                try
                {
                    InitializePaths();
                }
                catch (Exception ex)
                {
                    ShowFatalError("error.fatal.title", "error.paths.failed", ex.Message);
                    Shutdown(1);
                    return;
                }

                // 加载系统本地化资源（需要尽早加载，以便显示本地化的错误消息）
                try
                {
                    LoadSystemLocalization();
                }
                catch (Exception ex)
                {
                    // 本地化加载失败不是致命错误，继续执行
                    Debug.WriteLine($"System localization load failed: {ex.Message}");
                }

                // 初始化 SQLite
                try
                {
                    SQLitePCL.Batteries_V2.Init();
                }
                catch (Exception ex)
                {
                    ShowFatalError("error.fatal.title", "error.database.init.failed", ex.Message);
                    Shutdown(1);
                    return;
                }

                // 初始化数据库
                try
                {
                    await InitializeDatabase();
                }
                catch (Exception ex)
                {
                    ShowFatalError("error.fatal.title", "error.database.failed", ex.Message);
                    Shutdown(1);
                    return;
                }

                // 初始化本地化
                try
                {
                    InitializeLocalization();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Phobos", $"Localization init failed: {ex.Message}");
                    // 非致命错误，继续执行
                }

                // 加载主题
                try
                {
                    // 先初始化主题管理器（注册所有主题）
                    await PMTheme.Instance.Initialize();

                    // 然后加载已保存的主题
                    await PMTheme.Instance.LoadSavedThemeAsync();

                    await InitializeThemeManager();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Phobos", $"Theme init failed: {ex.Message}");
                    // 非致命错误，继续执行
                }

                // 初始化插件管理器
                try
                {
                    await InitializePluginManager();
                }
                catch (Exception ex)
                {
                    ShowFatalError("error.fatal.title", "error.plugin.manager.failed", ex.Message);
                    Shutdown(1);
                    return;
                }

                // 注册内置插件
                try
                {
                    await RegisterBuiltInPlugins();
                }
                catch (Exception ex)
                {
                    ShowFatalError("error.fatal.title", "error.plugin.register.failed", ex.Message);
                    Shutdown(1);
                    return;
                }

                // 执行启动项
                try
                {
                    await ExecuteBootItems();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Phobos", $"Boot items failed: {ex.Message}");
                    // 非致命错误，继续执行
                }

                // 处理命令行参数
                try
                {
                    await HandleCommandLineArgs(e.Args);
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Phobos", $"Command line args failed: {ex.Message}");
                    // 非致命错误，继续执行
                }

                PCLoggerPlugin.Info("Phobos", "Welcome to Phobos!");

                // 启动默认插件（Desktop）
                try
                {
                    await LaunchDefaultPlugin();
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("error.fatal.title", "error.plugin.launch.failed", ex.Message);
                    Shutdown(1);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowFatalError("error.fatal.title", "error.startup.failed", $"{ex.Message}\n\n{ex.StackTrace}");
                Shutdown(1);
            }
        }

        /// <summary>
        /// 加载系统本地化资源
        /// </summary>
        private void LoadSystemLocalization()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var systemLocDir = Path.Combine(baseDir, "Assets", "Localization", "System");

            if (Directory.Exists(systemLocDir))
            {
                LocalizationManager.Instance.LoadGlobalPacks(systemLocDir);
            }
        }

        private async Task<RequestResult> InitializeThemeManager()
        {
            var styles = PMTheme.Instance.CurrentTheme?.GetGlobalStyles();
            if (styles != null)
            {
                _resourceManager.SetHostTheme(styles);
            }
            return new RequestResult { Success = true };
        }

        /// <summary>
        /// 执行所有启动项
        /// </summary>
        private async Task ExecuteBootItems()
        {
            await PMPlugin.Instance.ExecuteBootItems();
            PCLoggerPlugin.Info("Phobos", "Boot items executed");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)
                )
            );
        }

        private void InitializePaths()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Phobos");

            _pluginsPath = Path.Combine(_appDataPath, "Plugins");
            _databasePath = Path.Combine(_appDataPath, "Phobos.db");

            // 确保目录存在（使用递归创建）
            Utils.IO.PUFileSystem.Instance.CreateFullFolders(_appDataPath);
            Utils.IO.PUFileSystem.Instance.CreateFullFolders(_pluginsPath);
        }

        private async Task InitializeDatabase()
        {
            var connected = await Manager.Database.PMDatabase.Instance.Initialize(_databasePath);

            if (!connected)
            {
                throw new Exception("Failed to connect to database");
            }

            // 初始化默认配置
            await InitializeDefaultConfig();
        }

        private async Task InitializeDefaultConfig()
        {
            if (Database == null) return;

            // 检查是否已初始化
            var result = await Database.ExecuteQuery(
                "SELECT COUNT(*) as count FROM Phobos_Main WHERE Key = 'Initialized'");

            if (result?.Count > 0 && Convert.ToInt32(result[0]["count"]) > 0)
                return;

            // 写入默认配置
            var defaultConfigs = new[]
            {
                ("Initialized", "true"),
                ("Version", "1.0.0"),
                ("Theme", "dark"),
                ("Language", "en-US")
            };

            foreach (var (key, value) in defaultConfigs)
            {
                await Database.ExecuteNonQuery(
                    "INSERT OR IGNORE INTO Phobos_Main (Key, Content, UpdateTime) VALUES (@key, @value, datetime('now'))",
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "@key", key },
                        { "@value", value }
                    });
            }

            // 确保 StartupPlugin 总是设置为 Desktop（覆盖旧值）
            await Database.ExecuteNonQuery(
                "INSERT OR REPLACE INTO Phobos_Main (Key, Content, UpdateTime) VALUES ('StartupPlugin', 'com.phobos.desktop', datetime('now'))");
        }

        private void InitializeLocalization()
        {
            var locManager = LocalizationManager.Instance;

            // 从数据库读取语言设置
            Task.Run(async () =>
            {
                if (Database != null)
                {
                    var result = await Database.ExecuteQuery(
                        "SELECT Content FROM Phobos_Main WHERE Key = 'Language'");
                    if (result?.Count > 0)
                    {
                        locManager.CurrentLanguage = result[0]["Content"]?.ToString() ?? "en-US";
                    }
                }
            }).Wait();
        }

        private async Task InitializePluginManager()
        {
            if (Database == null)
                throw new Exception("Database not initialized");

            await PMPlugin.Instance.Initialize(Database, _pluginsPath);
            PMProtocol.Instance.Initialize(Database);
        }

        private async Task RegisterBuiltInPlugins()
        {
            var pluginManager = PMPlugin.Instance;

            // 注册内置插件（直接添加到内存，不需要安装）
            var builtInPlugins = new IPhobosPlugin[]
            {
                new PCPluginManager(),
                new PCLoggerPlugin(),
                new PCSequencerPlugin(),
                new PCDialogPlugin(),
                new PCPluginInstaller(),
                new PCDesktopPlugin(),
                new PCThemePlugin(),
                new PCRunnerPlugin(),
                new PCNotifierPlugin(),
                new PCTaskManagerPlugin()
            };
            // 创建处理器

            var handlers = new PluginHandlers
            {
                RequestPhobos = PMPlugin.Instance.HandleRequestPhobos,
                Link = PMPlugin.Instance.HandleLink,
                Request = PMPlugin.Instance.HandleRequest,
                LinkDefault = PMPlugin.Instance.HandleLinkDefault,
                ReadConfig = PMPlugin.Instance.HandleReadConfig,
                WriteConfig = PMPlugin.Instance.HandleWriteConfig,
                ReadSysConfig = PMPlugin.Instance.HandleReadSysConfig,
                WriteSysConfig = PMPlugin.Instance.HandleWriteSysConfig,
                BootWithPhobos = PMPlugin.Instance.HandleBootWithPhobos,
                RemoveBootWithPhobos = PMPlugin.Instance.HandleRemoveBootWithPhobos,
                GetBootItems = PMPlugin.Instance.HandleGetBootItems,
                Subscribe = async (caller, eventId, eventName, args) =>
                {
                    return PMEvent.Instance.Subscribe(caller.PackageName, eventId, eventName);
                },
                Unsubscribe = async (caller, eventId, eventName, args) =>
                {
                    return PMEvent.Instance.Unsubscribe(caller.PackageName, eventId, eventName);
                },
                TriggerEvent = async (caller, eventId, eventName, args) =>
                {
                    await PMEvent.Instance.TriggerFromPluginAsync(caller.PackageName, eventId, eventName, args);
                    return new RequestResult { Success = true, Message = $"Event {eventId}.{eventName} triggered" };
                },
                SetDefaultHandler = PMPlugin.Instance.HandleSetDefaultHandler,
                GetDefaultHandler = PMPlugin.Instance.HandleGetDefaultHandler,
                SendNotification = PMPlugin.Instance.HandleSendNotification,
                SendNotificationObject = PMPlugin.Instance.HandleSendNotificationObject,
                Log = PMPlugin.Instance.HandleLog,
                RequestPlugin = PMPlugin.Instance.HandleRequestPlugin,
                RequestProtocolHandler = PMPlugin.Instance.HandleRequestProtocolHandler,
                GetCacheFolder = PMPlugin.Instance.HandleGetCacheFolder,
                GetPluginFolder = PMPlugin.Instance.HandleGetPluginFolder
            };


            foreach (var plugin in builtInPlugins)
            {
                // 将内置插件注册到 PMPlugin，让其可被 Launch/GetPlugin 获取
                PMPlugin.Instance.RegisterBuiltInPlugin(plugin);

                // 设置处理器
                if (plugin is PCPluginBase basePlugin)
                {
                    basePlugin.SetPhobosHandlers(handlers);
                }

                // 检查是否已在数据库中注册
                if (Database != null)
                {
                    var existing = await Database.ExecuteQuery(
                        "SELECT PackageName, Version FROM Phobos_Plugin WHERE PackageName = @packageName",
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "@packageName", plugin.Metadata.PackageName }
                        });
                    var uninstallInfoJson = plugin.Metadata.UninstallInfo != null ? Newtonsoft.Json.JsonConvert.SerializeObject(plugin.Metadata.UninstallInfo) : string.Empty;
                    var mainAssembly = plugin.Metadata.GetMainAssemblyFileName() ?? string.Empty;
                    if (existing?.Count == 0)
                    {
                        // 首次注册到数据库
                        await Database.ExecuteNonQuery(
                            @"INSERT INTO Phobos_Plugin (PackageName, Name, Manufacturer, Description, Version, Secret, Directory, MainAssembly,
                                Icon, IsSystemPlugin, SettingUri, UninstallInfo, IsEnabled, UpdateTime, Entry, LaunchFlag)
                              VALUES (@packageName, @name, @manufacturer, @description, @version, @secret, 'builtin', @mainAssembly,
                                @icon, @isSystemPlugin, @settingUri, @uninstallInfo, 1, datetime('now'), @entry, @launchFlag)",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@packageName", plugin.Metadata.PackageName },
                                { "@name", plugin.Metadata.Name },
                                { "@manufacturer", plugin.Metadata.Manufacturer },
                                { "@description", plugin.Metadata.GetLocalizedDescription("en-US") },
                                { "@version", plugin.Metadata.Version },
                                { "@secret", plugin.Metadata.Secret },
                                { "@mainAssembly", mainAssembly },
                                { "@icon", plugin.Metadata.Icon ?? string.Empty },
                                { "@isSystemPlugin", plugin.Metadata.IsSystemPlugin ? 1 : 0 },
                                { "@settingUri", plugin.Metadata.SettingUri ?? string.Empty },
                                { "@uninstallInfo", uninstallInfoJson },
                                { "@entry", plugin.Metadata.Entry ?? string.Empty },
                                { "@launchFlag", plugin.Metadata.LaunchFlag == true ? 1 : 0 }
                            });
                        // 首次安装时调用 OnInstall
                        await plugin.OnInstall();
                        PCLoggerPlugin.Debug("Phobos", $"Registered new built-in plugin: {plugin.Metadata.PackageName}");
                    }
                    else if (existing?.Count > 0)
                    {
                        // 检查版本是否有变化，只有版本更新时才更新数据库
                        var existingVersion = existing[0]["Version"]?.ToString() ?? "0.0.0";
                        var compareResult = Utils.Version.PUVersion.Compare(plugin.Metadata.Version, existingVersion);

                        if (compareResult == Utils.Version.VersionCompareResult.Greater)
                        {
                            // 版本更新，更新数据库记录
                            await Database.ExecuteNonQuery(
                                @"UPDATE Phobos_Plugin SET
                                    Name = @name, Manufacturer = @manufacturer, Description = @description,
                                    Version = @version, MainAssembly = @mainAssembly, Icon = @icon, IsSystemPlugin = @isSystemPlugin,
                                    SettingUri = @settingUri, UninstallInfo = @uninstallInfo, Entry = @entry,
                                    LaunchFlag = @launchFlag, UpdateTime = datetime('now')
                                  WHERE PackageName = @packageName",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@packageName", plugin.Metadata.PackageName },
                                    { "@name", plugin.Metadata.Name },
                                    { "@manufacturer", plugin.Metadata.Manufacturer },
                                    { "@description", plugin.Metadata.GetLocalizedDescription("en-US") },
                                    { "@version", plugin.Metadata.Version },
                                    { "@mainAssembly", mainAssembly },
                                    { "@icon", plugin.Metadata.Icon ?? string.Empty },
                                    { "@isSystemPlugin", plugin.Metadata.IsSystemPlugin ? 1 : 0 },
                                    { "@settingUri", plugin.Metadata.SettingUri ?? string.Empty },
                                    { "@uninstallInfo", uninstallInfoJson },
                                    { "@entry", plugin.Metadata.Entry ?? string.Empty },
                                    { "@launchFlag", plugin.Metadata.LaunchFlag == true ? 1 : 0 }
                                });
                            // 调用 OnUpdate
                            await plugin.OnUpdate(existingVersion, plugin.Metadata.Version);
                            PCLoggerPlugin.Info("Phobos", $"Updated built-in plugin: {plugin.Metadata.PackageName} ({existingVersion} -> {plugin.Metadata.Version})");
                        }
                        // 版本相同或更低时，跳过更新（静默加载）
                    }
                }

            }

            // 记录日志
            PCLoggerPlugin.Info("Phobos", $"Registered {builtInPlugins.Length} built-in plugins");
        }

        private async Task HandleCommandLineArgs(string[] args)
        {
            if (args.Length == 0) return;

            foreach (var arg in args)
            {
                // 处理协议链接
                if (arg.Contains("://"))
                {
                    var result = await PMProtocol.Instance.HandleProtocol(arg);
                    if (result.Success && result.Data.Count > 0)
                    {
                        var command = result.Data[0]?.ToString();
                        // 执行命令
                        PCLoggerPlugin.Info("Phobos", $"Handling protocol: {arg} -> {command}");
                    }
                }
            }
        }

        private async Task LaunchDefaultPlugin()
        {
            string startupPlugin = "com.phobos.desktop";

            if (Database != null)
            {
                // 首先查找关联了 launcher 特殊项的插件（优先使用）
                var launcherResult = await Database.ExecuteQuery(
                    @"SELECT ai.PackageName FROM Phobos_AssociatedItem ai
                      INNER JOIN Phobos_Protocol p ON ai.Name = p.AssociatedItem
                      WHERE p.Protocol = 'launcher'
                      ORDER BY p.UpdateTime DESC
                      LIMIT 1");

                if (launcherResult?.Count > 0)
                {
                    var launcherPlugin = launcherResult[0]["PackageName"]?.ToString();
                    if (!string.IsNullOrEmpty(launcherPlugin))
                    {
                        startupPlugin = launcherPlugin;
                        PCLoggerPlugin.Info("Phobos", $"Using launcher-associated plugin: {startupPlugin}");
                    }
                }
                else
                {
                    // 如果没有 launcher 关联，回退到数据库中的 StartupPlugin 设置
                    var result = await Database.ExecuteQuery(
                        "SELECT Content FROM Phobos_Main WHERE Key = 'StartupPlugin'");
                    if (result?.Count > 0)
                    {
                        startupPlugin = result[0]["Content"]?.ToString() ?? startupPlugin;
                    }
                }
            }

            // 启动插件管理器（Launch 会自动创建并显示窗口）
            var pluginManager = PMPlugin.Instance;
            await pluginManager.Launch(startupPlugin);

            // 获取已创建的窗口（不要再次创建）
            var windowManager = PMWindow.Instance;
            var window = windowManager.GetPluginWindow(startupPlugin);

            if (window != null)
            {
                // 设置为主窗口
                MainWindow = window;

                // 窗口关闭时退出应用
                window.Closed += (s, e) =>
                {
                    if (PMWindow.Instance.OpenWindows.Count == 0)
                    {
                        Shutdown();
                    }
                };
            }

            PCLoggerPlugin.Info("Phobos", "Application started successfully");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 关闭所有插件
            Task.Run(async () =>
            {
                var plugins = PMPlugin.Instance.LoadedPlugins;
                foreach (var kvp in plugins)
                {
                    if (kvp.Value.Instance != null)
                    {
                        await kvp.Value.Instance.OnClosing();
                    }
                }
            }).Wait();

            // 关闭数据库连接
            Manager.Database.PMDatabase.Instance.Close().Wait();

            PCLoggerPlugin.Info("Phobos", "Application exiting");

            base.OnExit(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Phobos_Awake(sender, e);
        }
    }
}
