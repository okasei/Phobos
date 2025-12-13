using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

namespace Phobos.Utils.Media
{
    /// <summary>
    /// 动画工具类 - 提供统一的动画API
    /// </summary>
    public static class PUAnimation
    {
        #region 常量

        /// <summary>
        /// 默认动画时长（毫秒）
        /// </summary>
        public const int DefaultDuration = 200;

        /// <summary>
        /// 快速动画时长（毫秒）
        /// </summary>
        public const int FastDuration = 150;

        /// <summary>
        /// 慢速动画时长（毫秒）
        /// </summary>
        public const int SlowDuration = 350;

        /// <summary>
        /// 默认滑动偏移量
        /// </summary>
        public const double DefaultSlideOffset = 30;

        #endregion

        #region 缓动函数工厂

        /// <summary>
        /// 创建平滑缓动函数 (CubicEase)
        /// </summary>
        public static CubicEase CreateSmoothEase(EasingMode mode = EasingMode.EaseOut)
        {
            return new CubicEase { EasingMode = mode };
        }

        /// <summary>
        /// 创建二次缓动函数 (QuadraticEase)
        /// </summary>
        public static QuadraticEase CreateQuadraticEase(EasingMode mode = EasingMode.EaseOut)
        {
            return new QuadraticEase { EasingMode = mode };
        }

        /// <summary>
        /// 创建回弹缓动函数 (BackEase)
        /// </summary>
        public static BackEase CreateBackEase(EasingMode mode = EasingMode.EaseOut, double amplitude = 0.3)
        {
            return new BackEase
            {
                Amplitude = amplitude,
                EasingMode = mode
            };
        }

        /// <summary>
        /// 创建指数缓动函数 (ExponentialEase)
        /// </summary>
        public static ExponentialEase CreateExpEase(EasingMode mode = EasingMode.EaseOut, double exponent = 4)
        {
            return new ExponentialEase
            {
                Exponent = exponent,
                EasingMode = mode
            };
        }

        /// <summary>
        /// 创建弹性缓动函数 (ElasticEase)
        /// </summary>
        public static ElasticEase CreateElasticEase(EasingMode mode = EasingMode.EaseOut, int oscillations = 1, double springiness = 8)
        {
            return new ElasticEase
            {
                EasingMode = mode,
                Oscillations = oscillations,
                Springiness = springiness
            };
        }

        #endregion

        #region 透明度动画

        /// <summary>
        /// 淡入动画
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="duration">时长（毫秒）</param>
        /// <param name="from">起始透明度</param>
        /// <param name="to">目标透明度</param>
        /// <param name="easing">缓动函数</param>
        /// <param name="delay">延迟（毫秒）</param>
        /// <param name="onCompleted">完成回调</param>
        public static void FadeIn(UIElement element, int duration = DefaultDuration, double from = 0, double to = 1,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            AnimateOpacity(element, from, to, duration, easing ?? CreateSmoothEase(), delay, onCompleted);
        }

        /// <summary>
        /// 淡出动画
        /// </summary>
        public static void FadeOut(UIElement element, int duration = DefaultDuration, double from = 1, double to = 0,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            AnimateOpacity(element, from, to, duration, easing ?? CreateSmoothEase(EasingMode.EaseIn), delay, onCompleted);
        }

        /// <summary>
        /// 透明度动画
        /// </summary>
        public static void AnimateOpacity(UIElement element, double from, double to, int duration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        /// <summary>
        /// 透明度动画（仅指定目标值）
        /// </summary>
        public static void AnimateOpacityTo(UIElement element, double targetOpacity, int duration = FastDuration,
            IEasingFunction? easing = null)
        {
            var animation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateQuadraticEase()
            };
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        #endregion

        #region 缩放动画

        /// <summary>
        /// 缩放动画
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="fromScale">起始缩放比例</param>
        /// <param name="toScale">目标缩放比例</param>
        /// <param name="duration">时长（毫秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <param name="delay">延迟（毫秒）</param>
        /// <param name="onCompleted">完成回调</param>
        public static void Scale(FrameworkElement element, double fromScale, double toScale, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            EnsureScaleTransform(element);
            var scaleTransform = GetScaleTransform(element);
            if (scaleTransform == null) return;

            var animX = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            var animY = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animX.Completed += (s, e) => onCompleted();
            }

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
        }

        /// <summary>
        /// 缩放动画（仅指定目标值）
        /// </summary>
        public static void ScaleTo(FrameworkElement element, double targetScale, int duration = FastDuration,
            IEasingFunction? easing = null)
        {
            EnsureScaleTransform(element);
            var scaleTransform = GetScaleTransform(element);
            if (scaleTransform == null) return;

            var animation = new DoubleAnimation
            {
                To = targetScale,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase()
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        /// <summary>
        /// 缩放进入动画（从小到大）
        /// </summary>
        public static void ScaleIn(FrameworkElement element, double fromScale = 0.5, double toScale = 1.0,
            int duration = SlowDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            Scale(element, fromScale, toScale, duration, easing ?? CreateBackEase(), delay, onCompleted);
        }

        /// <summary>
        /// 缩放退出动画（从大到小）
        /// </summary>
        public static void ScaleOut(FrameworkElement element, double fromScale = 1.0, double toScale = 0.5,
            int duration = SlowDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            Scale(element, fromScale, toScale, duration, easing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        #endregion

        #region 位移动画

        /// <summary>
        /// 位移动画
        /// </summary>
        public static void Translate(FrameworkElement element, double fromX, double fromY, double toX, double toY,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            EnsureTranslateTransform(element);
            var translateTransform = GetTranslateTransform(element);
            if (translateTransform == null) return;

            var animX = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            var animY = new DoubleAnimation
            {
                From = fromY,
                To = toY,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animX.Completed += (s, e) => onCompleted();
            }

            translateTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, animY);
        }

        /// <summary>
        /// X轴位移动画
        /// </summary>
        public static void TranslateX(FrameworkElement element, double from, double to, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            EnsureTranslateTransform(element);
            var translateTransform = GetTranslateTransform(element);
            if (translateTransform == null) return;

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            translateTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        /// <summary>
        /// Y轴位移动画
        /// </summary>
        public static void TranslateY(FrameworkElement element, double from, double to, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            EnsureTranslateTransform(element);
            var translateTransform = GetTranslateTransform(element);
            if (translateTransform == null) return;

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        /// <summary>
        /// 左滑进入动画（从右向左滑入）
        /// </summary>
        public static void SlideInFromRight(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateX(element, offset, 0, duration, easing ?? CreateExpEase(), delay, onCompleted);
        }

        /// <summary>
        /// 右滑进入动画（从左向右滑入）
        /// </summary>
        public static void SlideInFromLeft(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateX(element, -offset, 0, duration, easing ?? CreateExpEase(), delay, onCompleted);
        }

        /// <summary>
        /// 上滑进入动画（从下向上滑入）
        /// </summary>
        public static void SlideInFromBottom(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateY(element, offset, 0, duration, easing ?? CreateExpEase(), delay, onCompleted);
        }

        /// <summary>
        /// 下滑进入动画（从上向下滑入）
        /// </summary>
        public static void SlideInFromTop(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateY(element, -offset, 0, duration, easing ?? CreateExpEase(), delay, onCompleted);
        }

        /// <summary>
        /// 左滑退出动画（向左滑出）
        /// </summary>
        public static void SlideOutToLeft(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateX(element, 0, -offset, duration, easing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        /// <summary>
        /// 右滑退出动画（向右滑出）
        /// </summary>
        public static void SlideOutToRight(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateX(element, 0, offset, duration, easing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        /// <summary>
        /// 上滑退出动画（向上滑出）
        /// </summary>
        public static void SlideOutToTop(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateY(element, 0, -offset, duration, easing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        /// <summary>
        /// 下滑退出动画（向下滑出）
        /// </summary>
        public static void SlideOutToBottom(FrameworkElement element, double offset = DefaultSlideOffset,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            TranslateY(element, 0, offset, duration, easing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        #endregion

        #region 宽高动画

        /// <summary>
        /// 宽度动画
        /// </summary>
        public static void AnimateWidth(FrameworkElement element, double from, double to, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }

        /// <summary>
        /// 宽度动画（仅指定目标值）
        /// </summary>
        public static void AnimateWidthTo(FrameworkElement element, double targetWidth, int duration = DefaultDuration,
            IEasingFunction? easing = null, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase()
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }

        /// <summary>
        /// 高度动画
        /// </summary>
        public static void AnimateHeight(FrameworkElement element, double from, double to, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// <summary>
        /// MaxHeight 动画
        /// </summary>
        public static void AnimateMaxHeight(FrameworkElement element, double from, double to, int duration = DefaultDuration,
            IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(FrameworkElement.MaxHeightProperty, animation);
        }

        /// <summary>
        /// Grid 列宽动画
        /// </summary>
        public static void AnimateGridLength(ColumnDefinition column, GridLength from, GridLength to,
            int duration = DefaultDuration, IEasingFunction? easing = null, Action? onCompleted = null)
        {
            var animation = new GridLengthAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateQuadraticEase()
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            column.BeginAnimation(ColumnDefinition.WidthProperty, animation);
        }

        /// <summary>
        /// Grid 行高动画
        /// </summary>
        public static void AnimateGridLength(RowDefinition row, GridLength from, GridLength to,
            int duration = DefaultDuration, IEasingFunction? easing = null, Action? onCompleted = null)
        {
            var animation = new GridLengthAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateQuadraticEase()
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            row.BeginAnimation(RowDefinition.HeightProperty, animation);
        }

        #endregion

        #region 组合动画

        /// <summary>
        /// 滑入淡入组合动画（从指定方向滑入并淡入）
        /// </summary>
        public static void SlideAndFadeIn(FrameworkElement element, double slideOffsetX = 0, double slideOffsetY = 0,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            element.Opacity = 0;
            EnsureTranslateTransform(element);
            var translateTransform = GetTranslateTransform(element);
            if (translateTransform != null)
            {
                translateTransform.X = slideOffsetX;
                translateTransform.Y = slideOffsetY;
            }

            var ease = easing ?? CreateExpEase();

            FadeIn(element, duration, 0, 1, CreateSmoothEase(), delay);

            if (slideOffsetX != 0)
                TranslateX(element, slideOffsetX, 0, duration, ease, delay, slideOffsetY == 0 ? onCompleted : null);

            if (slideOffsetY != 0)
                TranslateY(element, slideOffsetY, 0, duration, ease, delay, onCompleted);
        }

        /// <summary>
        /// 滑出淡出组合动画
        /// </summary>
        public static void SlideAndFadeOut(FrameworkElement element, double slideOffsetX = 0, double slideOffsetY = 0,
            int duration = DefaultDuration, IEasingFunction? easing = null, int delay = 0, Action? onCompleted = null)
        {
            var ease = easing ?? CreateExpEase(EasingMode.EaseIn);

            FadeOut(element, duration, 1, 0, CreateSmoothEase(EasingMode.EaseIn), delay);

            if (slideOffsetX != 0)
                TranslateX(element, 0, slideOffsetX, duration, ease, delay, slideOffsetY == 0 ? onCompleted : null);

            if (slideOffsetY != 0)
                TranslateY(element, 0, slideOffsetY, duration, ease, delay, onCompleted);
        }

        /// <summary>
        /// 缩放淡入组合动画
        /// </summary>
        public static void ScaleAndFadeIn(FrameworkElement element, double fromScale = 0.5, double toScale = 1.0,
            int duration = SlowDuration, IEasingFunction? scaleEasing = null, IEasingFunction? fadeEasing = null,
            int delay = 0, Action? onCompleted = null)
        {
            element.Opacity = 0;
            FadeIn(element, duration, 0, 1, fadeEasing ?? CreateSmoothEase(), delay);
            ScaleIn(element, fromScale, toScale, duration, scaleEasing ?? CreateBackEase(), delay, onCompleted);
        }

        /// <summary>
        /// 缩放淡出组合动画
        /// </summary>
        public static void ScaleAndFadeOut(FrameworkElement element, double fromScale = 1.0, double toScale = 0.5,
            int duration = SlowDuration, IEasingFunction? scaleEasing = null, IEasingFunction? fadeEasing = null,
            int delay = 0, Action? onCompleted = null)
        {
            FadeOut(element, duration, 1, 0, fadeEasing ?? CreateSmoothEase(EasingMode.EaseIn), delay);
            ScaleOut(element, fromScale, toScale, duration, scaleEasing ?? CreateExpEase(EasingMode.EaseIn), delay, onCompleted);
        }

        /// <summary>
        /// 页面切换进入动画
        /// </summary>
        public static void PageIn(UIElement page, int duration = DefaultDuration, Action? onCompleted = null)
        {
            page.Opacity = 0;
            page.Visibility = Visibility.Visible;
            FadeIn(page, duration, 0, 1, CreateQuadraticEase(), 0, onCompleted);
        }

        /// <summary>
        /// 页面切换退出动画
        /// </summary>
        public static void PageOut(UIElement page, int duration = FastDuration, Action? onCompleted = null)
        {
            FadeOut(page, duration, 1, 0, CreateQuadraticEase(EasingMode.EaseIn), 0, () =>
            {
                page.Visibility = Visibility.Collapsed;
                onCompleted?.Invoke();
            });
        }

        #endregion

        #region Storyboard 辅助方法

        /// <summary>
        /// 创建 Storyboard 并添加透明度动画
        /// </summary>
        public static void AddOpacityAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加缩放X动画到 Storyboard
        /// </summary>
        public static void AddScaleXAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0,
            string transformPath = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)")
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(transformPath));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加缩放Y动画到 Storyboard
        /// </summary>
        public static void AddScaleYAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0,
            string transformPath = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)")
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(transformPath));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加位移X动画到 Storyboard
        /// </summary>
        public static void AddTranslateXAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0,
            string transformPath = "(UIElement.RenderTransform).(TranslateTransform.X)")
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(transformPath));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加位移Y动画到 Storyboard
        /// </summary>
        public static void AddTranslateYAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0,
            string transformPath = "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)")
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(transformPath));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加宽度动画到 Storyboard
        /// </summary>
        public static void AddWidthAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.WidthProperty));
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// 添加 MaxHeight 动画到 Storyboard
        /// </summary>
        public static void AddMaxHeightAnimation(Storyboard storyboard, DependencyObject target,
            double from, double to, int duration, IEasingFunction? easing = null, int delay = 0)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MaxHeightProperty));
            storyboard.Children.Add(animation);
        }

        #endregion

        #region 清除动画

        /// <summary>
        /// 清除元素上的所有动画
        /// </summary>
        public static void ClearAnimations(FrameworkElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.BeginAnimation(FrameworkElement.WidthProperty, null);
            element.BeginAnimation(FrameworkElement.HeightProperty, null);
            element.BeginAnimation(FrameworkElement.MaxHeightProperty, null);

            var scaleTransform = GetScaleTransform(element);
            if (scaleTransform != null)
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

            var translateTransform = GetTranslateTransform(element);
            if (translateTransform != null)
            {
                translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
        }

        #endregion

        #region Transform 辅助方法

        /// <summary>
        /// 确保元素有 ScaleTransform
        /// </summary>
        public static void EnsureScaleTransform(FrameworkElement element)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            if (element.RenderTransform is ScaleTransform)
                return;

            if (element.RenderTransform is TransformGroup group)
            {
                if (!group.Children.OfType<ScaleTransform>().Any())
                {
                    group.Children.Add(new ScaleTransform(1, 1));
                }
                return;
            }

            element.RenderTransform = new ScaleTransform(1, 1);
        }

        /// <summary>
        /// 确保元素有 TranslateTransform
        /// </summary>
        public static void EnsureTranslateTransform(FrameworkElement element)
        {
            if (element.RenderTransform is TranslateTransform)
                return;

            if (element.RenderTransform is TransformGroup group)
            {
                if (!group.Children.OfType<TranslateTransform>().Any())
                {
                    group.Children.Add(new TranslateTransform());
                }
                return;
            }

            element.RenderTransform = new TranslateTransform();
        }

        /// <summary>
        /// 确保元素有完整的 TransformGroup（包含 TranslateTransform, ScaleTransform, RotateTransform）
        /// </summary>
        public static void EnsureTransformGroup(FrameworkElement element)
        {
            if (element.RenderTransform is not TransformGroup)
            {
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new TranslateTransform());
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new RotateTransform());
                element.RenderTransform = transformGroup;
            }
        }

        /// <summary>
        /// 获取元素的 ScaleTransform
        /// </summary>
        public static ScaleTransform? GetScaleTransform(FrameworkElement element)
        {
            if (element.RenderTransform is ScaleTransform scale)
                return scale;

            if (element.RenderTransform is TransformGroup group)
                return group.Children.OfType<ScaleTransform>().FirstOrDefault();

            return null;
        }

        /// <summary>
        /// 获取元素的 TranslateTransform
        /// </summary>
        public static TranslateTransform? GetTranslateTransform(FrameworkElement element)
        {
            if (element.RenderTransform is TranslateTransform translate)
                return translate;

            if (element.RenderTransform is TransformGroup group)
                return group.Children.OfType<TranslateTransform>().FirstOrDefault();

            return null;
        }

        #endregion

        #region AnimationConfig 兼容方法

        /// <summary>
        /// 应用动画到元素（兼容旧版 AnimationConfig）
        /// </summary>
        public static void ApplyAnimation(FrameworkElement element, AnimationConfig config)
        {
            if (config.Types == AnimationType.None || element == null)
                return;

            var storyboard = new Storyboard();

            // FadeIn 动画
            if ((config.Types & AnimationType.FadeIn) != 0)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(fadeIn, element);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeIn);
            }

            // FadeOut 动画
            if ((config.Types & AnimationType.FadeOut) != 0)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(fadeOut, element);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeOut);
            }

            // SlideLeft 动画
            if ((config.Types & AnimationType.SlideLeft) != 0)
            {
                EnsureTransformGroup(element);
                var slideLeft = new DoubleAnimation
                {
                    From = element.ActualWidth > 0 ? element.ActualWidth : 100,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(slideLeft, element);
                Storyboard.SetTargetProperty(slideLeft, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));
                storyboard.Children.Add(slideLeft);
            }

            // SlideRight 动画
            if ((config.Types & AnimationType.SlideRight) != 0)
            {
                EnsureTransformGroup(element);
                var slideRight = new DoubleAnimation
                {
                    From = element.ActualWidth > 0 ? -element.ActualWidth : -100,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(slideRight, element);
                Storyboard.SetTargetProperty(slideRight, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));
                storyboard.Children.Add(slideRight);
            }

            // SlideUp 动画
            if ((config.Types & AnimationType.SlideUp) != 0)
            {
                EnsureTransformGroup(element);
                var slideUp = new DoubleAnimation
                {
                    From = element.ActualHeight > 0 ? element.ActualHeight : 50,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(slideUp, element);
                Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
                storyboard.Children.Add(slideUp);
            }

            // SlideDown 动画
            if ((config.Types & AnimationType.SlideDown) != 0)
            {
                EnsureTransformGroup(element);
                var slideDown = new DoubleAnimation
                {
                    From = element.ActualHeight > 0 ? -element.ActualHeight : -50,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(slideDown, element);
                Storyboard.SetTargetProperty(slideDown, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
                storyboard.Children.Add(slideDown);
            }

            // ScaleIn 动画
            if ((config.Types & AnimationType.ScaleIn) != 0)
            {
                EnsureTransformGroup(element);
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleX = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(scaleX, element);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(scaleY, element);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));
                storyboard.Children.Add(scaleY);
            }

            // ScaleOut 动画
            if ((config.Types & AnimationType.ScaleOut) != 0)
            {
                EnsureTransformGroup(element);
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleX = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(scaleX, element);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(scaleY, element);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));
                storyboard.Children.Add(scaleY);
            }

            // Rotate 动画
            if ((config.Types & AnimationType.Rotate) != 0)
            {
                EnsureTransformGroup(element);
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                var rotate = new DoubleAnimation
                {
                    From = -180,
                    To = 0,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                Storyboard.SetTarget(rotate, element);
                Storyboard.SetTargetProperty(rotate, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"));
                storyboard.Children.Add(rotate);
            }

            // Bounce 动画
            if ((config.Types & AnimationType.Bounce) != 0)
            {
                EnsureTransformGroup(element);

                var bounce = new DoubleAnimationUsingKeyFrames();
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-30, KeyTime.FromPercent(0.2)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.4)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromPercent(0.6)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.8)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromPercent(0.9)));
                bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
                bounce.Duration = config.Duration;

                Storyboard.SetTarget(bounce, element);
                Storyboard.SetTargetProperty(bounce, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
                storyboard.Children.Add(bounce);
            }

            storyboard.Begin();
        }

        #endregion

        #region DoubleAnimation 工厂方法

        /// <summary>
        /// 创建淡入动画
        /// </summary>
        public static DoubleAnimation CreateFadeIn(TimeSpan duration, IEasingFunction? easing = null)
        {
            return new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = easing ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
        }

        /// <summary>
        /// 创建淡出动画
        /// </summary>
        public static DoubleAnimation CreateFadeOut(TimeSpan duration, IEasingFunction? easing = null)
        {
            return new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = easing ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
        }

        /// <summary>
        /// 创建 DoubleAnimation
        /// </summary>
        public static DoubleAnimation CreateDoubleAnimation(double from, double to, int duration,
            IEasingFunction? easing = null, int delay = 0)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase(),
                BeginTime = delay > 0 ? TimeSpan.FromMilliseconds(delay) : TimeSpan.Zero
            };
        }

        /// <summary>
        /// 创建 DoubleAnimation（仅指定目标值）
        /// </summary>
        public static DoubleAnimation CreateDoubleAnimationTo(double to, int duration, IEasingFunction? easing = null)
        {
            return new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing ?? CreateSmoothEase()
            };
        }

        #endregion
    }

    /// <summary>
    /// GridLength 动画类
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        public IEasingFunction? EasingFunction
        {
            get => (IEasingFunction?)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }

        public override Type TargetPropertyType => typeof(GridLength);

        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromValue = From.Value;
            double toValue = To.Value;
            double progress = animationClock.CurrentProgress ?? 0;

            // 应用缓动函数
            if (EasingFunction != null)
            {
                progress = EasingFunction.Ease(progress);
            }

            double currentValue = fromValue + (toValue - fromValue) * progress;

            return new GridLength(currentValue, GridUnitType.Pixel);
        }
    }
}