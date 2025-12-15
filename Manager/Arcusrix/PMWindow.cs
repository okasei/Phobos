using Phobos.Class.Arcusrix;
using Phobos.Interface.Arcusrix;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Phobos.Manager.Arcusrix
{
    /// <summary>
    /// 窗口管理器实现
    /// </summary>
    public class PMWindow : PIWindowManager
    {
        private static PMWindow? _instance;
        private static readonly object _lock = new();

        private readonly List<Window> _openWindows = new();
        // 单窗口插件：一个包名对应一个窗口
        private readonly Dictionary<string, Window> _pluginWindows = new(StringComparer.OrdinalIgnoreCase);
        // 多窗口插件：一个包名可以对应多个窗口
        private readonly Dictionary<string, List<Window>> _multiPluginWindows = new(StringComparer.OrdinalIgnoreCase);

        public static PMWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMWindow();
                    }
                }
                return _instance;
            }
        }

        public Window? MainWindow => Application.Current?.MainWindow;
        public IReadOnlyList<Window> OpenWindows => _openWindows.AsReadOnly();

        public event EventHandler<WindowCreatedEventArgs>? WindowCreated;
        public event EventHandler<WindowClosedEventArgs>? WindowClosed;

        public Window CreatePluginWindow(IPhobosPlugin plugin, string? title = null)
        {
            var packageName = plugin.Metadata.PackageName;
            var allowMultiWindow = plugin.Metadata.AllowMultiWindow;

            if (allowMultiWindow)
            {
                // 多窗口模式：不关闭已有窗口，直接创建新窗口
                return CreateMultiWindow(plugin, packageName, title);
            }
            else
            {
                // 单窗口模式：如果已存在窗口，先关闭
                return CreateSingleWindow(plugin, packageName, title);
            }
        }

        private Window CreateSingleWindow(IPhobosPlugin plugin, string packageName, string? title)
        {
            // 如果已存在窗口，先关闭
            if (_pluginWindows.TryGetValue(packageName, out var existing))
            {
                CloseWindow(existing);
            }

            var window = new PCPluginWindow(plugin, title);

            window.SetIcon(BitmapFrame.Create(new Uri("pack://application:,,,/Assets/Icons/phobos_icon_odda.ico", UriKind.Absolute))); 

            window.Closed += (s, e) =>
            {
                _openWindows.Remove(window);
                _pluginWindows.Remove(packageName);
                WindowClosed?.Invoke(this, new WindowClosedEventArgs
                {
                    Window = window,
                    PluginPackageName = packageName
                });
            };

            _openWindows.Add(window);
            _pluginWindows[packageName] = window;

            WindowCreated?.Invoke(this, new WindowCreatedEventArgs
            {
                Window = window,
                PluginPackageName = packageName
            });

            return window;
        }

        private Window CreateMultiWindow(IPhobosPlugin plugin, string packageName, string? title)
        {
            var window = new PCPluginWindow(plugin, title);

            window.SetIcon(BitmapFrame.Create(new Uri("pack://application:,,,/Assets/Icons/phobos_icon_odda.ico", UriKind.Absolute)));

            // 确保列表存在
            if (!_multiPluginWindows.ContainsKey(packageName))
            {
                _multiPluginWindows[packageName] = new List<Window>();
            }

            window.Closed += (s, e) =>
            {
                _openWindows.Remove(window);
                if (_multiPluginWindows.TryGetValue(packageName, out var windowList))
                {
                    windowList.Remove(window);
                    if (windowList.Count == 0)
                    {
                        _multiPluginWindows.Remove(packageName);
                    }
                }
                WindowClosed?.Invoke(this, new WindowClosedEventArgs
                {
                    Window = window,
                    PluginPackageName = packageName
                });
            };

            _openWindows.Add(window);
            _multiPluginWindows[packageName].Add(window);

            WindowCreated?.Invoke(this, new WindowCreatedEventArgs
            {
                Window = window,
                PluginPackageName = packageName
            });

            return window;
        }

        public void ShowWindow(Window window)
        {
            if (window == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                window.Show();
                window.Activate();
            });
        }

        public void CloseWindow(Window window)
        {
            if (window == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                window.Close();
            });
        }

        public void CloseAllWindows()
        {
            var windowsToClose = _openWindows.ToList();
            foreach (var window in windowsToClose)
            {
                CloseWindow(window);
            }
        }

        /// <summary>
        /// 获取插件的单窗口（仅用于单窗口模式的插件）
        /// </summary>
        public Window? GetPluginWindow(string packageName)
        {
            _pluginWindows.TryGetValue(packageName, out var window);
            return window;
        }

        /// <summary>
        /// 获取插件的所有窗口（用于多窗口模式的插件）
        /// </summary>
        public IReadOnlyList<Window> GetPluginWindows(string packageName)
        {
            if (_multiPluginWindows.TryGetValue(packageName, out var windows))
            {
                return windows.AsReadOnly();
            }
            // 也检查单窗口字典
            if (_pluginWindows.TryGetValue(packageName, out var singleWindow))
            {
                return new List<Window> { singleWindow }.AsReadOnly();
            }
            return new List<Window>().AsReadOnly();
        }

        /// <summary>
        /// 关闭指定插件的所有窗口
        /// </summary>
        public void ClosePluginWindows(string packageName)
        {
            // 关闭多窗口
            if (_multiPluginWindows.TryGetValue(packageName, out var windows))
            {
                var windowsToClose = windows.ToList();
                foreach (var window in windowsToClose)
                {
                    CloseWindow(window);
                }
            }
            // 关闭单窗口
            if (_pluginWindows.TryGetValue(packageName, out var singleWindow))
            {
                CloseWindow(singleWindow);
            }
        }
    }
}