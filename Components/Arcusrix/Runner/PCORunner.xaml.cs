using Phobos.Class.Plugin.BuiltIn;
using Phobos.Components.Arcusrix.Menu;
using Phobos.Manager.Arcusrix;
using Phobos.Shared.Class;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IWshRuntimeLibrary;

namespace Phobos.Components.Arcusrix.Runner
{
    /// <summary>
    /// Runner 本地化字符串
    /// </summary>
    public static class RunnerLocalization
    {
        public const string Placeholder = "placeholder";
        public const string Run = "run";
        public const string OpenWith = "open_with";
        public const string OpenLocation = "open_location";
        public const string CopyPath = "copy_path";
        public const string NoResults = "no_results";

        private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            [Placeholder] = new()
            {
                { "en-US", "Enter command, file, or program to run..." },
                { "zh-CN", "输入要运行的命令、文件或程序..." },
                { "zh-TW", "輸入要運行的命令、檔案或程式..." },
                { "ja-JP", "実行するコマンド、ファイル、プログラムを入力..." },
                { "ko-KR", "실행할 명령, 파일 또는 프로그램 입력..." }
            },
            [Run] = new()
            {
                { "en-US", "Run" },
                { "zh-CN", "运行" },
                { "zh-TW", "運行" },
                { "ja-JP", "実行" },
                { "ko-KR", "실행" }
            },
            [OpenWith] = new()
            {
                { "en-US", "Open with..." },
                { "zh-CN", "打开方式..." },
                { "zh-TW", "開啟方式..." },
                { "ja-JP", "プログラムから開く..." },
                { "ko-KR", "연결 프로그램..." }
            },
            [OpenLocation] = new()
            {
                { "en-US", "Open file location" },
                { "zh-CN", "打开文件位置" },
                { "zh-TW", "開啟檔案位置" },
                { "ja-JP", "ファイルの場所を開く" },
                { "ko-KR", "파일 위치 열기" }
            },
            [CopyPath] = new()
            {
                { "en-US", "Copy path" },
                { "zh-CN", "复制路径" },
                { "zh-TW", "複製路徑" },
                { "ja-JP", "パスをコピー" },
                { "ko-KR", "경로 복사" }
            },
            [NoResults] = new()
            {
                { "en-US", "No matching items found" },
                { "zh-CN", "未找到匹配项" },
                { "zh-TW", "未找到匹配項" },
                { "ja-JP", "一致する項目が見つかりません" },
                { "ko-KR", "일치하는 항목 없음" }
            }
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
    }

    /// <summary>
    /// Phobos Runner 窗口
    /// 提供居中的圆角矩形输入框用于快速启动
    /// </summary>
    public partial class PCORunner : Window
    {
        private readonly PCRunnerPlugin _runner;
        private CancellationTokenSource? _searchCts;
        private int _selectedSuggestionIndex = -1;
        private List<ShortcutInfo> _currentSuggestions = new();
        private bool _isClosing = false;

        public PCORunner(PCRunnerPlugin runner)
        {
            _runner = runner;
            InitializeComponent();
            ApplyTheme();
            UpdatePlaceholder();
            CalculateWindowSize();
        }

        /// <summary>
        /// 计算窗口大小（占屏幕60%宽度，限制最大1200）
        /// </summary>
        private void CalculateWindowSize()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 60% 屏幕宽度，最大1200，最小600
            var targetWidth = Math.Max(600, Math.Min(1200, screenWidth * 0.6));

            // 高度固定为160（包含边距）
            var targetHeight = 160;

            Width = targetWidth;
            Height = targetHeight;

            // 更新建议列表宽度 (与主输入框同宽，减去 RootGrid 的 Margin 60 和外层 Grid 的 Margin 80)
            SuggestionsBorder.Width = targetWidth * 0.78;
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme()
        {
            try
            {
                var themeDict = PMTheme.Instance.CurrentTheme?.GetGlobalStyles();
                if (themeDict != null)
                {
                    Resources.MergedDictionaries.Clear();
                    Resources.MergedDictionaries.Add(themeDict);
                }
            }
            catch
            {
                // 使用默认样式
            }
        }

        /// <summary>
        /// 更新占位符文本
        /// </summary>
        private void UpdatePlaceholder()
        {
            PlaceholderText.Text = RunnerLocalization.Get(RunnerLocalization.Placeholder);
        }

        /// <summary>
        /// 聚焦输入框
        /// </summary>
        public void FocusInput()
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        #region 窗口事件

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayOpenAnimation();
            FocusInput();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // 失焦时关闭窗口（但如果建议列表打开且鼠标在上面则不关闭）
            if (!_isClosing)
            {
                // 延迟检查，让 Popup 有时间获取焦点
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_isClosing && !IsActive && !SuggestionsPopup.IsMouseOver)
                    {
                        CloseWithAnimation();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (SuggestionsPopup.IsOpen)
                    {
                        SuggestionsPopup.IsOpen = false;
                        _selectedSuggestionIndex = -1;
                    }
                    else
                    {
                        CloseWithAnimation();
                    }
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (_currentSuggestions.Count > 0)
                    {
                        _selectedSuggestionIndex = Math.Max(0, _selectedSuggestionIndex - 1);
                        UpdateSuggestionSelection();
                        e.Handled = true;
                    }
                    break;

                case Key.Down:
                    if (_currentSuggestions.Count > 0)
                    {
                        _selectedSuggestionIndex = Math.Min(_currentSuggestions.Count - 1, _selectedSuggestionIndex + 1);
                        UpdateSuggestionSelection();
                        e.Handled = true;
                    }
                    break;
            }
        }

        #endregion

        #region 输入处理

        private async void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = InputTextBox.Text;

            // 更新占位符可见性
            PlaceholderText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;

            // 更新清空按钮可见性（带动画）
            AnimateClearButton(!string.IsNullOrEmpty(text));

            // 取消之前的搜索
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(text))
            {
                SuggestionsPopup.IsOpen = false;
                _currentSuggestions.Clear();
                return;
            }

            // 延迟搜索以避免频繁查询
            try
            {
                await Task.Delay(200, _searchCts.Token);
                await SearchAndShowSuggestions(text);
            }
            catch (OperationCanceledException)
            {
                // 搜索被取消
            }
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // 如果有选中的建议项，使用它
                if (_selectedSuggestionIndex >= 0 && _selectedSuggestionIndex < _currentSuggestions.Count)
                {
                    var shortcut = _currentSuggestions[_selectedSuggestionIndex];
                    await ExecuteShortcut(shortcut);
                }
                else
                {
                    // 否则直接执行输入内容
                    await ExecuteInput(InputTextBox.Text);
                }
            }
        }

        #endregion

        #region 搜索建议

        private async Task SearchAndShowSuggestions(string searchText)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Runner] Searching for: {searchText}");

                var suggestions = await _runner.FindAllMatchingShortcuts(searchText);
                _currentSuggestions = suggestions;
                _selectedSuggestionIndex = suggestions.Count > 0 ? 0 : -1;

                System.Diagnostics.Debug.WriteLine($"[Runner] Found {suggestions.Count} suggestions");

                await Dispatcher.InvokeAsync(() =>
                {
                    SuggestionsPanel.Children.Clear();

                    if (suggestions.Count == 0)
                    {
                        if (SuggestionsPopup.IsOpen)
                        {
                            PlaySuggestionsCloseAnimation();
                        }
                        return;
                    }

                    foreach (var suggestion in suggestions)
                    {
                        var item = CreateSuggestionItem(suggestion);
                        SuggestionsPanel.Children.Add(item);
                    }

                    UpdateSuggestionSelection();

                    if (!SuggestionsPopup.IsOpen)
                    {
                        SuggestionsPopup.IsOpen = true;
                        PlaySuggestionsOpenAnimation();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Runner] Search error: {ex.Message}");
            }
        }

        /// <summary>
        /// 建议列表打开动画
        /// </summary>
        private void PlaySuggestionsOpenAnimation()
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(350);
            var elasticEase = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 1,
                Springiness = 6
            };
            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 初始状态
            SuggestionsBorder.Opacity = 0;
            SuggestionsScale.ScaleX = 0.95;
            SuggestionsScale.ScaleY = 0.9;
            SuggestionsTranslate.Y = -15;

            // 淡入
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = cubicEase };
            Storyboard.SetTarget(fadeIn, SuggestionsBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 缩放
            var scaleX = new DoubleAnimation(0.95, 1, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(scaleX, SuggestionsScale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation(0.9, 1, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(scaleY, SuggestionsScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            // 位移
            var translateY = new DoubleAnimation(-15, 0, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(translateY, SuggestionsTranslate);
            Storyboard.SetTargetProperty(translateY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(translateY);

            storyboard.Begin();
        }

        /// <summary>
        /// 建议列表关闭动画
        /// </summary>
        private void PlaySuggestionsCloseAnimation()
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(150);
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };

            var fadeOut = new DoubleAnimation(1, 0, duration) { EasingFunction = easing };
            Storyboard.SetTarget(fadeOut, SuggestionsBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            var scaleY = new DoubleAnimation(1, 0.95, duration) { EasingFunction = easing };
            Storyboard.SetTarget(scaleY, SuggestionsScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            var translateY = new DoubleAnimation(0, -10, duration) { EasingFunction = easing };
            Storyboard.SetTarget(translateY, SuggestionsTranslate);
            Storyboard.SetTargetProperty(translateY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(translateY);

            storyboard.Completed += (s, e) => SuggestionsPopup.IsOpen = false;
            storyboard.Begin();
        }

        private Border CreateSuggestionItem(ShortcutInfo shortcut)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                Tag = shortcut
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 图标
            var iconBorder = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(10),
                Background = (Brush)FindResource("Background3Brush"),
                Margin = new Thickness(0, 0, 16, 0)
            };

            // 尝试加载缓存的图标
            UIElement iconContent;
            if (!string.IsNullOrEmpty(shortcut.CachedIconPath) && System.IO.File.Exists(shortcut.CachedIconPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(shortcut.CachedIconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 48;
                    bitmap.DecodePixelHeight = 48;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    iconContent = new Image
                    {
                        Source = bitmap,
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                catch
                {
                    // 加载失败，使用默认图标
                    iconContent = new TextBlock
                    {
                        Text = shortcut.ShortcutType == ShortcutType.Url ? "🌐" : "📄",
                        FontSize = 22,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
            else
            {
                // 没有缓存图标，使用默认图标
                iconContent = new TextBlock
                {
                    Text = shortcut.ShortcutType == ShortcutType.Url ? "🌐" : "📄",
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            iconBorder.Child = iconContent;
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // 文本信息
            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = shortcut.Name,
                FontSize = 16,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("Foreground1Brush")
            };
            textStack.Children.Add(nameText);

            var pathText = new TextBlock
            {
                Text = shortcut.TargetPath,
                FontSize = 13,
                Foreground = (Brush)FindResource("Foreground4Brush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 4, 0, 0)
            };
            textStack.Children.Add(pathText);

            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            border.Child = grid;

            // 事件
            border.MouseEnter += (s, e) =>
            {
                var idx = SuggestionsPanel.Children.IndexOf(border);
                _selectedSuggestionIndex = idx;
                UpdateSuggestionSelection();
            };

            border.MouseLeftButtonUp += async (s, e) =>
            {
                await ExecuteShortcut(shortcut);
            };

            border.MouseRightButtonUp += (s, e) =>
            {
                ShowShortcutContextMenu(shortcut, border);
                e.Handled = true;
            };

            return border;
        }

        private void UpdateSuggestionSelection()
        {
            for (int i = 0; i < SuggestionsPanel.Children.Count; i++)
            {
                if (SuggestionsPanel.Children[i] is Border border)
                {
                    var isSelected = i == _selectedSuggestionIndex;
                    AnimateSuggestionHover(border, isSelected);
                }
            }
        }

        #endregion

        #region 右键菜单

        private void ShowShortcutContextMenu(ShortcutInfo shortcut, UIElement target)
        {
            var items = new List<PhobosMenuItem>
            {
                PMMenu.Instance.CreateItem("run", RunnerLocalization.Get(RunnerLocalization.Run), "▶", async () =>
                {
                    await ExecuteShortcut(shortcut);
                }),
                PMMenu.Instance.CreateSeparator(),
                PMMenu.Instance.CreateItem("open_location", RunnerLocalization.Get(RunnerLocalization.OpenLocation), "📁", () =>
                {
                    OpenFileLocation(shortcut.FullPath);
                }),
                PMMenu.Instance.CreateItem("copy_path", RunnerLocalization.Get(RunnerLocalization.CopyPath), "📋", () =>
                {
                    Clipboard.SetText(shortcut.TargetPath);
                })
            };

            var position = target.TranslatePoint(new Point(0, ((FrameworkElement)target).ActualHeight), RootGrid);
            PMMenu.Instance.ShowAt(RootGrid, items, position, null);
        }

        private void OpenFileLocation(string path)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to open location: {ex.Message}");
            }
        }

        #endregion

        #region 执行

        private async Task ExecuteInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            CloseWithAnimation();

            try
            {
                await _runner.OnLaunch(input);
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to execute: {ex.Message}");
            }
        }

        private async Task ExecuteShortcut(ShortcutInfo shortcut)
        {
            CloseWithAnimation();

            try
            {
                // 直接用 explorer 启动快捷方式文件本身
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{shortcut.FullPath}\"",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                PCLoggerPlugin.Info("Runner", $"Launched shortcut: {shortcut.Name}");
            }
            catch (Exception ex)
            {
                PCLoggerPlugin.Error("Runner", $"Failed to execute shortcut: {ex.Message}");
            }
        }

        #endregion

        #region 清空按钮

        private void ClearButton_Click(object sender, MouseButtonEventArgs e)
        {
            InputTextBox.Clear();
            FocusInput();
            AnimateClearButtonClick();
        }

        private void ClearButton_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimateClearButtonHover(true);
        }

        private void ClearButton_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateClearButtonHover(false);
        }

        #endregion

        #region 动画

        private void PlayOpenAnimation()
        {
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(500);
            var shortDuration = TimeSpan.FromMilliseconds(350);

            // 弹性缓动
            var elasticEase = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 1,
                Springiness = 5
            };

            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 初始状态
            MainBorder.Opacity = 0;
            BorderScale.ScaleX = 0.8;
            BorderScale.ScaleY = 0.8;
            BorderTranslate.Y = -30;

            // 淡入
            var fadeIn = new DoubleAnimation(0, 1, shortDuration) { EasingFunction = cubicEase };
            Storyboard.SetTarget(fadeIn, MainBorder);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 缩放 X - 弹性
            var scaleX = new DoubleAnimation(0.8, 1, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(scaleX, BorderScale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            // 缩放 Y - 弹性
            var scaleY = new DoubleAnimation(0.8, 1, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(scaleY, BorderScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            // Y 位移 - 弹性
            var translateY = new DoubleAnimation(-30, 0, duration) { EasingFunction = elasticEase };
            Storyboard.SetTarget(translateY, BorderTranslate);
            Storyboard.SetTargetProperty(translateY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(translateY);

            storyboard.Begin();
        }

        private void CloseWithAnimation()
        {
            if (_isClosing) return;
            _isClosing = true;

            // 关闭建议列表
            if (SuggestionsPopup.IsOpen)
            {
                SuggestionsPopup.IsOpen = false;
            }

            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(250);

            // 使用 Back 缓动产生略微收缩的感觉
            var backEase = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.3 };
            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseIn };

            // 淡出
            var fadeOut = new DoubleAnimation(1, 0, duration) { EasingFunction = cubicEase };
            Storyboard.SetTarget(fadeOut, MainBorder);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // 缩放 - 略微收缩
            var scaleX = new DoubleAnimation(1, 0.9, duration) { EasingFunction = backEase };
            Storyboard.SetTarget(scaleX, BorderScale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation(1, 0.9, duration) { EasingFunction = backEase };
            Storyboard.SetTarget(scaleY, BorderScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            // Y 位移 - 向上飘
            var translateY = new DoubleAnimation(0, -15, duration) { EasingFunction = cubicEase };
            Storyboard.SetTarget(translateY, BorderTranslate);
            Storyboard.SetTargetProperty(translateY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(translateY);

            storyboard.Completed += (s, e) => Close();
            storyboard.Begin();
        }

        private void AnimateClearButton(bool show)
        {
            var targetVisibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (ClearButton.Visibility == targetVisibility && show) return;

            if (show)
            {
                ClearButton.Visibility = Visibility.Visible;
                ClearButton.Opacity = 0;
                ClearButtonScale.ScaleX = 0.5;
                ClearButtonScale.ScaleY = 0.5;

                var storyboard = new Storyboard();
                var duration = TimeSpan.FromMilliseconds(350);
                var elasticEase = new ElasticEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Oscillations = 1,
                    Springiness = 6
                };

                // 淡入
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeIn, ClearButton);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
                storyboard.Children.Add(fadeIn);

                // 弹性缩放
                var scaleX = new DoubleAnimation(0.5, 1, duration) { EasingFunction = elasticEase };
                Storyboard.SetTarget(scaleX, ClearButtonScale);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation(0.5, 1, duration) { EasingFunction = elasticEase };
                Storyboard.SetTarget(scaleY, ClearButtonScale);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
                storyboard.Children.Add(scaleY);

                storyboard.Begin();
            }
            else
            {
                var storyboard = new Storyboard();
                var duration = TimeSpan.FromMilliseconds(150);
                var easing = new CubicEase { EasingMode = EasingMode.EaseIn };

                var fadeOut = new DoubleAnimation(1, 0, duration) { EasingFunction = easing };
                Storyboard.SetTarget(fadeOut, ClearButton);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
                storyboard.Children.Add(fadeOut);

                var scaleX = new DoubleAnimation(1, 0.5, duration) { EasingFunction = easing };
                Storyboard.SetTarget(scaleX, ClearButtonScale);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation(1, 0.5, duration) { EasingFunction = easing };
                Storyboard.SetTarget(scaleY, ClearButtonScale);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
                storyboard.Children.Add(scaleY);

                storyboard.Completed += (s, e) => ClearButton.Visibility = Visibility.Collapsed;
                storyboard.Begin();
            }
        }

        private void AnimateClearButtonHover(bool isHover)
        {
            var targetBrush = isHover
                ? (Brush)FindResource("Background3Brush")
                : Brushes.Transparent;

            ClearButton.Background = targetBrush;
        }

        private void AnimateClearButtonClick()
        {
            // 弹性点击反馈动画
            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(400);

            var elasticEase = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 1,
                Springiness = 4
            };

            // 先缩小
            var scaleDownX = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(80))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(scaleDownX, ClearButtonScale);
            Storyboard.SetTargetProperty(scaleDownX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleDownX);

            var scaleDownY = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(80))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(scaleDownY, ClearButtonScale);
            Storyboard.SetTargetProperty(scaleDownY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleDownY);

            // 再弹性恢复
            var scaleUpX = new DoubleAnimation(0.7, 1, duration)
            {
                BeginTime = TimeSpan.FromMilliseconds(80),
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleUpX, ClearButtonScale);
            Storyboard.SetTargetProperty(scaleUpX, new PropertyPath(ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleUpX);

            var scaleUpY = new DoubleAnimation(0.7, 1, duration)
            {
                BeginTime = TimeSpan.FromMilliseconds(80),
                EasingFunction = elasticEase
            };
            Storyboard.SetTarget(scaleUpY, ClearButtonScale);
            Storyboard.SetTargetProperty(scaleUpY, new PropertyPath(ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleUpY);

            storyboard.Begin();
        }

        private void AnimateSuggestionHover(Border item, bool isSelected)
        {
            var targetBrush = isSelected
                ? (SolidColorBrush)FindResource("Background3Brush")
                : new SolidColorBrush(Colors.Transparent);

            // 背景颜色动画 - 使用新的 SolidColorBrush 确保圆角正常
            var currentColor = item.Background is SolidColorBrush currentBrush
                ? currentBrush.Color
                : Colors.Transparent;

            var newBrush = new SolidColorBrush(currentColor);
            item.Background = newBrush;

            var animation = new ColorAnimation
            {
                To = targetBrush.Color,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            newBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        #endregion
    }
}