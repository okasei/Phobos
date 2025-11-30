using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Interface.Plugin;
using Phobos.Manager.System;
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

            var plugins = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin");
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
                Log = HandleLog
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

                if (existing?.Count > 0 && !options.ForceReinstall)
                {
                    tempContext.Unload();
                    return new RequestResult { Success = false, Message = "Plugin already installed" };
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
                    if (existing?.Count > 0)
                    {
                        await _database.ExecuteNonQuery(
                            @"UPDATE Phobos_Plugin SET 
                                Name = @name, Manufacturer = @manufacturer, Description = @description,
                                Version = @version, Secret = @secret, Directory = @directory
                              WHERE PackageName = @packageName COLLATE NOCASE",
                            new Dictionary<string, object>
                            {
                                { "@packageName", metadata.PackageName },
                                { "@name", TextEscaper.Escape(metadata.Name) },
                                { "@manufacturer", TextEscaper.Escape(metadata.Manufacturer) },
                                { "@description", TextEscaper.Escape(metadata.GetLocalizedDescription("en-US")) },
                                { "@version", metadata.Version },
                                { "@secret", metadata.Secret },
                                { "@directory", $"\\Plugins\\{metadata.PackageName}" }
                            });
                    }
                    else
                    {
                        await _database.ExecuteNonQuery(
                            @"INSERT INTO Phobos_Plugin (PackageName, Name, Manufacturer, Description, Version, Secret, Directory)
                              VALUES (@packageName, @name, @manufacturer, @description, @version, @secret, @directory)",
                            new Dictionary<string, object>
                            {
                                { "@packageName", metadata.PackageName },
                                { "@name", TextEscaper.Escape(metadata.Name) },
                                { "@manufacturer", TextEscaper.Escape(metadata.Manufacturer) },
                                { "@description", TextEscaper.Escape(metadata.GetLocalizedDescription("en-US")) },
                                { "@version", metadata.Version },
                                { "@secret", metadata.Secret },
                                { "@directory", pluginDir }
                            });
                    }
                }

                var loadResult = await Load(metadata.PackageName);
                if (loadResult.Success)
                {
                    var plugin = GetPlugin(metadata.PackageName);
                    if (plugin != null)
                    {
                        await plugin.OnInstall();
                    }
                }

                return new RequestResult { Success = true, Message = "Plugin installed successfully" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public async Task<RequestResult> Uninstall(string packageName)
        {
            try
            {
                var plugin = GetPlugin(packageName);
                if (plugin != null)
                {
                    await plugin.OnUninstall();
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

                    var pluginInfo = await _database.ExecuteQuery(
                        "SELECT Directory FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    await _database.ExecuteNonQuery(
                        "DELETE FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                        new Dictionary<string, object> { { "@packageName", packageName } });

                    if (pluginInfo?.Count > 0)
                    {
                        var directory = pluginInfo[0]["Directory"]?.ToString();
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        {
                            Directory.Delete(directory, true);
                        }
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

                var result = await context.Instance.OnLaunch(args);
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

                return new RequestResult { Success = true, Message = "Plugin updated successfully" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
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
                result.Add(new PluginMetadata
                {
                    PackageName = plugin["PackageName"]?.ToString() ?? string.Empty,
                    Name = TextEscaper.Unescape(plugin["Name"]?.ToString() ?? string.Empty),
                    Manufacturer = TextEscaper.Unescape(plugin["Manufacturer"]?.ToString() ?? string.Empty),
                    Version = plugin["Version"]?.ToString() ?? "1.0.0",
                    Secret = plugin["Secret"]?.ToString() ?? string.Empty
                });
            }

            return result;
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
                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_AssociatedItem (Name, PackageName, Description, Command)
                      VALUES (@name, @packageName, @description, @command)",
                    new Dictionary<string, object>
                    {
                        { "@name", association.Name },
                        { "@packageName", caller.PackageName },
                        { "@description", TextEscaper.Escape(association.Description) },
                        { "@command", association.Command }
                    });

                var existing = await _database.ExecuteQuery(
                    @"SELECT UUID FROM Phobos_Protocol 
                      WHERE Protocol = @protocol COLLATE NOCASE AND AssociatedItem = @associatedItem COLLATE NOCASE",
                    new Dictionary<string, object>
                    {
                        { "@protocol", association.Protocol.ToLowerInvariant() },
                        { "@associatedItem", association.Name }
                    });

                if (existing?.Count > 0)
                {
                    await _database.ExecuteNonQuery(
                        @"UPDATE Phobos_Protocol SET UpdateTime = datetime('now'), UpdateUID = @uid
                          WHERE UUID = @uuid",
                        new Dictionary<string, object>
                        {
                            { "@uuid", existing[0]["UUID"]?.ToString() ?? string.Empty },
                            { "@uid", caller.PackageName }
                        });
                }
                else
                {
                    await _database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Protocol (UUID, Protocol, AssociatedItem, UpdateUID, UpdateTime)
                          VALUES (@uuid, @protocol, @associatedItem, @uid, datetime('now'))",
                        new Dictionary<string, object>
                        {
                            { "@uuid", Guid.NewGuid().ToString("N") },
                            { "@protocol", association.Protocol.ToLowerInvariant() },
                            { "@associatedItem", association.Name },
                            { "@uid", caller.PackageName }
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
                var targetPackage = packageName ?? caller.PackageName;
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
                var targetPackage = packageName ?? caller.PackageName;
                var uKey = $"{targetPackage}_{key}";

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

        private async Task<RequestResult> HandleLog(PluginCallerContext context, LogLevel level, string arg3, Exception? exception, object[]? arg5)
        {
            switch (level)
            {
                case LogLevel.Error:
                    PCLoggerPlugin.Error(context.PackageName, arg3);
                    break;
                case LogLevel.Critical:
                    PCLoggerPlugin.Critical(context.PackageName, arg3);
                    break;
                case LogLevel.Warning:
                    PCLoggerPlugin.Warning(context.PackageName, arg3);
                    break;
                case LogLevel.Debug:
                    PCLoggerPlugin.Debug(context.PackageName, arg3);
                    break;
                default:
                    PCLoggerPlugin.Info(context.PackageName, arg3);
                    break;
            }
            return new RequestResult { Success = true, Message = $"Logged" };
        }

        #endregion
    }
}