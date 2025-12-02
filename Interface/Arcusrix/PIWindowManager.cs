using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Shared.Interface;

namespace Phobos.Interface.Arcusrix
{
    /// <summary>
    /// 窗口管理器接口
    /// </summary>
    public interface PIWindowManager
    {
        /// <summary>
        /// 主窗口
        /// </summary>
        Window? MainWindow { get; }

        /// <summary>
        /// 所有打开的窗口
        /// </summary>
        IReadOnlyList<Window> OpenWindows { get; }

        /// <summary>
        /// 创建插件窗口
        /// </summary>
        Window CreatePluginWindow(IPhobosPlugin plugin, string? title = null);

        /// <summary>
        /// 显示窗口
        /// </summary>
        void ShowWindow(Window window);

        /// <summary>
        /// 关闭窗口
        /// </summary>
        void CloseWindow(Window window);

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        void CloseAllWindows();

        /// <summary>
        /// 获取插件窗口
        /// </summary>
        Window? GetPluginWindow(string packageName);

        /// <summary>
        /// 窗口创建事件
        /// </summary>
        event EventHandler<WindowCreatedEventArgs>? WindowCreated;

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        event EventHandler<WindowClosedEventArgs>? WindowClosed;
    }

    public class WindowCreatedEventArgs : EventArgs
    {
        public Window Window { get; set; } = null!;
        public string? PluginPackageName { get; set; }
    }

    public class WindowClosedEventArgs : EventArgs
    {
        public Window Window { get; set; } = null!;
        public string? PluginPackageName { get; set; }
    }
}