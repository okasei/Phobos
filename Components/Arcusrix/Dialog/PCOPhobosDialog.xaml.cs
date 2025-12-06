using Phobos.Shared.Class;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Dialog
{
    /// <summary>
    /// PCOPhobosDialog - 完全异步的对话框系统
    /// 支持同时显示多个对话框，带动画效果
    /// </summary>
    public partial class PCOPhobosDialog : Window
    {
        private DialogConfig _config;
        private DialogCallbackResult _result;
        private readonly List<Button> _actionButtons = new();
        private string _currentLanguage = System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag;
        private TaskCompletionSource<DialogCallbackResult>? _tcs;
        private bool _isClosing = false;

        /// <summary>
        /// 对话框结果
        /// </summary>
        public DialogCallbackResult DialogCallbackResult => _result;

        /// <summary>
        /// 创建对话框
        /// </summary>
        public PCOPhobosDialog()
        {
            InitializeComponent();
            _config = new DialogConfig();
            _result = new DialogCallbackResult();

            try
            {
                _currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            }
            catch
            {
                _currentLanguage = "en-US";
            }
        }

        /// <summary>
        /// 使用配置创建对话框
        /// </summary>
        public PCOPhobosDialog(DialogConfig config) : this()
        {
            Configure(config);
        }

        /// <summary>
        /// 配置对话框
        /// </summary>
        public void Configure(DialogConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Width = config.Width;
            MinHeight = config.MinHeight;
            MaxHeight = config.MaxHeight;

            SetupTitle();
            SetupContent();
            SetupButtons();
            SetupPosition();

            if (config.ShowCloseButton)
            {
                CloseButton.Visibility = Visibility.Visible;
            }
        }

        private void SetupTitle()
        {
            var title = _config.GetLocalizedTitle(_currentLanguage);

            if (string.IsNullOrEmpty(title) && _config.CallerIcon == null && string.IsNullOrEmpty(_config.CallerIconPath))
            {
                TitleBar.Visibility = Visibility.Collapsed;
                return;
            }

            TitleBar.Visibility = Visibility.Visible;
            TitleText.Text = title ?? string.Empty;

            if (_config.CallerIcon != null)
            {
                CallerIconImage.Source = _config.CallerIcon;
                CallerIconImage.Visibility = Visibility.Visible;
            }
            else if (!string.IsNullOrEmpty(_config.CallerIconPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_config.CallerIconPath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    CallerIconImage.Source = bitmap;
                    CallerIconImage.Visibility = Visibility.Visible;
                }
                catch
                {
                    CallerIconImage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetupContent()
        {
            ImageTextContent.Visibility = Visibility.Collapsed;
            CenteredTextContent.Visibility = Visibility.Collapsed;
            LeftAlignedTextContent.Visibility = Visibility.Collapsed;
            ImageCaptionContent.Visibility = Visibility.Collapsed;
            CustomContentPresenter.Visibility = Visibility.Collapsed;

            var contentText = _config.GetLocalizedContentText(_currentLanguage);

            switch (_config.ContentMode)
            {
                case DialogContentMode.ImageWithText:
                    SetupImageWithText(contentText);
                    break;

                case DialogContentMode.CenteredText:
                    CenteredTextContent.Text = contentText ?? string.Empty;
                    CenteredTextContent.Visibility = Visibility.Visible;
                    break;

                case DialogContentMode.LeftAlignedText:
                    LeftAlignedTextContent.Text = contentText ?? string.Empty;
                    LeftAlignedTextContent.Visibility = Visibility.Visible;
                    break;

                case DialogContentMode.ImageWithCaption:
                    SetupImageWithCaption(contentText);
                    break;

                case DialogContentMode.Custom:
                    if (_config.CustomContent != null)
                    {
                        CustomContentPresenter.Content = _config.CustomContent;
                        CustomContentPresenter.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

        private void SetupImageWithText(string? text)
        {
            ImageTextContent.Visibility = Visibility.Visible;
            ImageTextBlock.Text = text ?? string.Empty;

            ContentImage.MaxWidth = _config.ContentImageMaxWidth;
            ContentImage.MaxHeight = _config.ContentImageMaxHeight;

            if (_config.ContentImage != null)
            {
                ContentImage.Source = _config.ContentImage;
            }
            else if (!string.IsNullOrEmpty(_config.ContentImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_config.ContentImagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ContentImage.Source = bitmap;
                }
                catch
                {
                    ContentImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ContentImage.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupImageWithCaption(string? text)
        {
            ImageCaptionContent.Visibility = Visibility.Visible;
            ImageCaptionTextBlock.Text = text ?? string.Empty;

            var caption = _config.GetLocalizedContentImageCaption(_currentLanguage);
            ImageCaptionBlock.Text = caption ?? string.Empty;
            ImageCaptionBlock.Visibility = string.IsNullOrEmpty(caption) ? Visibility.Collapsed : Visibility.Visible;

            CaptionContentImage.MaxWidth = _config.ContentImageMaxWidth;
            CaptionContentImage.MaxHeight = _config.ContentImageMaxHeight;

            if (_config.ContentImage != null)
            {
                CaptionContentImage.Source = _config.ContentImage;
            }
            else if (!string.IsNullOrEmpty(_config.ContentImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_config.ContentImagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    CaptionContentImage.Source = bitmap;
                }
                catch
                {
                    CaptionContentImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                CaptionContentImage.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupButtons()
        {
            RightButtonPanel.Children.Clear();
            _actionButtons.Clear();

            if (_config.CustomButtonArea != null)
            {
                CustomButtonPresenter.Content = _config.CustomButtonArea;
                CustomButtonPresenter.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
                RightButtonPanel.Visibility = Visibility.Collapsed;
                return;
            }

            CustomButtonPresenter.Visibility = Visibility.Collapsed;
            RightButtonPanel.Visibility = Visibility.Visible;

            if (_config.ShowCancelButton)
            {
                CancelButton.Content = _config.GetLocalizedCancelButtonText(_currentLanguage);
                CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }

            // Support up to 5 buttons
            var visibleCount = Math.Min(_config.VisibleButtonCount, _config.Buttons.Count);
            visibleCount = Math.Max(1, Math.Min(5, visibleCount));

            for (int i = 0; i < visibleCount && i < _config.Buttons.Count; i++)
            {
                var buttonConfig = _config.Buttons[i];
                var button = CreateButton(buttonConfig, i);
                RightButtonPanel.Children.Insert(0, button);
                _actionButtons.Insert(0, button);
            }
        }

        private Button CreateButton(DialogButton config, int index)
        {
            var button = new Button
            {
                Content = config.GetLocalizedText(_currentLanguage),
                Tag = config,
                IsEnabled = config.IsEnabled
            };

            var styleName = config.ButtonType switch
            {
                DialogButtonType.Primary => "PrimaryButtonStyle",
                DialogButtonType.Cancel => "CancelButtonStyle",
                _ => "SecondaryButtonStyle"
            };

            if (Resources.Contains(styleName))
            {
                button.Style = (Style)Resources[styleName];
            }

            button.Click += (s, e) => OnButtonClick(config, index);

            return button;
        }

        private void OnButtonClick(DialogButton buttonConfig, int index)
        {
            _result.ClickedButton = buttonConfig;
            _result.ButtonTag = buttonConfig.Tag;
            _result.Result = (Shared.Class.DialogResult)(index + 2);

            buttonConfig.OnClick?.Invoke(buttonConfig);

            if (buttonConfig.CloseOnClick)
            {
                CloseDialogWithAnimation(true);
            }
        }

        private void SetupPosition()
        {
            if (_config.PositionMode == DialogPositionMode.Custom)
            {
                if (_config.CustomPosition.HasValue)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = _config.CustomPosition.Value.X;
                    Top = _config.CustomPosition.Value.Y;
                }
            }
            else if (_config.PositionMode == DialogPositionMode.CenterOwner && _config.OwnerWindow != null)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
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

            // Fade in
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

            // Scale animation
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

            // Y axis slide
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

            // Fade out
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

            // Scale animation
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

            // Y axis slide
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

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_config.IsDraggable && e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _config.CloseOnEscape)
            {
                _result.Result = Shared.Class.DialogResult.Cancel;
                CloseDialogWithAnimation(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (_config.Buttons.Count > 0)
                {
                    OnButtonClick(_config.Buttons[0], 0);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result.Result = Shared.Class.DialogResult.Cancel;
            CloseDialogWithAnimation(false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _result.Result = Shared.Class.DialogResult.Cancel;
            CloseDialogWithAnimation(false);
        }

        private void CloseDialogWithAnimation(bool result)
        {
            if (_isClosing) return;
            _isClosing = true;

            PlayCloseAnimation(() =>
            {
                _tcs?.TrySetResult(_result);
                Close();
            });
        }

        #region Static async methods

        /// <summary>
        /// 异步显示对话框（完全异步，非阻塞）
        /// 这是推荐的使用方式，支持同时显示多个对话框
        /// </summary>
        public static Task<DialogCallbackResult> ShowAsync(DialogConfig config)
        {
            var tcs = new TaskCompletionSource<DialogCallbackResult>();

            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    ShowAsyncInternal(config, tcs);
                });
            }
            else
            {
                ShowAsyncInternal(config, tcs);
            }

            return tcs.Task;
        }

        private static void ShowAsyncInternal(DialogConfig config, TaskCompletionSource<DialogCallbackResult> tcs)
        {
            try
            {
                var dialog = new PCOPhobosDialog(config);
                dialog._tcs = tcs;

                bool ownerIsValid = config.OwnerWindow != null &&
                                    config.OwnerWindow.IsLoaded &&
                                    config.OwnerWindow.IsVisible;

                if (config.PositionMode == DialogPositionMode.CenterOwner && ownerIsValid)
                {
                    dialog.Owner = config.OwnerWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowInTaskbar = true;
                }

                dialog.Closed += (s, e) =>
                {
                    tcs.TrySetResult(dialog.DialogCallbackResult);
                };

                dialog.Show();
                dialog.Activate();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new DialogCallbackResult { Result = Shared.Class.DialogResult.Cancel });
            }
        }

        /// <summary>
        /// 显示对话框（异步回调，非阻塞）
        /// </summary>
        public static void ShowWithCallback(DialogConfig config, Action<DialogCallbackResult>? callback = null)
        {
            Action showAction = () =>
            {
                var dialog = new PCOPhobosDialog(config);

                dialog.Closed += (s, e) =>
                {
                    callback?.Invoke(dialog.DialogCallbackResult);
                };

                bool ownerIsValid = config.OwnerWindow != null &&
                                    config.OwnerWindow.IsLoaded &&
                                    config.OwnerWindow.IsVisible;

                if (config.PositionMode == DialogPositionMode.CenterOwner && ownerIsValid)
                {
                    dialog.Owner = config.OwnerWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowInTaskbar = true;
                }

                dialog.Show();
                dialog.Activate();
            };

            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.BeginInvoke(showAction);
            }
            else
            {
                showAction();
            }
        }

        /// <summary>
        /// 显示多个对话框（同时显示，带偏移）
        /// </summary>
        public static List<PCOPhobosDialog> ShowMultiple(params DialogConfig[] configs)
        {
            var dialogs = new List<PCOPhobosDialog>();

            Action showAction = () =>
            {
                int offsetX = 0;
                int offsetY = 0;

                foreach (var config in configs)
                {
                    if (config.PositionMode != DialogPositionMode.Custom)
                    {
                        config.CustomOffset = new Point(
                            config.CustomOffset.X + offsetX,
                            config.CustomOffset.Y + offsetY
                        );
                    }

                    var dialog = new PCOPhobosDialog(config);
                    dialog.Show();
                    dialog.Activate();
                    dialogs.Add(dialog);

                    offsetX += 30;
                    offsetY += 30;
                }
            };

            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(showAction);
            }
            else
            {
                showAction();
            }

            return dialogs;
        }

        /// <summary>
        /// 异步确认对话框
        /// </summary>
        public static async Task<bool> ConfirmAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.Confirm(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = await ShowAsync(config);
            return result.ButtonTag == "ok";
        }

        /// <summary>
        /// 异步信息对话框
        /// </summary>
        public static Task InfoAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.Info(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return ShowAsync(config);
        }

        /// <summary>
        /// 异步警告对话框
        /// </summary>
        public static Task WarningAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.Warning(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return ShowAsync(config);
        }

        /// <summary>
        /// 异步错误对话框
        /// </summary>
        public static Task ErrorAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.Error(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            return ShowAsync(config);
        }

        /// <summary>
        /// 异步是/否对话框
        /// </summary>
        public static async Task<bool?> YesNoAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.YesNo(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = await ShowAsync(config);
            return result.ButtonTag switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
        }

        /// <summary>
        /// 异步是/否/取消对话框
        /// </summary>
        public static async Task<bool?> YesNoCancelAsync(string message, string? title = null, Window? owner = null)
        {
            var config = DialogPresets.YesNoCancel(message, title);
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = await ShowAsync(config);
            return result.ButtonTag switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
        }

        #region Legacy synchronous methods (kept for backward compatibility)

        /// <summary>
        /// [已弃用] 同步显示对话框，建议使用 ShowAsync
        /// </summary>
        [Obsolete("Use ShowAsync for better multi-dialog support")]
        public static DialogCallbackResult Show(DialogConfig config)
        {
            DialogCallbackResult? result = null;

            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = ShowInternal(config);
                });
            }
            else
            {
                result = ShowInternal(config);
            }

            return result ?? new DialogCallbackResult();
        }

        private static DialogCallbackResult ShowInternal(DialogConfig config)
        {
            PCOPhobosDialog? dialog = null;

            try
            {
                dialog = new PCOPhobosDialog(config);

                bool ownerIsValid = config.OwnerWindow != null &&
                                    config.OwnerWindow.IsLoaded &&
                                    config.OwnerWindow.IsVisible;

                if (config.PositionMode == DialogPositionMode.CenterOwner && ownerIsValid)
                {
                    dialog.Owner = config.OwnerWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowInTaskbar = true;
                }

                if (config.IsModal)
                {
                    dialog.ShowDialog();
                }
                else
                {
                    dialog.Show();
                    dialog.Activate();
                }
            }
            catch
            {
                return new DialogCallbackResult { Result = Shared.Class.DialogResult.Cancel };
            }

            return dialog?.DialogCallbackResult ?? new DialogCallbackResult();
        }

        /// <summary>
        /// [已弃用] 同步确认对话框，建议使用 ConfirmAsync
        /// </summary>
        [Obsolete("Use ConfirmAsync for better multi-dialog support")]
        public static bool Confirm(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Confirm(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = Show(config);
            return result.ButtonTag == "ok";
        }

        /// <summary>
        /// [已弃用] 同步信息对话框，建议使用 InfoAsync
        /// </summary>
        [Obsolete("Use InfoAsync for better multi-dialog support")]
        public static void Info(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Info(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            Show(config);
        }

        /// <summary>
        /// [已弃用] 同步警告对话框，建议使用 WarningAsync
        /// </summary>
        [Obsolete("Use WarningAsync for better multi-dialog support")]
        public static void Warning(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Warning(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            Show(config);
        }

        /// <summary>
        /// [已弃用] 同步错误对话框，建议使用 ErrorAsync
        /// </summary>
        [Obsolete("Use ErrorAsync for better multi-dialog support")]
        public static void Error(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.Error(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            Show(config);
        }

        /// <summary>
        /// [已弃用] 同步是/否对话框，建议使用 YesNoAsync
        /// </summary>
        [Obsolete("Use YesNoAsync for better multi-dialog support")]
        public static bool? YesNo(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.YesNo(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = Show(config);
            return result.ButtonTag switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
        }

        /// <summary>
        /// [已弃用] 同步是/否/取消对话框，建议使用 YesNoCancelAsync
        /// </summary>
        [Obsolete("Use YesNoCancelAsync for better multi-dialog support")]
        public static bool? YesNoCancel(string message, string? title = null, bool isModal = true, Window? owner = null)
        {
            var config = DialogPresets.YesNoCancel(message, title);
            config.IsModal = isModal;
            if (owner != null)
            {
                config.OwnerWindow = owner;
                config.PositionMode = DialogPositionMode.CenterOwner;
            }

            var result = Show(config);
            return result.ButtonTag switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
        }

        #endregion

        #endregion
    }
}
