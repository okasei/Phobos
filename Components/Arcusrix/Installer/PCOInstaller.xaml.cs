using Microsoft.Win32;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Components.Arcusrix.Installer.Helpers;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Installer
{
    /// <summary>
    /// 安装器模式
    /// </summary>
    public enum InstallerMode
    {
        /// <summary>
        /// 用户主动打开模式 - 显示文件选择界面，支持返回
        /// </summary>
        UserOpen,

        /// <summary>
        /// 调用模式 - 直接显示插件详情，不支持返回
        /// </summary>
        Invoked
    }

    /// <summary>
    /// 插件安装上下文信息
    /// </summary>
    public class PluginInstallContext
    {
        public string PluginPath { get; set; } = string.Empty;
        public PluginMetadata? Metadata { get; set; }
        public PluginAssemblyLoadContext? LoadContext { get; set; }
        public List<PluginDependency> MissingRequired { get; set; } = new();
        public List<PluginDependency> MissingOptional { get; set; } = new();
        public List<PluginDependency> Satisfied { get; set; } = new();
        public bool CanInstall => MissingRequired.Count == 0;
    }

    /// <summary>
    /// 多插件信息（用于显示）
    /// </summary>
    public class MultiPluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    /// <summary>
    /// PCOInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class PCOInstaller : UserControl
    {
        private PCPluginInstaller? _hostPlugin;
        private InstallerMode _mode = InstallerMode.UserOpen;
        private PluginInstallContext? _installContext;
        private string _currentPluginPath = string.Empty;
        private List<MultiPluginInfo> _multiPlugins = new();

        /// <summary>
        /// 安装完成事件
        /// </summary>
        public event EventHandler<InstallCompletedEventArgs>? InstallCompleted;

        /// <summary>
        /// 退出请求事件
        /// </summary>
        public event EventHandler? ExitRequested;

        public PCOInstaller()
        {
            InitializeComponent();

            // 注册本地化
            InstallerLocalization.RegisterAll();

            // 应用本地化文本
            ApplyLocalization();
        }

        /// <summary>
        /// 应用本地化文本
        /// </summary>
        private void ApplyLocalization()
        {
            // 文件选择视图
            TitleText.Text = InstallerLocalization.Get(InstallerLocalization.Title);
            DropHintText.Text = InstallerLocalization.Get(InstallerLocalization.FileSelect_DropHint);
            OrText.Text = InstallerLocalization.Get(InstallerLocalization.FileSelect_Or);
            BrowseButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Browse);
            SupportedFilesText.Text = InstallerLocalization.Get(InstallerLocalization.FileSelect_SupportedFiles);
            FileSelectStatus.Text = InstallerLocalization.Get(InstallerLocalization.FileSelect_Ready);

            // 多插件视图
            MultiPluginTitleText.Text = InstallerLocalization.Get(InstallerLocalization.MultiPlugin_Title);
            MultiPluginBackButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Back);
            MultiPluginBackButton2.Content = InstallerLocalization.Get(InstallerLocalization.Button_PreviousStep);
            InstallAllButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_InstallAll);

            // 插件详情视图
            BackButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Back);
            IntroductionLabel.Text = InstallerLocalization.Get(InstallerLocalization.Detail_Introduction);
            DependenciesLabel.Text = InstallerLocalization.Get(InstallerLocalization.Detail_Dependencies);
            MissingRequiredLabel.Text = InstallerLocalization.Get(InstallerLocalization.Detail_MissingRequired);
            MissingRequiredHintText.Text = InstallerLocalization.Get(InstallerLocalization.Detail_MissingRequiredHint);
            MissingOptionalLabel.Text = InstallerLocalization.Get(InstallerLocalization.Detail_MissingOptional);
            MissingOptionalHintText.Text = InstallerLocalization.Get(InstallerLocalization.Detail_MissingOptionalHint);
            SatisfiedDependenciesLabel.Text = InstallerLocalization.Get(InstallerLocalization.Detail_SatisfiedDependencies);
            DetailBackButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_PreviousStep);
            InstallButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Install);
        }

        /// <summary>
        /// 设置宿主插件
        /// </summary>
        public void SetHostPlugin(PCPluginInstaller host)
        {
            _hostPlugin = host;
        }

        /// <summary>
        /// 设置安装器模式
        /// </summary>
        public void SetMode(InstallerMode mode)
        {
            _mode = mode;

            // 根据模式调整 UI
            if (mode == InstallerMode.Invoked)
            {
                // 调用模式：隐藏返回按钮
                BackButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 用户打开模式：显示返回按钮（在详情页时）
                BackButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 通过 URI 加载插件（调用模式）
        /// </summary>
        /// <param name="pluginPath">插件 DLL 路径</param>
        public async Task LoadFromUri(string pluginPath)
        {
            SetMode(InstallerMode.Invoked);
            await LoadPlugin(pluginPath);
        }

        /// <summary>
        /// 浏览按钮点击
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = InstallerLocalization.Get(InstallerLocalization.FileDialog_Filter),
                Title = InstallerLocalization.Get(InstallerLocalization.FileDialog_Title)
            };

            if (dlg.ShowDialog() == true)
            {
                _ = LoadPlugin(dlg.FileName);
            }
        }

        /// <summary>
        /// 拖放区域 - 拖入
        /// </summary>
        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && files[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = DragDropEffects.Copy;
                    DropZone.BorderBrush = (Brush)FindResource("PrimaryBrush");
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖放区域 - 拖出
        /// </summary>
        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (Brush)FindResource("BorderBrush");
        }

        /// <summary>
        /// 拖放区域 - 放下
        /// </summary>
        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (Brush)FindResource("BorderBrush");

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && files[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    _ = LoadPlugin(files[0]);
                }
                else
                {
                    SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_DropSingleDll));
                }
            }
        }

        /// <summary>
        /// 加载插件信息
        /// </summary>
        private async Task LoadPlugin(string pluginPath)
        {
            if (!File.Exists(pluginPath))
            {
                SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_FileNotExist));
                return;
            }

            ShowLoading(InstallerLocalization.Get(InstallerLocalization.Status_ReadingPlugin));
            _currentPluginPath = pluginPath;

            try
            {
                // 创建临时加载上下文
                var tempContext = new PluginAssemblyLoadContext(pluginPath);
                var assembly = tempContext.LoadFromAssemblyPath(pluginPath);

                // 获取所有实现 IPhobosPlugin 的类型
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPhobosPlugin).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                if (pluginTypes.Count == 0)
                {
                    tempContext.Unload();
                    HideLoading();
                    SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_InvalidPlugin));
                    return;
                }

                // 如果有多个插件，显示多插件选择视图
                if (pluginTypes.Count > 1)
                {
                    var lang = LocalizationManager.Instance.CurrentLanguage;
                    _multiPlugins.Clear();

                    foreach (var type in pluginTypes)
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(type) as IPhobosPlugin;
                            if (instance != null)
                            {
                                _multiPlugins.Add(new MultiPluginInfo
                                {
                                    Name = instance.Metadata.GetLocalizedName(lang),
                                    PackageName = instance.Metadata.PackageName,
                                    Version = instance.Metadata.Version,
                                    Manufacturer = instance.Metadata.Manufacturer,
                                    Icon = instance.Metadata.Icon
                                });
                            }
                        }
                        catch
                        {
                            // 忽略无法实例化的类型
                        }
                    }

                    tempContext.Unload();
                    HideLoading();

                    if (_multiPlugins.Count > 1)
                    {
                        // 显示多插件选择视图
                        MultiPluginSubtitle.Text = InstallerLocalization.GetFormat(InstallerLocalization.MultiPlugin_Subtitle, _multiPlugins.Count);
                        MultiPluginList.ItemsSource = _multiPlugins;
                        SwitchToMultiPluginView();
                        return;
                    }
                    else if (_multiPlugins.Count == 1)
                    {
                        // 只有一个有效插件，直接加载详情
                        await LoadSinglePlugin(pluginPath, _multiPlugins[0].PackageName);
                        return;
                    }
                    else
                    {
                        SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_NoPluginTypes));
                        return;
                    }
                }

                // 单插件情况
                var pluginType = pluginTypes.First();
                var pluginInstance = Activator.CreateInstance(pluginType) as IPhobosPlugin;
                if (pluginInstance == null)
                {
                    tempContext.Unload();
                    HideLoading();
                    SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_CannotCreateInstance));
                    return;
                }

                var metadata = pluginInstance.Metadata;

                // 检查依赖
                var installed = await PMPlugin.Instance.GetInstalledPlugins();

                var missingRequired = metadata.Dependencies
                    .Where(d => !d.IsOptional && !installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var missingOptional = metadata.Dependencies
                    .Where(d => d.IsOptional && !installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var satisfied = metadata.Dependencies
                    .Where(d => installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // 保存安装上下文
                _installContext = new PluginInstallContext
                {
                    PluginPath = pluginPath,
                    Metadata = metadata,
                    LoadContext = tempContext,
                    MissingRequired = missingRequired,
                    MissingOptional = missingOptional,
                    Satisfied = satisfied
                };

                // 更新 UI
                UpdatePluginDetailView();

                // 切换到详情视图
                SwitchToDetailView();

                HideLoading();
            }
            catch (Exception ex)
            {
                HideLoading();
                SetFileSelectStatus(InstallerLocalization.GetFormat(InstallerLocalization.FileSelect_LoadFailed, ex.Message));
            }
        }

        /// <summary>
        /// 加载指定包名的单个插件（用于多插件 DLL）
        /// </summary>
        private async Task LoadSinglePlugin(string pluginPath, string packageName)
        {
            ShowLoading(InstallerLocalization.Get(InstallerLocalization.Status_ReadingPlugin));

            try
            {
                var tempContext = new PluginAssemblyLoadContext(pluginPath);
                var assembly = tempContext.LoadFromAssemblyPath(pluginPath);

                // 查找指定包名的插件
                IPhobosPlugin? pluginInstance = null;
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPhobosPlugin).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                foreach (var type in pluginTypes)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(type) as IPhobosPlugin;
                        if (instance?.Metadata.PackageName == packageName)
                        {
                            pluginInstance = instance;
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (pluginInstance == null)
                {
                    tempContext.Unload();
                    HideLoading();
                    SetFileSelectStatus(InstallerLocalization.GetFormat(InstallerLocalization.FileSelect_PluginNotFound, packageName));
                    return;
                }

                var metadata = pluginInstance.Metadata;

                // 检查依赖
                var installed = await PMPlugin.Instance.GetInstalledPlugins();

                var missingRequired = metadata.Dependencies
                    .Where(d => !d.IsOptional && !installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var missingOptional = metadata.Dependencies
                    .Where(d => d.IsOptional && !installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var satisfied = metadata.Dependencies
                    .Where(d => installed.Any(p =>
                        string.Equals(p.PackageName, d.PackageName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // 保存安装上下文
                _installContext = new PluginInstallContext
                {
                    PluginPath = pluginPath,
                    Metadata = metadata,
                    LoadContext = tempContext,
                    MissingRequired = missingRequired,
                    MissingOptional = missingOptional,
                    Satisfied = satisfied
                };

                // 更新 UI
                UpdatePluginDetailView();

                // 切换到详情视图
                SwitchToDetailView();

                HideLoading();
            }
            catch (Exception ex)
            {
                HideLoading();
                SetFileSelectStatus(InstallerLocalization.GetFormat(InstallerLocalization.FileSelect_LoadFailed, ex.Message));
            }
        }

        /// <summary>
        /// 更新插件详情视图
        /// </summary>
        private void UpdatePluginDetailView()
        {
            if (_installContext?.Metadata == null) return;

            var metadata = _installContext.Metadata;
            var lang = LocalizationManager.Instance.CurrentLanguage;

            // 基本信息
            PluginNameText.Text = metadata.GetLocalizedName(lang);
            PluginVersionText.Text = $"v{metadata.Version}";
            PluginManufacturerText.Text = metadata.Manufacturer;
            PluginPackageNameText.Text = metadata.PackageName;
            PluginDescriptionText.Text = metadata.GetLocalizedDescription(lang);

            // 加载图标
            LoadPluginIcon(metadata, _installContext.PluginPath);

            // 更新依赖信息
            UpdateDependencyView();

            // 更新按钮状态
            InstallButton.IsEnabled = _installContext.CanInstall;
            InstallButton.Content = _installContext.CanInstall
                ? InstallerLocalization.Get(InstallerLocalization.Button_Install)
                : InstallerLocalization.Get(InstallerLocalization.Detail_CannotInstall);

            if (!_installContext.CanInstall)
            {
                SetInstallStatus(InstallerLocalization.Get(InstallerLocalization.Detail_HasMissingRequired));
            }
            else if (_installContext.MissingOptional.Count > 0)
            {
                SetInstallStatus(InstallerLocalization.Get(InstallerLocalization.Detail_HasMissingOptional));
            }
            else
            {
                SetInstallStatus("");
            }
        }

        /// <summary>
        /// 加载插件图标
        /// </summary>
        private void LoadPluginIcon(PluginMetadata metadata, string pluginPath)
        {
            try
            {
                if (string.IsNullOrEmpty(metadata.Icon)) return;

                var pluginDir = Path.GetDirectoryName(pluginPath);
                if (string.IsNullOrEmpty(pluginDir)) return;

                var iconPath = Path.Combine(pluginDir, metadata.Icon);
                if (File.Exists(iconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PluginIcon.Source = bitmap;
                }
            }
            catch
            {
                // 图标加载失败，保持默认
            }
        }

        /// <summary>
        /// 更新依赖视图
        /// </summary>
        private void UpdateDependencyView()
        {
            if (_installContext == null) return;

            var hasDependencies = _installContext.MissingRequired.Count > 0 ||
                                  _installContext.MissingOptional.Count > 0 ||
                                  _installContext.Satisfied.Count > 0;

            DependencySection.Visibility = hasDependencies ? Visibility.Visible : Visibility.Collapsed;

            // 必需依赖
            if (_installContext.MissingRequired.Count > 0)
            {
                MissingRequiredPanel.Visibility = Visibility.Visible;
                MissingRequiredList.ItemsSource = _installContext.MissingRequired;
            }
            else
            {
                MissingRequiredPanel.Visibility = Visibility.Collapsed;
            }

            // 可选依赖
            if (_installContext.MissingOptional.Count > 0)
            {
                MissingOptionalPanel.Visibility = Visibility.Visible;
                MissingOptionalList.ItemsSource = _installContext.MissingOptional;
            }
            else
            {
                MissingOptionalPanel.Visibility = Visibility.Collapsed;
            }

            // 已满足的依赖
            if (_installContext.Satisfied.Count > 0)
            {
                SatisfiedDependencyPanel.Visibility = Visibility.Visible;
                SatisfiedDependencyList.ItemsSource = _installContext.Satisfied;
            }
            else
            {
                SatisfiedDependencyPanel.Visibility = Visibility.Collapsed;
            }
        }

        #region 视图切换动画

        /// <summary>
        /// 当前显示的视图
        /// </summary>
        private enum CurrentView { FileSelect, MultiPlugin, Detail }
        private CurrentView _currentView = CurrentView.FileSelect;

        /// <summary>
        /// 动画时长
        /// </summary>
        private static readonly Duration AnimationDuration = new Duration(TimeSpan.FromMilliseconds(250));

        /// <summary>
        /// 缓动函数
        /// </summary>
        private static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

        /// <summary>
        /// 切换到详情视图
        /// </summary>
        private void SwitchToDetailView()
        {
            // 确保文件选择视图被隐藏
            if (_currentView != CurrentView.FileSelect)
            {
                FileSelectView.Visibility = Visibility.Collapsed;
                FileSelectView.Opacity = 0;
            }

            var fromRight = _currentView == CurrentView.FileSelect || _currentView == CurrentView.MultiPlugin;
            AnimateViewTransition(PluginDetailView, PluginDetailViewTransform, true, fromRight);

            if (_currentView == CurrentView.FileSelect)
            {
                AnimateViewTransition(FileSelectView, FileSelectViewTransform, false, false);
            }
            else if (_currentView == CurrentView.MultiPlugin)
            {
                AnimateViewTransition(MultiPluginSelectView, MultiPluginSelectViewTransform, false, false);
            }

            _currentView = CurrentView.Detail;

            // 根据模式和是否有多插件来显示/隐藏返回按钮
            BackButton.Visibility = (_mode == InstallerMode.UserOpen || _multiPlugins.Count > 1)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// 切换到多插件选择视图
        /// </summary>
        private void SwitchToMultiPluginView()
        {
            // 确保详情视图被隐藏（可能从未显示过，但需要确保 Z-order 不影响）
            PluginDetailView.Visibility = Visibility.Collapsed;
            PluginDetailView.Opacity = 0;

            var fromRight = _currentView == CurrentView.FileSelect;
            AnimateViewTransition(MultiPluginSelectView, MultiPluginSelectViewTransform, true, fromRight);

            if (_currentView == CurrentView.FileSelect)
            {
                AnimateViewTransition(FileSelectView, FileSelectViewTransform, false, false);
            }
            else if (_currentView == CurrentView.Detail)
            {
                AnimateViewTransition(PluginDetailView, PluginDetailViewTransform, false, true);
            }

            _currentView = CurrentView.MultiPlugin;

            // 根据模式显示/隐藏返回按钮
            MultiPluginBackButton.Visibility = _mode == InstallerMode.UserOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 切换到文件选择视图
        /// </summary>
        private void SwitchToFileSelectView()
        {
            // 清理当前加载上下文
            CleanupInstallContext();

            // 确保详情视图被隐藏
            PluginDetailView.Visibility = Visibility.Collapsed;
            PluginDetailView.Opacity = 0;

            AnimateViewTransition(FileSelectView, FileSelectViewTransform, true, false);

            if (_currentView == CurrentView.MultiPlugin)
            {
                AnimateViewTransition(MultiPluginSelectView, MultiPluginSelectViewTransform, false, true);
            }
            else if (_currentView == CurrentView.Detail)
            {
                AnimateViewTransition(PluginDetailView, PluginDetailViewTransform, false, true);
            }

            _currentView = CurrentView.FileSelect;
            SetFileSelectStatus(InstallerLocalization.Get(InstallerLocalization.FileSelect_Ready));
        }

        /// <summary>
        /// 执行视图切换动画
        /// </summary>
        /// <param name="view">要动画的视图</param>
        /// <param name="transform">视图的 TranslateTransform</param>
        /// <param name="show">true=显示，false=隐藏</param>
        /// <param name="fromRight">true=从右侧进入/退出到左侧，false=从左侧进入/退出到右侧</param>
        private void AnimateViewTransition(Grid view, TranslateTransform transform, bool show, bool fromRight)
        {
            var slideDistance = 30.0; // 滑动距离

            if (show)
            {
                // 显示动画：从偏移位置滑入到原位，同时淡入
                view.Visibility = Visibility.Visible;

                // 设置初始位置
                transform.X = fromRight ? slideDistance : -slideDistance;

                // X 动画
                var xAnimation = new DoubleAnimation
                {
                    From = transform.X,
                    To = 0,
                    Duration = AnimationDuration,
                    EasingFunction = EaseOut
                };
                transform.BeginAnimation(TranslateTransform.XProperty, xAnimation);

                // 透明度动画
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = AnimationDuration,
                    EasingFunction = EaseOut
                };
                view.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            }
            else
            {
                // 隐藏动画：从原位滑出到偏移位置，同时淡出
                var targetX = fromRight ? slideDistance : -slideDistance;

                // X 动画
                var xAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = targetX,
                    Duration = AnimationDuration,
                    EasingFunction = EaseOut
                };
                transform.BeginAnimation(TranslateTransform.XProperty, xAnimation);

                // 透明度动画
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = AnimationDuration,
                    EasingFunction = EaseOut
                };
                opacityAnimation.Completed += (s, e) =>
                {
                    view.Visibility = Visibility.Collapsed;
                };
                view.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            }
        }

        #endregion

        /// <summary>
        /// 清理安装上下文
        /// </summary>
        private void CleanupInstallContext()
        {
            if (_installContext?.LoadContext != null)
            {
                try
                {
                    _installContext.LoadContext.Unload();
                }
                catch { }
            }
            _installContext = null;

            // 清理 UI
            PluginIcon.Source = null;
            MissingRequiredList.ItemsSource = null;
            MissingOptionalList.ItemsSource = null;
            SatisfiedDependencyList.ItemsSource = null;
        }

        /// <summary>
        /// 返回按钮点击
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果有多个插件，返回到多插件选择视图
            if (_multiPlugins.Count > 1)
            {
                CleanupInstallContext();
                SwitchToMultiPluginView();
            }
            else if (_mode == InstallerMode.UserOpen)
            {
                SwitchToFileSelectView();
            }
        }

        /// <summary>
        /// 多插件视图返回按钮点击
        /// </summary>
        private void MultiPluginBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == InstallerMode.UserOpen)
            {
                _multiPlugins.Clear();
                SwitchToFileSelectView();
            }
        }

        /// <summary>
        /// 插件列表项点击
        /// </summary>
        private void PluginItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is MultiPluginInfo pluginInfo)
            {
                _ = LoadSinglePlugin(_currentPluginPath, pluginInfo.PackageName);
            }
        }

        /// <summary>
        /// 全部安装按钮点击
        /// </summary>
        private async void InstallAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_multiPlugins.Count == 0 || string.IsNullOrEmpty(_currentPluginPath)) return;

            InstallAllButton.IsEnabled = false;
            MultiPluginBackButton2.IsEnabled = false;
            ShowLoading(InstallerLocalization.GetFormat(InstallerLocalization.Status_InstallingMultiple, _multiPlugins.Count));

            try
            {
                var result = await PMPlugin.Instance.Install(_currentPluginPath);

                HideLoading();

                if (result.Success)
                {
                    MultiPluginStatus.Text = result.Message;
                    InstallAllButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Installed);

                    // 触发安装完成事件
                    InstallCompleted?.Invoke(this, new InstallCompletedEventArgs
                    {
                        Success = true,
                        Message = result.Message,
                        PackageName = string.Join(", ", _multiPlugins.Select(p => p.PackageName))
                    });

                    // 延迟后返回
                    await Task.Delay(1500);

                    if (_mode == InstallerMode.UserOpen)
                    {
                        _multiPlugins.Clear();
                        SwitchToFileSelectView();
                    }
                    else
                    {
                        ExitRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    MultiPluginStatus.Text = InstallerLocalization.GetFormat(InstallerLocalization.Status_InstallFailed, result.Message);
                    InstallAllButton.IsEnabled = true;
                    InstallAllButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Retry);
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                MultiPluginStatus.Text = InstallerLocalization.GetFormat(InstallerLocalization.Status_InstallError, ex.Message);
                InstallAllButton.IsEnabled = true;
                InstallAllButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Retry);
            }
            finally
            {
                MultiPluginBackButton2.IsEnabled = true;
            }
        }

        /// <summary>
        /// 插件详情视图的上一步按钮点击
        /// </summary>
        private void DetailBackButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果有多个插件，返回到多插件选择视图
            if (_multiPlugins.Count > 1)
            {
                CleanupInstallContext();
                SwitchToMultiPluginView();
            }
            else if (_mode == InstallerMode.UserOpen)
            {
                // 用户打开模式：返回到文件选择视图
                SwitchToFileSelectView();
            }
            else
            {
                // 调用模式且单插件：触发退出
                CleanupInstallContext();
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 安装按钮点击
        /// </summary>
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_installContext == null || !_installContext.CanInstall) return;

            // 如果有可选依赖缺失，先确认
            if (_installContext.MissingOptional.Count > 0)
            {
                var deps = string.Join(", ", _installContext.MissingOptional.Select(d => d.PackageName));
                var result = MessageBox.Show(
                    InstallerLocalization.GetFormat(InstallerLocalization.Dialog_OptionalDepsMessage, deps),
                    InstallerLocalization.Get(InstallerLocalization.Dialog_OptionalDepsTitle),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            await PerformInstall();
        }

        /// <summary>
        /// 执行安装
        /// </summary>
        private async Task PerformInstall()
        {
            if (_installContext == null) return;

            InstallButton.IsEnabled = false;
            DetailBackButton.IsEnabled = false;
            ShowLoading(InstallerLocalization.Get(InstallerLocalization.Status_Installing));

            try
            {
                // 卸载临时加载上下文（安装器会重新加载）
                var path = _installContext.PluginPath;
                _installContext.LoadContext?.Unload();
                _installContext.LoadContext = null;

                // 调用插件管理器安装
                var result = await PMPlugin.Instance.Install(path);

                HideLoading();

                if (result.Success)
                {
                    SetInstallStatus(InstallerLocalization.Get(InstallerLocalization.Status_InstallSuccess));
                    InstallButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Installed);

                    InstallCompleted?.Invoke(this, new InstallCompletedEventArgs
                    {
                        Success = true,
                        Message = result.Message,
                        PackageName = _installContext.Metadata?.PackageName ?? string.Empty
                    });
                    if (_hostPlugin != null)
                        await _hostPlugin?.TriggerEvent("App", "Installed", _installContext.Metadata?.PackageName ?? string.Empty, _installContext.Metadata?.Name ?? string.Empty);
                    // 延迟后自动关闭或返回
                    await Task.Delay(1500);

                    if (_mode == InstallerMode.UserOpen)
                    {
                        SwitchToFileSelectView();
                    }
                    else
                    {
                        ExitRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    SetInstallStatus(InstallerLocalization.GetFormat(InstallerLocalization.Status_InstallFailed, result.Message));
                    InstallButton.IsEnabled = true;
                    InstallButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Retry);
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                SetInstallStatus(InstallerLocalization.GetFormat(InstallerLocalization.Status_InstallError, ex.Message));
                InstallButton.IsEnabled = true;
                InstallButton.Content = InstallerLocalization.Get(InstallerLocalization.Button_Retry);
            }
            finally
            {
                DetailBackButton.IsEnabled = true;
            }
        }

        #region UI Helper Methods

        private void SetFileSelectStatus(string message)
        {
            FileSelectStatus.Text = message;
        }

        private void SetInstallStatus(string message)
        {
            InstallStatus.Text = message;
        }

        private void ShowLoading(string message)
        {
            LoadingText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
        }

        private void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion
    }

    /// <summary>
    /// 安装完成事件参数
    /// </summary>
    public class InstallCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
    }
}