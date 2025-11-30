using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

namespace Phobos.Utils.Media
{
    /// <summary>
    /// 动画工具类
    /// </summary>
    public static class PUAnimation
    {
        /// <summary>
        /// 应用动画到元素
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

        /// <summary>
        /// 确保元素有 TransformGroup
        /// </summary>
        private static void EnsureTransformGroup(FrameworkElement element)
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
    }
}