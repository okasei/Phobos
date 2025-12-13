using Phobos.Components.Arcusrix.TaskManager;
using Phobos.Components.Arcusrix.TaskManager.Helpers;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Phobos 任务管理器插件
    /// 提供运行中插件管理和启动项管理功能
    /// 单例模式 - 只允许一个实例
    /// </summary>
    public class PCTaskManagerPlugin : PCPluginBase
    {
        private static PCTaskManagerPlugin? _instance;
        private PCOTaskManager? _contentArea;
        private bool _isWindowOpen = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PCTaskManagerPlugin Instance => _instance ??= new PCTaskManagerPlugin();

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Task Manager",
            PackageName = "com.phobos.taskmanager",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_taskmanager_x8kj29ls0f",
            DatabaseKey = "PTaskManager",
            Icon = "Assets/taskmanager-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "taskmanager://show",
            SettingUri = "",
            PreferredWidth = 900,
            PreferredHeight = 600,
            MinWindowWidth = 700,
            MinWindowHeight = 450,
            ShowMinimizeButton = true,
            ShowMaximizeButton = true,
            ShowCloseButton = true,
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Task Manager is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Task Manager is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "任务管理器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "工作管理員是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "タスクマネージャーは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Task Manager" },
                { "zh-CN", "任务管理器" },
                { "zh-TW", "工作管理員" },
                { "ja-JP", "タスクマネージャー" },
                { "ko-KR", "작업 관리자" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Manage running plugins and startup items" },
                { "zh-CN", "管理运行中的插件和启动项" },
                { "zh-TW", "管理執行中的插件和啟動項目" },
                { "ja-JP", "実行中のプラグインとスタートアップアイテムを管理" },
                { "ko-KR", "실행 중인 플러그인 및 시작 항목 관리" }
            }
        };

        public override FrameworkElement? ContentArea
        {
            get
            {
                if (_contentArea == null)
                {
                    _contentArea = new PCOTaskManager();
                }
                return _contentArea;
            }
        }

        public PCTaskManagerPlugin()
        {
            _instance = this;
            // 注册本地化资源
            TaskManagerLocalization.RegisterAll();
        }

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            await Link(new LinkAssociation
            {
                Protocol = "taskmanager:",
                Name = "PhobosTaskManagerHandler",
                Description = "Phobos Task Manager Protocol Handler",
                Command = "phobos://plugin/com.phobos.taskmanager?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.TaskManager:",
                Name = "PhobosTaskManagerHandler",
                Description = "Phobos Task Manager Protocol Handler",
                Command = "phobos://plugin/com.phobos.taskmanager?action=%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                // 单例检查：如果窗口已经打开，激活它而不是创建新窗口
                if (_isWindowOpen && _contentArea != null)
                {
                    // 尝试激活现有窗口
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var window = Window.GetWindow(_contentArea);
                        if (window != null)
                        {
                            if (window.WindowState == WindowState.Minimized)
                            {
                                window.WindowState = WindowState.Normal;
                            }
                            window.Activate();
                            window.Focus();
                        }
                    });
                    return new RequestResult { Success = true, Message = "Task Manager activated" };
                }

                // 标记窗口即将打开
                _isWindowOpen = true;

                // 确保 ContentArea 已创建
                if (_contentArea == null)
                {
                    _contentArea = new PCOTaskManager();
                }

                // 初始化数据
                await _contentArea.InitializeAsync();

                return new RequestResult { Success = true, Message = "Task Manager launched" };
            }
            catch (Exception ex)
            {
                _isWindowOpen = false;
                PCLoggerPlugin.Error("TaskManager", $"Failed to launch: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public override async Task<RequestResult> OnClosing(params object[] args)
        {
            _isWindowOpen = false;

            // 清理资源
            if (_contentArea != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _contentArea.Cleanup();
                    _contentArea = null;
                });
            }

            return await base.OnClosing(args);
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
                        return await OnLaunch(args.Length > 1 ? args[1..] : Array.Empty<object>());

                    case "refresh":
                        return await RefreshData();

                    default:
                        return new RequestResult { Success = false, Message = $"Unknown command: {command}" };
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Command failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        private async Task<RequestResult> RefreshData()
        {
            if (_contentArea != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await _contentArea.RefreshDataAsync();
                });
            }
            return new RequestResult { Success = true, Message = "Data refreshed" };
        }
    }
}
