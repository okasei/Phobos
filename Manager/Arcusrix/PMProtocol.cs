using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Utils.General;

namespace Phobos.Manager.Arcusrix
{
    /// <summary>
    /// 特殊协议类型
    /// </summary>
    public static class SpecialProtocolType
    {
        /// <summary>
        /// 文本类型
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// 图片类型
        /// </summary>
        public const string Image = "image";

        /// <summary>
        /// 浏览器类型
        /// </summary>
        public const string Browser = "browser";

        /// <summary>
        /// 视频/音频类型
        /// </summary>
        public const string Video = "video";

        /// <summary>
        /// 文本文件扩展名
        /// </summary>
        public static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".log", ".ini", ".inf", ".cfg", ".conf", ".config",
            ".css", ".js", ".ts", ".jsx", ".tsx", ".json", ".xml",
            ".yml", ".yaml", ".toml", ".md", ".markdown", ".rst",
            ".srt", ".sub", ".ass", ".ssa", ".vtt", ".lrc",
            ".sh", ".bash", ".zsh", ".fish", ".ps1", ".psm1", ".bat", ".cmd",
            ".py", ".pyw", ".rb", ".php", ".pl", ".pm", ".lua",
            ".c", ".h", ".cpp", ".hpp", ".cc", ".cxx", ".cs", ".java",
            ".go", ".rs", ".swift", ".kt", ".kts", ".scala", ".clj",
            ".sql", ".graphql", ".gql",
            ".gitignore", ".gitattributes", ".editorconfig", ".env",
            ".properties", ".gradle", ".makefile", ".cmake",
            ".dockerfile", ".dockerignore", ".htaccess", ".nginx",
            ".csv", ".tsv", ".nfo", ".diz", ".readme", ".license",
            ".asm", ".s", ".vbs", ".vb", ".bas", ".cls", ".frm"
        };

        /// <summary>
        /// 图片文件扩展名
        /// </summary>
        public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".icon",
            ".webp", ".avif", ".apng", ".svg", ".svgz",
            ".tiff", ".tif", ".psd", ".ai", ".eps",
            ".raw", ".cr2", ".nef", ".orf", ".sr2", ".arw", ".dng",
            ".heic", ".heif", ".jfif", ".jp2", ".j2k", ".jpf", ".jpm",
            ".exr", ".hdr", ".pbm", ".pgm", ".ppm", ".pnm",
            ".tga", ".pcx", ".dds", ".cur", ".ani"
        };

        /// <summary>
        /// 浏览器可解析的扩展名
        /// </summary>
        public static readonly HashSet<string> BrowserExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".xhtml", ".xht", ".mhtml", ".mht",
            ".xml", ".xsl", ".xslt", ".rss", ".atom", ".rdf",
            ".php", ".asp", ".aspx", ".jsp", ".do", ".action",
            ".shtml", ".shtm", ".jhtml", ".cfm", ".cgi"
        };

        /// <summary>
        /// 浏览器协议
        /// </summary>
        public static readonly HashSet<string> BrowserProtocols = new(StringComparer.OrdinalIgnoreCase)
        {
            "http:", "https:", "ftp:", "ftps:", "file:", "data:", "about:", "javascript:"
        };

        /// <summary>
        /// 视频/音频文件扩展名
        /// </summary>
        public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // 视频格式
            ".mp4", ".m4v", ".mkv", ".avi", ".wmv", ".mov", ".flv", ".f4v",
            ".webm", ".ogv", ".3gp", ".3g2", ".ts", ".mts", ".m2ts",
            ".vob", ".mpg", ".mpeg", ".m2v", ".divx", ".xvid",
            ".rm", ".rmvb", ".asf", ".swf", ".dv", ".dvr-ms",
            ".wtv", ".h264", ".h265", ".hevc", ".av1", ".vp8", ".vp9",
            // 音频格式
            ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".oga",
            ".wma", ".aiff", ".aif", ".ape", ".alac", ".opus", ".ac3",
            ".dts", ".mka", ".mid", ".midi", ".mod", ".xm", ".it", ".s3m",
            ".ra", ".ram", ".au", ".snd", ".cda", ".amr", ".awb", ".wv"
        };

        /// <summary>
        /// 视频/流媒体协议
        /// </summary>
        public static readonly HashSet<string> VideoProtocols = new(StringComparer.OrdinalIgnoreCase)
        {
            "rtmp:", "rtmps:", "rtsp:", "rtp:", "mms:", "mmsh:", "mmst:",
            "hls:", "dash:", "udp:", "tcp:", "srt:", "rist:"
        };
    }

    /// <summary>
    /// 协议/文件类型关联项信息 (包含 Phobos_Shell, Phobos_Protocol 和 Phobos_AssociatedItem 的信息)
    /// </summary>
    public class ProtocolAssociationInfo
    {
        /// <summary>
        /// Phobos_Protocol 表中的 UUID
        /// </summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>
        /// 协议/文件类型键 (如 "http:", ".txt", "video")
        /// </summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// 关联项名称 (Phobos_AssociatedItem.Name)
        /// </summary>
        public string AssociatedItemName { get; set; } = string.Empty;

        /// <summary>
        /// 插件包名
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// 关联项描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 执行命令模板
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Phobos_Protocol 表中的更新时间
        /// </summary>
        public DateTime ProtocolUpdateTime { get; set; }

        /// <summary>
        /// Phobos_Shell 表中的更新时间 (如果有绑定)
        /// </summary>
        public DateTime? ShellUpdateTime { get; set; }

        /// <summary>
        /// 是否为当前默认绑定
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 是否为新更新的项 (相对于某个时间点)
        /// </summary>
        public bool IsNew { get; set; }
    }

    /// <summary>
    /// 查找关联项时的事件过滤选项
    /// </summary>
    public class ProtocolQueryOptions
    {
        /// <summary>
        /// 参考时间点 (用于判断是否为"新"项)
        /// </summary>
        public DateTime? ReferenceTime { get; set; }

        /// <summary>
        /// 只查找在参考时间之后更新的项
        /// </summary>
        public bool OnlyAfterReferenceTime { get; set; }

        /// <summary>
        /// 只查找在参考时间之前更新的项
        /// </summary>
        public bool OnlyBeforeReferenceTime { get; set; }

        /// <summary>
        /// 是否包含系统处理器 (com.microsoft.windows)
        /// </summary>
        public bool IncludeSystemHandler { get; set; } = true;
    }

    /// <summary>
    /// 用户选择默认打开方式的回调
    /// </summary>
    /// <param name="selectedHandler">用户选择的处理器 (null 表示取消)</param>
    /// <param name="setAsDefault">是否设为默认</param>
    public delegate void DefaultHandlerSelectedCallback(ProtocolHandlerOption? selectedHandler, bool setAsDefault);

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
                var protocolLower = protocol.ToLowerInvariant();

                // 查询是否已存在该包名+协议的组合
                var existing = await _database.ExecuteQuery(
                    @"SELECT UUID, AssociatedItem FROM Phobos_Protocol
                      WHERE Protocol = @protocol AND UpdateUID = @packageName",
                    new Dictionary<string, object>
                    {
                        { "@protocol", protocolLower },
                        { "@packageName", packageName }
                    });

                if (existing?.Count > 0)
                {
                    // 已存在：检查是否有变化
                    var existingUuid = existing[0]["UUID"]?.ToString() ?? "";
                    var oldAssociatedItem = existing[0]["AssociatedItem"]?.ToString() ?? "";

                    // 只有当 AssociatedItem 变化时才更新
                    if (oldAssociatedItem != associatedItemName)
                    {
                        await _database.ExecuteNonQuery(
                            @"UPDATE Phobos_Protocol
                              SET AssociatedItem = @associatedItem,
                                  UpdateTime = datetime('now'),
                                  LastValue = @lastValue
                              WHERE UUID = @uuid",
                            new Dictionary<string, object>
                            {
                                { "@uuid", existingUuid },
                                { "@associatedItem", associatedItemName },
                                { "@lastValue", oldAssociatedItem }
                            });
                    }
                }
                else
                {
                    // 不存在：插入新记录
                    await _database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Protocol (UUID, Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                          VALUES (@uuid, @protocol, @associatedItem, @uid, datetime('now'), '')",
                        new Dictionary<string, object>
                        {
                            { "@uuid", Guid.NewGuid().ToString("N") },
                            { "@protocol", protocolLower },
                            { "@associatedItem", associatedItemName },
                            { "@uid", packageName }
                        });
                }

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

        #region 协议/文件类型关联查询方法

        /// <summary>
        /// 查找一个协议/文件类型关联的所有项
        /// </summary>
        /// <param name="protocolOrType">协议名(如 "http:")、文件类型(如 ".txt")或特殊类型(如 "video")</param>
        /// <param name="options">查询选项 (可选)</param>
        /// <returns>关联项列表</returns>
        public async Task<List<ProtocolHandlerOption>> FindAllAssociatedItems(
            string protocolOrType,
            ProtocolQueryOptions? options = null)
        {
            var result = new List<ProtocolHandlerOption>();

            if (_database == null || string.IsNullOrWhiteSpace(protocolOrType))
                return result;

            options ??= new ProtocolQueryOptions();
            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                // 先获取 Shell 绑定信息，确定当前默认和参考时间
                DateTime? shellUpdateTime = null;
                string? currentDefaultUUID = null;

                var shellResult = await _database.ExecuteQuery(
                    "SELECT AssociatedItem, UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                if (shellResult?.Count > 0)
                {
                    currentDefaultUUID = shellResult[0]["AssociatedItem"]?.ToString();
                    if (DateTime.TryParse(shellResult[0]["UpdateTime"]?.ToString(), out var dt))
                    {
                        shellUpdateTime = dt;
                    }
                }

                // 确定参考时间
                var referenceTime = options.ReferenceTime ?? shellUpdateTime ?? DateTime.MinValue;

                // 查询所有关联项
                var handlers = await _database.ExecuteQuery(
                    @"SELECT p.UUID, p.Protocol, p.AssociatedItem, p.UpdateTime, p.UpdateUID,
                             ai.PackageName, ai.Description, ai.Command
                      FROM Phobos_Protocol p
                      INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                      WHERE p.Protocol = @protocol COLLATE NOCASE
                      ORDER BY p.UpdateTime DESC",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                foreach (var handler in handlers ?? new List<Dictionary<string, object>>())
                {
                    var packageName = handler["PackageName"]?.ToString() ?? string.Empty;

                    // 检查是否排除系统处理器
                    if (!options.IncludeSystemHandler && packageName == "com.microsoft.windows")
                        continue;

                    var updateTime = DateTime.TryParse(handler["UpdateTime"]?.ToString(), out var ut) ? ut : DateTime.MinValue;

                    // 检查时间过滤条件
                    if (options.OnlyAfterReferenceTime && updateTime <= referenceTime)
                        continue;
                    if (options.OnlyBeforeReferenceTime && updateTime >= referenceTime)
                        continue;

                    var uuid = handler["UUID"]?.ToString() ?? string.Empty;
                    var isNew = updateTime > referenceTime;

                    result.Add(new ProtocolHandlerOption
                    {
                        UUID = uuid,
                        Protocol = protocolLower,
                        AssociatedItem = handler["AssociatedItem"]?.ToString() ?? string.Empty,
                        PackageName = packageName,
                        Description = TextEscaper.Unescape(handler["Description"]?.ToString() ?? string.Empty),
                        Command = handler["Command"]?.ToString() ?? string.Empty,
                        UpdateTime = updateTime,
                        IsDefault = uuid == currentDefaultUUID,
                        IsUpdated = isNew
                    });
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"FindAllAssociatedItems failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 查找一个协议关联项的详细信息 (在 Phobos_Shell 里面找，包含 Phobos_Protocol 和 Phobos_AssociatedItem 里面的信息)
        /// 如果 Phobos_Shell 没有绑定，会自动查找 Phobos_Protocol：
        /// - 如果只有 1 个处理器，自动绑定
        /// - 如果有多个处理器，弹窗让用户选择
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        /// <returns>关联项详细信息 (如果没有绑定且用户取消选择则返回 null)</returns>
        public async Task<ProtocolAssociationInfo?> GetProtocolAssociationInfo(string protocolOrType)
        {
            if (_database == null || string.IsNullOrWhiteSpace(protocolOrType))
                return null;

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                // 1. 从 Phobos_Shell 获取当前绑定
                var shellResult = await _database.ExecuteQuery(
                    "SELECT AssociatedItem, UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                var protocolUUID = shellResult?.Count > 0 ? shellResult[0]["AssociatedItem"]?.ToString() : null;
                var shellUpdateTime = shellResult?.Count > 0 && DateTime.TryParse(shellResult[0]["UpdateTime"]?.ToString(), out var sdt) ? sdt : (DateTime?)null;

                // 2. 如果 Phobos_Shell 没有绑定，查找 Phobos_Protocol 中的处理器
                if (string.IsNullOrEmpty(protocolUUID))
                {
                    var allHandlers = await FindAllAssociatedItems(protocolLower);

                    if (allHandlers.Count == 0)
                    {
                        // 没有任何处理器
                        return null;
                    }
                    else if (allHandlers.Count == 1)
                    {
                        // 只有 1 个处理器，自动绑定
                        var handler = allHandlers[0];
                        await BindDefaultHandler(protocolLower, handler.UUID, handler.PackageName);
                        protocolUUID = handler.UUID;
                        PCLoggerPlugin.Info("PMProtocol", $"Auto-bound single handler for {protocolOrType}: {handler.PackageName}");
                    }
                    else
                    {
                        // 多个处理器，弹窗让用户选择
                        var (selectedHandler, setAsDefault) = await ShowDefaultHandlerDialog(protocolOrType);

                        if (selectedHandler == null)
                        {
                            // 用户取消了选择
                            return null;
                        }

                        protocolUUID = selectedHandler.UUID;
                    }

                    // 重新获取 shellUpdateTime
                    var newShellResult = await _database.ExecuteQuery(
                        "SELECT UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                        new Dictionary<string, object> { { "@protocol", protocolLower } });
                    if (newShellResult?.Count > 0 && DateTime.TryParse(newShellResult[0]["UpdateTime"]?.ToString(), out var newSdt))
                    {
                        shellUpdateTime = newSdt;
                    }
                }

                // 3. 通过 UUID 从 Phobos_Protocol 和 Phobos_AssociatedItem 获取详细信息
                var detailResult = await _database.ExecuteQuery(
                    @"SELECT p.UUID, p.Protocol, p.AssociatedItem, p.UpdateTime, p.UpdateUID,
                             ai.PackageName, ai.Description, ai.Command
                      FROM Phobos_Protocol p
                      INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                      WHERE p.UUID = @uuid",
                    new Dictionary<string, object> { { "@uuid", protocolUUID } });

                if (detailResult == null || detailResult.Count == 0)
                    return null;

                var detail = detailResult[0];
                var protocolUpdateTime = DateTime.TryParse(detail["UpdateTime"]?.ToString(), out var pdt) ? pdt : DateTime.MinValue;

                return new ProtocolAssociationInfo
                {
                    UUID = protocolUUID,
                    Protocol = detail["Protocol"]?.ToString() ?? protocolLower,
                    AssociatedItemName = detail["AssociatedItem"]?.ToString() ?? string.Empty,
                    PackageName = detail["PackageName"]?.ToString() ?? string.Empty,
                    Description = TextEscaper.Unescape(detail["Description"]?.ToString() ?? string.Empty),
                    Command = detail["Command"]?.ToString() ?? string.Empty,
                    ProtocolUpdateTime = protocolUpdateTime,
                    ShellUpdateTime = shellUpdateTime,
                    IsDefault = true
                };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"GetProtocolAssociationInfo failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 弹窗让用户选择默认打开方式
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        /// <param name="callback">用户选择后的回调 (可选)</param>
        /// <param name="newSinceTime">晚于此时间的项将显示"新"标签 (可选)</param>
        /// <param name="title">对话框标题 (可选)</param>
        /// <param name="subtitle">对话框副标题 (可选)</param>
        /// <param name="owner">父窗口 (可选)</param>
        /// <returns>用户选择的处理器和是否设为默认 (如果用户取消则返回 null, false)</returns>
        public async Task<(ProtocolHandlerOption? selectedHandler, bool setAsDefault)> ShowDefaultHandlerDialog(
            string protocolOrType,
            DefaultHandlerSelectedCallback? callback = null,
            DateTime? newSinceTime = null,
            string? title = null,
            string? subtitle = null,
            Window? owner = null)
        {
            if (_database == null || string.IsNullOrWhiteSpace(protocolOrType))
            {
                callback?.Invoke(null, false);
                return (null, false);
            }

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                // 获取所有处理器
                var allHandlers = await FindAllAssociatedItems(protocolLower);

                if (allHandlers.Count == 0)
                {
                    PCLoggerPlugin.Warning("PMProtocol", $"No handlers found for {protocolOrType}");
                    callback?.Invoke(null, false);
                    return (null, false);
                }

                // 根据 newSinceTime 分类
                var newHandlers = new List<ProtocolHandlerOption>();
                var existingHandlers = new List<ProtocolHandlerOption>();

                foreach (var handler in allHandlers)
                {
                    if (newSinceTime.HasValue && handler.UpdateTime > newSinceTime.Value)
                    {
                        handler.IsUpdated = true;
                        newHandlers.Add(handler);
                    }
                    else
                    {
                        handler.IsUpdated = false;
                        existingHandlers.Add(handler);
                    }
                }

                // 显示选择对话框
                ProtocolHandlerOption? selectedHandler = null;
                bool setAsDefault = false;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var dialog = new Components.Arcusrix.Runner.PCOHandlerSelectDialog(
                        newHandlers,
                        existingHandlers,
                        new RunRequest { DbKey = protocolLower, RawInput = protocolLower },
                        protocolLower,
                        title,
                        subtitle);

                    if (owner != null)
                    {
                        dialog.Owner = owner;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }

                    if (dialog.ShowDialog() == true)
                    {
                        selectedHandler = dialog.SelectedHandler;
                        setAsDefault = dialog.SetAsDefault;
                    }
                });

                // 如果用户选择了处理器且选择设为默认，则绑定
                if (selectedHandler != null && setAsDefault)
                {
                    await BindDefaultHandler(protocolLower, selectedHandler.UUID, selectedHandler.PackageName);
                }
                else if (selectedHandler != null && !setAsDefault)
                {
                    // 用户选择"仅一次"，删除现有绑定
                    await RemoveDefaultBinding(protocolLower);
                }

                // 调用回调
                callback?.Invoke(selectedHandler, setAsDefault);

                return (selectedHandler, setAsDefault);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"ShowDefaultHandlerDialog failed: {ex.Message}");
                callback?.Invoke(null, false);
                return (null, false);
            }
        }

        /// <summary>
        /// 检查协议/文件类型是否有新的处理器出现
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        /// <param name="sinceTime">自此时间之后</param>
        /// <returns>是否有新处理器，以及新处理器列表</returns>
        public async Task<(bool hasNew, List<ProtocolHandlerOption> newHandlers)> CheckForNewHandlers(
            string protocolOrType,
            DateTime? sinceTime = null)
        {
            var newHandlers = new List<ProtocolHandlerOption>();

            if (_database == null || string.IsNullOrWhiteSpace(protocolOrType))
                return (false, newHandlers);

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                // 如果没有指定时间，使用 Shell 中的更新时间
                DateTime referenceTime = sinceTime ?? DateTime.MinValue;

                if (!sinceTime.HasValue)
                {
                    var shellResult = await _database.ExecuteQuery(
                        "SELECT UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                        new Dictionary<string, object> { { "@protocol", protocolLower } });

                    if (shellResult?.Count > 0)
                    {
                        if (DateTime.TryParse(shellResult[0]["UpdateTime"]?.ToString(), out var dt))
                        {
                            referenceTime = dt;
                        }
                    }
                }

                // 查找新处理器
                var handlers = await FindAllAssociatedItems(protocolLower, new ProtocolQueryOptions
                {
                    ReferenceTime = referenceTime,
                    OnlyAfterReferenceTime = true
                });

                newHandlers.AddRange(handlers);

                return (newHandlers.Count > 0, newHandlers);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"CheckForNewHandlers failed: {ex.Message}");
                return (false, newHandlers);
            }
        }

        /// <summary>
        /// 绑定默认处理器 (存储 Phobos_Protocol.UUID 到 Phobos_Shell)
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        /// <param name="protocolUUID">Phobos_Protocol 表中的 UUID</param>
        /// <param name="packageName">插件包名</param>
        public async Task<RequestResult> BindDefaultHandler(string protocolOrType, string protocolUUID, string packageName)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                // 检查是否已存在绑定
                var existing = await _database.ExecuteQuery(
                    "SELECT AssociatedItem FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                if (existing != null && existing.Count > 0)
                {
                    // 更新现有绑定
                    var oldItem = existing[0]["AssociatedItem"]?.ToString() ?? string.Empty;
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
                            { "@newItem", protocolUUID },
                            { "@uid", packageName },
                            { "@lastValue", oldItem }
                        });
                }
                else
                {
                    // 插入新绑定
                    await _database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Shell (Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                          VALUES (@protocol, @protocolUUID, @uid, datetime('now'), '')",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocolLower },
                            { "@protocolUUID", protocolUUID },
                            { "@uid", packageName }
                        });
                }

                PCLoggerPlugin.Info("PMProtocol", $"Bound {protocolOrType} to {packageName} (UUID: {protocolUUID})");
                return new RequestResult { Success = true, Message = $"Bound {protocolOrType} to {packageName}" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"BindDefaultHandler failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 删除协议/文件类型的默认绑定
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        public async Task<RequestResult> RemoveDefaultBinding(string protocolOrType)
        {
            if (_database == null)
                return new RequestResult { Success = false, Message = "Database not initialized" };

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                await _database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });

                PCLoggerPlugin.Info("PMProtocol", $"Removed default binding for {protocolOrType}");
                return new RequestResult { Success = true, Message = $"Removed default binding for {protocolOrType}" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"RemoveDefaultBinding failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 更新 Shell 绑定的时间戳 (用于同一插件更新时刷新检查时间)
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        public async Task UpdateShellTimestamp(string protocolOrType)
        {
            if (_database == null) return;

            var protocolLower = protocolOrType.ToLowerInvariant();

            try
            {
                await _database.ExecuteNonQuery(
                    "UPDATE Phobos_Shell SET UpdateTime = datetime('now') WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocolLower } });
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PMProtocol", $"UpdateShellTimestamp failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 绑定到系统处理器
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型/特殊类型</param>
        public async Task<RequestResult> BindToSystemHandler(string protocolOrType)
        {
            var systemUUID = $"system_{protocolOrType.ToLowerInvariant()}";
            return await BindDefaultHandler(protocolOrType, systemUUID, "com.microsoft.windows");
        }

        #endregion

        #region 特殊类型解析

        /// <summary>
        /// 解析输入并返回特殊类型（如果匹配）
        /// </summary>
        /// <param name="input">输入的协议、文件名或路径</param>
        /// <returns>特殊类型 (text, image, browser, video) 或 null 如果不匹配任何特殊类型</returns>
        public static string? ResolveSpecialType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();

            // 1. 检查是否是协议
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 10) // 协议通常很短
            {
                var protocol = trimmed.Substring(0, colonIndex + 1).ToLowerInvariant();

                // 检查浏览器协议
                if (SpecialProtocolType.BrowserProtocols.Contains(protocol))
                    return SpecialProtocolType.Browser;

                // 检查视频/流媒体协议
                if (SpecialProtocolType.VideoProtocols.Contains(protocol))
                    return SpecialProtocolType.Video;
            }

            // 2. 提取扩展名
            string? extension = null;

            // 处理带协议的 URL（如 file:///path/to/file.txt 或 http://example.com/file.txt）
            if (colonIndex > 0)
            {
                // 尝试从 URL 中提取文件名和扩展名
                var path = trimmed;

                // 移除查询字符串和锚点
                var queryIndex = path.IndexOf('?');
                if (queryIndex > 0) path = path.Substring(0, queryIndex);
                var hashIndex = path.IndexOf('#');
                if (hashIndex > 0) path = path.Substring(0, hashIndex);

                // 获取最后一个路径段
                var lastSlash = path.LastIndexOfAny(new[] { '/', '\\' });
                var fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;

                // 提取扩展名
                var dotIndex = fileName.LastIndexOf('.');
                if (dotIndex > 0 && dotIndex < fileName.Length - 1)
                {
                    extension = fileName.Substring(dotIndex).ToLowerInvariant();
                }
            }
            else
            {
                // 普通文件路径或扩展名
                if (trimmed.StartsWith("."))
                {
                    // 已经是扩展名格式
                    extension = trimmed.ToLowerInvariant();
                }
                else
                {
                    // 尝试从文件名/路径提取扩展名
                    var lastSlash = trimmed.LastIndexOfAny(new[] { '/', '\\' });
                    var fileName = lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
                    var dotIndex = fileName.LastIndexOf('.');
                    if (dotIndex > 0 && dotIndex < fileName.Length - 1)
                    {
                        extension = fileName.Substring(dotIndex).ToLowerInvariant();
                    }
                }
            }

            // 3. 根据扩展名匹配特殊类型
            if (!string.IsNullOrEmpty(extension))
            {
                // 按优先级检查：video > image > browser > text
                if (SpecialProtocolType.VideoExtensions.Contains(extension))
                    return SpecialProtocolType.Video;

                if (SpecialProtocolType.ImageExtensions.Contains(extension))
                    return SpecialProtocolType.Image;

                if (SpecialProtocolType.BrowserExtensions.Contains(extension))
                    return SpecialProtocolType.Browser;

                if (SpecialProtocolType.TextExtensions.Contains(extension))
                    return SpecialProtocolType.Text;
            }

            return null;
        }

        /// <summary>
        /// 解析输入并返回实际协议（优先返回特殊类型，如果不匹配则返回原始协议/扩展名）
        /// </summary>
        /// <param name="input">输入的协议、文件名或路径</param>
        /// <returns>解析后的协议键（特殊类型或原始协议/扩展名）</returns>
        public static string ResolveProtocolKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 先尝试解析为特殊类型
            var specialType = ResolveSpecialType(input);
            if (specialType != null)
                return specialType;

            // 不是特殊类型，返回原始协议或扩展名
            var trimmed = input.Trim();

            // 检查是否是协议
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 10)
            {
                return trimmed.Substring(0, colonIndex + 1).ToLowerInvariant();
            }

            // 提取扩展名
            if (trimmed.StartsWith("."))
            {
                return trimmed.ToLowerInvariant();
            }

            var lastSlash = trimmed.LastIndexOfAny(new[] { '/', '\\' });
            var fileName = lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
            var dotIndex = fileName.LastIndexOf('.');
            if (dotIndex > 0 && dotIndex < fileName.Length - 1)
            {
                return fileName.Substring(dotIndex).ToLowerInvariant();
            }

            return trimmed.ToLowerInvariant();
        }

        /// <summary>
        /// 检查输入是否属于指定的特殊类型
        /// </summary>
        /// <param name="input">输入的协议、文件名或路径</param>
        /// <param name="specialType">特殊类型（使用 SpecialProtocolType 常量）</param>
        /// <returns>是否匹配</returns>
        public static bool IsSpecialType(string input, string specialType)
        {
            return ResolveSpecialType(input) == specialType;
        }

        #endregion
    }
}