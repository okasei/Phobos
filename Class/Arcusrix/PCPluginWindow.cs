using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Phobos.Shared.Interface;
using Phobos.Manager.Arcusrix;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace Phobos.Class.Arcusrix
{
    /// <summary>
    /// 插件窗口类 - 提供默认布局
    /// </summary>
    public class PCPluginWindow : Window
    {
        #region DWM Native APIs

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // Win32 消息常量
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCACTIVATE = 0x0086;
        private const int WM_GETMINMAXINFO = 0x0024;

        // 系统命令常量
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;
        private const int SC_CLOSE = 0xF060;

        // HitTest 结果常量
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        #endregion

        private HwndSource? _hwndSource;

        private readonly IPhobosPlugin _plugin;
        private WindowState _lastState = WindowState.Normal;
        private bool _isClosing = false;

        // 布局元素
        public Grid PhobosPluginArea { get; private set; }
        public Grid TitleArea { get; private set; }
        public Image IconImage { get; private set; }
        public TextBlock TitleText { get; private set; }
        public Label MinBtn { get; private set; }
        public Label ResBtn { get; private set; }
        public Label CloseBtn { get; private set; }
        public Label FakeTitle { get; private set; }
        public Grid BackgroundArea { get; private set; }
        public Border BackBorder { get; private set; }
        public Label BackLabel { get; private set; }
        public Label OpacityLabel { get; private set; }
        public Grid ContentArea { get; private set; }

        public PCPluginWindow(IPhobosPlugin plugin, string? title = null)
        {
            _plugin = plugin;
            var metadata = plugin.Metadata;

            // 基础窗口设置
            // 使用 SingleBorderWindow 而非 None，以保留原生窗口动画
            WindowStyle = WindowStyle.SingleBorderWindow;
            AllowsTransparency = false;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            // 应用窗口大小设置
            ApplyWindowSize(metadata);

            Title = title ?? metadata.GetLocalizedName(Shared.Class.LocalizationManager.Instance.CurrentLanguage);

            // 使用 WindowChrome 隐藏标题栏但保留原生动画
            var windowChrome = new WindowChrome
            {
                CaptionHeight = 0,                          // 隐藏系统标题栏
                ResizeBorderThickness = metadata.SizeMode == WindowSizeMode.Fixed
                    ? new Thickness(0)                      // 固定大小模式禁用调整边框
                    : new Thickness(6),                     // 系统调整大小边框
                GlassFrameThickness = new Thickness(0),     // 不使用玻璃效果
                UseAeroCaptionButtons = false,              // 不使用系统按钮
                NonClientFrameEdges = NonClientFrameEdges.None
            };
            WindowChrome.SetWindowChrome(this, windowChrome);

            // 固定大小模式禁止调整窗口大小
            if (metadata.SizeMode == WindowSizeMode.Fixed)
            {
                ResizeMode = ResizeMode.NoResize;
            }

            // 初始化布局（传入元数据以控制按钮显示）
            InitializeLayout(metadata);

            // 应用主题
            PMTheme.Instance.ApplyThemeToWindow(this);

            // 设置插件内容
            if (plugin.ContentArea != null)
            {
                // 如果插件内容已有父元素，先移除
                if (plugin.ContentArea.Parent is Panel parentPanel)
                {
                    parentPanel.Children.Remove(plugin.ContentArea);
                }
                else if (plugin.ContentArea.Parent is ContentControl parentContent)
                {
                    parentContent.Content = null;
                }
                else if (plugin.ContentArea.Parent is Decorator parentDecorator)
                {
                    parentDecorator.Child = null;
                }

                ContentArea.Children.Add(plugin.ContentArea);
            }

            // 窗口状态变化处理
            StateChanged += OnWindowStateChanged;

            // 播放打开动画
            Loaded += OnWindowLoaded;

            // 窗口初始化完成后应用 DWM 设置
            SourceInitialized += OnSourceInitialized;
        }

        /// <summary>
        /// 应用窗口大小设置
        /// </summary>
        private void ApplyWindowSize(PluginMetadata metadata)
        {
            // 默认值
            const double defaultWidth = 800;
            const double defaultHeight = 600;
            const double defaultMinWidth = 400;
            const double defaultMinHeight = 300;

            // 应用最小尺寸
            MinWidth = metadata.MinWindowWidth > 0 ? metadata.MinWindowWidth.Value : defaultMinWidth;
            MinHeight = metadata.MinWindowHeight > 0 ? metadata.MinWindowHeight.Value : defaultMinHeight;

            // 根据大小模式设置窗口尺寸
            switch (metadata.SizeMode)
            {
                case WindowSizeMode.SizeToContent:
                    // 自适应内容模式：设置 SizeToContent 并在 Loaded 后调整并居中
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    break;

                case WindowSizeMode.Fixed:
                case WindowSizeMode.Default:
                default:
                    // 使用首选尺寸或默认尺寸
                    Width = metadata.PreferredWidth > 0 ? metadata.PreferredWidth.Value : defaultWidth;
                    Height = metadata.PreferredHeight > 0 ? metadata.PreferredHeight.Value : defaultHeight;

                    // 确保不小于最小尺寸
                    if (Width < MinWidth) Width = MinWidth;
                    if (Height < MinHeight) Height = MinHeight;

                    // 居中显示
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    break;
            }
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            // 获取窗口句柄
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 添加 WndProc Hook 以启用原生动画
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);

            // 启用圆角（Windows 11）
            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            // 启用深色模式
            int useDarkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

            // 扩展 DWM 帧到客户区以启用原生动画
            var margins = new MARGINS { Left = 0, Right = 0, Top = 1, Bottom = 0 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }

        /// <summary>
        /// 窗口消息处理 - 用于保留原生动画
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xFFF0;
                    // 让系统处理最小化/恢复/最大化命令，以获得原生动画
                    if (command == SC_MINIMIZE || command == SC_RESTORE || command == SC_MAXIMIZE)
                    {
                        // 不标记为已处理，让系统继续处理以播放原生动画
                        handled = false;
                    }
                    break;

                case WM_NCACTIVATE:
                    // 返回 true 以防止非客户区在激活/失活时重绘
                    // 这可以避免闪烁同时保留动画
                    handled = true;
                    return new IntPtr(1);

                case WM_NCCALCSIZE:
                    // 当 wParam 为 true 时，我们可以调整客户区大小
                    if (wParam != IntPtr.Zero)
                    {
                        // 返回 0 让系统知道我们处理了这个消息
                        // 但不改变客户区大小（保持无边框外观）
                        handled = true;
                        return IntPtr.Zero;
                    }
                    break;

                case WM_GETMINMAXINFO:
                    // 处理最大化时的边界问题
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 处理 WM_GETMINMAXINFO 消息，确保最大化时窗口正确填充工作区
        /// </summary>
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            // 获取当前显示器的工作区
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var workArea = monitorInfo.rcWork;
                    var monitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(workArea.Right - workArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);

                    // 设置最小尺寸
                    mmi.ptMinTrackSize.X = (int)MinWidth;
                    mmi.ptMinTrackSize.Y = (int)MinHeight;
                }
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            PlayOpenAnimation();
        }

        private void PlayOpenAnimation()
        {
            var storyboard = new Storyboard();

            // 使用更有弹性的参数，让动画更灵动
            var bounceEase = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.4  // 回弹幅度
            };

            var smoothEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var quickEase = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 };

            // ===== 窗口背景动画 =====
            // 设置初始状态
            BackgroundArea.Opacity = 0;
            BackgroundArea.RenderTransformOrigin = new Point(0.5, 0.5);
            BackgroundArea.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(0.7, 0.7),       // 从70%开始，更明显的缩放
                    new TranslateTransform(0, 60),      // 从下方60px开始
                    new RotateTransform(-2)             // 轻微逆时针旋转
                }
            };

            // 1. 淡入动画 - 快速出现
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(fadeIn, BackgroundArea);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 2. 缩放动画 - 带回弹效果
            var scaleXAnim = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(scaleXAnim, BackgroundArea);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(scaleYAnim, BackgroundArea);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);

            // 3. Y轴位移动画 - 从下方弹入
            var slideAnim = new DoubleAnimation
            {
                From = 60,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(slideAnim, BackgroundArea);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideAnim);

            // 4. 旋转动画 - 轻微摆正
            var rotateAnim = new DoubleAnimation
            {
                From = -2,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(rotateAnim, BackgroundArea);
            Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"));
            storyboard.Children.Add(rotateAnim);

            // ===== 标题栏动画 - 延迟100ms =====
            TitleArea.Opacity = 0;
            TitleArea.RenderTransformOrigin = new Point(0.5, 0.5);
            TitleArea.RenderTransform = new TranslateTransform(0, -15);

            var titleFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(titleFadeIn, TitleArea);
            Storyboard.SetTargetProperty(titleFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(titleFadeIn);

            var titleSlide = new DoubleAnimation
            {
                From = -15,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(titleSlide, TitleArea);
            Storyboard.SetTargetProperty(titleSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(titleSlide);

            // ===== 内容区域动画 - 延迟150ms =====
            ContentArea.Opacity = 0;
            ContentArea.RenderTransformOrigin = new Point(0.5, 0.5);
            ContentArea.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(0.95, 0.95),
                    new TranslateTransform(0, 25)
                }
            };

            var contentFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentFadeIn, ContentArea);
            Storyboard.SetTargetProperty(contentFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeIn);

            var contentScaleX = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleX, ContentArea);
            Storyboard.SetTargetProperty(contentScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(contentScaleX);

            var contentScaleY = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleY, ContentArea);
            Storyboard.SetTargetProperty(contentScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(contentScaleY);

            var contentSlide = new DoubleAnimation
            {
                From = 25,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentSlide, ContentArea);
            Storyboard.SetTargetProperty(contentSlide, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(contentSlide);

            storyboard.Begin();
        }

        private void PlayCloseAnimation(Action onCompleted)
        {
            var storyboard = new Storyboard();

            // 使用加速曲线，让关闭动画更干脆
            var accelerateEase = new PowerEase { EasingMode = EasingMode.EaseIn, Power = 3 };
            var quickOut = new ExponentialEase { EasingMode = EasingMode.EaseIn, Exponent = 3 };

            // ===== 内容区域先消失 =====
            if (ContentArea.RenderTransform == null || ContentArea.RenderTransform is not TransformGroup)
            {
                ContentArea.RenderTransformOrigin = new Point(0.5, 0.5);
                ContentArea.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0)
                    }
                };
            }

            var contentFadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = quickOut
            };
            Storyboard.SetTarget(contentFadeOut, ContentArea);
            Storyboard.SetTargetProperty(contentFadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeOut);

            var contentSlideOut = new DoubleAnimation
            {
                To = 15,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(contentSlideOut, ContentArea);
            Storyboard.SetTargetProperty(contentSlideOut, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(contentSlideOut);

            // ===== 标题栏延迟消失 =====
            if (TitleArea.RenderTransform == null || TitleArea.RenderTransform is not TranslateTransform)
            {
                TitleArea.RenderTransformOrigin = new Point(0.5, 0.5);
                TitleArea.RenderTransform = new TranslateTransform(0, 0);
            }

            var titleFadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(100),
                BeginTime = TimeSpan.FromMilliseconds(30),
                EasingFunction = quickOut
            };
            Storyboard.SetTarget(titleFadeOut, TitleArea);
            Storyboard.SetTargetProperty(titleFadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(titleFadeOut);

            var titleSlideOut = new DoubleAnimation
            {
                To = -10,
                Duration = TimeSpan.FromMilliseconds(120),
                BeginTime = TimeSpan.FromMilliseconds(30),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(titleSlideOut, TitleArea);
            Storyboard.SetTargetProperty(titleSlideOut, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(titleSlideOut);

            // ===== 窗口背景最后消失 =====
            // 确保有变换组（包含旋转）
            if (BackgroundArea.RenderTransform == null || BackgroundArea.RenderTransform is not TransformGroup tg || tg.Children.Count < 3)
            {
                BackgroundArea.RenderTransformOrigin = new Point(0.5, 0.5);
                BackgroundArea.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0),
                        new RotateTransform(0)
                    }
                };
            }

            // 淡出动画 - 延迟后快速消失
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = quickOut
            };
            Storyboard.SetTarget(fadeOut, BackgroundArea);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // 缩放动画 - 快速缩小
            var scaleXAnim = new DoubleAnimation
            {
                To = 0.85,
                Duration = TimeSpan.FromMilliseconds(180),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(scaleXAnim, BackgroundArea);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                To = 0.85,
                Duration = TimeSpan.FromMilliseconds(180),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(scaleYAnim, BackgroundArea);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);

            // Y轴位移动画 - 向上飘走
            var slideAnim = new DoubleAnimation
            {
                To = -30,
                Duration = TimeSpan.FromMilliseconds(180),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(slideAnim, BackgroundArea);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideAnim);

            // 轻微旋转 - 增加灵动感
            var rotateAnim = new DoubleAnimation
            {
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(180),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = accelerateEase
            };
            Storyboard.SetTarget(rotateAnim, BackgroundArea);
            Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"));
            storyboard.Children.Add(rotateAnim);

            storyboard.Completed += (s, e) => onCompleted?.Invoke();
            storyboard.Begin();
        }

        /// <summary>
        /// 以动画方式关闭窗口
        /// </summary>
        public async void CloseWithAnimation()
        {
            if (_isClosing) return;
            _isClosing = true;

            await _plugin.OnClosing();
            PlayCloseAnimation(() => Close());
        }

        private void InitializeLayout(PluginMetadata metadata)
        {
            // PhobosPluginArea - 主容器
            PhobosPluginArea = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // 定义行
            PhobosPluginArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); // TitleArea
            PhobosPluginArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

            // BackgroundArea - 背景层
            BackgroundArea = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRowSpan(BackgroundArea, 2);

            // BackBorder - 边框
            BackBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // BackLabel - 背景
            BackLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            // OpacityLabel - 遮罩
            OpacityLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                IsHitTestVisible = false
            };

            BackgroundArea.Children.Add(BackLabel);
            BackgroundArea.Children.Add(BackBorder);
            BackgroundArea.Children.Add(OpacityLabel);

            // TitleArea - 标题栏
            TitleArea = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 40,
                Background = Brushes.Transparent
            };
            Grid.SetRow(TitleArea, 0);

            // IconImage
            IconImage = new Image
            {
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            // TitleText
            TitleText = new TextBlock
            {
                Text = Title,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(44, 0, 0, 0),
                FontSize = 14,
                Foreground = Brushes.White
            };

            // 窗口控制按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            // MinBtn - 最小化按钮
            if (metadata.ShowMinimizeButton)
            {
                MinBtn = CreateTitleButton("\uE921", false); // Segoe MDL2 Assets: ChromeMinimize
                MinBtn.MouseLeftButtonUp += (s, e) => WindowState = WindowState.Minimized;
                buttonPanel.Children.Add(MinBtn);
            }

            // ResBtn - 最大化/还原按钮
            if (metadata.ShowMaximizeButton)
            {
                ResBtn = CreateTitleButton("\uE922", false); // Segoe MDL2 Assets: ChromeMaximize
                ResBtn.MouseLeftButtonUp += (s, e) =>
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                };
                buttonPanel.Children.Add(ResBtn);
            }

            // CloseBtn - 关闭按钮
            if (metadata.ShowCloseButton)
            {
                CloseBtn = CreateTitleButton("\uE8BB", true); // Segoe MDL2 Assets: ChromeClose
                CloseBtn.MouseLeftButtonUp += (s, e) => CloseWithAnimation();
                buttonPanel.Children.Add(CloseBtn);
            }

            // FakeTitle - 拖动区域
            FakeTitle = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            FakeTitle.MouseLeftButtonDown += OnTitleMouseDown;
            FakeTitle.MouseMove += OnTitleMouseMove;

            TitleArea.Children.Add(FakeTitle);
            TitleArea.Children.Add(IconImage);
            TitleArea.Children.Add(TitleText);
            TitleArea.Children.Add(buttonPanel);

            // ContentArea - 内容区域
            ContentArea = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(8, 0, 8, 8)
            };
            Grid.SetRow(ContentArea, 1);

            // 组装布局
            PhobosPluginArea.Children.Add(BackgroundArea);
            PhobosPluginArea.Children.Add(TitleArea);
            PhobosPluginArea.Children.Add(ContentArea);

            Content = PhobosPluginArea;

            // 注意：不再需要自定义调整大小边框，WindowChrome.ResizeBorderThickness 已处理
        }

        private Label CreateTitleButton(string icon, bool isCloseButton)
        {
            var button = new Label
            {
                Content = icon,
                Width = 46,
                Height = 32,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10,
                Cursor = Cursors.Hand,
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };

            // 悬停颜色
            var hoverColor = isCloseButton
                ? Color.FromRgb(232, 17, 35)    // Windows 11 关闭按钮红色 (#E81123)
                : Color.FromRgb(255, 255, 255); // 普通按钮悬停白色
            var hoverOpacity = isCloseButton ? 1.0 : 0.1;

            button.MouseEnter += (s, e) =>
            {
                if (isCloseButton)
                {
                    button.Background = new SolidColorBrush(hoverColor);
                    button.Foreground = Brushes.White;
                }
                else
                {
                    button.Background = new SolidColorBrush(Color.FromArgb((byte)(hoverOpacity * 255), hoverColor.R, hoverColor.G, hoverColor.B));
                }
            };

            button.MouseLeave += (s, e) =>
            {
                button.Background = Brushes.Transparent;
                button.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            };

            // 按下效果
            button.MouseLeftButtonDown += (s, e) =>
            {
                if (isCloseButton)
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(241, 112, 122)); // 按下时较亮的红色
                }
                else
                {
                    button.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                }
            };

            return button;
        }

        private void OnTitleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnTitleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && WindowState == WindowState.Maximized)
            {
                var point = e.GetPosition(this);
                WindowState = WindowState.Normal;
                Left = point.X - Width / 2;
                Top = point.Y - 20;
                DragMove();
            }
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            // Segoe MDL2 Assets 图标: E922 = ChromeMaximize, E923 = ChromeRestore
            if (WindowState == WindowState.Maximized)
            {
                ResBtn.Content = "\uE923"; // ChromeRestore 图标
                // 最大化时动画过渡到无圆角
                PlayMaximizeAnimation();
            }
            else if (_lastState == WindowState.Maximized && WindowState == WindowState.Normal)
            {
                ResBtn.Content = "\uE922"; // ChromeMaximize 图标
                // 从最大化恢复到正常
                PlayRestoreFromMaximizeAnimation();
            }
            else if (_lastState == WindowState.Minimized && WindowState != WindowState.Minimized)
            {
                ResBtn.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
                // 从最小化恢复
                PlayRestoreFromMinimizeAnimation();
            }
            else
            {
                ResBtn.Content = "\uE922"; // ChromeMaximize 图标
                BackBorder.CornerRadius = new CornerRadius(8);
            }

            _lastState = WindowState;
        }

        /// <summary>
        /// 最大化动画 - 扩展效果（更明显）
        /// </summary>
        private void PlayMaximizeAnimation()
        {
            var storyboard = new Storyboard();
            var bounceEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            var smoothEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var quickEase = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 };

            // 确保有变换
            EnsureTransformGroups();

            // ===== 整体窗口快速淡入效果 =====
            PhobosPluginArea.Opacity = 0.7;
            var windowFadeIn = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(windowFadeIn, PhobosPluginArea);
            Storyboard.SetTargetProperty(windowFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(windowFadeIn);

            // ===== 背景区域动画 - 从小放大弹出 =====
            var bgScaleX = new DoubleAnimation
            {
                From = 0.92,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleX, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(bgScaleX);

            var bgScaleY = new DoubleAnimation
            {
                From = 0.92,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleY, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(bgScaleY);

            // 背景从中心向外扩展的位移效果
            var bgSlideY = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(bgSlideY, BackgroundArea);
            Storyboard.SetTargetProperty(bgSlideY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(bgSlideY);

            // ===== 圆角过渡动画 =====
            var cornerAnimation = new ObjectAnimationUsingKeyFrames();
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(6), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(4), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(2), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(0), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
            Storyboard.SetTarget(cornerAnimation, BackBorder);
            Storyboard.SetTargetProperty(cornerAnimation, new PropertyPath(Border.CornerRadiusProperty));
            storyboard.Children.Add(cornerAnimation);

            // ===== 内容区域动画 - 延迟弹入 =====
            ContentArea.Opacity = 0.5;
            var contentFadeIn = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(80),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentFadeIn, ContentArea);
            Storyboard.SetTargetProperty(contentFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeIn);

            var contentScaleX = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(60),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleX, ContentArea);
            Storyboard.SetTargetProperty(contentScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(contentScaleX);

            var contentScaleY = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(60),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleY, ContentArea);
            Storyboard.SetTargetProperty(contentScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(contentScaleY);

            var contentSlide = new DoubleAnimation
            {
                From = 25,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentSlide, ContentArea);
            Storyboard.SetTargetProperty(contentSlide, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(contentSlide);

            // ===== 标题栏动画 - 从上方滑入 =====
            TitleArea.Opacity = 0.6;
            var titleFadeIn = new DoubleAnimation
            {
                From = 0.6,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(titleFadeIn, TitleArea);
            Storyboard.SetTargetProperty(titleFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(titleFadeIn);

            var titleSlide = new DoubleAnimation
            {
                From = -15,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(40),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(titleSlide, TitleArea);
            Storyboard.SetTargetProperty(titleSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(titleSlide);

            storyboard.Begin();
        }

        /// <summary>
        /// 从最大化恢复动画 - 收缩效果（更明显）
        /// </summary>
        private void PlayRestoreFromMaximizeAnimation()
        {
            var storyboard = new Storyboard();
            var bounceEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
            var smoothEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var quickEase = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 };

            // 确保有变换
            EnsureTransformGroups();

            // ===== 整体窗口淡入 =====
            PhobosPluginArea.Opacity = 0.6;
            var windowFadeIn = new DoubleAnimation
            {
                From = 0.6,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(windowFadeIn, PhobosPluginArea);
            Storyboard.SetTargetProperty(windowFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(windowFadeIn);

            // ===== 背景区域动画 - 从大收缩到正常，带回弹 =====
            var bgScaleX = new DoubleAnimation
            {
                From = 1.08,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleX, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(bgScaleX);

            var bgScaleY = new DoubleAnimation
            {
                From = 1.08,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleY, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(bgScaleY);

            // 从上方落下的效果
            var bgSlideY = new DoubleAnimation
            {
                From = -25,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgSlideY, BackgroundArea);
            Storyboard.SetTargetProperty(bgSlideY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(bgSlideY);

            // 轻微旋转增加灵动感
            var bgRotate = new DoubleAnimation
            {
                From = 1.5,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(bgRotate, BackgroundArea);
            Storyboard.SetTargetProperty(bgRotate, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"));
            storyboard.Children.Add(bgRotate);

            // ===== 圆角过渡动画 =====
            var cornerAnimation = new ObjectAnimationUsingKeyFrames();
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(2), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(4), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(6), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            cornerAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new CornerRadius(8), KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
            Storyboard.SetTarget(cornerAnimation, BackBorder);
            Storyboard.SetTargetProperty(cornerAnimation, new PropertyPath(Border.CornerRadiusProperty));
            storyboard.Children.Add(cornerAnimation);

            // ===== 内容区域动画 - 延迟缩放弹入 =====
            ContentArea.Opacity = 0.4;
            var contentFadeIn = new DoubleAnimation
            {
                From = 0.4,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(80),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentFadeIn, ContentArea);
            Storyboard.SetTargetProperty(contentFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeIn);

            var contentScaleX = new DoubleAnimation
            {
                From = 1.05,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(60),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleX, ContentArea);
            Storyboard.SetTargetProperty(contentScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(contentScaleX);

            var contentScaleY = new DoubleAnimation
            {
                From = 1.05,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(60),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleY, ContentArea);
            Storyboard.SetTargetProperty(contentScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(contentScaleY);

            var contentSlide = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(70),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentSlide, ContentArea);
            Storyboard.SetTargetProperty(contentSlide, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(contentSlide);

            // ===== 标题栏动画 - 延迟从下方弹入 =====
            TitleArea.Opacity = 0.5;
            var titleFadeIn = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(titleFadeIn, TitleArea);
            Storyboard.SetTargetProperty(titleFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(titleFadeIn);

            var titleSlide = new DoubleAnimation
            {
                From = 12,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(40),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(titleSlide, TitleArea);
            Storyboard.SetTargetProperty(titleSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(titleSlide);

            storyboard.Begin();
        }

        /// <summary>
        /// 从最小化恢复动画 - 弹出效果（更明显）
        /// </summary>
        private void PlayRestoreFromMinimizeAnimation()
        {
            var storyboard = new Storyboard();
            var bounceEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
            var elasticEase = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 8 };
            var smoothEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var quickEase = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 };

            // 确保有变换
            EnsureTransformGroups();

            // ===== 整体窗口淡入 =====
            PhobosPluginArea.Opacity = 0;
            var windowFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = quickEase
            };
            Storyboard.SetTarget(windowFadeIn, PhobosPluginArea);
            Storyboard.SetTargetProperty(windowFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(windowFadeIn);

            // ===== 背景区域动画 - 从底部大幅弹出 =====
            var bgScaleX = new DoubleAnimation
            {
                From = 0.6,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleX, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(bgScaleX);

            var bgScaleY = new DoubleAnimation
            {
                From = 0.6,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(bgScaleY, BackgroundArea);
            Storyboard.SetTargetProperty(bgScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(bgScaleY);

            // 从底部大幅弹入
            var bgSlide = new DoubleAnimation
            {
                From = 80,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(bgSlide, BackgroundArea);
            Storyboard.SetTargetProperty(bgSlide, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(bgSlide);

            // 较大的旋转摆正效果
            var bgRotate = new DoubleAnimation
            {
                From = -3,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(bgRotate, BackgroundArea);
            Storyboard.SetTargetProperty(bgRotate, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)"));
            storyboard.Children.Add(bgRotate);

            // ===== 标题栏延迟动画 - 从上方滑入 =====
            TitleArea.Opacity = 0;
            var titleFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(280),
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(titleFadeIn, TitleArea);
            Storyboard.SetTargetProperty(titleFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(titleFadeIn);

            var titleSlide = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(titleSlide, TitleArea);
            Storyboard.SetTargetProperty(titleSlide, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            storyboard.Children.Add(titleSlide);

            // ===== 内容区域延迟动画 - 从下方弹入并缩放 =====
            ContentArea.Opacity = 0;
            var contentFadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentFadeIn, ContentArea);
            Storyboard.SetTargetProperty(contentFadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(contentFadeIn);

            var contentScaleX = new DoubleAnimation
            {
                From = 0.85,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(130),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleX, ContentArea);
            Storyboard.SetTargetProperty(contentScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(contentScaleX);

            var contentScaleY = new DoubleAnimation
            {
                From = 0.85,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(130),
                EasingFunction = bounceEase
            };
            Storyboard.SetTarget(contentScaleY, ContentArea);
            Storyboard.SetTargetProperty(contentScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(contentScaleY);

            var contentSlide = new DoubleAnimation
            {
                From = 40,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(380),
                BeginTime = TimeSpan.FromMilliseconds(140),
                EasingFunction = smoothEase
            };
            Storyboard.SetTarget(contentSlide, ContentArea);
            Storyboard.SetTargetProperty(contentSlide, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(contentSlide);

            // 设置正确的圆角
            BackBorder.CornerRadius = WindowState == WindowState.Maximized ? new CornerRadius(0) : new CornerRadius(8);

            storyboard.Begin();
        }

        /// <summary>
        /// 确保所有需要动画的元素都有正确的变换组
        /// </summary>
        private void EnsureTransformGroups()
        {
            // BackgroundArea
            if (BackgroundArea.RenderTransform == null || BackgroundArea.RenderTransform is not TransformGroup)
            {
                BackgroundArea.RenderTransformOrigin = new Point(0.5, 0.5);
                BackgroundArea.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0),
                        new RotateTransform(0)
                    }
                };
            }

            // TitleArea
            if (TitleArea.RenderTransform == null || TitleArea.RenderTransform is not TranslateTransform)
            {
                TitleArea.RenderTransformOrigin = new Point(0.5, 0.5);
                TitleArea.RenderTransform = new TranslateTransform(0, 0);
            }

            // ContentArea
            if (ContentArea.RenderTransform == null || ContentArea.RenderTransform is not TransformGroup)
            {
                ContentArea.RenderTransformOrigin = new Point(0.5, 0.5);
                ContentArea.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0)
                    }
                };
            }
        }

        private void AddResizeBorders()
        {
            // 边框用于调整窗口大小
            var edgeThickness = 4;   // 边缘厚度
            var cornerSize = 12;     // 角落大小（增大点击区域）

            // 角落边框（使用较大的尺寸便于点击）
            var corners = new[]
            {
                (Cursors.SizeNWSE, HorizontalAlignment.Left, VerticalAlignment.Top),      // 左上角
                (Cursors.SizeNESW, HorizontalAlignment.Right, VerticalAlignment.Top),     // 右上角
                (Cursors.SizeNWSE, HorizontalAlignment.Right, VerticalAlignment.Bottom),  // 右下角
                (Cursors.SizeNESW, HorizontalAlignment.Left, VerticalAlignment.Bottom)    // 左下角
            };

            foreach (var (cursor, hAlign, vAlign) in corners)
            {
                var border = new Border
                {
                    Width = cornerSize,
                    Height = cornerSize,
                    HorizontalAlignment = hAlign,
                    VerticalAlignment = vAlign,
                    Background = Brushes.Transparent,
                    Cursor = cursor
                };

                // 确保边框跨越所有行，这样底部边框才能正确定位
                Grid.SetRowSpan(border, 2);

                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (WindowState == WindowState.Maximized) return;
                    var resizeDirection = GetResizeDirection(hAlign, vAlign);
                    if (resizeDirection != ResizeDirection.None)
                    {
                        ResizeWindow(resizeDirection);
                    }
                };

                PhobosPluginArea.Children.Add(border);
            }

            // 边缘边框（需要避开角落区域）
            var edges = new[]
            {
                (Cursors.SizeNS, new Thickness(cornerSize, 0, cornerSize, 0), HorizontalAlignment.Stretch, VerticalAlignment.Top),      // 上边
                (Cursors.SizeWE, new Thickness(0, cornerSize, 0, cornerSize), HorizontalAlignment.Right, VerticalAlignment.Stretch),    // 右边
                (Cursors.SizeNS, new Thickness(cornerSize, 0, cornerSize, 0), HorizontalAlignment.Stretch, VerticalAlignment.Bottom),   // 下边
                (Cursors.SizeWE, new Thickness(0, cornerSize, 0, cornerSize), HorizontalAlignment.Left, VerticalAlignment.Stretch)      // 左边
            };

            foreach (var (cursor, margin, hAlign, vAlign) in edges)
            {
                var border = new Border
                {
                    Width = hAlign == HorizontalAlignment.Stretch ? double.NaN : edgeThickness,
                    Height = vAlign == VerticalAlignment.Stretch ? double.NaN : edgeThickness,
                    HorizontalAlignment = hAlign,
                    VerticalAlignment = vAlign,
                    Margin = margin,
                    Background = Brushes.Transparent,
                    Cursor = cursor
                };

                // 确保边框跨越所有行
                Grid.SetRowSpan(border, 2);

                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (WindowState == WindowState.Maximized) return;
                    var resizeDirection = GetResizeDirection(hAlign, vAlign);
                    if (resizeDirection != ResizeDirection.None)
                    {
                        ResizeWindow(resizeDirection);
                    }
                };

                PhobosPluginArea.Children.Add(border);
            }
        }

        private enum ResizeDirection
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8
        }

        private ResizeDirection GetResizeDirection(HorizontalAlignment hAlign, VerticalAlignment vAlign)
        {
            // Win32 WMSZ_* 常量映射:
            // WMSZ_LEFT=1, WMSZ_RIGHT=2, WMSZ_TOP=3, WMSZ_TOPLEFT=4,
            // WMSZ_TOPRIGHT=5, WMSZ_BOTTOM=6, WMSZ_BOTTOMLEFT=7, WMSZ_BOTTOMRIGHT=8
            if (hAlign == HorizontalAlignment.Left && vAlign == VerticalAlignment.Top)
                return ResizeDirection.TopLeft;
            if (hAlign == HorizontalAlignment.Stretch && vAlign == VerticalAlignment.Top)
                return ResizeDirection.Top;
            if (hAlign == HorizontalAlignment.Right && vAlign == VerticalAlignment.Top)
                return ResizeDirection.TopRight;
            if (hAlign == HorizontalAlignment.Right && vAlign == VerticalAlignment.Stretch)
                return ResizeDirection.Right;
            if (hAlign == HorizontalAlignment.Right && vAlign == VerticalAlignment.Bottom)
                return ResizeDirection.BottomRight;
            if (hAlign == HorizontalAlignment.Stretch && vAlign == VerticalAlignment.Bottom)
                return ResizeDirection.Bottom;
            if (hAlign == HorizontalAlignment.Left && vAlign == VerticalAlignment.Bottom)
                return ResizeDirection.BottomLeft;
            if (hAlign == HorizontalAlignment.Left && vAlign == VerticalAlignment.Stretch)
                return ResizeDirection.Left;
            return ResizeDirection.None;
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            // 使用 Win32 API 进行窗口调整
            var hwndSource = HwndSource.FromHwnd(
                new WindowInteropHelper(this).Handle);

            if (hwndSource != null)
            {
                SendMessage(hwndSource.Handle, 0x112, (IntPtr)(0xF000 + (int)direction), IntPtr.Zero);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 设置背景图片
        /// </summary>
        public void SetBackgroundImage(ImageSource? image)
        {
            if (image != null)
            {
                BackLabel.Background = new ImageBrush(image)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
        }

        /// <summary>
        /// 设置遮罩透明度
        /// </summary>
        public void SetOpacityMask(double opacity)
        {
            OpacityLabel.Background = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 0, 0, 0));
        }

        /// <summary>
        /// 设置窗口图标
        /// </summary>
        public void SetIcon(ImageSource? icon)
        {
            if (icon != null)
            {
                IconImage.Source = icon;
                Icon = icon;
            }
        }
    }
}