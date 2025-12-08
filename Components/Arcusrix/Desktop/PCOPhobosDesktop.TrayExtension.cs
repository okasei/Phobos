using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// PCOPhobosDesktop 的托盘图标和自动隐藏扩展
    /// 此 partial class 添加以下功能：
    /// 1. 托盘图标（使用 Hardcodet.NotifyIcon.Wpf）
    /// 2. 失焦自动隐藏
    /// 3. 任务栏位置感知的出现/消失动画
    /// 
    /// NuGet 安装: Install-Package Hardcodet.NotifyIcon.Wpf
    /// </summary>
    public partial class PCOPhobosDesktop
    {
        #region Win32 API for Taskbar Position

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint ABM_GETTASKBARPOS = 0x00000005;
        private const uint ABE_LEFT = 0;
        private const uint ABE_TOP = 1;
        private const uint ABE_RIGHT = 2;
        private const uint ABE_BOTTOM = 3;

        #endregion

        #region Tray Icon Fields

        /// <summary>
        /// 托盘图标实例 (Hardcodet.Wpf.TaskbarNotification)
        /// </summary>
        private TaskbarIcon? _taskbarIcon;

        /// <summary>
        /// 是否启用托盘图标
        /// </summary>
        private bool _enableTrayIcon = false;

        /// <summary>
        /// 是否启用失焦自动隐藏
        /// </summary>
        private bool _enableAutoHide = false;

        /// <summary>
        /// 是否启用任务栏位置感知动画
        /// </summary>
        private bool _enableTaskbarAwareAnimation = false;

        /// <summary>
        /// 子窗口列表（用于焦点跟踪）
        /// </summary>
        private readonly List<Window> _childWindows = new();

        /// <summary>
        /// 子窗口显示状态记忆（窗口隐藏前打开的子窗口）
        /// </summary>
        private readonly List<Window> _rememberedChildWindows = new();

        /// <summary>
        /// 窗口边距（距离任务栏的距离）
        /// </summary>
        private const int WindowMargin = 23;

        #endregion

        #region Public Properties

        /// <summary>
        /// 启用/禁用托盘图标
        /// </summary>
        public bool EnableTrayIcon
        {
            get => _enableTrayIcon;
            set
            {
                _enableTrayIcon = value;
                if (value)
                    InitializeTrayIcon();
                else
                    DisposeTrayIcon();
            }
        }

        /// <summary>
        /// 启用/禁用失焦自动隐藏
        /// </summary>
        public bool EnableAutoHide
        {
            get => _enableAutoHide;
            set
            {
                _enableAutoHide = value;
                if (value)
                {
                    Deactivated += Window_Deactivated_AutoHide;
                }
                else
                {
                    Deactivated -= Window_Deactivated_AutoHide;
                }
            }
        }

        /// <summary>
        /// 启用/禁用任务栏位置感知动画
        /// </summary>
        public bool EnableTaskbarAwareAnimation
        {
            get => _enableTaskbarAwareAnimation;
            set => _enableTaskbarAwareAnimation = value;
        }

        #endregion

        #region Tray Icon Implementation

        /// <summary>
        /// 初始化托盘图标
        /// NuGet 包: Install-Package Hardcodet.NotifyIcon.Wpf
        /// </summary>
        private void InitializeTrayIcon()
        {
            if (_taskbarIcon != null) return;

            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "Phobos Desktop"
            };

            // 设置图标
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "desktop-icon.ico");
                if (File.Exists(iconPath))
                {
                    _taskbarIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    // 尝试使用 PNG 图标
                    var pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "desktop-icon.png");
                    if (File.Exists(pngPath))
                    {
                        var bitmap = new BitmapImage(new Uri(pngPath, UriKind.Absolute));
                        _taskbarIcon.IconSource = bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrayIcon] Failed to load icon: {ex.Message}");
            }

            // 创建右键菜单
            var contextMenu = new ContextMenu();

            var showMenuItem = new MenuItem
            {
                Header = DesktopLocalization.Get(DesktopLocalization.Tray_Show),
                FontWeight = FontWeights.Bold
            };
            showMenuItem.Click += (s, e) => ShowFromTray();

            var separatorItem = new Separator();

            var exitMenuItem = new MenuItem
            {
                Header = DesktopLocalization.Get(DesktopLocalization.Tray_Exit)
            };
            exitMenuItem.Click += (s, e) => ExitFromTray();

            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(separatorItem);
            contextMenu.Items.Add(exitMenuItem);

            _taskbarIcon.ContextMenu = contextMenu;

            // 左键单击显示/隐藏
            _taskbarIcon.TrayLeftMouseDown += (s, e) =>
            {
                if (IsVisible && WindowState != WindowState.Minimized)
                    HideToTray();
                else
                    ShowFromTray();
            };

            // 双击显示
            _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowFromTray();
        }

        /// <summary>
        /// 释放托盘图标
        /// </summary>
        private void DisposeTrayIcon()
        {
            if (_taskbarIcon != null)
            {
                _taskbarIcon.Dispose();
                _taskbarIcon = null;
            }
        }

        /// <summary>
        /// 从托盘显示窗口
        /// </summary>
        public void ShowFromTray()
        {
            // 先定位窗口
            PositionWindowByTaskbar();

            Show();
            if (_layout.IsFullscreen)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
            Activate();

            if (_enableTaskbarAwareAnimation)
            {
                PlayTaskbarAwareShowAnimation();
            }

            // 恢复之前记忆的子窗口
            RestoreChildWindows();
        }

        /// <summary>
        /// 根据任务栏位置定位窗口
        /// </summary>
        private void PositionWindowByTaskbar()
        {
            // 如果是全屏模式，不需要定位
            if (_layout.IsFullscreen)
                return;

            var taskbarPosition = GetTaskbarPosition();
            var taskbarSize = GetTaskbarSize();
            var workArea = SystemParameters.WorkArea;
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 计算窗口尺寸: 宽度50%屏幕，高度60%屏幕
            double windowWidth = screenWidth * 0.5;
            double windowHeight = screenHeight * 0.6;

            // 根据任务栏位置计算窗口位置
            double left, top;

            switch (taskbarPosition)
            {
                case TaskbarPosition.Bottom:
                    // 左右居中，底部贴边
                    left = (screenWidth - windowWidth) / 2;
                    top = workArea.Bottom - windowHeight - WindowMargin;
                    break;

                case TaskbarPosition.Top:
                    // 左右居中，顶部贴边
                    left = (screenWidth - windowWidth) / 2;
                    top = workArea.Top + WindowMargin;
                    break;

                case TaskbarPosition.Left:
                    // 上下居中，左侧贴边
                    left = workArea.Left + WindowMargin;
                    top = (screenHeight - windowHeight) / 2;
                    break;

                case TaskbarPosition.Right:
                    // 上下居中，右侧贴边
                    left = workArea.Right - windowWidth - WindowMargin;
                    top = (screenHeight - windowHeight) / 2;
                    break;

                default:
                    left = (screenWidth - windowWidth) / 2;
                    top = workArea.Bottom - windowHeight - WindowMargin;
                    break;
            }

            // 设置窗口位置和大小
            Width = windowWidth;
            Height = windowHeight;
            Left = left;
            Top = top;
        }

        /// <summary>
        /// 恢复之前记忆的子窗口
        /// </summary>
        private void RestoreChildWindows()
        {
            foreach (var window in _rememberedChildWindows.ToList())
            {
                if (window != null && !window.IsVisible)
                {
                    window.Show();
                }
            }
            _rememberedChildWindows.Clear();
        }

        /// <summary>
        /// 隐藏到托盘
        /// </summary>
        public void HideToTray()
        {
            // 记忆并隐藏所有可见的子窗口
            RememberAndHideChildWindows();

            if (_enableTaskbarAwareAnimation)
            {
                PlayTaskbarAwareHideAnimation(() =>
                {
                    Hide();
                });
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// 记忆并隐藏所有可见的子窗口
        /// </summary>
        private void RememberAndHideChildWindows()
        {
            _rememberedChildWindows.Clear();
            foreach (var window in _childWindows.ToList())
            {
                if (window != null && window.IsVisible)
                {
                    _rememberedChildWindows.Add(window);
                    window.Hide();
                }
            }
        }

        /// <summary>
        /// 从托盘退出应用
        /// </summary>
        private void ExitFromTray()
        {
            DisposeTrayIcon();
            _isClosingFromTray = true;
            Application.Current.Shutdown();
        }

        #endregion

        #region Auto Hide Implementation

        /// <summary>
        /// 窗口失焦事件处理（自动隐藏）
        /// </summary>
        private void Window_Deactivated_AutoHide(object? sender, EventArgs e)
        {
            // 检查是否有子窗口获得焦点，或者自定义菜单正在显示
            if (!IsChildWindowFocused() && !IsMenuVisible())
            {
                HideToTray();
            }
        }

        /// <summary>
        /// 注册子窗口（防止子窗口获得焦点时误隐藏主窗口）
        /// </summary>
        public void RegisterChildWindow(Window childWindow)
        {
            if (!_childWindows.Contains(childWindow))
            {
                _childWindows.Add(childWindow);
                childWindow.Closed += (s, e) =>
                {
                    _childWindows.Remove(childWindow);
                    _rememberedChildWindows.Remove(childWindow);
                };
                // 子窗口激活时，确保主窗口不会自动隐藏
                childWindow.Activated += (s, e) =>
                {
                    // 子窗口激活时不做任何事情，由 IsChildWindowFocused 检查
                };
            }
        }

        /// <summary>
        /// 检查是否有子窗口获得焦点
        /// </summary>
        private bool IsChildWindowFocused()
        {
            foreach (var window in _childWindows)
            {
                if (window.IsActive)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查自定义菜单是否可见
        /// </summary>
        private bool IsMenuVisible()
        {
            return DesktopMenu != null && DesktopMenu.IsVisible;
        }

        #endregion

        #region Taskbar Position Detection

        /// <summary>
        /// 任务栏位置枚举
        /// </summary>
        public enum TaskbarPosition
        {
            Bottom,
            Top,
            Left,
            Right
        }

        /// <summary>
        /// 获取任务栏位置
        /// </summary>
        public TaskbarPosition GetTaskbarPosition()
        {
            var data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);

            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) != IntPtr.Zero)
            {
                return data.uEdge switch
                {
                    ABE_LEFT => TaskbarPosition.Left,
                    ABE_TOP => TaskbarPosition.Top,
                    ABE_RIGHT => TaskbarPosition.Right,
                    _ => TaskbarPosition.Bottom
                };
            }

            return TaskbarPosition.Bottom;
        }

        /// <summary>
        /// 获取任务栏尺寸
        /// </summary>
        public int GetTaskbarSize()
        {
            var data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);

            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) != IntPtr.Zero)
            {
                var position = GetTaskbarPosition();
                return position switch
                {
                    TaskbarPosition.Top or TaskbarPosition.Bottom => data.rc.Bottom - data.rc.Top,
                    TaskbarPosition.Left or TaskbarPosition.Right => data.rc.Right - data.rc.Left,
                    _ => 40
                };
            }

            return 40;
        }

        #endregion

        #region Taskbar Aware Animation

        /// <summary>
        /// 播放任务栏位置感知的显示动画
        /// </summary>
        private void PlayTaskbarAwareShowAnimation()
        {
            var taskbarPosition = GetTaskbarPosition();
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(400);

            var elasticEase = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 1,
                Springiness = 8
            };

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 淡入
            MainBorder.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeIn, MainBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 根据任务栏位置设置动画方向
            double fromY = 0, fromX = 0;
            switch (taskbarPosition)
            {
                case TaskbarPosition.Bottom:
                    fromY = 50;
                    MainBorder.RenderTransformOrigin = new Point(0.5, 1);
                    break;
                case TaskbarPosition.Top:
                    fromY = -50;
                    MainBorder.RenderTransformOrigin = new Point(0.5, 0);
                    break;
                case TaskbarPosition.Left:
                    fromX = -50;
                    MainBorder.RenderTransformOrigin = new Point(0, 0.5);
                    break;
                case TaskbarPosition.Right:
                    fromX = 50;
                    MainBorder.RenderTransformOrigin = new Point(1, 0.5);
                    break;
            }

            // 确保有 TransformGroup
            EnsureTransformGroup();

            if (fromY != 0)
            {
                var translateY = new DoubleAnimation
                {
                    From = fromY,
                    To = 0,
                    Duration = duration,
                    EasingFunction = elasticEase
                };
                Storyboard.SetTarget(translateY, MainBorder);
                Storyboard.SetTargetProperty(translateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
                storyboard.Children.Add(translateY);
            }

            if (fromX != 0)
            {
                var translateX = new DoubleAnimation
                {
                    From = fromX,
                    To = 0,
                    Duration = duration,
                    EasingFunction = elasticEase
                };
                Storyboard.SetTarget(translateX, MainBorder);
                Storyboard.SetTargetProperty(translateX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)"));
                storyboard.Children.Add(translateX);
            }

            // 缩放动画
            var scaleX = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = duration,
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleX, MainBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                Duration = duration,
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleY, MainBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            storyboard.Begin();
        }

        /// <summary>
        /// 播放任务栏位置感知的隐藏动画
        /// </summary>
        private void PlayTaskbarAwareHideAnimation(Action? onCompleted = null)
        {
            var taskbarPosition = GetTaskbarPosition();
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(250);

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseIn };

            // 淡出
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeOut, MainBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // 根据任务栏位置设置动画方向
            double toY = 0, toX = 0;
            switch (taskbarPosition)
            {
                case TaskbarPosition.Bottom:
                    toY = 30;
                    break;
                case TaskbarPosition.Top:
                    toY = -30;
                    break;
                case TaskbarPosition.Left:
                    toX = -30;
                    break;
                case TaskbarPosition.Right:
                    toX = 30;
                    break;
            }

            // 确保有 TransformGroup
            EnsureTransformGroup();

            if (toY != 0)
            {
                var translateY = new DoubleAnimation
                {
                    From = 0,
                    To = toY,
                    Duration = duration,
                    EasingFunction = cubicEase
                };
                Storyboard.SetTarget(translateY, MainBorder);
                Storyboard.SetTargetProperty(translateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
                storyboard.Children.Add(translateY);
            }

            if (toX != 0)
            {
                var translateX = new DoubleAnimation
                {
                    From = 0,
                    To = toX,
                    Duration = duration,
                    EasingFunction = cubicEase
                };
                Storyboard.SetTarget(translateX, MainBorder);
                Storyboard.SetTargetProperty(translateX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)"));
                storyboard.Children.Add(translateX);
            }

            // 缩放动画
            var scaleX = new DoubleAnimation
            {
                From = 1,
                To = 0.95,
                Duration = duration,
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleX, MainBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation
            {
                From = 1,
                To = 0.95,
                Duration = duration,
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleY, MainBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            storyboard.Completed += (s, e) => onCompleted?.Invoke();
            storyboard.Begin();
        }

        /// <summary>
        /// 确保 MainBorder 有正确的 TransformGroup
        /// </summary>
        private void EnsureTransformGroup()
        {
            if (MainBorder.RenderTransform is not TransformGroup)
            {
                MainBorder.RenderTransform = new TransformGroup
                {
                    Children =
                    {
                        new ScaleTransform(1, 1),
                        new TranslateTransform(0, 0)
                    }
                };
            }
        }

        #endregion

        #region Cleanup Extension

        /// <summary>
        /// 清理托盘图标（在窗口关闭时调用）
        /// 注意：请在现有的 PCOPhobosDesktop_Closing 方法中调用此方法
        /// </summary>
        private void CleanupTrayExtension()
        {
            DisposeTrayIcon();
        }

        #endregion
    }

    #region Desktop Localization Extension

    /// <summary>
    /// DesktopLocalization 扩展 - 添加托盘图标相关的本地化字符串
    /// </summary>
    public static partial class DesktopLocalization
    {
        // 托盘图标相关
        public const string Tray_Show = "Tray_Show";
        public const string Tray_Exit = "Tray_Exit";

        // 在 LocalizationData 字典中添加以下条目：
        // { Tray_Show, new() { { "en-US", "Show" }, { "zh-CN", "显示" }, { "zh-TW", "顯示" }, { "ja-JP", "表示" } } },
        // { Tray_Exit, new() { { "en-US", "Exit" }, { "zh-CN", "退出" }, { "zh-TW", "退出" }, { "ja-JP", "終了" } } },
    }

    #endregion
}