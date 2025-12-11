using Phobos.Class.Config;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Plugin;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Runner
{
    /// <summary>
    /// 处理器选择项视图模型
    /// </summary>
    public class HandlerSelectItem
    {
        public ProtocolHandlerOption Handler { get; set; } = null!;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public string? IconPath { get; set; }
        public bool IsNew { get; set; }
    }

    /// <summary>
    /// 处理器选择对话框
    /// </summary>
    public partial class PCOHandlerSelectDialog : Window
    {
        private readonly List<ProtocolHandlerOption> _newHandlers;
        private readonly List<ProtocolHandlerOption> _existingHandlers;
        private readonly RunRequest _request;
        private readonly string _protocolKey;

        public ProtocolHandlerOption? SelectedHandler { get; private set; }
        public bool SetAsDefault { get; private set; }

        public PCOHandlerSelectDialog(
            List<ProtocolHandlerOption> newHandlers,
            List<ProtocolHandlerOption> existingHandlers,
            RunRequest request,
            string protocolKey,
            string? title = null,
            string? subtitle = null)
        {
            InitializeComponent();

            _newHandlers = newHandlers;
            _existingHandlers = existingHandlers;
            _request = request;
            _protocolKey = protocolKey;

            if (!string.IsNullOrEmpty(title))
            {
                TitleText.Text = title;
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                SubtitleText.Text = subtitle;
                SubtitleText.Visibility = Visibility.Visible;
            }

            Loaded += async (s, e) => await LoadHandlersAsync();

            // ESC 键关闭
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    PlayCloseAnimation(() => Close());
                }
            };
        }

        private async Task LoadHandlersAsync()
        {
            var items = new List<HandlerSelectItem>();

            // 新处理器优先显示
            foreach (var handler in _newHandlers)
            {
                var item = await CreateHandlerItemAsync(handler, isNew: true);
                items.Add(item);
            }

            // 已有处理器
            foreach (var handler in _existingHandlers)
            {
                var item = await CreateHandlerItemAsync(handler, isNew: false);
                items.Add(item);
            }

            HandlerList.ItemsSource = items;
        }

        private async Task<HandlerSelectItem> CreateHandlerItemAsync(ProtocolHandlerOption handler, bool isNew)
        {
            var item = new HandlerSelectItem
            {
                Handler = handler,
                DisplayName = handler.PackageName,
                Description = handler.Description,
                IsNew = isNew
            };

            // 尝试获取插件的显示名称和图标
            try
            {
                var pluginManager = PMPlugin.Instance;
                if (pluginManager != null)
                {
                    // 获取插件实例来获取本地化名称
                    var plugin = pluginManager.GetPlugin(handler.PackageName);
                    if (plugin != null)
                    {
                        var localizedName = plugin.Metadata.GetLocalizedName(PCSysConfig.Instance.langCode);
                        if (!string.IsNullOrEmpty(localizedName))
                        {
                            item.DisplayName = localizedName;
                        }
                    }

                    // 获取图标路径
                    var iconPath = await pluginManager.GetPluginIcon(handler.PackageName);
                    if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    {
                        item.IconPath = iconPath;
                    }
                }
            }
            catch
            {
                // 忽略错误，使用默认值
            }

            return item;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayOpenAnimation();
        }

        private void PlayOpenAnimation()
        {
            var storyboard = new Storyboard();

            var elasticEase = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 1,
                Springiness = 6
            };

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            MainBorder.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeIn, MainBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            var scaleXAnim = new DoubleAnimation
            {
                From = 0.85,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleXAnim, MainBorder);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.85,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleYAnim, MainBorder);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);

            var slideAnim = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(slideAnim, MainBorder);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideAnim);

            storyboard.Begin();
        }

        private void PlayCloseAnimation(Action onCompleted)
        {
            var storyboard = new Storyboard();
            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseIn };

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(fadeOut, MainBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            var scaleXAnim = new DoubleAnimation
            {
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleXAnim, MainBorder);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(scaleYAnim, MainBorder);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);

            var slideAnim = new DoubleAnimation
            {
                To = 15,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = cubicEase
            };
            Storyboard.SetTarget(slideAnim, MainBorder);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
            storyboard.Children.Add(slideAnim);

            storyboard.Completed += (s, e) => onCompleted?.Invoke();
            storyboard.Begin();
        }

        private void HandlerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HandlerSelectItem item)
            {
                SelectedHandler = item.Handler;
                SetAsDefault = SetDefaultCheckBox.IsChecked == true;

                PlayCloseAnimation(() =>
                {
                    DialogResult = true;
                    Close();
                });
            }
        }

        private void HandlerItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = (Brush)FindResource("Background3Brush");
            }
        }

        private void HandlerItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is HandlerSelectItem item)
            {
                // 如果是新项，保持背景色
                if (!item.IsNew)
                {
                    border.Background = Brushes.Transparent;
                }
            }
        }

        /// <summary>
        /// 显示处理器选择对话框
        /// </summary>
        public static async Task<(ProtocolHandlerOption? handler, bool setAsDefault)> ShowAsync(
            List<ProtocolHandlerOption> newHandlers,
            List<ProtocolHandlerOption> existingHandlers,
            RunRequest request,
            string protocolKey,
            string? title = null,
            string? subtitle = null,
            Window? owner = null)
        {
            ProtocolHandlerOption? selectedHandler = null;
            bool setAsDefault = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new PCOHandlerSelectDialog(
                    newHandlers,
                    existingHandlers,
                    request,
                    protocolKey,
                    title,
                    subtitle);

                if (owner != null)
                {
                    dialog.Owner = owner;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                if (dialog.ShowDialog() == true)
                {
                    selectedHandler = dialog.SelectedHandler;
                    setAsDefault = dialog.SetAsDefault;
                }
            });

            return (selectedHandler, setAsDefault);
        }
    }
}
