using Microsoft.Win32;
using Phobos.Class.Plugin.BuiltIn;
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
    /// PCOInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class PCOInstaller : UserControl
    {
        private PCPluginInstaller? _hostPlugin;
        private InstallerMode _mode = InstallerMode.UserOpen;
        private PluginInstallContext? _installContext;

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
                Filter = "插件文件 (*.dll)|*.dll|所有文件 (*.*)|*.*",
                Title = "选择插件文件"
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
                    SetFileSelectStatus("请拖放单个 .dll 文件");
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
                SetFileSelectStatus("文件不存在");
                return;
            }

            ShowLoading("正在读取插件信息...");

            try
            {
                // 创建临时加载上下文
                var tempContext = new PluginAssemblyLoadContext(pluginPath);
                var assembly = tempContext.LoadFromAssemblyPath(pluginPath);
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPhobosPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null)
                {
                    tempContext.Unload();
                    HideLoading();
                    SetFileSelectStatus("无效的插件文件：未找到有效的插件类型");
                    return;
                }

                var pluginInstance = Activator.CreateInstance(pluginType) as IPhobosPlugin;
                if (pluginInstance == null)
                {
                    tempContext.Unload();
                    HideLoading();
                    SetFileSelectStatus("无法创建插件实例");
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
                SetFileSelectStatus($"加载失败: {ex.Message}");
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
            InstallButton.Content = _installContext.CanInstall ? "开始安装" : "无法安装";

            if (!_installContext.CanInstall)
            {
                SetInstallStatus("存在未满足的必需依赖，无法安装");
            }
            else if (_installContext.MissingOptional.Count > 0)
            {
                SetInstallStatus("存在未安装的可选依赖，部分功能可能受限");
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

        /// <summary>
        /// 切换到详情视图
        /// </summary>
        private void SwitchToDetailView()
        {
            FileSelectView.Visibility = Visibility.Collapsed;
            PluginDetailView.Visibility = Visibility.Visible;

            // 根据模式显示/隐藏返回按钮
            BackButton.Visibility = _mode == InstallerMode.UserOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 切换到文件选择视图
        /// </summary>
        private void SwitchToFileSelectView()
        {
            // 清理当前加载上下文
            CleanupInstallContext();

            PluginDetailView.Visibility = Visibility.Collapsed;
            FileSelectView.Visibility = Visibility.Visible;
            SetFileSelectStatus("就绪");
        }

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
            if (_mode == InstallerMode.UserOpen)
            {
                SwitchToFileSelectView();
            }
        }

        /// <summary>
        /// 退出按钮点击
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupInstallContext();
            ExitRequested?.Invoke(this, EventArgs.Empty);
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
                    $"以下可选依赖未安装，部分功能可能不可用：\n\n{deps}\n\n是否继续安装？",
                    "可选依赖确认",
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
            ExitButton.IsEnabled = false;
            ShowLoading("正在安装插件...");

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
                    SetInstallStatus("安装成功");
                    InstallButton.Content = "已安装";

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
                    SetInstallStatus($"安装失败: {result.Message}");
                    InstallButton.IsEnabled = true;
                    InstallButton.Content = "重试安装";
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                SetInstallStatus($"安装出错: {ex.Message}");
                InstallButton.IsEnabled = true;
                InstallButton.Content = "重试安装";
            }
            finally
            {
                ExitButton.IsEnabled = true;
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