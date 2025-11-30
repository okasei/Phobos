using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Phobos.Shared.Interface;

namespace Phobos.Interface.System
{
    /// <summary>
    /// 主题管理器接口
    /// </summary>
    public interface PIThemeManager
    {
        /// <summary>
        /// 当前主题
        /// </summary>
        IPhobosTheme? CurrentTheme { get; }

        /// <summary>
        /// 当前主题ID
        /// </summary>
        string CurrentThemeId { get; }

        /// <summary>
        /// 可用主题列表
        /// </summary>
        IReadOnlyList<IPhobosTheme> AvailableThemes { get; }

        /// <summary>
        /// 加载主题
        /// </summary>
        Task<bool> LoadTheme(string themeId);

        /// <summary>
        /// 注册主题
        /// </summary>
        void RegisterTheme(IPhobosTheme theme);

        /// <summary>
        /// 注销主题
        /// </summary>
        void UnregisterTheme(string themeId);

        /// <summary>
        /// 获取主题
        /// </summary>
        IPhobosTheme? GetTheme(string themeId);

        /// <summary>
        /// 应用主题到窗口
        /// </summary>
        void ApplyThemeToWindow(Window window);

        /// <summary>
        /// 主题变更事件
        /// </summary>
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    }

    /// <summary>
    /// 主题变更事件参数
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string OldThemeId { get; set; } = string.Empty;
        public string NewThemeId { get; set; } = string.Empty;
        public IPhobosTheme? NewTheme { get; set; }
    }
}