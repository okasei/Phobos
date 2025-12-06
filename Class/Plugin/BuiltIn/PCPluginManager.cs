using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Components.Arcusrix.PluginManager;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Plugin Manager plugin - Modern UI version
    /// UI is in Phobos.Components.Arcusrix.PluginManager.PCOPluginManager
    /// </summary>
    public class PCPluginManager : PCPluginBase
    {
        private PCOPluginManager? _content;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Plugin Manager",
            PackageName = "com.phobos.plugin.manager",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_plugin_manager_secret_jaso19d81las",
            DatabaseKey = "PManager",
            Icon = "Assets/plugin-manager-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "pm://show",
            SettingUri = "pm://settings",
            PreferredWidth = 1000,
            PreferredHeight = 680,
            MinWindowWidth = 800,
            MinWindowHeight = 500,
            SizeMode = WindowSizeMode.Default,
            ShowMinimizeButton = true,
            ShowMaximizeButton = true,
            ShowCloseButton = true,
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Plugin Manager is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" },
                    { "ko-KR", "시스템 플러그인을 제거할 수 없습니다" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Plugin Manager is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "插件管理器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "插件管理器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "プラグインマネージャーは Phobos のコアコンポーネントであり、アンインストールできません。" },
                    { "ko-KR", "플러그인 관리자는 Phobos의 핵심 구성 요소이며 제거할 수 없습니다." }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Plugin Manager" },
                { "zh-CN", "插件管理器" },
                { "zh-TW", "插件管理器" },
                { "ja-JP", "プラグインマネージャー" },
                { "ko-KR", "플러그인 관리자" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Manage installed plugins" },
                { "zh-CN", "管理已安装的插件" },
                { "zh-TW", "管理已安裝的插件" },
                { "ja-JP", "インストールされたプラグインを管理" },
                { "ko-KR", "설치된 플러그인 관리" }
            }
        };

        public override FrameworkElement? ContentArea => _content;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            await Link(new LinkAssociation
            {
                Protocol = "pm",
                Name = "PluginManagerHandler_General",
                Description = "Plugin Manager Protocol Handler",
                Command = "pm://v1?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.PluginManager",
                Name = "PluginManagerHandler_General",
                Description = "Plugin Manager Protocol Handler",
                Command = "pm://v1?action=%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                _content = new PCOPluginManager();
                return await base.OnLaunch(args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PluginManager", $"Failed to launch: {ex.Message}");
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
                        case "show":
                            return new RequestResult { Success = true, Message = "Shown" };

                        case "refresh":
                            if (_content != null)
                            {
                                await _content.RefreshPluginList();
                            }
                            return new RequestResult { Success = true, Message = "Refreshed" };

                        case "list":
                            var plugins = await PMPlugin.Instance.GetInstalledPlugins();
                            return new RequestResult
                            {
                                Success = true,
                                Message = $"{plugins.Count} plugins",
                                Data = new List<object>(plugins)
                            };
                    }
                }

                return await base.Run(args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PluginManager", $"Command failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }
    }
}
