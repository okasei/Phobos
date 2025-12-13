using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Components.Arcusrix.TaskManager.Helpers;
using Phobos.Interface.Plugin;
using Phobos.Manager.Arcusrix;
using Phobos.Manager.Database;
using Phobos.Manager.Plugin;
using Phobos.Shared.Interface;
using Phobos.Utils.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Phobos.Components.Arcusrix.TaskManager
{
    /// <summary>
    /// 任务管理器主界面
    /// </summary>
    public partial class PCOTaskManager : UserControl
    {
        private bool _isSidebarExpanded = true;
        private string _currentTab = "Running";
        private DispatcherTimer? _refreshTimer;
        private string? _editingStartupUUID;

        // 数据集合
        public ObservableCollection<RunningPluginItem> RunningPlugins { get; } = new();
        public ObservableCollection<StartupItem> StartupItems { get; } = new();

        // 选中项
        private RunningPluginItem? _selectedRunningPlugin;
        private StartupItem? _selectedStartupItem;

        public PCOTaskManager()
        {
            InitializeComponent();
            ApplyLocalization();
            SetupRefreshTimer();

            // 设置默认选中的选项卡样式
            UpdateTabSelection();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public async Task InitializeAsync()
        {
            await RefreshDataAsync();
        }

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            await RefreshRunningPluginsAsync();
            await RefreshStartupItemsAsync();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _refreshTimer?.Stop();
            _refreshTimer = null;
        }

        #region 本地化

        private void ApplyLocalization()
        {
            // 选项卡标签
            LabelRunning.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Tab_RunningPlugins);
            LabelStartup.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Tab_StartupItems);

            // 运行中插件页面
            TitleRunningPlugins.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Tab_RunningPlugins);
            BtnRefreshPluginsText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Refresh);
            BtnEndTaskText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_EndTask);
            HeaderPluginName.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_PluginName);
            HeaderPackageName.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_PackageName);
            HeaderStatus.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_Status);
            HeaderMemory.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_Memory);
            EmptyRunningText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Empty_NoRunningPlugins);

            // 启动项页面
            TitleStartupItems.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Tab_StartupItems);
            BtnRefreshStartupText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Refresh);
            BtnAddStartupText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Add);
            BtnEditStartupText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Edit);
            BtnDeleteStartupText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Delete);
            HeaderEnabled.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_Enabled);
            HeaderStartupPackage.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_PackageName);
            HeaderCommand.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_Command);
            HeaderPriority.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Header_Priority);
            EmptyStartupText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Empty_NoStartupItems);

            // 对话框
            LabelPackageName.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Dialog_PackageName);
            LabelCommand.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Dialog_Command);
            LabelPriority.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Dialog_Priority);
            BtnCancelText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Cancel);
            BtnSaveText.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Button_Save);
        }

        #endregion

        #region 定时刷新

        private void SetupRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                if (_currentTab == "Running")
                {
                    await RefreshRunningPluginsAsync();
                }
            };
            _refreshTimer.Start();
        }

        #endregion

        #region 汉堡菜单

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarExpanded = !_isSidebarExpanded;
            AnimateSidebar(_isSidebarExpanded);
        }

        private void AnimateSidebar(bool expand)
        {
            double targetWidth = expand ? 220 : 56;

            // 动画侧边栏宽度
            PUAnimation.AnimateGridLength(NavColumn, NavColumn.Width, new GridLength(targetWidth), 200);

            // 动画标签和标题透明度
            double labelOpacity = expand ? 1 : 0;
            PUAnimation.AnimateOpacityTo(LabelRunning, labelOpacity, 200);
            PUAnimation.AnimateOpacityTo(LabelStartup, labelOpacity, 200);
            PUAnimation.AnimateOpacityTo(NavTitle, labelOpacity, 200);
        }

        #endregion

        #region 选项卡切换

        private void TabRunningPlugins_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == "Running") return;
            SwitchToTab("Running");
        }

        private void TabStartup_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab == "Startup") return;
            SwitchToTab("Startup");
        }

        private void SwitchToTab(string tabName)
        {
            var oldTab = _currentTab;
            _currentTab = tabName;

            // 更新选项卡样式
            UpdateTabSelection();

            // 动画切换页面
            if (tabName == "Running")
            {
                AnimatePageTransition(PageStartup, PageRunningPlugins);
            }
            else
            {
                AnimatePageTransition(PageRunningPlugins, PageStartup);
            }
        }

        private void UpdateTabSelection()
        {
            // 使用 Tag 属性来控制 NavItemStyle 的 Active 状态
            if (_currentTab == "Running")
            {
                NavRunningPlugins.Tag = "Active";
                NavStartupItems.Tag = null;
            }
            else
            {
                NavRunningPlugins.Tag = null;
                NavStartupItems.Tag = "Active";
            }
        }

        private void AnimatePageTransition(Grid outPage, Grid inPage)
        {
            // 淡出旧页面
            PUAnimation.FadeOut(outPage, 150, 1, 0, null, 0, () =>
            {
                outPage.Visibility = Visibility.Collapsed;
                inPage.Visibility = Visibility.Visible;
                inPage.Opacity = 0;

                // 滑入新页面
                PUAnimation.SlideAndFadeIn(inPage, 0, 20, 200);
            });
        }

        #endregion

        #region 运行中插件管理

        private async Task RefreshRunningPluginsAsync()
        {
            try
            {
                var plugins = await Task.Run(() =>
                {
                    var result = new List<RunningPluginItem>();
                    var loadedPlugins = PMPlugin.Instance.LoadedPlugins;

                    foreach (var kvp in loadedPlugins)
                    {
                        var context = kvp.Value;
                        if (context.Instance == null) continue;

                        // 只显示状态为 Running 的插件
                        if (context.State != PluginState.Running && context.State != PluginState.Loaded) continue;

                        var item = new RunningPluginItem
                        {
                            PackageName = context.PackageName,
                            Name = context.Instance.Metadata.GetLocalizedName(TaskManagerLocalization.CurrentLanguage),
                            Icon = null, // 可以后续加载图标
                            Status = context.State == PluginState.Running ? PluginStatus.Running : PluginStatus.Suspended,
                            IsSystemPlugin = context.Instance.Metadata.IsSystemPlugin,
                            MemoryUsage = "N/A" // 内存占用需要特殊处理
                        };
                        result.Add(item);
                    }

                    return result;
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    RunningPlugins.Clear();
                    foreach (var plugin in plugins)
                    {
                        RunningPlugins.Add(plugin);
                    }
                    RunningPluginsList.ItemsSource = RunningPlugins;

                    // 更新空状态显示
                    EmptyRunningState.Visibility = RunningPlugins.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to refresh running plugins: {ex.Message}");
            }
        }

        private void BtnRefreshPlugins_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshRunningPluginsAsync();
        }

        private void PluginItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is RunningPluginItem item)
            {
                // 取消之前选中的项
                if (_selectedRunningPlugin != null)
                {
                    _selectedRunningPlugin.IsSelected = false;
                }

                // 选中当前项
                item.IsSelected = true;
                _selectedRunningPlugin = item;

                // 启用结束任务按钮（系统插件不允许结束）
                BtnEndTask.IsEnabled = !item.IsSystemPlugin;

                // 刷新显示
                RunningPluginsList.Items.Refresh();
            }
        }

        private async void BtnEndTask_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRunningPlugin == null || _selectedRunningPlugin.IsSystemPlugin)
                return;

            try
            {
                var result = await PMPlugin.Instance.Stop(_selectedRunningPlugin.PackageName);
                if (result.Success)
                {
                    await RefreshRunningPluginsAsync();
                    _selectedRunningPlugin = null;
                    BtnEndTask.IsEnabled = false;
                }
                else
                {
                    // 显示错误提示
                    PCLoggerPlugin.Error("TaskManager", $"Failed to end task: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to end task: {ex.Message}");
            }
        }

        #endregion

        #region 启动项管理

        private async Task RefreshStartupItemsAsync()
        {
            try
            {
                var items = await Task.Run(async () =>
                {
                    var result = new List<StartupItem>();
                    var db = PMDatabase.Instance.Database;

                    if (db != null)
                    {
                        var bootItems = await db.ExecuteQuery(
                            "SELECT UUID, Command, PackageName, IsEnabled, Priority FROM Phobos_Boot ORDER BY Priority ASC");

                        if (bootItems != null)
                        {
                            foreach (var row in bootItems)
                            {
                                var item = new StartupItem
                                {
                                    UUID = row["UUID"]?.ToString() ?? string.Empty,
                                    PackageName = row["PackageName"]?.ToString() ?? string.Empty,
                                    Command = row["Command"]?.ToString() ?? string.Empty,
                                    IsEnabled = Convert.ToInt32(row["IsEnabled"]) == 1,
                                    Priority = Convert.ToInt32(row["Priority"])
                                };
                                result.Add(item);
                            }
                        }
                    }

                    return result;
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    StartupItems.Clear();
                    foreach (var item in items)
                    {
                        StartupItems.Add(item);
                    }
                    StartupItemsList.ItemsSource = StartupItems;

                    // 更新空状态显示
                    EmptyStartupState.Visibility = StartupItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to refresh startup items: {ex.Message}");
            }
        }

        private void BtnRefreshStartup_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshStartupItemsAsync();
        }

        private void StartupItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is StartupItem item)
            {
                // 取消之前选中的项
                if (_selectedStartupItem != null)
                {
                    _selectedStartupItem.IsSelected = false;
                }

                // 选中当前项
                item.IsSelected = true;
                _selectedStartupItem = item;

                // 启用编辑和删除按钮
                BtnEditStartup.IsEnabled = true;
                BtnDeleteStartup.IsEnabled = true;

                // 刷新显示
                StartupItemsList.Items.Refresh();
            }
        }

        private async void StartupItem_EnabledChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is StartupItem item)
            {
                await UpdateStartupItemEnabledAsync(item.UUID, item.IsEnabled);
            }
        }

        private async void StartupItem_ToggleEnabled(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StartupItem item)
            {
                item.IsEnabled = !item.IsEnabled;
                await UpdateStartupItemEnabledAsync(item.UUID, item.IsEnabled);
                StartupItemsList.Items.Refresh();
            }
        }

        private async Task UpdateStartupItemEnabledAsync(string uuid, bool enabled)
        {
            try
            {
                var db = PMDatabase.Instance.Database;
                if (db != null)
                {
                    await db.ExecuteNonQuery(
                        "UPDATE Phobos_Boot SET IsEnabled = @enabled WHERE UUID = @uuid",
                        new Dictionary<string, object>
                        {
                            { "@enabled", enabled ? 1 : 0 },
                            { "@uuid", uuid }
                        });
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to update startup item: {ex.Message}");
            }
        }

        private void BtnAddStartup_Click(object sender, RoutedEventArgs e)
        {
            _editingStartupUUID = null;
            DialogTitle.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Dialog_AddTitle);
            TxtPackageName.Text = string.Empty;
            TxtCommand.Text = string.Empty;
            TxtPriority.Text = "100";
            ShowStartupDialog();
        }

        private void BtnEditStartup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStartupItem == null) return;

            _editingStartupUUID = _selectedStartupItem.UUID;
            DialogTitle.Text = TaskManagerLocalization.Get(TaskManagerLocalization.Dialog_EditTitle);
            TxtPackageName.Text = _selectedStartupItem.PackageName;
            TxtCommand.Text = _selectedStartupItem.Command;
            TxtPriority.Text = _selectedStartupItem.Priority.ToString();
            ShowStartupDialog();
        }

        private async void BtnDeleteStartup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStartupItem == null) return;

            try
            {
                var db = PMDatabase.Instance.Database;
                if (db != null)
                {
                    await db.ExecuteNonQuery(
                        "DELETE FROM Phobos_Boot WHERE UUID = @uuid",
                        new Dictionary<string, object>
                        {
                            { "@uuid", _selectedStartupItem.UUID }
                        });

                    await RefreshStartupItemsAsync();
                    _selectedStartupItem = null;
                    BtnEditStartup.IsEnabled = false;
                    BtnDeleteStartup.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to delete startup item: {ex.Message}");
            }
        }

        #endregion

        #region 对话框

        private void ShowStartupDialog()
        {
            StartupEditDialog.Visibility = Visibility.Visible;
            PUAnimation.FadeIn(StartupEditDialog, 200);
            TxtPackageName.Focus();
        }

        private void HideStartupDialog()
        {
            PUAnimation.FadeOut(StartupEditDialog, 150, 1, 0, null, 0, () =>
            {
                StartupEditDialog.Visibility = Visibility.Collapsed;
            });
        }

        private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            HideStartupDialog();
        }

        private async void BtnDialogSave_Click(object sender, RoutedEventArgs e)
        {
            var packageName = TxtPackageName.Text.Trim();
            var command = TxtCommand.Text.Trim();

            if (string.IsNullOrEmpty(packageName))
            {
                // 显示验证错误
                return;
            }

            if (!int.TryParse(TxtPriority.Text, out int priority))
            {
                priority = 100;
            }

            try
            {
                var db = PMDatabase.Instance.Database;
                if (db != null)
                {
                    if (string.IsNullOrEmpty(_editingStartupUUID))
                    {
                        // 新增
                        var uuid = Guid.NewGuid().ToString();
                        await db.ExecuteNonQuery(
                            "INSERT INTO Phobos_Boot (UUID, PackageName, Command, IsEnabled, Priority) VALUES (@uuid, @package, @command, 1, @priority)",
                            new Dictionary<string, object>
                            {
                                { "@uuid", uuid },
                                { "@package", packageName },
                                { "@command", command },
                                { "@priority", priority }
                            });
                    }
                    else
                    {
                        // 编辑
                        await db.ExecuteNonQuery(
                            "UPDATE Phobos_Boot SET PackageName = @package, Command = @command, Priority = @priority WHERE UUID = @uuid",
                            new Dictionary<string, object>
                            {
                                { "@uuid", _editingStartupUUID },
                                { "@package", packageName },
                                { "@command", command },
                                { "@priority", priority }
                            });
                    }

                    HideStartupDialog();
                    await RefreshStartupItemsAsync();
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("TaskManager", $"Failed to save startup item: {ex.Message}");
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 运行中插件项
    /// </summary>
    public class RunningPluginItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string PackageName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
        public PluginStatus Status { get; set; }
        public bool IsSystemPlugin { get; set; }
        public string MemoryUsage { get; set; } = "N/A";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string StatusText => Status switch
        {
            PluginStatus.Running => TaskManagerLocalization.Get(TaskManagerLocalization.Status_Running),
            PluginStatus.Suspended => TaskManagerLocalization.Get(TaskManagerLocalization.Status_Suspended),
            _ => "Unknown"
        };

        public SolidColorBrush StatusColor => Status switch
        {
            PluginStatus.Running => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            PluginStatus.Suspended => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// 启动项
    /// </summary>
    public class StartupItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isEnabled;

        public string UUID { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleIcon));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string ToggleIcon => IsEnabled ? "\uE73E" : "\uE711";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// 插件状态
    /// </summary>
    public enum PluginStatus
    {
        Running,
        Suspended,
        Stopped
    }

    #endregion
}
