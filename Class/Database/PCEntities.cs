using System;
using System.IO;

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
        /// <summary>
        /// 包名（主键）
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 制造商
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 密钥
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// 安装时间
        /// </summary>
        public DateTime InstallTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 安装目录
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// 图标路径（相对于插件目录）
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 是否为系统插件
        /// 系统插件不可被用户卸载
        /// </summary>
        public bool IsSystemPlugin { get; set; } = false;

        /// <summary>
        /// 设置页面 URI
        /// 例如: "log://settings"
        /// </summary>
        public string SettingUri { get; set; } = string.Empty;

        /// <summary>
        /// 卸载信息 JSON
        /// </summary>
        public string UninstallInfo { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取图标完整路径
        /// </summary>
        public string? GetIconFullPath()
        {
            if (string.IsNullOrEmpty(Icon) || string.IsNullOrEmpty(Directory))
                return null;
            return Path.Combine(Directory, Icon);
        }

        /// <summary>
        /// 是否有设置页面
        /// </summary>
        public bool HasSettings => !string.IsNullOrEmpty(SettingUri);

        /// <summary>
        /// 获取解析后的卸载信息
        /// </summary>
        public Phobos.Shared.Interface.PluginUninstallInfo? GetUninstallInfo()
        {
            if (string.IsNullOrEmpty(UninstallInfo))
                return null;

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Phobos.Shared.Interface.PluginUninstallInfo>(UninstallInfo);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置卸载信息
        /// </summary>
        public void SetUninstallInfo(Phobos.Shared.Interface.PluginUninstallInfo? info)
        {
            if (info == null)
            {
                UninstallInfo = string.Empty;
            }
            else
            {
                UninstallInfo = Newtonsoft.Json.JsonConvert.SerializeObject(info);
            }
        }

        /// <summary>
        /// 是否可以卸载
        /// </summary>
        public bool CanUninstall
        {
            get
            {
                if (!IsSystemPlugin)
                    return true;

                var info = GetUninstallInfo();
                return info?.AllowUninstall ?? true;
            }
        }
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