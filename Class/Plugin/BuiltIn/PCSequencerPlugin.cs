using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Class.Plugin;
using Phobos.Components.Arcusrix.Sequencer;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Sequencer 插件 - 逐行执行目标文件中的命令
    /// UI 位于 Phobos.Components.Arcusrix.Sequencer.PCOSequencer
    /// </summary>
    public class PCSequencerPlugin : PCPluginBase
    {
        private PCOSequencer? _content;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Sequencer",
            PackageName = "com.phobos.sequencer",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_sequencer_secret_djf901las0pd",
            DatabaseKey = "PSequencer",
            Icon = "Assets/sequencer-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "seq://show",
            SettingUri = "seq://settings",
            PreferredWidth = 700,
            PreferredHeight = 500,
            MinWindowWidth = 500,
            MinWindowHeight = 400,
            ShowMinimizeButton = true,
            ShowMaximizeButton = true,
            ShowCloseButton = true,
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Sequencer is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Sequencer is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "序列执行器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "序列執行器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "シーケンサーは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Sequencer" },
                { "zh-CN", "序列执行器" },
                { "zh-TW", "序列執行器" },
                { "ja-JP", "シーケンサー" },
                { "ko-KR", "시퀀서" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Execute commands line by line from a file" },
                { "zh-CN", "逐行执行文件中的命令" },
                { "zh-TW", "逐行執行檔案中的命令" },
                { "ja-JP", "ファイルからコマンドを行ごとに実行" },
                { "ko-KR", "파일에서 명령을 한 줄씩 실행" }
            }
        };

        public override FrameworkElement? ContentArea => _content;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            var protocols = new[] { "seq:", "sequence:", "Phobos.Sequencer:" };
            foreach (var protocol in protocols)
            {
                await Link(new LinkAssociation
                {
                    Protocol = protocol,
                    Name = "SequencerHandler_General",
                    Description = "Sequencer Protocol Handler",
                    Command = "seq://v1?file=%0"
                });
            }

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                _content = new PCOSequencer();
                return await base.OnLaunch(args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Sequencer", $"Failed to launch: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            try
            {
                if (args.Length > 0 && args[0] is string action)
                {
                    switch (action.ToLowerInvariant())
                    {
                        case "execute":
                            if (args.Length > 1 && args[1] is string filePath)
                            {
                                _content?.SetFilePath(filePath);
                                _content?.Start();
                                return new RequestResult { Success = true, Message = "Sequence started" };
                            }
                            break;

                        case "stop":
                            _content?.Stop();
                            return new RequestResult { Success = true, Message = "Sequence stopped" };
                    }
                }

                return await base.Run(args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Sequencer", $"Command failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public override Task<RequestResult> OnClosing(params object[] args)
        {
            _content?.Cleanup();
            return base.OnClosing(args);
        }
    }
}
