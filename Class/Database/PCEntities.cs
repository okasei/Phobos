using System;

namespace Phobos.Class.Database
{
    /// <summary>
    /// 主配置表实体
    /// </summary>
    public class PCPhobosMain
    {
        public string Key { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string UpdateUID { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string LastValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 插件表实体
    /// </summary>
    public class PCPhobosPlugin
    {
        public string PackageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Secret { get; set; } = string.Empty;
        public string Language { get; set; } = "en-US";
        public DateTime InstallTime { get; set; } = DateTime.Now;
        public string Directory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 插件配置表实体
    /// </summary>
    public class PCPhobosAppdata
    {
        public string UKey { get; set; } = string.Empty; // 格式: 插件包名_配置项名称
        public string PackageName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string UpdateUID { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string LastValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 协议关联表实体 - 存储所有可用的协议打开方式
    /// </summary>
    public class PCPhobosProtocol
    {
        public string UUID { get; set; } = Guid.NewGuid().ToString("N");
        public string Protocol { get; set; } = string.Empty;
        public string AssociatedItem { get; set; } = string.Empty;
        public string UpdateUID { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string LastValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 关联项配置表实体
    /// </summary>
    public class PCPhobosAssociatedItem
    {
        public string Name { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty; // %1 代替原链接, %0 代替不含协议头的链接
    }

    /// <summary>
    /// 启动项表实体 - 记录随 Phobos 一起启动的项目
    /// </summary>
    public class PCPhobosBoot
    {
        public string UUID { get; set; } = Guid.NewGuid().ToString("N");
        public string Command { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 100; // 数字越小优先级越高
    }

    /// <summary>
    /// 默认打开方式表实体 - 记录协议的默认打开方式
    /// </summary>
    public class PCPhobosShell
    {
        public string Protocol { get; set; } = string.Empty; // 主键
        public string AssociatedItem { get; set; } = string.Empty;
        public string UpdateUID { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string LastValue { get; set; } = string.Empty; // 上一个默认打开方式
    }
}