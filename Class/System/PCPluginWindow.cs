using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Phobos.Shared.Interface;
using Phobos.Manager.System;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace Phobos.Class.System
{
    /// <summary>
    /// 插件窗口类 - 提供默认布局
    /// </summary>
    public class PCPluginWindow : Window
    {
        private readonly IPhobosPlugin _plugin;
        private WindowState _lastState = WindowState.Normal;

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

            // 基础窗口设置
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Width = 800;
            Height = 600;
            MinWidth = 400;
            MinHeight = 300;
            Title = title ?? plugin.Metadata.GetLocalizedName(Shared.Class.LocalizationManager.Instance.CurrentLanguage);

            // 初始化布局
            InitializeLayout();

            // 应用主题
            PMTheme.Instance.ApplyThemeToWindow(this);

            // 设置插件内容
            if (plugin.ContentArea != null)
            {
                ContentArea.Children.Add(plugin.ContentArea);
            }

            // 窗口状态变化处理
            StateChanged += OnWindowStateChanged;
        }

        private void InitializeLayout()
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

            // MinBtn
            MinBtn = CreateTitleButton("—");
            MinBtn.MouseLeftButtonUp += (s, e) => WindowState = WindowState.Minimized;

            // ResBtn
            ResBtn = CreateTitleButton("□");
            ResBtn.MouseLeftButtonUp += (s, e) =>
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };

            // CloseBtn
            CloseBtn = CreateTitleButton("×");
            CloseBtn.MouseLeftButtonUp += async (s, e) =>
            {
                await _plugin.OnClosing();
                Close();
            };
            CloseBtn.MouseEnter += (s, e) => CloseBtn.Background = new SolidColorBrush(Color.FromRgb(232, 17, 35));

            buttonPanel.Children.Add(MinBtn);
            buttonPanel.Children.Add(ResBtn);
            buttonPanel.Children.Add(CloseBtn);

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

            // 添加调整大小的边框
            AddResizeBorders();
        }

        private Label CreateTitleButton(string text)
        {
            return new Label
            {
                Content = text,
                Width = 46,
                Height = 40,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                FontSize = 16,
                Cursor = Cursors.Hand
            };
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
            if (WindowState == WindowState.Maximized)
            {
                ResBtn.Content = "❐";
                BackBorder.CornerRadius = new CornerRadius(0);
            }
            else
            {
                ResBtn.Content = "□";
                BackBorder.CornerRadius = new CornerRadius(8);
            }

            // 处理从最小化恢复的动画
            if (_lastState == WindowState.Minimized && WindowState != WindowState.Minimized)
            {
                ApplyRestoreAnimation();
            }

            _lastState = WindowState;
        }

        private void ApplyRestoreAnimation()
        {
            var theme = PMTheme.Instance.CurrentTheme;
            if (theme == null) return;

            var config = theme.GetDefaultAnimationConfig().OnRestore;
            if (config.Types == AnimationType.None) return;

            // 应用淡入动画
            if ((config.Types & AnimationType.FadeIn) != 0)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = config.Duration,
                    EasingFunction = config.EasingFunction
                };
                BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void AddResizeBorders()
        {
            // 边框用于调整窗口大小
            var thickness = 4;
            var cursors = new[]
            {
                (Cursors.SizeNWSE, new Thickness(0, 0, 0, 0), HorizontalAlignment.Left, VerticalAlignment.Top),
                (Cursors.SizeNS, new Thickness(thickness, 0, thickness, 0), HorizontalAlignment.Stretch, VerticalAlignment.Top),
                (Cursors.SizeNESW, new Thickness(0, 0, 0, 0), HorizontalAlignment.Right, VerticalAlignment.Top),
                (Cursors.SizeWE, new Thickness(0, thickness, 0, thickness), HorizontalAlignment.Right, VerticalAlignment.Stretch),
                (Cursors.SizeNWSE, new Thickness(0, 0, 0, 0), HorizontalAlignment.Right, VerticalAlignment.Bottom),
                (Cursors.SizeNS, new Thickness(thickness, 0, thickness, 0), HorizontalAlignment.Stretch, VerticalAlignment.Bottom),
                (Cursors.SizeNESW, new Thickness(0, 0, 0, 0), HorizontalAlignment.Left, VerticalAlignment.Bottom),
                (Cursors.SizeWE, new Thickness(0, thickness, 0, thickness), HorizontalAlignment.Left, VerticalAlignment.Stretch)
            };

            foreach (var (cursor, margin, hAlign, vAlign) in cursors)
            {
                var border = new Border
                {
                    Width = hAlign == HorizontalAlignment.Stretch ? double.NaN : thickness,
                    Height = vAlign == VerticalAlignment.Stretch ? double.NaN : thickness,
                    HorizontalAlignment = hAlign,
                    VerticalAlignment = vAlign,
                    Margin = margin,
                    Background = Brushes.Transparent,
                    Cursor = cursor
                };

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
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private ResizeDirection GetResizeDirection(HorizontalAlignment hAlign, VerticalAlignment vAlign)
        {
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