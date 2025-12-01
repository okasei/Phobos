using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Class;

namespace Phobos.Manager.Database
{
    /// <summary>
    /// 数据库管理器
    /// </summary>
    public class PMDatabase
    {
        private static PMDatabase? _instance;
        private static readonly object _lock = new();

        private PCSqliteDatabase? _database;

        public static PMDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMDatabase();
                    }
                }
                return _instance;
            }
        }

        public PCSqliteDatabase? Database => _database;

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public async Task<bool> Initialize(string databasePath)
        {
            _database = new PCSqliteDatabase(databasePath);
            return await _database.Connect();
        }

        /// <summary>
        /// 读取系统配置
        /// </summary>
        public async Task<string?> GetSystemConfig(string key)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Main WHERE Key = @key COLLATE NOCASE",
                    new Dictionary<string, object> { { "@key", key } });

                if (result?.Count > 0)
                {
                    return TextEscaper.Unescape(result[0]["Content"]?.ToString());
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 写入系统配置
        /// </summary>
        public async Task<bool> SetSystemConfig(string key, string value, string? updateUid = null)
        {
            if (_database == null) return false;

            try
            {
                // 获取旧值
                var oldValue = await GetSystemConfig(key) ?? string.Empty;

                // 如果值相同，不写入
                if (oldValue == value)
                    return true;

                var escapedValue = TextEscaper.Escape(value);
                var escapedOldValue = TextEscaper.Escape(oldValue);

                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Main (Key, Content, UpdateUID, UpdateTime, LastValue)
                      VALUES (@key, @content, @uid, datetime('now'), @lastValue)",
                    new Dictionary<string, object>
                    {
                        { "@key", key },
                        { "@content", escapedValue },
                        { "@uid", updateUid ?? string.Empty },
                        { "@lastValue", escapedOldValue }
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除系统配置
        /// </summary>
        public async Task<bool> DeleteSystemConfig(string key)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Main WHERE Key = @key COLLATE NOCASE",
                    new Dictionary<string, object> { { "@key", key } });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 读取插件配置
        /// </summary>
        public async Task<string?> GetPluginConfig(string packageName, string key)
        {
            if (_database == null) return null;

            try
            {
                var uKey = $"{packageName}_{key}";
                var result = await _database.ExecuteQuery(
                    "SELECT Content FROM Phobos_Appdata WHERE UKey = @uKey COLLATE NOCASE",
                    new Dictionary<string, object> { { "@uKey", uKey } });

                if (result?.Count > 0)
                {
                    return TextEscaper.Unescape(result[0]["Content"]?.ToString());
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 写入插件配置
        /// </summary>
        public async Task<bool> SetPluginConfig(string packageName, string key, string value, string? updateUid = null)
        {
            if (_database == null) return false;

            try
            {
                var uKey = $"{packageName}_{key}";

                // 获取旧值
                var oldValue = await GetPluginConfig(packageName, key) ?? string.Empty;

                // 如果值相同，不写入
                if (oldValue == value)
                    return true;

                var escapedValue = TextEscaper.Escape(value);
                var escapedOldValue = TextEscaper.Escape(oldValue);

                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Appdata (UKey, PackageName, Content, UpdateUID, UpdateTime, LastValue)
                      VALUES (@uKey, @packageName, @content, @uid, datetime('now'), @lastValue)",
                    new Dictionary<string, object>
                    {
                        { "@uKey", uKey },
                        { "@packageName", packageName },
                        { "@content", escapedValue },
                        { "@uid", updateUid ?? string.Empty },
                        { "@lastValue", escapedOldValue }
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除插件配置
        /// </summary>
        public async Task<bool> DeletePluginConfig(string packageName, string key)
        {
            if (_database == null) return false;

            try
            {
                var uKey = $"{packageName}_{key}";
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Appdata WHERE UKey = @uKey COLLATE NOCASE",
                    new Dictionary<string, object> { { "@uKey", uKey } });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取插件的所有配置
        /// </summary>
        public async Task<Dictionary<string, string>> GetAllPluginConfigs(string packageName)
        {
            var result = new Dictionary<string, string>();

            if (_database == null) return result;

            try
            {
                var prefix = $"{packageName}_";
                var rows = await _database.ExecuteQuery(
                    "SELECT UKey, Content FROM Phobos_Appdata WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                foreach (var row in rows ?? new List<Dictionary<string, object>>())
                {
                    var uKey = row["UKey"]?.ToString() ?? string.Empty;
                    var content = TextEscaper.Unescape(row["Content"]?.ToString());

                    if (uKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var key = uKey[prefix.Length..];
                        result[key] = content ?? string.Empty;
                    }
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 清除插件的所有配置
        /// </summary>
        public async Task<bool> ClearPluginConfigs(string packageName)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Appdata WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Plugin Management

        /// <summary>
        /// 注册插件到数据库
        /// </summary>
        public async Task<bool> RegisterPlugin(Shared.Interface.PluginMetadata metadata, string directory)
        {
            if (_database == null) return false;

            try
            {
                var uninstallInfoJson = metadata.UninstallInfo != null
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(metadata.UninstallInfo)
                    : string.Empty;

                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Plugin 
                      (PackageName, Name, Manufacturer, Description, Version, Secret, Language, 
                       InstallTime, Directory, Icon, IsSystemPlugin, SettingUri, UninstallInfo, IsEnabled, UpdateTime)
                      VALUES 
                      (@packageName, @name, @manufacturer, @description, @version, @secret, @language,
                       datetime('now'), @directory, @icon, @isSystemPlugin, @settingUri, @uninstallInfo, 1, datetime('now'))",
                    new Dictionary<string, object>
                    {
                        { "@packageName", metadata.PackageName },
                        { "@name", metadata.Name },
                        { "@manufacturer", metadata.Manufacturer },
                        { "@description", metadata.Description },
                        { "@version", metadata.Version },
                        { "@secret", metadata.Secret },
                        { "@language", "en-US" },
                        { "@directory", directory },
                        { "@icon", metadata.Icon ?? string.Empty },
                        { "@isSystemPlugin", metadata.IsSystemPlugin ? 1 : 0 },
                        { "@settingUri", metadata.SettingUri ?? string.Empty },
                        { "@uninstallInfo", uninstallInfoJson }
                    });

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Plugin.Register", $"Failed to register plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新插件信息
        /// </summary>
        public async Task<bool> UpdatePlugin(Shared.Interface.PluginMetadata metadata)
        {
            if (_database == null) return false;

            try
            {
                var uninstallInfoJson = metadata.UninstallInfo != null
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(metadata.UninstallInfo)
                    : string.Empty;

                await _database.ExecuteNonQuery(
                    @"UPDATE Phobos_Plugin SET 
                      Name = @name, Manufacturer = @manufacturer, Description = @description, 
                      Version = @version, Icon = @icon, IsSystemPlugin = @isSystemPlugin, 
                      SettingUri = @settingUri, UninstallInfo = @uninstallInfo, UpdateTime = datetime('now')
                      WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object>
                    {
                        { "@packageName", metadata.PackageName },
                        { "@name", metadata.Name },
                        { "@manufacturer", metadata.Manufacturer },
                        { "@description", metadata.Description },
                        { "@version", metadata.Version },
                        { "@icon", metadata.Icon ?? string.Empty },
                        { "@isSystemPlugin", metadata.IsSystemPlugin ? 1 : 0 },
                        { "@settingUri", metadata.SettingUri ?? string.Empty },
                        { "@uninstallInfo", uninstallInfoJson }
                    });

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Plugin.Update", $"Failed to update plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除插件记录
        /// </summary>
        public async Task<bool> UnregisterPlugin(string packageName)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取插件信息
        /// </summary>
        public async Task<PluginRecord?> GetPluginRecord(string packageName)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    return ParsePluginRecord(result[0]);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取所有插件记录
        /// </summary>
        public async Task<List<PluginRecord>> GetAllPluginRecords()
        {
            var records = new List<PluginRecord>();

            if (_database == null) return records;

            try
            {
                var result = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin ORDER BY Name");

                foreach (var row in result ?? new List<Dictionary<string, object>>())
                {
                    var record = ParsePluginRecord(row);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }
            catch { }

            return records;
        }

        /// <summary>
        /// 获取所有非系统插件
        /// </summary>
        public async Task<List<PluginRecord>> GetUserPluginRecords()
        {
            var records = new List<PluginRecord>();

            if (_database == null) return records;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE IsSystemPlugin = 0 ORDER BY Name");

                foreach (var row in result ?? new List<Dictionary<string, object>>())
                {
                    var record = ParsePluginRecord(row);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }
            catch { }

            return records;
        }

        /// <summary>
        /// 获取所有系统插件
        /// </summary>
        public async Task<List<PluginRecord>> GetSystemPluginRecords()
        {
            var records = new List<PluginRecord>();

            if (_database == null) return records;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE IsSystemPlugin = 1 ORDER BY Name");

                foreach (var row in result ?? new List<Dictionary<string, object>>())
                {
                    var record = ParsePluginRecord(row);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }
            catch { }

            return records;
        }

        /// <summary>
        /// 启用/禁用插件
        /// </summary>
        public async Task<bool> SetPluginEnabled(string packageName, bool enabled)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    "UPDATE Phobos_Plugin SET IsEnabled = @enabled, UpdateTime = datetime('now') WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object>
                    {
                        { "@packageName", packageName },
                        { "@enabled", enabled ? 1 : 0 }
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查插件是否已注册
        /// </summary>
        public async Task<bool> IsPluginRegistered(string packageName)
        {
            if (_database == null) return false;

            try
            {
                var result = await _database.ExecuteScalar(
                    "SELECT COUNT(*) FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                return Convert.ToInt64(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        private PluginRecord? ParsePluginRecord(Dictionary<string, object> row)
        {
            try
            {
                var record = new PluginRecord
                {
                    PackageName = row["PackageName"]?.ToString() ?? string.Empty,
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Manufacturer = row["Manufacturer"]?.ToString() ?? string.Empty,
                    Description = row["Description"]?.ToString() ?? string.Empty,
                    Version = row["Version"]?.ToString() ?? "1.0.0",
                    Secret = row["Secret"]?.ToString() ?? string.Empty,
                    Language = row["Language"]?.ToString() ?? "en-US",
                    Directory = row["Directory"]?.ToString() ?? string.Empty,
                    Icon = row["Icon"]?.ToString() ?? string.Empty,
                    IsSystemPlugin = Convert.ToInt32(row["IsSystemPlugin"] ?? 0) == 1,
                    SettingUri = row["SettingUri"]?.ToString() ?? string.Empty,
                    IsEnabled = Convert.ToInt32(row["IsEnabled"] ?? 1) == 1
                };

                // 解析时间
                if (DateTime.TryParse(row["InstallTime"]?.ToString(), out var installTime))
                    record.InstallTime = installTime;
                if (DateTime.TryParse(row["UpdateTime"]?.ToString(), out var updateTime))
                    record.UpdateTime = updateTime;

                // 解析卸载信息
                var uninstallInfoJson = row["UninstallInfo"]?.ToString();
                if (!string.IsNullOrEmpty(uninstallInfoJson))
                {
                    try
                    {
                        record.UninstallInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Shared.Interface.PluginUninstallInfo>(uninstallInfoJson);
                    }
                    catch { }
                }

                return record;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析为 PCPhobosPlugin 实体
        /// </summary>
        private Phobos.Class.Database.PCPhobosPlugin? ParsePluginEntity(Dictionary<string, object> row)
        {
            try
            {
                var entity = new Phobos.Class.Database.PCPhobosPlugin
                {
                    PackageName = row["PackageName"]?.ToString() ?? string.Empty,
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Manufacturer = row["Manufacturer"]?.ToString() ?? string.Empty,
                    Description = row["Description"]?.ToString() ?? string.Empty,
                    Version = row["Version"]?.ToString() ?? "1.0.0",
                    Secret = row["Secret"]?.ToString() ?? string.Empty,
                    Language = row["Language"]?.ToString() ?? "en-US",
                    Directory = row["Directory"]?.ToString() ?? string.Empty,
                    Icon = row["Icon"]?.ToString() ?? string.Empty,
                    IsSystemPlugin = Convert.ToInt32(row["IsSystemPlugin"] ?? 0) == 1,
                    SettingUri = row["SettingUri"]?.ToString() ?? string.Empty,
                    UninstallInfo = row["UninstallInfo"]?.ToString() ?? string.Empty,
                    IsEnabled = Convert.ToInt32(row["IsEnabled"] ?? 1) == 1
                };

                // 解析时间
                if (DateTime.TryParse(row["InstallTime"]?.ToString(), out var installTime))
                    entity.InstallTime = installTime;
                if (DateTime.TryParse(row["UpdateTime"]?.ToString(), out var updateTime))
                    entity.UpdateTime = updateTime;

                return entity;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取插件实体
        /// </summary>
        public async Task<Phobos.Class.Database.PCPhobosPlugin?> GetPluginEntity(string packageName)
        {
            if (_database == null) return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT * FROM Phobos_Plugin WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                if (result?.Count > 0)
                {
                    return ParsePluginEntity(result[0]);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取所有插件实体
        /// </summary>
        public async Task<List<Phobos.Class.Database.PCPhobosPlugin>> GetAllPluginEntities()
        {
            var entities = new List<Phobos.Class.Database.PCPhobosPlugin>();

            if (_database == null) return entities;

            try
            {
                var result = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin ORDER BY Name");

                foreach (var row in result ?? new List<Dictionary<string, object>>())
                {
                    var entity = ParsePluginEntity(row);
                    if (entity != null)
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch { }

            return entities;
        }

        /// <summary>
        /// 从实体注册插件
        /// </summary>
        public async Task<bool> RegisterPluginEntity(Phobos.Class.Database.PCPhobosPlugin entity)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Plugin 
                      (PackageName, Name, Manufacturer, Description, Version, Secret, Language, 
                       InstallTime, Directory, Icon, IsSystemPlugin, SettingUri, UninstallInfo, IsEnabled, UpdateTime)
                      VALUES 
                      (@packageName, @name, @manufacturer, @description, @version, @secret, @language,
                       @installTime, @directory, @icon, @isSystemPlugin, @settingUri, @uninstallInfo, @isEnabled, @updateTime)",
                    new Dictionary<string, object>
                    {
                        { "@packageName", entity.PackageName },
                        { "@name", entity.Name },
                        { "@manufacturer", entity.Manufacturer },
                        { "@description", entity.Description },
                        { "@version", entity.Version },
                        { "@secret", entity.Secret },
                        { "@language", entity.Language },
                        { "@installTime", entity.InstallTime.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "@directory", entity.Directory },
                        { "@icon", entity.Icon },
                        { "@isSystemPlugin", entity.IsSystemPlugin ? 1 : 0 },
                        { "@settingUri", entity.SettingUri },
                        { "@uninstallInfo", entity.UninstallInfo },
                        { "@isEnabled", entity.IsEnabled ? 1 : 0 },
                        { "@updateTime", entity.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss") }
                    });

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Phobos.Plugin.RegisterE", $"Failed to register plugin entity: {ex.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 备份数据库
        /// </summary>
        public async Task<bool> Backup(string backupPath)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery($"VACUUM INTO '{backupPath}'");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public async Task Close()
        {
            if (_database != null)
            {
                await _database.Disconnect();
                _database.Dispose();
                _database = null;
            }
        }
    }
}