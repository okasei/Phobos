using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Notifier
{
    /// <summary>
    /// 灵动岛样式通知窗口
    /// 实现类似 iOS Dynamic Island 的弹性动画效果
    /// </summary>
    public partial class PCONotifierWindow : Window
    {
        private IPhobosNotification? _notification;
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private bool _isExiting = false;
        private CancellationTokenSource? _autoCloseTokenSource;
        private readonly Action<string>? _onClosed;
        private readonly Action? _onNextNotification;

        // 尺寸常量 - 最小化状态
        private const double MinWidth_Collapsed = 200;
        private const double MinHeight_Collapsed = 28;  // 最小化状态的内容高度

        // 尺寸常量 - 展开状态（基础值，会根据内容动态调整）
        private const double MinWidth_Expanded_Base = 340;
        private const double MaxWidth_ScreenRatio = 0.6;  // 最大宽度为屏幕宽度的60%
        private const double MaxHeight_PlainText = 200;   // 纯文本最大高度
        private const int MaxLines_PlainText = 6;         // 纯文本最大行数

        // 当前通知的动态展开尺寸
        private double _currentExpandedWidth = MinWidth_Expanded_Base;
        private double _currentMaxContentHeight = MaxHeight_PlainText;
        private bool _isContentSizeLimited = true;  // 是否限制内容尺寸

        // 窗口固定位置（初始化后不再改变）
        private double _fixedLeft;
        private double _fixedTop;
        private bool _isPositioned = false;

        // 默认图标
        private static readonly ImageSource? DefaultInfoIcon;
        private static readonly ImageSource? DefaultWarningIcon;
        private static readonly ImageSource? DefaultErrorIcon;

        static PCONotifierWindow()
        {
            // 可以在这里加载默认图标
        }

        public PCONotifierWindow(Action<string>? onClosed = null, Action? onNextNotification = null)
        {
            InitializeComponent();
            _onClosed = onClosed;
            _onNextNotification = onNextNotification;

            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 定位到屏幕顶部居中（只在首次加载时计算）
            if (!_isPositioned)
            {
                PositionWindowFixed();
            }
        }

        /// <summary>
        /// 计算并设置固定的窗口位置（基于窗口固定宽度居中）
        /// </summary>
        private void PositionWindowFixed()
        {
            var screen = SystemParameters.WorkArea;
            // 窗口宽度固定为 420，直接用于计算居中位置
            _fixedLeft = (screen.Width - 420) / 2 + screen.Left;
            _fixedTop = screen.Top + 10;

            Left = _fixedLeft;
            Top = _fixedTop;
            _isPositioned = true;
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        public void ShowNotification(IPhobosNotification notification)
        {
            _notification = notification;

            // 设置图标
            SetIcon(notification);

            // 设置标题
            MinTitle.Text = notification.Title ?? "通知";
            ExpandTitle.Text = notification.Title ?? "通知";

            // 设置提示图片
            SetImage(notification);

            // 设置内容
            SetContent(notification);

            // 设置 Hero 图片
            SetHeroImage(notification);

            // 设置操作按钮
            SetActions(notification);

            // 显示窗口
            Show();

            // 播放入场动画
            PlayEnterAnimation();

            // 启动自动关闭计时器
            StartAutoCloseTimer(notification.Duration);
        }

        private void SetIcon(IPhobosNotification notification)
        {
            ImageSource? icon = notification.Icon;

            if (icon == null && !string.IsNullOrEmpty(notification.IconPath))
            {
                try
                {
                    icon = new BitmapImage(new Uri(notification.IconPath, UriKind.RelativeOrAbsolute));
                }
                catch { }
            }

            // 如果还是没有图标，使用默认图标
            icon ??= DefaultInfoIcon;

            MinIcon.Source = icon;
            ExpandIcon.Source = icon;
        }

        private void SetImage(IPhobosNotification notification)
        {
            ImageSource? image = notification.Image;

            if (image == null && !string.IsNullOrEmpty(notification.ImagePath))
            {
                try
                {
                    image = new BitmapImage(new Uri(notification.ImagePath, UriKind.RelativeOrAbsolute));
                }
                catch { }
            }

            if (image != null)
            {
                MinImage.Source = image;
                MinImage.Visibility = Visibility.Visible;
            }
            else
            {
                MinImage.Visibility = Visibility.Collapsed;
            }
        }

        private void SetContent(IPhobosNotification notification)
        {
            // 计算动态尺寸
            CalculateDynamicSize(notification);

            // 获取主题画刷
            var foregroundBrush = (Brush)FindResource("Foreground1Brush");
            var secondaryForegroundBrush = (Brush)FindResource("Foreground2Brush");

            switch (notification.ContentType)
            {
                case NotificationContentType.PlainText:
                    var textBlock = new TextBlock
                    {
                        Text = notification.Content?.ToString() ?? string.Empty,
                        Foreground = secondaryForegroundBrush,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.9,
                        MaxHeight = _currentMaxContentHeight,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    ContentPresenter.Content = textBlock;
                    break;

                case NotificationContentType.Html:
                    // HTML 内容不限制尺寸
                    var htmlTextBlock = new TextBlock
                    {
                        Text = notification.Content?.ToString() ?? string.Empty,
                        Foreground = secondaryForegroundBrush,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.9
                    };
                    ContentPresenter.Content = htmlTextBlock;
                    break;

                case NotificationContentType.UserControl:
                    // UserControl 不限制尺寸
                    if (notification.Content is UIElement element)
                    {
                        ContentPresenter.Content = element;
                    }
                    break;
            }

            // 更新内容区域的最大高度
            if (_isContentSizeLimited)
            {
                ContentArea.MaxHeight = _currentMaxContentHeight;
            }
            else
            {
                ContentArea.ClearValue(MaxHeightProperty);
            }
        }

        /// <summary>
        /// 根据通知内容计算动态尺寸
        /// </summary>
        private void CalculateDynamicSize(IPhobosNotification notification)
        {
            var screen = SystemParameters.WorkArea;
            double maxScreenWidth = screen.Width * MaxWidth_ScreenRatio;

            switch (notification.ContentType)
            {
                case NotificationContentType.PlainText:
                    _isContentSizeLimited = true;
                    string text = notification.Content?.ToString() ?? string.Empty;

                    // 根据文本长度估算需要的宽度
                    int textLength = text.Length;
                    if (textLength <= 50)
                    {
                        // 短文本，使用基础宽度
                        _currentExpandedWidth = MinWidth_Expanded_Base;
                    }
                    else if (textLength <= 150)
                    {
                        // 中等文本，适当增加宽度
                        _currentExpandedWidth = Math.Min(420, maxScreenWidth);
                    }
                    else
                    {
                        // 长文本，使用更大宽度
                        _currentExpandedWidth = Math.Min(500, maxScreenWidth);
                    }

                    // 限制内容高度
                    _currentMaxContentHeight = MaxHeight_PlainText;
                    break;

                case NotificationContentType.Html:
                case NotificationContentType.UserControl:
                    // HTML 和 UserControl 不限制尺寸，但宽度仍有上限
                    _isContentSizeLimited = false;
                    _currentExpandedWidth = Math.Min(500, maxScreenWidth);
                    _currentMaxContentHeight = screen.Height * 0.5;  // 最大高度为屏幕高度的50%
                    break;
            }

            // 确保展开宽度不小于基础值
            _currentExpandedWidth = Math.Max(_currentExpandedWidth, MinWidth_Expanded_Base);
        }

        private void SetHeroImage(IPhobosNotification notification)
        {
            ImageSource? heroImage = notification.HeroImage;

            if (heroImage == null && !string.IsNullOrEmpty(notification.HeroImagePath))
            {
                try
                {
                    heroImage = new BitmapImage(new Uri(notification.HeroImagePath, UriKind.RelativeOrAbsolute));
                }
                catch { }
            }

            if (heroImage != null)
            {
                HeroImage.Source = heroImage;
                HeroImageBorder.Visibility = Visibility.Visible;
            }
            else
            {
                HeroImageBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void SetActions(IPhobosNotification notification)
        {
            ActionsPanel.Items.Clear();

            if (notification.Actions == null || notification.Actions.Count == 0)
            {
                ActionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ActionsPanel.Visibility = Visibility.Visible;

            foreach (var action in notification.Actions)
            {
                var button = new Button
                {
                    Content = action.Text,
                    Margin = new Thickness(4, 0, 0, 0),
                    Style = action.Category == NotificationActionCategory.Primary
                        ? (Style)FindResource("NotifierButtonPrimary")
                        : (Style)FindResource("NotifierButton")
                };

                var localAction = action;
                button.Click += (s, e) =>
                {
                    e.Handled = true;
                    localAction.OnClick?.Invoke();
                    if (localAction.CloseOnClick)
                    {
                        CloseNotification();
                    }
                };

                ActionsPanel.Items.Add(button);
            }
        }

        private void StartAutoCloseTimer(int duration)
        {
            if (duration <= 0) return;

            _autoCloseTokenSource?.Cancel();
            _autoCloseTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(duration, _autoCloseTokenSource.Token);
                    await Dispatcher.InvokeAsync(CloseNotification);
                }
                catch (TaskCanceledException)
                {
                    // 计时器被取消
                }
            });
        }

        private void PauseAutoCloseTimer()
        {
            _autoCloseTokenSource?.Cancel();
        }

        private void ResumeAutoCloseTimer()
        {
            if (_notification != null && _notification.Duration > 0)
            {
                // 重新开始计时（使用剩余时间或固定时间）
                StartAutoCloseTimer(2000); // 鼠标移出后 2 秒关闭
            }
        }

        #region 动画

        /// <summary>
        /// 创建优雅的缓动函数 - 使用 CubicEase 代替过度弹性的 ElasticEase
        /// </summary>
        private static CubicEase CreateSmoothEase(EasingMode mode = EasingMode.EaseOut)
        {
            return new CubicEase { EasingMode = mode };
        }

        /// <summary>
        /// 创建轻微回弹缓动函数
        /// </summary>
        private static BackEase CreateSubtleBackEase(EasingMode mode = EasingMode.EaseOut)
        {
            return new BackEase
            {
                Amplitude = 0.2,  // 降低回弹幅度，更优雅
                EasingMode = mode
            };
        }

        /// <summary>
        /// 入场动画 - 从屏幕上方优雅滑入
        /// </summary>
        private void PlayEnterAnimation()
        {
            // 初始状态
            BorderScale.ScaleX = 0.85;
            BorderScale.ScaleY = 0.85;
            BorderTranslate.Y = -50;
            MainBorder.Opacity = 0;
            MainBorder.MinWidth = MinWidth_Collapsed * 0.8;

            var storyboard = new Storyboard();

            // 滑入动画
            var translateY = new DoubleAnimation(-50, 0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(translateY, MainBorder);
            Storyboard.SetTargetProperty(translateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(translateY);

            // 缩放动画
            var scaleX = new DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(scaleX, MainBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(scaleY, MainBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            // 淡入
            var opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            Storyboard.SetTarget(opacity, MainBorder);
            Storyboard.SetTargetProperty(opacity, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(opacity);

            // 宽度展开
            var widthAnim = new DoubleAnimation(MinWidth_Collapsed * 0.8, MinWidth_Collapsed, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = CreateSubtleBackEase()
            };
            Storyboard.SetTarget(widthAnim, MainBorder);
            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(MinWidthProperty));
            storyboard.Children.Add(widthAnim);

            storyboard.Begin();
        }

        /// <summary>
        /// 退场动画 - 优雅收缩后滑出
        /// </summary>
        private void PlayExitAnimation(Action onComplete)
        {
            if (_isExiting) return;
            _isExiting = true;

            if (_isExpanded)
            {
                CollapseNotificationForExit(() => PlayExitAnimationInternal(onComplete));
            }
            else
            {
                PlayExitAnimationInternal(onComplete);
            }
        }

        private void PlayExitAnimationInternal(Action onComplete)
        {
            var storyboard = new Storyboard();

            // 先收缩宽度 (与入场的宽度展开对称)
            var widthShrink = new DoubleAnimation(MinWidth_Collapsed, MinWidth_Collapsed * 0.8, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = CreateSmoothEase(EasingMode.EaseIn)
            };
            Storyboard.SetTarget(widthShrink, MainBorder);
            Storyboard.SetTargetProperty(widthShrink, new PropertyPath(MinWidthProperty));
            storyboard.Children.Add(widthShrink);

            // 淡出 (与入场的淡入对称)
            var opacity = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250))
            {
                BeginTime = TimeSpan.FromMilliseconds(100)
            };
            Storyboard.SetTarget(opacity, MainBorder);
            Storyboard.SetTargetProperty(opacity, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(opacity);

            // 缩放 (与入场的缩放对称: 1.0 → 0.85)
            var scaleX = new DoubleAnimation(1.0, 0.85, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase(EasingMode.EaseIn)
            };
            Storyboard.SetTarget(scaleX, MainBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation(1.0, 0.85, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase(EasingMode.EaseIn)
            };
            Storyboard.SetTarget(scaleY, MainBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            // 向上滑出 (与入场的滑入对称: 0 → -50)
            var translateY = new DoubleAnimation(0, -50, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = CreateSmoothEase(EasingMode.EaseIn)
            };
            Storyboard.SetTarget(translateY, MainBorder);
            Storyboard.SetTargetProperty(translateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(translateY);

            storyboard.Completed += (s, e) =>
            {
                _isExiting = false;
                onComplete();
            };
            storyboard.Begin();
        }

        /// <summary>
        /// 展开通知 - hover时优雅展开
        /// </summary>
        private void ExpandNotification()
        {
            if (_isExpanded || _isAnimating || _isExiting) return;
            _isAnimating = true;

            PauseAutoCloseTimer();

            // 记录当前最小化内容的高度
            double currentHeight = MinimizedContent.ActualHeight;

            // 先显示展开内容以测量其高度
            ExpandedContent.Visibility = Visibility.Visible;
            ExpandedContent.Opacity = 0;
            ExpandedContent.UpdateLayout();
            double targetHeight = ExpandedContent.ActualHeight;

            // 隐藏最小化内容
            MinimizedContent.Visibility = Visibility.Collapsed;

            // 设置初始 MaxHeight 为当前高度，然后动画到目标高度
            ContentGrid.MaxHeight = currentHeight;

            var storyboard = new Storyboard();

            // 宽度展开（使用动态计算的宽度）
            var widthExpand = new DoubleAnimation(MinWidth_Collapsed, _currentExpandedWidth, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = CreateSubtleBackEase()
            };
            Storyboard.SetTarget(widthExpand, MainBorder);
            Storyboard.SetTargetProperty(widthExpand, new PropertyPath(MinWidthProperty));
            storyboard.Children.Add(widthExpand);

            // 高度展开
            var heightExpand = new DoubleAnimation(currentHeight, targetHeight, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = CreateSubtleBackEase()
            };
            Storyboard.SetTarget(heightExpand, ContentGrid);
            Storyboard.SetTargetProperty(heightExpand, new PropertyPath(MaxHeightProperty));
            storyboard.Children.Add(heightExpand);

            // 内容淡入
            var contentFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                BeginTime = TimeSpan.FromMilliseconds(80)
            };
            Storyboard.SetTarget(contentFadeIn, ExpandedContent);
            Storyboard.SetTargetProperty(contentFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeIn);

            storyboard.Completed += (s, e) =>
            {
                // 移除 MaxHeight 限制，让内容自然布局
                ContentGrid.ClearValue(MaxHeightProperty);
                _isExpanded = true;
                _isAnimating = false;
            };
            storyboard.Begin();
        }

        /// <summary>
        /// 收缩通知 - hover离开时优雅收缩
        /// 内容淡出 + 高度收缩 + 宽度收缩同时进行，然后淡入最小化内容
        /// </summary>
        private void CollapseNotification()
        {
            if (!_isExpanded || _isAnimating || _isExiting) return;
            _isAnimating = true;

            // 记录当前展开内容的高度
            double currentHeight = ExpandedContent.ActualHeight;

            var storyboard = new Storyboard();

            // 展开内容淡出
            var contentFadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            Storyboard.SetTarget(contentFadeOut, ExpandedContent);
            Storyboard.SetTargetProperty(contentFadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeOut);

            // 宽度收缩（从动态宽度收缩回最小化宽度）
            var widthCollapse = new DoubleAnimation(_currentExpandedWidth, MinWidth_Collapsed, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(widthCollapse, MainBorder);
            Storyboard.SetTargetProperty(widthCollapse, new PropertyPath(MinWidthProperty));
            storyboard.Children.Add(widthCollapse);

            // 高度收缩 - 使用 MaxHeight 动画
            var heightCollapse = new DoubleAnimation(currentHeight, MinHeight_Collapsed, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(heightCollapse, ContentGrid);
            Storyboard.SetTargetProperty(heightCollapse, new PropertyPath(MaxHeightProperty));
            storyboard.Children.Add(heightCollapse);

            storyboard.Completed += (s, e) =>
            {
                // 切换内容
                ExpandedContent.Visibility = Visibility.Collapsed;
                ExpandedContent.Opacity = 1;
                MinimizedContent.Visibility = Visibility.Visible;
                MinimizedContent.Opacity = 0;

                // 移除 MaxHeight 限制
                ContentGrid.ClearValue(MaxHeightProperty);

                // 淡入最小化内容
                var fadeInStoryboard = new Storyboard();
                var minFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                Storyboard.SetTarget(minFadeIn, MinimizedContent);
                Storyboard.SetTargetProperty(minFadeIn, new PropertyPath(OpacityProperty));
                fadeInStoryboard.Children.Add(minFadeIn);

                fadeInStoryboard.Completed += (s2, e2) =>
                {
                    _isExpanded = false;
                    _isAnimating = false;
                    ResumeAutoCloseTimer();
                };
                fadeInStoryboard.Begin();
            };
            storyboard.Begin();
        }

        /// <summary>
        /// 为退出准备的收缩动画
        /// </summary>
        private void CollapseNotificationForExit(Action onComplete)
        {
            if (!_isExpanded)
            {
                onComplete();
                return;
            }

            _isAnimating = true;

            // 记录当前展开内容的高度
            double currentHeight = ExpandedContent.ActualHeight;

            var storyboard = new Storyboard();

            // 展开内容淡出
            var contentFadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
            Storyboard.SetTarget(contentFadeOut, ExpandedContent);
            Storyboard.SetTargetProperty(contentFadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeOut);

            // 宽度收缩（从动态宽度收缩回最小化宽度）
            var widthCollapse = new DoubleAnimation(_currentExpandedWidth, MinWidth_Collapsed, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(widthCollapse, MainBorder);
            Storyboard.SetTargetProperty(widthCollapse, new PropertyPath(MinWidthProperty));
            storyboard.Children.Add(widthCollapse);

            // 高度收缩
            var heightCollapse = new DoubleAnimation(currentHeight, MinHeight_Collapsed, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = CreateSmoothEase()
            };
            Storyboard.SetTarget(heightCollapse, ContentGrid);
            Storyboard.SetTargetProperty(heightCollapse, new PropertyPath(MaxHeightProperty));
            storyboard.Children.Add(heightCollapse);

            storyboard.Completed += (s, e) =>
            {
                // 切换内容
                ExpandedContent.Visibility = Visibility.Collapsed;
                ExpandedContent.Opacity = 1;
                MinimizedContent.Visibility = Visibility.Visible;
                MinimizedContent.Opacity = 0;

                // 移除 MaxHeight 限制
                ContentGrid.ClearValue(MaxHeightProperty);

                var fadeInStoryboard = new Storyboard();
                var minFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                Storyboard.SetTarget(minFadeIn, MinimizedContent);
                Storyboard.SetTargetProperty(minFadeIn, new PropertyPath(OpacityProperty));
                fadeInStoryboard.Children.Add(minFadeIn);

                fadeInStoryboard.Completed += (s2, e2) =>
                {
                    _isExpanded = false;
                    _isAnimating = false;
                    onComplete();
                };
                fadeInStoryboard.Begin();
            };
            storyboard.Begin();
        }

        #endregion

        #region 事件处理

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            ExpandNotification();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            CollapseNotification();
        }

        private void OnContentClick(object sender, MouseButtonEventArgs e)
        {
            _notification?.ContentAction?.Invoke();
        }

        private void CloseNotification()
        {
            _autoCloseTokenSource?.Cancel();

            PlayExitAnimation(() =>
            {
                var notificationId = _notification?.Id ?? string.Empty;
                _onClosed?.Invoke(notificationId);

                // 检查是否有待处理的通知
                _onNextNotification?.Invoke();

                Hide();
            });
        }

        #endregion
    }
}
