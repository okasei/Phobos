using Phobos.Interface.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Utils.IO
{
    /// <summary>
    /// 消息通信实现
    /// </summary>
    public class PUMessenger : PIMessenger
    {
        private static PUMessenger? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, List<Action<object>>> _subscriptions = new();

        public static PUMessenger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PUMessenger();
                    }
                }
                return _instance;
            }
        }

        public async Task<bool> Send(string channel, object message)
        {
            return await Task.Run(() =>
            {
                if (_subscriptions.TryGetValue(channel, out var handlers))
                {
                    lock (handlers)
                    {
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                handler(message);
                            }
                            catch { }
                        }
                    }
                    return true;
                }
                return false;
            });
        }

        public void Subscribe(string channel, Action<object> handler)
        {
            var handlers = _subscriptions.GetOrAdd(channel, _ => new List<Action<object>>());
            lock (handlers)
            {
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        public void Unsubscribe(string channel, Action<object>? handler = null)
        {
            if (_subscriptions.TryGetValue(channel, out var handlers))
            {
                lock (handlers)
                {
                    if (handler != null)
                    {
                        handlers.Remove(handler);
                    }
                    else
                    {
                        handlers.Clear();
                    }
                }
            }
        }

        public async Task Publish(string channel, object message)
        {
            await Send(channel, message);
        }
    }
}
