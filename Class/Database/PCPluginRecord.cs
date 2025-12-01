using System;
using System.IO;
using Phobos.Shared.Interface;

namespace Phobos.Class.Database
{
    /// <summary>
    /// 数据库中的插件记录（业务逻辑层）
    /// 与 PCPhobosPlugin 实体类对应，但 UninstallInfo 已反序列化
    /// </summary>
    public class PluginRecord
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
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

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
        /// </summary>
        public bool IsSystemPlugin { get; set; } = false;

        /// <summary>
        /// 设置页面 URI
        /// </summary>
        public string SettingUri { get; set; } = string.Empty;

        /// <summary>
        /// 卸载信息（已反序列化）
        /// </summary>
        public PluginUninstallInfo? UninstallInfo { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

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
        /// 是否可以卸载
        /// </summary>
        public bool CanUninstall => !IsSystemPlugin || (UninstallInfo?.AllowUninstall ?? true);

        /// <summary>
        /// 转换为 PluginMetadata
        /// </summary>
        public PluginMetadata ToMetadata()
        {
            return new PluginMetadata
            {
                PackageName = PackageName,
                Name = Name,
                Manufacturer = Manufacturer,
                Description = Description,
                Version = Version,
                Secret = Secret,
                Icon = Icon,
                IsSystemPlugin = IsSystemPlugin,
                SettingUri = SettingUri,
                UninstallInfo = UninstallInfo
            };
        }

        /// <summary>
        /// 转换为 PCPhobosPlugin 实体
        /// </summary>
        public PCPhobosPlugin ToEntity()
        {
            var entity = new PCPhobosPlugin
            {
                PackageName = PackageName,
                Name = Name,
                Manufacturer = Manufacturer,
                Description = Description,
                Version = Version,
                Secret = Secret,
                Language = Language,
                InstallTime = InstallTime,
                UpdateTime = UpdateTime,
                Directory = Directory,
                Icon = Icon,
                IsSystemPlugin = IsSystemPlugin,
                SettingUri = SettingUri,
                IsEnabled = IsEnabled
            };
            entity.SetUninstallInfo(UninstallInfo);
            return entity;
        }

        /// <summary>
        /// 从 PluginMetadata 创建记录
        /// </summary>
        public static PluginRecord FromMetadata(PluginMetadata metadata, string directory)
        {
            return new PluginRecord
            {
                PackageName = metadata.PackageName,
                Name = metadata.Name,
                Manufacturer = metadata.Manufacturer,
                Description = metadata.Description,
                Version = metadata.Version,
                Secret = metadata.Secret,
                Directory = directory,
                Icon = metadata.Icon ?? string.Empty,
                IsSystemPlugin = metadata.IsSystemPlugin,
                SettingUri = metadata.SettingUri ?? string.Empty,
                UninstallInfo = metadata.UninstallInfo,
                IsEnabled = true,
                InstallTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 从 PCPhobosPlugin 实体创建记录
        /// </summary>
        public static PluginRecord FromEntity(PCPhobosPlugin entity)
        {
            return new PluginRecord
            {
                PackageName = entity.PackageName,
                Name = entity.Name,
                Manufacturer = entity.Manufacturer,
                Description = entity.Description,
                Version = entity.Version,
                Secret = entity.Secret,
                Language = entity.Language,
                InstallTime = entity.InstallTime,
                UpdateTime = entity.UpdateTime,
                Directory = entity.Directory,
                Icon = entity.Icon,
                IsSystemPlugin = entity.IsSystemPlugin,
                SettingUri = entity.SettingUri,
                UninstallInfo = entity.GetUninstallInfo(),
                IsEnabled = entity.IsEnabled
            };
        }
    }
}