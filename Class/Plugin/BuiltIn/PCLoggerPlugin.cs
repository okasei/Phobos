using System.IO;
using System.Windows;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Utils.IO;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Logger 插件 - 系统日志记录组件
    /// </summary>
    public class PCLoggerPlugin : PCPluginBase
    {
        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Logger",
            PackageName = "com.phobos.logger",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_logger_secret_kalf91ka0djs",
            DatabaseKey = "PLogger",
            Icon = "Assets/logger-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = false,
            SettingUri = "log://settings",
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Logger is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Logger is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "日志记录器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "日誌記錄器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "ロガーは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Logger" },
                { "zh-CN", "日志记录器" },
                { "zh-TW", "日誌記錄器" },
                { "ja-JP", "ロガー" },
                { "ko-KR", "로거" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "System and plugin logging" },
                { "zh-CN", "系统和插件日志记录" },
                { "zh-TW", "系統和插件日誌記錄" },
                { "ja-JP", "システムとプラグインのログ記録" },
                { "ko-KR", "시스템 및 플러그인 로깅" }
            }
        };

        public override FrameworkElement? ContentArea => null;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            var protocols = new[] { "log:", "logi:", "loge:", "logc:", "Phobos.Logger:","phobostest:" };
            foreach (var protocol in protocols)
            {
                await Link(new LinkAssociation
                {
                    Protocol = protocol,
                    Name = string.Format("LoggerHandler_General", protocol),
                    Description = "Logger Protocol Handler",
                    Command = "log://v1?log=%0"
                });
            }

            // 初始化日志文件
            try
            {
                CreateLogDirectory();
            }
            catch { }
            return await base.OnInstall(args);
        }

        private static string CreateLogDirectory()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Phobos", "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            var logFilePath = Path.Combine(logDir, $"Phobos_{DateTime.Now:yyyyMMdd}.log");
            return logFilePath;
        }

        public override Task<RequestResult> OnLaunch(params object[] args)
        {
            return base.OnLaunch(args);
        }

        private static void ClearOldLogs()
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Phobos", "Logs");
            PUFile.Instance.DeleteFilesExcept(logDir, "*.log", $"Phobos_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// 静态日志方法
        /// </summary>
        public static void Log(LogLevel level, string source, string message)
        {
            var entry = new LogEntry
            {
                Level = level,
                Source = source,
                Message = TextEscaper.Escape(message)
            };

            try
            {
                ClearOldLogs();
            }
            catch { }

            // 写入文件
            try
            {
                File.AppendAllText(CreateLogDirectory(), entry.ToString() + Environment.NewLine);
            }
            catch { }

        }

        public static void Debug(string source, string message) => Log(LogLevel.Debug, source, message);
        public static void Info(string source, string message) => Log(LogLevel.Info, source, message);
        public static void Warning(string source, string message) => Log(LogLevel.Warning, source, message);
        public static void Error(string source, string message) => Log(LogLevel.Error, source, message);
        public static void Critical(string source, string message) => Log(LogLevel.Critical, source, message);

        public override Task<RequestResult> Run(params object[] args)
        {
            if (args.Length >= 2 && args[0] is string action)
            {
                var message = args.Length > 1 ? args[1]?.ToString() ?? string.Empty : string.Empty;
                var source = args.Length > 2 ? args[2]?.ToString() ?? "Unknown" : "Unknown";

                switch (action.ToLowerInvariant())
                {
                    case "debug":
                        Debug(source, message);
                        return Task.FromResult(new RequestResult { Success = true });
                    case "info":
                        Info(source, message);
                        return Task.FromResult(new RequestResult { Success = true });
                    case "warning":
                        Warning(source, message);
                        return Task.FromResult(new RequestResult { Success = true });
                    case "error":
                        Error(source, message);
                        return Task.FromResult(new RequestResult { Success = true });
                    case "critical":
                        Critical(source, message);
                        return Task.FromResult(new RequestResult { Success = true });
                }
            }

            return base.Run(args);
        }
    }
}