using Phobos.Components.Arcusrix.Desktop;
using Phobos.Components.Arcusrix.Desktop.Components;
using Phobos.Service.Arcusrix;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Phobos 桌面插件
    /// 提供桌面启动器功能，支持托盘图标和自动隐藏
    /// 实现 IPhobosDesktop 接口
    /// </summary>
    public class PCDesktopPlugin : PCPluginBase, IPhobosDesktop
    {
        private static PCDesktopPlugin? _instance;
        private PCOPhobosDesktop? _desktopWindow;
        private PSSystemMonitor? _systemMonitor;

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
            // 注册桌面本地化资源
            DesktopLocalization.RegisterAll();
        }

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册 launcher 特殊项（用于标识默认启动插件）
            await Link(new LinkAssociation
            {
                Protocol = "launcher",
                Name = "PhobosLauncher",
                Description = "Phobos Default Launcher (Desktop)",
                Command = "phobos://plugin/com.phobos.desktop"
            });

            // 注册协议
            await Link(new LinkAssociation
            {
                Protocol = "desktop:",
                Name = "PhobosDesktopHandler",
                Description = "Phobos Desktop Protocol Handler",
                Command = "phobos://plugin/com.phobos.desktop?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "home:",
                Name = "PhobosDesktopHandler",
                Description = "Phobos Desktop Protocol Handler",
                Command = "phobos://plugin/com.phobos.desktop?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.Desktop:",
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

                // 单例检查：如果窗口已存在，激活它而不是创建新实例
                if (_desktopWindow != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _desktopWindow.ShowFromTray();
                    });
                    return new RequestResult { Success = true, Message = "Desktop activated" };
                }

                // 创建并显示桌面窗口
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _desktopWindow = new PCOPhobosDesktop();
                    _desktopWindow.Closed += (s, e) => _desktopWindow = null;
                    _desktopWindow.ShowFromTray();
                });

                // 启动系统监控服务
                await StartSystemMonitorAsync();

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
            // 停止系统监控服务
            await StopSystemMonitorAsync();

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

        #region IPhobosDesktop Implementation

        /// <summary>
        /// 搜索桌面项
        /// </summary>
        public async Task<List<PhobosSuggestionItem>> SearchDesktop(string keyword, int maxResults = 20)
        {
            var suggestions = new List<PhobosSuggestionItem>();

            if (_desktopWindow == null || string.IsNullOrWhiteSpace(keyword))
                return suggestions;

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var items = _desktopWindow.GetAllDesktopItems();
                var lowerKeyword = keyword.ToLowerInvariant();

                foreach (var item in items)
                {
                    string name = string.Empty;
                    string description = string.Empty;
                    string? iconPath = null;
                    string? packageName = null;
                    string? arguments = null;

                    if (item is PluginDesktopItem pluginItem)
                    {
                        var plugin = _desktopWindow.GetPluginByPackageName(pluginItem.PackageName);
                        if (plugin == null) continue;

                        name = plugin.Metadata.GetLocalizedName("zh-CN");
                        description = plugin.Metadata.GetLocalizedDescription("zh-CN");
                        iconPath = plugin.Metadata.Icon;
                        packageName = pluginItem.PackageName;
                    }
                    else if (item is FolderDesktopItem folderItem)
                    {
                        name = folderItem.Name;
                        description = $"文件夹 ({folderItem.PluginPackageNames.Count} 个项目)";
                    }
                    else if (item is ShortcutDesktopItem shortcutItem)
                    {
                        name = shortcutItem.Name;
                        description = $"快捷方式 -> {shortcutItem.TargetPackageName}";
                        iconPath = shortcutItem.CustomIconPath;
                        packageName = shortcutItem.TargetPackageName;
                        arguments = shortcutItem.Arguments;
                    }

                    // 匹配检测
                    var lowerName = name.ToLowerInvariant();
                    var lowerDesc = description.ToLowerInvariant();
                    var lowerPkg = (packageName ?? string.Empty).ToLowerInvariant();

                    if (lowerName.Contains(lowerKeyword) || lowerDesc.Contains(lowerKeyword) || lowerPkg.Contains(lowerKeyword))
                    {
                        // 计算匹配得分
                        int score = 0;
                        if (lowerName.StartsWith(lowerKeyword)) score += 100;
                        else if (lowerName.Contains(lowerKeyword)) score += 50;
                        if (lowerPkg.Contains(lowerKeyword)) score += 30;
                        if (lowerDesc.Contains(lowerKeyword)) score += 10;

                        suggestions.Add(new PhobosSuggestionItem
                        {
                            Id = item.Id,
                            Name = name,
                            Description = description,
                            Info = item.Info,
                            IconPath = iconPath,
                            PackageName = packageName,
                            Type = item.Type switch
                            {
                                DesktopItemType.Plugin => SuggestionType.Plugin,
                                DesktopItemType.Shortcut => SuggestionType.Shortcut,
                                _ => SuggestionType.Other
                            },
                            Score = score,
                            SourcePackageName = Metadata.PackageName,
                            Arguments = [arguments]
                        });
                    }

                    if (suggestions.Count >= maxResults)
                        break;
                }

                return suggestions.OrderByDescending(s => s.Score).Take(maxResults).ToList();
            });
        }

        /// <summary>
        /// 创建快捷方式
        /// </summary>
        public async Task<RequestResult> CreateShortcut(CreateShortcutRequest request)
        {
            if (_desktopWindow == null)
                return new RequestResult { Success = false, Message = "Desktop window not initialized" };

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TargetPackageName))
                return new RequestResult { Success = false, Message = "Name and TargetPackageName are required" };

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var shortcut = new ShortcutDesktopItem
                    {
                        Name = request.Name,
                        TargetPackageName = request.TargetPackageName,
                        Arguments = request.Arguments,
                        CustomIconPath = request.CustomIconPath ?? string.Empty,
                        Info = request.Info ?? string.Empty,
                        Hotkey = request.Hotkey ?? string.Empty
                    };

                    // 如果指定了位置，使用指定位置
                    if (request.GridX.HasValue && request.GridY.HasValue)
                    {
                        shortcut.GridX = request.GridX.Value;
                        shortcut.GridY = request.GridY.Value;
                    }

                    _desktopWindow.AddDesktopItem(shortcut);
                    _desktopWindow.RenderDesktop();

                    return new RequestResult
                    {
                        Success = true,
                        Message = "Shortcut created",
                        Data = new List<object> { shortcut.Id }
                    };
                }
                catch (Exception ex)
                {
                    return new RequestResult { Success = false, Message = ex.Message, Error = ex };
                }
            });
        }

        /// <summary>
        /// 启动桌面项
        /// </summary>
        public async Task<RequestResult> LaunchDesktopItem(string itemId, params object[] args)
        {
            if (_desktopWindow == null)
                return new RequestResult { Success = false, Message = "Desktop window not initialized" };

            if (string.IsNullOrWhiteSpace(itemId))
                return new RequestResult { Success = false, Message = "Item ID is required" };

            try
            {
                var item = await Application.Current.Dispatcher.InvokeAsync(() =>
                    _desktopWindow.GetDesktopItemById(itemId));

                if (item == null)
                    return new RequestResult { Success = false, Message = $"Item not found: {itemId}" };

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await _desktopWindow.LaunchDesktopItem(item, args);
                });

                return new RequestResult { Success = true, Message = "Item launched" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取所有桌面项
        /// </summary>
        public async Task<List<DesktopItem>> GetAllDesktopItems()
        {
            if (_desktopWindow == null)
                return new List<DesktopItem>();

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var items = _desktopWindow.GetAllDesktopItems();
                return items.Select(item =>
                {
                    DesktopItem result = item switch
                    {
                        PluginDesktopItem pluginItem => new PluginDesktopItem
                        {
                            Id = pluginItem.Id,
                            PackageName = pluginItem.PackageName,
                            GridX = pluginItem.GridX,
                            GridY = pluginItem.GridY,
                            Hotkey = pluginItem.Hotkey,
                            Info = pluginItem.Info,
                            Name = _desktopWindow.GetPluginByPackageName(pluginItem.PackageName)?.Metadata.GetLocalizedName("zh-CN") ?? pluginItem.PackageName
                        },
                        FolderDesktopItem folderItem => new FolderDesktopItem
                        {
                            Id = folderItem.Id,
                            Name = folderItem.Name,
                            PluginPackageNames = folderItem.PluginPackageNames,
                            GridX = folderItem.GridX,
                            GridY = folderItem.GridY,
                            Hotkey = folderItem.Hotkey,
                            Info = folderItem.Info
                        },
                        ShortcutDesktopItem shortcutItem => new ShortcutDesktopItem
                        {
                            Id = shortcutItem.Id,
                            Name = shortcutItem.Name,
                            TargetPackageName = shortcutItem.TargetPackageName,
                            Arguments = shortcutItem.Arguments,
                            CustomIconPath = shortcutItem.CustomIconPath,
                            GridX = shortcutItem.GridX,
                            GridY = shortcutItem.GridY,
                            Hotkey = shortcutItem.Hotkey,
                            Info = shortcutItem.Info
                        },
                        _ => new DesktopItem
                        {
                            Id = item.Id,
                            Type = (Shared.Models.DesktopItemType)(int)item.Type,
                            GridX = item.GridX,
                            GridY = item.GridY,
                            Hotkey = item.Hotkey,
                            Info = item.Info
                        }
                    };
                    return result;
                }).ToList();
            });
        }

        /// <summary>
        /// 根据 ID 获取桌面项
        /// </summary>
        public async Task<DesktopItem?> GetDesktopItem(string itemId)
        {
            var items = await GetAllDesktopItems();
            return items.FirstOrDefault(i => i.Id == itemId);
        }

        /// <summary>
        /// 删除桌面项
        /// </summary>
        public async Task<RequestResult> RemoveDesktopItem(string itemId)
        {
            if (_desktopWindow == null)
                return new RequestResult { Success = false, Message = "Desktop window not initialized" };

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var item = _desktopWindow.GetDesktopItemById(itemId);
                    if (item == null)
                        return new RequestResult { Success = false, Message = $"Item not found: {itemId}" };

                    _desktopWindow.RemoveDesktopItem(item);
                    _desktopWindow.RenderDesktop();
                    return new RequestResult { Success = true, Message = "Item removed" };
                }
                catch (Exception ex)
                {
                    return new RequestResult { Success = false, Message = ex.Message, Error = ex };
                }
            });
        }

        /// <summary>
        /// 刷新桌面显示
        /// </summary>
        void IPhobosDesktop.RefreshDesktop()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _desktopWindow?.RefreshPlugins();
                _desktopWindow?.RenderDesktop();
            });
        }

        #endregion

        #region System Monitor

        /// <summary>
        /// 启动系统监控服务
        /// </summary>
        private async Task StartSystemMonitorAsync()
        {
            try
            {
                if (_systemMonitor != null) return;

                _systemMonitor = new PSSystemMonitor();
                _systemMonitor.SystemNotificationReceived += OnSystemNotificationReceived;
                await _systemMonitor.StartAsync();

                PCLoggerPlugin.Info("Desktop", "System monitor started");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"Failed to start system monitor: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止系统监控服务
        /// </summary>
        private async Task StopSystemMonitorAsync()
        {
            try
            {
                if (_systemMonitor == null) return;

                _systemMonitor.SystemNotificationReceived -= OnSystemNotificationReceived;
                await _systemMonitor.StopAsync();
                _systemMonitor.Dispose();
                _systemMonitor = null;

                PCLoggerPlugin.Info("Desktop", "System monitor stopped");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"Failed to stop system monitor: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理系统通知事件
        /// </summary>
        private async void OnSystemNotificationReceived(object? sender, SystemNotificationEventArgs e)
        {
            try
            {
                var notification = new PhobosNotification
                {
                    Title = e.Title,
                    Content = e.Content,
                    ContentType = NotificationContentType.PlainText,
                    PackageName = Metadata.PackageName,
                    Duration = GetNotificationDuration(e.Type),
                    IconPath = e.IconPath,
                    Actions = e.Actions,
                    Priority = GetNotificationPriority(e.Type)
                };

                // 根据通知类型设置图片路径
                notification.ImagePath = GetNotificationImagePath(e.Type);

                await SendNotification(notification);

                PCLoggerPlugin.Info("Desktop", $"System notification sent: {e.Type} - {e.Title}");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"Failed to send system notification: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取通知持续时间
        /// </summary>
        private static int GetNotificationDuration(SystemNotificationType type)
        {
            return type switch
            {
                SystemNotificationType.BatteryStatus => 8000,  // 电量通知显示较长时间
                SystemNotificationType.NetworkStatus => 5000,
                SystemNotificationType.DeviceConnected => 5000,
                SystemNotificationType.DeviceDisconnected => 4000,
                SystemNotificationType.ChargingStatus => 4000,
                SystemNotificationType.PowerSaverMode => 5000,
                _ => 5000
            };
        }

        /// <summary>
        /// 获取通知优先级
        /// </summary>
        private static int GetNotificationPriority(SystemNotificationType type)
        {
            return type switch
            {
                SystemNotificationType.BatteryStatus => 80,    // 电量警告高优先级
                SystemNotificationType.NetworkStatus => 70,    // 网络状态较高优先级
                SystemNotificationType.ChargingStatus => 50,
                SystemNotificationType.PowerSaverMode => 50,
                SystemNotificationType.DeviceConnected => 40,
                SystemNotificationType.DeviceDisconnected => 40,
                _ => 30
            };
        }

        /// <summary>
        /// 获取通知提示图片路径
        /// </summary>
        private static string? GetNotificationImagePath(SystemNotificationType type)
        {
            return type switch
            {
                SystemNotificationType.BatteryStatus => "pack://application:,,,/Assets/Icons/notification-battery.png",
                SystemNotificationType.NetworkStatus => "pack://application:,,,/Assets/Icons/notification-network.png",
                SystemNotificationType.ChargingStatus => "pack://application:,,,/Assets/Icons/notification-power.png",
                SystemNotificationType.PowerSaverMode => "pack://application:,,,/Assets/Icons/notification-power.png",
                SystemNotificationType.DeviceConnected => "pack://application:,,,/Assets/Icons/notification-device.png",
                SystemNotificationType.DeviceDisconnected => "pack://application:,,,/Assets/Icons/notification-device.png",
                _ => null
            };
        }

        #endregion

        #region OnRequestReceived - 处理来自其他插件的请求

        /// <summary>
        /// 处理来自其他插件的请求
        /// </summary>
        public override async Task<RequestResult> OnRequestReceived(string sourcePackageName, string command, params object[] args)
        {
            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "wallpaper":
                        return await GetWallpaperInfo();

                    case "opacity":
                        return await GetOpacityInfo();

                    default:
                        return await base.OnRequestReceived(sourcePackageName, command, args);
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Desktop", $"OnRequestReceived failed: {ex.Message}");
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取壁纸信息
        /// </summary>
        private async Task<RequestResult> GetWallpaperInfo()
        {
            if (_desktopWindow == null)
            {
                return new RequestResult
                {
                    Success = false,
                    Message = "Desktop window not initialized"
                };
            }

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var wallpaperPath = _desktopWindow.GetWallpaperPath();
                var stretch = _desktopWindow.GetWallpaperStretch();

                return new RequestResult
                {
                    Success = true,
                    Message = "Wallpaper info retrieved",
                    Data = new List<object>
                    {
                        wallpaperPath ?? string.Empty,
                        stretch.ToString()
                    }
                };
            });
        }

        /// <summary>
        /// 获取透明度信息
        /// </summary>
        private async Task<RequestResult> GetOpacityInfo()
        {
            if (_desktopWindow == null)
            {
                return new RequestResult
                {
                    Success = false,
                    Message = "Desktop window not initialized"
                };
            }

            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var opacity = _desktopWindow.GetWallpaperOpacity();

                return new RequestResult
                {
                    Success = true,
                    Message = "Opacity info retrieved",
                    Data = new List<object> { opacity }
                };
            });
        }

        #endregion
    }
}