using Phobos.Class.Config;
using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Interface.Plugin;
using Phobos.Manager.Arcusrix;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Utils.General;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Phobos.Manager.Plugin
{
    /// <summary>
    /// 插件程序集加载上下文
    /// </summary>
    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
    }

    /// <summary>
    /// 插件管理器 i18n 错误消息
    /// </summary>
    internal static class PMPluginMessages
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _messages = new()
        {
            ["error.secret_mismatch"] = new()
            {
                { "en-US", "Plugin update failed: Secret key mismatch. This may indicate a different publisher or tampering." },
                { "zh-CN", "插件更新失败：密钥不匹配。这可能表示发布者不同或文件被篡改。" },
                { "zh-TW", "插件更新失敗：密鑰不匹配。這可能表示發布者不同或檔案被竄改。" },
                { "ja-JP", "プラグインの更新に失敗しました：秘密鍵が一致しません。発行者が異なるか、改ざんされている可能性があります。" },
                { "ko-KR", "플러그인 업데이트 실패: 시크릿 키 불일치. 다른 게시자이거나 변조된 것일 수 있습니다." }
            },
            ["error.plugin_not_found"] = new()
            {
                { "en-US", "Plugin not found" },
                { "zh-CN", "插件未找到" },
                { "zh-TW", "插件未找到" },
                { "ja-JP", "プラグインが見つかりません" },
                { "ko-KR", "플러그인을 찾을 수 없습니다" }
            },
            ["error.file_not_found"] = new()
            {
                { "en-US", "Plugin file not found" },
                { "zh-CN", "插件文件未找到" },
                { "zh-TW", "插件檔案未找到" },
                { "ja-JP", "プラグインファイルが見つかりません" },
                { "ko-KR", "플러그인 파일을 찾을 수 없습니다" }
            },
            ["error.invalid_plugin"] = new()
            {
                { "en-US", "No valid plugin type found in assembly" },
                { "zh-CN", "程序集中未找到有效的插件类型" },
                { "zh-TW", "組件中未找到有效的插件類型" },
                { "ja-JP", "アセンブリに有効なプラグインタイプが見つかりません" },
                { "ko-KR", "어셈블리에서 유효한 플러그인 유형을 찾을 수 없습니다" }
            },
            ["error.create_instance_failed"] = new()
            {
                { "en-US", "Failed to create plugin instance" },
                { "zh-CN", "创建插件实例失败" },
                { "zh-TW", "創建插件實例失敗" },
                { "ja-JP", "プラグインインスタンスの作成に失敗しました" },
                { "ko-KR", "플러그인 인스턴스 생성 실패" }
            },
            ["error.already_installed"] = new()
            {
                { "en-US", "Plugin already installed" },
                { "zh-CN", "插件已安装" },
                { "zh-TW", "插件已安裝" },
                { "ja-JP", "プラグインは既にインストールされています" },
                { "ko-KR", "플러그인이 이미 설치되어 있습니다" }
            }
        };

        public static string Get(string key)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            if (_messages.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var str)) return str;
                // Try base language
                var baseLang = lang.Contains("-") ? lang.Split('-')[0] : lang;
                foreach (var kvp in dict)
                {
                    if (kvp.Key.StartsWith(baseLang)) return kvp.Value;
                }
                if (dict.TryGetValue("en-US", out var enStr)) return enStr;
            }
            return key;
        }
    }

    /// <summary>
    /// 插件管理器实现
    /// </summary>
    public class PMPlugin : PIPluginManager
    {
        private static PMPlugin? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, PluginLoadContext> _loadedPlugins = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PluginAssemblyLoadContext> _assemblyContexts = new(StringComparer.OrdinalIgnoreCase);
        private PCSqliteDatabase? _database;
        private string _pluginsDirectory = string.Empty;

        // 记录 Phobos_Shell 的上次检查时间，用于检测 Protocol 更新
        private readonly Dictionary<string, DateTime> _shellLastCheckTime = new(StringComparer.OrdinalIgnoreCase);

        public static PMPlugin Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMPlugin();
                    }
                }
                return _instance;
            }
        }

        public IReadOnlyDictionary<string, PluginLoadContext> LoadedPlugins => _loadedPlugins;

        /// <summary>
        /// 初始化插件管理器
        /// </summary>
        public async Task Initialize(PCSqliteDatabase database, string pluginsDirectory)
        {
            _database = database;
            _pluginsDirectory = pluginsDirectory;

            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
            }

            // 加载所有已安装的插件
            await LoadInstalledPlugins();
        }

        private async Task LoadInstalledPlugins()
        {
            if (_database == null)
                return;

            var plugins = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin WHERE IsEnabled = 1");
            foreach (var plugin in plugins)
            {
                var packageName = plugin["PackageName"]?.ToString() ?? string.Empty;
                var directory = plugin["Directory"]?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(packageName) && !string.IsNullOrEmpty(directory))
                {
                    try
                    {
                        await Load(packageName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to load plugin {packageName}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 创建插件处理器
        /// </summary>
        private PluginHandlers CreatePluginHandlers()
        {
            return new PluginHandlers
            {
                RequestPhobos = HandleRequestPhobos,
                Link = HandleLink,
                Request = HandleRequest,
                LinkDefault = HandleLinkDefault,
                ReadConfig = HandleReadConfig,
                WriteConfig = HandleWriteConfig,
                ReadSysConfig = HandleReadSysConfig,
                WriteSysConfig = HandleWriteSysConfig,
                BootWithPhobos = HandleBootWithPhobos,
                RemoveBootWithPhobos = HandleRemoveBootWithPhobos,
                GetBootItems = HandleGetBootItems,
                Log = HandleLog,
                Subscribe = HandleSubscribe,
                Unsubscribe = HandleUnsubscribe,
                TriggerEvent = HandleTriggerEvent,
                GetMergedDictionaries = (ctx) =>
                {
                    var currentTheme = PMTheme.Instance.CurrentTheme;
                    if (currentTheme != null)
                    {
                        return currentTheme.GetGlobalStyles();
                    }
                    return App.Current?.Resources; // 回退
                },
                SetDefaultHandler = HandleSetDefaultHandler,
                GetDefaultHandler = HandleGetDefaultHandler,
                SendNotification = HandleSendNotification,
                SendNotificationObject = HandleSendNotificationObject,
                // 插件间通信
                RequestPlugin = HandleRequestPlugin,
                RequestProtocolHandler = HandleRequestProtocolHandler,
            };
        }

        /// <summary>
        /// 处理订阅请求
        /// </summary>
        private async Task<RequestResult> HandleSubscribe(
            PluginCallerContext caller,
            string eventId,
            string eventName,
            object[] args)
        {
            return PMEvent.Instance.Subscribe(caller.PackageName, eventId, eventName);
        }

        /// <summary>
        /// 处理取消订阅请求
        /// </summary>
        private async Task<RequestResult> HandleUnsubscribe(
            PluginCallerContext caller,
            string eventId,
            string eventName,
            object[] args)
        {
            return PMEvent.Instance.Unsubscribe(caller.PackageName, eventId, eventName);
        }

        /// <summary>
        /// 处理事件触发请求
        /// </summary>
        private async Task<RequestResult> HandleTriggerEvent(
            PluginCallerContext caller,
            string eventId,
            string eventName,
            object[] args)
        {
            try
            {
                await PMEvent.Instance.TriggerFromPluginAsync(caller.PackageName, eventId, eventName, args);
                return new RequestResult
                {
                    Success = true,
                    Message = $"Event {eventId}.{eventName} triggered successfully"
                };
            }
            catch (Exception ex)
            {
                return new RequestResult
                {
                    Success = false,
                    Message = $"Failed to trigger event: {ex.Message}",
                    Error = ex
                };
            }
        }


        /// <summary>
        /// Register a built-in plugin instance so it is available via GetPlugin/Launch
        /// </summary>
        /// <param name="plugin">插件实例</param>
        public void RegisterBuiltInPlugin(IPhobosPlugin plugin)
        {
            if (plugin == null) return;

            var packageName = plugin.Metadata.PackageName;
            if (string.IsNullOrEmpty(packageName)) return;

            if (_loadedPlugins.ContainsKey(packageName)) return;

            if (plugin is PCPluginBase basePlugin)
            {
                basePlugin.SetPhobosHandlers(CreatePluginHandlers());
            }

            _loadedPlugins[packageName] = new PluginLoadContext
            {
                PackageName = packageName,
                PluginPath = string.Empty,
                Instance = plugin,
                State = PluginState.Loaded,
                LoadTime = DateTime.Now
            };
        }

        public async Task<RequestResult> Install(string pluginPath, PluginInstallOptions? options = null)
        {
            options ??= new PluginInstallOptions();

            try
            {
                if (!File.Exists(pluginPath))
                {
                    return new RequestResult { Success = false, Message = "Plugin file not found" };
                }

                var tempContext = new PluginAssemblyLoadContext(pluginPath);
                var assembly = tempContext.LoadFromAssemblyPath(pluginPath);
                var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(IPhobosPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null)
                {
                    tempContext.Unload();
                    return new RequestResult { Success = false, Message = "No valid plugin type found in assembly" };
                }

                var pluginInstance = Activator.CreateInstance(pluginType) as IPhobosPlugin;
                if (pluginInstance == null)
                {
                    tempContext.Unload();
                    return new RequestResult { Success = false, Message = "Failed to create plugin instance" };
                }

                var metadata = pluginInstance.Metadata;

                var existing = await _database?.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", metadata.PackageName } });

                if (existing?.Count > 0)
                {
                    // 验证 Secret Key 是否一致
                    var existingSecret = existing[0]["Secret"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(existingSecret) && existingSecret != metadata.Secret)
                    {
                        tempContext.Unload();
                        PCLoggerPlugin.Warning("PMPlugin", $"Secret key mismatch for plugin {metadata.PackageName}");
                        return new RequestResult
                        {
                            Success = false,
                            Message = PMPluginMessages.Get("error.secret_mismatch")
                        };
                    }

                    if (!options.ForceReinstall)
                    {
                        tempContext.Unload();
                        return new RequestResult { Success = false, Message = PMPluginMessages.Get("error.already_installed") };
                    }
                }

                if (!options.IgnoreDependencies)
                {
                    var depResult = await CheckDependencies(metadata);
                    if (!depResult.Success)
                    {
                        tempContext.Unload();
                        return depResult;
                    }
                }

                tempContext.Unload();

                var pluginDir = Path.Combine(_pluginsDirectory, metadata.PackageName);
                if (Directory.Exists(pluginDir))
                {
                    Directory.Delete(pluginDir, true);
                }
                Directory.CreateDirectory(pluginDir);

                var destPath = Path.Combine(pluginDir, Path.GetFileName(pluginPath));
                File.Copy(pluginPath, destPath, true);

                var sourceDir = Path.GetDirectoryName(pluginPath);
                if (!string.IsNullOrEmpty(sourceDir))
                {
                    foreach (var dllFile in Directory.GetFiles(sourceDir, "*.dll"))
                    {
                        var dllDest = Path.Combine(pluginDir, Path.GetFileName(dllFile));
                        if (!File.Exists(dllDest))
                        {
                            File.Copy(dllFile, dllDest);
                        }
                    }
                }

                if (_database != null)
                {
                    var uninstallInfoJson = metadata.UninstallInfo != null
                        ? Newtonsoft.Json.JsonConvert.SerializeObject(metadata.UninstallInfo)
                        : string.Empty;

                    if (existing?.Count > 0)
                    {
                        await _database.ExecuteNonQuery(
                            @"UPDATE Phobos_Plugin SET
                                Name = @name, Manufacturer = @manufacturer, Description = @description,
                                Version = @version, Secret = @secret, Directory = @directory,
                                Icon = @icon, IsSystemPlugin = @isSystemPlugin, SettingUri = @settingUri,
                                UninstallInfo = @uninstallInfo, Entry = @entry, LaunchFlag = @launchFlag, UpdateTime = datetime('now')
                              WHERE PackageName = @packageName COLLATE NOCASE",
                            new Dictionary<string, object>
                            {
                                { "@packageName", metadata.PackageName },
                                { "@name", TextEscaper.Escape(metadata.Name) },
                                { "@manufacturer", TextEscaper.Escape(metadata.Manufacturer) },
                                { "@description", TextEscaper.Escape(metadata.GetLocalizedDescription("en-US")) },
                                { "@version", metadata.Version },
                                { "@secret", metadata.Secret },
                                { "@directory", pluginDir },
                                { "@icon", metadata.Icon ?? string.Empty },
                                { "@isSystemPlugin", metadata.IsSystemPlugin ? 1 : 0 },
                                { "@settingUri", metadata.SettingUri ?? string.Empty },
                                { "@uninstallInfo", uninstallInfoJson },
                                { "@entry", metadata.Entry ?? string.Empty },
                                { "@launchFlag", metadata.LaunchFlag == true ? 1 : 0 }
                            });
                    }
                    else
                    {
                        await _database.ExecuteNonQuery(
                            @"INSERT INTO Phobos_Plugin (PackageName, Name, Manufacturer, Description, Version, Secret, Directory,
                                Icon, IsSystemPlugin, SettingUri, UninstallInfo, IsEnabled, UpdateTime, Entry, LaunchFlag)
                              VALUES (@packageName, @name, @manufacturer, @description, @version, @secret, @directory,
                                @icon, @isSystemPlugin, @settingUri, @uninstallInfo, 1, datetime('now'), @entry, @launchFlag)",
                            new Dictionary<string, object>
                            {
                                { "@packageName", metadata.PackageName },
                                { "@name", TextEscaper.Escape(metadata.Name) },
                                { "@manufacturer", TextEscaper.Escape(metadata.Manufacturer) },
                                { "@description", TextEscaper.Escape(metadata.GetLocalizedDescription("en-US")) },
                                { "@version", metadata.Version },
                                { "@secret", metadata.Secret },
                                { "@directory", pluginDir },
                                { "@icon", metadata.Icon ?? string.Empty },
                                { "@isSystemPlugin", metadata.IsSystemPlugin ? 1 : 0 },
                                { "@settingUri", metadata.SettingUri ?? string.Empty },
                                { "@uninstallInfo", uninstallInfoJson },
                                { "@entry", metadata.Entry ?? string.Empty },
                                { "@launchFlag", metadata.LaunchFlag == true ? 1 : 0 }
                            });
                    }
                }

                var loadResult = await Load(metadata.PackageName);
                if (loadResult.Success)
                {
                    var plugin = GetPlugin(metadata.PackageName);
                    if (plugin != null)
                    {
                        try
                        {
                            await plugin.OnInstall();
                        }
                        catch (Exception pluginEx)
                        {
                            // 插件安装回调失败不应阻止安装流程
                            PCLoggerPlugin.Warning("PMPlugin", $"Plugin OnInstall callback failed for {metadata.PackageName}: {pluginEx.Message}");
                        }
                    }
                }

                return new RequestResult { Success = true, Message = "Plugin installed successfully", Data = [metadata.PackageName, metadata.Name] };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Uninstall(string packageName, bool force = false)
        {
            try
            {
                // 获取插件信息
                var pluginInfo = await _database?.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (pluginInfo == null || pluginInfo.Count == 0)
                {
                    return new RequestResult { Success = false, Message = "Plugin not found" };
                }

                var isSystemPlugin = Convert.ToInt32(pluginInfo[0]["IsSystemPlugin"] ?? 0) == 1;
                var uninstallInfoJson = pluginInfo[0]["UninstallInfo"]?.ToString() ?? string.Empty;

                // 检查是否为系统插件
                if (isSystemPlugin && !force)
                {
                    PluginUninstallInfo? uninstallInfo = null;
                    if (!string.IsNullOrEmpty(uninstallInfoJson))
                    {
                        try
                        {
                            uninstallInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginUninstallInfo>(uninstallInfoJson);
                        }
                        catch { }
                    }

                    if (uninstallInfo != null && !uninstallInfo.AllowUninstall)
                    {
                        return new RequestResult
                        {
                            Success = false,
                            Message = uninstallInfo.GetLocalizedMessage(PCSysConfig.Instance.langCode)
                        };
                    }
                }

                var plugin = GetPlugin(packageName);
                if (plugin != null)
                {
                    try
                    {
                        await plugin.OnUninstall();
                    }
                    catch (Exception pluginEx)
                    {
                        // 插件卸载回调失败不应阻止卸载流程
                        PCLoggerPlugin.Warning("PMPlugin", $"Plugin OnUninstall callback failed for {packageName}: {pluginEx.Message}");
                    }
                }

                await Unload(packageName);

                if (_database != null)
                {
                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Appdata WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_AssociatedItem WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Protocol WHERE UpdateUID = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Boot WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    var directory = pluginInfo[0]["Directory"]?.ToString();

                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }

                return new RequestResult { Success = true, Message = "Plugin uninstalled successfully" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Load(string packageName)
        {
            try
            {
                if (_loadedPlugins.ContainsKey(packageName))
                {
                    return new RequestResult { Success = true, Message = "Plugin already loaded" };
                }

                if (_database == null)
                {
                    return new RequestResult { Success = false, Message = "Database not initialized" };
                }

                var pluginInfo = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (pluginInfo == null || pluginInfo.Count == 0)
                {
                    return new RequestResult { Success = false, Message = "Plugin not found in database" };
                }

                // 检查是否启用
                var isEnabled = Convert.ToInt32(pluginInfo[0]["IsEnabled"] ?? 1) == 1;
                if (!isEnabled)
                {
                    return new RequestResult { Success = false, Message = "Plugin is disabled" };
                }

                var directory = pluginInfo[0]["Directory"]?.ToString() ?? string.Empty;

                if (directory == "builtin")
                {
                    return new RequestResult { Success = true, Message = "Built-in Plugin" };
                }

                var dllFiles = Directory.GetFiles(directory, "*.dll");
                var mainDll = dllFiles.FirstOrDefault(f => !f.Contains("deps", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(mainDll))
                {
                    return new RequestResult { Success = false, Message = "Plugin DLL not found" };
                }

                var context = new PluginAssemblyLoadContext(mainDll);
                var assembly = context.LoadFromAssemblyPath(mainDll);
                var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(IPhobosPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null)
                {
                    context.Unload();
                    return new RequestResult { Success = false, Message = "No valid plugin type found" };
                }

                var instance = Activator.CreateInstance(pluginType) as IPhobosPlugin;
                if (instance == null)
                {
                    context.Unload();
                    return new RequestResult { Success = false, Message = "Failed to create plugin instance" };
                }

                if (instance is PCPluginBase basePlugin)
                {
                    basePlugin.SetPhobosHandlers(CreatePluginHandlers());
                }

                _assemblyContexts[packageName] = context;
                _loadedPlugins[packageName] = new PluginLoadContext
                {
                    PackageName = packageName,
                    PluginPath = mainDll,
                    Instance = instance,
                    State = PluginState.Loaded,
                    LoadTime = DateTime.Now
                };

                return new RequestResult { Success = true, Message = "Plugin loaded successfully" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Unload(string packageName)
        {
            try
            {
                if (!_loadedPlugins.TryGetValue(packageName, out var context))
                {
                    return new RequestResult { Success = true, Message = "Plugin not loaded" };
                }

                if (context.Instance != null)
                {
                    await context.Instance.OnClosing();
                }

                _loadedPlugins.Remove(packageName);

                if (_assemblyContexts.TryGetValue(packageName, out var assemblyContext))
                {
                    assemblyContext.Unload();
                    _assemblyContexts.Remove(packageName);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                return new RequestResult { Success = true, Message = "Plugin unloaded successfully" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Launch(string packageName, params object[] args)
        {
            try
            {
                if (!_loadedPlugins.TryGetValue(packageName, out var context))
                {
                    var loadResult = await Load(packageName);
                    if (!loadResult.Success)
                        return loadResult;

                    context = _loadedPlugins[packageName];
                }

                if (context.Instance == null)
                {
                    return new RequestResult { Success = false, Message = "Plugin instance not found" };
                }

                // 调用 OnLaunch 让插件初始化
                var result = await context.Instance.OnLaunch(args);
                if (!result.Success)
                {
                    return result;
                }

                // 如果插件有 ContentArea，则使用 WindowManager 载入窗口
                if (context.Instance.ContentArea != null)
                {
                    var windowManager = PMWindow.Instance;
                    var pluginWindow = windowManager.CreatePluginWindow(context.Instance, context.Instance.Metadata.GetLocalizedName(PCSysConfig.Instance.langCode));
                    windowManager.ShowWindow(pluginWindow);
                }
                // 否则，插件自行设置自定义窗口启动（在 OnLaunch 中处理）

                context.State = PluginState.Running;
                return result;
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 调用插件的 Run 方法
        /// </summary>
        /// <param name="packageName">插件包名</param>
        /// <param name="args">参数（第一个通常是命令名）</param>
        public async Task<RequestResult> Run(string packageName, params object[] args)
        {
            try
            {
                if (!_loadedPlugins.TryGetValue(packageName, out var context))
                {
                    var loadResult = await Load(packageName);
                    if (!loadResult.Success)
                        return loadResult;

                    context = _loadedPlugins[packageName];
                }

                if (context.Instance == null)
                {
                    return new RequestResult { Success = false, Message = "Plugin instance not found" };
                }

                var result = await context.Instance.Run(args);
                if (result.Success)
                {
                    context.State = PluginState.Running;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Stop(string packageName)
        {
            try
            {
                if (!_loadedPlugins.TryGetValue(packageName, out var context))
                {
                    return new RequestResult { Success = false, Message = "Plugin not loaded" };
                }

                if (context.Instance != null)
                {
                    await context.Instance.OnClosing();
                }

                context.State = PluginState.Loaded;
                return new RequestResult { Success = true, Message = "Plugin stopped" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Update(string packageName, string newPluginPath)
        {
            try
            {
                var plugin = GetPlugin(packageName);
                var oldVersion = plugin?.Metadata.Version ?? "0.0.0";

                await Unload(packageName);

                var installResult = await Install(newPluginPath, new PluginInstallOptions { ForceReinstall = true });
                if (!installResult.Success)
                    return installResult;

                plugin = GetPlugin(packageName);
                if (plugin != null)
                {
                    await plugin.OnUpdate(oldVersion, plugin.Metadata.Version);
                }

                return new RequestResult { Success = true, Message = "Plugin updated successfully", Data = [plugin?.Metadata.PackageName ?? string.Empty, plugin?.Metadata.Name ?? string.Empty] };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 启用/禁用插件
        /// </summary>
        public async Task<RequestResult> SetPluginEnabled(string packageName, bool enabled)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                await _database.ExecuteNonQuery(
                    "UPDATE Phobos_Plugin SET IsEnabled = @enabled, UpdateTime = datetime('now') WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object>
                    {
                        { "@packageName", packageName },
                        { "@enabled", enabled ? 1 : 0 }
                    });

                if (!enabled && _loadedPlugins.ContainsKey(packageName))
                {
                    await Unload(packageName);
                }
                else if (enabled && !_loadedPlugins.ContainsKey(packageName))
                {
                    await Load(packageName);
                }

                return new RequestResult { Success = true, Message = enabled ? "Plugin enabled" : "Plugin disabled" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 检查插件是否为系统插件
        /// </summary>
        public async Task<bool> IsSystemPlugin(string packageName)
        {
            if (_database == null) return false;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT IsSystemPlugin FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    return Convert.ToInt32(result[0]["IsSystemPlugin"] ?? 0) == 1;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 获取插件的设置 URI
        /// </summary>
        public async Task<string?> GetSettingUri(string packageName)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT SettingUri FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    var uri = result[0]["SettingUri"]?.ToString();
                    return string.IsNullOrEmpty(uri) ? null : uri;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取插件图标路径
        /// </summary>
        public async Task<string?> GetPluginIcon(string packageName)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Icon, Directory FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    var icon = result[0]["Icon"]?.ToString();
                    var directory = result[0]["Directory"]?.ToString();

                    if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(directory))
                    {
                        return Path.Combine(directory, icon);
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取插件卸载信息
        /// </summary>
        public async Task<PluginUninstallInfo?> GetUninstallInfo(string packageName)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT UninstallInfo FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    var json = result[0]["UninstallInfo"]?.ToString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<PluginUninstallInfo>(json);
                    }
                }
            }
            catch { }

            return null;
        }

        public IPhobosPlugin? GetPlugin(string packageName)
        {
            if (_loadedPlugins.TryGetValue(packageName, out var context))
                return context.Instance;
            return null;
        }

        public PluginState GetPluginState(string packageName)
        {
            if (_loadedPlugins.TryGetValue(packageName, out var context))
                return context.State;
            return PluginState.NotInstalled;
        }

        public async Task<List<PluginMetadata>> GetInstalledPlugins()
        {
            var result = new List<PluginMetadata>();

            if (_database == null)
                return result;

            var plugins = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin");
            foreach (var plugin in plugins)
            {
                var uninstallInfoJson = plugin["UninstallInfo"]?.ToString();
                PluginUninstallInfo? uninstallInfo = null;
                if (!string.IsNullOrEmpty(uninstallInfoJson))
                {
                    try
                    {
                        uninstallInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginUninstallInfo>(uninstallInfoJson);
                    }
                    catch { }
                }

                result.Add(new PluginMetadata
                {
                    PackageName = plugin["PackageName"]?.ToString() ?? string.Empty,
                    Name = TextEscaper.Unescape(plugin["Name"]?.ToString() ?? string.Empty),
                    Manufacturer = TextEscaper.Unescape(plugin["Manufacturer"]?.ToString() ?? string.Empty),
                    Version = plugin["Version"]?.ToString() ?? "1.0.0",
                    Secret = plugin["Secret"]?.ToString() ?? string.Empty,
                    Icon = plugin["Icon"]?.ToString(),
                    IsSystemPlugin = Convert.ToInt32(plugin["IsSystemPlugin"] ?? 0) == 1,
                    SettingUri = plugin["SettingUri"]?.ToString(),
                    UninstallInfo = uninstallInfo
                });
            }

            return result;
        }

        /// <summary>
        /// 获取所有用户插件（非系统插件）
        /// </summary>
        public async Task<List<PluginMetadata>> GetUserPlugins()
        {
            var all = await GetInstalledPlugins();
            return all.Where(p => !p.IsSystemPlugin).ToList();
        }

        /// <summary>
        /// 获取所有系统插件
        /// </summary>
        public async Task<List<PluginMetadata>> GetSystemPlugins()
        {
            var all = await GetInstalledPlugins();
            return all.Where(p => p.IsSystemPlugin).ToList();
        }

        public async Task<RequestResult> CheckDependencies(PluginMetadata metadata)
        {
            foreach (var dep in metadata.Dependencies)
            {
                var installed = await GetInstalledPlugins();
                var found = installed.FirstOrDefault(p =>
                    string.Equals(p.PackageName, dep.PackageName, StringComparison.OrdinalIgnoreCase));

                if (found == null)
                {
                    if (!dep.IsOptional)
                    {
                        return new RequestResult
                        {
                            Success = false,
                            Message = $"Required dependency not found: {dep.PackageName}"
                        };
                    }
                }
                else if (PUText.CompareVersion(found.Version, dep.MinVersion) < 0)
                {
                    return new RequestResult
                    {
                        Success = false,
                        Message = $"Dependency version too low: {dep.PackageName} requires {dep.MinVersion}, found {found.Version}"
                    };
                }
            }

            return new RequestResult { Success = true, Message = "All dependencies satisfied" };
        }

        public async Task<RequestResult> SendCommand(string packageName, string command, params object[] args)
        {
            var plugin = GetPlugin(packageName);
            if (plugin == null)
            {
                return new RequestResult { Success = false, Message = "Plugin not found" };
            }

            return await plugin.Run(command, args);
        }

        public async Task<RequestResult> PluginToPlugin(string sourcePackage, string targetPackage, string message, params object[] args)
        {
            var targetPlugin = GetPlugin(targetPackage);
            if (targetPlugin == null)
            {
                return new RequestResult { Success = false, Message = "Target plugin not found" };
            }

            return await targetPlugin.Run("PluginMessage", sourcePackage, message, args);
        }

        /// <summary>
        /// 执行所有启动项
        /// </summary>
        public async Task ExecuteBootItems()
        {
            if (_database == null) return;

            var bootItems = await _database.ExecuteQuery(
                "SELECT * FROM Phobos_Boot WHERE IsEnabled = 1 ORDER BY Priority ASC");

            foreach (var item in bootItems ?? new List<Dictionary<string, object>>())
            {
                var command = item["Command"]?.ToString() ?? string.Empty;
                var packageName = item["PackageName"]?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(command) && !string.IsNullOrEmpty(packageName))
                {
                    try
                    {
                        var plugin = GetPlugin(packageName);
                        if (plugin != null)
                        {
                            await plugin.Run("Boot", command);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Boot item failed: {packageName} - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 恢复上一个默认打开方式
        /// </summary>
        public async Task<RequestResult> RevertShellDefault(string protocol)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                var protocolLower = protocol.ToLowerInvariant();

                var existing = await _database.ExecuteQuery(
                    "SELECT LastValue FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                if (existing == null || existing.Count == 0)
                {
                    return new RequestResult { Success = false, Message = "No shell entry found for this protocol" };
                }

                var lastValue = existing[0]["LastValue"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(lastValue))
                {
                    return new RequestResult { Success = false, Message = "No previous value to revert to" };
                }

                await _database.ExecuteNonQuery(
                    @"UPDATE Phobos_Shell SET 
                        AssociatedItem = @lastValue, 
                        UpdateUID = 'Phobos.Reverse', 
                        UpdateTime = datetime('now'),
                        LastValue = ''
                      WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object>
                    {
                        { "@protocol", protocolLower },
                        { "@lastValue", lastValue }
                    });

                return new RequestResult { Success = true, Message = "Reverted to previous default" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取协议的所有打开方式选项（带更新检测）
        /// </summary>
        public async Task<(List<ProtocolHandlerOption> UpdatedOptions, List<ProtocolHandlerOption> OtherOptions, string? CurrentDefault)>
            GetProtocolHandlerOptions(string protocol)
        {
            var updatedOptions = new List<ProtocolHandlerOption>();
            var otherOptions = new List<ProtocolHandlerOption>();
            string? currentDefault = null;

            if (_database == null)
                return (updatedOptions, otherOptions, currentDefault);

            var protocolLower = protocol.ToLowerInvariant();

            var shellResult = await _database.ExecuteQuery(
                "SELECT AssociatedItem, UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                new Dictionary<string, object> { { "@protocol", protocolLower } });

            DateTime lastShellCheckTime = DateTime.MinValue;
            if (shellResult?.Count > 0)
            {
                currentDefault = shellResult[0]["AssociatedItem"]?.ToString();
                if (_shellLastCheckTime.TryGetValue(protocolLower, out var lastCheck))
                {
                    lastShellCheckTime = lastCheck;
                }
            }

            var handlers = await _database.ExecuteQuery(
                @"SELECT p.UUID, p.Protocol, p.AssociatedItem, p.UpdateTime, 
                         ai.PackageName, ai.Description, ai.Command
                  FROM Phobos_Protocol p
                  INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                  WHERE p.Protocol = @protocol COLLATE NOCASE
                  ORDER BY p.UpdateTime DESC",
                new Dictionary<string, object> { { "@protocol", protocolLower } });

            foreach (var handler in handlers ?? new List<Dictionary<string, object>>())
            {
                var updateTimeStr = handler["UpdateTime"]?.ToString() ?? string.Empty;
                DateTime.TryParse(updateTimeStr, out var updateTime);

                var option = new ProtocolHandlerOption
                {
                    UUID = handler["UUID"]?.ToString() ?? string.Empty,
                    Protocol = protocolLower,
                    AssociatedItem = handler["AssociatedItem"]?.ToString() ?? string.Empty,
                    PackageName = handler["PackageName"]?.ToString() ?? string.Empty,
                    Description = TextEscaper.Unescape(handler["Description"]?.ToString() ?? string.Empty),
                    Command = handler["Command"]?.ToString() ?? string.Empty,
                    UpdateTime = updateTime,
                    IsDefault = handler["AssociatedItem"]?.ToString() == currentDefault,
                    IsUpdated = updateTime > lastShellCheckTime
                };

                if (option.IsUpdated)
                {
                    updatedOptions.Add(option);
                }
                else
                {
                    otherOptions.Add(option);
                }
            }

            _shellLastCheckTime[protocolLower] = DateTime.Now;

            return (updatedOptions, otherOptions, currentDefault);
        }

        #region Handler Methods

        private Task<List<object>> HandleRequestPhobos(PluginCallerContext caller, object[] args)
        {
            var result = new List<object>();

            if (args.Length > 0 && args[0] is string request)
            {
                switch (request.ToLowerInvariant())
                {
                    case "username":
                        result.Add(Environment.UserName);
                        break;
                    case "machinename":
                        result.Add(Environment.MachineName);
                        break;
                    case "currentdirectory":
                        result.Add(Environment.CurrentDirectory);
                        break;
                    case "pluginsdirectory":
                        result.Add(_pluginsDirectory);
                        break;
                    case "caller":
                        result.Add(caller.PackageName);
                        result.Add(caller.DatabaseKey);
                        break;
                }
            }

            return Task.FromResult(result);
        }

        private async Task<RequestResult> HandleLink(PluginCallerContext caller, LinkAssociation association)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                var protocol = association.Protocol.ToLowerInvariant();
                var packageName = caller.PackageName;

                // 查询是否已存在该包名+协议的组合
                var existing = await _database.ExecuteQuery(
                    @"SELECT p.UUID, p.AssociatedItem
                      FROM Phobos_Protocol p
                      INNER JOIN Phobos_AssociatedItem a ON p.AssociatedItem = a.Name
                      WHERE p.Protocol = @protocol COLLATE NOCASE AND a.PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object>
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
                        new Dictionary<string, object>
                        {
                            { "@newName", association.Name },
                            { "@oldName", oldAssociatedItem },
                            { "@packageName", packageName },
                            { "@description", TextEscaper.Escape(association.Description) },
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
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
                        {
                            { "@name", association.Name },
                            { "@packageName", packageName },
                            { "@description", TextEscaper.Escape(association.Description) },
                            { "@command", association.Command }
                        });

                    await _database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Protocol (UUID, Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                          VALUES (@uuid, @protocol, @associatedItem, @uid, datetime('now'), '')",
                        new Dictionary<string, object>
                        {
                            { "@uuid", Guid.NewGuid().ToString("N") },
                            { "@protocol", protocol },
                            { "@associatedItem", association.Name },
                            { "@uid", packageName }
                        });
                }

                return new RequestResult { Success = true, Message = "Link association registered" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        private async Task<RequestResult> HandleRequest(PluginCallerContext caller, string command, Action<RequestResult>? callback, object[] args)
        {
            var result = new RequestResult { Success = true, Message = $"Command '{command}' executed by {caller.PackageName}" };
            callback?.Invoke(result);
            return await Task.FromResult(result);
        }

        private async Task<RequestResult> HandleLinkDefault(PluginCallerContext caller, string protocol)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                var protocolLower = protocol.ToLowerInvariant();

                var protocolItems = await _database.ExecuteQuery(
                    @"SELECT p.AssociatedItem FROM Phobos_Protocol p
                      INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                      WHERE p.Protocol = @protocol COLLATE NOCASE AND ai.PackageName = @packageName COLLATE NOCASE
                      LIMIT 1",
                    new Dictionary<string, object>
                    {
                        { "@protocol", protocolLower },
                        { "@packageName", caller.PackageName }
                    });

                if (protocolItems == null || protocolItems.Count == 0)
                {
                    return new RequestResult { Success = false, Message = "No handler registered for this protocol by this plugin" };
                }

                var associatedItem = protocolItems[0]["AssociatedItem"]?.ToString() ?? string.Empty;

                var existingShell = await _database.ExecuteQuery(
                    "SELECT AssociatedItem FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                if (existingShell?.Count > 0)
                {
                    var oldAssociatedItem = existingShell[0]["AssociatedItem"]?.ToString() ?? string.Empty;

                    await _database.ExecuteNonQuery(
                        @"UPDATE Phobos_Shell SET 
                            AssociatedItem = @newItem, 
                            UpdateUID = @uid, 
                            UpdateTime = datetime('now'),
                            LastValue = @lastValue
                          WHERE Protocol = @protocol COLLATE NOCASE",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocolLower },
                            { "@newItem", associatedItem },
                            { "@uid", caller.PackageName },
                            { "@lastValue", oldAssociatedItem }
                        });
                }
                else
                {
                    await _database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Shell (Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                          VALUES (@protocol, @associatedItem, @uid, datetime('now'), '')",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocolLower },
                            { "@associatedItem", associatedItem },
                            { "@uid", caller.PackageName }
                        });
                }

                return new RequestResult { Success = true, Message = $"Set as default for {protocol}" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        private async Task<ConfigResult> HandleReadConfig(PluginCallerContext caller, string key, string? packageName)
        {
            if (_database == null)
                return new ConfigResult { Success = false, Key = key, Message = "Database not initialized" };

            try
            {
                var targetPackage = packageName ?? caller.DatabaseKey ?? caller.PackageName;
                var uKey = $"{targetPackage}_{key}";
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Appdata WHERE UKey = @uKey COLLATE NOCASE",
                    new Dictionary<string, object> { { "@uKey", uKey } });

                if (result?.Count > 0)
                {
                    return new ConfigResult
                    {
                        Success = true,
                        Key = key,
                        Value = TextEscaper.Unescape(result[0]["Content"]?.ToString() ?? string.Empty)
                    };
                }

                return new ConfigResult { Success = false, Key = key, Message = "Config not found" };
            }
            catch (Exception ex)
            {
                return new ConfigResult { Success = false, Key = key, Message = ex.Message };
            }
        }

        private async Task<ConfigResult> HandleWriteConfig(PluginCallerContext caller, string key, string value, string? packageName)
        {
            if (_database == null)
                return new ConfigResult { Success = false, Key = key, Message = "Database not initialized" };

            try
            {
                var databaseKey = caller.DatabaseKey ?? caller.PackageName;
                var targetPackage = packageName ?? caller.PackageName;
                var uKey = $"{databaseKey}_{key}";

                if (!string.Equals(targetPackage, caller.PackageName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Plugin {caller.PackageName} is writing to {targetPackage}'s config");
                }

                var existing = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Appdata WHERE UKey = @uKey COLLATE NOCASE",
                    new Dictionary<string, object> { { "@uKey", uKey } });

                var oldValue = existing?.Count > 0 ? TextEscaper.Unescape(existing[0]["Content"]?.ToString() ?? string.Empty) : string.Empty;

                if (oldValue == value)
                {
                    return new ConfigResult { Success = true, Key = key, Value = value, Message = "Value unchanged" };
                }

                var escapedValue = TextEscaper.Escape(value);

                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Appdata (UKey, PackageName, Content, UpdateUID, UpdateTime, LastValue)
                      VALUES (@uKey, @packageName, @content, @uid, datetime('now'), @lastValue)",
                    new Dictionary<string, object>
                    {
                        { "@uKey", uKey },
                        { "@packageName", targetPackage },
                        { "@content", escapedValue },
                        { "@uid", caller.PackageName },
                        { "@lastValue", TextEscaper.Escape(oldValue) }
                    });

                return new ConfigResult { Success = true, Key = key, Value = value, Message = "Config saved" };
            }
            catch (Exception ex)
            {
                return new ConfigResult { Success = false, Key = key, Message = ex.Message };
            }
        }

        private async Task<ConfigResult> HandleReadSysConfig(PluginCallerContext caller, string key)
        {
            if (_database == null)
                return new ConfigResult { Success = false, Key = key, Message = "Database not initialized" };

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = @key COLLATE NOCASE",
                    new Dictionary<string, object> { { "@key", key } });

                if (result?.Count > 0)
                {
                    return new ConfigResult
                    {
                        Success = true,
                        Key = key,
                        Value = TextEscaper.Unescape(result[0]["Content"]?.ToString() ?? string.Empty)
                    };
                }

                return new ConfigResult { Success = false, Key = key, Message = "Config not found" };
            }
            catch (Exception ex)
            {
                return new ConfigResult { Success = false, Key = key, Message = ex.Message };
            }
        }

        private async Task<ConfigResult> HandleWriteSysConfig(PluginCallerContext caller, string key, string value)
        {
            if (_database == null)
                return new ConfigResult { Success = false, Key = key, Message = "Database not initialized" };

            try
            {
                var existing = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = @key COLLATE NOCASE",
                    new Dictionary<string, object> { { "@key", key } });

                var oldValue = existing?.Count > 0 ? TextEscaper.Unescape(existing[0]["Content"]?.ToString() ?? string.Empty) : string.Empty;

                if (oldValue == value)
                {
                    return new ConfigResult { Success = true, Key = key, Value = value, Message = "Value unchanged" };
                }

                var escapedValue = TextEscaper.Escape(value);

                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Main (Key, Content, UpdateUID, UpdateTime, LastValue)
                      VALUES (@key, @content, @uid, datetime('now'), @lastValue)",
                    new Dictionary<string, object>
                    {
                        { "@key", key },
                        { "@content", escapedValue },
                        { "@uid", caller.PackageName },
                        { "@lastValue", TextEscaper.Escape(oldValue) }
                    });

                return new ConfigResult { Success = true, Key = key, Value = value, Message = "System config saved" };
            }
            catch (Exception ex)
            {
                return new ConfigResult { Success = false, Key = key, Message = ex.Message };
            }
        }

        private async Task<BootResult> HandleBootWithPhobos(PluginCallerContext caller, string command, int priority, object[] args)
        {
            if (_database == null)
                return new BootResult { Success = false, Message = "Database not initialized" };

            try
            {
                var uuid = Guid.NewGuid().ToString("N");

                await _database.ExecuteNonQuery(
                    @"INSERT INTO Phobos_Boot (UUID, Command, PackageName, IsEnabled, Priority)
                      VALUES (@uuid, @command, @packageName, 1, @priority)",
                    new Dictionary<string, object>
                    {
                        { "@uuid", uuid },
                        { "@command", command },
                        { "@packageName", caller.PackageName },
                        { "@priority", priority }
                    });

                return new BootResult { Success = true, UUID = uuid, Message = "Boot item added" };
            }
            catch (Exception ex)
            {
                return new BootResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<BootResult> HandleRemoveBootWithPhobos(PluginCallerContext caller, string? uuid)
        {
            if (_database == null)
                return new BootResult { Success = false, Message = "Database not initialized" };

            try
            {
                if (!string.IsNullOrEmpty(uuid))
                {
                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Boot WHERE UUID = @uuid AND PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object>
                        {
                            { "@uuid", uuid },
                            { "@packageName", caller.PackageName }
                        });
                }
                else
                {
                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Boot WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", caller.PackageName } });
                }

                return new BootResult { Success = true, Message = "Boot item(s) removed" };
            }
            catch (Exception ex)
            {
                return new BootResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<List<object>> HandleGetBootItems(PluginCallerContext caller)
        {
            var result = new List<object>();

            if (_database == null)
                return result;

            try
            {
                var items = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Boot WHERE PackageName = @packageName COLLATE NOCASE ORDER BY Priority ASC",
                    new Dictionary<string, object> { { "@packageName", caller.PackageName } });

                foreach (var item in items ?? new List<Dictionary<string, object>>())
                {
                    result.Add(new PCPhobosBoot
                    {
                        UUID = item["UUID"]?.ToString() ?? string.Empty,
                        Command = item["Command"]?.ToString() ?? string.Empty,
                        PackageName = item["PackageName"]?.ToString() ?? string.Empty,
                        IsEnabled = Convert.ToInt32(item["IsEnabled"]) == 1,
                        Priority = Convert.ToInt32(item["Priority"])
                    });
                }
            }
            catch { }

            return result;
        }

        internal async Task<RequestResult> HandleLog(PluginCallerContext context, LogLevel level, string arg3, Exception? exception, object[]? arg5)
        {
            var appName = "";
            if (!context.Name.TryGetValue(PCSysConfig.Instance.langCode, out appName))
            {
                if (!context.Name.TryGetValue("en-US", out appName))
                    appName = context.PackageName;
            }
            switch (level)
            {
                case LogLevel.Error:
                    PCLoggerPlugin.Error(appName, arg3);
                    break;
                case LogLevel.Critical:
                    PCLoggerPlugin.Critical(appName, arg3);
                    break;
                case LogLevel.Warning:
                    PCLoggerPlugin.Warning(appName, arg3);
                    break;
                case LogLevel.Debug:
                    PCLoggerPlugin.Debug(appName, arg3);
                    break;
                default:
                    PCLoggerPlugin.Info(appName, arg3);
                    break;
            }
            return new RequestResult { Success = true, Message = $"Logged" };
        }

        /// <summary>
        /// 设置指定协议/扩展名的默认处理插件
        /// </summary>
        internal async Task<RequestResult> HandleSetDefaultHandler(PluginCallerContext caller, string protocolOrExtension, string? protocolUUID)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                var protocolLower = protocolOrExtension.ToLowerInvariant();

                // 如果没有指定 UUID，查找调用者自己注册的 UUID
                if (string.IsNullOrEmpty(protocolUUID))
                {
                    var callerProtocol = await _database.ExecuteQuery(
                        @"SELECT p.UUID FROM Phobos_Protocol p
                          INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                          WHERE p.Protocol = @protocol COLLATE NOCASE AND ai.PackageName = @packageName COLLATE NOCASE
                          LIMIT 1",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocolLower },
                            { "@packageName", caller.PackageName }
                        });

                    if (callerProtocol == null || callerProtocol.Count == 0)
                    {
                        return new RequestResult { Success = false, Message = "No handler registered for this protocol by this plugin" };
                    }

                    protocolUUID = callerProtocol[0]["UUID"]?.ToString() ?? string.Empty;
                }

                // 绑定默认处理器
                return await PMProtocol.Instance.BindDefaultHandler(protocolLower, protocolUUID, caller.PackageName);
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取指定协议/扩展名的当前默认处理插件信息
        /// </summary>
        internal async Task<ProtocolHandlerOption?> HandleGetDefaultHandler(PluginCallerContext caller, string protocolOrExtension)
        {
            if (_database == null)
                return null;

            try
            {
                var protocolLower = protocolOrExtension.ToLowerInvariant();
                var info = await PMProtocol.Instance.GetProtocolAssociationInfo(protocolLower);

                if (info == null)
                    return null;

                return new ProtocolHandlerOption
                {
                    UUID = info.UUID,
                    Protocol = info.Protocol,
                    AssociatedItem = info.AssociatedItemName,
                    PackageName = info.PackageName,
                    Description = info.Description,
                    Command = info.Command,
                    UpdateTime = info.ProtocolUpdateTime,
                    IsDefault = true,
                    IsUpdated = false
                };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPlugin", $"HandleGetDefaultHandler failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 发送简单通知
        /// </summary>
        internal async Task<NotifyResult> HandleSendNotification(PluginCallerContext caller, string title, string content, int duration)
        {
            try
            {
                // 查找默认的 notifier 插件
                var notifierInfo = await PMProtocol.Instance.GetProtocolAssociationInfo("notifier");
                if (notifierInfo == null)
                {
                    return new NotifyResult { Success = false, Message = "No notifier plugin bound" };
                }

                // 获取 notifier 插件实例
                var notifierPlugin = GetPlugin(notifierInfo.PackageName);
                if (notifierPlugin == null)
                {
                    // 尝试加载
                    var loadResult = await Load(notifierInfo.PackageName);
                    if (!loadResult.Success)
                    {
                        return new NotifyResult { Success = false, Message = $"Failed to load notifier plugin: {loadResult.Message}" };
                    }
                    notifierPlugin = GetPlugin(notifierInfo.PackageName);
                }

                if (notifierPlugin is IPhobosNotifier notifier)
                {
                    return await notifier.Notify(title, content, duration);
                }

                return new NotifyResult { Success = false, Message = "Notifier plugin does not implement IPhobosNotifier" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPlugin", $"HandleSendNotification failed: {ex.Message}");
                return new NotifyResult { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 发送通知对象
        /// </summary>
        internal async Task<NotifyResult> HandleSendNotificationObject(PluginCallerContext caller, IPhobosNotification notification)
        {
            try
            {
                // 设置发送者包名
                notification.PackageName = caller.PackageName;

                // 查找默认的 notifier 插件
                var notifierInfo = await PMProtocol.Instance.GetProtocolAssociationInfo("notifier");
                if (notifierInfo == null)
                {
                    return new NotifyResult { Success = false, Message = "No notifier plugin bound" };
                }

                // 获取 notifier 插件实例
                var notifierPlugin = GetPlugin(notifierInfo.PackageName);
                if (notifierPlugin == null)
                {
                    // 尝试加载
                    var loadResult = await Load(notifierInfo.PackageName);
                    if (!loadResult.Success)
                    {
                        return new NotifyResult { Success = false, Message = $"Failed to load notifier plugin: {loadResult.Message}" };
                    }
                    notifierPlugin = GetPlugin(notifierInfo.PackageName);
                }

                if (notifierPlugin is IPhobosNotifier notifier)
                {
                    return await notifier.Notify(notification);
                }

                return new NotifyResult { Success = false, Message = "Notifier plugin does not implement IPhobosNotifier" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPlugin", $"HandleSendNotificationObject failed: {ex.Message}");
                return new NotifyResult { Success = false, Message = ex.Message };
            }
        }

        #region 插件间通信处理器

        /// <summary>
        /// 处理插件间请求 - 向指定插件发送请求
        /// </summary>
        internal async Task<PluginRequestResult> HandleRequestPlugin(
            PluginCallerContext caller,
            string targetPackageName,
            string command,
            PluginRequestOptions? options,
            object[] args)
        {
            options ??= new PluginRequestOptions();

            try
            {
                // 检查目标插件是否已加载
                var targetPlugin = GetPlugin(targetPackageName);
                bool wasAutoLaunched = false;

                if (targetPlugin == null)
                {
                    // 插件未加载，检查是否需要自动启动
                    if (!options.AutoLaunchIfNotLoaded)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Target plugin '{targetPackageName}' is not loaded and AutoLaunchIfNotLoaded is false",
                            TargetPackageName = targetPackageName
                        };
                    }

                    // 检查插件是否已安装
                    if (_database == null)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = "Database not initialized",
                            TargetPackageName = targetPackageName
                        };
                    }

                    var pluginInfo = await _database.ExecuteQuery(
                        "SELECT IsEnabled FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", targetPackageName } });

                    if (pluginInfo == null || pluginInfo.Count == 0)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Target plugin '{targetPackageName}' is not installed",
                            TargetPackageName = targetPackageName
                        };
                    }

                    // 尝试加载并启动插件
                    PCLoggerPlugin.Info("PMPlugin", $"Auto-launching plugin '{targetPackageName}' for request from '{caller.PackageName}'");

                    var loadResult = await Load(targetPackageName);
                    if (!loadResult.Success)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Failed to load target plugin '{targetPackageName}': {loadResult.Message}",
                            TargetPackageName = targetPackageName
                        };
                    }

                    targetPlugin = GetPlugin(targetPackageName);
                    if (targetPlugin == null)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Failed to get plugin instance after loading '{targetPackageName}'",
                            TargetPackageName = targetPackageName
                        };
                    }

                    wasAutoLaunched = true;
                }

                // 执行请求（带超时）
                var runTask = targetPlugin.OnRequestReceived(caller.PackageName, command, args);

                if (options.WaitForResponse)
                {
                    var timeoutTask = Task.Delay(options.TimeoutMs);
                    var completedTask = await Task.WhenAny(runTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Request to '{targetPackageName}' timed out after {options.TimeoutMs}ms",
                            TargetPackageName = targetPackageName,
                            WasAutoLaunched = wasAutoLaunched
                        };
                    }

                    var result = await runTask;
                    return new PluginRequestResult
                    {
                        Success = result.Success,
                        Message = result.Message,
                        Data = result.Data,
                        Error = result.Error,
                        TargetPackageName = targetPackageName,
                        WasAutoLaunched = wasAutoLaunched,
                        ResponseData = result.Data.Count > 0 ? result.Data[0] : null
                    };
                }
                else
                {
                    // 不等待响应，立即返回
                    _ = runTask; // Fire and forget
                    return new PluginRequestResult
                    {
                        Success = true,
                        Message = $"Request sent to '{targetPackageName}' (not waiting for response)",
                        TargetPackageName = targetPackageName,
                        WasAutoLaunched = wasAutoLaunched
                    };
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPlugin", $"HandleRequestPlugin failed: {ex.Message}");
                return new PluginRequestResult
                {
                    Success = false,
                    Message = ex.Message,
                    Error = ex,
                    TargetPackageName = targetPackageName
                };
            }
        }

        /// <summary>
        /// 处理协议/扩展名请求 - 向指定协议/扩展名的默认处理插件发送请求
        /// </summary>
        internal async Task<PluginRequestResult> HandleRequestProtocolHandler(
            PluginCallerContext caller,
            string protocolOrExtension,
            string command,
            PluginRequestOptions? options,
            object[] args)
        {
            options ??= new PluginRequestOptions();

            try
            {
                var protocolLower = protocolOrExtension.ToLowerInvariant();

                if (_database == null)
                {
                    return new PluginRequestResult
                    {
                        Success = false,
                        Message = "Database not initialized"
                    };
                }

                // 1. 首先查找 Phobos_Shell 中的默认绑定
                var shellResult = await _database.ExecuteQuery(
                    "SELECT AssociatedItem FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                string? targetPackageName = null;

                if (shellResult != null && shellResult.Count > 0)
                {
                    // 有默认绑定，通过 UUID 查找包名
                    var protocolUUID = shellResult[0]["AssociatedItem"]?.ToString();
                    if (!string.IsNullOrEmpty(protocolUUID))
                    {
                        var packageResult = await _database.ExecuteQuery(
                            @"SELECT ai.PackageName FROM Phobos_Protocol p
                              INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                              WHERE p.UUID = @uuid",
                            new Dictionary<string, object> { { "@uuid", protocolUUID } });

                        if (packageResult != null && packageResult.Count > 0)
                        {
                            targetPackageName = packageResult[0]["PackageName"]?.ToString();
                        }
                    }
                }

                // 2. 如果没有默认绑定，查找所有可用的处理器
                if (string.IsNullOrEmpty(targetPackageName))
                {
                    var allHandlers = await PMProtocol.Instance.FindAllAssociatedItems(protocolLower);

                    if (allHandlers.Count == 0)
                    {
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"No handler found for '{protocolOrExtension}'"
                        };
                    }

                    if (allHandlers.Count == 1)
                    {
                        // 只有一个处理器，直接使用
                        targetPackageName = allHandlers[0].PackageName;
                        PCLoggerPlugin.Info("PMPlugin", $"Auto-selected single handler '{targetPackageName}' for '{protocolOrExtension}'");
                    }
                    else
                    {
                        // 有多个处理器但没有默认绑定，返回失败
                        var handlerNames = string.Join(", ", allHandlers.Select(h => h.PackageName));
                        return new PluginRequestResult
                        {
                            Success = false,
                            Message = $"Multiple handlers available for '{protocolOrExtension}' but no default is set. Available handlers: {handlerNames}. Please set a default handler first."
                        };
                    }
                }

                // 3. 调用目标插件
                return await HandleRequestPlugin(caller, targetPackageName, command, options, args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPlugin", $"HandleRequestProtocolHandler failed: {ex.Message}");
                return new PluginRequestResult
                {
                    Success = false,
                    Message = ex.Message,
                    Error = ex
                };
            }
        }

        #endregion

        #endregion
    }
}