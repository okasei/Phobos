using Phobos.Class.Database;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Plugin;
using Phobos.Shared.Interface;
using Phobos.Utils.Media;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Phobos.Shared.Models;
using DesktopLocalization = Phobos.Components.Arcusrix.Desktop.Components.DesktopLocalization;
using Phobos.Components.Arcusrix.Desktop.Components;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// 壁纸变化事件参数
    /// </summary>
    public class WallpaperChangedEventArgs : EventArgs
    {
        public string WallpaperPath { get; set; } = string.Empty;
        public Stretch Stretch { get; set; } = Stretch.UniformToFill;
    }

    /// <summary>
    /// 透明度变化事件参数
    /// </summary>
    public class OpacityChangedEventArgs : EventArgs
    {
        public double Opacity { get; set; } = 1.0;
    }

    /// <summary>
    /// 插件显示项（用于桌面图标）
    /// </summary>
    public class PluginDisplayItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _packageName = string.Empty;
        private bool _isSystemPlugin;
        private ImageSource? _icon;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string PackageName
        {
            get => _packageName;
            set { _packageName = value; OnPropertyChanged(nameof(PackageName)); }
        }

        public bool IsSystemPlugin
        {
            get => _isSystemPlugin;
            set { _isSystemPlugin = value; OnPropertyChanged(nameof(IsSystemPlugin)); }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// PCOPhobosDesktop.xaml 的交互逻辑
    /// </summary>
    public partial class PCOPhobosDesktop : Window
    {
        private PCSqliteDatabase? _database;
        private DesktopLayout _layout = new();
        private Dictionary<string, PluginDisplayItem> _allPlugins = new();
        private string _layoutPath = string.Empty;
        private FolderDesktopItem? _currentOpenFolder = null;
        private bool _isLayoutLoaded = false; // 布局是否已加载完成
        private bool _isClosingFromTray = false; // 是否从托盘关闭（真正关闭）

        // 拖拽相关
        private Border? _draggingIcon = null;
        private PluginDisplayItem? _draggingPlugin = null;
        private FolderDesktopItem? _draggingFolder = null;
        private System.Windows.Threading.DispatcherTimer? _longPressTimer = null;
        private Point _mouseDownPosition;
        private bool _isDragging = false;
        private bool _isDraggingFromFolder = false;

        // 拖拽视觉反馈
        private Border? _dragPreview = null;
        private Border? _dragOverlay = null;

        public event EventHandler<string>? PluginClicked;

        /// <summary>
        /// 公共方法：刷新插件列表
        /// </summary>
        public async void RefreshPlugins()
        {
            await LoadPlugins();
        }

        /// <summary>
        /// 刷新插件列表并将新安装的插件添加到桌面布局
        /// </summary>
        public async void RefreshAndAddNewPlugins()
        {
            await LoadPlugins();
            AddMissingPluginsToLayout();
            RenderDesktop();
        }

        /// <summary>
        /// 创建桌面窗口
        /// </summary>
        public PCOPhobosDesktop()
        {
            try
            {
                InitializeComponent();
                EnableTrayIcon = true;
                EnableAutoHide = true;
                EnableTaskbarAwareAnimation = true;

                // 设置布局文件路径
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var layoutDir = Path.Combine(appDataPath, "Phobos", "Plugins", "com.phobos.desktop", "Layout");
                Utils.IO.PUFileSystem.Instance.CreateFullFolders(layoutDir);
                _layoutPath = Path.Combine(layoutDir, "desktop_layout.json");

                System.Diagnostics.Debug.WriteLine($"[PCOPhobosDesktop] Layout path: {_layoutPath}");

                StateChanged += (s, e) =>
                {
                    try
                    {
                        // 全屏/还原时保存状态
                        if (_isLayoutLoaded)
                        {
                            SaveLayout();
                        }
                        // 全屏/还原时播放动画
                        AnimateWindowStateChange();
                        // 延迟更新网格布局，等待布局完成
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateGridLayout();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    catch (Exception stateEx)
                    {
                        PCLoggerPlugin.Error("PCOPhobosDesktop", $"Error in StateChanged handler: {stateEx.Message}");
                    }
                };

                Loaded += PCOPhobosDesktop_Loaded;
                Closing += PCOPhobosDesktop_Closing;

                // 搜索框焦点事件
                SearchBox.GotFocus += SearchBox_GotFocus;
                SearchBox.LostFocus += SearchBox_LostFocus;

                // 初始设置窗口为透明，准备入场动画
                MainBorder.Opacity = 0;
                MainBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                MainBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PCOPhobosDesktop", $"Error in constructor: {ex.Message}\n{ex.StackTrace}");
                throw; // 重新抛出以让调用者知道初始化失败
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void PCOPhobosDesktop_Closing(object? sender, CancelEventArgs e)
        {
            // 如果是从托盘退出，直接关闭
            if (_isClosingFromTray)
            {
                // 注销所有快捷键
                Manager.Hotkey.PMHotkey.Instance.Dispose();
                CleanupTrayExtension();
                Application.Current.Shutdown(0);
                return;
            }

            // 否则取消关闭，改为隐藏到托盘
            e.Cancel = true;
            HideToTray();
        }

        /// <summary>
        /// 设置数据库实例
        /// </summary>
        public void SetDatabase(PCSqliteDatabase database)
        {
            _database = database;
        }

        private async void PCOPhobosDesktop_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PCOPhobosDesktop] Window loaded, starting initialization...");

                // 应用本地化文本
                ApplyLocalization();

                // 播放窗口入场动画
                PlayWindowOpenAnimation();

                // 尝试从 PMPlugin 获取数据库实例
                if (_database == null)
                {
                    try
                    {
                        var field = typeof(PMPlugin).GetField("_database",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        _database = field?.GetValue(PMPlugin.Instance) as PCSqliteDatabase;
                        System.Diagnostics.Debug.WriteLine($"[PCOPhobosDesktop] Database instance: {(_database != null ? "OK" : "NULL")}");
                    }
                    catch (Exception dbEx)
                    {
                        PCLoggerPlugin.Error("PCOPhobosDesktop", $"Failed to get database instance: {dbEx.Message}");
                    }
                }

                await LoadPlugins();
                System.Diagnostics.Debug.WriteLine($"[PCOPhobosDesktop] Loaded {_allPlugins.Count} plugins");

                await LoadLayout();
                System.Diagnostics.Debug.WriteLine($"[PCOPhobosDesktop] Layout loaded: {_layout.Items.Count} items");

                RenderDesktop(playAnimation: true); // 窗口初次加载时播放动画

                // 注册DesktopScrollViewer的SizeChanged事件来动态更新布局
                DesktopScrollViewer.SizeChanged += (s, args) =>
                {
                    try
                    {
                        UpdateGridLayout();
                    }
                    catch (Exception sizeEx)
                    {
                        PCLoggerPlugin.Error("PCOPhobosDesktop", $"Error in ScrollViewer SizeChanged handler: {sizeEx.Message}");
                    }
                };

                // 注册所有快捷键
                try
                {
                    RegisterAllHotkeys();
                }
                catch (Exception hotkeyEx)
                {
                    PCLoggerPlugin.Error("PCOPhobosDesktop", $"Failed to register hotkeys: {hotkeyEx.Message}");
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PCOPhobosDesktop", $"Critical error during window initialization: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[PCOPhobosDesktop] Critical error: {ex.Message}");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 创建文字阴影效果，用于桌面图标文字
        /// </summary>
        private System.Windows.Media.Effects.DropShadowEffect CreateTextShadowEffect()
        {
            // 获取 Background1Brush 的颜色用于阴影
            var bgBrush = FindResource("Background1Brush") as SolidColorBrush;
            var shadowColor = bgBrush?.Color ?? Colors.Black;

            return new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = shadowColor,
                BlurRadius = 4,
                ShadowDepth = 0,
                Opacity = 0.9,
                Direction = 0
            };
        }

        #endregion

        #region 窗口动画

        /// <summary>
        /// 播放窗口打开动画
        /// </summary>
        private void PlayWindowOpenAnimation()
        {
            var storyboard = new Storyboard();

            var elasticEase = PUAnimation.CreateElasticEase(EasingMode.EaseOut, 1, 8);
            var cubicEase = PUAnimation.CreateSmoothEase();

            // 淡入
            PUAnimation.AddOpacityAnimation(storyboard, MainBorder, 0, 1, 300, cubicEase);

            // 缩放X
            PUAnimation.AddScaleXAnimation(storyboard, MainBorder, 0.95, 1, 400, elasticEase, 0,
                "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");

            // 缩放Y
            PUAnimation.AddScaleYAnimation(storyboard, MainBorder, 0.95, 1, 400, elasticEase, 0,
                "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

            storyboard.Begin();
        }

        /// <summary>
        /// 窗口状态改变时的动画（全屏/还原）
        /// </summary>
        private void AnimateWindowStateChange()
        {
            var storyboard = new Storyboard();

            var elasticEase = PUAnimation.CreateElasticEase(EasingMode.EaseOut, 1, 10);

            // 快速缩放弹跳效果
            PUAnimation.AddScaleXAnimation(storyboard, MainBorder, 0.98, 1, 300, elasticEase, 0,
                "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");

            PUAnimation.AddScaleYAnimation(storyboard, MainBorder, 0.98, 1, 300, elasticEase, 0,
                "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

            storyboard.Begin();

            // 收集所有图标并播放飞入动画
            var iconControls = new List<(Border control, int index)>();
            int index = 0;
            foreach (UIElement child in DesktopGrid.Children)
            {
                if (child is Border border)
                {
                    iconControls.Add((border, index++));
                }
            }

            if (iconControls.Count > 0)
            {
                AnimateIconsFlyIn(iconControls);
            }
        }

        #endregion

        #region 本地化

        /// <summary>
        /// 应用本地化文本到控件
        /// </summary>
        private void ApplyLocalization()
        {
            // 搜索框占位符
            SearchPlaceholder.Text = DesktopLocalization.Get(DesktopLocalization.Desktop_Search_Placeholder);
        }

        #endregion

        #region 搜索栏动画

        /// <summary>
        /// 搜索框获取焦点 - 展开动画
        /// </summary>
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AnimateSearchBarWidth(SearchBorder, 600); // 展开到 600
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 搜索框失去焦点 - 收缩动画
        /// </summary>
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }

            AnimateSearchBarWidth(SearchBorder, 400); // 收缩回 400
        }

        /// <summary>
        /// 搜索栏宽度动画
        /// </summary>
        private void AnimateSearchBarWidth(Border border, double targetWidth)
        {
            PUAnimation.AnimateWidthTo(border, targetWidth, 250, PUAnimation.CreateSmoothEase());
        }

        #endregion

        /// <summary>
        /// 加载所有插件数据
        /// </summary>
        private async Task LoadPlugins()
        {
            _allPlugins.Clear();

            if (_database == null)
                return;

            try
            {
                var pluginRecords = await _database.ExecuteQuery("SELECT * FROM Phobos_Plugin ORDER BY Name");

                foreach (var record in pluginRecords)
                {
                    var packageName = record["PackageName"]?.ToString() ?? string.Empty;
                    var name = record["Name"]?.ToString() ?? string.Empty;
                    var directory = record["Directory"]?.ToString() ?? string.Empty;
                    var icon = record["Icon"]?.ToString() ?? string.Empty;
                    var isSystemPlugin = Convert.ToBoolean(record["IsSystemPlugin"]);
                    var launchFlag = Convert.ToInt32(record["LaunchFlag"] ?? 0) == 1;

                    // 跳过 LaunchFlag 为 false 的插件（不可被显式启动的插件不在桌面显示）
                    if (!launchFlag)
                        continue;

                    // 跳过 Desktop 插件自身（不在桌面显示自己）
                    if (packageName == "com.phobos.desktop")
                        continue;

                    // 判断是否为内建插件
                    bool isBuiltIn = string.Equals(directory, "builtin", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(directory, "built-in", StringComparison.OrdinalIgnoreCase);

                    if (isBuiltIn)
                        isSystemPlugin = true;

                    var displayItem = new PluginDisplayItem
                    {
                        PackageName = packageName,
                        Name = name,
                        IsSystemPlugin = isSystemPlugin
                    };

                    // 加载图标
                    if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(directory))
                    {
                        try
                        {
                            string iconPath = isBuiltIn
                                ? (Path.IsPathRooted(icon) ? icon : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, icon))
                                : (Path.IsPathRooted(icon) ? icon : Path.Combine(directory, icon));

                            if (File.Exists(iconPath))
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                displayItem.Icon = bitmap;
                            }
                        }
                        catch { }
                    }

                    _allPlugins[packageName] = displayItem;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load plugins: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载布局配置
        /// </summary>
        private async Task LoadLayout()
        {
            bool layoutLoaded = false;

            try
            {
                if (File.Exists(_layoutPath))
                {
                    var json = await File.ReadAllTextAsync(_layoutPath);

                    var layout = JsonConvert.DeserializeObject<DesktopLayout>(json);
                    if (layout != null)
                    {
                        _layout = layout;
                        WindowState = _layout.IsFullscreen ? WindowState.Maximized : WindowState.Normal;
                        layoutLoaded = true;

                        // 加载背景设置
                        _backgroundImagePath = _layout.BackgroundImagePath ?? string.Empty;
                        _backgroundOpacity = _layout.BackgroundOpacity;
                        if (!string.IsNullOrEmpty(_layout.BackgroundStretch) &&
                            Enum.TryParse<Stretch>(_layout.BackgroundStretch, out var stretch))
                        {
                            _backgroundStretch = stretch;
                        }
                    }
                    else
                    {
                        PCLoggerPlugin.Error("PCOPhobosDesktop", "[LoadLayout] Failed to deserialize layout - result is null");
                    }
                }
                else
                {
                    PCLoggerPlugin.Info("PCOPhobosDesktop", $"[LoadLayout] Layout file not found: {_layoutPath}");
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PCOPhobosDesktop", $"[LoadLayout] Failed to load layout: {ex.Message}\n{ex.StackTrace}");
            }

            if (!layoutLoaded)
            {
                // 创建默认布局
                CreateDefaultLayout();
            }
            else
            {
                // 检查是否有新插件需要添加到布局中
                AddMissingPluginsToLayout();
            }

            // 标记布局加载完成，允许保存
            _isLayoutLoaded = true;
            PCLoggerPlugin.Info("PCOPhobosDesktop", "[LoadLayout] Layout loading completed, saving enabled");

            // 应用背景图片设置
            ApplyBackgroundImage();
        }

        /// <summary>
        /// 将缺失的插件添加到布局中
        /// </summary>
        private void AddMissingPluginsToLayout()
        {
            var existingPackageNames = new HashSet<string>();

            // 收集已在布局中的插件和文件夹中的插件
            foreach (var item in _layout.Items)
            {
                if (item is PluginDesktopItem pluginItem)
                {
                    existingPackageNames.Add(pluginItem.PackageName);
                }
                else if (item is FolderDesktopItem folderItem)
                {
                    // 收集文件夹中的插件
                    foreach (var packageName in folderItem.PluginPackageNames)
                    {
                        existingPackageNames.Add(packageName);
                    }
                }
            }

            // 兼容旧版本：也检查 Folders 列表
            foreach (var folder in _layout.Folders)
            {
                foreach (var packageName in folder.PluginPackageNames)
                {
                    existingPackageNames.Add(packageName);
                }
            }

            // 查找缺失的插件
            var missingPlugins = _allPlugins.Keys.Where(p => !existingPackageNames.Contains(p)).ToList();

            if (missingPlugins.Count > 0)
            {
                PCLoggerPlugin.Info("PCOPhobosDesktop", $"[AddMissingPlugins] Found {missingPlugins.Count} missing plugins");

                foreach (var packageName in missingPlugins)
                {
                    // 查找第一个空位
                    var position = FindFirstEmptyPosition();
                    PCLoggerPlugin.Info("PCOPhobosDesktop", $"[AddMissingPlugins] Adding plugin: {packageName} at ({position.X}, {position.Y})");

                    _layout.Items.Add(new PluginDesktopItem
                    {
                        PackageName = packageName,
                        GridX = position.X,
                        GridY = position.Y
                    });
                }

                SaveLayout();
            }
            else
            {
                PCLoggerPlugin.Info("PCOPhobosDesktop", "[AddMissingPlugins] No missing plugins");
            }
        }

        /// <summary>
        /// 查找第一个空位（从左到右，从上到下扫描）
        /// </summary>
        private (int X, int Y) FindFirstEmptyPosition()
        {
            // 收集已占用的位置
            var occupiedPositions = new HashSet<(int, int)>();
            foreach (var item in _layout.Items)
            {
                occupiedPositions.Add((item.GridX, item.GridY));
            }

            // 从上到下，从左到右扫描
            for (int y = 0; ; y++)
            {
                for (int x = 0; x < _layout.Columns; x++)
                {
                    if (!occupiedPositions.Contains((x, y)))
                    {
                        return (x, y);
                    }
                }
            }
        }

        /// <summary>
        /// 创建默认布局
        /// </summary>
        private void CreateDefaultLayout()
        {
            PCLoggerPlugin.Info("PCOPhobosDesktop", "[CreateDefaultLayout] Creating default layout...");

            _layout = new DesktopLayout
            {
                Columns = 6,
                Rows = 4,
                IsFullscreen = false,
                Items = new List<DesktopItem>(),
                Folders = new List<FolderDesktopItem>()
            };

            // 将所有插件按顺序放入网格
            int x = 0, y = 0;
            foreach (var plugin in _allPlugins.Values)
            {

                _layout.Items.Add(new PluginDesktopItem
                {
                    PackageName = plugin.PackageName,
                    GridX = x,
                    GridY = y
                });

                x++;
                if (x >= _layout.Columns)
                {
                    x = 0;
                    y++;
                }
            }


            // 临时允许保存，然后保存默认布局
            _isLayoutLoaded = true;
            SaveLayout();
        }

        /// <summary>
        /// 保存布局配置
        /// </summary>
        private void SaveLayout()
        {
            // 只有在布局加载完成后才允许保存
            if (!_isLayoutLoaded)
            {
                PCLoggerPlugin.Warning("PCOPhobosDesktop", "[SaveLayout] Skipped - layout not yet loaded");
                return;
            }

            try
            {
                _layout.IsFullscreen = WindowState == WindowState.Maximized;

                // 保存背景设置
                _layout.BackgroundImagePath = _backgroundImagePath;
                _layout.BackgroundStretch = _backgroundStretch.ToString();
                _layout.BackgroundOpacity = _backgroundOpacity;

                PCLoggerPlugin.Info("PCOPhobosDesktop", $"[SaveLayout] Saving layout to: {_layoutPath}");
                PCLoggerPlugin.Info("PCOPhobosDesktop", $"[SaveLayout] Items: {_layout.Items.Count}");

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                var json = JsonConvert.SerializeObject(_layout, settings);

                PCLoggerPlugin.Info("PCOPhobosDesktop", $"[SaveLayout] JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");

                File.WriteAllText(_layoutPath, json);
                PCLoggerPlugin.Info("PCOPhobosDesktop", "[SaveLayout] Layout saved successfully");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PCOPhobosDesktop", $"[SaveLayout] Failed to save layout: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 更新网格布局（根据窗口大小调整行列数）
        /// </summary>
        private void UpdateGridLayout()
        {
            double availableWidth = DesktopScrollViewer.ActualWidth - 40; // 减去 Margin

            if (availableWidth <= 0)
                return;

            // 根据窗口宽度动态计算列数
            const double iconSize = 100; // 图标大小 + 边距
            int columns = Math.Max(3, (int)(availableWidth / iconSize));

            if (_layout.Columns != columns)
            {
                int oldColumns = _layout.Columns;
                _layout.Columns = columns;

                // 列数变化时重新排列图标
                if (columns < oldColumns)
                {
                    // 列数减少：重新排列超出范围的图标
                    ReflowIconsForNewColumns(columns);
                }
                else
                {
                    // 列数增加：重新紧凑排列所有图标以利用新空间
                    ReflowIconsCompact(columns);
                }

                RenderDesktop(playAnimation: true);
                SaveLayout();
            }
        }

        /// <summary>
        /// 当列数减少时，重新排列超出范围的图标
        /// </summary>
        private void ReflowIconsForNewColumns(int newColumns)
        {
            // 收集所有需要重新放置的图标（GridX >= newColumns）
            var itemsToReflow = _layout.Items.Where(item => item.GridX >= newColumns).ToList();

            if (itemsToReflow.Count == 0)
                return;

            // 构建已占用位置的集合
            var occupiedPositions = new HashSet<(int x, int y)>();
            foreach (var item in _layout.Items)
            {
                if (item.GridX < newColumns)
                {
                    occupiedPositions.Add((item.GridX, item.GridY));
                }
            }

            // 为每个需要重新放置的图标找到新位置
            foreach (var item in itemsToReflow)
            {
                var newPos = FindNextAvailablePosition(occupiedPositions, newColumns);
                item.GridX = newPos.x;
                item.GridY = newPos.y;
                occupiedPositions.Add(newPos);
            }
        }

        /// <summary>
        /// 当列数增加时，重新紧凑排列所有图标以利用新空间
        /// </summary>
        private void ReflowIconsCompact(int newColumns)
        {
            // 按照当前位置排序所有图标（先按行，再按列）
            var sortedItems = _layout.Items.OrderBy(item => item.GridY).ThenBy(item => item.GridX).ToList();

            // 重新分配位置，紧凑排列
            int currentRow = 0;
            int currentCol = 0;

            foreach (var item in sortedItems)
            {
                item.GridX = currentCol;
                item.GridY = currentRow;

                currentCol++;
                if (currentCol >= newColumns)
                {
                    currentCol = 0;
                    currentRow++;
                }
            }
        }

        /// <summary>
        /// 查找下一个可用的网格位置
        /// </summary>
        private (int x, int y) FindNextAvailablePosition(HashSet<(int x, int y)> occupiedPositions, int columns)
        {
            int row = 0;
            while (true)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (!occupiedPositions.Contains((col, row)))
                    {
                        return (col, row);
                    }
                }
                row++;
            }
        }

        /// <summary>
        /// 渲染桌面图标
        /// </summary>
        /// <param name="playAnimation">是否播放飞入动画（仅在窗口初次加载和取消搜索时为true）</param>
        public void RenderDesktop(bool playAnimation = false)
        {
            DesktopGrid.Children.Clear();
            DesktopGrid.RowDefinitions.Clear();
            DesktopGrid.ColumnDefinitions.Clear();

            // 计算需要的行数（根据最大 GridY）
            int maxY = 0;
            foreach (var item in _layout.Items)
            {
                if (item.GridY > maxY)
                    maxY = item.GridY;
            }
            int requiredRows = maxY + 1;

            // 至少显示足够填满可见区域的行数
            double availableHeight = DesktopScrollViewer.ActualHeight - 40;
            int visibleRows = Math.Max(2, (int)(availableHeight / 110)); // 增加单元格高度
            int totalRows = Math.Max(requiredRows, visibleRows);

            // 创建网格定义 - 固定行高以支持滚动
            for (int i = 0; i < totalRows; i++)
                DesktopGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(110) }); // 增加高度

            for (int i = 0; i < _layout.Columns; i++)
                DesktopGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // 渲染每个桌面项并收集用于动画
            var iconControls = new List<(Border control, int index)>();
            int iconIndex = 0;

            foreach (var item in _layout.Items)
            {
                if (item.GridX >= _layout.Columns)
                    continue;

                // 如果行超出当前定义，动态添加行
                while (item.GridY >= DesktopGrid.RowDefinitions.Count)
                {
                    DesktopGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(110) });
                }

                Border? iconControl = null;

                if (item is PluginDesktopItem pluginItem)
                {
                    if (_allPlugins.TryGetValue(pluginItem.PackageName, out var plugin))
                    {
                        iconControl = CreatePluginIcon(plugin);
                    }
                }
                else if (item is FolderDesktopItem folderItem)
                {
                    iconControl = CreateFolderIcon(folderItem);
                }
                else if (item is ShortcutDesktopItem shortcutItem)
                {
                    iconControl = CreateShortcutIcon(shortcutItem);
                }

                if (iconControl != null)
                {
                    Grid.SetRow(iconControl, item.GridY);
                    Grid.SetColumn(iconControl, item.GridX);
                    DesktopGrid.Children.Add(iconControl);
                    iconControls.Add((iconControl, iconIndex++));
                }
            }

            // 仅在指定场景播放飞入动画
            if (playAnimation)
            {
                AnimateIconsFlyIn(iconControls);
            }
        }

        /// <summary>
        /// 图标逐层飞入动画
        /// </summary>
        private void AnimateIconsFlyIn(List<(Border control, int index)> icons)
        {
            foreach (var (control, index) in icons)
            {
                // 设置初始状态
                control.Opacity = 0; // 动画开始前设置透明
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new TranslateTransform(0, 30));
                transformGroup.Children.Add(new ScaleTransform(0.8, 0.8));
                control.RenderTransform = transformGroup;
                control.RenderTransformOrigin = new Point(0.5, 0.5);

                // 计算延迟（基于行和列的位置实现逐层效果）
                int row = Grid.GetRow(control);
                int col = Grid.GetColumn(control);
                int delay = (row * _layout.Columns + col) * 30; // 每个图标延迟30ms

                // 创建动画
                var storyboard = new Storyboard();
                storyboard.BeginTime = TimeSpan.FromMilliseconds(delay);

                // 弹性缓动函数
                var elasticEase = PUAnimation.CreateElasticEase(EasingMode.EaseOut, 1, 5);
                var cubicEase = PUAnimation.CreateSmoothEase();

                // 透明度动画
                PUAnimation.AddOpacityAnimation(storyboard, control, 0, 1, 300, cubicEase);

                // Y轴位移动画（向上弹入）
                PUAnimation.AddTranslateYAnimation(storyboard, control, 30, 0, 400, elasticEase, 0,
                    "(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)");

                // 缩放动画
                PUAnimation.AddScaleXAnimation(storyboard, control, 0.8, 1, 350, elasticEase, 0,
                    "(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)");

                PUAnimation.AddScaleYAnimation(storyboard, control, 0.8, 1, 350, elasticEase, 0,
                    "(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)");

                storyboard.Begin();
            }
        }

        /// <summary>
        /// 创建插件图标控件
        /// </summary>
        private Border CreatePluginIcon(PluginDisplayItem plugin, int index = 0)
        {
            var border = new Border
            {
                Style = (Style)FindResource("DesktopIconStyle"),
                Tag = plugin
            };

            // 使用Grid布局，图标固定在顶部，文字在下方
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 88
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // 图标固定高度
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // 文字固定高度（两行）

            // 图标
            var iconBorder = new Border
            {
                Width = 64,
                Height = 64,
                Background = (SolidColorBrush)FindResource("Background3Brush"),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (plugin.Icon != null)
            {
                iconBorder.Child = new Image
                {
                    Source = plugin.Icon,
                    Width = 48,
                    Height = 48,
                    Stretch = Stretch.Uniform
                };
            }

            Grid.SetRow(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // 名称 - 限制两行，超出省略
            var fontSize = (double)FindResource("FontSizeSm");
            var nameText = new TextBlock
            {
                Text = plugin.Name,
                FontSize = fontSize,
                LineHeight = fontSize * 1.3, // 行高为字体大小的1.3倍
                MaxHeight = fontSize * 1.3 * 2, // 最多两行
                Width = 88, // 与容器同宽，确保文字能换行
                Foreground = (SolidColorBrush)FindResource("Foreground1Brush"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = CreateTextShadowEffect()
            };

            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            border.Child = grid;

            // 事件处理 - 左键按下（启动长按计时器）
            border.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    _mouseDownPosition = e.GetPosition(DesktopGrid);
                    _draggingPlugin = plugin;
                    _draggingIcon = border;
                    _draggingFolder = null;
                    _isDragging = false;
                    _isDraggingFromFolder = _currentOpenFolder != null;

                    // 启动长按计时器（500ms）
                    _longPressTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    _longPressTimer.Tick += (ts, te) =>
                    {
                        _longPressTimer?.Stop();
                        _isDragging = true;
                        StartDragging(border, plugin);
                    };
                    _longPressTimer.Start();

                    border.CaptureMouse();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseLeftButtonDown error: {ex.Message}");
                }
            };

            // 鼠标移动
            border.MouseMove += (s, e) =>
            {
                try
                {
                    if (_draggingIcon == border && border.IsMouseCaptured)
                    {
                        var currentPos = e.GetPosition(DesktopGrid);
                        var distance = (currentPos - _mouseDownPosition).Length;

                        // 如果移动超过阈值，取消长按
                        if (distance > 10 && !_isDragging)
                        {
                            CancelDragging();
                        }
                        else if (_isDragging)
                        {
                            // 更新拖拽预览位置
                            UpdateDragPreview(currentPos);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseMove error: {ex.Message}");
                }
            };

            // 左键抬起
            border.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    border.ReleaseMouseCapture();

                    if (_isDragging)
                    {
                        // 完成拖拽
                        CompleteDragging(e.GetPosition(DesktopGrid));
                    }
                    else if (_longPressTimer?.IsEnabled == true)
                    {
                        // 短按 - 启动插件
                        _longPressTimer.Stop();
                        LaunchPlugin(plugin);
                    }

                    CancelDragging();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseLeftButtonUp error: {ex.Message}");
                }
            };

            // 右键菜单
            border.MouseRightButtonDown += (s, e) =>
            {
                try
                {
                    CancelDragging();
                    ShowPluginContextMenu(plugin, border);
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseRightButtonDown error: {ex.Message}");
                }
            };

            // 悬停效果
            border.MouseEnter += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconBorder, 1.1, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseEnter error: {ex.Message}");
                }
            };

            border.MouseLeave += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconBorder, 1.0, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreatePluginIcon] MouseLeave error: {ex.Message}");
                }
            };

            return border;
        }

        /// <summary>
        /// 创建文件夹图标控件
        /// </summary>
        private Border CreateFolderIcon(FolderDesktopItem folder)
        {
            var border = new Border
            {
                Style = (Style)FindResource("FolderIconStyle"),
                Tag = folder
            };

            // 使用Grid布局，图标固定在顶部，文字在下方
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 88
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // 图标固定高度
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // 文字固定高度

            // 文件夹图标
            var iconBorder = new Border
            {
                Width = 64,
                Height = 64,
                Background = (SolidColorBrush)FindResource("PrimaryBrush"),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 简单的文件夹图标（可以替换为更复杂的设计）
            var folderIcon = new TextBlock
            {
                Text = "📁",
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBorder.Child = folderIcon;
            Grid.SetRow(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // 文件夹名称 - 限制两行，超出省略
            var fontSize = (double)FindResource("FontSizeSm");
            var nameText = new TextBlock
            {
                Text = folder.Name,
                FontSize = fontSize,
                LineHeight = fontSize * 1.3, // 行高为字体大小的1.3倍
                MaxHeight = fontSize * 1.3 * 2, // 最多两行
                Width = 88, // 与容器同宽，确保文字能换行
                Foreground = (SolidColorBrush)FindResource("Foreground1Brush"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = CreateTextShadowEffect()
            };

            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            border.Child = grid;

            // 事件处理 - 左键按下（启动长按计时器）
            border.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    _mouseDownPosition = e.GetPosition(DesktopGrid);
                    _draggingFolder = folder;
                    _draggingIcon = border;
                    _draggingPlugin = null;
                    _isDragging = false;
                    _isDraggingFromFolder = false;

                    // 启动长按计时器（500ms）
                    _longPressTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    _longPressTimer.Tick += (ts, te) =>
                    {
                        _longPressTimer?.Stop();
                        _isDragging = true;
                        StartFolderDragging(border, folder);
                    };
                    _longPressTimer.Start();

                    border.CaptureMouse();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseLeftButtonDown error: {ex.Message}");
                }
            };

            // 鼠标移动
            border.MouseMove += (s, e) =>
            {
                try
                {
                    if (_draggingIcon == border && border.IsMouseCaptured)
                    {
                        var currentPos = e.GetPosition(DesktopGrid);
                        var distance = (currentPos - _mouseDownPosition).Length;

                        // 如果移动超过阈值，取消长按
                        if (distance > 10 && !_isDragging)
                        {
                            CancelDragging();
                        }
                        else if (_isDragging)
                        {
                            // 更新拖拽预览位置
                            UpdateDragPreview(currentPos);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseMove error: {ex.Message}");
                }
            };

            // 左键抬起
            border.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    border.ReleaseMouseCapture();

                    if (_isDragging)
                    {
                        // 完成文件夹拖拽
                        CompleteFolderDragging(e.GetPosition(DesktopGrid));
                    }
                    else if (_longPressTimer?.IsEnabled == true)
                    {
                        // 短按 - 打开文件夹
                        _longPressTimer.Stop();
                        OpenFolder(folder);
                    }

                    CancelDragging();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseLeftButtonUp error: {ex.Message}");
                }
            };

            border.MouseRightButtonDown += (s, e) =>
            {
                try
                {
                    CancelDragging();
                    ShowFolderContextMenu(folder, border);
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseRightButtonDown error: {ex.Message}");
                }
            };

            // 悬停效果
            border.MouseEnter += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconBorder, 1.1, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseEnter error: {ex.Message}");
                }
            };

            border.MouseLeave += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconBorder, 1.0, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateFolderIcon] MouseLeave error: {ex.Message}");
                }
            };

            return border;
        }

        /// <summary>
        /// 创建快捷方式图标控件
        /// </summary>
        private Border CreateShortcutIcon(ShortcutDesktopItem shortcut)
        {
            var border = new Border
            {
                Style = (Style)FindResource("DesktopIconStyle"),
                Tag = shortcut
            };

            // 使用Grid布局，图标固定在顶部，文字在下方
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 88,
                Height = 100 // 64 (图标) + 36 (文字) = 100，与插件图标一致
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // 图标固定高度
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // 文字固定高度

            // 图标容器（带右下角插件标识）
            var iconContainer = new Grid
            {
                Width = 68, // 64 + 4 用于容纳超出的overlayBorder
                Height = 68,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, -4) // 补偿超出部分
            };

            // 主图标
            var iconBorder = new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(12),
                Background = (SolidColorBrush)FindResource("Background3Brush"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            var iconImage = new Image
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 设置图标
            try
            {
                // 优先使用自定义图标
                if (!string.IsNullOrEmpty(shortcut.CustomIconPath) && System.IO.File.Exists(shortcut.CustomIconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(shortcut.CustomIconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    iconImage.Source = bitmap;
                }
                // 否则使用目标插件的图标
                else if (_allPlugins.TryGetValue(shortcut.TargetPackageName, out var targetPlugin))
                {
                    iconImage.Source = targetPlugin.Icon;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] Failed to load icon: {ex.Message}");
            }

            iconBorder.Child = iconImage;
            iconContainer.Children.Add(iconBorder);

            // 右下角的插件标识小图标
            if (_allPlugins.TryGetValue(shortcut.TargetPackageName, out var plugin))
            {
                var overlayBorder = new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(4),
                    Background = (SolidColorBrush)FindResource("Background2Brush"),
                    BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var overlayImage = new Image
                {
                    Width = 14,
                    Height = 14,
                    Source = plugin.Icon,
                    Stretch = Stretch.Uniform
                };

                overlayBorder.Child = overlayImage;
                iconContainer.Children.Add(overlayBorder);
            }

            Grid.SetRow(iconContainer, 0);
            grid.Children.Add(iconContainer);

            // 快捷方式名称 - 限制两行，超出省略
            var fontSize = (double)FindResource("FontSizeSm");
            var nameText = new TextBlock
            {
                Text = shortcut.Name,
                FontSize = fontSize,
                LineHeight = fontSize * 1.3, // 行高为字体大小的1.3倍
                MaxHeight = fontSize * 1.3 * 2, // 最多两行
                Width = 88, // 与容器同宽，确保文字能换行
                Foreground = (SolidColorBrush)FindResource("Foreground1Brush"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Effect = CreateTextShadowEffect()
            };

            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            border.Child = grid;

            // 事件处理 - 左键按下（启动长按计时器）
            border.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    _mouseDownPosition = e.GetPosition(DesktopGrid);
                    _draggingIcon = border;
                    _draggingPlugin = null;
                    _draggingFolder = null;
                    _isDragging = false;
                    _isDraggingFromFolder = false;

                    // 启动长按计时器（500ms）
                    _longPressTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    _longPressTimer.Tick += (ts, te) =>
                    {
                        _longPressTimer?.Stop();
                        _isDragging = true;
                        StartShortcutDragging(border, shortcut);
                    };
                    _longPressTimer.Start();

                    border.CaptureMouse();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseLeftButtonDown error: {ex.Message}");
                }
            };

            // 鼠标移动
            border.MouseMove += (s, e) =>
            {
                try
                {
                    if (_draggingIcon == border && border.IsMouseCaptured)
                    {
                        var currentPos = e.GetPosition(DesktopGrid);
                        var distance = (currentPos - _mouseDownPosition).Length;

                        // 如果移动超过阈值，取消长按
                        if (distance > 10 && !_isDragging)
                        {
                            CancelDragging();
                        }
                        else if (_isDragging)
                        {
                            // 更新拖拽预览位置
                            UpdateDragPreview(currentPos);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseMove error: {ex.Message}");
                }
            };

            // 左键抬起
            border.MouseLeftButtonUp += (s, e) =>
            {
                try
                {
                    border.ReleaseMouseCapture();

                    if (_isDragging)
                    {
                        // 完成快捷方式拖拽
                        CompleteShortcutDragging(e.GetPosition(DesktopGrid), shortcut);
                    }
                    else if (_longPressTimer?.IsEnabled == true)
                    {
                        // 短按 - 运行快捷方式
                        _longPressTimer.Stop();
                        RunShortcut(shortcut);
                    }

                    CancelDragging();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseLeftButtonUp error: {ex.Message}");
                }
            };

            border.MouseRightButtonDown += (s, e) =>
            {
                try
                {
                    CancelDragging();
                    ShowShortcutContextMenu(shortcut, border);
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseRightButtonDown error: {ex.Message}");
                }
            };

            // 悬停效果 - 对整个iconContainer进行缩放，这样主图标和右下角小图标会一起动
            border.MouseEnter += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconContainer, 1.1, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseEnter error: {ex.Message}");
                }
            };

            border.MouseLeave += (s, e) =>
            {
                try
                {
                    if (!_isDragging)
                        AnimateIconScale(iconContainer, 1.0, 150);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateShortcutIcon] MouseLeave error: {ex.Message}");
                }
            };

            return border;
        }

        /// <summary>
        /// 运行快捷方式
        /// </summary>
        private async void RunShortcut(ShortcutDesktopItem shortcut)
        {
            try
            {
                var args = shortcut.ParseArguments();
                System.Diagnostics.Debug.WriteLine($"[RunShortcut] Running {shortcut.TargetPackageName} with {args.Length} arguments");

                await PMPlugin.Instance.Launch(shortcut.TargetPackageName, args);
                HideToTray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunShortcut] Error: {ex.Message}");
                await Service.Arcusrix.PSDialogService.Warning(
                    ex.Message,
                    Components.DesktopLocalization.Get(Components.DesktopLocalization.Dialog_LaunchError),
                    this);
            }
        }

        /// <summary>
        /// 开始拖拽快捷方式
        /// </summary>
        private void StartShortcutDragging(Border iconBorder, ShortcutDesktopItem shortcut)
        {
            _draggingIcon = iconBorder;

            // 获取图标
            ImageSource? icon = null;
            if (!string.IsNullOrEmpty(shortcut.CustomIconPath) && System.IO.File.Exists(shortcut.CustomIconPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(shortcut.CustomIconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    icon = bitmap;
                }
                catch { }
            }
            else if (_allPlugins.TryGetValue(shortcut.TargetPackageName, out var plugin))
            {
                icon = plugin.Icon;
            }

            CreateDragPreview(shortcut.Name, icon, false);
            iconBorder.Opacity = 0.3;
        }

        /// <summary>
        /// 完成快捷方式拖拽
        /// </summary>
        private void CompleteShortcutDragging(Point dropPosition, ShortcutDesktopItem shortcut)
        {
            RemoveDragPreview();

            // 计算目标网格位置
            int targetCol = (int)(dropPosition.X / (DesktopGrid.ActualWidth / _layout.Columns));
            int targetRow = (int)(dropPosition.Y / 110);

            targetCol = Math.Max(0, Math.Min(targetCol, _layout.Columns - 1));
            targetRow = Math.Max(0, targetRow);

            // 检查目标位置是否已被占用
            var existingItem = _layout.Items.FirstOrDefault(i => i.GridX == targetCol && i.GridY == targetRow);
            if (existingItem != null && existingItem != shortcut)
            {
                // 交换位置
                existingItem.GridX = shortcut.GridX;
                existingItem.GridY = shortcut.GridY;
            }

            // 更新快捷方式位置
            shortcut.GridX = targetCol;
            shortcut.GridY = targetRow;

            SaveLayout();
            RenderDesktop();
        }

        /// <summary>
        /// 显示快捷方式右键菜单
        /// </summary>
        private void ShowShortcutContextMenu(ShortcutDesktopItem shortcut, Border iconBorder)
        {
            var items = new List<DesktopMenuItem>
            {
                new DesktopMenuItem
                {
                    Id = "open",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Shortcut_Open),
                    Icon = "▶",
                    OnClick = () => RunShortcut(shortcut)
                },
                new DesktopMenuItem
                {
                    Id = "edit",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Shortcut_Edit),
                    Icon = "✏",
                    OnClick = () => EditShortcut(shortcut)
                },
                new DesktopMenuItem { IsSeparator = true },
                new DesktopMenuItem
                {
                    Id = "delete",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Shortcut_Delete),
                    Icon = "🗑",
                    OnClick = () => DeleteShortcut(shortcut)
                }
            };

            var position = iconBorder.TransformToAncestor(MainBorder).Transform(new Point(iconBorder.ActualWidth, 0));
            DesktopMenu.Show(items, position);
        }

        /// <summary>
        /// 注册单个桌面项的快捷键
        /// </summary>
        private async void RegisterItemHotkey(DesktopItem item)
        {
            if (string.IsNullOrEmpty(item.Hotkey))
                return;

            var hotkeyInfo = Manager.Hotkey.HotkeyInfo.Parse(item.Hotkey);
            if (hotkeyInfo == null)
                return;

            hotkeyInfo.Id = item.Id;
            hotkeyInfo.Callback = () => ExecuteItemAction(item);

            if (!Manager.Hotkey.PMHotkey.Instance.Register(hotkeyInfo))
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to register hotkey: {item.Hotkey}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
            }
        }

        /// <summary>
        /// 执行桌面项的操作（由快捷键触发）
        /// </summary>
        private void ExecuteItemAction(DesktopItem item)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (item)
                    {
                        case PluginDesktopItem pluginItem:
                            if (_allPlugins.TryGetValue(pluginItem.PackageName, out var plugin))
                            {
                                LaunchPlugin(plugin);
                            }
                            break;

                        case ShortcutDesktopItem shortcut:
                            RunShortcut(shortcut);
                            break;

                        case FolderDesktopItem folder:
                            // 显示桌面并打开文件夹
                            ShowFromTray();
                            OpenFolder(folder);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("Desktop", ex.Message);
                }

            });
        }

        /// <summary>
        /// 注册所有桌面项的快捷键
        /// </summary>
        private void RegisterAllHotkeys()
        {
            // 初始化快捷键管理器
            Manager.Hotkey.PMHotkey.Instance.Initialize(this);

            foreach (var item in _layout.Items)
            {
                if (!string.IsNullOrEmpty(item.Hotkey))
                {
                    RegisterItemHotkey(item);
                }
            }

            PCLoggerPlugin.Info("PCOPhobosDesktop", $"[RegisterAllHotkeys] Registered hotkeys for {_layout.Items.Count(i => !string.IsNullOrEmpty(i.Hotkey))} items");
        }

        /// <summary>
        /// 注销所有快捷键
        /// </summary>
        private void UnregisterAllHotkeys()
        {
            Manager.Hotkey.PMHotkey.Instance.UnregisterAll();
        }

        /// <summary>
        /// 编辑快捷方式
        /// </summary>
        private void EditShortcut(ShortcutDesktopItem shortcut)
        {
            var dialog = new PCOShortcutEditDialog(_allPlugins, shortcut)
            {
                Owner = this
            };
            RegisterChildWindow(dialog);

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                var result = dialog.Result;

                // 更新快捷方式属性
                shortcut.Name = result.Name;
                shortcut.TargetPackageName = result.TargetPackageName;
                shortcut.Arguments = result.Arguments;
                shortcut.CustomIconPath = result.CustomIconPath;

                // 处理热键变更
                if (dialog.HotkeyChanged || shortcut.Hotkey != result.Hotkey)
                {
                    // 注销旧热键
                    Manager.Hotkey.PMHotkey.Instance.Unregister(shortcut.Id);

                    // 更新热键
                    shortcut.Hotkey = result.Hotkey;

                    // 注册新热键
                    if (!string.IsNullOrEmpty(shortcut.Hotkey))
                    {
                        RegisterItemHotkey(shortcut);
                    }
                }

                SaveLayout();
                RenderDesktop();
            }
        }

        /// <summary>
        /// 删除快捷方式
        /// </summary>
        private void DeleteShortcut(ShortcutDesktopItem shortcut)
        {
            // 注销热键
            if (!string.IsNullOrEmpty(shortcut.Hotkey))
            {
                Manager.Hotkey.PMHotkey.Instance.Unregister(shortcut.Id);
            }

            _layout.Items.Remove(shortcut);
            SaveLayout();
            RenderDesktop();
        }

        /// <summary>
        /// 图标缩放动画
        /// </summary>
        private void AnimateIconScale(FrameworkElement icon, double scale, int duration)
        {
            PUAnimation.ScaleTo(icon, scale, duration, PUAnimation.CreateSmoothEase());
        }

        /// <summary>
        /// 显示插件右键菜单
        /// </summary>
        private void ShowPluginContextMenu(PluginDisplayItem plugin, Border icon)
        {
            var items = new List<DesktopMenuItem>
            {
                new DesktopMenuItem
                {
                    Id = "open",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_Open),
                    Icon = "▶",
                    OnClick = () => LaunchPlugin(plugin)
                },
                new DesktopMenuItem
                {
                    Id = "info",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_Info),
                    Icon = "ℹ",
                    OnClick = () => ShowPluginInfo(plugin)
                },
                new DesktopMenuItem
                {
                    Id = "settings",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_Settings),
                    Icon = "⚙",
                    OnClick = () => OpenPluginSettings(plugin)
                }
            };

            // 卸载（非系统插件）
            if (!plugin.IsSystemPlugin)
            {
                items.Add(new DesktopMenuItem { IsSeparator = true });
                items.Add(new DesktopMenuItem
                {
                    Id = "uninstall",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_Uninstall),
                    Icon = "🗑",
                    IsDanger = true,
                    OnClick = () => UninstallPlugin(plugin)
                });
            }

            var position = icon.TransformToAncestor(MainBorder).Transform(new Point(icon.ActualWidth, 0));
            DesktopMenu.Show(items, position);
        }

        /// <summary>
        /// 显示文件夹右键菜单
        /// </summary>
        private void ShowFolderContextMenu(FolderDesktopItem folder, Border icon)
        {
            var items = new List<DesktopMenuItem>
            {
                new DesktopMenuItem
                {
                    Id = "open",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Folder_Open),
                    Icon = "📂",
                    OnClick = () => OpenFolder(folder)
                },
                new DesktopMenuItem
                {
                    Id = "rename",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Folder_Rename),
                    Icon = "✏",
                    OnClick = () => RenameFolder(folder)
                },
                new DesktopMenuItem { IsSeparator = true },
                new DesktopMenuItem
                {
                    Id = "delete",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Folder_Delete),
                    Icon = "🗑",
                    IsDanger = true,
                    OnClick = () => DeleteFolder(folder)
                }
            };

            var position = icon.TransformToAncestor(MainBorder).Transform(new Point(icon.ActualWidth, 0));
            DesktopMenu.Show(items, position);
        }

        /// <summary>
        /// 桌面空白区域右键菜单
        /// </summary>
        private void DesktopGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var items = new List<DesktopMenuItem>
            {
                new DesktopMenuItem
                {
                    Id = "toggleFullscreen",
                    Text = WindowState == WindowState.Maximized
                        ? DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_ExitFullscreen)
                        : DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_Fullscreen),
                    Icon = WindowState == WindowState.Maximized ? "🗗" : "🗖",
                    OnClick = () => ToggleMaximize()
                },
                new DesktopMenuItem
                {
                    Id = "settings",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_Settings),
                    Icon = "⚙",
                    OnClick = () => OpenDesktopSettings()
                },
                new DesktopMenuItem { IsSeparator = true },
                new DesktopMenuItem
                {
                    Id = "newFolder",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_NewFolder),
                    Icon = "📁",
                    OnClick = () => CreateNewFolder()
                },
                new DesktopMenuItem
                {
                    Id = "newShortcut",
                    Text = DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_NewShortcut),
                    Icon = "🔗",
                    OnClick = () => CreateNewShortcut()
                }
            };

            var position = e.GetPosition(MainBorder);
            DesktopMenu.Show(items, position);
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        private void OpenFolder(FolderDesktopItem folder)
        {
            _currentOpenFolder = folder;
            FolderTitle.Text = folder.Name;
            FolderItemsControl.Items.Clear();

            foreach (var packageName in folder.PluginPackageNames)
            {
                if (_allPlugins.TryGetValue(packageName, out var plugin))
                {
                    var iconControl = CreatePluginIcon(plugin);
                    iconControl.Width = 100;
                    FolderItemsControl.Items.Add(iconControl);
                }
            }

            // 设置初始状态用于动画
            FolderOverlay.Opacity = 0;
            FolderPanel.RenderTransform = new TransformGroup
            {
                Children = { new ScaleTransform(0.8, 0.8), new TranslateTransform(0, 20) }
            };
            FolderPanel.RenderTransformOrigin = new Point(0.5, 0.5);
            FolderPanel.Opacity = 0;

            FolderOverlay.Visibility = Visibility.Visible;

            // 播放打开动画
            PlayFolderOpenAnimation();
        }

        /// <summary>
        /// 播放文件夹打开动画
        /// </summary>
        private void PlayFolderOpenAnimation()
        {
            var storyboard = new Storyboard();

            var elasticEase = PUAnimation.CreateElasticEase(EasingMode.EaseOut, 1, 6);
            var cubicEase = PUAnimation.CreateSmoothEase();

            // 遮罩淡入
            PUAnimation.AddOpacityAnimation(storyboard, FolderOverlay, 0, 1, 200, cubicEase);

            // 面板淡入
            PUAnimation.AddOpacityAnimation(storyboard, FolderPanel, 0, 1, 250, cubicEase);

            // 面板缩放X
            PUAnimation.AddScaleXAnimation(storyboard, FolderPanel, 0.8, 1, 350, elasticEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            // 面板缩放Y
            PUAnimation.AddScaleYAnimation(storyboard, FolderPanel, 0.8, 1, 350, elasticEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            // 面板上移
            PUAnimation.AddTranslateYAnimation(storyboard, FolderPanel, 20, 0, 350, elasticEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");

            storyboard.Begin();
        }

        /// <summary>
        /// 关闭文件夹（带动画）
        /// </summary>
        private void CloseFolder()
        {
            var storyboard = new Storyboard();

            var cubicEase = PUAnimation.CreateSmoothEase(EasingMode.EaseIn);

            // 遮罩淡出
            PUAnimation.AddOpacityAnimation(storyboard, FolderOverlay, 1, 0, 150, cubicEase);

            // 面板淡出
            PUAnimation.AddOpacityAnimation(storyboard, FolderPanel, 1, 0, 150, cubicEase);

            // 面板缩小
            PUAnimation.AddScaleXAnimation(storyboard, FolderPanel, 1, 0.9, 150, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            PUAnimation.AddScaleYAnimation(storyboard, FolderPanel, 1, 0.9, 150, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            storyboard.Completed += (s, e) =>
            {
                FolderOverlay.Visibility = Visibility.Collapsed;
                _currentOpenFolder = null;
            };

            storyboard.Begin();
        }

        /// <summary>
        /// 关闭文件夹
        /// </summary>
        private void FolderOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == FolderOverlay)
            {
                CloseFolder();
            }
        }

        /// <summary>
        /// 创建新文件夹
        /// </summary>
        private void CreateNewFolder()
        {
            var folderName = PCOInputDialog.Show(this,
                DesktopLocalization.Get(DesktopLocalization.Dialog_NewFolder),
                DesktopLocalization.Get(DesktopLocalization.Dialog_NewFolder_Prompt),
                DesktopLocalization.Get(DesktopLocalization.Menu_Desktop_NewFolder));

            if (!string.IsNullOrWhiteSpace(folderName))
            {
                // 查找空闲位置
                int gridX = 0, gridY = 0;
                bool positionFound = false;

                for (int y = 0; y < _layout.Rows && !positionFound; y++)
                {
                    for (int x = 0; x < _layout.Columns && !positionFound; x++)
                    {
                        if (!_layout.Items.Any(item => item.GridX == x && item.GridY == y))
                        {
                            gridX = x;
                            gridY = y;
                            positionFound = true;
                        }
                    }
                }

                var folder = new FolderDesktopItem
                {
                    Name = folderName,
                    GridX = gridX,
                    GridY = gridY
                };

                _layout.Items.Add(folder);
                _layout.Folders.Add(folder);
                RenderDesktop();
                SaveLayout();
            }
        }

        /// <summary>
        /// 创建新快捷方式
        /// </summary>
        private void CreateNewShortcut()
        {
            var dialog = new PCOShortcutEditDialog(_allPlugins, null)
            {
                Owner = this
            };
            RegisterChildWindow(dialog);

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                var result = dialog.Result;

                // 查找空闲位置
                int gridX = 0, gridY = 0;
                bool positionFound = false;

                for (int y = 0; y < _layout.Rows && !positionFound; y++)
                {
                    for (int x = 0; x < _layout.Columns && !positionFound; x++)
                    {
                        if (!_layout.Items.Any(item => item.GridX == x && item.GridY == y))
                        {
                            gridX = x;
                            gridY = y;
                            positionFound = true;
                        }
                    }
                }

                // 如果没找到位置，放在新行
                if (!positionFound)
                {
                    gridY = _layout.Items.Max(i => i.GridY) + 1;
                    gridX = 0;
                }

                result.GridX = gridX;
                result.GridY = gridY;

                _layout.Items.Add(result);

                // 注册热键
                if (!string.IsNullOrEmpty(result.Hotkey))
                {
                    RegisterItemHotkey(result);
                }

                RenderDesktop();
                SaveLayout();
            }
        }

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        private void RenameFolder(FolderDesktopItem folder)
        {
            var newName = PCOInputDialog.Show(this,
                DesktopLocalization.Get(DesktopLocalization.Dialog_RenameFolder),
                DesktopLocalization.Get(DesktopLocalization.Dialog_RenameFolder_Prompt),
                folder.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != folder.Name)
            {
                folder.Name = newName;
                RenderDesktop();
                SaveLayout();
            }
        }

        /// <summary>
        /// 删除文件夹（将文件夹内的插件释放回桌面）
        /// </summary>
        private void DeleteFolder(FolderDesktopItem folder)
        {
            // 获取文件夹的位置，用于放置第一个释放的插件
            int folderX = folder.GridX;
            int folderY = folder.GridY;

            // 先从布局中移除文件夹
            _layout.Items.Remove(folder);
            _layout.Folders.Remove(folder);

            // 将文件夹内的插件释放回桌面
            bool firstPlugin = true;
            foreach (var packageName in folder.PluginPackageNames)
            {
                if (_allPlugins.ContainsKey(packageName))
                {
                    (int X, int Y) position;
                    if (firstPlugin)
                    {
                        // 第一个插件放在文件夹原来的位置
                        position = (folderX, folderY);
                        firstPlugin = false;
                    }
                    else
                    {
                        // 其他插件查找空位
                        position = FindFirstEmptyPosition();
                    }

                    _layout.Items.Add(new PluginDesktopItem
                    {
                        PackageName = packageName,
                        GridX = position.X,
                        GridY = position.Y
                    });
                }
            }

            RenderDesktop();
            SaveLayout();
        }

        /// <summary>
        /// 启动插件
        /// </summary>
        private async void LaunchPlugin(PluginDisplayItem plugin)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LaunchPlugin] Launching plugin: {plugin.PackageName}");
                PluginClicked?.Invoke(this, plugin.PackageName);
                await PMPlugin.Instance.Launch(plugin.PackageName);
                HideToTray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LaunchPlugin] Error: {ex.Message}");
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to launch plugin: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_LaunchError),
                    this);
            }
        }

        /// <summary>
        /// 创建拖拽预览图标
        /// </summary>
        private void CreateDragPreview(string name, ImageSource? icon, bool isFolder)
        {
            try
            {
                // 创建预览容器 - 使用Canvas布局，让文字可以自由延展
                _dragPreview = new Border
                {
                    Width = 80,
                    Height = 80, // 只包含图标高度
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsHitTestVisible = false
                };

                var canvas = new Canvas
                {
                    Width = 80,
                    Height = 80
                };

                // 图标
                var iconBorder = new Border
                {
                    Width = 64,
                    Height = 64,
                    Background = isFolder
                        ? (SolidColorBrush)FindResource("PrimaryBrush")
                        : (SolidColorBrush)FindResource("Background3Brush"),
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(0)
                };

                // 居中图标
                Canvas.SetLeft(iconBorder, 8); // (80 - 64) / 2
                Canvas.SetTop(iconBorder, 0);

                if (isFolder)
                {
                    iconBorder.Child = new TextBlock
                    {
                        Text = "📁",
                        FontSize = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                else if (icon != null)
                {
                    iconBorder.Child = new Image
                    {
                        Source = icon,
                        Width = 48,
                        Height = 48,
                        Stretch = Stretch.Uniform
                    };
                }

                canvas.Children.Add(iconBorder);

                // 名称 - 放在图标下方，允许自由延展
                var nameText = new TextBlock
                {
                    Text = name,
                    FontSize = (double)FindResource("FontSizeSm"),
                    Foreground = (SolidColorBrush)FindResource("Foreground1Brush"),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 80
                };

                Canvas.SetLeft(nameText, 0);
                Canvas.SetTop(nameText, 68); // 图标高度 + 4px间距

                canvas.Children.Add(nameText);

                _dragPreview.Child = canvas;

                // 添加到DesktopGrid
                _dragPreview.RenderTransform = new TranslateTransform();
                Panel.SetZIndex(_dragPreview, 1000);
                DesktopGrid.Children.Add(_dragPreview);

                // 更新位置
                UpdateDragPreview(_mouseDownPosition);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateDragPreview] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新拖拽预览位置
        /// </summary>
        private void UpdateDragPreview(Point position)
        {
            try
            {
                if (_dragPreview?.RenderTransform is TranslateTransform transform)
                {
                    transform.X = position.X - 40; // 居中（80 / 2）
                    transform.Y = position.Y - 32; // 图标中心偏移（64 / 2）
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateDragPreview] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除拖拽预览
        /// </summary>
        private void RemoveDragPreview()
        {
            try
            {
                if (_dragPreview != null)
                {
                    DesktopGrid.Children.Remove(_dragPreview);
                    _dragPreview = null;
                }

                // 移除遮罩
                if (_dragOverlay != null)
                {
                    if (_draggingIcon != null)
                    {
                        // 支持Grid和StackPanel两种布局
                        Border? iconBorder = null;
                        if (_draggingIcon.Child is Grid grid && grid.Children.Count > 0)
                        {
                            iconBorder = grid.Children[0] as Border;
                        }
                        else if (_draggingIcon.Child is StackPanel stackPanel && stackPanel.Children.Count > 0)
                        {
                            iconBorder = stackPanel.Children[0] as Border;
                        }

                        if (iconBorder != null && iconBorder.Child is Grid overlayGrid)
                        {
                            var overlay = overlayGrid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "DragOverlay");
                            if (overlay != null)
                            {
                                overlayGrid.Children.Remove(overlay);
                            }
                        }
                    }
                    _dragOverlay = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoveDragPreview] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加暗色遮罩到图标
        /// </summary>
        private void AddDarkOverlay(Border iconBorder)
        {
            try
            {
                // 获取原有内容
                var originalChild = iconBorder.Child;

                // 创建Grid来容纳原内容和遮罩
                var grid = new Grid();
                if (originalChild != null)
                {
                    iconBorder.Child = null;
                    grid.Children.Add(originalChild as UIElement);
                }

                // 创建暗色遮罩
                _dragOverlay = new Border
                {
                    Name = "DragOverlay",
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    CornerRadius = new CornerRadius(12)
                };
                grid.Children.Add(_dragOverlay);

                iconBorder.Child = grid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddDarkOverlay] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除暗色遮罩
        /// </summary>
        private void RemoveDarkOverlay(Border iconBorder)
        {
            try
            {
                if (iconBorder.Child is Grid grid)
                {
                    var overlay = grid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "DragOverlay");
                    if (overlay != null)
                    {
                        grid.Children.Remove(overlay);
                    }

                    // 恢复原内容
                    if (grid.Children.Count == 1)
                    {
                        var originalChild = grid.Children[0];
                        grid.Children.Clear();
                        iconBorder.Child = originalChild;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoveDarkOverlay] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始拖拽插件
        /// </summary>
        private void StartDragging(Border icon, PluginDisplayItem plugin)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[StartDragging] Plugin: {plugin.PackageName}");

                // 获取图标Border并添加遮罩（支持Grid和StackPanel两种布局）
                Border? iconBorder = null;
                if (icon.Child is Grid grid && grid.Children.Count > 0)
                {
                    iconBorder = grid.Children[0] as Border;
                }
                else if (icon.Child is StackPanel stackPanel && stackPanel.Children.Count > 0)
                {
                    iconBorder = stackPanel.Children[0] as Border;
                }

                if (iconBorder != null)
                {
                    AddDarkOverlay(iconBorder);
                }

                // 创建拖拽预览
                CreateDragPreview(plugin.Name, plugin.Icon, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StartDragging] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始拖拽文件夹
        /// </summary>
        private void StartFolderDragging(Border icon, FolderDesktopItem folder)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[StartFolderDragging] Folder: {folder.Name}");

                // 获取图标Border并添加遮罩
                var stackPanel = icon.Child as StackPanel;
                if (stackPanel != null && stackPanel.Children.Count > 0)
                {
                    var iconBorder = stackPanel.Children[0] as Border;
                    if (iconBorder != null)
                    {
                        AddDarkOverlay(iconBorder);
                    }
                }

                // 创建拖拽预览
                CreateDragPreview(folder.Name, null, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StartFolderDragging] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消拖拽
        /// </summary>
        private void CancelDragging()
        {
            try
            {
                _longPressTimer?.Stop();
                _longPressTimer = null;

                // 移除预览和遮罩
                RemoveDragPreview();

                if (_draggingIcon != null)
                {
                    // 移除图标遮罩（支持Grid和StackPanel两种布局）
                    Border? iconBorder = null;
                    if (_draggingIcon.Child is Grid grid && grid.Children.Count > 0)
                    {
                        iconBorder = grid.Children[0] as Border;
                    }
                    else if (_draggingIcon.Child is StackPanel stackPanel && stackPanel.Children.Count > 0)
                    {
                        iconBorder = stackPanel.Children[0] as Border;
                    }

                    if (iconBorder != null)
                    {
                        RemoveDarkOverlay(iconBorder);
                    }
                    _draggingIcon.ReleaseMouseCapture();
                }

                _draggingIcon = null;
                _draggingPlugin = null;
                _draggingFolder = null;
                _isDragging = false;
                _isDraggingFromFolder = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CancelDragging] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 完成插件拖拽
        /// </summary>
        private void CompleteDragging(Point dropPosition)
        {
            try
            {
                if (_draggingPlugin == null || _draggingIcon == null)
                    return;

                System.Diagnostics.Debug.WriteLine($"[CompleteDragging] Drop at: ({dropPosition.X}, {dropPosition.Y}), FromFolder: {_isDraggingFromFolder}");

                // 如果是从文件夹内拖出
                if (_isDraggingFromFolder && _currentOpenFolder != null)
                {
                    // 检查是否拖到文件夹弹出窗口外
                    var folderPanel = FolderPanel;
                    if (folderPanel != null)
                    {
                        var panelPos = folderPanel.TransformToAncestor(this).Transform(new Point(0, 0));
                        var panelRect = new Rect(panelPos, new Size(folderPanel.ActualWidth, folderPanel.ActualHeight));

                        // 获取鼠标在窗口中的位置
                        var windowPos = Mouse.GetPosition(this);

                        if (!panelRect.Contains(windowPos))
                        {
                            // 从文件夹移出到桌面
                            RemovePluginFromFolder(_draggingPlugin, _currentOpenFolder);
                            return;
                        }
                    }
                }
                else
                {
                    // 检查是否拖到文件夹上
                    foreach (var child in DesktopGrid.Children)
                    {
                        if (child is Border folderBorder && folderBorder.Tag is FolderDesktopItem folder && folderBorder != _draggingIcon)
                        {
                            var folderPos = folderBorder.TransformToAncestor(DesktopGrid).Transform(new Point(0, 0));
                            var folderRect = new Rect(folderPos, new Size(folderBorder.ActualWidth, folderBorder.ActualHeight));

                            if (folderRect.Contains(dropPosition))
                            {
                                // 添加到文件夹
                                AddPluginToFolder(_draggingPlugin, folder);
                                return;
                            }
                        }
                    }

                    // 计算目标网格位置并移动插件
                    MovePluginToPosition(dropPosition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CompleteDragging] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 移动插件到指定位置
        /// </summary>
        private void MovePluginToPosition(Point dropPosition)
        {
            if (_draggingPlugin == null)
                return;

            // 计算目标网格位置
            double cellWidth = DesktopGrid.ActualWidth / _layout.Columns;
            double cellHeight = 100; // 固定单元格高度

            int targetX = Math.Max(0, Math.Min(_layout.Columns - 1, (int)(dropPosition.X / cellWidth)));
            int targetY = Math.Max(0, (int)(dropPosition.Y / cellHeight));

            // 查找当前插件的布局项
            var currentItem = _layout.Items.OfType<PluginDesktopItem>()
                .FirstOrDefault(p => p.PackageName == _draggingPlugin.PackageName);

            if (currentItem == null)
                return;

            // 检查目标位置是否有其他项目
            var targetItem = _layout.Items.FirstOrDefault(item => item.GridX == targetX && item.GridY == targetY && item != currentItem);

            if (targetItem != null)
            {
                // 交换位置
                int oldX = currentItem.GridX;
                int oldY = currentItem.GridY;

                targetItem.GridX = oldX;
                targetItem.GridY = oldY;
            }

            currentItem.GridX = targetX;
            currentItem.GridY = targetY;

            SaveLayout();
            RenderDesktop();
        }

        /// <summary>
        /// 完成文件夹拖拽
        /// </summary>
        private void CompleteFolderDragging(Point dropPosition)
        {
            try
            {
                if (_draggingFolder == null || _draggingIcon == null)
                    return;

                System.Diagnostics.Debug.WriteLine($"[CompleteFolderDragging] Drop at: ({dropPosition.X}, {dropPosition.Y})");

                // 计算目标网格位置
                double cellWidth = DesktopGrid.ActualWidth / _layout.Columns;
                double cellHeight = DesktopGrid.ActualHeight / _layout.Rows;

                int targetX = Math.Max(0, Math.Min(_layout.Columns - 1, (int)(dropPosition.X / cellWidth)));
                int targetY = Math.Max(0, Math.Min(_layout.Rows - 1, (int)(dropPosition.Y / cellHeight)));

                // 检查目标位置是否有其他项目
                var targetItem = _layout.Items.FirstOrDefault(item => item.GridX == targetX && item.GridY == targetY && item != _draggingFolder);

                if (targetItem != null)
                {
                    // 交换位置
                    int oldX = _draggingFolder.GridX;
                    int oldY = _draggingFolder.GridY;

                    targetItem.GridX = oldX;
                    targetItem.GridY = oldY;
                }

                _draggingFolder.GridX = targetX;
                _draggingFolder.GridY = targetY;

                SaveLayout();
                RenderDesktop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CompleteFolderDragging] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 从文件夹中移除插件到桌面
        /// </summary>
        private async void RemovePluginFromFolder(PluginDisplayItem plugin, FolderDesktopItem folder)
        {
            try
            {
                if (!folder.PluginPackageNames.Contains(plugin.PackageName))
                    return;

                // 从文件夹移除
                folder.PluginPackageNames.Remove(plugin.PackageName);

                // 找到桌面上的空闲位置
                int gridX = 0, gridY = 0;
                bool found = false;

                for (int y = 0; y < _layout.Rows && !found; y++)
                {
                    for (int x = 0; x < _layout.Columns && !found; x++)
                    {
                        if (!_layout.Items.Any(item => item.GridX == x && item.GridY == y))
                        {
                            gridX = x;
                            gridY = y;
                            found = true;
                        }
                    }
                }

                // 添加到桌面
                _layout.Items.Add(new PluginDesktopItem
                {
                    PackageName = plugin.PackageName,
                    GridX = gridX,
                    GridY = gridY
                });

                System.Diagnostics.Debug.WriteLine($"[RemovePluginFromFolder] Moved {plugin.PackageName} from folder {folder.Name} to desktop at ({gridX}, {gridY})");

                // 关闭文件夹弹出窗口（不使用动画，直接关闭因为需要立即渲染桌面）
                FolderOverlay.Visibility = Visibility.Collapsed;
                FolderOverlay.Opacity = 1;
                _currentOpenFolder = null;

                SaveLayout();
                RenderDesktop();
            }
            catch (Exception ex)
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to move plugin out of folder: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
            }
        }

        /// <summary>
        /// 将插件添加到文件夹
        /// </summary>
        private async void AddPluginToFolder(PluginDisplayItem plugin, FolderDesktopItem folder)
        {
            try
            {
                if (folder.PluginPackageNames.Contains(plugin.PackageName))
                {
                    await Service.Arcusrix.PSDialogService.Warning(
                        DesktopLocalization.GetFormat(DesktopLocalization.Dialog_AlreadyInFolder, plugin.Name),
                        DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                        this);
                    return;
                }

                // 从桌面移除插件
                var itemToRemove = _layout.Items.FirstOrDefault(
                    item => item is PluginDesktopItem pluginItem && pluginItem.PackageName == plugin.PackageName);

                if (itemToRemove != null)
                {
                    _layout.Items.Remove(itemToRemove);
                    folder.PluginPackageNames.Add(plugin.PackageName);
                    SaveLayout();
                    RenderDesktop();

                    System.Diagnostics.Debug.WriteLine($"[AddPluginToFolder] Added {plugin.PackageName} to folder {folder.Name}");
                }
            }
            catch (Exception ex)
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to add plugin to folder: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
            }
        }

        /// <summary>
        /// 显示插件信息
        /// </summary>
        private async void ShowPluginInfo(PluginDisplayItem plugin)
        {
            try
            {
                // 查找对应的 DesktopItem
                var desktopItem = _layout.Items.FirstOrDefault(i => i is PluginDesktopItem pi && pi.PackageName == plugin.PackageName);

                var hotkeyChanged = await PCOPluginInfoDialog.ShowAsync(
                    this,
                    plugin.PackageName,
                    _database,
                    desktopItem,
                    OnPluginHotkeyChanged);

                if (hotkeyChanged)
                {
                    SaveLayout();
                    RenderDesktop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowPluginInfo] Error: {ex.Message}");
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to show plugin info: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
            }
        }

        /// <summary>
        /// 插件快捷键变更回调
        /// </summary>
        private void OnPluginHotkeyChanged(DesktopItem item)
        {
            // 先注销旧快捷键
            Manager.Hotkey.PMHotkey.Instance.Unregister(item.Id);

            // 如果有新快捷键，注册
            if (!string.IsNullOrEmpty(item.Hotkey))
            {
                RegisterItemHotkey(item);
            }
        }

        /// <summary>
        /// 打开插件设置
        /// </summary>
        private async void OpenPluginSettings(PluginDisplayItem plugin)
        {
            try
            {
                // 从数据库获取插件的 SettingUri
                var records = await _database?.ExecuteQuery($"SELECT SettingUri FROM Phobos_Plugin WHERE PackageName = '{plugin.PackageName}'");

                if (records != null && records.Count > 0)
                {
                    var settingUri = records[0]["SettingUri"]?.ToString();

                    if (!string.IsNullOrEmpty(settingUri))
                    {
                        // 使用 PMPlugin 打开设置页面
                        await PMPlugin.Instance.Run(plugin.PackageName, settingUri);
                    }
                    else
                    {
                        // 没有设置页面，显示提示
                        await Service.Arcusrix.PSDialogService.Warning(
                            DesktopLocalization.GetFormat(DesktopLocalization.Dialog_NoSettings, plugin.Name),
                            DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_Settings),
                            this);
                    }
                }
            }
            catch (Exception ex)
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to open settings: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
            }
        }

        /// <summary>
        /// 卸载插件
        /// </summary>
        public async void UninstallPlugin(PluginDisplayItem plugin, bool? alreadyUninstalled = false)
        {
            var t = alreadyUninstalled == true;
            // 显示确认对话框
            var result = t ? t : await Service.Arcusrix.PSDialogService.Confirm(
                DesktopLocalization.GetFormat(DesktopLocalization.Dialog_ConfirmUninstall_Message, plugin.Name),
                DesktopLocalization.Get(DesktopLocalization.Dialog_ConfirmUninstall),
                this);

            if (result)
            {
                try
                {
                    if (!t)
                        // 使用 PMPlugin 卸载插件
                        await PMPlugin.Instance.Uninstall(plugin.PackageName);

                    // 从布局中移除插件
                    var itemsToRemove = _layout.Items
                        .Where(item => item is PluginDesktopItem pluginItem && pluginItem.PackageName == plugin.PackageName)
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _layout.Items.Remove(item);
                    }

                    // 从所有文件夹中移除插件
                    foreach (var folder in _layout.Folders)
                    {
                        folder.PluginPackageNames.Remove(plugin.PackageName);
                    }

                    //移除指向的快捷方式
                    var linkItems = _layout.Items
                        .Where(item => item is ShortcutDesktopItem pluginItem && pluginItem.TargetPackageName == plugin.PackageName)
                        .ToList();

                    foreach (var item in linkItems)
                    {
                        _layout.Items.Remove(item);
                    }

                    // 从插件列表中移除
                    _allPlugins.Remove(plugin.PackageName);

                    // 重新渲染并保存
                    RenderDesktop();
                    SaveLayout();

                    if (!t)
                        await Service.Arcusrix.PSDialogService.Info(
                            $"Plugin '{plugin.Name}' has been uninstalled successfully.",
                            DesktopLocalization.Get(DesktopLocalization.Dialog_UninstallComplete),
                            true,
                            this);
                }
                catch (Exception ex)
                {
                    await Service.Arcusrix.PSDialogService.Warning(
                        $"Failed to uninstall plugin: {ex.Message}",
                        DesktopLocalization.Get(DesktopLocalization.Dialog_UninstallFailed),
                        this);
                }
            }
        }

        /// <summary>
        /// 打开桌面设置
        /// </summary>
        private void OpenDesktopSettings()
        {
            ShowSettingsPanel();
        }

        #region 窗口控制

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            SaveLayout();
        }

        public void SetTitle(string title)
        {
            TitleText.Text = title;
            Title = title;
        }

        public void SetWindowIcon(ImageSource iconSource)
        {
            WindowIcon.Source = iconSource;
            Icon = iconSource;
        }

        public void SetWindowIcon(string iconPath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                SetWindowIcon(bitmap);
            }
            catch
            {
                WindowIcon.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region 搜索功能

        private string _searchQuery = string.Empty;
        private bool _isSearchMode = false;

        /// <summary>
        /// 搜索框文本改变事件
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text.Trim();

            // 更新占位符和清除按钮的可见性
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchQuery) ? Visibility.Visible : Visibility.Collapsed;
            SearchClearButton.Visibility = string.IsNullOrEmpty(_searchQuery) ? Visibility.Collapsed : Visibility.Visible;

            // 应用搜索过滤
            ApplySearchFilter();
        }

        /// <summary>
        /// 清除搜索按钮点击事件
        /// </summary>
        private void SearchClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            SearchBox.Focus();
        }

        /// <summary>
        /// 应用搜索过滤
        /// </summary>
        private void ApplySearchFilter()
        {
            bool wasSearchMode = _isSearchMode;
            bool newSearchMode = !string.IsNullOrEmpty(_searchQuery);

            // 判断是否是模式切换（从无到有 或 从有到无）
            bool isModeChange = wasSearchMode != newSearchMode;
            _isSearchMode = newSearchMode;

            if (_isSearchMode)
            {
                // 进入搜索模式：渲染扁平化的搜索结果
                // 仅在模式切换时播放动画
                RenderSearchResults(playAnimation: isModeChange);
            }
            else if (wasSearchMode)
            {
                // 退出搜索模式：恢复正常桌面布局（播放动画）
                RenderDesktop(true);
            }
        }

        /// <summary>
        /// 渲染搜索结果（扁平化网格，从左到右，从上到下排列）
        /// </summary>
        /// <param name="playAnimation">是否播放飞入动画</param>
        private void RenderSearchResults(bool playAnimation = false)
        {
            DesktopGrid.Children.Clear();
            DesktopGrid.RowDefinitions.Clear();
            DesktopGrid.ColumnDefinitions.Clear();

            // 收集匹配的插件和快捷方式
            var matchedPlugins = new List<PluginDisplayItem>();
            var matchedShortcuts = new List<ShortcutDesktopItem>();

            // 搜索桌面上的项目
            foreach (var item in _layout.Items)
            {
                if (item is PluginDesktopItem pluginItem)
                {
                    if (_allPlugins.TryGetValue(pluginItem.PackageName, out var plugin))
                    {
                        if (MatchesSearch(plugin))
                        {
                            matchedPlugins.Add(plugin);
                        }
                    }
                }
                else if (item is FolderDesktopItem folder)
                {
                    // 搜索文件夹内的插件
                    foreach (var pkgName in folder.PluginPackageNames)
                    {
                        if (_allPlugins.TryGetValue(pkgName, out var plugin))
                        {
                            if (MatchesSearch(plugin))
                            {
                                matchedPlugins.Add(plugin);
                            }
                        }
                    }
                }
                else if (item is ShortcutDesktopItem shortcut)
                {
                    if (MatchesSearch(shortcut))
                    {
                        matchedShortcuts.Add(shortcut);
                    }
                }
            }

            // 也搜索未放置在桌面上的插件
            foreach (var kvp in _allPlugins)
            {
                if (!matchedPlugins.Contains(kvp.Value) && MatchesSearch(kvp.Value))
                {
                    matchedPlugins.Add(kvp.Value);
                }
            }

            // 去重
            matchedPlugins = matchedPlugins.Distinct().ToList();

            int totalItems = matchedPlugins.Count + matchedShortcuts.Count;
            if (totalItems == 0)
            {
                // 没有搜索结果，显示一个空网格
                return;
            }

            // 计算需要的行数
            int columns = _layout.Columns;
            int rows = (int)Math.Ceiling((double)totalItems / columns);

            // 创建网格定义
            for (int i = 0; i < rows; i++)
                DesktopGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(110) });

            for (int i = 0; i < columns; i++)
                DesktopGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // 渲染搜索结果图标
            var iconControls = new List<(Border control, int index)>();
            int index = 0;

            // 先渲染插件
            foreach (var plugin in matchedPlugins)
            {
                int row = index / columns;
                int col = index % columns;

                var iconControl = CreatePluginIcon(plugin, index);
                Grid.SetRow(iconControl, row);
                Grid.SetColumn(iconControl, col);
                DesktopGrid.Children.Add(iconControl);
                iconControls.Add((iconControl, index));
                index++;
            }

            // 再渲染快捷方式
            foreach (var shortcut in matchedShortcuts)
            {
                int row = index / columns;
                int col = index % columns;

                var iconControl = CreateShortcutIcon(shortcut);
                Grid.SetRow(iconControl, row);
                Grid.SetColumn(iconControl, col);
                DesktopGrid.Children.Add(iconControl);
                iconControls.Add((iconControl, index));
                index++;
            }

            // 仅在模式切换时播放飞入动画
            if (playAnimation)
            {
                AnimateIconsFlyIn(iconControls);
            }
        }

        /// <summary>
        /// 检查插件是否匹配搜索条件
        /// </summary>
        private bool MatchesSearch(PluginDisplayItem plugin)
        {
            if (string.IsNullOrEmpty(_searchQuery))
                return true;

            return plugin.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                   plugin.PackageName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查快捷方式是否匹配搜索条件
        /// </summary>
        private bool MatchesSearch(ShortcutDesktopItem shortcut)
        {
            if (string.IsNullOrEmpty(_searchQuery))
                return true;

            // 搜索名称、目标包名、参数
            return shortcut.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                   shortcut.TargetPackageName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                   shortcut.Arguments.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 壁纸和透明度公共访问方法

        /// <summary>
        /// 获取壁纸路径
        /// </summary>
        public string? GetWallpaperPath() => string.IsNullOrEmpty(_backgroundImagePath) ? null : _backgroundImagePath;

        /// <summary>
        /// 获取壁纸伸展方式
        /// </summary>
        public Stretch GetWallpaperStretch() => _backgroundStretch;

        /// <summary>
        /// 获取壁纸透明度
        /// </summary>
        public double GetWallpaperOpacity() => _backgroundOpacity;

        /// <summary>
        /// 壁纸变化事件
        /// </summary>
        public event EventHandler<WallpaperChangedEventArgs>? WallpaperChanged;

        /// <summary>
        /// 透明度变化事件
        /// </summary>
        public event EventHandler<OpacityChangedEventArgs>? OpacityChanged;

        /// <summary>
        /// 触发壁纸变化事件
        /// </summary>
        private void OnWallpaperChanged()
        {
            WallpaperChanged?.Invoke(this, new WallpaperChangedEventArgs
            {
                WallpaperPath = _backgroundImagePath,
                Stretch = _backgroundStretch
            });
        }

        /// <summary>
        /// 触发透明度变化事件
        /// </summary>
        private void OnOpacityChanged()
        {
            OpacityChanged?.Invoke(this, new OpacityChangedEventArgs
            {
                Opacity = _backgroundOpacity
            });
        }

        #endregion

        #region 设置面板

        private string _backgroundImagePath = string.Empty;
        private Stretch _backgroundStretch = Stretch.UniformToFill;
        private double _backgroundOpacity = 1.0;

        /// <summary>
        /// 显示设置面板
        /// </summary>
        private void ShowSettingsPanel()
        {
            // 加载当前设置到UI
            BackgroundPathTextBox.Text = _backgroundImagePath;
            BackgroundOpacitySlider.Value = _backgroundOpacity * 100;
            OpacityValueText.Text = $"{(int)(_backgroundOpacity * 100)}%";

            // 设置本地化文本
            SettingsTitleText.Text = DesktopLocalization.Get(DesktopLocalization.Settings_Title);
            BackgroundImageLabel.Text = DesktopLocalization.Get(DesktopLocalization.Settings_BackgroundImage);
            NoBackgroundText.Text = DesktopLocalization.Get(DesktopLocalization.Settings_NoBackground);
            BrowseBackgroundButton.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Browse);
            ClearBackgroundButton.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Clear);
            ScaleModeLabel.Text = DesktopLocalization.Get(DesktopLocalization.Settings_ScalingMode);
            ScaleModeFill.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Scale_Fill);
            ScaleModeFit.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Scale_Fit);
            ScaleModeStretch.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Scale_Stretch);
            ScaleModeTile.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Scale_Tile);
            BackgroundOpacityLabel.Text = DesktopLocalization.Get(DesktopLocalization.Settings_BackgroundOpacity);
            SaveSettingsButton.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Save);

            // 设置缩放模式选择
            for (int i = 0; i < ScaleModeComboBox.Items.Count; i++)
            {
                if (ScaleModeComboBox.Items[i] is ComboBoxItem item &&
                    item.Tag?.ToString() == _backgroundStretch.ToString())
                {
                    ScaleModeComboBox.SelectedIndex = i;
                    break;
                }
            }

            // 更新预览
            UpdateBackgroundPreview();

            // 显示面板
            SettingsOverlay.Visibility = Visibility.Visible;
            PlaySettingsOpenAnimation();
        }

        /// <summary>
        /// 隐藏设置面板
        /// </summary>
        private void HideSettingsPanel()
        {
            PlaySettingsCloseAnimation(() =>
            {
                SettingsOverlay.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// 设置面板打开动画
        /// </summary>
        private void PlaySettingsOpenAnimation()
        {
            var storyboard = new Storyboard();

            var elasticEase = PUAnimation.CreateElasticEase(EasingMode.EaseOut, 1, 6);
            var cubicEase = PUAnimation.CreateSmoothEase();

            // 遮罩淡入
            SettingsOverlay.Opacity = 0;
            PUAnimation.AddOpacityAnimation(storyboard, SettingsOverlay, 0, 1, 200, cubicEase);

            // 面板淡入
            SettingsPanel.Opacity = 0;
            PUAnimation.AddOpacityAnimation(storyboard, SettingsPanel, 0, 1, 250, cubicEase);

            // 缩放动画
            PUAnimation.AddScaleXAnimation(storyboard, SettingsPanel, 0.85, 1, 350, elasticEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            PUAnimation.AddScaleYAnimation(storyboard, SettingsPanel, 0.85, 1, 350, elasticEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            // Y轴位移
            PUAnimation.AddTranslateYAnimation(storyboard, SettingsPanel, 20, 0, 300, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");

            storyboard.Begin();
        }

        /// <summary>
        /// 设置面板关闭动画
        /// </summary>
        private void PlaySettingsCloseAnimation(Action onCompleted)
        {
            var storyboard = new Storyboard();

            var cubicEase = PUAnimation.CreateSmoothEase(EasingMode.EaseIn);

            // 遮罩淡出
            PUAnimation.AddOpacityAnimation(storyboard, SettingsOverlay, 1, 0, 200, cubicEase);

            // 面板淡出
            PUAnimation.AddOpacityAnimation(storyboard, SettingsPanel, 1, 0, 180, cubicEase);

            // 缩放动画
            PUAnimation.AddScaleXAnimation(storyboard, SettingsPanel, 1, 0.9, 180, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");

            PUAnimation.AddScaleYAnimation(storyboard, SettingsPanel, 1, 0.9, 180, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

            // Y轴位移
            PUAnimation.AddTranslateYAnimation(storyboard, SettingsPanel, 0, 15, 180, cubicEase, 0,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");

            storyboard.Completed += (s, e) => onCompleted?.Invoke();
            storyboard.Begin();
        }

        private void SettingsOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == SettingsOverlay)
            {
                HideSettingsPanel();
            }
        }

        private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideSettingsPanel();
        }

        private void BrowseBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Title = "Select Background Image"
            };

            if (dialog.ShowDialog() == true)
            {
                BackgroundPathTextBox.Text = dialog.FileName;
                UpdateBackgroundPreview();
            }
        }

        private void ClearBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundPathTextBox.Text = string.Empty;
            UpdateBackgroundPreview();
        }

        private void ScaleModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBackgroundPreview();
        }

        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueText != null)
            {
                OpacityValueText.Text = $"{(int)e.NewValue}%";
            }
            UpdateBackgroundPreview();
        }

        private void UpdateBackgroundPreview()
        {
            var path = BackgroundPathTextBox?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                BackgroundPreview.Source = null;
                NoBackgroundText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                BackgroundPreview.Source = bitmap;
                BackgroundPreview.Opacity = (BackgroundOpacitySlider?.Value ?? 100) / 100.0;

                if (ScaleModeComboBox?.SelectedItem is ComboBoxItem item)
                {
                    var stretchStr = item.Tag?.ToString() ?? "UniformToFill";
                    if (Enum.TryParse<Stretch>(stretchStr, out var stretch))
                    {
                        BackgroundPreview.Stretch = stretch;
                    }
                }

                NoBackgroundText.Visibility = Visibility.Collapsed;
            }
            catch
            {
                BackgroundPreview.Source = null;
                NoBackgroundText.Visibility = Visibility.Visible;
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 记录旧值用于检测变化
            var oldWallpaperPath = _backgroundImagePath;
            var oldStretch = _backgroundStretch;
            var oldOpacity = _backgroundOpacity;

            // 保存设置
            _backgroundImagePath = BackgroundPathTextBox.Text;
            _backgroundOpacity = BackgroundOpacitySlider.Value / 100.0;

            if (ScaleModeComboBox.SelectedItem is ComboBoxItem item)
            {
                var stretchStr = item.Tag?.ToString() ?? "UniformToFill";
                if (Enum.TryParse<Stretch>(stretchStr, out var stretch))
                {
                    _backgroundStretch = stretch;
                }
            }

            // 应用背景
            ApplyBackgroundImage();

            // 保存到布局JSON
            SaveLayout();

            // 触发事件通知订阅者
            if (oldWallpaperPath != _backgroundImagePath || oldStretch != _backgroundStretch)
            {
                OnWallpaperChanged();
            }
            if (Math.Abs(oldOpacity - _backgroundOpacity) > 0.001)
            {
                OnOpacityChanged();
            }

            // 关闭设置面板
            HideSettingsPanel();
        }

        private void ApplyBackgroundImage()
        {
            if (string.IsNullOrEmpty(_backgroundImagePath) || !File.Exists(_backgroundImagePath))
            {
                BackgroundImage.Source = null;
                BackgroundImage.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_backgroundImagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                BackgroundImage.Source = bitmap;
                BackgroundImage.Stretch = _backgroundStretch;
                BackgroundImage.Opacity = _backgroundOpacity;
                BackgroundImage.Visibility = Visibility.Visible;
            }
            catch
            {
                BackgroundImage.Source = null;
                BackgroundImage.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region IPhobosDesktop Helper Methods

        /// <summary>
        /// 获取所有桌面项
        /// </summary>
        public List<DesktopItem> GetAllDesktopItems()
        {
            return _layout.Items.ToList();
        }

        /// <summary>
        /// 根据包名获取插件显示项
        /// </summary>
        public IPhobosPlugin? GetPluginByPackageName(string packageName)
        {
            return PMPlugin.Instance.GetPlugin(packageName);
        }

        /// <summary>
        /// 根据 ID 获取桌面项
        /// </summary>
        public DesktopItem? GetDesktopItemById(string itemId)
        {
            return _layout.Items.FirstOrDefault(i => i.Id == itemId);
        }

        /// <summary>
        /// 添加桌面项
        /// </summary>
        public void AddDesktopItem(DesktopItem item)
        {
            // 如果没有指定位置，找一个空位
            if (item.GridX == 0 && item.GridY == 0)
            {
                var position = FindFirstEmptyPosition();
                item.GridX = position.X;
                item.GridY = position.Y;
            }

            _layout.Items.Add(item);
            SaveLayout();
        }

        /// <summary>
        /// 移除桌面项
        /// </summary>
        public void RemoveDesktopItem(DesktopItem item)
        {
            _layout.Items.Remove(item);

            // 如果是文件夹，也从 Folders 列表移除
            if (item is FolderDesktopItem folder)
            {
                _layout.Folders.Remove(folder);
            }

            SaveLayout();
        }

        /// <summary>
        /// 启动桌面项
        /// </summary>
        public async Task LaunchDesktopItem(DesktopItem item, params object[] args)
        {
            try
            {
                switch (item)
                {
                    case PluginDesktopItem pluginItem:
                        if (_allPlugins.TryGetValue(pluginItem.PackageName, out var plugin))
                        {
                            await PMPlugin.Instance.Launch(pluginItem.PackageName, args);
                        }
                        break;

                    case ShortcutDesktopItem shortcut:
                        var shortcutArgs = shortcut.ParseArguments();
                        var allArgs = shortcutArgs.Concat(args.Select(a => a?.ToString() ?? string.Empty)).ToArray();
                        await PMPlugin.Instance.Launch(shortcut.TargetPackageName, allArgs);
                        break;

                    case FolderDesktopItem folder:
                        OpenFolder(folder);
                        break;
                }
            }
            catch (Exception ex)
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    $"Failed to launch plugin: {ex.Message}",
                    DesktopLocalization.Get(DesktopLocalization.Dialog_LaunchError),
                    this);

            }
        }

        #endregion
    }
}