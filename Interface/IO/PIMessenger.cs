using System;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Interface.IO
{
    /// <summary>
    /// 消息通信接口
    /// </summary>
    public interface PIMessenger
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        Task<bool> Send(string channel, object message);

        /// <summary>
        /// 订阅频道
        /// </summary>
        void Subscribe(string channel, Action<object> handler);

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe(string channel, Action<object>? handler = null);

        /// <summary>
        /// 发布消息（广播）
        /// </summary>
        Task Publish(string channel, object message);
    }
}
