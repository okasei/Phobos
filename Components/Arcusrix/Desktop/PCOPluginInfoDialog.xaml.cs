using Phobos.Class.Database;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// PCOPluginInfoDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PCOPluginInfoDialog : Window
    {
        private DesktopItem? _desktopItem;
        private Action<DesktopItem>? _onHotkeyChanged;

        /// <summary>
        /// 快捷键是否发生变化
        /// </summary>
        public bool HotkeyChanged { get; private set; }

        public PCOPluginInfoDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置本地化文本
            DialogTitleText.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_Title);
            PackageNameLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_PackageName);
            ManufacturerLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_Manufacturer);
            DescriptionLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_Description);
            DirectoryLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_InstallDirectory);
            InstallTimeLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_InstallTime);
            StatusLabel.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_Status);
            SystemPluginBadgeText.Text = DesktopLocalization.Get(DesktopLocalization.PluginInfo_SystemPlugin);
            CloseButton.Content = DesktopLocalization.Get(DesktopLocalization.PluginInfo_Close);
            UpdateHotkeyButtonText();

            PlayOpenAnimation();
        }

        /// <summary>
        /// 更新快捷键按钮文本
        /// </summary>
        private void UpdateHotkeyButtonText()
        {
            if (_desktopItem != null && !string.IsNullOrEmpty(_desktopItem.Hotkey))
            {
                HotkeyButtonText.Text = $"{DesktopLocalization.Get(DesktopLocalization.Hotkey_SetHotkey)}: {_desktopItem.Hotkey}";
            }
            else
            {
                HotkeyButtonText.Text = DesktopLocalization.Get(DesktopLocalization.Menu_Plugin_SetHotkey);
            }
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_desktopItem == null)
                return;

            var result = PCOHotkeyDialog.ShowDialog(this, _desktopItem.Hotkey);
            if (result != null)
            {
                _desktopItem.Hotkey = result;
                HotkeyChanged = true;
                UpdateHotkeyButtonText();
                _onHotkeyChanged?.Invoke(_desktopItem);
            }
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

            // 淡入
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

            // 缩放动画
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

            // Y轴位移
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

            // 淡出
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

            // 缩放动画
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

            // Y轴位移
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayCloseAnimation(() => Close());
        }

        /// <summary>
        /// 显示插件信息对话框
        /// </summary>
        public static async Task<bool> ShowAsync(Window owner, string packageName, PCSqliteDatabase? database, DesktopItem? desktopItem = null, Action<DesktopItem>? onHotkeyChanged = null)
        {
            var dialog = new PCOPluginInfoDialog
            {
                Owner = owner,
                _desktopItem = desktopItem,
                _onHotkeyChanged = onHotkeyChanged
            };

            // 注册为子窗口
            if (owner is PCOPhobosDesktop desktop)
            {
                desktop.RegisterChildWindow(dialog);
            }

            await dialog.LoadPluginInfo(packageName, database);
            dialog.ShowDialog();
            return dialog.HotkeyChanged;
        }

        private async Task LoadPluginInfo(string packageName, PCSqliteDatabase? database)
        {
            if (database == null)
                return;

            try
            {
                var records = await database.ExecuteQuery(
                    $"SELECT * FROM Phobos_Plugin WHERE PackageName = '{packageName}'");

                if (records.Count > 0)
                {
                    var record = records[0];

                    PluginName.Text = record["Name"]?.ToString() ?? "Unknown";
                    PluginVersion.Text = $"Version {record["Version"]?.ToString() ?? "1.0.0"}";
                    PackageNameText.Text = packageName;
                    ManufacturerText.Text = record["Manufacturer"]?.ToString() ?? "Unknown";
                    DescriptionText.Text = record["Description"]?.ToString() ?? "No description available.";
                    DirectoryText.Text = record["Directory"]?.ToString() ?? "Unknown";

                    // 安装时间
                    if (record["InstallTime"] != null && DateTime.TryParse(record["InstallTime"].ToString(), out var installTime))
                    {
                        InstallTimeText.Text = installTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    // 状态
                    bool isEnabled = Convert.ToBoolean(record["IsEnabled"]);
                    StatusText.Text = isEnabled
                        ? DesktopLocalization.Get(DesktopLocalization.PluginInfo_Enabled)
                        : DesktopLocalization.Get(DesktopLocalization.PluginInfo_Disabled);
                    StatusText.Foreground = isEnabled
                        ? (SolidColorBrush)FindResource("SuccessBrush")
                        : (SolidColorBrush)FindResource("Foreground3Brush");

                    // 系统插件标记
                    bool isSystemPlugin = Convert.ToBoolean(record["IsSystemPlugin"]);
                    var directory = record["Directory"]?.ToString() ?? string.Empty;
                    bool isBuiltIn = string.Equals(directory, "builtin", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(directory, "built-in", StringComparison.OrdinalIgnoreCase);

                    if (isSystemPlugin || isBuiltIn)
                    {
                        SystemPluginBadge.Visibility = Visibility.Visible;
                    }

                    // 加载图标
                    var icon = record["Icon"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(directory))
                    {
                        try
                        {
                            string iconPath = isBuiltIn
                                ? (System.IO.Path.IsPathRooted(icon) ? icon : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, icon))
                                : (System.IO.Path.IsPathRooted(icon) ? icon : System.IO.Path.Combine(directory, icon));

                            if (System.IO.File.Exists(iconPath))
                            {
                                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                PluginIcon.Source = bitmap;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load plugin info: {ex.Message}");
            }
        }
    }
}