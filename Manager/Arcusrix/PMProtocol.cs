using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phobos.Class.Database;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Utils.General;

namespace Phobos.Manager.Arcusrix
{
    /// <summary>
    /// 协议管理器 - 管理链接关联
    /// </summary>
    public class PMProtocol
    {
        private static PMProtocol? _instance;
        private static readonly object _lock = new();

        private PCSqliteDatabase? _database;

        public static PMProtocol Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMProtocol();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化协议管理器
        /// </summary>
        public void Initialize(PCSqliteDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// 注册协议关联
        /// </summary>
        public async Task<RequestResult> RegisterProtocol(string protocol, string associatedItemName, string packageName)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            try
            {
                await _database.ExecuteNonQuery(
                    @"INSERT OR REPLACE INTO Phobos_Protocol (Protocol, AssociatedItem, UpdateUID, UpdateTime)
                      VALUES (@protocol, @associatedItem, @uid, datetime('now'))",
                    new Dictionary<string, object>
                    {
                        { "@protocol", protocol.ToLowerInvariant() },
                        { "@associatedItem", associatedItemName },
                        { "@uid", packageName },
                        { "@UpdateTime", "datetime('now')" }
                    });

                return new RequestResult { Success = true, Message = "Protocol registered" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 注册关联项
        /// </summary>
        public async Task<RequestResult> RegisterAssociatedItem(LinkAssociation association, string packageName)
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
                        { "@packageName", packageName },
                        { "@description", TextEscaper.Escape(association.Description) },
                        { "@command", association.Command }
                    });

                return new RequestResult { Success = true, Message = "Associated item registered" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取协议对应的关联项
        /// </summary>
        public async Task<string?> GetAssociatedItem(string protocol)
        {
            if (_database == null)
                return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT AssociatedItem FROM Phobos_Protocol WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocol.ToLowerInvariant() } });

                if (result?.Count > 0)
                {
                    return result[0]["AssociatedItem"]?.ToString();
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取关联项的命令
        /// </summary>
        public async Task<string?> GetCommand(string associatedItemName)
        {
            if (_database == null)
                return null;

            try
            {
                var result = await _database.ExecuteQuery(
                    "SELECT Command FROM Phobos_AssociatedItem WHERE Name = @name COLLATE NOCASE",
                    new Dictionary<string, object> { { "@name", associatedItemName } });

                if (result?.Count > 0)
                {
                    return result[0]["Command"]?.ToString();
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 处理协议链接
        /// </summary>
        public async Task<RequestResult> HandleProtocol(string url)
        {
            var protocol = PUText.ExtractProtocol(url);
            if (string.IsNullOrEmpty(protocol))
            {
                return new RequestResult { Success = false, Message = "Invalid protocol URL" };
            }

            var associatedItem = await GetAssociatedItem(protocol);
            if (string.IsNullOrEmpty(associatedItem))
            {
                return new RequestResult { Success = false, Message = $"No handler registered for protocol: {protocol}" };
            }

            var command = await GetCommand(associatedItem);
            if (string.IsNullOrEmpty(command))
            {
                return new RequestResult { Success = false, Message = $"No command found for: {associatedItem}" };
            }

            // 替换占位符
            var withoutProtocol = PUText.ExtractWithoutProtocol(url);
            var finalCommand = command
                .Replace("%1", url)
                .Replace("%0", withoutProtocol);

            return new RequestResult
            {
                Success = true,
                Message = "Protocol handled",
                Data = new List<object> { finalCommand, associatedItem, protocol }
            };
        }

        /// <summary>
        /// 获取协议的所有可用处理程序
        /// </summary>
        public async Task<List<LinkAssociation>> GetProtocolHandlers(string protocol)
        {
            var result = new List<LinkAssociation>();

            if (_database == null)
                return result;

            try
            {
                var items = await _database.ExecuteQuery(
                    @"SELECT ai.Name, ai.PackageName, ai.Description, ai.Command
                      FROM Phobos_AssociatedItem ai
                      INNER JOIN Phobos_Protocol p ON p.AssociatedItem = ai.Name
                      WHERE p.Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocol.ToLowerInvariant() } });

                foreach (var item in items ?? new List<Dictionary<string, object>>())
                {
                    result.Add(new LinkAssociation
                    {
                        Name = item["Name"]?.ToString() ?? string.Empty,
                        Protocol = protocol,
                        PackageName = item["PackageName"]?.ToString() ?? string.Empty,
                        Description = TextEscaper.Unescape(item["Description"]?.ToString() ?? string.Empty),
                        Command = item["Command"]?.ToString() ?? string.Empty
                    });
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// 删除插件的所有协议关联
        /// </summary>
        public async Task RemovePluginProtocols(string packageName)
        {
            if (_database == null)
                return;

            try
            {
                // 删除关联项
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_AssociatedItem WHERE PackageName = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });

                // 删除协议映射
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Protocol WHERE UpdateUID = @packageName COLLATE NOCASE",
                    new Dictionary<string, object> { { "@packageName", packageName } });
            }
            catch { }
        }
    }
}