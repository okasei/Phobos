using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Manager.Arcusrix;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Components.Arcusrix.PluginManager
{
    /// <summary>
    /// Plugin Manager i18n strings
    /// </summary>
    public static class PluginManagerLocalization
    {
        // Keys
        public const string Title = "title";
        public const string Subtitle = "subtitle";
        public const string Search = "search";
        public const string Install = "install";
        public const string Uninstall = "uninstall";
        public const string Launch = "launch";
        public const string Refresh = "refresh";
        public const string Installed = "installed";
        public const string System = "system";
        public const string PluginCount = "plugin_count";
        public const string Loading = "loading";
        public const string Ready = "ready";
        public const string SelectPlugin = "select_plugin";
        public const string CannotUninstallSelf = "cannot_uninstall_self";
        public const string Installing = "installing";
        public const string Uninstalling = "uninstalling";
        public const string ConfirmUninstall = "confirm_uninstall";
        public const string ConfirmUninstallMessage = "confirm_uninstall_message";
        public const string Launching = "launching";
        public const string SelectDll = "select_dll";
        public const string FromLocal = "from_local";

        private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            [Title] = new() { { "en-US", "Plugin Manager" }, { "zh-CN", "插件管理器" }, { "zh-TW", "插件管理器" }, { "ja-JP", "プラグインマネージャー" }, { "ko-KR", "플러그인 관리자" } },
            [Subtitle] = new() { { "en-US", "Manage your plugins" }, { "zh-CN", "管理您的插件" }, { "zh-TW", "管理您的插件" }, { "ja-JP", "プラグインを管理" }, { "ko-KR", "플러그인 관리" } },
            [Search] = new() { { "en-US", "Search plugins..." }, { "zh-CN", "搜索插件..." }, { "zh-TW", "搜索插件..." }, { "ja-JP", "プラグインを検索..." }, { "ko-KR", "플러그인 검색..." } },
            [Install] = new() { { "en-US", "Install" }, { "zh-CN", "安装" }, { "zh-TW", "安裝" }, { "ja-JP", "インストール" }, { "ko-KR", "설치" } },
            [Uninstall] = new() { { "en-US", "Uninstall" }, { "zh-CN", "卸载" }, { "zh-TW", "卸載" }, { "ja-JP", "アンインストール" }, { "ko-KR", "제거" } },
            [Launch] = new() { { "en-US", "Launch" }, { "zh-CN", "启动" }, { "zh-TW", "啟動" }, { "ja-JP", "起動" }, { "ko-KR", "실행" } },
            [Refresh] = new() { { "en-US", "Refresh" }, { "zh-CN", "刷新" }, { "zh-TW", "重新整理" }, { "ja-JP", "更新" }, { "ko-KR", "새로고침" } },
            [Installed] = new() { { "en-US", "Installed" }, { "zh-CN", "已安装" }, { "zh-TW", "已安裝" }, { "ja-JP", "インストール済み" }, { "ko-KR", "설치됨" } },
            [System] = new() { { "en-US", "System" }, { "zh-CN", "系统" }, { "zh-TW", "系統" }, { "ja-JP", "システム" }, { "ko-KR", "시스템" } },
            [PluginCount] = new() { { "en-US", "{0} plugins" }, { "zh-CN", "{0} 个插件" }, { "zh-TW", "{0} 個插件" }, { "ja-JP", "{0} プラグイン" }, { "ko-KR", "{0}개 플러그인" } },
            [Loading] = new() { { "en-US", "Loading..." }, { "zh-CN", "加载中..." }, { "zh-TW", "載入中..." }, { "ja-JP", "読み込み中..." }, { "ko-KR", "로딩 중..." } },
            [Ready] = new() { { "en-US", "Ready" }, { "zh-CN", "就绪" }, { "zh-TW", "就緒" }, { "ja-JP", "準備完了" }, { "ko-KR", "준비 완료" } },
            [SelectPlugin] = new() { { "en-US", "Please select a plugin" }, { "zh-CN", "请选择一个插件" }, { "zh-TW", "請選擇一個插件" }, { "ja-JP", "プラグインを選択してください" }, { "ko-KR", "플러그인을 선택하세요" } },
            [CannotUninstallSelf] = new() { { "en-US", "Cannot uninstall Plugin Manager" }, { "zh-CN", "无法卸载插件管理器" }, { "zh-TW", "無法卸載插件管理器" }, { "ja-JP", "プラグインマネージャーをアンインストールできません" }, { "ko-KR", "플러그인 관리자를 제거할 수 없습니다" } },
            [Installing] = new() { { "en-US", "Installing..." }, { "zh-CN", "安装中..." }, { "zh-TW", "安裝中..." }, { "ja-JP", "インストール中..." }, { "ko-KR", "설치 중..." } },
            [Uninstalling] = new() { { "en-US", "Uninstalling..." }, { "zh-CN", "卸载中..." }, { "zh-TW", "卸載中..." }, { "ja-JP", "アンインストール中..." }, { "ko-KR", "제거 중..." } },
            [ConfirmUninstall] = new() { { "en-US", "Confirm Uninstall" }, { "zh-CN", "确认卸载" }, { "zh-TW", "確認卸載" }, { "ja-JP", "アンインストールの確認" }, { "ko-KR", "제거 확인" } },
            [ConfirmUninstallMessage] = new() { { "en-US", "Are you sure you want to uninstall \"{0}\"? This action cannot be undone." }, { "zh-CN", "确定要卸载 \"{0}\" 吗？此操作无法撤销。" }, { "zh-TW", "確定要卸載 \"{0}\" 嗎？此操作無法撤銷。" }, { "ja-JP", "「{0}」をアンインストールしますか？この操作は元に戻せません。" }, { "ko-KR", "\"{0}\"을(를) 제거하시겠습니까? 이 작업은 취소할 수 없습니다." } },
            [Launching] = new() { { "en-US", "Launching {0}..." }, { "zh-CN", "正在启动 {0}..." }, { "zh-TW", "正在啟動 {0}..." }, { "ja-JP", "{0} を起動中..." }, { "ko-KR", "{0} 실행 중..." } },
            [SelectDll] = new() { { "en-US", "Select Plugin DLL" }, { "zh-CN", "选择插件 DLL" }, { "zh-TW", "選擇插件 DLL" }, { "ja-JP", "プラグイン DLL を選択" }, { "ko-KR", "플러그인 DLL 선택" } },
            [FromLocal] = new() { { "en-US", "From Local..." }, { "zh-CN", "从本地安装..." }, { "zh-TW", "從本地安裝..." }, { "ja-JP", "ローカルから..." }, { "ko-KR", "로컬에서..." } },
        };

        public static string Get(string key)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            if (_strings.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var str)) return str;
                if (dict.TryGetValue("en-US", out var enStr)) return enStr;
            }
            return key;
        }

        public static string GetFormat(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }

    /// <summary>
    /// PCOPluginManager.xaml
    /// </summary>
    public partial class PCOPluginManager : UserControl
    {
        private List<PluginMetadata> _allPlugins = new();
        private string _searchPlaceholder = string.Empty;
        private string _selfPackageName = "com.phobos.plugin.manager";

        public PCOPluginManager()
        {
            InitializeComponent();
            Loaded += PCOPluginManager_Loaded;
        }

        private async void PCOPluginManager_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLocalizedText();
            await RefreshPluginList();
        }

        /// <summary>
        /// Update localized text
        /// </summary>
        private void UpdateLocalizedText()
        {
            TitleText.Text = PluginManagerLocalization.Get(PluginManagerLocalization.Title);
            SubtitleText.Text = PluginManagerLocalization.Get(PluginManagerLocalization.Subtitle);
            RefreshButton.Content = PluginManagerLocalization.Get(PluginManagerLocalization.Refresh);
            InstallButton.Content = PluginManagerLocalization.Get(PluginManagerLocalization.FromLocal);
            StatusText.Text = PluginManagerLocalization.Get(PluginManagerLocalization.Ready);

            // Setup search placeholder
            _searchPlaceholder = PluginManagerLocalization.Get(PluginManagerLocalization.Search);
            SearchBox.Text = _searchPlaceholder;
            SearchBox.Foreground = (Brush)FindResource("Foreground4Brush");

            SearchBox.GotFocus += (s, e) =>
            {
                if (SearchBox.Text == _searchPlaceholder)
                {
                    SearchBox.Text = "";
                    SearchBox.Foreground = (Brush)FindResource("Foreground1Brush");
                }
            };

            SearchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchBox.Text = _searchPlaceholder;
                    SearchBox.Foreground = (Brush)FindResource("Foreground4Brush");
                }
            };
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPluginList();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            await InstallPlugin();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text != _searchPlaceholder)
            {
                FilterPlugins(SearchBox.Text);
            }
        }

        /// <summary>
        /// Refresh plugin list
        /// </summary>
        public async Task RefreshPluginList()
        {
            SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Loading));
            _allPlugins = await PMPlugin.Instance.GetInstalledPlugins();
            DisplayPlugins(_allPlugins);
            SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Ready));
        }

        /// <summary>
        /// Display plugins
        /// </summary>
        private void DisplayPlugins(List<PluginMetadata> plugins)
        {
            PluginList.Items.Clear();
            foreach (var plugin in plugins)
            {
                PluginList.Items.Add(CreatePluginCard(plugin));
            }

            PluginCountText.Text = PluginManagerLocalization.GetFormat(PluginManagerLocalization.PluginCount, plugins.Count);
        }

        /// <summary>
        /// Filter plugins by search text
        /// </summary>
        private void FilterPlugins(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText == _searchPlaceholder)
            {
                DisplayPlugins(_allPlugins);
                return;
            }

            var filtered = _allPlugins.FindAll(p =>
                p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.PackageName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.GetLocalizedDescription(LocalizationManager.Instance.CurrentLanguage).Contains(searchText, StringComparison.OrdinalIgnoreCase)
            );

            DisplayPlugins(filtered);
        }

        /// <summary>
        /// Create plugin card UI
        /// </summary>
        private Border CreatePluginCard(PluginMetadata plugin)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            var isThisPlugin = plugin.PackageName == _selfPackageName;

            var card = new Border
            {
                Style = (Style)FindResource("PluginCardStyle"),
                Tag = plugin.PackageName
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            // Icon
            var iconBorder = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 16, 0),
                Background = (Brush)FindResource("Background3Brush")
            };

            var iconText = new TextBlock
            {
                Text = "\U0001F9E9", // puzzle piece emoji
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);
            mainGrid.Children.Add(iconBorder);

            // Info Section
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Name row with badges
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var nameText = new TextBlock
            {
                Text = plugin.GetLocalizedName(lang),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            nameRow.Children.Add(nameText);

            // System badge
            if (plugin.IsSystemPlugin)
            {
                var systemBadge = CreateBadge(PluginManagerLocalization.Get(PluginManagerLocalization.System), "WarningBrush");
                nameRow.Children.Add(systemBadge);
            }

            // Installed badge
            var installedBadge = CreateBadge(PluginManagerLocalization.Get(PluginManagerLocalization.Installed), "SuccessBrush");
            nameRow.Children.Add(installedBadge);

            infoStack.Children.Add(nameRow);

            // Version & Manufacturer
            var metaText = new TextBlock
            {
                Text = $"v{plugin.Version} · {plugin.Manufacturer}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = (Brush)FindResource("Foreground4Brush")
            };
            infoStack.Children.Add(metaText);

            // Description
            var descText = new TextBlock
            {
                Text = plugin.GetLocalizedDescription(lang),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0),
                MaxWidth = 500,
                Foreground = (Brush)FindResource("Foreground3Brush")
            };
            infoStack.Children.Add(descText);

            Grid.SetColumn(infoStack, 1);
            mainGrid.Children.Add(infoStack);

            // Action Buttons
            var actionStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Launch button (not for self, not for non-launchable plugins)
            if (!isThisPlugin && plugin.CanLaunch)
            {
                var launchButton = new Button
                {
                    Style = (Style)FindResource("CardButtonStyle"),
                    Content = PluginManagerLocalization.Get(PluginManagerLocalization.Launch),
                    Tag = plugin.PackageName
                };
                launchButton.Click += LaunchButton_Click;
                actionStack.Children.Add(launchButton);
            }

            // Uninstall button (not for system plugins)
            if (!plugin.IsSystemPlugin)
            {
                var uninstallButton = new Button
                {
                    Style = (Style)FindResource("DangerButtonStyle"),
                    Content = PluginManagerLocalization.Get(PluginManagerLocalization.Uninstall),
                    Tag = plugin
                };
                uninstallButton.Click += UninstallButton_Click;
                actionStack.Children.Add(uninstallButton);
            }

            Grid.SetColumn(actionStack, 2);
            mainGrid.Children.Add(actionStack);

            card.Child = mainGrid;
            return card;
        }

        /// <summary>
        /// Create badge UI
        /// </summary>
        private Border CreateBadge(string text, string colorResourceKey)
        {
            var badge = new Border
            {
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(8, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
            };

            var badgeText = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = (Brush)FindResource(colorResourceKey)
            };

            badge.Child = badgeText;
            return badge;
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string packageName)
            {
                try
                {
                    SetStatus(PluginManagerLocalization.GetFormat(PluginManagerLocalization.Launching, packageName));
                    var result = await PMPlugin.Instance.Launch(packageName);
                    if (!result.Success)
                    {
                        await PCDialogPlugin.ErrorDialogAsync(result.Message, PluginManagerLocalization.Get(PluginManagerLocalization.Title));
                    }
                    SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Ready));
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to launch plugin {packageName}: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PluginManagerLocalization.Get(PluginManagerLocalization.Title));
                    SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Ready));
                }
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PluginMetadata plugin)
            {
                try
                {
                    var lang = LocalizationManager.Instance.CurrentLanguage;
                    var pluginName = plugin.GetLocalizedName(lang);

                    // Show confirmation dialog
                    var confirmed = await PCDialogPlugin.ConfirmDialogAsync(
                        PluginManagerLocalization.GetFormat(PluginManagerLocalization.ConfirmUninstallMessage, pluginName),
                        PluginManagerLocalization.Get(PluginManagerLocalization.ConfirmUninstall));

                    if (confirmed)
                    {
                        SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Uninstalling));
                        var result = await PMPlugin.Instance.Uninstall(plugin.PackageName);
                        if (!result.Success)
                        {
                            await PCDialogPlugin.ErrorDialogAsync(result.Message, PluginManagerLocalization.Get(PluginManagerLocalization.ConfirmUninstall));
                        }
                        await RefreshPluginList();
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to uninstall plugin: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PluginManagerLocalization.Get(PluginManagerLocalization.ConfirmUninstall));
                    SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Ready));
                }
            }
        }

        /// <summary>
        /// Install plugin from local file
        /// </summary>
        private async Task InstallPlugin()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                Title = PluginManagerLocalization.Get(PluginManagerLocalization.SelectDll)
            };

            if (dialog.ShowDialog() == true)
            {
                SetStatus(PluginManagerLocalization.Get(PluginManagerLocalization.Installing));
                var result = await PMPlugin.Instance.Install(dialog.FileName);
                SetStatus(result.Message);

                if (result.Success)
                {
                    await RefreshPluginList();
                }
            }
        }

        /// <summary>
        /// Set status message
        /// </summary>
        private void SetStatus(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }
    }
}
