using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phobos.Class.Database;
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
            _database = new PCSqliteDatabase(databasePath, useEncryption: false);
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