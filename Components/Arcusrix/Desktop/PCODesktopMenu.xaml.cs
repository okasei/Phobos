using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// 菜单项数据
    /// </summary>
    public class DesktopMenuItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsSeparator { get; set; } = false;
        public bool IsDanger { get; set; } = false;
        public Action? OnClick { get; set; }
    }

    /// <summary>
    /// PCODesktopMenu.xaml 的交互逻辑
    /// </summary>
    public partial class PCODesktopMenu : UserControl
    {
        private Window? _parentWindow;
        private bool _isOpen = false;

        public event EventHandler? MenuClosed;

        public PCODesktopMenu()
        {
            InitializeComponent();
            Loaded += PCODesktopMenu_Loaded;
        }

        private void PCODesktopMenu_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown += ParentWindow_PreviewMouseDown;
                _parentWindow.Deactivated += ParentWindow_Deactivated;
            }
        }

        private void ParentWindow_Deactivated(object? sender, EventArgs e)
        {
            if (_isOpen)
            {
                Close();
            }
        }

        private void ParentWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isOpen) return;

            // 检查点击是否在菜单外部
            var clickPoint = e.GetPosition(this);
            if (clickPoint.X < 0 || clickPoint.Y < 0 ||
                clickPoint.X > ActualWidth || clickPoint.Y > ActualHeight)
            {
                Close();
            }
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        public void Show(IEnumerable<DesktopMenuItem> items, Point position)
        {
            if (_isOpen)
            {
                Close();
                return;
            }

            // 清空并重建菜单项
            MenuItemsPanel.Children.Clear();

            foreach (var item in items)
            {
                if (item.IsSeparator)
                {
                    var separator = CreateSeparator();
                    MenuItemsPanel.Children.Add(separator);
                }
                else
                {
                    var menuItem = CreateMenuItem(item);
                    MenuItemsPanel.Children.Add(menuItem);
                }
            }

            // 设置位置
            Margin = new Thickness(position.X, position.Y, 0, 0);
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            // 显示菜单并播放动画
            Visibility = Visibility.Visible;
            _isOpen = true;
            PlayOpenAnimation();
        }

        /// <summary>
        /// 在元素旁边显示菜单
        /// </summary>
        public void ShowAt(IEnumerable<DesktopMenuItem> items, FrameworkElement target)
        {
            var position = target.TransformToAncestor((Visual)Parent).Transform(new Point(0, target.ActualHeight));
            Show(items, position);
        }

        /// <summary>
        /// 关闭菜单
        /// </summary>
        public void Close()
        {
            if (!_isOpen) return;

            PlayCloseAnimation(() =>
            {
                Visibility = Visibility.Collapsed;
                _isOpen = false;
                MenuClosed?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// 创建菜单项
        /// </summary>
        private Border CreateMenuItem(DesktopMenuItem item)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 8, 12, 8),
                Cursor = Cursors.Hand,
                Tag = item
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 图标
            var iconText = new TextBlock
            {
                Text = item.Icon,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(iconText, 0);
            grid.Children.Add(iconText);

            // 文本
            var textBlock = new TextBlock
            {
                Text = item.Text,
                FontSize = (double)FindResource("FontSizeSm"),
                Foreground = item.IsDanger
                    ? (SolidColorBrush)FindResource("DangerBrush")
                    : (SolidColorBrush)FindResource("Foreground1Brush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            border.Child = grid;

            // 悬停效果
            border.MouseEnter += (s, e) =>
            {
                AnimateMenuItemHover(border, true);
            };

            border.MouseLeave += (s, e) =>
            {
                AnimateMenuItemHover(border, false);
            };

            // 点击事件
            border.MouseLeftButtonUp += (s, e) =>
            {
                item.OnClick?.Invoke();
                Close();
                e.Handled = true;
            };

            return border;
        }

        /// <summary>
        /// 创建分隔线
        /// </summary>
        private Border CreateSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = (SolidColorBrush)FindResource("BorderBrush"),
                Margin = new Thickness(8, 4, 8, 4),
                Opacity = 0.5
            };
        }

        /// <summary>
        /// 菜单项悬停动画
        /// </summary>
        private void AnimateMenuItemHover(Border menuItem, bool isHover)
        {
            var targetColor = isHover
                ? ((SolidColorBrush)FindResource("Background3Brush")).Color
                : Colors.Transparent;

            var animation = new ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            if (menuItem.Background is SolidColorBrush brush)
            {
                brush = brush.Clone();
                menuItem.Background = brush;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                menuItem.Background = new SolidColorBrush(isHover ? targetColor : Colors.Transparent);
            }
        }

        /// <summary>
        /// 播放打开动画
        /// </summary>
        private void PlayOpenAnimation()
        {
            var storyboard = new Storyboard();

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 淡入
            MenuBorder.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeIn, MenuBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 缩放Y
            var scaleY = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleY, MenuBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            // Y轴位移
            var slideDown = new DoubleAnimation
            {
                From = -8,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(slideDown, MenuBorder);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideDown);

            // 菜单项逐个淡入
            int index = 0;
            foreach (UIElement child in MenuItemsPanel.Children)
            {
                child.Opacity = 0;
                var itemFade = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(150),
                    BeginTime = TimeSpan.FromMilliseconds(50 + index * 30),
                    EasingFunction = cubicEase
                };
                Storyboard.SetTarget(itemFade, child);
                Storyboard.SetTargetProperty(itemFade, new PropertyPath(OpacityProperty));
                storyboard.Children.Add(itemFade);
                index++;
            }

            storyboard.Begin();
        }

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        private void PlayCloseAnimation(Action onCompleted)
        {
            var storyboard = new Storyboard();

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseIn };

            // 淡出
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeOut, MenuBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // 缩放Y
            var scaleY = new DoubleAnimation
            {
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleY, MenuBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            // Y轴位移
            var slideUp = new DoubleAnimation
            {
                To = -5,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(slideUp, MenuBorder);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideUp);

            storyboard.Completed += (s, e) => onCompleted?.Invoke();
            storyboard.Begin();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown -= ParentWindow_PreviewMouseDown;
                _parentWindow.Deactivated -= ParentWindow_Deactivated;
            }
        }
    }
}
