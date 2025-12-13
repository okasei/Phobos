using Phobos.Class.Plugin.BuiltIn;
using Phobos.Components.Plugin;
using Phobos.Manager.Arcusrix;
using Phobos.Manager.Permission;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;
using Phobos.Utils.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Phobos.Components.Arcusrix.PluginManager
{
    /// <summary>
    /// Plugin Manager Localization Helper - Uses JSON-based localization
    /// </summary>
    public static class PMLocalization
    {
        private static PluginLocalizationContext? _context;

        public static void Initialize()
        {
            var basePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "Localization", "PluginManager");

            _context = LocalizationManager.Instance.RegisterPlugin("com.phobos.plugin.manager", basePath);
        }

        public static string Get(string key)
        {
            if (_context == null) Initialize();
            return _context?.Get(key) ?? key;
        }

        public static string GetFormat(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }

    /// <summary>
    /// Supported languages and special protocol types
    /// </summary>
    public static class SupportedLanguages
    {
        public static readonly List<string> Languages = new()
        {
            "en-US", "zh-CN", "zh-TW", "ja-JP", "fr-FR", "de-DE", "ru-RU", "es-ES"
        };

        public static readonly Dictionary<string, string> SpecialProtocols = new()
        {
            { "text", "protocols.text" },
            { "image", "protocols.image" },
            { "video", "protocols.video" },
            { "browser", "protocols.browser" },
            { "launcher", "protocols.launcher" },
            { "runner", "protocols.runner" },
            { "auth", "protocols.auth" }
        };
    }

    /// <summary>
    /// PCOPluginManager.xaml - Plugin Manager with left navigation
    /// </summary>
    public partial class PCOPluginManager : UserControl
    {
        private List<PluginMetadata> _allPlugins = new();
        private string _selfPackageName = "com.phobos.plugin.manager";
        private PCPluginManager? _pm;
        private bool _isMenuExpanded = true;
        private const double ExpandedWidth = 220;
        private const double CollapsedWidth = 56;

        public PCOPluginManager(PCPluginManager pm)
        {
            InitializeComponent();
            _pm = pm;
            PMLocalization.Initialize();
            Loaded += PCOPluginManager_Loaded;
        }

        private async void PCOPluginManager_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLocalizedText();
            SetActiveNav(NavPlugins);
            await RefreshPluginList();
            await LoadProtocols();
            LoadLanguageSettings();
        }

        #region Localization

        private void UpdateLocalizedText()
        {
            // Navigation
            NavTitle.Text = PMLocalization.Get("pm.title");
            NavPluginsText.Text = PMLocalization.Get("tab.plugins");
            NavProtocolsText.Text = PMLocalization.Get("tab.protocols");
            NavLanguageText.Text = PMLocalization.Get("tab.language");
            NavPermissionsText.Text = PMLocalization.Get("tab.permissions");

            // Plugins Page
            PluginsTitleText.Text = PMLocalization.Get("plugins.title");
            SearchBox.Tag = PMLocalization.Get("plugins.search");
            RefreshButtonText.Text = PMLocalization.Get("plugins.refresh");
            InstallButtonText.Text = PMLocalization.Get("plugins.install");

            // Protocols Page
            ProtocolsTitleText.Text = PMLocalization.Get("protocols.title");
            ProtocolsSubtitleText.Text = PMLocalization.Get("protocols.subtitle");
            SpecialProtocolsTitle.Text = PMLocalization.Get("protocols.special");
            AssociatedItemsTitle.Text = PMLocalization.Get("protocols.associated_items");

            // Language Page
            LanguageTitleText.Text = PMLocalization.Get("language.title");
            LanguageSubtitleText.Text = PMLocalization.Get("language.subtitle");
            SystemLanguageTitle.Text = PMLocalization.Get("language.system");
            SystemLanguageDesc.Text = PMLocalization.Get("language.system_desc");
            PluginLanguageTitle.Text = PMLocalization.Get("language.plugin");
            PluginLanguageDesc.Text = PMLocalization.Get("language.plugin_desc");
            ApplyLanguageButtonText.Text = PMLocalization.Get("language.apply");

            // Permissions Page
            PermissionsTitleText.Text = PMLocalization.Get("permissions.title");
            PermissionsSubtitleText.Text = PMLocalization.Get("permissions.subtitle");
            PermissionsSearchBox.Tag = PMLocalization.Get("permissions.search");

            // Menu tooltip
            UpdateMenuTooltip();

            // Status
            StatusText.Text = PMLocalization.Get("plugins.ready");
        }

        private void UpdateMenuTooltip()
        {
            MenuTooltip.Text = _isMenuExpanded
                ? PMLocalization.Get("menu.collapse")
                : PMLocalization.Get("menu.expand");
        }

        #endregion

        #region Navigation

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu();
        }

        private void ToggleMenu()
        {
            _isMenuExpanded = !_isMenuExpanded;
            double targetWidth = _isMenuExpanded ? ExpandedWidth : CollapsedWidth;

            PUAnimation.AnimateGridLength(NavColumn, NavColumn.Width, new GridLength(targetWidth));

            // Toggle text visibility
            var textOpacity = _isMenuExpanded ? 1.0 : 0.0;
            PUAnimation.AnimateOpacityTo(NavTitle, textOpacity);
            PUAnimation.AnimateOpacityTo(NavPluginsText, textOpacity);
            PUAnimation.AnimateOpacityTo(NavProtocolsText, textOpacity);
            PUAnimation.AnimateOpacityTo(NavLanguageText, textOpacity);
            PUAnimation.AnimateOpacityTo(NavPermissionsText, textOpacity);

            UpdateMenuTooltip();
        }

        private void NavPlugins_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavPlugins);
            ShowPage(PluginsPage);
        }

        private void NavProtocols_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavProtocols);
            ShowPage(ProtocolsPage);
        }

        private void NavLanguage_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavLanguage);
            ShowPage(LanguagePage);
        }

        private void NavPermissions_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavPermissions);
            ShowPage(PermissionsPage);
            _ = LoadPermissionsAsync();
        }

        private void SetActiveNav(Button activeButton)
        {
            NavPlugins.Tag = null;
            NavProtocols.Tag = null;
            NavLanguage.Tag = null;
            NavPermissions.Tag = null;
            activeButton.Tag = "Active";
        }

        private void ShowPage(UIElement page)
        {
            // Fade out all pages
            var pages = new[] { PluginsPage, ProtocolsPage, LanguagePage, PermissionsPage };
            foreach (var p in pages)
            {
                if (p != page && p.Visibility == Visibility.Visible)
                {
                    PUAnimation.PageOut(p);
                }
            }

            // Fade in target page
            PUAnimation.PageIn(page);
        }

        #endregion

        #region Plugins Tab

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
            FilterPlugins(SearchBox.Text);
        }

        public async Task RefreshPluginList()
        {
            SetStatus(PMLocalization.Get("plugins.loading"));
            _allPlugins = await PMPlugin.Instance.GetInstalledPlugins();
            DisplayPlugins(_allPlugins);
            SetStatus(PMLocalization.Get("plugins.ready"));
        }

        private void DisplayPlugins(List<PluginMetadata> plugins)
        {
            PluginList.Items.Clear();
            foreach (var plugin in plugins)
            {
                PluginList.Items.Add(CreatePluginCard(plugin));
            }

            PluginCountText.Text = PMLocalization.GetFormat("plugins.count", plugins.Count);
        }

        private void FilterPlugins(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
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
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
                Text = "\uEA86",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("PrimaryBrush")
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

            if (plugin.IsSystemPlugin)
            {
                var systemBadge = CreateBadge(PMLocalization.Get("plugins.system"), "WarningBrush");
                nameRow.Children.Add(systemBadge);
            }

            if (plugin.CanLaunch)
            {
                var launchableBadge = CreateBadge(PMLocalization.Get("plugins.launchable"), "SuccessBrush");
                nameRow.Children.Add(launchableBadge);
            }

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

            // Settings button (if SettingUri exists)
            if (!string.IsNullOrEmpty(plugin.SettingUri))
            {
                var settingsButton = new Button
                {
                    Style = (Style)FindResource("PhobosButtonSecondary"),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = plugin.PackageName,
                    ToolTip = PMLocalization.Get("plugins.settings")
                };

                var settingsContent = new StackPanel { Orientation = Orientation.Horizontal };
                settingsContent.Children.Add(new TextBlock
                {
                    Text = "\uE713",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                settingsContent.Children.Add(new TextBlock
                {
                    Text = PMLocalization.Get("plugins.settings"),
                    Margin = new Thickness(6, 0, 0, 0)
                });
                settingsButton.Content = settingsContent;
                settingsButton.Click += SettingsButton_Click;
                actionStack.Children.Add(settingsButton);
            }

            // Launch button
            if (!isThisPlugin && plugin.CanLaunch)
            {
                var launchButton = new Button
                {
                    Style = (Style)FindResource("PhobosButton"),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = plugin.PackageName
                };

                var launchContent = new StackPanel { Orientation = Orientation.Horizontal };
                launchContent.Children.Add(new TextBlock
                {
                    Text = "\uE768",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                launchContent.Children.Add(new TextBlock
                {
                    Text = PMLocalization.Get("plugins.launch"),
                    Margin = new Thickness(6, 0, 0, 0)
                });
                launchButton.Content = launchContent;
                launchButton.Click += LaunchButton_Click;
                actionStack.Children.Add(launchButton);
            }

            // Uninstall button
            if (!plugin.IsSystemPlugin)
            {
                var uninstallButton = new Button
                {
                    Style = (Style)FindResource("PhobosButtonSecondary"),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = plugin,
                    ToolTip = PMLocalization.Get("plugins.uninstall")
                };

                var uninstallContent = new StackPanel { Orientation = Orientation.Horizontal };
                uninstallContent.Children.Add(new TextBlock
                {
                    Text = "\uE74D",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (Brush)FindResource("ErrorBrush")
                });
                uninstallContent.Children.Add(new TextBlock
                {
                    Text = PMLocalization.Get("plugins.uninstall"),
                    Margin = new Thickness(6, 0, 0, 0),
                    Foreground = (Brush)FindResource("ErrorBrush")
                });
                uninstallButton.Content = uninstallContent;
                uninstallButton.Click += UninstallButton_Click;
                actionStack.Children.Add(uninstallButton);
            }

            Grid.SetColumn(actionStack, 2);
            mainGrid.Children.Add(actionStack);

            card.Child = mainGrid;
            return card;
        }

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

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string packageName)
            {
                try
                {
                    var plugin = _allPlugins.FirstOrDefault(p => p.PackageName == packageName);
                    if (plugin != null && !string.IsNullOrEmpty(plugin.SettingUri))
                    {
                        // Launch the plugin with settings URI as argument
                        await PMPlugin.Instance.Launch(packageName, "settings", plugin.SettingUri);
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to open settings for {packageName}: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("pm.title"));
                }
            }
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string packageName)
            {
                try
                {
                    SetStatus(PMLocalization.GetFormat("plugins.launching", packageName));
                    var result = await PMPlugin.Instance.Launch(packageName);
                    if (!result.Success)
                    {
                        await PCDialogPlugin.ErrorDialogAsync(result.Message, PMLocalization.Get("pm.title"));
                    }
                    SetStatus(PMLocalization.Get("plugins.ready"));
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to launch plugin {packageName}: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("pm.title"));
                    SetStatus(PMLocalization.Get("plugins.ready"));
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

                    var confirmed = await PCDialogPlugin.ConfirmDialogAsync(
                        PMLocalization.GetFormat("plugins.confirm_uninstall_message", pluginName),
                        PMLocalization.Get("plugins.confirm_uninstall"));

                    if (confirmed)
                    {
                        SetStatus(PMLocalization.Get("plugins.uninstalling"));
                        var result = await PMPlugin.Instance.Uninstall(plugin.PackageName);
                        if (!result.Success)
                        {
                            await PCDialogPlugin.ErrorDialogAsync(result.Message, PMLocalization.Get("plugins.confirm_uninstall"));
                        }
                        await RefreshPluginList();
                        if (_pm != null)
                            await _pm.TriggerEvent("App", "Uninstalled", plugin.PackageName, plugin.Name);
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to uninstall plugin: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("plugins.confirm_uninstall"));
                    SetStatus(PMLocalization.Get("plugins.ready"));
                }
            }
        }

        private async Task InstallPlugin()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                Title = PMLocalization.Get("plugins.select_dll")
            };

            if (dialog.ShowDialog() == true)
            {
                SetStatus(PMLocalization.Get("plugins.installing"));
                var result = await PMPlugin.Instance.Install(dialog.FileName);
                SetStatus(result.Message);

                if (result.Success)
                {
                    await RefreshPluginList();
                    if (_pm != null)
                        await _pm.TriggerEvent("App", "Installed", result.Data.Count > 0 ? result.Data[0] ?? "" : "", result.Data.Count > 1 ? result.Data[1] ?? "" : "");
                }
            }
        }

        #endregion

        #region Protocols Tab

        private async Task LoadProtocols()
        {
            await LoadSpecialProtocols();
            await LoadAssociatedItems();
        }

        private async Task LoadSpecialProtocols()
        {
            SpecialProtocolList.Items.Clear();

            foreach (var protocol in SupportedLanguages.SpecialProtocols)
            {
                var card = await CreateSpecialProtocolCard(protocol.Key, protocol.Value);
                SpecialProtocolList.Items.Add(card);
            }
        }

        private async Task<Border> CreateSpecialProtocolCard(string protocolType, string locKey)
        {
            var card = new Border
            {
                Style = (Style)FindResource("SettingsCardStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconText = new TextBlock
            {
                Text = GetProtocolIcon(protocolType),
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                Width = 32,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("PrimaryBrush")
            };
            Grid.SetColumn(iconText, 0);
            grid.Children.Add(iconText);

            // Info
            var infoStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };

            var titleText = new TextBlock
            {
                Text = PMLocalization.Get(locKey),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            infoStack.Children.Add(titleText);

            // Get current handler
            var currentHandler = await GetDefaultHandler(protocolType);
            var handlerText = new TextBlock
            {
                Text = string.IsNullOrEmpty(currentHandler)
                    ? PMLocalization.Get("protocols.no_handler")
                    : currentHandler,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = (Brush)FindResource("Foreground4Brush")
            };
            infoStack.Children.Add(handlerText);

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            // Select button
            var selectButton = new Button
            {
                Style = (Style)FindResource("PhobosButtonSecondary"),
                Tag = protocolType
            };

            var selectContent = new StackPanel { Orientation = Orientation.Horizontal };
            selectContent.Children.Add(new TextBlock
            {
                Text = "\uE70F",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });
            selectContent.Children.Add(new TextBlock
            {
                Text = PMLocalization.Get("protocols.select_handler"),
                Margin = new Thickness(6, 0, 0, 0)
            });
            selectButton.Content = selectContent;
            selectButton.Click += SelectProtocolHandler_Click;

            Grid.SetColumn(selectButton, 2);
            grid.Children.Add(selectButton);

            card.Child = grid;
            return card;
        }

        private string GetProtocolIcon(string protocolType)
        {
            return protocolType switch
            {
                "text" => "\uE8A5",
                "image" => "\uEB9F",
                "video" => "\uE714",
                "browser" => "\uE774",
                "launcher" => "\uE7FC",
                "runner" => "\uE756",
                "auth" => "\uE72E",
                _ => "\uE71B"
            };
        }

        private async Task<string> GetDefaultHandler(string protocolType)
        {
            try
            {
                // Use PMProtocol to get the default handler info
                var info = await PMProtocol.Instance.GetProtocolAssociationInfo(protocolType);
                if (info != null)
                {
                    return $"{info.PackageName} - {info.Description}";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        private async void SelectProtocolHandler_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string protocolType)
            {
                try
                {
                    // Use the built-in handler selection dialog from PMProtocol
                    var (selectedHandler, setAsDefault) = await PMProtocol.Instance.ShowDefaultHandlerDialog(
                        protocolType,
                        title: PMLocalization.Get("protocols.select_handler"),
                        subtitle: PMLocalization.Get(SupportedLanguages.SpecialProtocols.GetValueOrDefault(protocolType, "protocols.title")));

                    if (selectedHandler != null)
                    {
                        SetStatus(PMLocalization.Get("protocols.set_default_success"));
                        await LoadSpecialProtocols();
                        await LoadAssociatedItems();
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to select handler: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("pm.title"));
                }
            }
        }

        private async Task LoadAssociatedItems()
        {
            AssociatedItemList.Items.Clear();

            try
            {
                // Load all protocol associations from database
                var items = await LoadAssociatedItemsFromDatabase();
                foreach (var item in items)
                {
                    var card = CreateAssociatedItemCard(item);
                    AssociatedItemList.Items.Add(card);
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("PluginManager", $"Failed to load associated items: {ex.Message}");
            }
        }

        private async Task<List<AssociatedItem>> LoadAssociatedItemsFromDatabase()
        {
            var result = new List<AssociatedItem>();
            try
            {
                // Load all special protocol bindings
                foreach (var protocol in SupportedLanguages.SpecialProtocols.Keys)
                {
                    var info = await PMProtocol.Instance.GetProtocolAssociationInfo(protocol);
                    if (info != null)
                    {
                        result.Add(new AssociatedItem
                        {
                            Protocol = protocol,
                            Handler = info.PackageName,
                            Command = info.Command
                        });
                    }
                }
            }
            catch { }
            return result;
        }

        private Border CreateAssociatedItemCard(AssociatedItem item)
        {
            var card = new Border
            {
                Style = (Style)FindResource("SettingsCardStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Protocol
            var protocolText = new TextBlock
            {
                Text = item.Protocol,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("PrimaryBrush")
            };
            Grid.SetColumn(protocolText, 0);
            grid.Children.Add(protocolText);

            // Handler
            var handlerText = new TextBlock
            {
                Text = item.Handler,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("Foreground2Brush")
            };
            Grid.SetColumn(handlerText, 1);
            grid.Children.Add(handlerText);

            // Command
            var commandText = new TextBlock
            {
                Text = item.Command,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)FindResource("Foreground4Brush"),
                ToolTip = item.Command
            };
            Grid.SetColumn(commandText, 2);
            grid.Children.Add(commandText);

            card.Child = grid;
            return card;
        }

        #endregion

        #region Language Tab

        private async void LoadLanguageSettings()
        {
            await LoadSystemLanguageAsync();
            await LoadPluginLanguagesAsync();
        }

        private async Task LoadSystemLanguageAsync()
        {
            SystemLanguageCombo.Items.Clear();

            foreach (var lang in SupportedLanguages.Languages)
            {
                var item = new ComboBoxItem
                {
                    Content = PMLocalization.Get($"lang.{lang}"),
                    Tag = lang
                };
                SystemLanguageCombo.Items.Add(item);

                if (lang == LocalizationManager.Instance.CurrentLanguage)
                {
                    SystemLanguageCombo.SelectedItem = item;
                }
            }
        }

        private async Task LoadPluginLanguagesAsync()
        {
            PluginLanguageList.Items.Clear();

            foreach (var plugin in _allPlugins)
            {
                var card = await CreatePluginLanguageCardAsync(plugin);
                PluginLanguageList.Items.Add(card);
            }
        }

        private async Task<Border> CreatePluginLanguageCardAsync(PluginMetadata plugin)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;

            var card = new Border
            {
                Style = (Style)FindResource("SettingsCardStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Plugin info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = plugin.GetLocalizedName(lang),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            infoStack.Children.Add(nameText);

            var packageText = new TextBlock
            {
                Text = plugin.PackageName,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (Brush)FindResource("Foreground4Brush")
            };
            infoStack.Children.Add(packageText);

            Grid.SetColumn(infoStack, 0);
            grid.Children.Add(infoStack);

            // Language selector
            var langCombo = new ComboBox
            {
                Style = (Style)FindResource("PhobosComboBox"),
                Width = 180,
                Tag = plugin.PackageName
            };

            // Add "Follow System" option
            var followSystemItem = new ComboBoxItem
            {
                Content = PMLocalization.Get("language.follow_system"),
                Tag = "system"
            };
            langCombo.Items.Add(followSystemItem);

            var currentPluginLang = await GetPluginLanguageSettingAsync(plugin.PackageName);

            foreach (var langCode in SupportedLanguages.Languages)
            {
                var item = new ComboBoxItem
                {
                    Content = PMLocalization.Get($"lang.{langCode}"),
                    Tag = langCode
                };
                langCombo.Items.Add(item);

                if (langCode == currentPluginLang)
                {
                    langCombo.SelectedItem = item;
                }
            }

            if (currentPluginLang == "system" || string.IsNullOrEmpty(currentPluginLang))
            {
                langCombo.SelectedItem = followSystemItem;
            }

            langCombo.SelectionChanged += PluginLanguageCombo_SelectionChanged;

            Grid.SetColumn(langCombo, 1);
            grid.Children.Add(langCombo);

            card.Child = grid;
            return card;
        }

        private async Task<string> GetPluginLanguageSettingAsync(string packageName)
        {
            try
            {
                // First try to read from database
                if (_pm != null)
                {
                    var result = await _pm.ReadConfig($"PluginLang_{packageName}");
                    if (result.Success && !string.IsNullOrEmpty(result.Value))
                    {
                        // Update in-memory setting to match database
                        LocalizationManager.Instance.SetPluginLanguage(packageName, result.Value);
                        return result.Value;
                    }
                }

                // Fallback to in-memory setting
                var context = LocalizationManager.Instance.GetPluginContext(packageName);
                return context.LanguageSetting;
            }
            catch
            {
                return "system";
            }
        }

        private void SystemLanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle in ApplyLanguageButton_Click
        }

        private async void PluginLanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.Tag is string packageName && combo.SelectedItem is ComboBoxItem item)
            {
                var selectedLang = item.Tag?.ToString() ?? "system";
                try
                {
                    // Update in-memory setting
                    LocalizationManager.Instance.SetPluginLanguage(packageName, selectedLang);

                    // Persist to database via PMPlugin WriteConfig
                    // Use a special key format: PluginLang_{packageName}
                    if (_pm != null)
                    {
                        var result = await _pm.WriteConfig($"PluginLang_{packageName}", selectedLang);
                        if (!result.Success)
                        {
                            PCLoggerPlugin.Warning("PluginManager", $"Failed to save plugin language for {packageName}: {result.Message}");
                        }
                    }

                    SetStatus(PMLocalization.Get("language.saved"));
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to save plugin language: {ex.Message}");
                }
            }
        }

        private async void ApplyLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (SystemLanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                try
                {
                    // Set the current language
                    LocalizationManager.Instance.CurrentLanguage = langCode;

                    // Save to system config (Phobos_Main table) via WriteSysConfig
                    if (_pm != null)
                    {
                        var result = await _pm.WriteSysConfig("Language", langCode);
                        if (!result.Success)
                        {
                            PCLoggerPlugin.Warning("PluginManager", $"Failed to save language to database: {result.Message}");
                        }
                    }

                    // Refresh UI
                    PMLocalization.Initialize();
                    UpdateLocalizedText();
                    await RefreshPluginList();
                    await LoadPluginLanguagesAsync();

                    SetStatus(PMLocalization.Get("language.saved"));

                    await PCDialogPlugin.InfoDialogAsync(
                        PMLocalization.Get("language.restart_required"),
                        PMLocalization.Get("language.title"));
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to apply language: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("language.title"));
                }
            }
        }

        #endregion

        #region Permissions Tab

        private List<PluginMetadata> _permissionsPluginList = new();

        private void PermissionsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterPermissionsPlugins(PermissionsSearchBox.Text);
        }

        private async Task LoadPermissionsAsync()
        {
            _permissionsPluginList = await PMPlugin.Instance.GetInstalledPlugins();
            DisplayPermissionsPlugins(_permissionsPluginList);
        }

        private void FilterPermissionsPlugins(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                DisplayPermissionsPlugins(_permissionsPluginList);
                return;
            }

            var filtered = _permissionsPluginList.FindAll(p =>
                p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.PackageName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            );

            DisplayPermissionsPlugins(filtered);
        }

        private void DisplayPermissionsPlugins(List<PluginMetadata> plugins)
        {
            PermissionsList.Items.Clear();

            // Filter out system plugins that have all default permissions
            var manageable = plugins.Where(p => !p.IsSystemPlugin).ToList();

            if (manageable.Count == 0)
            {
                var noPluginsText = new TextBlock
                {
                    Text = PMLocalization.Get("permissions.no_plugins"),
                    FontSize = 14,
                    Foreground = (Brush)FindResource("Foreground4Brush"),
                    Margin = new Thickness(0, 20, 0, 0)
                };
                PermissionsList.Items.Add(noPluginsText);
                return;
            }

            foreach (var plugin in manageable)
            {
                var card = CreatePermissionCard(plugin);
                PermissionsList.Items.Add(card);
            }
        }

        private Border CreatePermissionCard(PluginMetadata plugin)
        {
            var lang = LocalizationManager.Instance.CurrentLanguage;
            var permissionRecord = PMPermission.Instance.GetPermissionRecord(plugin.PackageName);

            var card = new Border
            {
                Style = (Style)FindResource("PluginCardStyle"),
                Tag = plugin.PackageName
            };

            var mainStack = new StackPanel();

            // Header row with plugin info and trust toggle
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 12, 0),
                Background = (Brush)FindResource("Background3Brush")
            };

            var iconText = new TextBlock
            {
                Text = "\uE8D7",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("PrimaryBrush")
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);
            headerGrid.Children.Add(iconBorder);

            // Plugin info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var nameText = new TextBlock
            {
                Text = plugin.GetLocalizedName(lang),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            nameRow.Children.Add(nameText);

            if (permissionRecord.IsTrusted)
            {
                var trustedBadge = CreateBadge(PMLocalization.Get("permissions.trusted"), "SuccessBrush");
                nameRow.Children.Add(trustedBadge);
            }

            infoStack.Children.Add(nameRow);

            var packageText = new TextBlock
            {
                Text = plugin.PackageName,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (Brush)FindResource("Foreground4Brush")
            };
            infoStack.Children.Add(packageText);

            Grid.SetColumn(infoStack, 1);
            headerGrid.Children.Add(infoStack);

            // Trust button
            var trustButton = new Button
            {
                Style = (Style)FindResource(permissionRecord.IsTrusted ? "PhobosButtonSecondary" : "PhobosButton"),
                Tag = plugin,
                Margin = new Thickness(8, 0, 0, 0)
            };

            var trustContent = new StackPanel { Orientation = Orientation.Horizontal };
            trustContent.Children.Add(new TextBlock
            {
                Text = permissionRecord.IsTrusted ? "\uE8FB" : "\uE8FA",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });
            trustContent.Children.Add(new TextBlock
            {
                Text = permissionRecord.IsTrusted
                    ? PMLocalization.Get("permissions.remove_trusted")
                    : PMLocalization.Get("permissions.set_trusted"),
                Margin = new Thickness(6, 0, 0, 0)
            });
            trustButton.Content = trustContent;
            trustButton.Click += TrustButton_Click;

            Grid.SetColumn(trustButton, 2);
            headerGrid.Children.Add(trustButton);

            mainStack.Children.Add(headerGrid);

            // Permission list (only show if not trusted)
            if (!permissionRecord.IsTrusted)
            {
                var permissionsGrid = CreatePermissionsGrid(plugin.PackageName, permissionRecord);
                permissionsGrid.Margin = new Thickness(0, 16, 0, 0);
                mainStack.Children.Add(permissionsGrid);
            }

            card.Child = mainStack;
            return card;
        }

        private Grid CreatePermissionsGrid(string packageName, PermissionRecord record)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var permissionsStack = new StackPanel();

            // All standard permissions
            var allPermissions = new[]
            {
                PhobosPermissions.NOTIFICATION_SEND,
                PhobosPermissions.AUTOSTART,
                PhobosPermissions.LINKSTART,
                PhobosPermissions.READ_SYS,
                PhobosPermissions.WRITE_SYS,
                PhobosPermissions.PLUGINSETTING_READ,
                PhobosPermissions.PLUGINSETTING_WRITE,
                PhobosPermissions.DESKTOPITEM
            };

            foreach (var permission in allPermissions)
            {
                var row = CreatePermissionRow(packageName, permission, record);
                permissionsStack.Children.Add(row);
            }

            Grid.SetColumn(permissionsStack, 0);
            Grid.SetColumnSpan(permissionsStack, 2);
            grid.Children.Add(permissionsStack);

            return grid;
        }

        private Border CreatePermissionRow(string packageName, string permission, PermissionRecord record)
        {
            var permissionState = PMPermission.Instance.GetPermissionState(packageName, permission);

            var row = new Border
            {
                Background = (Brush)FindResource("Background2Brush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Permission info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var permissionName = new TextBlock
            {
                Text = PMLocalization.Get($"permission.{permission}"),
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            infoStack.Children.Add(permissionName);

            var permissionDesc = new TextBlock
            {
                Text = PMLocalization.Get($"permission.{permission}.desc"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (Brush)FindResource("Foreground4Brush")
            };
            infoStack.Children.Add(permissionDesc);

            Grid.SetColumn(infoStack, 0);
            grid.Children.Add(infoStack);

            // Action buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Status indicator
            var statusText = new TextBlock
            {
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            switch (permissionState)
            {
                case PermissionState.Granted:
                    statusText.Text = PMLocalization.Get("permissions.granted");
                    statusText.Foreground = (Brush)FindResource("SuccessBrush");
                    break;
                case PermissionState.Denied:
                    statusText.Text = PMLocalization.Get("permissions.denied");
                    statusText.Foreground = (Brush)FindResource("ErrorBrush");
                    break;
                default:
                    statusText.Text = PMLocalization.Get("permissions.not_set");
                    statusText.Foreground = (Brush)FindResource("Foreground4Brush");
                    break;
            }
            buttonStack.Children.Add(statusText);

            // Grant button
            var grantButton = new Button
            {
                Style = (Style)FindResource("PhobosButtonSecondary"),
                Padding = new Thickness(8, 4, 8, 4),
                Tag = new PermissionActionTag { PackageName = packageName, Permission = permission },
                IsEnabled = permissionState != PermissionState.Granted
            };
            grantButton.Content = new TextBlock
            {
                Text = PMLocalization.Get("permissions.grant"),
                FontSize = 11
            };
            grantButton.Click += GrantPermission_Click;
            buttonStack.Children.Add(grantButton);

            // Deny button
            var denyButton = new Button
            {
                Style = (Style)FindResource("PhobosButtonSecondary"),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                Tag = new PermissionActionTag { PackageName = packageName, Permission = permission },
                IsEnabled = permissionState != PermissionState.Denied
            };
            var denyContent = new TextBlock
            {
                Text = PMLocalization.Get("permissions.deny"),
                FontSize = 11,
                Foreground = (Brush)FindResource("ErrorBrush")
            };
            denyButton.Content = denyContent;
            denyButton.Click += DenyPermission_Click;
            buttonStack.Children.Add(denyButton);

            // Reset button
            var resetButton = new Button
            {
                Style = (Style)FindResource("PhobosButtonSecondary"),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                Tag = new PermissionActionTag { PackageName = packageName, Permission = permission },
                IsEnabled = permissionState != PermissionState.NotSet
            };
            resetButton.Content = new TextBlock
            {
                Text = PMLocalization.Get("permissions.reset"),
                FontSize = 11
            };
            resetButton.Click += ResetPermission_Click;
            buttonStack.Children.Add(resetButton);

            Grid.SetColumn(buttonStack, 1);
            grid.Children.Add(buttonStack);

            row.Child = grid;
            return row;
        }

        private async void TrustButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PluginMetadata plugin)
            {
                var lang = LocalizationManager.Instance.CurrentLanguage;
                var pluginName = plugin.GetLocalizedName(lang);
                var record = PMPermission.Instance.GetPermissionRecord(plugin.PackageName);

                try
                {
                    if (record.IsTrusted)
                    {
                        // Confirm revoke trust
                        var confirmed = await PCDialogPlugin.ConfirmDialogAsync(
                            PMLocalization.GetFormat("permissions.confirm_revoke_trust_message", pluginName),
                            PMLocalization.Get("permissions.confirm_revoke_trust"));

                        if (confirmed)
                        {
                            await PMPermission.Instance.SetTrusted(plugin.PackageName, false);
                            SetStatus(PMLocalization.Get("permissions.saved"));
                            await LoadPermissionsAsync();
                        }
                    }
                    else
                    {
                        // Confirm trust
                        var confirmed = await PCDialogPlugin.ConfirmDialogAsync(
                            PMLocalization.GetFormat("permissions.confirm_trust_message", pluginName),
                            PMLocalization.Get("permissions.confirm_trust"));

                        if (confirmed)
                        {
                            await PMPermission.Instance.SetTrusted(plugin.PackageName, true);
                            SetStatus(PMLocalization.Get("permissions.saved"));
                            await LoadPermissionsAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to change trust status: {ex.Message}");
                    await PCDialogPlugin.ErrorDialogAsync(ex.Message, PMLocalization.Get("permissions.title"));
                }
            }
        }

        private async void GrantPermission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PermissionActionTag tag)
            {
                try
                {
                    await PMPermission.Instance.GrantPermission(tag.PackageName, tag.Permission);
                    SetStatus(PMLocalization.Get("permissions.saved"));
                    await LoadPermissionsAsync();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to grant permission: {ex.Message}");
                }
            }
        }

        private async void DenyPermission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PermissionActionTag tag)
            {
                try
                {
                    await PMPermission.Instance.DenyPermissionPermanently(tag.PackageName, tag.Permission);
                    SetStatus(PMLocalization.Get("permissions.saved"));
                    await LoadPermissionsAsync();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to deny permission: {ex.Message}");
                }
            }
        }

        private async void ResetPermission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PermissionActionTag tag)
            {
                try
                {
                    await PMPermission.Instance.ResetPermission(tag.PackageName, tag.Permission);
                    SetStatus(PMLocalization.Get("permissions.saved"));
                    await LoadPermissionsAsync();
                }
                catch (Exception ex)
                {
                    PCLoggerPlugin.Error("PluginManager", $"Failed to reset permission: {ex.Message}");
                }
            }
        }

        #endregion

        #region Helpers

        private void SetStatus(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }

        #endregion
    }

    /// <summary>
    /// Associated item from database
    /// </summary>
    public class AssociatedItem
    {
        public string Protocol { get; set; } = "";
        public string Handler { get; set; } = "";
        public string Command { get; set; } = "";
    }

    /// <summary>
    /// Permission action tag for button click handlers
    /// </summary>
    public class PermissionActionTag
    {
        public string PackageName { get; set; } = "";
        public string Permission { get; set; } = "";
    }
}
