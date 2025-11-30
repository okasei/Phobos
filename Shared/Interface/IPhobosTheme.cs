using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace Phobos.Shared.Interface
{
    /// <summary>
    /// 动画类型枚举
    /// </summary>
    [Flags]
    public enum AnimationType
    {
        None = 0,
        FadeIn = 1,
        FadeOut = 2,
        SlideLeft = 4,
        SlideRight = 8,
        SlideUp = 16,
        SlideDown = 32,
        ScaleIn = 64,
        ScaleOut = 128,
        Rotate = 256,
        Bounce = 512
    }

    /// <summary>
    /// 动画配置
    /// </summary>
    public class AnimationConfig
    {
        public AnimationType Types { get; set; } = AnimationType.None;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public IEasingFunction? EasingFunction { get; set; }
    }

    /// <summary>
    /// 控件动画配置
    /// </summary>
    public class ControlAnimationConfig
    {
        public AnimationConfig OnLoad { get; set; } = new();
        public AnimationConfig OnRestore { get; set; } = new();
    }

    /// <summary>
    /// 主题接口
    /// </summary>
    public interface IPhobosTheme
    {
        /// <summary>
        /// 主题名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 主题唯一标识
        /// </summary>
        string ThemeId { get; }

        /// <summary>
        /// 主题版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 主题作者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 获取本地化名称
        /// </summary>
        string GetLocalizedName(string languageCode);

        /// <summary>
        /// 获取全局样式资源字典
        /// </summary>
        ResourceDictionary GetGlobalStyles();

        /// <summary>
        /// 获取指定控件类型的样式
        /// </summary>
        Style? GetControlStyle(Type controlType);

        /// <summary>
        /// 获取指定控件类型的动画配置
        /// </summary>
        ControlAnimationConfig GetControlAnimationConfig(Type controlType);

        /// <summary>
        /// 获取默认动画配置
        /// </summary>
        ControlAnimationConfig GetDefaultAnimationConfig();

        /// <summary>
        /// 应用主题
        /// </summary>
        void Apply();

        /// <summary>
        /// 卸载主题
        /// </summary>
        void Unload();
    }
}