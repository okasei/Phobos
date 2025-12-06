using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Phobos.Components.Arcusrix.Installer;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// 插件安装器插件（UI 位于 Phobos.Components.Arcusrix.Installer）
    /// </summary>
    public class PCPluginInstaller : PCPluginBase
    {
        private PCOInstaller? _content;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Plugin Installer",
            PackageName = "com.phobos.plugin.installer",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_plugin_installer_secret_a1b2c3",
            DatabaseKey = "PInstaller",
            Icon = "Assets/installer-icon.png",
            IsSystemPlugin = true,
            LaunchFlag = true,
            Entry = "pi://show",
            SettingUri = "pi://settings",
            PreferredWidth = 550,
            PreferredHeight = 500,
            MinWindowWidth = 450,
            MinWindowHeight = 400,
            ShowMinimizeButton = true,
            ShowMaximizeButton = false,
            ShowCloseButton = true,
            UninstallInfo = new PluginUninstallInfo
            {
                AllowUninstall = false,
                Title = "Cannot Uninstall System Plugin",
                Message = "Plugin Installer is a core component of Phobos and cannot be uninstalled.",
                LocalizedTitles = new Dictionary<string, string>
                {
                    { "en-US", "Cannot Uninstall System Plugin" },
                    { "zh-CN", "无法卸载系统插件" },
                    { "zh-TW", "無法卸載系統插件" },
                    { "ja-JP", "システムプラグインをアンインストールできません" }
                },
                LocalizedMessages = new Dictionary<string, string>
                {
                    { "en-US", "Plugin Installer is a core component of Phobos and cannot be uninstalled." },
                    { "zh-CN", "插件安装器是 Phobos 的核心组件，无法卸载。" },
                    { "zh-TW", "插件安裝器是 Phobos 的核心元件，無法卸載。" },
                    { "ja-JP", "プラグインインストーラーは Phobos のコアコンポーネントであり、アンインストールできません。" }
                }
            },
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Plugin Installer" },
                { "zh-CN", "插件安装器" },
                { "zh-TW", "插件安裝器" },
                { "ja-JP", "プラグインインストーラー" },
                { "ko-KR", "플러그인 설치 프로그램" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Install plugins from DLL files" },
                { "zh-CN", "从 DLL 文件安装插件" },
                { "zh-TW", "從 DLL 檔案安裝插件" },
                { "ja-JP", "DLLファイルからプラグインをインストール" },
                { "ko-KR", "DLL 파일에서 플러그인 설치" }
            }
        };

        public override FrameworkElement? ContentArea => _content;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议，允许通过协议打开安装器
            // 格式: pi://install?path=C:\path\to\plugin.dll
            await Link(new LinkAssociation
            {
                Protocol = "pi",
                Name = "PluginInstallerHandler_General",
                Description = "Plugin Installer Protocol Handler",
                Command = "pi://%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            try
            {
                // 创建 UI 实例
                _content = new PCOInstaller();
                _content.SetHostPlugin(this);

                // 订阅事件
                _content.InstallCompleted += OnInstallCompleted;
                _content.ExitRequested += OnExitRequested;

                // 检查是否有启动参数（调用模式）
                if (args.Length > 0)
                {
                    var firstArg = args[0]?.ToString();

                    // 处理 URI 格式: pi://install?path=xxx 或直接路径
                    var pluginPath = ParsePluginPath(firstArg);

                    if (!string.IsNullOrEmpty(pluginPath))
                    {
                        // 调用模式：直接加载指定的插件
                        await _content.LoadFromUri(pluginPath);
                    }
                    else
                    {
                        // 用户打开模式
                        _content.SetMode(InstallerMode.UserOpen);
                    }
                }
                else
                {
                    // 用户打开模式
                    _content.SetMode(InstallerMode.UserOpen);
                }

                return await base.OnLaunch(args);
            }
            catch (Exception ex)
            {
                return new RequestResult { Success = false, Message = ex.Message, Error = ex };
            }
        }

        /// <summary>
        /// 解析插件路径
        /// </summary>
        /// <param name="input">输入字符串，可能是 URI 或直接路径</param>
        /// <returns>插件文件路径</returns>
        private string? ParsePluginPath(string? input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            // 如果是 URI 格式 (pi://install?path=xxx)
            if (input.StartsWith("pi://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(input);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var path = query["path"];
                    if (!string.IsNullOrEmpty(path))
                    {
                        return Uri.UnescapeDataString(path);
                    }

                    // 也支持 pi://install/C:/path/to/plugin.dll 格式
                    if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                    {
                        var absolutePath = uri.AbsolutePath.TrimStart('/');
                        return Uri.UnescapeDataString(absolutePath);
                    }
                }
                catch
                {
                    // URI 解析失败
                }
                return null;
            }

            // 如果是直接的文件路径
            if (input.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            return null;
        }

        /// <summary>
        /// 安装完成回调
        /// </summary>
        private void OnInstallCompleted(object? sender, InstallCompletedEventArgs e)
        {
            if (e.Success)
            {
                // 可以在这里添加日志或通知
                PCLoggerPlugin.Info("PluginInstaller", $"Plugin installed successfully: {e.PackageName}");
            }
            else
            {
                PCLoggerPlugin.Warning("PluginInstaller", $"Plugin installation failed: {e.Message}");
            }
        }

        /// <summary>
        /// 退出请求回调
        /// </summary>
        private void OnExitRequested(object? sender, EventArgs e)
        {
            // 通知宿主关闭插件窗口
            // 这里的实现取决于 Phobos 的窗口管理机制
        }

        public override Task<RequestResult> Run(params object[] args)
        {
            try
            {
                // 提供命令接口
                if (args.Length > 0 && args[0] is string action)
                {
                    switch (action.ToLowerInvariant())
                    {
                        case "install":
                            // 通过命令安装: Run("install", "C:\path\to.dll")
                            if (args.Length > 1 && args[1] is string path)
                            {
                                _ = PMPlugin.Instance.Install(path);
                                return Task.FromResult(new RequestResult
                                {
                                    Success = true,
                                    Message = "Install started"
                                });
                            }
                            return Task.FromResult(new RequestResult
                            {
                                Success = false,
                                Message = "No plugin path specified"
                            });

                        case "open":
                            // 打开安装器并直接加载指定插件: Run("open", "C:\path\to.dll")
                            if (args.Length > 1 && args[1] is string openPath)
                            {
                                if (_content != null)
                                {
                                    _ = _content.LoadFromUri(openPath);
                                    return Task.FromResult(new RequestResult
                                    {
                                        Success = true,
                                        Message = "Plugin loaded in installer"
                                    });
                                }
                            }
                            return Task.FromResult(new RequestResult
                            {
                                Success = false,
                                Message = "Installer not ready or no path specified"
                            });
                    }
                }

                return base.Run(args);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PluginInstaller", $"Command failed: {ex.Message}");
                return Task.FromResult(new RequestResult { Success = false, Message = ex.Message, Error = ex });
            }
        }

        public override async Task<RequestResult> OnClosing(params object[] args)
        {
            // 清理事件订阅
            if (_content != null)
            {
                _content.InstallCompleted -= OnInstallCompleted;
                _content.ExitRequested -= OnExitRequested;
            }

            return await base.OnClosing(args);
        }
    }
}