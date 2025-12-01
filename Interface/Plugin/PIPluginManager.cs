using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phobos.Shared.Interface;

namespace Phobos.Interface.Plugin
{
    /// <summary>
    /// 插件状态
    /// </summary>
    public enum PluginState
    {
        NotInstalled,
        Installed,
        Loaded,
        Running,
        Suspended,
        Error
    }

    /// <summary>
    /// 插件加载上下文
    /// </summary>
    public class PluginLoadContext
    {
        public string PackageName { get; set; } = string.Empty;
        public string PluginPath { get; set; } = string.Empty;
        public IPhobosPlugin? Instance { get; set; }
        public PluginState State { get; set; } = PluginState.NotInstalled;
        public DateTime LoadTime { get; set; }
        public Exception? LastError { get; set; }
    }

    /// <summary>
    /// 插件安装选项
    /// </summary>
    public class PluginInstallOptions
    {
        public bool IgnoreDependencies { get; set; } = false;
        public bool ForceReinstall { get; set; } = false;
        public bool Silent { get; set; } = false;
    }

    /// <summary>
    /// 插件管理器接口
    /// </summary>
    public interface PIPluginManager
    {
        /// <summary>
        /// 当前加载的插件列表
        /// </summary>
        IReadOnlyDictionary<string, PluginLoadContext> LoadedPlugins { get; }

        /// <summary>
        /// 安装插件
        /// </summary>
        Task<RequestResult> Install(string pluginPath, PluginInstallOptions? options = null);

        /// <summary>
        /// 卸载插件
        /// </summary>
        Task<RequestResult> Uninstall(string packageName, bool force = false);

        /// <summary>
        /// 加载插件
        /// </summary>
        Task<RequestResult> Load(string packageName);

        /// <summary>
        /// 卸载插件（从内存）
        /// </summary>
        Task<RequestResult> Unload(string packageName);

        /// <summary>
        /// 启动插件
        /// </summary>
        Task<RequestResult> Launch(string packageName, params object[] args);

        /// <summary>
        /// 停止插件
        /// </summary>
        Task<RequestResult> Stop(string packageName);

        /// <summary>
        /// 更新插件
        /// </summary>
        Task<RequestResult> Update(string packageName, string newPluginPath);

        /// <summary>
        /// 获取插件实例
        /// </summary>
        IPhobosPlugin? GetPlugin(string packageName);

        /// <summary>
        /// 获取插件状态
        /// </summary>
        PluginState GetPluginState(string packageName);

        /// <summary>
        /// 获取所有已安装插件信息
        /// </summary>
        Task<List<PluginMetadata>> GetInstalledPlugins();

        /// <summary>
        /// 检查依赖
        /// </summary>
        Task<RequestResult> CheckDependencies(PluginMetadata metadata);

        /// <summary>
        /// 向插件发送命令
        /// </summary>
        Task<RequestResult> SendCommand(string packageName, string command, params object[] args);

        /// <summary>
        /// 插件间通信
        /// </summary>
        Task<RequestResult> PluginToPlugin(string sourcePackage, string targetPackage, string message, params object[] args);
    }
}