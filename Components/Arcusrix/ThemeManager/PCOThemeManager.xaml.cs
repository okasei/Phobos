using Microsoft.Win32;
using Phobos.Class.Plugin.BuiltIn;
using Phobos.Shared.Interface;
using Phobos.Class.Theme;
using Phobos.Manager.Arcusrix;
using Phobos.Shared.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Phobos.Components.Arcusrix.Common;
using Phobos.Components.Arcusrix.ThemeManager.Helpers;
using Newtonsoft.Json;
using System.Windows.Media;

namespace Phobos.Components.Arcusrix.ThemeManager
{
    public partial class PCOThemeManager : UserControl
    {
        private List<ThemeInfo> _themes = new();
        private string? _selectedThemeId;
        private ResourceDictionary? _previewDictionary;
        private PCThemeConfig? _editingConfig;
        private bool _editingConfigChanged = false;

        public PCOThemeManager()
        {
            InitializeComponent();
            ThemeManagerLocalization.RegisterAll();
            ApplyLocalization();
            Loaded += PCOThemeManager_Loaded;
        }

        #region Localization

        private void ApplyLocalization()
        {
            // Header
            TitleText.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.Title);
            SubtitleText.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.Subtitle);

            // Buttons
            RefreshButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Refresh);
            ImportButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Import);
            NewButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Create);
            ApplyButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Apply);
            PreviewButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Preview);
            ExportButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Export);
            SaveButton.Content = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Save);

            // Search
            FilterBox.Tag = ThemeManagerLocalization.Get(ThemeManagerLocalization.List_Search);

            // Default text
            ThemeTitle.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.List_Select);
            CurrentBadgeText.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.List_Current);
            ColorEditorTitle.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Title);
            StatusText.Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_Ready);
        }

        #endregion

        private async void PCOThemeManager_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshThemesAsync();

            // 自动选中当前主题
            var currentThemeId = PMTheme.Instance.CurrentThemeId;
            if (!string.IsNullOrEmpty(currentThemeId))
            {
                SelectTheme(currentThemeId);
            }
        }

        #region Theme List

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshThemesAsync();
        }

        private async Task RefreshThemesAsync()
        {
            SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_Loading));
            await PMTheme.Instance.RefreshThemes();
            await Dispatcher.InvokeAsync(() =>
            {
                _themes = PMTheme.Instance.GetAvailableThemeInfos();
                DisplayThemes(_themes);
                SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Found, _themes.Count));
            });
        }

        /// <summary>
        /// 供外部调用的刷新方法（如主题安装/卸载事件触发时）
        /// </summary>
        public async Task RefreshThemesFromExternalAsync()
        {
            await RefreshThemesAsync();

            // 如果当前选中的主题已被卸载，重置选择
            if (!string.IsNullOrEmpty(_selectedThemeId))
            {
                var theme = _themes.FirstOrDefault(t => t.ThemeId == _selectedThemeId);
                if (theme == null)
                {
                    _selectedThemeId = null;
                    // 自动选中当前主题
                    var currentThemeId = PMTheme.Instance.CurrentThemeId;
                    if (!string.IsNullOrEmpty(currentThemeId))
                    {
                        SelectTheme(currentThemeId);
                    }
                }
            }
        }

        private void DisplayThemes(List<ThemeInfo> themes)
        {
            ThemeList.Items.Clear();
            foreach (var theme in themes)
            {
                ThemeList.Items.Add(CreateThemeCard(theme));
            }
        }

        private Border CreateThemeCard(ThemeInfo theme)
        {
            var isSelected = theme.ThemeId == _selectedThemeId;
            var isCurrent = theme.IsCurrent;

            var card = new Border
            {
                Style = (Style)FindResource(isSelected ? "ThemeCardSelectedStyle" : "ThemeCardStyle"),
                Tag = theme.ThemeId
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Theme info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameStack = new StackPanel { Orientation = Orientation.Horizontal };
            var nameText = new TextBlock
            {
                Text = theme.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            nameStack.Children.Add(nameText);

            if (isCurrent)
            {
                var currentBadge = new Border
                {
                    Background = (Brush)FindResource("PrimaryBrush"),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                currentBadge.Child = new TextBlock
                {
                    Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.List_Current),
                    FontSize = 10,
                    Foreground = (Brush)FindResource("Background1Brush")
                };
                nameStack.Children.Add(currentBadge);
            }

            infoStack.Children.Add(nameStack);

            var metaText = new TextBlock
            {
                Text = $"v{theme.Version} • {theme.Author}",
                Foreground = (Brush)FindResource("Foreground4Brush"),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            };
            infoStack.Children.Add(metaText);

            Grid.SetColumn(infoStack, 0);
            mainGrid.Children.Add(infoStack);

            card.Child = mainGrid;
            card.MouseLeftButtonUp += (s, e) => SelectTheme(theme.ThemeId);
            return card;
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = FilterBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(filter))
            {
                DisplayThemes(_themes);
            }
            else
            {
                var filtered = _themes.Where(t =>
                    t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    t.ThemeId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    t.Author.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                DisplayThemes(filtered);
            }
        }

        #endregion

        #region Theme Selection & Preview

        private void SelectTheme(string themeId)
        {
            _selectedThemeId = themeId;
            var theme = PMTheme.Instance.GetTheme(themeId);
            var themeInfo = _themes.FirstOrDefault(t => t.ThemeId == themeId);

            // Update header
            ThemeTitle.Text = theme?.Name ?? themeId;
            ThemeInfo.Text = $"v{theme?.Version ?? "1.0.0"} • {theme?.Author ?? "Unknown"}";

            // Refresh list to update selection style
            DisplayThemes(_themes);

            // Populate color editor
            PopulateColorEditor(theme);

            // Show preview
            if (theme != null)
            {
                ShowPreview(theme);
            }

            SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Selected, theme?.Name ?? themeId));
        }

        private void PopulateColorEditor(IPhobosTheme? theme)
        {
            ColorEditorPanel.Children.Clear();
            _editingConfig = null;
            _editingConfigChanged = false;

            if (theme is PCConfigBasedTheme configTheme)
            {
                // Deep copy the config for editing
                var cfg = configTheme.GetConfig();
                var json = JsonConvert.SerializeObject(cfg);
                _editingConfig = JsonConvert.DeserializeObject<PCThemeConfig>(json);

                if (_editingConfig != null)
                {
                    // Primary colors section
                    AddColorSection(ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Primary));
                    AddColorPicker("Primary", _editingConfig.Colors.Primary);
                    AddColorPicker("PrimaryHover", _editingConfig.Colors.PrimaryHover);
                    AddColorPicker("PrimaryPressed", _editingConfig.Colors.PrimaryPressed);

                    // Background colors
                    AddColorSection(ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Background));
                    AddColorPicker("Background1", _editingConfig.Colors.Background1);
                    AddColorPicker("Background2", _editingConfig.Colors.Background2);
                    AddColorPicker("Background3", _editingConfig.Colors.Background3);

                    // Foreground colors
                    AddColorSection(ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Foreground));
                    AddColorPicker("Foreground1", _editingConfig.Colors.Foreground1);
                    AddColorPicker("Foreground2", _editingConfig.Colors.Foreground2);
                    AddColorPicker("Foreground3", _editingConfig.Colors.Foreground3);

                    // Status colors
                    AddColorSection(ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Status));
                    AddColorPicker("Success", _editingConfig.Colors.Success);
                    AddColorPicker("Warning", _editingConfig.Colors.Warning);
                    AddColorPicker("Danger", _editingConfig.Colors.Danger);
                    AddColorPicker("Info", _editingConfig.Colors.Info);

                    // Border colors
                    AddColorSection(ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Border));
                    AddColorPicker("Border", _editingConfig.Colors.Border);
                    AddColorPicker("BorderFocus", _editingConfig.Colors.BorderFocus);
                }
            }
            else
            {
                // Code-defined theme - show read-only info
                var infoText = new TextBlock
                {
                    Text = ThemeManagerLocalization.Get(ThemeManagerLocalization.Editor_Readonly),
                    Foreground = (Brush)FindResource("Foreground3Brush"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                ColorEditorPanel.Children.Add(infoText);
            }
        }

        private void AddColorSection(string title)
        {
            var section = new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Foreground2Brush"),
                Margin = new Thickness(0, 12, 0, 8)
            };
            ColorEditorPanel.Children.Add(section);
        }

        private void AddColorPicker(string label, string colorHex)
        {
            var picker = new PCOColorPicker
            {
                LabelText = label,
                ColorHex = PCThemeLoader.ResolveVariable(colorHex, _editingConfig!),
                Margin = new Thickness(0, 0, 0, 4)
            };
            picker.ColorChanged += (s, hex) => OnColorChanged(label, hex);
            ColorEditorPanel.Children.Add(picker);
        }

        private void OnColorChanged(string label, string hex)
        {
            if (_editingConfig == null) return;

            // Map label to property
            var propertyName = label;
            var prop = typeof(ThemeColors).GetProperties()
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (prop != null)
            {
                prop.SetValue(_editingConfig.Colors, hex);
                _editingConfigChanged = true;
                RefreshPreviewFromEditingConfig();
            }
        }

        private void RefreshPreviewFromEditingConfig()
        {
            if (_editingConfig == null) return;
            var tempTheme = new PCConfigBasedTheme(_editingConfig);
            ShowPreview(tempTheme);
        }

        private IPhobosTheme? GetPreferredFallbackTheme(IPhobosTheme? exclude = null)
        {
            var list = PMTheme.Instance.AvailableThemes;
            var fileBased = list.FirstOrDefault(t => t is PCConfigBasedTheme && (exclude == null || t.ThemeId != exclude.ThemeId));
            if (fileBased != null) return fileBased;
            return PMTheme.Instance.GetTheme("light");
        }

        private void ShowPreview(IPhobosTheme theme)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowPreview(theme));
                return;
            }

            // Remove existing preview dictionary
            if (_previewDictionary != null)
            {
                PreviewArea.Resources.MergedDictionaries.Remove(_previewDictionary);
                _previewDictionary = null;
            }

            try
            {
                // Get resources on UI thread
                var resources = theme.GetGlobalStyles();
                _previewDictionary = new ResourceDictionary();
                foreach (var key in resources.Keys)
                {
                    _previewDictionary[key] = resources[key];
                }

                // Add base fallback theme
                var baseTheme = GetPreferredFallbackTheme(theme);
                if (baseTheme != null)
                {
                    var baseStyles = baseTheme.GetGlobalStyles();
                    if (!PreviewArea.Resources.MergedDictionaries.Contains(baseStyles))
                    {
                        PreviewArea.Resources.MergedDictionaries.Add(baseStyles);
                    }
                }

                PreviewArea.Resources.MergedDictionaries.Add(_previewDictionary);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("ThemeManager.Preview", $"Failed to apply preview: {ex.Message}");

                // Cleanup on failure
                try
                {
                    if (_previewDictionary != null && PreviewArea.Resources.MergedDictionaries.Contains(_previewDictionary))
                        PreviewArea.Resources.MergedDictionaries.Remove(_previewDictionary);
                }
                catch { }
            }
        }

        #endregion

        #region Actions

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedThemeId))
            {
                await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_SelectTheme), TitleText.Text);
                return;
            }

            SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ApplyRestart));
            var success = await PMTheme.Instance.LoadThemeAndSaveAsync(_selectedThemeId);

            if (success)
            {
                SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Applied, _selectedThemeId));
                await RefreshThemesAsync();
                SelectTheme(_selectedThemeId);
            }
            else
            {
                await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ApplyFailed), TitleText.Text);
                SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ApplyFailed));
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedThemeId))
            {
                _ = PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_SelectTheme), TitleText.Text);
                return;
            }

            var theme = PMTheme.Instance.GetTheme(_selectedThemeId);
            if (theme != null)
            {
                ShowPreview(theme);
                SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Selected, theme.Name));
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedThemeId))
            {
                await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_SelectTheme), TitleText.Text);
                return;
            }

            var theme = PMTheme.Instance.GetTheme(_selectedThemeId) as PCConfigBasedTheme;
            if (theme == null)
            {
                await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_ExportOnlyFile), TitleText.Text);
                return;
            }

            var save = new SaveFileDialog
            {
                Filter = "Phobos Theme (*.phobos-theme.json)|*.phobos-theme.json",
                FileName = theme.ThemeId
            };

            if (save.ShowDialog() == true)
            {
                var config = _editingConfigChanged && _editingConfig != null ? _editingConfig : theme.GetConfig();
                if (PCThemeLoader.SaveToFile(config, save.FileName))
                {
                    SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Exported, save.FileName));
                }
                else
                {
                    await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_ExportFailed), TitleText.Text);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingConfig == null)
            {
                await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_NoEditable), TitleText.Text);
                return;
            }

            if (!_editingConfigChanged)
            {
                SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_NoChanges));
                return;
            }

            var save = new SaveFileDialog
            {
                Filter = "Phobos Theme (*.phobos-theme.json)|*.phobos-theme.json",
                FileName = _editingConfig.Metadata.Id
            };

            if (save.ShowDialog() == true)
            {
                if (File.Exists(save.FileName))
                {
                    var overwrite = await PCDialogPlugin.ConfirmDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_FileExists), TitleText.Text);
                    if (!overwrite) return;
                }

                if (PCThemeLoader.SaveToFile(_editingConfig, save.FileName))
                {
                    // Install the theme
                    var theme = await PMTheme.Instance.InstallThemeAsync(save.FileName, copyToThemesFolder: false);
                    if (theme != null)
                    {
                        await RefreshThemesAsync();
                        SelectTheme(theme.ThemeId);
                        _editingConfigChanged = false;
                        SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Saved, theme.Name));
                    }
                }
                else
                {
                    await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_SaveFailed), TitleText.Text);
                }
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Phobos Theme (*.phobos-theme.json)|*.phobos-theme.json|All files (*.*)|*.*",
                Title = ThemeManagerLocalization.Get(ThemeManagerLocalization.Button_Import)
            };

            if (dialog.ShowDialog() == true)
            {
                SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_Importing));

                // Validate theme file
                var cfg = PCThemeLoader.LoadFromFile(dialog.FileName);
                if (cfg == null)
                {
                    await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_LoadFailed), TitleText.Text);
                    SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ImportFailed));
                    return;
                }

                // Check for duplicate
                var existing = PMTheme.Instance.GetTheme(cfg.Metadata.Id);
                if (existing != null)
                {
                    var overwrite = await PCDialogPlugin.ConfirmDialogAsync(
                        ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Dialog_Duplicate, cfg.Metadata.Id), TitleText.Text);
                    if (!overwrite)
                    {
                        SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ImportCancelled));
                        return;
                    }
                }

                // Install
                var theme = await PMTheme.Instance.InstallThemeAsync(dialog.FileName);
                if (theme != null)
                {
                    await RefreshThemesAsync();
                    SelectTheme(theme.ThemeId);
                    SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Imported, theme.Name));
                }
                else
                {
                    await PCDialogPlugin.ErrorDialogAsync(ThemeManagerLocalization.Get(ThemeManagerLocalization.Dialog_ImportFailed), TitleText.Text);
                    SetStatus(ThemeManagerLocalization.Get(ThemeManagerLocalization.Status_ImportFailed));
                }
            }
        }

        private async void NewButton_Click(object sender, RoutedEventArgs e)
        {
            // Create new theme based on current selection or default
            var newId = "com.phobos.theme.custom-" + Guid.NewGuid().ToString("N")[..8];
            var newConfig = new PCThemeConfig();
            newConfig.Metadata.Id = newId;
            newConfig.Metadata.Name = "Custom Theme";
            newConfig.Metadata.Author = "User";
            newConfig.Metadata.Version = "1.0.0";

            // Copy colors from selected theme if available
            if (_selectedThemeId != null && PMTheme.Instance.GetTheme(_selectedThemeId) is PCConfigBasedTheme selected)
            {
                var src = selected.GetConfig();
                newConfig.Colors = JsonConvert.DeserializeObject<ThemeColors>(JsonConvert.SerializeObject(src.Colors))!;
            }

            var save = new SaveFileDialog
            {
                Filter = "Phobos Theme (*.phobos-theme.json)|*.phobos-theme.json",
                FileName = newId
            };

            if (save.ShowDialog() == true)
            {
                if (PCThemeLoader.SaveToFile(newConfig, save.FileName))
                {
                    var theme = await PMTheme.Instance.InstallThemeAsync(save.FileName, copyToThemesFolder: false);
                    if (theme != null)
                    {
                        await RefreshThemesAsync();
                        SelectTheme(theme.ThemeId);
                        SetStatus(ThemeManagerLocalization.GetFormat(ThemeManagerLocalization.Status_Created, theme.Name));
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private void SetStatus(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => StatusText.Text = message);
            }
            else
            {
                StatusText.Text = message;
            }
        }

        #endregion
    }
}
