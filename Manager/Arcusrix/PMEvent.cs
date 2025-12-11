using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Plugin;
using Phobos.Shared.Interface;

namespace Phobos.Manager.Arcusrix
{
    /// <summary>
    /// 事件订阅信息
    /// </summary>
    public class EventSubscription
    {
        public string PackageName { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime SubscribedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 事件触发来源信息
    /// </summary>
    public class EventTriggerSource
    {
        public string SourcePackageName { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public object[] Args { get; set; } = Array.Empty<object>();
        public DateTime TriggeredAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 事件管理器 - 处理插件间事件订阅和广播
    /// </summary>
    public class PMEvent
    {
        private static PMEvent? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, List<EventSubscription>> _subscriptions = new();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PMEvent Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMEvent();
                    }
                }
                return _instance;
            }
        }

        private PMEvent() { }

        private static string GetEventKey(string eventId, string eventName) => $"{eventId}.{eventName}";

        #region 订阅管理

        /// <summary>
        /// 订阅事件
        /// </summary>
        public RequestResult Subscribe(string packageName, string eventId, string eventName)
        {
            var key = GetEventKey(eventId, eventName);

            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(key))
                {
                    _subscriptions[key] = new List<EventSubscription>();
                }

                if (_subscriptions[key].Any(s => s.PackageName == packageName))
                {
                    PCLoggerPlugin.Info("PMEvent", $"[PMEvent] {packageName} already subscribed to {eventId}.{eventName}");
                    return new RequestResult { Success = true, Message = "Already subscribed" };
                }

                _subscriptions[key].Add(new EventSubscription
                {
                    PackageName = packageName,
                    EventId = eventId,
                    EventName = eventName,
                    SubscribedAt = DateTime.Now
                });

                PCLoggerPlugin.Info("PMEvent", $"[PMEvent] {packageName} subscribed to {eventId}.{eventName}");
            }

            return new RequestResult { Success = true, Message = $"Subscribed to {eventId}.{eventName}" };
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public RequestResult Unsubscribe(string packageName, string eventId, string eventName)
        {
            var key = GetEventKey(eventId, eventName);

            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(key))
                {
                    return new RequestResult { Success = true, Message = "No subscription found" };
                }

                var removed = _subscriptions[key].RemoveAll(s => s.PackageName == packageName);

                if (_subscriptions[key].Count == 0)
                {
                    _subscriptions.Remove(key);
                }

                return new RequestResult { Success = true, Message = removed > 0 ? "Unsubscribed" : "No subscription found" };
            }
        }

        /// <summary>
        /// 取消指定插件的所有订阅
        /// </summary>
        public void UnsubscribeAll(string packageName)
        {
            lock (_subscriptions)
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in _subscriptions)
                {
                    kvp.Value.RemoveAll(s => s.PackageName == packageName);
                    if (kvp.Value.Count == 0)
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var key in keysToRemove)
                    _subscriptions.Remove(key);
            }
        }

        /// <summary>
        /// 获取指定事件的订阅者列表
        /// </summary>
        public List<string> GetSubscribers(string eventId, string eventName)
        {
            var key = GetEventKey(eventId, eventName);
            lock (_subscriptions)
            {
                if (_subscriptions.TryGetValue(key, out var subs))
                    return subs.Select(s => s.PackageName).ToList();
            }
            return new List<string>();
        }

        #endregion

        #region 事件触发

        /// <summary>
        /// 触发事件（由主程序调用）- 通知所有订阅者
        /// </summary>
        public async Task TriggerAsync(string eventId, string eventName, string source, params object[] args)
        {
            var subscribers = GetSubscribers(eventId, eventName);

            foreach (var packageName in subscribers)
            {
                if (!packageName.Equals(source))
                    try
                    {
                        await NotifyPluginAsync(packageName, eventId, eventName, args);
                    }
                    catch (Exception ex)
                    {
                        PCLoggerPlugin.Warning("PMEvent", $"[PMEvent] Failed to notify {packageName}: {ex.Message}");
                    }
            }
        }

        /// <summary>
        /// 触发事件（由插件调用）- 通知所有订阅者
        /// </summary>
        public async Task TriggerFromPluginAsync(string sourcePackageName, string eventId, string eventName, params object[] args)
        {
            // 在 args 前面加上 sourcePackageName
            var fullArgs = new object[args.Length + 1];
            fullArgs[0] = sourcePackageName;
            Array.Copy(args, 0, fullArgs, 1, args.Length);

            await TriggerAsync(eventId, eventName, sourcePackageName, fullArgs);
        }

        /// <summary>
        /// 通知单个插件
        /// </summary>
        private async Task NotifyPluginAsync(string packageName, string eventId, string eventName, object[] args)
        {
            var pluginManager = PMPlugin.Instance;
            var plugin = pluginManager.GetPlugin(packageName);

            if (plugin != null)
            {
                await plugin.OnEventReceived(eventId, eventName, args);
            }
        }

        #endregion
    }

    /// <summary>
    /// App 事件名称扩展
    /// </summary>
    public static class PhobosAppEventNames
    {
        public const string Installed = "Installed";
        public const string Uninstalled = "Uninstalled";
    }
}