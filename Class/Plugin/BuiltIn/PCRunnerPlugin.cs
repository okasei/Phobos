using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
using Phobos.Class.Database;
using Phobos.Components.Arcusrix.Runner;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// 快捷方式类型
    /// </summary>
    public enum ShortcutType
    {
        /// <summary>
        /// Windows 快捷方式 (.lnk)
        /// </summary>
        Lnk,

        /// <summary>
        /// URL 快捷方式 (.url)
        /// </summary>
        Url
    }

    /// <summary>
    /// 快捷方式信息
    /// </summary>
    public class ShortcutInfo
    {
        /// <summary>
        /// 快捷方式名称（不含扩展名）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 快捷方式完整路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 目标路径或 URL
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 启动参数
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// 工作目录
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 图标路径
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        /// 缓存的图标文件路径 (PNG)
        /// </summary>
        public string? CachedIconPath { get; set; }

        /// <summary>
        /// 快捷方式类型
        /// </summary>
        public ShortcutType ShortcutType { get; set; }
    }

    /// <summary>
    /// 运行类型
    /// </summary>
    public enum RunType
    {
        /// <summary>
        /// 协议类型 (如 http:, run:, pi:)
        /// </summary>
        Protocol,

        /// <summary>
        /// 文件类型 (如 .txt, .pdf, .exe)
        /// </summary>
        FileType,

        /// <summary>
        /// 特殊类型 (如 text, video, image, web)
        /// </summary>
        SpecialType,

        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 运行请求
    /// </summary>
    public class RunRequest
    {
        /// <summary>
        /// 原始输入
        /// </summary>
        public string RawInput { get; set; } = string.Empty;

        /// <summary>
        /// 运行类型
        /// </summary>
        public RunType Type { get; set; } = RunType.Unknown;

        /// <summary>
        /// 协议名/文件扩展名/特殊类型名 (不含前缀符号)
        /// </summary>
        public string TypeKey { get; set; } = string.Empty;

        /// <summary>
        /// 用于数据库查询的键 (协议带冒号, 文件类型带点, 特殊类型无前缀)
        /// </summary>
        public string DbKey { get; set; } = string.Empty;

        /// <summary>
        /// 参数列表
        /// </summary>
        public List<string> Arguments { get; set; } = new();

        /// <summary>
        /// 完整路径 (文件类型时使用)
        /// </summary>
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// 运行结果
    /// </summary>
    public class RunResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 是否有新的支持此类型的插件出现
        /// </summary>
        public bool HasNewHandlers { get; set; } = false;

        /// <summary>
        /// 新出现的处理器列表 (供 UI 显示选择)
        /// </summary>
        public List<ProtocolHandlerOption> NewHandlers { get; set; } = new();

        /// <summary>
        /// 已有的处理器列表
        /// </summary>
        public List<ProtocolHandlerOption> ExistingHandlers { get; set; } = new();

        /// <summary>
        /// 当前使用的处理器包名
        /// </summary>
        public string? UsedHandlerPackage { get; set; }

        /// <summary>
        /// 是否交还给系统处理
        /// </summary>
        public bool HandledBySystem { get; set; } = false;

        /// <summary>
        /// 是否需要用户选择处理器 (多个可用处理器或有新处理器时为 true)
        /// </summary>
        public bool NeedsUserSelection { get; set; } = false;

        /// <summary>
        /// 原始请求 (用于用户选择后重新执行)
        /// </summary>
        public RunRequest? OriginalRequest { get; set; }

        /// <summary>
        /// 协议/文件类型键 (用于绑定)
        /// </summary>
        public string? ProtocolKey { get; set; }
    }

    /// <summary>
    /// 已知的特殊类型常量
    /// </summary>
    public static class SpecialTypes
    {
        public const string Text = "text";
        public const string Video = "video";
        public const string Image = "image";
        public const string Web = "web";
        public const string Audio = "audio";
        public const string Document = "document";
        public const string Archive = "archive";
        public const string Executable = "executable";

        /// <summary>
        /// 文件扩展名到特殊类型的映射
        /// </summary>
        public static readonly Dictionary<string, string> ExtensionToSpecialType = new(StringComparer.OrdinalIgnoreCase)
        {
            // Text
            { ".txt", Text },
            { ".md", Text },
            { ".log", Text },
            { ".ini", Text },
            { ".cfg", Text },
            { ".conf", Text },
            { ".json", Text },
            { ".xml", Text },
            { ".yaml", Text },
            { ".yml", Text },
            { ".csv", Text },
            { ".rtf", Text },

            // Video
            { ".mp4", Video },
            { ".mkv", Video },
            { ".avi", Video },
            { ".mov", Video },
            { ".wmv", Video },
            { ".flv", Video },
            { ".webm", Video },
            { ".m4v", Video },
            { ".3gp", Video },

            // Image
            { ".jpg", Image },
            { ".jpeg", Image },
            { ".png", Image },
            { ".gif", Image },
            { ".bmp", Image },
            { ".webp", Image },
            { ".svg", Image },
            { ".ico", Image },
            { ".tiff", Image },
            { ".tif", Image },
            { ".raw", Image },

            // Audio
            { ".mp3", Audio },
            { ".wav", Audio },
            { ".flac", Audio },
            { ".aac", Audio },
            { ".ogg", Audio },
            { ".wma", Audio },
            { ".m4a", Audio },

            // Web
            { ".html", Web },
            { ".htm", Web },
            { ".url", Web },
            { ".webloc", Web },

            // Document
            { ".pdf", Document },
            { ".doc", Document },
            { ".docx", Document },
            { ".xls", Document },
            { ".xlsx", Document },
            { ".ppt", Document },
            { ".pptx", Document },
            { ".odt", Document },
            { ".ods", Document },
            { ".odp", Document },

            // Archive
            { ".zip", Archive },
            { ".rar", Archive },
            { ".7z", Archive },
            { ".tar", Archive },
            { ".gz", Archive },
            { ".bz2", Archive },

            // Executable
            { ".exe", Executable },
            { ".msi", Executable },
            { ".bat", Executable },
            { ".cmd", Executable },
            { ".ps1", Executable },
            { ".sh", Executable },
        };

        /// <summary>
        /// 所有已知的特殊类型
        /// </summary>
        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            Text, Video, Image, Web, Audio, Document, Archive, Executable
        };

        /// <summary>
        /// 获取文件扩展名对应的特殊类型
        /// </summary>
        public static string? GetSpecialTypeForExtension(string extension)
        {
            if (ExtensionToSpecialType.TryGetValue(extension, out var specialType))
                return specialType;
            return null;
        }
    }

    /// <summary>
    /// 系统处理器包名常量
    /// </summary>
    public static class SystemHandlers
    {
        /// <summary>
        /// Windows 系统处理器
        /// </summary>
        public const string Windows = "com.microsoft.windows";
    }

    /// <summary>
    /// Phobos Runner 插件 - 协议/文件类型运行器
    /// 负责解析和执行协议链接、文件类型关联
    /// 提供居中输入框 GUI 用于快速启动
    /// </summary>
    public class PCRunnerPlugin : PCPluginBase
    {
        private PCORunner? _runnerWindow;

        /// <summary>
        /// 从 PMDatabase 获取数据库实例
        /// </summary>
        private PCSqliteDatabase? Database => Manager.Database.PMDatabase.Instance.Database;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Runner",
            PackageName = "com.phobos.runner",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_runner_secret_r8un2n3r9x",
            DatabaseKey = "PRunner",
            Icon = "Assets/runner-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true, // 启用 GUI
            Entry = "runner://show",
            PreferredWidth = 600,
            PreferredHeight = 80,
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Runner is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Runner is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "运行器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "運行器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "ランナーは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Runner" },
                { "zh-CN", "运行器" },
                { "zh-TW", "運行器" },
                { "ja-JP", "ランナー" },
                { "ko-KR", "러너" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Protocol and file type runner" },
                { "zh-CN", "协议和文件类型运行器" },
                { "zh-TW", "協議和檔案類型運行器" },
                { "ja-JP", "プロトコルとファイルタイプランナー" },
                { "ko-KR", "프로토콜 및 파일 유형 러너" }
            }
        };

        // 使用自定义窗口，不通过 ContentArea
        public override FrameworkElement? ContentArea => null;

        /// <summary>
        /// 缓存的快捷方式列表
        /// </summary>
        private List<ShortcutInfo>? _cachedShortcuts;
        private DateTime _lastShortcutScan = DateTime.MinValue;
        private readonly TimeSpan _shortcutCacheExpiry = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 图标缓存目录路径
        /// </summary>
        private static readonly string IconCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Phobos", "Cache", "Icon");

        #region Win32 API for Icon Extraction

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        #endregion

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // Runner 本身注册 run: 协议
            await Link(new LinkAssociation
            {
                Protocol = "run:",
                Name = "RunnerHandler_General",
                Description = "Phobos Runner Protocol Handler",
                Command = "run://v1?cmd=%0"
            });

            return await base.OnInstall(args);
        }

        /// <summary>
        /// 启动入口 - 无参数时显示 GUI，有参数时直接运行
        /// </summary>
        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            await RefreshShortcutCache();
            EnsureIconCacheDirectory();

            // 如果没有传入参数，显示 Runner GUI
            if (args.Length == 0)
            {
                return await ShowRunnerWindow();
            }

            // 获取第一个参数作为输入
            var input = args[0]?.ToString();
            if (string.IsNullOrWhiteSpace(input))
            {
                return await ShowRunnerWindow();
            }

            // 检查是否是 show 命令
            if (input.Equals("show", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("runner://show", StringComparison.OrdinalIgnoreCase))
            {
                return await ShowRunnerWindow();
            }

            try
            {
                PCLoggerPlugin.Info("Runner", $"OnLaunch with input: {input}");

                // 解析并执行 (不走快捷方式扫描，避免死循环)
                var request = ParseInput(input);
                var result = await ExecuteRequest(request, skipShortcutScan: false);

                // 如果需要用户选择处理器，显示对话框
                if (result.NeedsUserSelection)
                {
                    PCLoggerPlugin.Info("Runner", $"Showing handler selection dialog for {request.DbKey}");
                    result = await ShowHandlerSelectionAndExecute(result);
                }

                var requestResult = new RequestResult
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = new List<object> { result }
                };

                if (result.HasNewHandlers && result.NewHandlers.Count > 0)
                {
                    PCLoggerPlugin.Info("Runner", $"New handlers available for {request.DbKey}: {string.Join(", ", result.NewHandlers.ConvertAll(h => h.PackageName))}");
                }

                return requestResult;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"OnLaunch failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 显示 Runner 窗口
        /// </summary>
        private async Task<RequestResult> ShowRunnerWindow()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_runnerWindow == null || !_runnerWindow.IsLoaded)
                    {
                        _runnerWindow = new PCORunner(this);
                        _runnerWindow.Closed += (s, e) => _runnerWindow = null;
                    }

                    _runnerWindow.Show();
                    _runnerWindow.Activate();
                    _runnerWindow.FocusInput();
                });

                return new RequestResult { Success = true, Message = "Runner window shown" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to show runner window: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 运行入口 - 解析并执行
        /// </summary>
        public override async Task<RequestResult> Run(params object[] args)
        {
            if (args.Length == 0 || args[0] is not string input)
            {
                return new RequestResult { Success = false, Message = "No input provided" };
            }

            // 检查是否有 skipShortcutScan 标志
            bool skipShortcutScan = args.Length > 1 && args[1] is bool skip && skip;

            try
            {
                var request = ParseInput(input);
                var result = await ExecuteRequest(request, skipShortcutScan);

                return new RequestResult
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = new List<object> { result }
                };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Run failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        #region 解析逻辑

        /// <summary>
        /// 解析输入, 判断类型
        /// </summary>
        public RunRequest ParseInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new RunRequest { RawInput = input, Type = RunType.Unknown };
            }

            input = input.Trim();

            // 1. 检查是否是协议类型
            // 格式: protocol(args,...) 或 protocol://... 或 #protocol#args...
            var protocolRequest = TryParseProtocol(input);
            if (protocolRequest != null)
            {
                return protocolRequest;
            }

            // 2. 检查是否是本地文件路径
            var fileRequest = TryParseFilePath(input);
            if (fileRequest != null)
            {
                return fileRequest;
            }

            // 3. 检查是否是特殊类型 (直接指定 text, video 等)
            var specialRequest = TryParseSpecialType(input);
            if (specialRequest != null)
            {
                return specialRequest;
            }

            // 未知类型
            return new RunRequest { RawInput = input, Type = RunType.Unknown };
        }

        /// <summary>
        /// 尝试解析为协议类型
        /// </summary>
        private RunRequest? TryParseProtocol(string input)
        {
            // 格式1: protocol(args,...)
            var funcMatch = Regex.Match(input, @"^([a-zA-Z][a-zA-Z0-9]*)\((.*)?\)$");
            if (funcMatch.Success)
            {
                var protocol = funcMatch.Groups[1].Value.ToLowerInvariant();
                var argsStr = funcMatch.Groups[2].Value;
                var args = ParseArguments(argsStr);

                return new RunRequest
                {
                    RawInput = input,
                    Type = RunType.Protocol,
                    TypeKey = protocol,
                    DbKey = protocol + ":",
                    Arguments = args
                };
            }

            // 格式2: protocol://... 或 protocol:...
            var uriMatch = Regex.Match(input, @"^([a-zA-Z][a-zA-Z0-9]*):(.*)$");
            if (uriMatch.Success)
            {
                var protocol = uriMatch.Groups[1].Value.ToLowerInvariant();
                var remainder = uriMatch.Groups[2].Value;

                // 排除 Windows 盘符路径 (如 C:\...)
                if (protocol.Length == 1 && char.IsLetter(protocol[0]) && remainder.StartsWith("\\"))
                {
                    return null; // 这是文件路径, 不是协议
                }

                // 去除前导 //
                if (remainder.StartsWith("//"))
                {
                    remainder = remainder[2..];
                }

                // 解析 query string 或路径部分作为参数
                var args = ParseUriArguments(remainder);

                return new RunRequest
                {
                    RawInput = input,
                    Type = RunType.Protocol,
                    TypeKey = protocol,
                    DbKey = protocol + ":",
                    Arguments = args
                };
            }

            // 格式3: #protocol#args...
            var hashMatch = Regex.Match(input, @"^#([a-zA-Z][a-zA-Z0-9]*)#(.*)$");
            if (hashMatch.Success)
            {
                var protocol = hashMatch.Groups[1].Value.ToLowerInvariant();
                var argsStr = hashMatch.Groups[2].Value;

                return new RunRequest
                {
                    RawInput = input,
                    Type = RunType.Protocol,
                    TypeKey = protocol,
                    DbKey = protocol + ":",
                    Arguments = new List<string> { argsStr }
                };
            }

            return null;
        }

        /// <summary>
        /// 尝试解析为文件路径
        /// </summary>
        private RunRequest? TryParseFilePath(string input)
        {
            // 检查是否是本地文件路径 (排除 ftp://, http:// 等远程路径)
            if (input.Contains("://") && !input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                // 这是远程协议, 解析为协议类型
                return TryParseProtocol(input);
            }

            // file:// 协议转换为本地路径
            if (input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                input = input[7..];
                // 处理 file:///C:/... 格式
                if (input.StartsWith("/") && input.Length > 2 && input[2] == ':')
                {
                    input = input[1..];
                }
            }

            // 检查是否是有效的本地路径格式
            bool isLocalPath = false;

            // Windows 绝对路径: C:\... 或 C:/...
            if (input.Length >= 3 && char.IsLetter(input[0]) && input[1] == ':' && (input[2] == '\\' || input[2] == '/'))
            {
                isLocalPath = true;
            }
            // UNC 路径: \\server\share
            else if (input.StartsWith("\\\\"))
            {
                isLocalPath = true;
            }
            // 相对路径: .\... 或 ..\...
            else if (input.StartsWith(".\\") || input.StartsWith("./") || input.StartsWith("..\\") || input.StartsWith("../"))
            {
                isLocalPath = true;
                input = Path.GetFullPath(input);
            }

            if (!isLocalPath)
            {
                return null;
            }

            // 获取文件扩展名
            var extension = Path.GetExtension(input);
            if (string.IsNullOrEmpty(extension))
            {
                // 无扩展名, 可能是目录或未知类型
                return new RunRequest
                {
                    RawInput = input,
                    Type = RunType.FileType,
                    TypeKey = string.Empty,
                    DbKey = string.Empty,
                    FilePath = input,
                    Arguments = new List<string> { input }
                };
            }

            // 扩展名转小写, 保留点号
            extension = extension.ToLowerInvariant();

            return new RunRequest
            {
                RawInput = input,
                Type = RunType.FileType,
                TypeKey = extension.TrimStart('.'),
                DbKey = extension,
                FilePath = input,
                Arguments = new List<string> { input }
            };
        }

        /// <summary>
        /// 尝试解析为特殊类型
        /// </summary>
        private RunRequest? TryParseSpecialType(string input)
        {
            // 检查是否是已知的特殊类型名
            var lower = input.ToLowerInvariant();
            if (SpecialTypes.All.Contains(lower))
            {
                return new RunRequest
                {
                    RawInput = input,
                    Type = RunType.SpecialType,
                    TypeKey = lower,
                    DbKey = lower,
                    Arguments = new List<string>()
                };
            }

            return null;
        }

        /// <summary>
        /// 解析逗号分隔的参数列表
        /// </summary>
        private List<string> ParseArguments(string argsStr)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(argsStr))
                return result;

            // 支持引号包裹的参数
            var matches = Regex.Matches(argsStr, @"(?:""([^""]*)""|'([^']*)'|([^,]+))");
            foreach (Match match in matches)
            {
                var value = match.Groups[1].Success ? match.Groups[1].Value :
                            match.Groups[2].Success ? match.Groups[2].Value :
                            match.Groups[3].Value.Trim();
                result.Add(value);
            }

            return result;
        }

        /// <summary>
        /// 解析 URI 参数
        /// </summary>
        private List<string> ParseUriArguments(string remainder)
        {
            var result = new List<string>();

            // 分离路径和 query string
            var qIndex = remainder.IndexOf('?');
            if (qIndex >= 0)
            {
                var path = remainder[..qIndex];
                var query = remainder[(qIndex + 1)..];

                if (!string.IsNullOrEmpty(path))
                {
                    result.Add(Uri.UnescapeDataString(path));
                }

                // 解析 query string
                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eqIndex = pair.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var value = Uri.UnescapeDataString(pair[(eqIndex + 1)..]);
                        result.Add(value);
                    }
                    else
                    {
                        result.Add(Uri.UnescapeDataString(pair));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(remainder))
            {
                result.Add(Uri.UnescapeDataString(remainder));
            }

            return result;
        }

        #endregion

        #region 执行逻辑

        /// <summary>
        /// 执行运行请求
        /// </summary>
        public async Task<RunResult> ExecuteRequest(RunRequest request, bool skipShortcutScan = false)
        {
            return request.Type switch
            {
                RunType.Protocol => await ExecuteProtocol(request, skipShortcutScan),
                RunType.FileType => await ExecuteFileType(request, skipShortcutScan),
                RunType.SpecialType => await ExecuteSpecialType(request),
                _ => await FallbackToSystem(request, skipShortcutScan)
            };
        }

        /// <summary>
        /// 执行协议类型
        /// </summary>
        private async Task<RunResult> ExecuteProtocol(RunRequest request, bool skipShortcutScan = false)
        {
            if (Database == null)
            {
                PCLoggerPlugin.Warning("Runner", "ExecuteProtocol: Database is null, falling back to system");
                return await FallbackToSystem(request, skipShortcutScan);
            }

            // 协议使用 DbKey (带冒号，如 "seq:")，与数据库中注册的格式一致
            var protocol = request.DbKey;
            PCLoggerPlugin.Info("Runner", $"ExecuteProtocol: protocol='{protocol}', RawInput='{request.RawInput}'");

            // 1. 查找 Phobos_Shell 中是否有绑定 (AssociatedItem 存储的是 Phobos_Protocol.UUID)
            var shellResult = await Database.ExecuteQuery(
                "SELECT AssociatedItem, UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                new Dictionary<string, object> { { "@protocol", protocol } });

            if (shellResult != null && shellResult.Count > 0)
            {
                var protocolUUID = shellResult[0]["AssociatedItem"]?.ToString();
                var shellUpdateTime = DateTime.TryParse(shellResult[0]["UpdateTime"]?.ToString(), out var dt) ? dt : DateTime.MinValue;

                if (!string.IsNullOrEmpty(protocolUUID))
                {
                    // 通过 Phobos_Protocol.UUID 查找关联项详情
                    var associatedResult = await Database.ExecuteQuery(
                        @"SELECT p.AssociatedItem, ai.PackageName, ai.Command
                          FROM Phobos_Protocol p
                          INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                          WHERE p.UUID = @uuid",
                        new Dictionary<string, object> { { "@uuid", protocolUUID } });

                    if (associatedResult != null && associatedResult.Count > 0)
                    {
                        var packageName = associatedResult[0]["PackageName"]?.ToString();
                        var command = associatedResult[0]["Command"]?.ToString();

                        // 检查是否是系统处理器
                        if (packageName == SystemHandlers.Windows)
                        {
                            return await FallbackToSystem(request, skipShortcutScan);
                        }

                        if (!string.IsNullOrEmpty(packageName))
                        {
                            // 2. 检查是否有更新的处理器出现
                            var checkResult = await CheckForNewHandlers(protocol, shellUpdateTime);

                            // 如果有新的处理器出现
                            if (checkResult.hasNew)
                            {
                                // 检查是否只是当前绑定的插件自身更新了（UUID 相同）
                                // 如果 newHandlers 中只有当前绑定的项，则只需更新时间戳
                                var reallyNewHandlers = checkResult.newHandlers
                                    .Where(h => h.UUID != protocolUUID)
                                    .ToList();

                                if (reallyNewHandlers.Count > 0)
                                {
                                    // 有真正的新处理器，需要让用户重新选择
                                    return new RunResult
                                    {
                                        Success = false,
                                        Message = "New handlers available, please select one",
                                        NeedsUserSelection = true,
                                        HasNewHandlers = true,
                                        NewHandlers = checkResult.newHandlers,
                                        ExistingHandlers = checkResult.existingHandlers,
                                        OriginalRequest = request,
                                        ProtocolKey = protocol
                                    };
                                }
                                else
                                {
                                    // 只是当前绑定的插件更新了，更新 Shell 的时间戳
                                    await UpdateShellTimestamp(protocol);
                                    PCLoggerPlugin.Info("Runner", $"ExecuteProtocol: Same handler updated, refreshed shell timestamp for '{protocol}'");
                                }
                            }

                            // 3. 没有新处理器（或只是同一插件更新），直接启动关联的插件
                            var launchResult = await LaunchPluginHandler(packageName, request, command);

                            return new RunResult
                            {
                                Success = launchResult.Success,
                                Message = launchResult.Message,
                                UsedHandlerPackage = packageName,
                                HasNewHandlers = false,
                                NewHandlers = checkResult.newHandlers,
                                ExistingHandlers = checkResult.existingHandlers
                            };
                        }
                    }
                }
            }

            // 没有绑定, 查找支持此协议的插件
            PCLoggerPlugin.Info("Runner", $"ExecuteProtocol: No shell binding, searching for handlers...");
            var handlers = await GetProtocolHandlers(protocol);
            PCLoggerPlugin.Info("Runner", $"ExecuteProtocol: Found {handlers.Count} handlers for '{protocol}'");

            if (handlers.Count == 0)
            {
                // 没有任何插件支持, 交还系统
                PCLoggerPlugin.Warning("Runner", $"ExecuteProtocol: No handlers found for '{protocol}', falling back to system");
                return await FallbackToSystem(request, skipShortcutScan);
            }
            else if (handlers.Count == 1)
            {
                // 只有一个插件支持, 自动绑定并启动
                var handler = handlers[0];
                await AutoBindProtocol(protocol, handler.UUID, handler.PackageName);

                var launchResult = await LaunchPluginHandler(handler.PackageName, request, handler.Command);
                return new RunResult
                {
                    Success = launchResult.Success,
                    Message = launchResult.Message,
                    UsedHandlerPackage = handler.PackageName
                };
            }
            else
            {
                // 多个插件支持, 返回列表供用户选择，不自动绑定
                return new RunResult
                {
                    Success = false,
                    Message = "Multiple handlers available, please select one",
                    NeedsUserSelection = true,
                    HasNewHandlers = false,
                    ExistingHandlers = handlers,
                    OriginalRequest = request,
                    ProtocolKey = protocol
                };
            }
        }

        /// <summary>
        /// 执行文件类型
        /// </summary>
        private async Task<RunResult> ExecuteFileType(RunRequest request, bool skipShortcutScan = false)
        {
            if (Database == null || string.IsNullOrEmpty(request.DbKey))
            {
                return await FallbackToSystem(request, skipShortcutScan);
            }

            // 首先检查特殊类型关联
            var specialType = SpecialTypes.GetSpecialTypeForExtension(request.DbKey);
            if (!string.IsNullOrEmpty(specialType))
            {
                // 尝试用特殊类型查找
                var specialResult = await ExecuteTypeAssociation(specialType, request);
                if (specialResult.Success)
                {
                    return specialResult;
                }
            }

            // 然后检查具体文件扩展名关联
            var extResult = await ExecuteTypeAssociation(request.DbKey, request);
            if (extResult.Success)
            {
                return extResult;
            }

            // 都没有找到, 交还系统
            return await FallbackToSystem(request, skipShortcutScan);
        }

        /// <summary>
        /// 执行特殊类型
        /// </summary>
        private async Task<RunResult> ExecuteSpecialType(RunRequest request)
        {
            return await ExecuteTypeAssociation(request.DbKey, request);
        }

        /// <summary>
        /// 执行类型关联 (文件类型或特殊类型)
        /// </summary>
        private async Task<RunResult> ExecuteTypeAssociation(string typeKey, RunRequest request)
        {
            if (Database == null)
            {
                return new RunResult { Success = false, Message = "Database not initialized" };
            }

            // 查找 Phobos_Shell 中的绑定 (AssociatedItem 存储的是 Phobos_Protocol.UUID)
            var shellResult = await Database.ExecuteQuery(
                "SELECT AssociatedItem, UpdateTime FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                new Dictionary<string, object> { { "@protocol", typeKey } });

            if (shellResult != null && shellResult.Count > 0)
            {
                var protocolUUID = shellResult[0]["AssociatedItem"]?.ToString();
                var shellUpdateTime = DateTime.TryParse(shellResult[0]["UpdateTime"]?.ToString(), out var dt) ? dt : DateTime.MinValue;

                if (!string.IsNullOrEmpty(protocolUUID))
                {
                    // 通过 Phobos_Protocol.UUID 查找关联项详情
                    var associatedResult = await Database.ExecuteQuery(
                        @"SELECT p.AssociatedItem, ai.PackageName, ai.Command
                          FROM Phobos_Protocol p
                          INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                          WHERE p.UUID = @uuid",
                        new Dictionary<string, object> { { "@uuid", protocolUUID } });

                    if (associatedResult != null && associatedResult.Count > 0)
                    {
                        var packageName = associatedResult[0]["PackageName"]?.ToString();
                        var command = associatedResult[0]["Command"]?.ToString();

                        if (packageName == SystemHandlers.Windows)
                        {
                            return await FallbackToSystem(request);
                        }

                        if (!string.IsNullOrEmpty(packageName))
                        {
                            var checkResult = await CheckForNewHandlers(typeKey, shellUpdateTime);

                            // 如果有新的处理器出现
                            if (checkResult.hasNew)
                            {
                                // 检查是否只是当前绑定的插件自身更新了（UUID 相同）
                                var reallyNewHandlers = checkResult.newHandlers
                                    .Where(h => h.UUID != protocolUUID)
                                    .ToList();

                                if (reallyNewHandlers.Count > 0)
                                {
                                    // 有真正的新处理器，需要让用户重新选择
                                    return new RunResult
                                    {
                                        Success = false,
                                        Message = "New handlers available, please select one",
                                        NeedsUserSelection = true,
                                        HasNewHandlers = true,
                                        NewHandlers = checkResult.newHandlers,
                                        ExistingHandlers = checkResult.existingHandlers,
                                        OriginalRequest = request,
                                        ProtocolKey = typeKey
                                    };
                                }
                                else
                                {
                                    // 只是当前绑定的插件更新了，更新 Shell 的时间戳
                                    await UpdateShellTimestamp(typeKey);
                                    PCLoggerPlugin.Info("Runner", $"ExecuteByType: Same handler updated, refreshed shell timestamp for '{typeKey}'");
                                }
                            }

                            // 没有新处理器（或只是同一插件更新），直接启动
                            var launchResult = await LaunchPluginHandler(packageName, request, command);

                            return new RunResult
                            {
                                Success = launchResult.Success,
                                Message = launchResult.Message,
                                UsedHandlerPackage = packageName,
                                HasNewHandlers = false,
                                NewHandlers = checkResult.newHandlers,
                                ExistingHandlers = checkResult.existingHandlers
                            };
                        }
                    }
                }
            }

            // 没有绑定, 查找支持此类型的插件
            var handlers = await GetProtocolHandlers(typeKey);

            if (handlers.Count == 0)
            {
                return new RunResult { Success = false, Message = $"No handler found for {typeKey}" };
            }
            else if (handlers.Count == 1)
            {
                // 只有一个处理器，自动绑定并启动
                var handler = handlers[0];
                await AutoBindProtocol(typeKey, handler.UUID, handler.PackageName);

                var launchResult = await LaunchPluginHandler(handler.PackageName, request, handler.Command);
                return new RunResult
                {
                    Success = launchResult.Success,
                    Message = launchResult.Message,
                    UsedHandlerPackage = handler.PackageName
                };
            }
            else
            {
                // 多个处理器，返回列表供用户选择，不自动绑定
                return new RunResult
                {
                    Success = false,
                    Message = "Multiple handlers available, please select one",
                    NeedsUserSelection = true,
                    HasNewHandlers = false,
                    ExistingHandlers = handlers,
                    OriginalRequest = request,
                    ProtocolKey = typeKey
                };
            }
        }

        /// <summary>
        /// 检查是否有新的处理器出现
        /// </summary>
        private async Task<(bool hasNew, List<ProtocolHandlerOption> newHandlers, List<ProtocolHandlerOption> existingHandlers)>
            CheckForNewHandlers(string protocol, DateTime lastCheckTime)
        {
            var newHandlers = new List<ProtocolHandlerOption>();
            var existingHandlers = new List<ProtocolHandlerOption>();

            if (Database == null)
            {
                return (false, newHandlers, existingHandlers);
            }

            var handlers = await Database.ExecuteQuery(
                @"SELECT p.UUID, p.Protocol, p.AssociatedItem, p.UpdateTime, 
                         ai.PackageName, ai.Description, ai.Command
                  FROM Phobos_Protocol p
                  INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                  WHERE p.Protocol = @protocol COLLATE NOCASE
                  ORDER BY p.UpdateTime DESC",
                new Dictionary<string, object> { { "@protocol", protocol } });

            foreach (var handler in handlers ?? new List<Dictionary<string, object>>())
            {
                var updateTime = DateTime.TryParse(handler["UpdateTime"]?.ToString(), out var dt) ? dt : DateTime.MinValue;
                var option = new ProtocolHandlerOption
                {
                    UUID = handler["UUID"]?.ToString() ?? string.Empty,
                    Protocol = handler["Protocol"]?.ToString() ?? string.Empty,
                    AssociatedItem = handler["AssociatedItem"]?.ToString() ?? string.Empty,
                    PackageName = handler["PackageName"]?.ToString() ?? string.Empty,
                    Description = handler["Description"]?.ToString() ?? string.Empty,
                    Command = handler["Command"]?.ToString() ?? string.Empty,
                    UpdateTime = updateTime,
                    IsUpdated = updateTime > lastCheckTime
                };

                if (option.IsUpdated)
                {
                    newHandlers.Add(option);
                }
                else
                {
                    existingHandlers.Add(option);
                }
            }

            return (newHandlers.Count > 0, newHandlers, existingHandlers);
        }

        /// <summary>
        /// 获取协议/类型的所有处理器
        /// </summary>
        private async Task<List<ProtocolHandlerOption>> GetProtocolHandlers(string protocol)
        {
            var result = new List<ProtocolHandlerOption>();

            if (Database == null)
            {
                return result;
            }

            var handlers = await Database.ExecuteQuery(
                @"SELECT p.UUID, p.Protocol, p.AssociatedItem, p.UpdateTime, 
                         ai.PackageName, ai.Description, ai.Command
                  FROM Phobos_Protocol p
                  INNER JOIN Phobos_AssociatedItem ai ON p.AssociatedItem = ai.Name
                  WHERE p.Protocol = @protocol COLLATE NOCASE
                  ORDER BY p.UpdateTime DESC",
                new Dictionary<string, object> { { "@protocol", protocol } });

            foreach (var handler in handlers ?? new List<Dictionary<string, object>>())
            {
                result.Add(new ProtocolHandlerOption
                {
                    UUID = handler["UUID"]?.ToString() ?? string.Empty,
                    Protocol = handler["Protocol"]?.ToString() ?? string.Empty,
                    AssociatedItem = handler["AssociatedItem"]?.ToString() ?? string.Empty,
                    PackageName = handler["PackageName"]?.ToString() ?? string.Empty,
                    Description = handler["Description"]?.ToString() ?? string.Empty,
                    Command = handler["Command"]?.ToString() ?? string.Empty,
                    UpdateTime = DateTime.TryParse(handler["UpdateTime"]?.ToString(), out var dt) ? dt : DateTime.MinValue
                });
            }

            return result;
        }

        /// <summary>
        /// 自动绑定协议/类型到插件 (存储 Phobos_Protocol.UUID)
        /// </summary>
        /// <param name="protocol">协议名/文件类型</param>
        /// <param name="protocolUUID">Phobos_Protocol 表中的 UUID</param>
        /// <param name="packageName">插件包名</param>
        private async Task AutoBindProtocol(string protocol, string protocolUUID, string packageName)
        {
            if (Database == null)
            {
                return;
            }

            try
            {
                // 检查是否已存在绑定
                var existing = await Database.ExecuteQuery(
                    "SELECT AssociatedItem FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocol } });

                if (existing != null && existing.Count > 0)
                {
                    // 更新现有绑定 - AssociatedItem 存储的是 Phobos_Protocol.UUID
                    var oldItem = existing[0]["AssociatedItem"]?.ToString() ?? string.Empty;
                    await Database.ExecuteNonQuery(
                        @"UPDATE Phobos_Shell SET
                            AssociatedItem = @newItem,
                            UpdateUID = @uid,
                            UpdateTime = datetime('now'),
                            LastValue = @lastValue
                          WHERE Protocol = @protocol COLLATE NOCASE",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocol },
                            { "@newItem", protocolUUID },
                            { "@uid", packageName },
                            { "@lastValue", oldItem }
                        });
                }
                else
                {
                    // 插入新绑定 - AssociatedItem 存储的是 Phobos_Protocol.UUID
                    await Database.ExecuteNonQuery(
                        @"INSERT INTO Phobos_Shell (Protocol, AssociatedItem, UpdateUID, UpdateTime, LastValue)
                          VALUES (@protocol, @protocolUUID, @uid, datetime('now'), '')",
                        new Dictionary<string, object>
                        {
                            { "@protocol", protocol },
                            { "@protocolUUID", protocolUUID },
                            { "@uid", packageName }
                        });
                }

                PCLoggerPlugin.Info("Runner", $"Auto-bound {protocol} to {packageName} (UUID: {protocolUUID})");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to auto-bind: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新 Shell 绑定的时间戳（用于同一插件更新时刷新检查时间）
        /// </summary>
        private async Task UpdateShellTimestamp(string protocol)
        {
            if (Database == null) return;

            try
            {
                await Database.ExecuteNonQuery(
                    "UPDATE Phobos_Shell SET UpdateTime = datetime('now') WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocol } });
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to update shell timestamp: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除协议/类型的默认绑定 (用于"仅一次"选择后取消默认关联)
        /// </summary>
        public async Task RemoveDefaultBinding(string protocol)
        {
            if (Database == null)
            {
                return;
            }

            try
            {
                await Database.ExecuteNonQuery(
                    "DELETE FROM Phobos_Shell WHERE Protocol = @protocol COLLATE NOCASE",
                    new Dictionary<string, object> { { "@protocol", protocol } });

                PCLoggerPlugin.Info("Runner", $"Removed default binding for {protocol}");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to remove default binding: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动插件处理器
        /// </summary>
        private async Task<RequestResult> LaunchPluginHandler(string packageName, RunRequest request, string? commandTemplate)
        {
            try
            {
                // 构建参数
                var args = new List<object>();

                // 替换命令模板中的占位符
                if (!string.IsNullOrEmpty(commandTemplate))
                {
                    var command = commandTemplate;
                    for (int i = 0; i < request.Arguments.Count; i++)
                    {
                        command = command.Replace($"%{i}", request.Arguments[i]);
                    }
                    args.Add(command);
                }

                // 添加原始参数
                args.AddRange(request.Arguments);

                // 调用 PMPlugin.Launch
                var result = await PMPlugin.Instance.Launch(packageName, args.ToArray());

                if (!result.Success)
                {
                    // Launch 失败, 尝试 Run
                    result = await PMPlugin.Instance.Run(packageName, args.ToArray());
                }

                return result;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to launch handler: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 交还给系统处理
        /// </summary>
        private async Task<RunResult> FallbackToSystem(RunRequest request, bool skipShortcutScan = false)
        {
            try
            {
                // 如果没有跳过快捷方式扫描，先尝试在开始菜单/桌面查找匹配的快捷方式
                if (!skipShortcutScan)
                {
                    var shortcut = await FindMatchingShortcut(request.RawInput);
                    if (shortcut != null)
                    {
                        PCLoggerPlugin.Info("Runner", $"Found matching shortcut: {shortcut.Name} -> {shortcut.FullPath}");

                        // 直接用 explorer 启动快捷方式文件本身
                        var epsi = new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{shortcut.FullPath}\"",
                            UseShellExecute = true
                        };
                        Process.Start(epsi);

                        return new RunResult
                        {
                            Success = true,
                            Message = $"Launched shortcut: {shortcut.Name}",
                            HandledBySystem = true
                        };
                    }
                }

                string target;
                bool useExplorer = false;

                switch (request.Type)
                {
                    case RunType.Protocol:
                        // 协议直接传递
                        target = request.RawInput;
                        break;

                    case RunType.FileType:
                        // 文件路径需要用 explorer 打开
                        target = request.FilePath ?? request.RawInput;
                        useExplorer = true;
                        break;

                    default:
                        target = request.RawInput;
                        break;
                }

                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true
                };

                if (useExplorer)
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = $"\"{target}\"";
                }
                else
                {
                    psi.FileName = target;
                }

                Process.Start(psi);

                PCLoggerPlugin.Info("Runner", $"Handed off to system: {target}");

                return await Task.FromResult(new RunResult
                {
                    Success = true,
                    Message = "Handled by system",
                    HandledBySystem = true
                });
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"System fallback failed: {ex.Message}");
                return new RunResult
                {
                    Success = false,
                    Message = $"System fallback failed: {ex.Message}",
                    HandledBySystem = true
                };
            }
        }

        #endregion

        #region 快捷方式扫描

        /// <summary>
        /// 查找匹配的快捷方式
        /// </summary>
        private async Task<ShortcutInfo?> FindMatchingShortcut(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            // 刷新缓存（如果过期）
            await RefreshShortcutCache();

            if (_cachedShortcuts == null || _cachedShortcuts.Count == 0)
            {
                PCLoggerPlugin.Info("Runner", "No shortcuts in cache");
                return null;
            }

            PCLoggerPlugin.Info("Runner", $"Searching for '{searchText}' in {_cachedShortcuts.Count} shortcuts");

            // 打印前10个快捷方式名称用于调试
            var sampleNames = string.Join(", ", _cachedShortcuts.Take(10).Select(s => s.Name));
            PCLoggerPlugin.Info("Runner", $"Sample shortcuts: {sampleNames}");

            // 忽略大小写查找名称包含输入内容的快捷方式
            var matches = _cachedShortcuts
                .Where(s => s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            PCLoggerPlugin.Info("Runner", $"Found {matches.Count} matches");

            if (matches.Count > 0)
            {
                PCLoggerPlugin.Info("Runner", $"First match: {matches[0].Name} -> {matches[0].FullPath}");
            }

            // 返回第一个匹配项（可以扩展为返回多个供选择）
            return matches.FirstOrDefault();
        }

        /// <summary>
        /// 获取所有匹配的快捷方式（供 GUI 使用）
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="maxResults">最大返回数量，默认20</param>
        public async Task<List<ShortcutInfo>> FindAllMatchingShortcuts(string searchText, int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<ShortcutInfo>();

            await RefreshShortcutCache();

            if (_cachedShortcuts == null)
                return new List<ShortcutInfo>();

            // 忽略大小写查找，限制返回数量
            return _cachedShortcuts
                .Where(s => s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase))
                .ThenBy(s => s.Name.Length)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// 刷新快捷方式缓存
        /// </summary>
        private async Task RefreshShortcutCache()
        {
            if (_cachedShortcuts != null && DateTime.Now - _lastShortcutScan < _shortcutCacheExpiry)
                return;

            // COM objects like WshShell require STA thread
            var tcs = new TaskCompletionSource<List<ShortcutInfo>>();
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var shortcuts = new List<ShortcutInfo>();

                    // 扫描开始菜单
                    var startMenuPaths = new[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
                    };

                    PCLoggerPlugin.Info("Runner", $"Start menu paths: {string.Join(", ", startMenuPaths)}");

                    foreach (var startMenuPath in startMenuPaths)
                    {
                        PCLoggerPlugin.Info("Runner", $"Checking start menu path: {startMenuPath}, exists: {Directory.Exists(startMenuPath)}");
                        if (Directory.Exists(startMenuPath))
                        {
                            ScanDirectory(startMenuPath, shortcuts);
                        }
                    }

                    // 扫描桌面
                    var desktopPaths = new[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                    };

                    PCLoggerPlugin.Info("Runner", $"Desktop paths: {string.Join(", ", desktopPaths)}");

                    foreach (var desktopPath in desktopPaths)
                    {
                        PCLoggerPlugin.Info("Runner", $"Checking desktop path: {desktopPath}, exists: {Directory.Exists(desktopPath)}");
                        if (Directory.Exists(desktopPath))
                        {
                            ScanDirectory(desktopPath, shortcuts, recursive: false);
                        }
                    }

                    // 统计图标缓存情况
                    var cachedIconCount = shortcuts.Count(s => !string.IsNullOrEmpty(s.CachedIconPath));
                    PCLoggerPlugin.Info("Runner", $"Scanned {shortcuts.Count} shortcuts, cached {cachedIconCount} icons to {IconCacheDirectory}");

                    tcs.SetResult(shortcuts);
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Runner", $"Failed to scan shortcuts: {ex.Message}");
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();

            try
            {
                _cachedShortcuts = await tcs.Task;
                _lastShortcutScan = DateTime.Now;
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Shortcut scan failed: {ex.Message}");
                _cachedShortcuts = new List<ShortcutInfo>();
            }
        }

        /// <summary>
        /// 扫描目录中的快捷方式
        /// </summary>
        private void ScanDirectory(string path, List<ShortcutInfo> shortcuts, bool recursive = true)
        {
            try
            {
                PCLoggerPlugin.Info("Runner", $"Scanning directory: {path}, recursive: {recursive}");

                // 扫描当前目录的 .lnk 文件
                try
                {
                    var lnkFiles = Directory.GetFiles(path, "*.lnk", SearchOption.TopDirectoryOnly);
                    foreach (var lnkFile in lnkFiles)
                    {
                        try
                        {
                            var shortcut = ResolveShortcut(lnkFile);
                            if (shortcut != null)
                            {
                                shortcuts.Add(shortcut);
                            }
                        }
                        catch (Exception ex)
                        {
                            PCLoggerPlugin.Error("Runner", $"Failed to resolve shortcut {lnkFile}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 忽略无权访问的目录
                }

                // 扫描当前目录的 .url 文件
                try
                {
                    var urlFiles = Directory.GetFiles(path, "*.url", SearchOption.TopDirectoryOnly);
                    foreach (var urlFile in urlFiles)
                    {
                        try
                        {
                            var shortcut = ResolveUrlShortcut(urlFile);
                            if (shortcut != null)
                            {
                                shortcuts.Add(shortcut);
                            }
                        }
                        catch (Exception ex)
                        {
                            PCLoggerPlugin.Error("Runner", $"Failed to resolve URL shortcut {urlFile}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 忽略无权访问的目录
                }

                // 递归扫描子目录
                if (recursive)
                {
                    try
                    {
                        foreach (var subDir in Directory.GetDirectories(path))
                        {
                            try
                            {
                                // 跳过符号链接和联接点
                                var dirInfo = new DirectoryInfo(subDir);
                                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                                {
                                    continue;
                                }

                                ScanDirectory(subDir, shortcuts, recursive: true);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // 忽略无权访问的子目录
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 忽略无权获取子目录列表的目录
                    }
                }

                PCLoggerPlugin.Info("Runner", $"Scanned {path}, total shortcuts so far: {shortcuts.Count}");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to scan directory {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析 .lnk 快捷方式
        /// </summary>
        private ShortcutInfo? ResolveShortcut(string lnkPath)
        {
            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(lnkPath);

                var name = System.IO.Path.GetFileNameWithoutExtension(lnkPath);
                var targetPath = shortcut.TargetPath;

                if (string.IsNullOrEmpty(targetPath))
                    return null;

                var info = new ShortcutInfo
                {
                    Name = name,
                    FullPath = lnkPath,
                    TargetPath = targetPath,
                    Arguments = shortcut.Arguments,
                    WorkingDirectory = shortcut.WorkingDirectory,
                    IconPath = shortcut.IconLocation,
                    ShortcutType = ShortcutType.Lnk
                };

                // 缓存图标
                info.CachedIconPath = CacheIcon(info);

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 .url 快捷方式
        /// </summary>
        private ShortcutInfo? ResolveUrlShortcut(string urlPath)
        {
            try
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(urlPath);
                var lines = System.IO.File.ReadAllLines(urlPath);
                string? url = null;
                string? iconPath = null;

                foreach (var line in lines)
                {
                    if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        url = line[4..];
                    }
                    else if (line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase))
                    {
                        iconPath = line[9..];
                    }
                }

                if (string.IsNullOrEmpty(url))
                    return null;

                var info = new ShortcutInfo
                {
                    Name = name,
                    FullPath = urlPath,
                    TargetPath = url,
                    IconPath = iconPath,
                    ShortcutType = ShortcutType.Url
                };

                // 缓存图标
                info.CachedIconPath = CacheIcon(info);

                return info;
            }
            catch
            {
                return null;
            }
        }

        #region 图标缓存

        /// <summary>
        /// 确保图标缓存目录存在
        /// </summary>
        private static void EnsureIconCacheDirectory()
        {
            if (!Directory.Exists(IconCacheDirectory))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Phobos"));
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Phobos","Cache"));
                Directory.CreateDirectory(IconCacheDirectory);
                PCLoggerPlugin.Info("Runner", $"Created icon cache directory: {IconCacheDirectory}");
            }
        }

        /// <summary>
        /// 生成图标缓存文件名 (基于快捷方式路径的哈希)
        /// </summary>
        private static string GetIconCacheFileName(string shortcutPath)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(shortcutPath.ToLowerInvariant()));
            var hashStr = Convert.ToHexStringLower(hash);
            return $"{hashStr}.png";
        }

        /// <summary>
        /// 缓存快捷方式的图标
        /// </summary>
        private string? CacheIcon(ShortcutInfo shortcut)
        {
            try
            {
                EnsureIconCacheDirectory();

                var cacheFileName = GetIconCacheFileName(shortcut.FullPath);
                var cachePath = Path.Combine(IconCacheDirectory, cacheFileName);

                // 检查缓存是否已存在且有效
                if (System.IO.File.Exists(cachePath))
                {
                    var cacheInfo = new FileInfo(cachePath);
                    var shortcutInfo = new FileInfo(shortcut.FullPath);

                    // 如果缓存文件比快捷方式新，直接使用缓存
                    if (cacheInfo.LastWriteTime >= shortcutInfo.LastWriteTime)
                    {
                        return cachePath;
                    }
                }

                // 提取图标句柄
                IntPtr hIcon = IntPtr.Zero;

                // 1. 首先尝试从 IconLocation 提取
                if (!string.IsNullOrEmpty(shortcut.IconPath))
                {
                    hIcon = ExtractIconHandleFromLocation(shortcut.IconPath);
                }

                // 2. 如果失败，尝试从目标文件提取
                if (hIcon == IntPtr.Zero && !string.IsNullOrEmpty(shortcut.TargetPath) && System.IO.File.Exists(shortcut.TargetPath))
                {
                    hIcon = ExtractIconHandleFromFile(shortcut.TargetPath);
                }

                // 3. 如果还是失败，尝试从快捷方式文件本身提取
                if (hIcon == IntPtr.Zero && System.IO.File.Exists(shortcut.FullPath))
                {
                    hIcon = ExtractIconHandleFromFile(shortcut.FullPath);
                }

                if (hIcon != IntPtr.Zero)
                {
                    try
                    {
                        // 保存为 PNG (使用 WPF Imaging)
                        SaveIconHandleAsPng(hIcon, cachePath);
                        PCLoggerPlugin.Info("Runner", $"Cached icon for: {shortcut.Name} -> {cachePath}");
                        return cachePath;
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to cache icon for {shortcut.Name}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从 IconLocation 字符串提取图标句柄 (格式: "path,index")
        /// </summary>
        private IntPtr ExtractIconHandleFromLocation(string iconLocation)
        {
            try
            {
                if (string.IsNullOrEmpty(iconLocation))
                    return IntPtr.Zero;

                // 解析 IconLocation 格式: "C:\path\file.exe,0" 或 "C:\path\file.ico"
                var lastComma = iconLocation.LastIndexOf(',');
                string filePath;
                int iconIndex = 0;

                if (lastComma > 0)
                {
                    filePath = iconLocation[..lastComma].Trim();
                    if (int.TryParse(iconLocation[(lastComma + 1)..].Trim(), out var idx))
                    {
                        iconIndex = idx;
                    }
                }
                else
                {
                    filePath = iconLocation.Trim();
                }

                // 展开环境变量
                filePath = Environment.ExpandEnvironmentVariables(filePath);

                if (!System.IO.File.Exists(filePath))
                    return IntPtr.Zero;

                return ExtractIconHandleFromFile(filePath, iconIndex);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 从文件提取图标句柄
        /// </summary>
        private IntPtr ExtractIconHandleFromFile(string filePath, int iconIndex = 0)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return IntPtr.Zero;

                // 使用 ExtractIconEx 提取大图标
                var largeIcons = new IntPtr[1];
                var smallIcons = new IntPtr[1];

                var count = ExtractIconEx(filePath, iconIndex, largeIcons, smallIcons, 1);

                if (count > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    // 销毁小图标（如果有）
                    if (smallIcons[0] != IntPtr.Zero)
                    {
                        DestroyIcon(smallIcons[0]);
                    }
                    return largeIcons[0];
                }

                // 回退: 使用 ExtractIcon
                var hIcon = ExtractIcon(IntPtr.Zero, filePath, iconIndex);
                if (hIcon != IntPtr.Zero && hIcon.ToInt64() > 1)
                {
                    return hIcon;
                }

                return IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 使用 WPF Imaging 将图标句柄保存为 PNG 文件
        /// </summary>
        private static void SaveIconHandleAsPng(IntPtr hIcon, string outputPath)
        {
            // 使用 Imaging.CreateBitmapSourceFromHIcon 转换为 WPF BitmapSource
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // 使用 PngBitmapEncoder 保存为 PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var fileStream = new FileStream(outputPath, FileMode.Create);
            encoder.Save(fileStream);
        }

        #endregion

        #endregion

        #region 公共方法 (供外部调用)

        /// <summary>
        /// 运行协议链接
        /// </summary>
        public async Task<RunResult> RunProtocol(string protocolUri)
        {
            var request = ParseInput(protocolUri);
            return await ExecuteRequest(request);
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        public async Task<RunResult> OpenFile(string filePath)
        {
            var request = ParseInput(filePath);
            return await ExecuteRequest(request);
        }

        /// <summary>
        /// 设置默认处理器 (使用 Phobos_Protocol.UUID)
        /// </summary>
        /// <param name="protocolOrType">协议名/文件类型</param>
        /// <param name="protocolUUID">Phobos_Protocol 表中的 UUID</param>
        /// <param name="packageName">插件包名</param>
        public async Task<RequestResult> SetDefaultHandler(string protocolOrType, string protocolUUID, string packageName)
        {
            if (Database == null)
            {
                return new RequestResult { Success = false, Message = "Database not initialized" };
            }

            try
            {
                await AutoBindProtocol(protocolOrType, protocolUUID, packageName);
                return new RequestResult { Success = true, Message = $"Set {packageName} as default for {protocolOrType}" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取可用的处理器列表
        /// </summary>
        public async Task<List<ProtocolHandlerOption>> GetAvailableHandlers(string protocolOrType)
        {
            return await GetProtocolHandlers(protocolOrType);
        }

        /// <summary>
        /// 绑定系统处理器
        /// </summary>
        public async Task<RequestResult> BindToSystem(string protocolOrType)
        {
            // 对于系统处理器，我们创建一个特殊的 UUID
            var systemUUID = $"system_{protocolOrType}";
            return await SetDefaultHandler(protocolOrType, systemUUID, SystemHandlers.Windows);
        }

        /// <summary>
        /// 用户选择处理器后执行 (供 UI 调用)
        /// </summary>
        /// <param name="handler">用户选择的处理器</param>
        /// <param name="request">原始请求</param>
        /// <param name="setAsDefault">是否设为默认 (true=总是, false=仅一次)</param>
        public async Task<RunResult> ExecuteWithSelectedHandler(ProtocolHandlerOption handler, RunRequest request, bool setAsDefault)
        {
            // 协议类型保留冒号（如 "seq:"），文件类型和特殊类型直接使用 DbKey
            var protocol = request.DbKey;

            // 检查是否是系统处理器
            if (handler.PackageName == SystemHandlers.Windows)
            {
                if (setAsDefault)
                {
                    await BindToSystem(protocol);
                }
                else
                {
                    // 仅一次，删除现有绑定
                    await RemoveDefaultBinding(protocol);
                }
                return await FallbackToSystem(request, skipShortcutScan: true);
            }

            // 如果选择"总是"，设为默认
            if (setAsDefault)
            {
                await AutoBindProtocol(protocol, handler.UUID, handler.PackageName);
            }
            else
            {
                // 选择"仅一次"，删除现有绑定（取消默认关联）
                await RemoveDefaultBinding(protocol);
            }

            // 启动选择的处理器
            var launchResult = await LaunchPluginHandler(handler.PackageName, request, handler.Command);
            return new RunResult
            {
                Success = launchResult.Success,
                Message = launchResult.Message,
                UsedHandlerPackage = handler.PackageName
            };
        }

        /// <summary>
        /// 仅一次运行指定处理器 (不绑定)
        /// </summary>
        public async Task<RunResult> RunOnce(ProtocolHandlerOption handler, RunRequest request)
        {
            return await ExecuteWithSelectedHandler(handler, request, setAsDefault: false);
        }

        /// <summary>
        /// 设置为默认并运行
        /// </summary>
        public async Task<RunResult> RunAndSetDefault(ProtocolHandlerOption handler, RunRequest request)
        {
            return await ExecuteWithSelectedHandler(handler, request, setAsDefault: true);
        }

        /// <summary>
        /// 显示处理器选择对话框并执行用户选择
        /// </summary>
        /// <param name="runResult">包含 NeedsUserSelection=true 的运行结果</param>
        /// <param name="title">对话框标题（可选）</param>
        /// <param name="subtitle">对话框副标题（可选）</param>
        /// <param name="owner">父窗口（可选）</param>
        /// <returns>执行结果</returns>
        public async Task<RunResult> ShowHandlerSelectionAndExecute(
            RunResult runResult,
            string? title = null,
            string? subtitle = null,
            Window? owner = null)
        {
            if (!runResult.NeedsUserSelection || runResult.OriginalRequest == null || runResult.ProtocolKey == null)
            {
                return runResult;
            }

            var (selectedHandler, setAsDefault) = await Components.Arcusrix.Runner.PCOHandlerSelectDialog.ShowAsync(
                runResult.NewHandlers,
                runResult.ExistingHandlers,
                runResult.OriginalRequest,
                runResult.ProtocolKey,
                title,
                subtitle,
                owner);

            if (selectedHandler == null)
            {
                return new RunResult
                {
                    Success = false,
                    Message = "User cancelled selection"
                };
            }

            return await ExecuteWithSelectedHandler(selectedHandler, runResult.OriginalRequest, setAsDefault);
        }

        #endregion
    }
}




