using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin
{
    /// <summary>
    /// 插件处理器委托定义 - 带调用者上下文
    /// </summary>
    public class PluginHandlers
    {
        public Func<PluginCallerContext, object[], Task<List<object>>>? RequestPhobos { get; set; }
        public Func<PluginCallerContext, LinkAssociation, Task<RequestResult>>? Link { get; set; }
        public Func<PluginCallerContext, string, Action<RequestResult>?, object[], Task<RequestResult>>? Request { get; set; }
        public Func<PluginCallerContext, string, Task<RequestResult>>? LinkDefault { get; set; }
        public Func<PluginCallerContext, string, string?, Task<ConfigResult>>? ReadConfig { get; set; }
        public Func<PluginCallerContext, string, string, string?, Task<ConfigResult>>? WriteConfig { get; set; }
        public Func<PluginCallerContext, string, Task<ConfigResult>>? ReadSysConfig { get; set; }
        public Func<PluginCallerContext, string, string, Task<ConfigResult>>? WriteSysConfig { get; set; }
        public Func<PluginCallerContext, string, int, object[], Task<BootResult>>? BootWithPhobos { get; set; }
        public Func<PluginCallerContext, string?, Task<BootResult>>? RemoveBootWithPhobos { get; set; }
        public Func<PluginCallerContext, Task<List<object>>>? GetBootItems { get; set; }
    }

    /// <summary>
    /// 插件基类，提供默认实现
    /// </summary>
    public abstract class PCPluginBase : IPhobosPlugin
    {
        private PluginHandlers? _handlers;

        /// <summary>
        /// 插件元数据
        /// </summary>
        public abstract PluginMetadata Metadata { get; }

        /// <summary>
        /// 插件内容区域
        /// </summary>
        public virtual FrameworkElement? ContentArea { get; protected set; }

        /// <summary>
        /// 获取当前插件的调用者上下文
        /// </summary>
        protected PluginCallerContext GetCallerContext()
        {
            return new PluginCallerContext
            {
                PackageName = Metadata.PackageName,
                DatabaseKey = Metadata.DatabaseKey,
                IsTrusted = false, // 默认不信任，由主程序判断
                CallTime = DateTime.Now
            };
        }

        /// <summary>
        /// 设置主程序处理器
        /// </summary>
        public void SetPhobosHandlers(PluginHandlers handlers)
        {
            _handlers = handlers;
        }

        public virtual async Task<RequestResult> OnInstall(params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = "Installed successfully" });
        }

        public virtual async Task<RequestResult> OnLaunch(params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = "Launched successfully" });
        }

        public virtual async Task<RequestResult> OnClosing(params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = "Closing" });
        }

        public virtual async Task<RequestResult> OnUninstall(params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = "Uninstalled successfully" });
        }

        public virtual async Task<RequestResult> OnUpdate(string oldVersion, string newVersion, params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = $"Updated from {oldVersion} to {newVersion}" });
        }

        public virtual async Task<List<object>> RequestPhobos(params object[] args)
        {
            if (_handlers?.RequestPhobos != null)
                return await _handlers.RequestPhobos(GetCallerContext(), args);
            return new List<object>();
        }

        public virtual async Task<RequestResult> Run(params object[] args)
        {
            return await Task.FromResult(new RequestResult { Success = true, Message = "Run completed" });
        }

        public virtual async Task<RequestResult> Link(LinkAssociation association)
        {
            if (_handlers?.Link != null)
                return await _handlers.Link(GetCallerContext(), association);
            return new RequestResult { Success = false, Message = "Link handler not set" };
        }

        public virtual async Task<RequestResult> Request(string command, Action<RequestResult>? callback = null, params object[] args)
        {
            if (_handlers?.Request != null)
                return await _handlers.Request(GetCallerContext(), command, callback, args);
            return new RequestResult { Success = false, Message = "Request handler not set" };
        }

        public virtual async Task<RequestResult> LinkDefault(string protocol)
        {
            if (_handlers?.LinkDefault != null)
                return await _handlers.LinkDefault(GetCallerContext(), protocol);
            return new RequestResult { Success = false, Message = "LinkDefault handler not set" };
        }

        public virtual async Task<ConfigResult> ReadConfig(string key, string? targetPackageName = null)
        {
            if (_handlers?.ReadConfig != null)
                return await _handlers.ReadConfig(GetCallerContext(), key, targetPackageName ?? Metadata.PackageName);
            return new ConfigResult { Success = false, Key = key, Message = "ReadConfig handler not set" };
        }

        public virtual async Task<ConfigResult> WriteConfig(string key, string value, string? targetPackageName = null)
        {
            if (_handlers?.WriteConfig != null)
                return await _handlers.WriteConfig(GetCallerContext(), key, value, targetPackageName ?? Metadata.PackageName);
            return new ConfigResult { Success = false, Key = key, Message = "WriteConfig handler not set" };
        }

        public virtual async Task<ConfigResult> ReadSysConfig(string key)
        {
            if (_handlers?.ReadSysConfig != null)
                return await _handlers.ReadSysConfig(GetCallerContext(), key);
            return new ConfigResult { Success = false, Key = key, Message = "ReadSysConfig handler not set" };
        }

        public virtual async Task<ConfigResult> WriteSysConfig(string key, string value)
        {
            if (_handlers?.WriteSysConfig != null)
                return await _handlers.WriteSysConfig(GetCallerContext(), key, value);
            return new ConfigResult { Success = false, Key = key, Message = "WriteSysConfig handler not set" };
        }

        public virtual async Task<BootResult> BootWithPhobos(string command, int priority = 100, params object[] args)
        {
            if (_handlers?.BootWithPhobos != null)
                return await _handlers.BootWithPhobos(GetCallerContext(), command, priority, args);
            return new BootResult { Success = false, Message = "BootWithPhobos handler not set" };
        }

        public virtual async Task<BootResult> RemoveBootWithPhobos(string? uuid = null)
        {
            if (_handlers?.RemoveBootWithPhobos != null)
                return await _handlers.RemoveBootWithPhobos(GetCallerContext(), uuid);
            return new BootResult { Success = false, Message = "RemoveBootWithPhobos handler not set" };
        }

        public virtual async Task<List<object>> GetBootItems()
        {
            if (_handlers?.GetBootItems != null)
                return await _handlers.GetBootItems(GetCallerContext());
            return new List<object>();
        }
    }
}