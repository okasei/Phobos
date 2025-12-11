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
        private PCSqliteDatabase? _database;
        private string _appDataPath = string.Empty;
        private string _pluginsPath = string.Empty;
        private string _databasePath = string.Empty;
        private readonly PluginResourceManager _resourceManager = new();

        #region i18n Error Messages

        private static readonly Dictionary<string, Dictionary<string, string>> _errorMessages = new()
        {
            ["error.title"] = new() {
                { "en-US", "Error - Phobos" },
                { "zh-CN", "错误 - Phobos" },
                { "zh-TW", "錯誤 - Phobos" },
                { "ja-JP", "エラー - Phobos" },
                { "ko-KR", "오류 - Phobos" }
            },
            ["error.fatal.title"] = new() {
                { "en-US", "Fatal Error" },
                { "zh-CN", "致命错误" },
                { "zh-TW", "致命錯誤" },
                { "ja-JP", "致命的エラー" },
                { "ko-KR", "치명적 오류" }
            },
            ["error.startup.failed"] = new() {
                { "en-US", "Failed to start Phobos" },
                { "zh-CN", "Phobos 启动失败" },
                { "zh-TW", "Phobos 啟動失敗" },
                { "ja-JP", "Phobos の起動に失敗しました" },
                { "ko-KR", "Phobos 시작 실패" }
            },
            ["error.paths.failed"] = new() {
                { "en-US", "Failed to initialize application paths" },
                { "zh-CN", "初始化应用程序路径失败" },
                { "zh-TW", "初始化應用程式路徑失敗" },
                { "ja-JP", "アプリケーションパスの初期化に失敗しました" },
                { "ko-KR", "애플리케이션 경로 초기화 실패" }
            },
            ["error.database.failed"] = new() {
                { "en-US", "Failed to connect to database" },
                { "zh-CN", "数据库连接失败" },
                { "zh-TW", "資料庫連接失敗" },
                { "ja-JP", "データベース接続に失敗しました" },
                { "ko-KR", "데이터베이스 연결 실패" }
            },
            ["error.database.init.failed"] = new() {
                { "en-US", "Failed to initialize database" },
                { "zh-CN", "初始化数据库失败" },
                { "zh-TW", "初始化資料庫失敗" },
                { "ja-JP", "データベースの初期化に失敗しました" },
                { "ko-KR", "데이터베이스 초기화 실패" }
            },
            ["error.theme.failed"] = new() {
                { "en-US", "Failed to load theme" },
                { "zh-CN", "加载主题失败" },
                { "zh-TW", "載入主題失敗" },
                { "ja-JP", "テーマの読み込みに失敗しました" },
                { "ko-KR", "테마 로드 실패" }
            },
            ["error.plugin.manager.failed"] = new() {
                { "en-US", "Failed to initialize plugin manager" },
                { "zh-CN", "初始化插件管理器失败" },
                { "zh-TW", "初始化插件管理器失敗" },
                { "ja-JP", "プラグインマネージャーの初期化に失敗しました" },
                { "ko-KR", "플러그인 관리자 초기화 실패" }
            },
            ["error.plugin.register.failed"] = new() {
                { "en-US", "Failed to register built-in plugins" },
                { "zh-CN", "注册内置插件失败" },
                { "zh-TW", "註冊內建插件失敗" },
                { "ja-JP", "組み込みプラグインの登録に失敗しました" },
                { "ko-KR", "내장 플러그인 등록 실패" }
            },
            ["error.plugin.launch.failed"] = new() {
                { "en-US", "Failed to launch default plugin" },
                { "zh-CN", "启动默认插件失败" },
                { "zh-TW", "啟動預設插件失敗" },
                { "ja-JP", "デフォルトプラグインの起動に失敗しました" },
                { "ko-KR", "기본 플러그인 실행 실패" }
            },
            ["error.boot.failed"] = new() {
                { "en-US", "Failed to execute boot items" },
                { "zh-CN", "执行启动项失败" },
                { "zh-TW", "執行啟動項失敗" },
                { "ja-JP", "起動項目の実行に失敗しました" },
                { "ko-KR", "부팅 항목 실행 실패" }
            },
            ["error.details"] = new() {
                { "en-US", "Error details:" },
                { "zh-CN", "错误详情：" },
                { "zh-TW", "錯誤詳情：" },
                { "ja-JP", "エラー詳細：" },
                { "ko-KR", "오류 세부정보:" }
            },
            ["button.ok"] = new() {
                { "en-US", "OK" },
                { "zh-CN", "确定" },
                { "zh-TW", "確定" },
                { "ja-JP", "OK" },
                { "ko-KR", "확인" }
            },
            ["button.exit"] = new() {
                { "en-US", "Exit" },
                { "zh-CN", "退出" },
                { "zh-TW", "退出" },
                { "ja-JP", "終了" },
                { "ko-KR", "종료" }
            }
        };

        private static string GetErrorMessage(string key)
        {
            var lang = CultureInfo.CurrentCulture.IetfLanguageTag;
            if (_errorMessages.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var str)) return str;
                // Try base language (e.g., "zh" from "zh-Hans")
                var baseLang = lang.Contains("-") ? lang.Split('-')[0] : lang;
                foreach (var kvp in dict)
                {
                    if (kvp.Key.StartsWith(baseLang)) return kvp.Value;
                }
                if (dict.TryGetValue("en-US", out var enStr)) return enStr;
            }
            return key;
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
                    await InitializeTheme();
                    await PMTheme.Instance.Initialize();
                    await PMTheme.Instance.LoadTheme("com.phobos.theme.dark-orange");
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
                ("Language", "en-US")
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

            // 确保 StartupPlugin 总是设置为 Desktop（覆盖旧值）
            await _database.ExecuteNonQuery(
                "INSERT OR REPLACE INTO Phobos_Main (Key, Content, UpdateTime) VALUES ('StartupPlugin', 'com.phobos.desktop', datetime('now'))");
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

            // 注册桌面本地化资源
            DesktopLocalization.RegisterAll();

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
                new PCPluginManager(),
                new PCLoggerPlugin(),
                new PCSequencerPlugin(),
                new PCDialogPlugin(),
                new PCPluginInstaller(),
                new PCDesktopPlugin(),
                new PCRunnerPlugin()
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
                        var protocol = association.Protocol.ToLowerInvariant();
                        var packageName = caller.PackageName;

                        // 查询是否已存在该包名+协议的组合
                        var existing = await _database.ExecuteQuery(
                            @"SELECT p.UUID, p.AssociatedItem
                              FROM Phobos_Protocol p
                              INNER JOIN Phobos_AssociatedItem a ON p.AssociatedItem = a.Name
                              WHERE p.Protocol = @protocol AND a.PackageName = @packageName",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@protocol", protocol },
                                { "@packageName", packageName }
                            });

                        if (existing?.Count > 0)
                        {
                            // 已存在：更新记录，保存旧值到 LastValue
                            var existingUuid = existing[0]["UUID"]?.ToString() ?? "";
                            var oldAssociatedItem = existing[0]["AssociatedItem"]?.ToString() ?? "";

                            // 更新已有的 AssociatedItem（通过旧 Name 找到它）
                            await _database.ExecuteNonQuery(
                                @"UPDATE Phobos_AssociatedItem
                                  SET Name = @newName, Description = @description, Command = @command
                                  WHERE Name = @oldName AND PackageName = @packageName",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@newName", association.Name },
                                    { "@oldName", oldAssociatedItem },
                                    { "@packageName", packageName },
                                    { "@description", Shared.Class.TextEscaper.Escape(association.Description) },
                                    { "@command", association.Command }
                                });

                            // 更新 Protocol 记录，同时更新 AssociatedItem 引用和 LastValue
                            await _database.ExecuteNonQuery(
                                @"UPDATE Phobos_Protocol
                                  SET AssociatedItem = @associatedItem,
                                      UpdateUID = @uid,
                                      UpdateTime = datetime('now'),
                                      LastValue = @lastValue
                                  WHERE UUID = @uuid",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@uuid", existingUuid },
                                    { "@associatedItem", association.Name },
                                    { "@uid", packageName },
                                    { "@lastValue", oldAssociatedItem }
                                });
                        }
                        else
                        {
                            // 不存在：插入新记录
                            await _database.ExecuteNonQuery(
                                @"INSERT OR REPLACE INTO Phobos_AssociatedItem (Name, PackageName, Description, Command)
                                  VALUES (@name, @packageName, @description, @command)",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@name", association.Name },
                                    { "@packageName", packageName },
                                    { "@description", Shared.Class.TextEscaper.Escape(association.Description) },
                                    { "@command", association.Command }
                                });

                            await _database.ExecuteNonQuery(
                                @"INSERT INTO Phobos_Protocol (UUID, Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                                  VALUES (@uuid, @protocol, @associatedItem, @uid, datetime('now'), '')",
                                new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "@uuid", Guid.NewGuid().ToString("N") },
                                    { "@protocol", protocol },
                                    { "@associatedItem", association.Name },
                                    { "@uid", packageName }
                                });
                        }
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
                GetBootItems = (caller) => Task.FromResult(new System.Collections.Generic.List<object>()),
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
                }

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
                if (_database != null)
                {
                    var existing = await _database.ExecuteQuery(
                        "SELECT PackageName FROM Phobos_Plugin WHERE PackageName = @packageName",
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "@packageName", plugin.Metadata.PackageName }
                        });
                    var uninstallInfoJson = plugin.Metadata.UninstallInfo != null ? Newtonsoft.Json.JsonConvert.SerializeObject(plugin.Metadata.UninstallInfo) : string.Empty;
                    if (existing?.Count == 0)
                    {
                        // 注册到数据库
                        await _database.ExecuteNonQuery(
                            @"INSERT INTO Phobos_Plugin (PackageName, Name, Manufacturer, Description, Version, Secret, Directory,
                                Icon, IsSystemPlugin, SettingUri, UninstallInfo, IsEnabled, UpdateTime, Entry, LaunchFlag)
                              VALUES (@packageName, @name, @manufacturer, @description, @version, @secret, 'builtin',
                                @icon, @isSystemPlugin, @settingUri, @uninstallInfo, 1, datetime('now'), @entry, @launchFlag)",
                            new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "@packageName", plugin.Metadata.PackageName },
                                { "@name", plugin.Metadata.Name },
                                { "@manufacturer", plugin.Metadata.Manufacturer },
                                { "@description", plugin.Metadata.GetLocalizedDescription("en-US") },
                                { "@version", plugin.Metadata.Version },
                                { "@secret", plugin.Metadata.Secret },
                                { "@icon", plugin.Metadata.Icon ?? string.Empty },
                                { "@isSystemPlugin", plugin.Metadata.IsSystemPlugin ? 1 : 0 },
                                { "@settingUri", plugin.Metadata.SettingUri ?? string.Empty },
                                { "@uninstallInfo", uninstallInfoJson },
                                { "@entry", plugin.Metadata.Entry ?? string.Empty },
                                { "@launchFlag", plugin.Metadata.LaunchFlag == true ? 1 : 0 }
                            });
                    }
                    else
                    {
                        // 更新现有记录
                        await _database.ExecuteNonQuery(
                            @"UPDATE Phobos_Plugin SET
                                Name = @name, Manufacturer = @manufacturer, Description = @description,
                                Version = @version, Icon = @icon, IsSystemPlugin = @isSystemPlugin,
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
                                { "@icon", plugin.Metadata.Icon ?? string.Empty },
                                { "@isSystemPlugin", plugin.Metadata.IsSystemPlugin ? 1 : 0 },
                                { "@settingUri", plugin.Metadata.SettingUri ?? string.Empty },
                                { "@uninstallInfo", uninstallInfoJson },
                                { "@entry", plugin.Metadata.Entry ?? string.Empty },
                                { "@launchFlag", plugin.Metadata.LaunchFlag == true ? 1 : 0 }
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
            string startupPlugin = "com.phobos.desktop";

            if (_database != null)
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = 'StartupPlugin'");
                if (result?.Count > 0)
                {
                    startupPlugin = result[0]["Content"]?.ToString() ?? startupPlugin;
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