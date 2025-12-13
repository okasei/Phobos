using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Interface;
using Phobos.Utils.Media;
using System;
using System.Collections.Generic;
using System.Media;
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
    ///
    /// 动画状态:
    /// - 最小状态: 默认显示状态，只显示标题一行
    /// - 默认状态: Hover后展开的状态，显示完整内容
    ///
    /// 动画流程:
    /// - 最小状态出现: 从屏幕顶向下延展并飞出，布局左滑淡入
    /// - 最小状态->默认状态: 布局右滑淡出, 窗口动态展开, 新布局左滑淡入
    /// - 默认状态->最小状态: 布局右滑淡出, 窗口动态收缩, 新布局左滑淡入
    /// - 最小状态离开: 飞向屏幕顶且收缩
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

        // 声音播放相关
        private SoundPlayer? _soundPlayer;
        private CancellationTokenSource? _alarmLoopTokenSource;
        private bool _isAlarmPlaying = false;

        #region 尺寸常量

        // 最小状态尺寸
        private const double MinState_BaseHeight = 28;     // 最小状态的基础高度
        private const double MaxWidth_ScreenRatio = 0.3;   // 最小状态最大宽度为屏幕宽度的30%

        // 默认（展开）状态尺寸
        private const double DefaultState_MinWidth = 380;  // 展开状态最小宽度
        private const double DefaultState_MaxLines = 3;    // 纯文本最多显示3行（含自动换行）
        private const double LineHeight = 20;              // 行高

        // 动画时长常量 (毫秒)
        private const int AnimDuration_SlideContent = 200;   // 内容滑动时长
        private const int AnimDuration_WindowResize = 280;   // 窗口尺寸变化时长
        private const int AnimDuration_Enter = 350;          // 入场动画时长
        private const int AnimDuration_Exit = 300;           // 退场动画时长

        // 动画位移常量
        private const double SlideOffset = 30;               // 内容滑动位移

        #endregion

        #region 动态计算的尺寸

        // 最小状态尺寸（根据标题计算）
        private double _minStateWidth = 200;
        private double _minStateHeight = MinState_BaseHeight;

        // 默认（展开）状态尺寸
        private double _defaultStateWidth = DefaultState_MinWidth;
        private double _defaultStateHeight = 200;

        #endregion

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
            // 窗口宽度固定为 580，直接用于计算居中位置
            _fixedLeft = (screen.Width - 580) / 2 + screen.Left;
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
            PCLoggerPlugin.Debug("Notifier", "ShowNotification started");

            _notification = notification;

            // 设置关闭回调，供 HTML/UserControl 提供者调用
            notification.CloseAction = () => CloseNotification();

            // 设置图标
            SetIcon(notification);

            // 设置标题
            MinTitle.Text = notification.Title ?? "通知";
            ExpandTitle.Text = notification.Title ?? "通知";

            // 设置提示图片
            SetImage(notification);

            // 计算动态尺寸（必须在设置内容之前）
            PCLoggerPlugin.Debug("Notifier", "Calculating dynamic size...");
            CalculateDynamicSize(notification);
            PCLoggerPlugin.Debug("Notifier", $"MinState: {_minStateWidth}x{_minStateHeight}, DefaultState: {_defaultStateWidth}x{_defaultStateHeight}");

            // 设置内容
            SetContent(notification);

            // 设置 Hero 图片
            SetHeroImage(notification);

            // 设置操作按钮
            SetActions(notification);

            // 显示窗口
            PCLoggerPlugin.Debug("Notifier", "Calling Show()...");
            Show();
            PCLoggerPlugin.Debug("Notifier", $"Window shown. IsVisible={IsVisible}, Left={Left}, Top={Top}");

            // 播放入场动画
            PCLoggerPlugin.Debug("Notifier", "Playing enter animation...");
            PlayEnterAnimation();

            // 播放声音
            PlayNotificationSound(notification);

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

        /// <summary>
        /// 根据通知内容计算动态尺寸
        /// </summary>
        private void CalculateDynamicSize(IPhobosNotification notification)
        {
            try
            {
                var screen = SystemParameters.WorkArea;
                double maxMinStateWidth = screen.Width * MaxWidth_ScreenRatio;

                // 直接从控件获取字体
                var typeface = new Typeface(
                    MinTitle.FontFamily,
                    MinTitle.FontStyle,
                    MinTitle.FontWeight,
                    MinTitle.FontStretch);

                #region 计算最小状态尺寸

                // 最小状态只显示标题一行
                string title = notification.Title ?? "通知";
                var titleMeasure = MeasureText(title, typeface, MinTitle.FontSize, maxMinStateWidth - 80);

                // 最小状态宽度 = 图标(28) + 间距(8) + 标题宽度 + 间距(8) + 右侧图片(如果有,28) + 边距(28)
                double minContentWidth = 28 + 8 + titleMeasure.width + 28;
                if (notification.Image != null || !string.IsNullOrEmpty(notification.ImagePath))
                {
                    minContentWidth += 8 + 28;
                }

                _minStateWidth = Math.Min(Math.Max(minContentWidth, 160), maxMinStateWidth);
                _minStateHeight = MinState_BaseHeight;

                #endregion

                #region 计算默认（展开）状态尺寸

                switch (notification.ContentType)
                {
                    case NotificationContentType.PlainText:
                        CalculatePlainTextSize(notification, screen);
                        break;

                    case NotificationContentType.Html:
                    case NotificationContentType.UserControl:
                        CalculateCustomContentSize(notification, screen);
                        break;
                }

                #endregion
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Notifier", $"CalculateDynamicSize failed: {ex.Message}");
                // 使用默认值
                _minStateWidth = 200;
                _minStateHeight = MinState_BaseHeight;
                _defaultStateWidth = DefaultState_MinWidth;
                _defaultStateHeight = 150;
            }
        }

        /// <summary>
        /// 计算纯文本内容的展开尺寸
        /// 最多显示3行（含自动换行），超出部分用省略号
        /// </summary>
        private void CalculatePlainTextSize(IPhobosNotification notification, Rect screen)
        {
            string content = notification.Content?.ToString() ?? string.Empty;
            double maxContentWidth = screen.Width * 0.5;  // 展开状态最大宽度为屏幕50%

            var typeface = new Typeface(
                new FontFamily((string)FindResource("FontPrimary") ?? "Segoe UI"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            // 先用默认宽度测量，确定需要多宽
            double testWidth = DefaultState_MinWidth - 60; // 减去边距
            var (measuredWidth, measuredHeight, lineCount) = MeasureTextWithLineCount(content, typeface, 14, testWidth, LineHeight);

            // 如果行数过多，尝试增加宽度
            if (lineCount > DefaultState_MaxLines && testWidth < maxContentWidth - 60)
            {
                testWidth = Math.Min(maxContentWidth - 60, 500);
                (measuredWidth, measuredHeight, lineCount) = MeasureTextWithLineCount(content, typeface, 14, testWidth, LineHeight);
            }

            // 限制最多3行
            double maxContentHeight = DefaultState_MaxLines * LineHeight;
            double contentHeight = Math.Min(measuredHeight, maxContentHeight);

            // 计算最终尺寸
            _defaultStateWidth = Math.Max(testWidth + 60, DefaultState_MinWidth);
            _defaultStateWidth = Math.Min(_defaultStateWidth, maxContentWidth);

            // 展开高度计算:
            // - 标题行: 图标44 + 上边距2 + 下边距10 = 56
            // - 内容区域: 内容高度 + ContentArea的Padding(8*2=16) + 下边距10 = contentHeight + 26
            // - ContentGrid的Margin: 上下各10 = 20 (但这是外部的，不计入MaxHeight)
            // 注意: ContentGrid.MaxHeight 控制的是内部内容高度
            _defaultStateHeight = 56 + contentHeight + 26;

            // 如果有Hero图片，增加高度
            if (notification.HeroImage != null || !string.IsNullOrEmpty(notification.HeroImagePath))
            {
                _defaultStateHeight += 158; // 150高度 + 8边距
            }

            // 如果有操作按钮，增加高度 (按钮高度约32 + 上边距)
            if (notification.Actions != null && notification.Actions.Count > 0)
            {
                _defaultStateHeight += 36;
            }
        }

        /// <summary>
        /// 计算自定义内容（UserControl/HTML）的展开尺寸
        /// 最小状态同 PlainText 计算，展开状态用 ExpandedWidth 和 ExpandedHeight
        /// </summary>
        private void CalculateCustomContentSize(IPhobosNotification notification, Rect screen)
        {
            double maxContentWidth = screen.Width * 0.6;
            double maxContentHeight = screen.Height * 0.5;

            // 使用通知提供的尺寸，或使用默认值
            _defaultStateWidth = notification.ExpandedWidth.HasValue
                ? Math.Min(notification.ExpandedWidth.Value, maxContentWidth)
                : Math.Min(540, maxContentWidth);

            // 基础高度: 标题行56 + 内容区域边距26
            double baseHeight = 56 + 26;

            // 如果有Hero图片，增加高度
            if (notification.HeroImage != null || !string.IsNullOrEmpty(notification.HeroImagePath))
            {
                baseHeight += 158;
            }

            // 如果有操作按钮，增加高度
            if (notification.Actions != null && notification.Actions.Count > 0)
            {
                baseHeight += 36;
            }

            _defaultStateHeight = notification.ExpandedHeight.HasValue
                ? Math.Min(notification.ExpandedHeight.Value + baseHeight, maxContentHeight)
                : Math.Min(200 + baseHeight, maxContentHeight);

            // 确保展开宽度不小于最小值
            _defaultStateWidth = Math.Max(_defaultStateWidth, DefaultState_MinWidth);
        }

        /// <summary>
        /// 测量文本尺寸（单行）
        /// </summary>
        private (double width, double height) MeasureText(string text, Typeface typeface, double fontSize, double maxWidth)
        {
            double pixelsPerDip = 1.0;
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    pixelsPerDip = source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch { }

            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip)
            {
                MaxTextWidth = maxWidth,
                Trimming = TextTrimming.CharacterEllipsis,
                MaxLineCount = 1
            };

            return (formattedText.Width, formattedText.Height);
        }

        /// <summary>
        /// 测量文本尺寸（多行，带行数统计）
        /// </summary>
        private (double width, double height, int lineCount) MeasureTextWithLineCount(string text, Typeface typeface, double fontSize, double maxWidth, double lineHeight)
        {
            double pixelsPerDip = 1.0;
            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    pixelsPerDip = source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch { }

            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip)
            {
                MaxTextWidth = maxWidth,
                Trimming = TextTrimming.None
            };

            int lineCount = (int)Math.Ceiling(formattedText.Height / lineHeight);
            return (formattedText.Width, formattedText.Height, lineCount);
        }

        private void SetContent(IPhobosNotification notification)
        {
            // 获取主题画刷
            var foregroundBrush = (Brush)FindResource("Foreground1Brush");
            var secondaryForegroundBrush = (Brush)FindResource("Foreground2Brush");

            // 计算内容区域的最大高度（仅对 PlainText 限制为3行）
            double maxContentHeight = notification.ContentType == NotificationContentType.PlainText
                ? DefaultState_MaxLines * LineHeight
                : double.PositiveInfinity;

            switch (notification.ContentType)
            {
                case NotificationContentType.PlainText:
                    var textBlock = new TextBlock
                    {
                        Text = notification.Content?.ToString() ?? string.Empty,
                        Foreground = secondaryForegroundBrush,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.9,
                        MaxHeight = maxContentHeight,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        LineHeight = LineHeight
                    };
                    ContentPresenter.Content = textBlock;
                    break;

                case NotificationContentType.Html:
                    var htmlTextBlock = new TextBlock
                    {
                        Text = notification.Content?.ToString() ?? string.Empty,
                        Foreground = secondaryForegroundBrush,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.9,
                        LineHeight = LineHeight
                    };
                    ContentPresenter.Content = htmlTextBlock;
                    break;

                case NotificationContentType.UserControl:
                    if (notification.Content is UIElement element)
                    {
                        ContentPresenter.Content = element;
                    }
                    break;
            }

            // 设置内容区域的最大高度
            if (notification.ContentType == NotificationContentType.PlainText)
            {
                ContentArea.MaxHeight = maxContentHeight + 16; // 加上内边距
            }
            else
            {
                ContentArea.ClearValue(MaxHeightProperty);
            }
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
                    await Dispatcher.InvokeAsync(() => CloseNotification(isTimeout: true));
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

        #region 声音播放

        /// <summary>
        /// 播放通知声音
        /// </summary>
        private void PlayNotificationSound(IPhobosNotification notification)
        {
            if (string.IsNullOrEmpty(notification.SoundPath))
                return;

            try
            {
                _soundPlayer = new SoundPlayer(notification.SoundPath);

                if (notification.Type == NotificationType.Alarm)
                {
                    // 闹钟模式：循环播放直到超时或关闭
                    StartAlarmLoop();
                }
                else
                {
                    // 普通模式：播放一次
                    _soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Notifier", $"Failed to play sound: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始闹钟循环播放
        /// </summary>
        private void StartAlarmLoop()
        {
            if (_soundPlayer == null || _isAlarmPlaying)
                return;

            _isAlarmPlaying = true;
            _alarmLoopTokenSource?.Cancel();
            _alarmLoopTokenSource = new CancellationTokenSource();

            var token = _alarmLoopTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && _isAlarmPlaying)
                    {
                        try
                        {
                            _soundPlayer?.PlaySync();
                        }
                        catch
                        {
                            // 忽略播放错误，继续尝试
                        }

                        // 短暂延迟后继续播放
                        await Task.Delay(100, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // 循环被取消
                }
            }, token);
        }

        /// <summary>
        /// 停止声音播放
        /// </summary>
        private void StopSound()
        {
            _isAlarmPlaying = false;
            _alarmLoopTokenSource?.Cancel();

            try
            {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
                _soundPlayer = null;
            }
            catch
            {
                // 忽略停止错误
            }
        }

        #endregion

        #region 动画

        /// <summary>
        /// 入场动画 - 最小状态出现
        /// 从屏幕顶整体缩放飞出，布局左滑淡入
        /// </summary>
        private void PlayEnterAnimation()
        {
            // 确保显示最小化内容
            MinimizedContent.Visibility = Visibility.Visible;
            ExpandedContent.Visibility = Visibility.Collapsed;

            // 初始状态：整体缩小，在上方
            BorderScale.ScaleX = 0.5;
            BorderScale.ScaleY = 0.5;
            BorderTranslate.Y = -40;
            MainBorder.Opacity = 0;
            MainBorder.Width = _minStateWidth;

            // 内容初始状态：在左侧，透明
            MinContentTranslate.X = -SlideOffset;
            MinimizedContent.Opacity = 0;

            var storyboard = new Storyboard();

            // 1. 淡入
            PUAnimation.AddOpacityAnimation(storyboard, MainBorder, 0, 1, 180, PUAnimation.CreateSmoothEase());

            // 2. 整体缩放 X (0.5 -> 1)
            PUAnimation.AddScaleXAnimation(storyboard, MainBorder, 0.5, 1.0, AnimDuration_Enter,
                PUAnimation.CreateBackEase(EasingMode.EaseOut, 0.3), 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            // 3. 整体缩放 Y (0.5 -> 1)
            PUAnimation.AddScaleYAnimation(storyboard, MainBorder, 0.5, 1.0, AnimDuration_Enter,
                PUAnimation.CreateBackEase(EasingMode.EaseOut, 0.3), 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            // 4. 向下飞出 (Y: -40 -> 0)
            PUAnimation.AddTranslateYAnimation(storyboard, MainBorder, -40, 0, AnimDuration_Enter,
                PUAnimation.CreateExpEase(EasingMode.EaseOut, 3), 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");

            // 5. 内容左滑淡入 (X: -SlideOffset -> 0, Opacity: 0 -> 1)
            PUAnimation.AddTranslateXAnimation(storyboard, MinimizedContent, -SlideOffset, 0, AnimDuration_SlideContent,
                PUAnimation.CreateExpEase(EasingMode.EaseOut, 3), 150,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, MinimizedContent, 0, 1, AnimDuration_SlideContent,
                PUAnimation.CreateSmoothEase(), 150);

            storyboard.Completed += (s, e) =>
            {
                // 清除动画，设置最终值
                MainBorder.BeginAnimation(WidthProperty, null);
                MainBorder.Width = _minStateWidth;
                BorderScale.ScaleX = 1.0;
                BorderScale.ScaleY = 1.0;
                BorderTranslate.Y = 0;
                MinContentTranslate.X = 0;
                MinimizedContent.Opacity = 1;
            };

            storyboard.Begin();
        }

        /// <summary>
        /// 退场动画 - 最小状态离开
        /// 飞向屏幕顶且收缩
        /// </summary>
        private void PlayExitAnimation(Action onComplete)
        {
            if (_isExiting) return;
            _isExiting = true;

            if (_isExpanded)
            {
                // 如果当前是展开状态，先收缩再退出
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

            // 1. 内容右滑淡出
            PUAnimation.AddTranslateXAnimation(storyboard, MinimizedContent, 0, SlideOffset, 150,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn), 0,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, MinimizedContent, 1, 0, 150,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn));

            // 2. 向上飞出 (Y: 0 -> -40)
            PUAnimation.AddTranslateYAnimation(storyboard, MainBorder, 0, -40, AnimDuration_Exit,
                PUAnimation.CreateExpEase(EasingMode.EaseIn, 3), 80,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");

            // 3. 整体缩放 X (1 -> 0.5)
            PUAnimation.AddScaleXAnimation(storyboard, MainBorder, 1.0, 0.5, AnimDuration_Exit,
                PUAnimation.CreateExpEase(EasingMode.EaseIn, 3), 80,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            // 4. 整体缩放 Y (1 -> 0.5)
            PUAnimation.AddScaleYAnimation(storyboard, MainBorder, 1.0, 0.5, AnimDuration_Exit,
                PUAnimation.CreateExpEase(EasingMode.EaseIn, 3), 80,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            // 5. 淡出
            PUAnimation.AddOpacityAnimation(storyboard, MainBorder, 1, 0, 200,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn), AnimDuration_Exit - 100);

            storyboard.Completed += (s, e) =>
            {
                _isExiting = false;
                onComplete();
            };

            storyboard.Begin();
        }

        /// <summary>
        /// 展开通知 - 最小状态 -> 默认状态
        /// 布局右滑淡出, 窗口动态展开, 新布局左滑淡入
        /// </summary>
        private void ExpandNotification()
        {
            if (_isExpanded || _isAnimating || _isExiting) return;
            _isAnimating = true;

            PauseAutoCloseTimer();

            // 清除之前的动画
            MainBorder.BeginAnimation(WidthProperty, null);
            ContentGrid.BeginAnimation(MaxHeightProperty, null);

            // 使用最小状态的计算尺寸作为起点
            double currentWidth = _minStateWidth;
            double currentHeight = _minStateHeight;

            // 准备展开内容
            ExpandedContent.Visibility = Visibility.Visible;
            ExpandedContent.Opacity = 0;
            ExpandContentTranslate.X = -SlideOffset;

            // 设置初始值
            MainBorder.Width = currentWidth;
            ContentGrid.MaxHeight = currentHeight;

            var storyboard = new Storyboard();

            // 1. 最小化内容右滑淡出
            PUAnimation.AddTranslateXAnimation(storyboard, MinimizedContent, 0, SlideOffset, AnimDuration_SlideContent,
                PUAnimation.CreateExpEase(EasingMode.EaseIn, 3), 0,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, MinimizedContent, 1, 0, AnimDuration_SlideContent,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn));

            // 2. 窗口宽度展开
            PUAnimation.AddWidthAnimation(storyboard, MainBorder, currentWidth, _defaultStateWidth, AnimDuration_WindowResize,
                PUAnimation.CreateBackEase(EasingMode.EaseOut, 0.25), 100);

            // 3. 窗口高度展开
            PUAnimation.AddMaxHeightAnimation(storyboard, ContentGrid, currentHeight, _defaultStateHeight, AnimDuration_WindowResize,
                PUAnimation.CreateBackEase(EasingMode.EaseOut, 0.25), 100);

            // 4. 展开内容左滑淡入
            PUAnimation.AddTranslateXAnimation(storyboard, ExpandedContent, -SlideOffset, 0, AnimDuration_SlideContent,
                PUAnimation.CreateExpEase(EasingMode.EaseOut, 3), 200,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, ExpandedContent, 0, 1, AnimDuration_SlideContent,
                PUAnimation.CreateSmoothEase(), 200);

            storyboard.Completed += (s, e) =>
            {
                // 清除动画，设置最终值
                MainBorder.BeginAnimation(WidthProperty, null);
                MainBorder.Width = _defaultStateWidth;
                ContentGrid.BeginAnimation(MaxHeightProperty, null);
                ContentGrid.MaxHeight = _defaultStateHeight;

                MinimizedContent.Visibility = Visibility.Collapsed;
                MinimizedContent.Opacity = 1;
                MinContentTranslate.X = 0;

                ExpandedContent.Opacity = 1;
                ExpandContentTranslate.X = 0;

                _isExpanded = true;
                _isAnimating = false;
            };

            storyboard.Begin();
        }

        /// <summary>
        /// 收缩通知 - 默认状态 -> 最小状态
        /// 布局右滑淡出, 窗口动态收缩, 新布局左滑淡入
        /// </summary>
        private void CollapseNotification()
        {
            if (!_isExpanded || _isAnimating || _isExiting) return;
            _isAnimating = true;

            // 清除之前的动画
            MainBorder.BeginAnimation(WidthProperty, null);
            ContentGrid.BeginAnimation(MaxHeightProperty, null);

            // 使用展开状态的目标尺寸作为起点（与展开动画结束时设置的值一致）
            double currentWidth = _defaultStateWidth;
            double currentHeight = _defaultStateHeight;

            // 目标尺寸使用最小状态的计算值
            double targetWidth = _minStateWidth;
            double targetHeight = _minStateHeight;

            // 准备最小化内容
            MinimizedContent.Visibility = Visibility.Visible;
            MinimizedContent.Opacity = 0;
            MinContentTranslate.X = -SlideOffset;

            // 设置初始值
            MainBorder.Width = currentWidth;
            ContentGrid.MaxHeight = currentHeight;

            var storyboard = new Storyboard();

            // 1. 展开内容右滑淡出
            PUAnimation.AddTranslateXAnimation(storyboard, ExpandedContent, 0, SlideOffset, AnimDuration_SlideContent,
                PUAnimation.CreateExpEase(EasingMode.EaseIn, 3), 0,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, ExpandedContent, 1, 0, AnimDuration_SlideContent,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn));

            // 2. 窗口宽度收缩（恢复到保存的宽度）
            PUAnimation.AddWidthAnimation(storyboard, MainBorder, currentWidth, targetWidth, AnimDuration_WindowResize,
                PUAnimation.CreateSmoothEase(), 100);

            // 3. 窗口高度收缩（恢复到保存的高度）
            PUAnimation.AddMaxHeightAnimation(storyboard, ContentGrid, currentHeight, targetHeight, AnimDuration_WindowResize,
                PUAnimation.CreateSmoothEase(), 100);

            // 4. 最小化内容左滑淡入
            PUAnimation.AddTranslateXAnimation(storyboard, MinimizedContent, -SlideOffset, 0, AnimDuration_SlideContent,
                PUAnimation.CreateExpEase(EasingMode.EaseOut, 3), 200,
                "(UIElement.RenderTransform).(TranslateTransform.X)");

            PUAnimation.AddOpacityAnimation(storyboard, MinimizedContent, 0, 1, AnimDuration_SlideContent,
                PUAnimation.CreateSmoothEase(), 200);

            storyboard.Completed += (s, e) =>
            {
                // 清除动画，设置最终值
                MainBorder.BeginAnimation(WidthProperty, null);
                MainBorder.Width = _minStateWidth;
                ContentGrid.BeginAnimation(MaxHeightProperty, null);
                ContentGrid.MaxHeight = _minStateHeight;

                ExpandedContent.Visibility = Visibility.Collapsed;
                ExpandedContent.Opacity = 1;
                ExpandContentTranslate.X = 0;

                MinimizedContent.Opacity = 1;
                MinContentTranslate.X = 0;

                _isExpanded = false;
                _isAnimating = false;

                ResumeAutoCloseTimer();
            };

            storyboard.Begin();
        }

        /// <summary>
        /// 为退出准备的收缩动画（更快速）
        /// </summary>
        private void CollapseNotificationForExit(Action onComplete)
        {
            if (!_isExpanded)
            {
                onComplete();
                return;
            }

            _isAnimating = true;

            // 清除之前的动画
            MainBorder.BeginAnimation(WidthProperty, null);
            ContentGrid.BeginAnimation(MaxHeightProperty, null);

            // 使用展开状态的目标尺寸作为起点（与展开动画结束时设置的值一致）
            double currentWidth = _defaultStateWidth;
            double currentHeight = _defaultStateHeight;

            // 准备最小化内容
            MinimizedContent.Visibility = Visibility.Visible;
            MinimizedContent.Opacity = 0;

            // 设置初始值
            MainBorder.Width = currentWidth;
            ContentGrid.MaxHeight = currentHeight;

            var storyboard = new Storyboard();

            // 快速淡出展开内容
            PUAnimation.AddOpacityAnimation(storyboard, ExpandedContent, 1, 0, 100,
                PUAnimation.CreateSmoothEase(EasingMode.EaseIn));

            // 快速收缩宽度
            PUAnimation.AddWidthAnimation(storyboard, MainBorder, currentWidth, _minStateWidth, 180,
                PUAnimation.CreateSmoothEase());

            // 快速收缩高度
            PUAnimation.AddMaxHeightAnimation(storyboard, ContentGrid, currentHeight, _minStateHeight, 180,
                PUAnimation.CreateSmoothEase());

            // 快速淡入最小化内容
            PUAnimation.AddOpacityAnimation(storyboard, MinimizedContent, 0, 1, 100, null, 100);

            storyboard.Completed += (s, e) =>
            {
                // 清除动画，设置最终值
                MainBorder.BeginAnimation(WidthProperty, null);
                MainBorder.Width = _minStateWidth;
                ContentGrid.BeginAnimation(MaxHeightProperty, null);
                ContentGrid.MaxHeight = _minStateHeight;

                ExpandedContent.Visibility = Visibility.Collapsed;
                ExpandedContent.Opacity = 1;
                MinimizedContent.Opacity = 1;

                _isExpanded = false;
                _isAnimating = false;

                onComplete();
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

        private void CloseNotification(bool isTimeout = false)
        {
            _autoCloseTokenSource?.Cancel();

            // 立刻停止声音播放
            StopSound();

            // 如果是超时且为闹钟类型，调用超时回调
            if (isTimeout && _notification?.Type == NotificationType.Alarm)
            {
                try
                {
                    _notification.TimeoutAction?.Invoke();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Notifier", $"TimeoutAction failed: {ex.Message}");
                }
            }

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
