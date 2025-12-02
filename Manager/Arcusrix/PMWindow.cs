using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Phobos.Class.Arcusrix;
using Phobos.Interface.Arcusrix;
using Phobos.Shared.Interface;

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
        private readonly Dictionary<string, Window> _pluginWindows = new(StringComparer.OrdinalIgnoreCase);

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

            // 如果已存在窗口，先关闭
            if (_pluginWindows.TryGetValue(packageName, out var existing))
            {
                CloseWindow(existing);
            }

            var window = new PCPluginWindow(plugin, title);

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

        public Window? GetPluginWindow(string packageName)
        {
            _pluginWindows.TryGetValue(packageName, out var window);
            return window;
        }
    }
}