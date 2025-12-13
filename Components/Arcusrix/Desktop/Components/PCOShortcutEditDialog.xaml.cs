using Phobos.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Desktop.Components
{
    /// <summary>
    /// 快捷方式编辑对话框
    /// </summary>
    public partial class PCOShortcutEditDialog : Window
    {
        private ShortcutDesktopItem? _shortcut;
        private Dictionary<string, PluginDisplayItem> _plugins;
        private bool _isEditMode = false;
        private bool _isUpdatingFromComboBox = false;
        private bool _isUpdatingFromTextBox = false;
        private string _currentHotkey = string.Empty;

        /// <summary>
        /// 对话框结果
        /// </summary>
        public ShortcutDesktopItem? Result { get; private set; }

        /// <summary>
        /// 热键是否变更
        /// </summary>
        public bool HotkeyChanged { get; private set; } = false;

        /// <summary>
        /// 创建新快捷方式对话框
        /// </summary>
        public PCOShortcutEditDialog(Dictionary<string, PluginDisplayItem> plugins, ShortcutDesktopItem? existing = null)
        {
            InitializeComponent();
            _plugins = plugins;
            _shortcut = existing;
            _isEditMode = existing != null;

            Loaded += PCOShortcutEditDialog_Loaded;
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

        private void PCOShortcutEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置本地化文本
            DialogTitle.Text = _isEditMode
                ? DesktopLocalization.Get(DesktopLocalization.Shortcut_EditTitle)
                : DesktopLocalization.Get(DesktopLocalization.Shortcut_NewTitle);
            NameLabel.Text = DesktopLocalization.Get(DesktopLocalization.Shortcut_Name);
            PackageLabel.Text = DesktopLocalization.Get(DesktopLocalization.Shortcut_TargetPlugin);
            OrInputLabel.Text = DesktopLocalization.Get(DesktopLocalization.Shortcut_OrInputPackageName);
            ArgsLabel.Text = DesktopLocalization.Get(DesktopLocalization.Shortcut_Arguments);
            IconLabel.Text = DesktopLocalization.Get(DesktopLocalization.Shortcut_CustomIcon);
            BrowseIconButton.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Browse);
            ClearIconButton.Content = DesktopLocalization.Get(DesktopLocalization.Settings_Clear);
            CancelButton.Content = DesktopLocalization.Get(DesktopLocalization.Shortcut_Cancel);
            SaveButton.Content = DesktopLocalization.Get(DesktopLocalization.Shortcut_Save);

            // 填充插件下拉列表
            PopulatePluginComboBox();

            // 如果是编辑模式，填充现有数据
            if (_isEditMode && _shortcut != null)
            {
                NameTextBox.Text = _shortcut.Name;
                ArgsTextBox.Text = _shortcut.Arguments;
                IconPathTextBox.Text = _shortcut.CustomIconPath;
                _currentHotkey = _shortcut.Hotkey ?? string.Empty;

                // 尝试选择目标插件，如果找不到则填入手动输入框
                bool found = false;
                for (int i = 0; i < PackageComboBox.Items.Count; i++)
                {
                    if (PackageComboBox.Items[i] is ComboBoxItem item &&
                        item.Tag?.ToString() == _shortcut.TargetPackageName)
                    {
                        PackageComboBox.SelectedIndex = i;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // 插件不在列表中，使用手动输入
                    PackageNameTextBox.Text = _shortcut.TargetPackageName;
                }

                UpdateIconPreview();
            }

            // 更新热键按钮显示
            UpdateHotkeyButtonText();
        }

        /// <summary>
        /// 更新热键按钮文本
        /// </summary>
        private void UpdateHotkeyButtonText()
        {
            if (string.IsNullOrEmpty(_currentHotkey))
            {
                HotkeyButtonText.Text = DesktopLocalization.Get(DesktopLocalization.Menu_Shortcut_SetHotkey);
            }
            else
            {
                var hotkeyInfo = Manager.Hotkey.HotkeyInfo.Parse(_currentHotkey);
                if (hotkeyInfo != null)
                {
                    HotkeyButtonText.Text = $"{DesktopLocalization.Get(DesktopLocalization.Hotkey_SetHotkey)}: {hotkeyInfo.GetDisplayString()}";
                }
                else
                {
                    HotkeyButtonText.Text = DesktopLocalization.Get(DesktopLocalization.Menu_Shortcut_SetHotkey);
                }
            }
        }

        /// <summary>
        /// 热键按钮点击事件
        /// </summary>
        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            var result = PCOHotkeyDialog.ShowDialog(this, _currentHotkey);
            if (result != null)
            {
                _currentHotkey = result;
                HotkeyChanged = true;
                UpdateHotkeyButtonText();
            }
        }

        private void PopulatePluginComboBox()
        {
            PackageComboBox.Items.Clear();

            foreach (var kvp in _plugins)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{kvp.Value.Name} ({kvp.Key})",
                    Tag = kvp.Key
                };
                PackageComboBox.Items.Add(item);
            }

            if (PackageComboBox.Items.Count > 0)
            {
                PackageComboBox.SelectedIndex = 0;
            }
        }

        private void PackageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromTextBox)
                return;

            _isUpdatingFromComboBox = true;
            try
            {
                // 当从下拉列表选择时，清空手动输入框
                if (PackageComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Tag is string packageName)
                {
                    PackageNameTextBox.Text = string.Empty;
                }

                // 如果自定义图标为空，使用插件图标
                if (string.IsNullOrEmpty(IconPathTextBox.Text))
                {
                    UpdateIconPreview();
                }
            }
            finally
            {
                _isUpdatingFromComboBox = false;
            }
        }

        private void PackageNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromComboBox)
                return;

            _isUpdatingFromTextBox = true;
            try
            {
                // 当手动输入时，清除下拉选择
                if (!string.IsNullOrWhiteSpace(PackageNameTextBox.Text))
                {
                    PackageComboBox.SelectedIndex = -1;
                }
                else if (PackageComboBox.Items.Count > 0)
                {
                    // 如果清空了手动输入，恢复默认选择
                    PackageComboBox.SelectedIndex = 0;
                }

                UpdateIconPreview();
            }
            finally
            {
                _isUpdatingFromTextBox = false;
            }
        }

        /// <summary>
        /// 获取当前选择的包名（优先手动输入，其次下拉选择）
        /// </summary>
        private string GetSelectedPackageName()
        {
            // 优先使用手动输入的包名
            if (!string.IsNullOrWhiteSpace(PackageNameTextBox.Text))
            {
                return PackageNameTextBox.Text.Trim();
            }

            // 其次使用下拉选择的包名
            if (PackageComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Tag is string packageName)
            {
                return packageName;
            }

            return string.Empty;
        }

        private void UpdateIconPreview()
        {
            try
            {
                // 优先使用自定义图标
                if (!string.IsNullOrEmpty(IconPathTextBox.Text) && File.Exists(IconPathTextBox.Text))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(IconPathTextBox.Text, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    IconPreview.Source = bitmap;
                    return;
                }

                // 否则使用选中插件的图标
                var packageName = GetSelectedPackageName();
                if (!string.IsNullOrEmpty(packageName) && _plugins.TryGetValue(packageName, out var plugin))
                {
                    IconPreview.Source = plugin.Icon;
                }
                else
                {
                    IconPreview.Source = null;
                }
            }
            catch
            {
                IconPreview.Source = null;
            }
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All Files|*.*",
                Title = DesktopLocalization.Get(DesktopLocalization.Shortcut_SelectFile)
            };

            if (dialog.ShowDialog() == true)
            {
                // 追加到现有参数
                var currentArgs = ArgsTextBox.Text;
                var filePath = dialog.FileName;

                // 如果文件路径包含逗号，用双引号包裹
                if (filePath.Contains(','))
                {
                    filePath = $"\"{filePath}\"";
                }

                if (string.IsNullOrEmpty(currentArgs))
                {
                    ArgsTextBox.Text = filePath;
                }
                else
                {
                    ArgsTextBox.Text = currentArgs + "," + filePath;
                }
            }
        }

        private void BrowseIconButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.bmp|All Files|*.*",
                Title = DesktopLocalization.Get(DesktopLocalization.Shortcut_SelectIcon)
            };

            if (dialog.ShowDialog() == true)
            {
                IconPathTextBox.Text = dialog.FileName;
                UpdateIconPreview();
            }
        }

        private void ClearIconButton_Click(object sender, RoutedEventArgs e)
        {
            IconPathTextBox.Text = string.Empty;
            UpdateIconPreview();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayCloseAnimation(() =>
            {
                DialogResult = false;
                Close();
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PlayCloseAnimation(() =>
            {
                DialogResult = false;
                Close();
            });
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    DesktopLocalization.Get(DesktopLocalization.Shortcut_NameRequired),
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
                return;
            }

            var selectedPackage = GetSelectedPackageName();
            if (string.IsNullOrEmpty(selectedPackage))
            {
                await Service.Arcusrix.PSDialogService.Warning(
                    DesktopLocalization.Get(DesktopLocalization.Shortcut_PluginRequired),
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    this);
                return;
            }

            // 创建或更新快捷方式
            Result = _shortcut ?? new ShortcutDesktopItem();
            Result.Name = NameTextBox.Text.Trim();
            Result.TargetPackageName = selectedPackage;
            Result.Arguments = ArgsTextBox.Text.Trim();
            Result.CustomIconPath = IconPathTextBox.Text.Trim();
            Result.Hotkey = _currentHotkey;

            PlayCloseAnimation(() =>
            {
                DialogResult = true;
                Close();
            });
        }

        /// <summary>
        /// 显示对话框并返回结果
        /// </summary>
        public static ShortcutDesktopItem? ShowDialog(Window owner, Dictionary<string, PluginDisplayItem> plugins, ShortcutDesktopItem? existing = null)
        {
            var dialog = new PCOShortcutEditDialog(plugins, existing)
            {
                Owner = owner
            };

            // 注册为子窗口
            if (owner is PCOPhobosDesktop desktop)
            {
                desktop.RegisterChildWindow(dialog);
            }

            if (dialog.ShowDialog() == true)
            {
                return dialog.Result;
            }

            return null;
        }
    }
}
