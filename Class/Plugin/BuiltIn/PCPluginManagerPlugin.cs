using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Phobos.Manager.Plugin;
using Phobos.Shared.Class;
using Phobos.Shared.Interface;

namespace Phobos.Class.Plugin.BuiltIn
{
    /// <summary>
    /// 插件管理器插件
    /// </summary>
    public class PCPluginManagerPlugin : PCPluginBase
    {
        private Grid? _contentGrid;
        private ListBox? _pluginListBox;
        private TextBlock? _statusText;

        public override PluginMetadata Metadata { get; } = new PluginMetadata
        {
            Name = "Plugin Manager",
            PackageName = "com.phobos.plugin.manager",
            Manufacturer = "Phobos Team",
            Version = "1.0.0",
            Secret = "phobos_plugin_manager_secret_jaso19d81las",
            DatabaseKey = "pm",
            LocalizedNames = new Dictionary<string, string>
            {
                { "en-US", "Plugin Manager" },
                { "zh-CN", "插件管理器" },
                { "zh-TW", "插件管理器" },
                { "ja-JP", "プラグインマネージャー" },
                { "ko-KR", "플러그인 관리자" }
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                { "en-US", "Manage installed plugins" },
                { "zh-CN", "管理已安装的插件" },
                { "zh-TW", "管理已安裝的插件" },
                { "ja-JP", "インストールされたプラグインを管理" },
                { "ko-KR", "설치된 플러그인 관리" }
            },
            Entry = "Phobos.Plugin.Manager()",
            IsSystemPlugin = true
        };

        public override FrameworkElement? ContentArea => _contentGrid;

        public override async Task<RequestResult> OnInstall(params object[] args)
        {
            // 注册协议
            await Link(new LinkAssociation
            {
                Protocol = "pm",
                Name = "PluginManagerHandler_General",
                Description = "Plugin Manager Protocol Handler",
                Command = "pm://v1?action=%0"
            });

            await Link(new LinkAssociation
            {
                Protocol = "Phobos.PluginManager",
                Name = "PluginManagerHandler_General",
                Description = "Plugin Manager Protocol Handler",
                Command = "pm://v1?action=%0"
            });

            return await base.OnInstall(args);
        }

        public override async Task<RequestResult> OnLaunch(params object[] args)
        {
            InitializeUI();
            await RefreshPluginList();
            return await base.OnLaunch(args);
        }

        private void InitializeUI()
        {
            _contentGrid = new Grid
            {
                Margin = new Thickness(10)
            };

            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            _contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            // 标题
            var titleLabel = new TextBlock
            {
                Text = Metadata.GetLocalizedName(Shared.Class.LocalizationManager.Instance.CurrentLanguage),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(titleLabel, 0);

            // 插件列表
            _pluginListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(_pluginListBox, 1);

            // 按钮区
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 5)
            };
            Grid.SetRow(buttonPanel, 2);

            var installButton = CreateButton("Install", async () => await InstallPlugin());
            var uninstallButton = CreateButton("Uninstall", async () => await UninstallSelectedPlugin());
            var refreshButton = CreateButton("Refresh", async () => await RefreshPluginList());
            var launchButton = CreateButton("Launch", async () => await LaunchSelectedPlugin());

            buttonPanel.Children.Add(installButton);
            buttonPanel.Children.Add(uninstallButton);
            buttonPanel.Children.Add(refreshButton);
            buttonPanel.Children.Add(launchButton);

            // 状态栏
            _statusText = new TextBlock
            {
                Text = "Ready",
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_statusText, 3);

            _contentGrid.Children.Add(titleLabel);
            _contentGrid.Children.Add(_pluginListBox);
            _contentGrid.Children.Add(buttonPanel);
            _contentGrid.Children.Add(_statusText);
        }

        private Button CreateButton(string text, Func<Task> onClick)
        {
            var button = new Button
            {
                Content = text,
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            button.Click += async (s, e) =>
            {
                try
                {
                    button.IsEnabled = false;
                    await onClick();
                }
                finally
                {
                    button.IsEnabled = true;
                }
            };

            return button;
        }

        private async Task RefreshPluginList()
        {
            if (_pluginListBox == null) return;

            SetStatus("Loading plugins...");

            var plugins = await PMPlugin.Instance.GetInstalledPlugins();

            _pluginListBox.Items.Clear();
            foreach (var plugin in plugins)
            {
                var item = new ListBoxItem
                {
                    Content = $"{plugin.GetLocalizedName(Shared.Class.LocalizationManager.Instance.CurrentLanguage)} ({plugin.PackageName}) - v{plugin.Version}",
                    Tag = plugin.PackageName,
                    Foreground = Brushes.White
                };
                _pluginListBox.Items.Add(item);
            }

            SetStatus($"Loaded {plugins.Count} plugins");
        }

        private async Task InstallPlugin()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                Title = "Select Plugin DLL"
            };

            if (dialog.ShowDialog() == true)
            {
                SetStatus("Installing...");
                var result = await PMPlugin.Instance.Install(dialog.FileName);
                SetStatus(result.Message);

                if (result.Success)
                {
                    await RefreshPluginList();
                }
            }
        }

        private async Task UninstallSelectedPlugin()
        {
            if (_pluginListBox?.SelectedItem is not ListBoxItem item)
            {
                SetStatus("Please select a plugin");
                return;
            }

            var packageName = item.Tag?.ToString();
            if (string.IsNullOrEmpty(packageName))
                return;

            if (packageName == Metadata.PackageName)
            {
                SetStatus("Cannot uninstall Plugin Manager");
                return;
            }

            SetStatus("Uninstalling...");
            var result = await PMPlugin.Instance.Uninstall(packageName);
            SetStatus(result.Message);

            if (result.Success)
            {
                await RefreshPluginList();
            }
        }

        private async Task LaunchSelectedPlugin()
        {
            if (_pluginListBox?.SelectedItem is not ListBoxItem item)
            {
                SetStatus("Please select a plugin");
                return;
            }

            var packageName = item.Tag?.ToString();
            if (string.IsNullOrEmpty(packageName))
                return;

            SetStatus($"Launching {packageName}...");
            var result = await PMPlugin.Instance.Launch(packageName);
            SetStatus(result.Message);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
            }
        }

        public override async Task<RequestResult> Run(params object[] args)
        {
            if (args.Length > 0 && args[0] is string action)
            {
                switch (action.ToLowerInvariant())
                {
                    case "refresh":
                        await RefreshPluginList();
                        return new RequestResult { Success = true, Message = "Refreshed" };

                    case "list":
                        var plugins = await PMPlugin.Instance.GetInstalledPlugins();
                        return new RequestResult
                        {
                            Success = true,
                            Message = $"{plugins.Count} plugins",
                            Data = new List<object>(plugins)
                        };
                }
            }

            return await base.Run(args);
        }
    }
}