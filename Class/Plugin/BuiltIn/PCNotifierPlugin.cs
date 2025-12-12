using Phobos.Components.Arcusrix.Notifier;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// Phobos 通知器插件
    /// 提供灵动岛样式的通知功能
    /// 实现 IPhobosNotifier 接口
    /// </summary>
    public class PCNotifierPlugin : PCPluginBase, IPhobosNotifier
    {
        private PCONotifierWindow? _notifierWindow;
        private readonly ConcurrentQueue<IPhobosNotification> _pendingNotifications = new();
        private IPhobosNotification? _currentNotification;
        private bool _isShowing = false;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Notifier",
            PackageName = "com.phobos.notifier",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_notifier_secret_n0t1f13r",
            DatabaseKey = "PNotifier",
            Icon = "Assets/notifier-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = false, // 不需要独立 GUI 窗口
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Notifier is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Notifier is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "通知器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "通知器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "通知器は Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Notifier" },
                { "zh-CN", "通知器" },
                { "zh-TW", "通知器" },
                { "ja-JP", "通知器" },
                { "ko-KR", "알림기" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Dynamic Island style notification system" },
                { "zh-CN", "灵动岛样式通知系统" },
                { "zh-TW", "靈動島樣式通知系統" },
                { "ja-JP", "ダイナミックアイランドスタイル通知システム" },
                { "ko-KR", "다이나믹 아일랜드 스타일 알림 시스템" }
            }
        };

        public override FrameworkElement? ContentArea => null;

        #region IPhobosNotifier Implementation

        /// <summary>
        /// 显示通知
        /// </summary>
        public async Task<NotifyResult> Notify(IPhobosNotification notification)
        {
            try
            {
                // 确保 ID 存在
                if (string.IsNullOrEmpty(notification.Id))
                {
                    notification.Id = Guid.NewGuid().ToString("N");
                }

                // 设置创建时间
                if (notification.CreatedAt == default)
                {
                    notification.CreatedAt = DateTime.Now;
                }

                // 如果当前正在显示通知，加入队列
                if (_isShowing)
                {
                    _pendingNotifications.Enqueue(notification);
                    return new NotifyResult
                    {
                        Success = true,
                        Message = "Notification queued",
                        NotificationId = notification.Id
                    };
                }

                // 显示通知
                await ShowNotificationInternal(notification);

                return new NotifyResult
                {
                    Success = true,
                    Message = "Notification shown",
                    NotificationId = notification.Id
                };
            }
            catch (Exception ex)
            {
                return new NotifyResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 显示简单文本通知
        /// </summary>
        public async Task<NotifyResult> Notify(string title, string content, int duration = 5000)
        {
            var notification = new PhobosNotification
            {
                Title = title,
                Content = content,
                ContentType = NotificationContentType.PlainText,
                Duration = duration,
                PackageName = "com.phobos.notifier"
            };

            return await Notify(notification);
        }

        /// <summary>
        /// 取消/关闭指定通知
        /// </summary>
        public async Task<RequestResult> Dismiss(string notificationId)
        {
            try
            {
                // 如果是当前正在显示的通知
                if (_currentNotification?.Id == notificationId)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _notifierWindow?.Hide();
                    });

                    _currentNotification = null;
                    _isShowing = false;

                    // 显示下一个通知
                    await ShowNextNotification();

                    return new RequestResult { Success = true, Message = "Notification dismissed" };
                }

                // 从队列中移除（需要重建队列）
                var tempQueue = new ConcurrentQueue<IPhobosNotification>();
                while (_pendingNotifications.TryDequeue(out var notification))
                {
                    if (notification.Id != notificationId)
                    {
                        tempQueue.Enqueue(notification);
                    }
                }

                while (tempQueue.TryDequeue(out var notification))
                {
                    _pendingNotifications.Enqueue(notification);
                }

                return new RequestResult { Success = true, Message = "Notification removed from queue" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 清除所有通知
        /// </summary>
        public async Task<RequestResult> DismissAll()
        {
            try
            {
                // 清空队列
                while (_pendingNotifications.TryDequeue(out _)) { }

                // 关闭当前通知
                if (_isShowing)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _notifierWindow?.Hide();
                    });

                    _currentNotification = null;
                    _isShowing = false;
                }

                return new RequestResult { Success = true, Message = "All notifications dismissed" };
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取当前显示的通知数量
        /// </summary>
        public int ActiveNotificationCount => _isShowing ? 1 : 0;

        /// <summary>
        /// 获取待显示的通知队列数量
        /// </summary>
        public int PendingNotificationCount => _pendingNotifications.Count;

        /// <summary>
        /// 是否正在显示通知
        /// </summary>
        public bool IsShowing => _isShowing;

        #endregion

        #region 内部方法

        private async Task ShowNotificationInternal(IPhobosNotification notification)
        {
            _currentNotification = notification;
            _isShowing = true;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 确保窗口存在
                if (_notifierWindow == null || !_notifierWindow.IsLoaded)
                {
                    _notifierWindow = new PCONotifierWindow(
                        OnNotificationClosed,
                        OnCheckNextNotification
                    );
                }

                // 尝试加载发送者图标
                if (notification.Icon == null && string.IsNullOrEmpty(notification.IconPath))
                {
                    TryLoadSenderIcon(notification);
                }

                _notifierWindow.ShowNotification(notification);
            });
        }

        private void TryLoadSenderIcon(IPhobosNotification notification)
        {
            if (string.IsNullOrEmpty(notification.PackageName)) return;

            try
            {
                var plugin = PMPlugin.Instance.GetPlugin(notification.PackageName);
                if (plugin != null && !string.IsNullOrEmpty(plugin.Metadata.Icon))
                {
                    notification.IconPath = plugin.Metadata.Icon;
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        private void OnNotificationClosed(string notificationId)
        {
            if (_currentNotification?.Id == notificationId)
            {
                _currentNotification = null;
                _isShowing = false;
            }
        }

        private void OnCheckNextNotification()
        {
            // 检查是否有待处理的通知
            _ = ShowNextNotification();
        }

        private async Task ShowNextNotification()
        {
            if (_pendingNotifications.TryDequeue(out var nextNotification))
            {
                // 短暂延迟，让上一个通知完全退出
                await Task.Delay(200);
                await ShowNotificationInternal(nextNotification);
            }
        }

        #endregion

        #region 生命周期

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            // 如果传入了通知参数，直接显示
            if (args.Length > 0)
            {
                if (args[0] is IPhobosNotification notification)
                {
                    var result = await Notify(notification);
                    return new RequestResult
                    {
                        Success = result.Success,
                        Message = result.Message
                    };
                }
                else if (args.Length >= 2 && args[0] is string title && args[1] is string content)
                {
                    int duration = args.Length > 2 && args[2] is int d ? d : 5000;
                    var result = await Notify(title, content, duration);
                    return new RequestResult
                    {
                        Success = result.Success,
                        Message = result.Message
                    };
                }
            }

            return new RequestResult { Success = true, Message = "Notifier ready" };
        }

        public override async Task<RequestResult> OnClosing(params object[] args)
        {
            await DismissAll();
            return await base.OnClosing(args);
        }

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册 launcher 特殊项（用于标识默认启动插件）
            await Link(new LinkAssociation
            {
                Protocol = "notifier",
                Name = "PhobosIslandNotifier",
                Description = "Phobos Island Notifier",
                Command = "notify://%0"
            });

            return await base.OnInstall(args);
        }


        #endregion
    }
}
