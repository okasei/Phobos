using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Phobos.Interface.IO
{
    /// <summary>
    /// 文件系统接口
    /// </summary>
    public interface PIFileSystem
    {
        /// <summary>
        /// 读取文件内容
        /// </summary>
        Task<string> ReadAllText(string path);

        /// <summary>
        /// 写入文件内容
        /// </summary>
        Task WriteAllText(string path, string content);

        /// <summary>
        /// 读取所有字节
        /// </summary>
        Task<byte[]> ReadAllBytes(string path);

        /// <summary>
        /// 写入所有字节
        /// </summary>
        Task WriteAllBytes(string path, byte[] bytes);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// 创建目录
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        /// 删除文件
        /// </summary>
        void DeleteFile(string path);

        /// <summary>
        /// 删除目录
        /// </summary>
        void DeleteDirectory(string path, bool recursive = false);

        /// <summary>
        /// 获取目录中的文件
        /// </summary>
        string[] GetFiles(string path, string searchPattern = "*", bool recursive = false);

        /// <summary>
        /// 获取目录中的子目录
        /// </summary>
        string[] GetDirectories(string path);

        /// <summary>
        /// 复制文件
        /// </summary>
        void CopyFile(string source, string dest, bool overwrite = false);

        /// <summary>
        /// 移动文件
        /// </summary>
        void MoveFile(string source, string dest);
    }

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