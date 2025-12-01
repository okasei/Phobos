using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Plugin;
using Phobos.Manager.System;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Components.Plugin;

namespace Phobos
{
    /// <summary>
    /// Phobos Application Entry Point
    /// </summary>
    public partial class App : Application
    {
        private PCSqliteDatabase? _database;
        private string _appDataPath = string.Empty;
        private string _pluginsPath = string.Empty;
        private string _databasePath = string.Empty;

        /// <summary>
        /// 应用程序启动入口
        /// </summary>
        private async void Phobos_Awake(object sender, StartupEventArgs e)
        {
            try
            {
                // 初始化路径
                InitializePaths();

                SQLitePCL.Batteries_V2.Init();

                // 初始化数据库
                await InitializeDatabase();

                // 初始化本地化
                InitializeLocalization();

                // 加载主题
                await InitializeTheme();

                // 初始化插件管理器
                await InitializePluginManager();

                // 注册内置插件
                await RegisterBuiltInPlugins();

                // 执行启动项
                await ExecuteBootItems();

                // 处理命令行参数
                await HandleCommandLineArgs(e.Args);

                // 启动默认插件（Plugin Manager）
                await LaunchDefaultPlugin();

                PCLoggerPlugin.Info("Phobos", "Welcome to Phobos!");

                //var a = await PMPlugin.Instance.Install("C:\\Aurev\\Dev\\Phobos.Calculator\\bin\\x64\\Release\\net10.0-windows\\Phobos.Calculator.dll");
                //MessageBox.Show(a.Message);
                await PMTheme.Instance.Initialize();
                //await PMTheme.Instance.LoadThemeFromFile("C:\\Users\\Aurev\\AppData\\Roaming\\Phobos\\Themes\\com.phobos.theme.light-orange.json");
                await PMTheme.Instance.LoadTheme("com.phobos.theme.dark-orange");
                // PMTheme.Instance.LoadTheme("dark");
                await PMPlugin.Instance.Run("com.phobos.calculator", "show");
                //PMPlugin.Instance.Launch("com.phobos.plugin.manager", "");
                //new PCOPluginInstaller().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Phobos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
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

            // 确保目录存在
            if (!Directory.Exists(_appDataPath))
                Directory.CreateDirectory(_appDataPath);

            if (!Directory.Exists(_pluginsPath))
                Directory.CreateDirectory(_pluginsPath);
        }

        private async Task InitializeDatabase()
        {
            _database = new PCSqliteDatabase(_databasePath, useEncryption: false);
            var connected = await _database.Connect();

            if (!connected)
            {
                throw new Exception("Failed to connect to database");
            }

            // 初始化默认配置
            await InitializeDefaultConfig();
        }

        private async Task InitializeDefaultConfig()
        {
            if (_database == null) return;

            // 检查是否已初始化
            var result = await _database.ExecuteQuery(
                "SELECT COUNT(*) as count FROM Phobos_Main WHERE Key = 'Initialized'");

            if (result?.Count > 0 && Convert.ToInt32(result[0]["count"]) > 0)
                return;

            // 写入默认配置
            var defaultConfigs = new[]
            {
                ("Initialized", "true"),
                ("Version", "1.0.0"),
                ("Theme", "dark"),
                ("Language", "en-US"),
                ("StartupPlugin", "com.phobos.plugin.manager")
            };

            foreach (var (key, value) in defaultConfigs)
            {
                await _database.ExecuteNonQuery(
                    "INSERT OR IGNORE INTO Phobos_Main (Key, Content, UpdateTime) VALUES (@key, @value, datetime('now'))",
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "@key", key },
                        { "@value", value }
                    });
            }
        }

        private void InitializeLocalization()
        {
            var locManager = LocalizationManager.Instance;

            // 注册默认本地化资源
            locManager.Register("app.name", new LocalizedString(new System.Collections.Generic.Dictionary<string, string>
            {
                { "en-US", "Phobos" },
                { "zh-CN", "Phobos" }
            }));

            locManager.Register("app.error.database", new LocalizedString(new System.Collections.Generic.Dictionary<string, string>
            {
                { "en-US", "Database connection failed" },
                { "zh-CN", "数据库连接失败" }
            }));

            locManager.Register("app.error.plugin", new LocalizedString(new System.Collections.Generic.Dictionary<string, string>
            {
                { "en-US", "Plugin loading failed" },
                { "zh-CN", "插件加载失败" }
            }));

            // 从数据库读取语言设置
            Task.Run(async () =>
            {
                if (_database != null)
                {
                    var result = await _database.ExecuteQuery(
                        "SELECT Content FROM Phobos_Main WHERE Key = 'Language'");
                    if (result?.Count > 0)
                    {
                        locManager.CurrentLanguage = result[0]["Content"]?.ToString() ?? "en-US";
                    }
                }
            }).Wait();
        }

        private async Task InitializeTheme()
        {
            var themeManager = PMTheme.Instance;

            // 从数据库读取主题设置
            string themeId = "dark";
            if (_database != null)
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = 'Theme'");
                if (result?.Count > 0)
                {
                    themeId = result[0]["Content"]?.ToString() ?? "dark";
                }
            }

            // 加载主题
            await themeManager.LoadTheme(themeId);
        }

        private async Task InitializePluginManager()
        {
            if (_database == null)
                throw new Exception("Database not initialized");

            await PMPlugin.Instance.Initialize(_database, _pluginsPath);
            PMProtocol.Instance.Initialize(_database);
        }

        private async Task RegisterBuiltInPlugins()
        {
            var pluginManager = PMPlugin.Instance;

            // 注册内置插件（直接添加到内存，不需要安装）
            var builtInPlugins = new IPhobosPlugin[]
            {
                new PCPluginManagerPlugin(),
                new PCLoggerPlugin(),
                new PCSequencerPlugin()
            };
            // 创建处理器

            var handlers = new PluginHandlers
            {
                RequestPhobos = (caller, args) => Task.FromResult(new System.Collections.Generic.List<object>()),
                Link = async (caller, association) =>
                {
                    // 内置插件的 Link 处理
                    if (_database != null)
                    {
                        await _database.ExecuteNonQuery(
                            @"INSERT OR REPLACE INTO Phobos_AssociatedItem (Name, PackageName, Description, Command)
                              VALUES (@name, @packageName, @description, @command)",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@name", association.Name },
                                { "@packageName", caller.PackageName },
                                { "@description", Shared.Class.TextEscaper.Escape(association.Description) },
                                { "@command", association.Command }
                            });
                        await _database.ExecuteNonQuery(
                            @"INSERT OR REPLACE INTO Phobos_Protocol (UUID, Protocol, AssociatedItem, UpdateUID, UpdateTime)
                              VALUES (@uuid, @protocol, @associatedItem, @uid, datetime('now'))
                              ON CONFLICT(UUID) DO UPDATE SET UpdateTime = datetime('now')",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@uuid", Guid.NewGuid().ToString("N") },
                                { "@protocol", association.Protocol.ToLowerInvariant() },
                                { "@associatedItem", association.Name },
                                { "@uid", caller.PackageName }
                            });
                    }
                    return new RequestResult { Success = true };
                },
                Request = (caller, cmd, callback, args) => Task.FromResult(new RequestResult { Success = true }),
                LinkDefault = (caller, protocol) => Task.FromResult(new RequestResult { Success = true }),
                ReadConfig = (caller, key, pkg) => Task.FromResult(new ConfigResult { Success = false }),
                WriteConfig = (caller, key, val, pkg) => Task.FromResult(new ConfigResult { Success = true }),
                ReadSysConfig = (caller, key) => Task.FromResult(new ConfigResult { Success = false }),
                WriteSysConfig = (caller, key, val) => Task.FromResult(new ConfigResult { Success = true }),
                BootWithPhobos = (caller, cmd, priority, args) => Task.FromResult(new BootResult { Success = true }),
                RemoveBootWithPhobos = (caller, uuid) => Task.FromResult(new BootResult { Success = true }),
                GetBootItems = (caller) => Task.FromResult(new System.Collections.Generic.List<object>())

            };


            foreach (var plugin in builtInPlugins)
            {
                // 设置处理器
                if (plugin is PCPluginBase basePlugin)
                {
                    basePlugin.SetPhobosHandlers(handlers);
                }

                // 检查是否已在数据库中注册
                if (_database != null)
                {
                    var existing = await _database.ExecuteQuery(
                        "SELECT PackageName FROM Phobos_Plugin WHERE PackageName = @packageName",
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "@packageName", plugin.Metadata.PackageName }
                        });

                    if (existing?.Count == 0)
                    {
                        // 注册到数据库
                        await _database.ExecuteNonQuery(
                            @"INSERT INTO Phobos_Plugin (PackageName, Name, Manufacturer, Description, Version, Secret, Directory)
                              VALUES (@packageName, @name, @manufacturer, @description, @version, @secret, 'builtin')",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@packageName", plugin.Metadata.PackageName },
                                { "@name", plugin.Metadata.Name },
                                { "@manufacturer", plugin.Metadata.Manufacturer },
                                { "@description", plugin.Metadata.GetLocalizedDescription("en-US") },
                                { "@version", plugin.Metadata.Version },
                                { "@secret", plugin.Metadata.Secret }
                            });


                        // 调用 OnInstall (首次)
                        await plugin.OnInstall();
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
            string startupPlugin = "com.phobos.plugin.manager";

            if (_database != null)
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = 'StartupPlugin'");
                if (result?.Count > 0)
                {
                    startupPlugin = result[0]["Content"]?.ToString() ?? startupPlugin;
                }
            }

            // 启动插件管理器
            var pluginManager = PMPlugin.Instance;
            await pluginManager.Launch(startupPlugin);

            // 获取插件实例并创建窗口
            var plugin = pluginManager.GetPlugin(startupPlugin);
            if (plugin != null)
            {
                var windowManager = PMWindow.Instance;
                var window = windowManager.CreatePluginWindow(plugin);

                // 设置为主窗口
                MainWindow = window;
                windowManager.ShowWindow(window);

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
            _database?.Dispose();

            PCLoggerPlugin.Info("Phobos", "Application exiting");

            base.OnExit(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Phobos_Awake(sender, e);
        }
    }
}