using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Phobos.Manager.Permission
{
    /// <summary>
    /// 权限类型定义
    /// </summary>
    public static class PhobosPermissions
    {
        /// <summary>
        /// 发送通知权限
        /// </summary>
        public const string NOTIFICATION_SEND = "PHOBOS_PERMISSION_NOTIFICATION_SEND";

        /// <summary>
        /// 设置自启动权限
        /// </summary>
        public const string AUTOSTART = "PHOBOS_PERMISSION_AUTOSTART";

        /// <summary>
        /// 关联启动权限
        /// </summary>
        public const string LINKSTART = "PHOBOS_PERMISSION_LINKSTART";

        /// <summary>
        /// 读取系统设置权限
        /// </summary>
        public const string READ_SYS = "PHOBOS_PERMISSION_READ_SYS";

        /// <summary>
        /// 写入系统设置权限
        /// </summary>
        public const string WRITE_SYS = "PHOBOS_PERMISSION_WRITE_SYS";

        /// <summary>
        /// 读取其它插件设置权限
        /// </summary>
        public const string PLUGINSETTING_READ = "PHOBOS_PERMISSION_PLUGINSETTING_READ";

        /// <summary>
        /// 写入其它插件设置权限
        /// </summary>
        public const string PLUGINSETTING_WRITE = "PHOBOS_PERMISSION_PLUGINSETTING_WRITE";

        /// <summary>
        /// 获取桌面项权限
        /// </summary>
        public const string DESKTOPITEM = "PHOBOS_PERMISSION_DESKTOPITEM";

        /// <summary>
        /// 订阅事件权限前缀 (格式: PHOBOS_PERMISSION_SUB_{EventId}_{EventName})
        /// </summary>
        public const string SUB_EVENT_PREFIX = "PHOBOS_PERMISSION_SUB_";

        /// <summary>
        /// 所有基础权限列表（不包含订阅事件权限）
        /// </summary>
        public static readonly string[] AllBasePermissions = new[]
        {
            NOTIFICATION_SEND,
            AUTOSTART,
            LINKSTART,
            READ_SYS,
            WRITE_SYS,
            PLUGINSETTING_READ,
            PLUGINSETTING_WRITE,
            DESKTOPITEM
        };

        /// <summary>
        /// 系统插件默认拥有的权限（无法关闭）
        /// </summary>
        public static readonly string[] SystemPluginPermissions = new[]
        {
            READ_SYS,
            WRITE_SYS,
            PLUGINSETTING_READ,
            PLUGINSETTING_WRITE
        };

        /// <summary>
        /// 获取订阅事件权限名称
        /// </summary>
        public static string GetSubscribePermission(string eventId, string eventName)
        {
            return $"{SUB_EVENT_PREFIX}{eventId}_{eventName}";
        }
    }

    /// <summary>
    /// 权限检查结果
    /// </summary>
    public class PermissionCheckResult
    {
        /// <summary>
        /// 是否允许
        /// </summary>
        public bool Allowed { get; set; }

        /// <summary>
        /// 是否需要询问用户
        /// </summary>
        public bool NeedAsk { get; set; }

        /// <summary>
        /// 是否已被永久拒绝
        /// </summary>
        public bool PermanentlyDenied { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 权限管理器
    /// </summary>
    public class PMPermission
    {
        private static PMPermission? _instance;
        private static readonly object _lock = new();

        private PCSqliteDatabase? _database;

        // 内存缓存
        private readonly Dictionary<string, PCPhobosPermission> _permissionCache = new(StringComparer.OrdinalIgnoreCase);
        private bool _cacheLoaded = false;

        public static PMPermission Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMPermission();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化权限管理器
        /// </summary>
        public async Task Initialize(PCSqliteDatabase database)
        {
            _database = database;
            await LoadPermissionsToCache();
        }

        /// <summary>
        /// 加载权限数据到缓存
        /// </summary>
        private async Task LoadPermissionsToCache()
        {
            if (_database == null) return;

            try
            {
                var result = await _database.ExecuteQuery("SELECT * FROM Phobos_Permission");
                _permissionCache.Clear();

                if (result != null)
                {
                    foreach (var row in result)
                    {
                        var permission = new PCPhobosPermission
                        {
                            PackageName = row["PackageName"]?.ToString() ?? string.Empty,
                            IsTrusted = Convert.ToInt32(row["IsTrusted"] ?? 0) == 1,
                            Permissions = row["Permissions"]?.ToString() ?? string.Empty,
                            Denied = row["Denied"]?.ToString() ?? string.Empty
                        };
                        _permissionCache[permission.PackageName] = permission;
                    }
                }
                _cacheLoaded = true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to load permissions: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查权限
        /// </summary>
        /// <param name="packageName">插件包名</param>
        /// <param name="permission">权限名称</param>
        /// <returns>权限检查结果</returns>
        public async Task<PermissionCheckResult> CheckPermission(string packageName, string permission)
        {
            // 检查是否为系统插件
            var isSystemPlugin = await IsSystemPlugin(packageName);

            // 系统插件默认权限检查
            if (isSystemPlugin && PhobosPermissions.SystemPluginPermissions.Contains(permission))
            {
                return new PermissionCheckResult { Allowed = true, Message = "System plugin default permission" };
            }

            // 获取权限记录
            var permissionRecord = await GetPermissionRecordAsync(packageName);

            // 信任的插件所有权限全通
            if (permissionRecord?.IsTrusted == true)
            {
                return new PermissionCheckResult { Allowed = true, Message = "Trusted plugin" };
            }

            // 检查是否已被永久拒绝
            if (permissionRecord != null && HasDeniedPermission(permissionRecord, permission))
            {
                return new PermissionCheckResult
                {
                    Allowed = false,
                    PermanentlyDenied = true,
                    Message = "Permission permanently denied"
                };
            }

            // 检查是否已授权
            if (permissionRecord != null && HasGrantedPermission(permissionRecord, permission))
            {
                return new PermissionCheckResult { Allowed = true, Message = "Permission granted" };
            }

            // 需要询问用户
            return new PermissionCheckResult
            {
                Allowed = false,
                NeedAsk = true,
                Message = "Permission not granted, need to ask user"
            };
        }

        /// <summary>
        /// 授予权限
        /// </summary>
        public async Task<bool> GrantPermission(string packageName, string permission)
        {
            try
            {
                var record = await GetOrCreatePermissionRecord(packageName);
                var permissions = ParsePermissionList(record.Permissions);

                if (!permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    permissions.Add(permission);
                    record.Permissions = string.Join(",", permissions);
                    await SavePermissionRecord(record);
                }

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to grant permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 撤销权限
        /// </summary>
        public async Task<bool> RevokePermission(string packageName, string permission)
        {
            try
            {
                var record = await GetPermissionRecordAsync(packageName);
                if (record == null) return true;

                var permissions = ParsePermissionList(record.Permissions);
                permissions.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
                record.Permissions = string.Join(",", permissions);
                await SavePermissionRecord(record);

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to revoke permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 永久拒绝权限
        /// </summary>
        public async Task<bool> DenyPermissionPermanently(string packageName, string permission)
        {
            try
            {
                var record = await GetOrCreatePermissionRecord(packageName);
                var denied = ParsePermissionList(record.Denied);

                if (!denied.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    denied.Add(permission);
                    record.Denied = string.Join(",", denied);
                }

                // 同时从已授权列表中移除
                var permissions = ParsePermissionList(record.Permissions);
                permissions.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
                record.Permissions = string.Join(",", permissions);

                await SavePermissionRecord(record);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to deny permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取消永久拒绝权限
        /// </summary>
        public async Task<bool> UndenyPermission(string packageName, string permission)
        {
            try
            {
                var record = await GetPermissionRecordAsync(packageName);
                if (record == null) return true;

                var denied = ParsePermissionList(record.Denied);
                denied.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
                record.Denied = string.Join(",", denied);
                await SavePermissionRecord(record);

                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to undeny permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重置权限（移除授权和拒绝状态）
        /// </summary>
        public async Task<bool> ResetPermission(string packageName, string permission)
        {
            try
            {
                var record = await GetPermissionRecordAsync(packageName);
                if (record == null) return true;

                // 从授权列表移除
                var permissions = ParsePermissionList(record.Permissions);
                permissions.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
                record.Permissions = string.Join(",", permissions);

                // 从拒绝列表移除
                var denied = ParsePermissionList(record.Denied);
                denied.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
                record.Denied = string.Join(",", denied);

                await SavePermissionRecord(record);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to reset permission: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置插件为信任状态
        /// </summary>
        public async Task<bool> SetTrusted(string packageName, bool trusted)
        {
            try
            {
                var record = await GetOrCreatePermissionRecord(packageName);
                record.IsTrusted = trusted;
                await SavePermissionRecord(record);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to set trusted: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查插件是否被信任
        /// </summary>
        public async Task<bool> IsTrusted(string packageName)
        {
            var record = await GetPermissionRecordAsync(packageName);
            return record?.IsTrusted ?? false;
        }

        /// <summary>
        /// 获取插件的所有权限状态
        /// </summary>
        public async Task<Dictionary<string, PermissionStatus>> GetPluginPermissionStatus(string packageName)
        {
            var result = new Dictionary<string, PermissionStatus>();
            var record = await GetPermissionRecordAsync(packageName);
            var isSystemPlugin = await IsSystemPlugin(packageName);

            var grantedList = record != null ? ParsePermissionList(record.Permissions) : new List<string>();
            var deniedList = record != null ? ParsePermissionList(record.Denied) : new List<string>();

            foreach (var permission in PhobosPermissions.AllBasePermissions)
            {
                var status = new PermissionStatus
                {
                    Permission = permission,
                    IsSystemDefault = isSystemPlugin && PhobosPermissions.SystemPluginPermissions.Contains(permission)
                };

                if (status.IsSystemDefault)
                {
                    status.State = PermissionState.Granted;
                }
                else if (record?.IsTrusted == true)
                {
                    status.State = PermissionState.Granted;
                }
                else if (deniedList.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    status.State = PermissionState.Denied;
                }
                else if (grantedList.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    status.State = PermissionState.Granted;
                }
                else
                {
                    status.State = PermissionState.NotSet;
                }

                result[permission] = status;
            }

            return result;
        }

        /// <summary>
        /// 获取所有非系统插件的权限记录
        /// </summary>
        public async Task<List<PCPhobosPermission>> GetAllPermissionRecords()
        {
            if (!_cacheLoaded)
            {
                await LoadPermissionsToCache();
            }
            return _permissionCache.Values.ToList();
        }

        /// <summary>
        /// 获取插件的权限记录（同步，用于 UI 绑定）
        /// </summary>
        public PermissionRecord GetPermissionRecord(string packageName)
        {
            if (_permissionCache.TryGetValue(packageName, out var record))
            {
                return new PermissionRecord
                {
                    PackageName = record.PackageName,
                    IsTrusted = record.IsTrusted,
                    GrantedPermissions = ParsePermissionList(record.Permissions),
                    DeniedPermissions = ParsePermissionList(record.Denied)
                };
            }

            return new PermissionRecord
            {
                PackageName = packageName,
                IsTrusted = false,
                GrantedPermissions = new List<string>(),
                DeniedPermissions = new List<string>()
            };
        }

        /// <summary>
        /// 获取单个权限状态（同步，用于 UI 绑定）
        /// </summary>
        public PermissionState GetPermissionState(string packageName, string permission)
        {
            var record = GetPermissionRecord(packageName);

            if (record.IsTrusted)
            {
                return PermissionState.Granted;
            }

            if (record.DeniedPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            {
                return PermissionState.Denied;
            }

            if (record.GrantedPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            {
                return PermissionState.Granted;
            }

            return PermissionState.NotSet;
        }

        /// <summary>
        /// 删除插件的权限记录（卸载插件时调用）
        /// </summary>
        public async Task<bool> DeletePermissionRecord(string packageName)
        {
            if (_database == null) return false;

            try
            {
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Permission WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                _permissionCache.Remove(packageName);
                return true;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMPermission", $"Failed to delete permission record: {ex.Message}");
                return false;
            }
        }

        #region Private Methods

        private async Task<bool> IsSystemPlugin(string packageName)
        {
            try
            {
                var plugins = await PMPlugin.Instance.GetInstalledPlugins();
                var plugin = plugins.FirstOrDefault(p => p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));
                return plugin?.IsSystemPlugin ?? false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<PCPhobosPermission?> GetPermissionRecordAsync(string packageName)
        {
            if (!_cacheLoaded)
            {
                await LoadPermissionsToCache();
            }

            return _permissionCache.TryGetValue(packageName, out var record) ? record : null;
        }

        private async Task<PCPhobosPermission> GetOrCreatePermissionRecord(string packageName)
        {
            var record = await GetPermissionRecordAsync(packageName);
            if (record == null)
            {
                record = new PCPhobosPermission
                {
                    PackageName = packageName,
                    IsTrusted = false,
                    Permissions = string.Empty,
                    Denied = string.Empty
                };
                _permissionCache[packageName] = record;
            }
            return record;
        }

        private async Task SavePermissionRecord(PCPhobosPermission record)
        {
            if (_database == null) return;

            await _database.ExecuteNonQuery(
                @"INSERT OR REPLACE INTO Phobos_Permission (PackageName, IsTrusted, Permissions, Denied)
                  VALUES (@packageName, @isTrusted, @permissions, @denied)",
                new Dictionary<string, object>
                {
                    { "@packageName", record.PackageName },
                    { "@isTrusted", record.IsTrusted ? 1 : 0 },
                    { "@permissions", record.Permissions },
                    { "@denied", record.Denied }
                });

            _permissionCache[record.PackageName] = record;
        }

        private static List<string> ParsePermissionList(string permissionString)
        {
            if (string.IsNullOrWhiteSpace(permissionString))
                return new List<string>();

            return permissionString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
        }

        private static bool HasGrantedPermission(PCPhobosPermission record, string permission)
        {
            var permissions = ParsePermissionList(record.Permissions);
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        private static bool HasDeniedPermission(PCPhobosPermission record, string permission)
        {
            var denied = ParsePermissionList(record.Denied);
            return denied.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }

    /// <summary>
    /// 权限状态
    /// </summary>
    public enum PermissionState
    {
        /// <summary>
        /// 未设置（需要询问）
        /// </summary>
        NotSet,

        /// <summary>
        /// 已授权
        /// </summary>
        Granted,

        /// <summary>
        /// 已拒绝
        /// </summary>
        Denied
    }

    /// <summary>
    /// 权限状态详情
    /// </summary>
    public class PermissionStatus
    {
        /// <summary>
        /// 权限名称
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// 权限状态
        /// </summary>
        public PermissionState State { get; set; } = PermissionState.NotSet;

        /// <summary>
        /// 是否为系统默认权限（系统插件的默认权限）
        /// </summary>
        public bool IsSystemDefault { get; set; }
    }

    /// <summary>
    /// 权限记录（用于 UI 绑定）
    /// </summary>
    public class PermissionRecord
    {
        /// <summary>
        /// 插件包名
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// 是否信任
        /// </summary>
        public bool IsTrusted { get; set; }

        /// <summary>
        /// 已授权的权限列表
        /// </summary>
        public List<string> GrantedPermissions { get; set; } = new();

        /// <summary>
        /// 已拒绝的权限列表
        /// </summary>
        public List<string> DeniedPermissions { get; set; } = new();
    }
}
