using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Phobos.Components.Arcusrix.Menu
{
    /// <summary>
    /// 菜单项数据模型
    /// </summary>
    public class PhobosMenuItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsSeparator { get; set; } = false;
        public bool IsDanger { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public object? Tag { get; set; }
        public Action? OnClick { get; set; }
        public List<PhobosMenuItem>? Children { get; set; }
        public Dictionary<string, string>? LocalizedTexts { get; set; }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public string GetLocalizedText(string languageCode)
        {
            if (LocalizedTexts != null && LocalizedTexts.TryGetValue(languageCode, out var text))
                return text;
            if (LocalizedTexts != null && LocalizedTexts.TryGetValue("en-US", out var fallback))
                return fallback;
            return Text;
        }

        /// <summary>
        /// 快速创建分隔符
        /// </summary>
        public static PhobosMenuItem Separator() => new PhobosMenuItem { IsSeparator = true };

        /// <summary>
        /// 快速创建菜单项
        /// </summary>
        public static PhobosMenuItem Create(string id, string text, string icon = "", Action? onClick = null, bool isDanger = false)
        {
            return new PhobosMenuItem
            {
                Id = id,
                Text = text,
                Icon = icon,
                OnClick = onClick,
                IsDanger = isDanger
            };
        }
    }

    /// <summary>
    /// 菜单选中事件参数
    /// </summary>
    public class PhobosMenuItemSelectedEventArgs : EventArgs
    {
        public PhobosMenuItem SelectedItem { get; }
        public string ItemId => SelectedItem.Id;
        public object? Tag => SelectedItem.Tag;

        public PhobosMenuItemSelectedEventArgs(PhobosMenuItem selectedItem)
        {
            SelectedItem = selectedItem;
        }
    }

    /// <summary>
    /// PCOMenu - 通用菜单组件
    /// 提供设置菜单项和菜单项被选中的回调功能
    /// </summary>
    public partial class PCOMenu : UserControl
    {
        private Window? _parentWindow;
        private bool _isOpen = false;
        private string _currentLanguage = System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag;

        /// <summary>
        /// 菜单关闭事件
        /// </summary>
        public event EventHandler? MenuClosed;

        /// <summary>
        /// 菜单项被选中事件
        /// </summary>
        public event EventHandler<PhobosMenuItemSelectedEventArgs>? ItemSelected;

        /// <summary>
        /// 菜单是否打开
        /// </summary>
        public bool IsOpen => _isOpen;

        public PCOMenu()
        {
            InitializeComponent();
            Loaded += PCOMenu_Loaded;

            try
            {
                _currentLanguage = Phobos.Shared.Class.LocalizationManager.Instance.CurrentLanguage;
            }
            catch
            {
                _currentLanguage = "en-US";
            }
        }

        private void PCOMenu_Loaded(object sender, RoutedEventArgs e)
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
        public void Show(IEnumerable<PhobosMenuItem> items, Point position)
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
        /// 显示菜单（带回调）
        /// </summary>
        public void Show(IEnumerable<PhobosMenuItem> items, Point position, Action<PhobosMenuItem>? onItemSelected)
        {
            if (onItemSelected != null)
            {
                EventHandler<PhobosMenuItemSelectedEventArgs>? handler = null;
                handler = (s, e) =>
                {
                    onItemSelected(e.SelectedItem);
                    ItemSelected -= handler;
                };
                ItemSelected += handler;
            }

            Show(items, position);
        }

        /// <summary>
        /// 在元素旁边显示菜单
        /// </summary>
        public void ShowAt(IEnumerable<PhobosMenuItem> items, FrameworkElement target)
        {
            var position = target.TransformToAncestor((Visual)Parent).Transform(new Point(0, target.ActualHeight));
            Show(items, position);
        }

        /// <summary>
        /// 在元素旁边显示菜单（带回调）
        /// </summary>
        public void ShowAt(IEnumerable<PhobosMenuItem> items, FrameworkElement target, Action<PhobosMenuItem>? onItemSelected)
        {
            if (onItemSelected != null)
            {
                EventHandler<PhobosMenuItemSelectedEventArgs>? handler = null;
                handler = (s, e) =>
                {
                    onItemSelected(e.SelectedItem);
                    ItemSelected -= handler;
                };
                ItemSelected += handler;
            }

            ShowAt(items, target);
        }

        /// <summary>
        /// 关闭菜单
        /// </summary>
        public void Close()
        {
            if (!_isOpen) return;
            PlayCloseAnimation();
        }

        /// <summary>
        /// 创建菜单项
        /// </summary>
        private Border CreateMenuItem(PhobosMenuItem item)
        {
            var styleName = !item.IsEnabled ? "DisabledMenuItemStyle" :
                           item.IsDanger ? "DangerMenuItemStyle" : "MenuItemStyle";

            var border = new Border
            {
                Style = (Style)FindResource(styleName),
                Tag = item
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 图标
            if (!string.IsNullOrEmpty(item.Icon))
            {
                var iconText = new TextBlock
                {
                    Text = item.Icon,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(iconText, 0);
                grid.Children.Add(iconText);
            }

            // 文本
            var textBlock = new TextBlock
            {
                Text = item.GetLocalizedText(_currentLanguage),
                FontSize = 13,
                Foreground = item.IsDanger
                    ? (SolidColorBrush)FindResource("DangerBrush")
                    : (SolidColorBrush)FindResource("Foreground1Brush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            // 子菜单箭头
            if (item.Children != null && item.Children.Count > 0)
            {
                var arrow = new TextBlock
                {
                    Text = "›",
                    FontSize = 16,
                    Foreground = (SolidColorBrush)FindResource("Foreground3Brush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(arrow, 2);
                grid.Children.Add(arrow);
            }

            border.Child = grid;

            if (item.IsEnabled)
            {
                border.MouseLeftButtonUp += (s, e) =>
                {
                    item.OnClick?.Invoke();
                    ItemSelected?.Invoke(this, new PhobosMenuItemSelectedEventArgs(item));
                    Close();
                };
            }

            return border;
        }

        /// <summary>
        /// 创建分隔符
        /// </summary>
        private Rectangle CreateSeparator()
        {
            return new Rectangle
            {
                Height = 1,
                Fill = (SolidColorBrush)FindResource("Background4Brush"),
                Margin = new Thickness(8, 4, 8, 4)
            };
        }

        /// <summary>
        /// 播放打开动画
        /// </summary>
        private void PlayOpenAnimation()
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(200);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 淡入
            var fadeIn = new DoubleAnimation(0, 1, duration) { EasingFunction = easing };
            Storyboard.SetTarget(fadeIn, MenuBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 缩放 X
            var scaleX = new DoubleAnimation(0.95, 1, duration) { EasingFunction = easing };
            Storyboard.SetTarget(scaleX, MenuBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            // 缩放 Y
            var scaleY = new DoubleAnimation(0.95, 1, duration) { EasingFunction = easing };
            Storyboard.SetTarget(scaleY, MenuBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            // Y 轴偏移
            var translateY = new DoubleAnimation(-5, 0, duration) { EasingFunction = easing };
            Storyboard.SetTarget(translateY, MenuBorder);
            Storyboard.SetTargetProperty(translateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(translateY);

            storyboard.Begin();
        }

        /// <summary>
        /// 播放关闭动画
        /// </summary>
        private void PlayCloseAnimation()
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(150);
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };

            // 淡出
            var fadeOut = new DoubleAnimation(1, 0, duration) { EasingFunction = easing };
            Storyboard.SetTarget(fadeOut, MenuBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // 缩放
            var scaleX = new DoubleAnimation(1, 0.95, duration) { EasingFunction = easing };
            Storyboard.SetTarget(scaleX, MenuBorder);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation(1, 0.95, duration) { EasingFunction = easing };
            Storyboard.SetTarget(scaleY, MenuBorder);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleY);

            storyboard.Completed += (s, e) =>
            {
                _isOpen = false;
                Visibility = Visibility.Collapsed;
                MenuClosed?.Invoke(this, EventArgs.Empty);
            };

            storyboard.Begin();
        }
    }
}