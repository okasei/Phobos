using Phobos.Components.Arcusrix.ThemeManager;
using Phobos.Manager.Arcusrix;
using Phobos.Manager.Database;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// 主题管理器插件
    /// 提供主题选择、预览、导入和创建功能
    /// </summary>
    public class PCThemePlugin : PCPluginBase
    {
        private PCOThemeManager? _content;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Theme Manager",
            PackageName = "com.phobos.theme.manager",
            Manufacturer = "Phobos Team",
            Version = "2.0.0",
            Secret = "phobos_theme_manager_secret_abc123",
            DatabaseKey = "PThemeManager",
            Icon = "Assets/theme-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "theme://show",
            SettingUri = "theme://settings",
            PreferredWidth = 960,
            PreferredHeight = 720,
            MinWindowWidth = 800,
            MinWindowHeight = 600,
            ShowMinimizeButton = true,
            ShowMaximizeButton = true,
            ShowCloseButton = true
        };

        public override FrameworkElement? ContentArea => _content;

        public PCThemePlugin()
        {
            // 订阅主题安装和卸载事件
            PMEvent.Instance.Subscribe(Metadata.PackageName, "Theme", "Installed");
            PMEvent.Instance.Subscribe(Metadata.PackageName, "Theme", "Uninstalled");
        }

        public override async Task OnEventReceived(string eventId, string eventName, params object[] args)
        {
            if (eventId == "Theme" && (eventName == "Installed" || eventName == "Uninstalled"))
            {
                // 通知 UI 刷新主题列表
                if (_content != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await _content.RefreshThemesFromExternalAsync();
                    });
                }
            }
            await base.OnEventReceived(eventId, eventName, args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _content ??= new PCOThemeManager();
                });
                return new RequestResult { Success = true, Message = "Theme manager opened" };
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("ThemeManager", ex.Message);
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            if (args.Length > 0 && args[0] is string action)
            {
                action = action.ToLowerInvariant();
                switch (action)
                {
                    case "show":
                        await OnLaunch();
                        break;

                    case "apply":
                        // theme://apply/{themeId}
                        if (args.Length > 1 && args[1] is string themeId)
                        {
                            return await ApplyTheme(themeId);
                        }
                        return new RequestResult { Success = false, Message = "Missing theme ID" };

                    case "get":
                    case "current":
                        // theme://current - 获取当前主题
                        return new RequestResult
                        {
                            Success = true,
                            Message = PMTheme.Instance.CurrentThemeId,
                            Data = [PMTheme.Instance.CurrentTheme!]
                        };

                    case "list":
                        // theme://list - 获取所有主题列表
                        var themes = PMTheme.Instance.GetAvailableThemeInfos();
                        return new RequestResult
                        {
                            Success = true,
                            Message = $"Found {themes.Count} themes",
                            Data = themes.Cast<object>().ToList()
                        };

                    case "refresh":
                        // theme://refresh - 刷新主题列表
                        await PMTheme.Instance.RefreshThemes();
                        return new RequestResult { Success = true, Message = "Themes refreshed" };

                    case "settings":
                        // theme://settings - 打开设置
                        await OnLaunch();
                        break;
                }
            }

            return await base.Run(args);
        }

        /// <summary>
        /// 应用指定主题并保存到数据库
        /// </summary>
        private async Task<RequestResult> ApplyTheme(string themeId)
        {
            try
            {
                var success = await PMTheme.Instance.LoadThemeAndSaveAsync(themeId);
                if (success)
                {
                    return new RequestResult
                    {
                        Success = true,
                        Message = $"Theme '{themeId}' applied",
                        Data = [PMTheme.Instance.CurrentTheme!]
                    };
                }
                else
                {
                    return new RequestResult
                    {
                        Success = false,
                        Message = $"Failed to apply theme '{themeId}'"
                    };
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("ThemeManager.ApplyTheme", ex.Message);
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 获取当前主题 ID
        /// </summary>
        public static string GetCurrentThemeId()
        {
            return PMTheme.Instance.CurrentThemeId;
        }

        /// <summary>
        /// 获取可用主题列表
        /// </summary>
        public static System.Collections.Generic.List<ThemeInfo> GetAvailableThemes()
        {
            return PMTheme.Instance.GetAvailableThemeInfos();
        }

        /// <summary>
        /// 应用主题（静态辅助方法）
        /// </summary>
        public static async Task<bool> ApplyThemeAsync(string themeId, bool saveToDatabase = true)
        {
            return await PMTheme.Instance.LoadThemeAndSaveAsync(themeId, saveToDatabase);
        }
    }
}
