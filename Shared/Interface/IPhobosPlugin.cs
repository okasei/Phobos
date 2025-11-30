using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Shared.Interface
{
    /// <summary>
    /// 插件依赖项
    /// </summary>
    public class PluginDependency
    {
        public string PackageName { get; set; } = string.Empty;
        public string MinVersion { get; set; } = "1.0.0";
        public bool IsOptional { get; set; } = false;
    }

    /// <summary>
    /// 插件元数据
    /// </summary>
    public class PluginMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Secret { get; set; } = string.Empty;
        public string DatabaseKey { get; set; } = string.Empty;
        public List<PluginDependency> Dependencies { get; set; } = new();
        public Dictionary<string, string> LocalizedNames { get; set; } = new();
        public Dictionary<string, string> LocalizedDescriptions { get; set; } = new();

        /// <summary>
        /// 获取本地化名称
        /// </summary>
        public string GetLocalizedName(string languageCode)
        {
            if (LocalizedNames.TryGetValue(languageCode, out var name))
                return name;
            if (LocalizedNames.TryGetValue("en-US", out var defaultName))
                return defaultName;
            return Name;
        }

        /// <summary>
        /// 获取本地化描述
        /// </summary>
        public string GetLocalizedDescription(string languageCode)
        {
            if (LocalizedDescriptions.TryGetValue(languageCode, out var desc))
                return desc;
            if (LocalizedDescriptions.TryGetValue("en-US", out var defaultDesc))
                return defaultDesc;
            return string.Empty;
        }
    }

    /// <summary>
    /// 插件调用者上下文 - 用于标识调用来源
    /// </summary>
    public class PluginCallerContext
    {
        /// <summary>
        /// 调用者包名
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// 调用者数据库键前缀
        /// </summary>
        public string DatabaseKey { get; set; } = string.Empty;

        /// <summary>
        /// 是否为受信任的插件
        /// </summary>
        public bool IsTrusted { get; set; } = false;

        /// <summary>
        /// 调用时间
        /// </summary>
        public DateTime CallTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 请求结果
    /// </summary>
    public class RequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<object> Data { get; set; } = new();
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// 配置读写结果
    /// </summary>
    public class ConfigResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 启动项结果
    /// </summary>
    public class BootResult
    {
        public bool Success { get; set; }
        public string UUID { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 链接关联配置
    /// </summary>
    public class LinkAssociation
    {
        public string Protocol { get; set; } = string.Empty;
        //只是用来获取可用列表, 调用的时候会通过 callerContext 传进去
        public string PackageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public Dictionary<string, string> LocalizedDescriptions { get; set; } = new();

        public string GetLocalizedDescription(string languageCode)
        {
            if (LocalizedDescriptions.TryGetValue(languageCode, out var desc))
                return desc;
            if (LocalizedDescriptions.TryGetValue("en-US", out var defaultDesc))
                return defaultDesc;
            return Description;
        }
    }

    /// <summary>
    /// 协议打开方式选项
    /// </summary>
    public class ProtocolHandlerOption
    {
        public string UUID { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string AssociatedItem { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; }
        public bool IsUpdated { get; set; } = false; // 标记是否为新更新的选项
        public bool IsDefault { get; set; } = false; // 标记是否为当前默认
    }

    /// <summary>
    /// 插件接口
    /// </summary>
    public interface IPhobosPlugin
    {
        /// <summary>
        /// 插件元数据
        /// </summary>
        PluginMetadata Metadata { get; }

        /// <summary>
        /// 插件内容区域
        /// </summary>
        FrameworkElement? ContentArea { get; }

        /// <summary>
        /// 安装时调用
        /// </summary>
        Task<RequestResult> OnInstall(params object[] args);

        /// <summary>
        /// 启动时调用
        /// </summary>
        Task<RequestResult> OnLaunch(params object[] args);

        /// <summary>
        /// 关闭时调用
        /// </summary>
        Task<RequestResult> OnClosing(params object[] args);

        /// <summary>
        /// 卸载时调用
        /// </summary>
        Task<RequestResult> OnUninstall(params object[] args);

        /// <summary>
        /// 更新时调用
        /// </summary>
        Task<RequestResult> OnUpdate(string oldVersion, string newVersion, params object[] args);

        /// <summary>
        /// 从主程序请求数据
        /// </summary>
        Task<List<object>> RequestPhobos(params object[] args);

        /// <summary>
        /// 主程序/其他插件调用插件方法
        /// </summary>
        Task<RequestResult> Run(params object[] args);

        /// <summary>
        /// 向主程序发送链接关联请求
        /// </summary>
        Task<RequestResult> Link(LinkAssociation association);

        /// <summary>
        /// 向主程序发送命令请求
        /// </summary>
        Task<RequestResult> Request(string command, Action<RequestResult>? callback = null, params object[] args);

        /// <summary>
        /// 请求设置为默认打开方式
        /// </summary>
        Task<RequestResult> LinkDefault(string protocol);

        /// <summary>
        /// 读取插件配置
        /// </summary>
        Task<ConfigResult> ReadConfig(string key, string? targetPackageName = null);

        /// <summary>
        /// 写入插件配置
        /// </summary>
        Task<ConfigResult> WriteConfig(string key, string value, string? targetPackageName = null);

        /// <summary>
        /// 读取系统配置
        /// </summary>
        Task<ConfigResult> ReadSysConfig(string key);

        /// <summary>
        /// 写入系统配置
        /// </summary>
        Task<ConfigResult> WriteSysConfig(string key, string value);

        /// <summary>
        /// 注册随 Phobos 启动的命令
        /// </summary>
        Task<BootResult> BootWithPhobos(string command, int priority = 100, params object[] args);

        /// <summary>
        /// 取消随 Phobos 启动
        /// </summary>
        Task<BootResult> RemoveBootWithPhobos(string? uuid = null);

        /// <summary>
        /// 获取本插件的所有启动项
        /// </summary>
        Task<List<object>> GetBootItems();
    }
}