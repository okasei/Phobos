using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Phobos.Shared.Interface;

namespace Phobos.UI.PIPlugin
{
    /*
     * 
    // 使用示例
    <local:PluginContainer x:Name="PluginHost" />

    // 加载插件 UI
    await PluginHost.LoadPlugin("com.phobos.calculator");
    // 或
    await PluginHost.LoadPlugin(pluginInstance);

    // 卸载当前插件
    await PluginHost.UnloadCurrentPlugin();

    // 清空容器
    await PluginHost.Clear();
     */
    /// <summary>
    /// 插件容器控件 - 用于承载插件的 UI
    /// </summary>
    public class PluginContainer : ContentControl
    {
        #region 依赖属性

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(PluginContainer),
                new PropertyMetadata(false));

        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(PluginContainer),
                new PropertyMetadata("Loading..."));

        public static readonly DependencyProperty CurrentPluginProperty =
            DependencyProperty.Register(nameof(CurrentPlugin), typeof(IPhobosPlugin), typeof(PluginContainer),
                new PropertyMetadata(null));

        public static readonly DependencyProperty EnableAnimationProperty =
            DependencyProperty.Register(nameof(EnableAnimation), typeof(bool), typeof(PluginContainer),
                new PropertyMetadata(true));

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(PluginContainer),
                new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// 加载提示文本
        /// </summary>
        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        /// <summary>
        /// 当前加载的插件
        /// </summary>
        public IPhobosPlugin? CurrentPlugin
        {
            get => (IPhobosPlugin?)GetValue(CurrentPluginProperty);
            private set => SetValue(CurrentPluginProperty, value);
        }

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool EnableAnimation
        {
            get => (bool)GetValue(EnableAnimationProperty);
            set => SetValue(EnableAnimationProperty, value);
        }

        /// <summary>
        /// 动画持续时间
        /// </summary>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 插件加载完成事件
        /// </summary>
        public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

        /// <summary>
        /// 插件卸载完成事件
        /// </summary>
        public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

        /// <summary>
        /// 插件加载失败事件
        /// </summary>
        public event EventHandler<PluginLoadFailedEventArgs>? PluginLoadFailed;

        #endregion

        #region 私有字段

        private Grid? _rootGrid;
        private ContentPresenter? _contentPresenter;
        private Border? _loadingOverlay;
        private TextBlock? _loadingTextBlock;

        #endregion

        #region 构造函数

        static PluginContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PluginContainer),
                new FrameworkPropertyMetadata(typeof(PluginContainer)));
        }

        public PluginContainer()
        {
            InitializeVisualTree();
        }

        private void InitializeVisualTree()
        {
            _rootGrid = new Grid();

            // 内容区域
            _contentPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_contentPresenter);

            // 加载遮罩层
            _loadingOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                Visibility = Visibility.Collapsed
            };

            var loadingStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 加载动画（简单的旋转圆环）
            var loadingRing = new Border
            {
                Width = 40,
                Height = 40,
                BorderThickness = new Thickness(3),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 144, 255)),
                CornerRadius = new CornerRadius(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform()
            };

            // 旋转动画
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever
            };
            loadingRing.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

            _loadingTextBlock = new TextBlock
            {
                Text = LoadingText,
                Foreground = Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            loadingStack.Children.Add(loadingRing);
            loadingStack.Children.Add(_loadingTextBlock);
            _loadingOverlay.Child = loadingStack;

            _rootGrid.Children.Add(_loadingOverlay);

            Content = _rootGrid;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 加载插件 UI
        /// </summary>
        /// <param name="plugin">插件实例</param>
        /// <param name="showLoading">是否显示加载动画</param>
        public async Task<bool> LoadPlugin(IPhobosPlugin plugin, bool showLoading = true)
        {
            if (plugin == null)
            {
                OnPluginLoadFailed(null, new ArgumentNullException(nameof(plugin)));
                return false;
            }

            try
            {
                if (showLoading)
                {
                    ShowLoading($"Loading {plugin.Metadata.Name}...");
                }

                // 先卸载当前插件
                if (CurrentPlugin != null)
                {
                    await UnloadCurrentPlugin(false);
                }

                // 获取插件 UI
                var pluginUI = plugin.ContentArea;
                if (pluginUI == null)
                {
                    HideLoading();
                    OnPluginLoadFailed(plugin, new InvalidOperationException("Plugin has no ContentArea"));
                    return false;
                }

                // 设置内容
                await Dispatcher.InvokeAsync(() =>
                {
                    if (EnableAnimation)
                    {
                        pluginUI.Opacity = 0;
                    }

                    _contentPresenter!.Content = pluginUI;
                    CurrentPlugin = plugin;

                    if (EnableAnimation)
                    {
                        // 淡入动画
                        var fadeIn = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = AnimationDuration,
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        pluginUI.BeginAnimation(OpacityProperty, fadeIn);
                    }
                });

                HideLoading();
                OnPluginLoaded(plugin);
                return true;
            }
            catch (Exception ex)
            {
                HideLoading();
                OnPluginLoadFailed(plugin, ex);
                return false;
            }
        }

        /// <summary>
        /// 通过包名加载插件 UI
        /// </summary>
        /// <param name="packageName">插件包名</param>
        /// <param name="showLoading">是否显示加载动画</param>
        public async Task<bool> LoadPlugin(string packageName, bool showLoading = true)
        {
            var plugin = Manager.Plugin.PMPlugin.Instance.GetPlugin(packageName);
            if (plugin == null)
            {
                OnPluginLoadFailed(null, new InvalidOperationException($"Plugin not found: {packageName}"));
                return false;
            }

            return await LoadPlugin(plugin, showLoading);
        }

        /// <summary>
        /// 卸载当前插件 UI
        /// </summary>
        /// <param name="notifyPlugin">是否通知插件（调用 OnClosing）</param>
        public async Task UnloadCurrentPlugin(bool notifyPlugin = true)
        {
            if (CurrentPlugin == null)
                return;

            var plugin = CurrentPlugin;

            try
            {
                // 通知插件关闭
                if (notifyPlugin)
                {
                    await plugin.OnClosing();
                }

                await Dispatcher.InvokeAsync(async () =>
                {
                    if (EnableAnimation && _contentPresenter?.Content is FrameworkElement element)
                    {
                        // 淡出动画
                        var fadeOut = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = AnimationDuration,
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                        };

                        var tcs = new TaskCompletionSource<bool>();
                        fadeOut.Completed += (s, e) => tcs.SetResult(true);
                        element.BeginAnimation(OpacityProperty, fadeOut);
                        await tcs.Task;
                    }

                    _contentPresenter!.Content = null;
                    CurrentPlugin = null;
                });

                OnPluginUnloaded(plugin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unloading plugin: {ex.Message}");
                // 强制清除
                _contentPresenter!.Content = null;
                CurrentPlugin = null;
            }
        }

        /// <summary>
        /// 清空容器
        /// </summary>
        public async Task Clear()
        {
            await UnloadCurrentPlugin(true);
        }

        /// <summary>
        /// 显示加载动画
        /// </summary>
        public void ShowLoading(string? text = null)
        {
            if (_loadingOverlay == null) return;

            if (text != null && _loadingTextBlock != null)
            {
                _loadingTextBlock.Text = text;
            }

            IsLoading = true;
            _loadingOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏加载动画
        /// </summary>
        public void HideLoading()
        {
            if (_loadingOverlay == null) return;

            IsLoading = false;
            _loadingOverlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 刷新当前插件 UI
        /// </summary>
        public async Task RefreshCurrentPlugin()
        {
            if (CurrentPlugin == null) return;

            var plugin = CurrentPlugin;
            await UnloadCurrentPlugin(false);
            await LoadPlugin(plugin, false);
        }

        /// <summary>
        /// 检查是否已加载指定插件
        /// </summary>
        public bool IsPluginLoaded(string packageName)
        {
            return CurrentPlugin?.Metadata.PackageName == packageName;
        }

        #endregion

        #region 事件触发

        private void OnPluginLoaded(IPhobosPlugin plugin)
        {
            PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(plugin));
        }

        private void OnPluginUnloaded(IPhobosPlugin plugin)
        {
            PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(plugin));
        }

        private void OnPluginLoadFailed(IPhobosPlugin? plugin, Exception exception)
        {
            PluginLoadFailed?.Invoke(this, new PluginLoadFailedEventArgs(plugin, exception));
        }

        #endregion
    }

    #region 事件参数

    public class PluginLoadedEventArgs : EventArgs
    {
        public IPhobosPlugin Plugin { get; }

        public PluginLoadedEventArgs(IPhobosPlugin plugin)
        {
            Plugin = plugin;
        }
    }

    public class PluginUnloadedEventArgs : EventArgs
    {
        public IPhobosPlugin Plugin { get; }

        public PluginUnloadedEventArgs(IPhobosPlugin plugin)
        {
            Plugin = plugin;
        }
    }

    public class PluginLoadFailedEventArgs : EventArgs
    {
        public IPhobosPlugin? Plugin { get; }
        public Exception Exception { get; }

        public PluginLoadFailedEventArgs(IPhobosPlugin? plugin, Exception exception)
        {
            Plugin = plugin;
            Exception = exception;
        }
    }

    #endregion
}