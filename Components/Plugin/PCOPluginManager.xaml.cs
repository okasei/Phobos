using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Phobos.Components.Plugin
{
    /// <summary>
    /// POPluginInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class POPluginInstaller : Window
    {
        public PluginViewModel ViewModel { get; }
        public POPluginInstaller()
        {
            InitializeComponent(); InitializeComponent();
            ViewModel = new PluginViewModel();
            DataContext = ViewModel;

            // 加载示例数据
            ViewModel.LoadSamplePlugins();
        }
    }

    /// <summary>
    /// 插件视图模型
    /// </summary>
    public class PluginViewModel : INotifyPropertyChanged
    {
        private string _searchText = string.Empty;
        private string _selectedCategory = "全部";
        private ObservableCollection<Plugin> _plugins = new();
        private ObservableCollection<Plugin> _filteredPlugins = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterPlugins();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterPlugins();
            }
        }

        public ObservableCollection<Plugin> Plugins
        {
            get => _plugins;
            set
            {
                _plugins = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Plugin> FilteredPlugins
        {
            get => _filteredPlugins;
            set
            {
                _filteredPlugins = value;
                OnPropertyChanged();
            }
        }

        // 命令
        public ICommand InstallCommand { get; }
        public ICommand UninstallCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand InstallFromLocalCommand { get; }

        public PluginViewModel()
        {
            InstallCommand = new RelayCommand<Plugin>(InstallPlugin);
            UninstallCommand = new RelayCommand<Plugin>(UninstallPlugin);
            UpdateCommand = new RelayCommand<Plugin>(UpdatePlugin);
            RefreshCommand = new RelayCommand<Plugin>(_ => RefreshPlugins());
            InstallFromLocalCommand = new RelayCommand<Plugin>(_ => InstallFromLocal());
        }

        /// <summary>
        /// 加载示例插件数据
        /// </summary>
        public void LoadSamplePlugins()
        {
            Plugins = new ObservableCollection<Plugin>
            {
                new Plugin
                {
                    Id = "dark-theme-pro",
                    Name = "Dark Theme Pro",
                    Version = "2.4.1",
                    Author = "ThemeLab",
                    Description = "专业级深色主题，支持自定义配色方案，护眼模式，以及多种预设风格。",
                    Category = "主题外观",
                    Icon = "🎨",
                    IconBackground = "#1E40AF",
                    Downloads = 128000,
                    Rating = 4.9,
                    Status = PluginStatus.Installed,
                    Tags = new[] { "主题", "外观" }
                },
                new Plugin
                {
                    Id = "data-visualizer",
                    Name = "Data Visualizer",
                    Version = "3.1.0",
                    Author = "DataWorks",
                    Description = "强大的数据可视化工具，支持图表、仪表盘、实时数据流展示。",
                    Category = "数据分析",
                    Icon = "📊",
                    IconBackground = "#059669",
                    Downloads = 89000,
                    Rating = 4.7,
                    Status = PluginStatus.UpdateAvailable,
                    Tags = new[] { "数据", "图表" }
                },
                new Plugin
                {
                    Id = "code-formatter",
                    Name = "Code Formatter",
                    Version = "1.8.2",
                    Author = "DevTools",
                    Description = "一键格式化代码，支持 C#、JavaScript、Python 等多种语言。",
                    Category = "开发工具",
                    Icon = "🔧",
                    IconBackground = "#DC2626",
                    Downloads = 256000,
                    Rating = 4.8,
                    Status = PluginStatus.NotInstalled,
                    Tags = new[] { "开发", "效率" }
                },
                new Plugin
                {
                    Id = "ai-assistant",
                    Name = "AI Assistant",
                    Version = "4.0.0",
                    Author = "AILabs",
                    Description = "智能 AI 助手，提供代码补全、文档生成、智能问答等功能。",
                    Category = "开发工具",
                    Icon = "🤖",
                    IconBackground = "#7C3AED",
                    Downloads = 512000,
                    Rating = 4.9,
                    Status = PluginStatus.NotInstalled,
                    Tags = new[] { "AI", "智能" }
                },
                new Plugin
                {
                    Id = "security-scanner",
                    Name = "Security Scanner",
                    Version = "2.2.0",
                    Author = "SecureLab",
                    Description = "代码安全扫描工具，自动检测潜在的安全漏洞和风险。",
                    Category = "安全隐私",
                    Icon = "🔒",
                    IconBackground = "#0891B2",
                    Downloads = 67000,
                    Rating = 4.6,
                    Status = PluginStatus.NotInstalled,
                    Tags = new[] { "安全", "扫描" }
                },
                new Plugin
                {
                    Id = "api-tester",
                    Name = "API Tester",
                    Version = "1.5.3",
                    Author = "WebDev",
                    Description = "轻量级 API 测试工具，支持 REST、GraphQL，可保存请求历史。",
                    Category = "网络工具",
                    Icon = "🌐",
                    IconBackground = "#EA580C",
                    Downloads = 145000,
                    Rating = 4.5,
                    Status = PluginStatus.NotInstalled,
                    Tags = new[] { "API", "测试" }
                }
            };

            FilterPlugins();
        }

        /// <summary>
        /// 筛选插件
        /// </summary>
        private void FilterPlugins()
        {
            var filtered = new ObservableCollection<Plugin>();

            foreach (var plugin in Plugins)
            {
                bool matchesSearch = string.IsNullOrEmpty(SearchText) ||
                    plugin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    plugin.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                bool matchesCategory = SelectedCategory == "全部" ||
                    plugin.Category == SelectedCategory;

                if (matchesSearch && matchesCategory)
                {
                    filtered.Add(plugin);
                }
            }

            FilteredPlugins = filtered;
        }

        /// <summary>
        /// 安装插件（实际操作略）
        /// </summary>
        private void InstallPlugin(Plugin? plugin)
        {
            if (plugin == null) return;

            // TODO: 实际安装逻辑
            // 1. 下载插件包
            // 2. 验证签名
            // 3. 解压到插件目录
            // 4. 加载插件
            // 5. 更新配置

            plugin.Status = PluginStatus.Installed;
            MessageBox.Show($"插件 {plugin.Name} 安装成功！", "安装完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 卸载插件（实际操作略）
        /// </summary>
        private void UninstallPlugin(Plugin? plugin)
        {
            if (plugin == null) return;

            var result = MessageBox.Show(
                $"确定要卸载插件 {plugin.Name} 吗？",
                "确认卸载",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: 实际卸载逻辑
                // 1. 卸载插件
                // 2. 删除插件文件
                // 3. 清理配置

                plugin.Status = PluginStatus.NotInstalled;
                MessageBox.Show($"插件 {plugin.Name} 已卸载。", "卸载完成",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 更新插件（实际操作略）
        /// </summary>
        private void UpdatePlugin(Plugin? plugin)
        {
            if (plugin == null) return;

            // TODO: 实际更新逻辑
            // 1. 下载新版本
            // 2. 备份旧版本
            // 3. 替换文件
            // 4. 重新加载

            plugin.Status = PluginStatus.Installed;
            MessageBox.Show($"插件 {plugin.Name} 更新成功！", "更新完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 刷新插件列表（实际操作略）
        /// </summary>
        private void RefreshPlugins()
        {
            // TODO: 从服务器获取最新插件列表
            LoadSamplePlugins();
            MessageBox.Show("插件列表已刷新。", "刷新完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 从本地安装（实际操作略）
        /// </summary>
        private void InstallFromLocal()
        {
            // TODO: 打开文件选择对话框
            // 选择 .zip 或 .plugin 文件
            // 验证并安装

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择插件包",
                Filter = "插件包 (*.zip;*.plugin)|*.zip;*.plugin|所有文件 (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                // TODO: 安装本地插件
                MessageBox.Show($"正在安装: {filePath}", "本地安装",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 插件数据模型
    /// </summary>
    public class Plugin : INotifyPropertyChanged
    {
        private PluginStatus _status;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = "📦";
        public string IconBackground { get; set; } = "#6366F1";
        public int Downloads { get; set; }
        public double Rating { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();

        public PluginStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsInstalled));
                OnPropertyChanged(nameof(HasUpdate));
            }
        }

        public string StatusText => Status switch
        {
            PluginStatus.Installed => "已安装",
            PluginStatus.UpdateAvailable => "可更新",
            _ => ""
        };

        public bool IsInstalled => Status == PluginStatus.Installed;
        public bool HasUpdate => Status == PluginStatus.UpdateAvailable;

        public string DownloadsDisplay => Downloads >= 1000
            ? $"{Downloads / 1000}K"
            : Downloads.ToString();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 插件状态枚举
    /// </summary>
    public enum PluginStatus
    {
        NotInstalled,
        Installed,
        UpdateAvailable
    }

    /// <summary>
    /// 通用命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
