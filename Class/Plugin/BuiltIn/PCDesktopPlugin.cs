using Phobos.Components.Arcusrix.Desktop;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Phobos 桌面插件
    /// 提供桌面启动器功能，支持托盘图标和自动隐藏
    /// </summary>
    public class PCDesktopPlugin : PCPluginBase
    {
        private static PCDesktopPlugin? _instance;
        private PCOPhobosDesktop? _desktopWindow;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PCDesktopPlugin Instance => _instance ??= new PCDesktopPlugin();

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Phobos Desktop",
            PackageName = "com.phobos.desktop",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_desktop_aur3v1x_k9sj28",
            DatabaseKey = "PDesktop",
            Icon = "Assets/desktop-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "desktop://show",
            SettingUri = "desktop://settings",
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Phobos Desktop is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Phobos Desktop is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "Phobos 桌面是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "Phobos 桌面是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "Phobos デスクトップは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Phobos Desktop" },
                { "zh-CN", "Phobos 桌面" },
                { "zh-TW", "Phobos 桌面" },
                { "ja-JP", "Phobos デスクトップ" },
                { "ko-KR", "Phobos 데스크톱" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Desktop launcher and app management" },
                { "zh-CN", "桌面启动器与应用管理" },
                { "zh-TW", "桌面啟動器與應用管理" },
                { "ja-JP", "デスクトップランチャーとアプリ管理" },
                { "ko-KR", "데스크톱 런처 및 앱 관리" }
            }
        };

        // 桌面插件不使用 ContentArea，而是直接管理窗口
        public override FrameworkElement? ContentArea => null;

        public PCDesktopPlugin()
        {
            _instance = this;
        }

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            await Link(new LinkAssociation
            {
                Protocol = "desktop",
                Name = "PhobosDesktopHandler",
                Description = "Phobos Desktop Protocol Handler",
                Command = "phobos://plugin/com.phobos.desktop?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.Desktop",
                Name = "PhobosDesktopHandler",
                Description = "Phobos Desktop Protocol Handler",
                Command = "phobos://plugin/com.phobos.desktop?action=%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                // 订阅 App.Installed 和 App.Uninstalled 事件
                await Subscribe(PhobosEventIds.App, "Installed");
                await Subscribe(PhobosEventIds.App, "Uninstalled");

                // 如果窗口已存在且可见，激活它
                if (_desktopWindow != null && _desktopWindow.IsVisible)
                {
                    _desktopWindow.ShowFromTray();
                    return new RequestResult { Success = true, Message = "Desktop activated" };
                }

                // 创建并显示桌面窗口
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow = new PCOPhobosDesktop();
                    _desktopWindow.Closed += (s, e) => _desktopWindow = null;
                    _desktopWindow.ShowFromTray();
                });

                return new RequestResult { Success = true, Message = "Desktop launched" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"Failed to launch: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public override async Task<RequestResult> OnClosing(params object[] args)
        {
            // 取消订阅会在基类的 OnClosing 中自动处理
            if (_desktopWindow != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow?.Close();
                    _desktopWindow = null;
                });
            }

            return await base.OnClosing(args);
        }

        /// <summary>
        /// 处理接收到的事件
        /// </summary>
        public override async Task OnEventReceived(string eventId, string eventName, params object[] args)
        {
            await base.OnEventReceived(eventId, eventName, args);

            // 处理 App 事件
            if (eventId == PhobosEventIds.App)
            {
                switch (eventName)
                {
                    case "Installed":
                        await HandleAppInstalled(args);
                        break;
                    case "Uninstalled":
                        await HandleAppUninstalled(args);
                        break;
                }
            }
        }

        /// <summary>
        /// 处理应用安装事件
        /// </summary>
        private async Task HandleAppInstalled(object[] args)
        {
            // args[0] = sourcePackageName (触发事件的插件)
            // args[1] = installedPackageName (被安装的插件包名)
            // args[2] = installedPluginName (被安装的插件名称，可选)
            if (args.Length > 1)
            {
                var packageName = args[1]?.ToString() ?? string.Empty;
                PCLoggerPlugin.Info("Desktop", $"App installed: {packageName}");

                // 刷新桌面并添加新安装的插件到布局
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow?.RefreshAndAddNewPlugins();
                });
            }
        }

        /// <summary>
        /// 处理应用卸载事件
        /// </summary>
        private async Task HandleAppUninstalled(object[] args)
        {
            // args[0] = sourcePackageName (触发事件的插件)
            // args[1] = uninstalledPackageName (被卸载的插件包名)
            if (args.Length > 1)
            {
                var packageName = args[1]?.ToString() ?? string.Empty;
                PCLoggerPlugin.Info("Desktop", $"App uninstalled: {packageName}");
                _desktopWindow?.UninstallPlugin(new() { PackageName = packageName }, true);

                // 刷新桌面
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow?.RefreshPlugins();
                    _desktopWindow?.RenderDesktop();
                });
            }
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            try
            {
                if (args.Length == 0)
                    return new RequestResult { Success = false, Message = "No command specified" };

                var command = args[0]?.ToString()?.ToLowerInvariant() ?? string.Empty;

                switch (command)
                {
                    case "show":
                        return await ShowDesktop();

                    case "hide":
                        return await HideDesktop();

                    case "toggle":
                        return await ToggleDesktop();

                    case "refresh":
                        return await RefreshDesktop();

                    default:
                        return new RequestResult { Success = false, Message = $"Unknown command: {command}" };
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"Command failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        private async Task<RequestResult> ShowDesktop()
        {
            if (_desktopWindow == null)
            {
                return await OnLaunch();
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _desktopWindow?.ShowFromTray();
            });

            return new RequestResult { Success = true, Message = "Desktop shown" };
        }

        private async Task<RequestResult> HideDesktop()
        {
            if (_desktopWindow != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow?.HideToTray();
                });
            }
            return new RequestResult { Success = true, Message = "Desktop hidden" };
        }

        private async Task<RequestResult> ToggleDesktop()
        {
            if (_desktopWindow == null)
            {
                return await ShowDesktop();
            }

            // 检查窗口可见性并切换
            // 注意: 实际实现需要在 PCOPhobosDesktop 中暴露可见性状态
            return await ShowDesktop();
        }

        private async Task<RequestResult> RefreshDesktop()
        {
            if (_desktopWindow != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow?.RefreshPlugins();
                });
            }
            return new RequestResult { Success = true, Message = "Desktop refreshed" };
        }

        /// <summary>
        /// 获取桌面窗口实例
        /// </summary>
        public PCOPhobosDesktop? GetDesktopWindow() => _desktopWindow;
    }
}