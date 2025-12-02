using Phobos.Shared.Class;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// PhobosDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PCOPhobosDialog : Window
    {
        private DialogConfig _config;
        private DialogCallbackResult _result;
        private readonly List<Button> _actionButtons = new();
        private string _currentLanguage = "en-US";

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

            // 获取当前语言
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

            // 设置窗口大小
            Width = config.Width;
            MinHeight = config.MinHeight;
            MaxHeight = config.MaxHeight;

            // 设置标题
            SetupTitle();

            // 设置内容区域
            SetupContent();

            // 设置按钮区域
            SetupButtons();

            // 设置位置
            SetupPosition();

            // 设置其他属性
            if (config.ShowCloseButton)
            {
                CloseButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 设置标题
        /// </summary>
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

            // 设置调用者图标
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

        /// <summary>
        /// 设置内容区域
        /// </summary>
        private void SetupContent()
        {
            // 隐藏所有内容模式
            ImageTextContent.Visibility = Visibility.Collapsed;
            CenteredTextContent.Visibility = Visibility.Collapsed;
            LeftAlignedTextContent.Visibility = Visibility.Collapsed;
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

                case DialogContentMode.Custom:
                    if (_config.CustomContent != null)
                    {
                        CustomContentPresenter.Content = _config.CustomContent;
                        CustomContentPresenter.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

        /// <summary>
        /// 设置图片+文字模式
        /// </summary>
        private void SetupImageWithText(string? text)
        {
            ImageTextContent.Visibility = Visibility.Visible;
            ImageTextBlock.Text = text ?? string.Empty;

            // 设置图片尺寸限制
            ContentImage.MaxWidth = _config.ContentImageMaxWidth;
            ContentImage.MaxHeight = _config.ContentImageMaxHeight;

            // 设置图片
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

        /// <summary>
        /// 设置按钮区域
        /// </summary>
        private void SetupButtons()
        {
            RightButtonPanel.Children.Clear();
            _actionButtons.Clear();

            // 自定义按钮区域
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

            // 设置取消按钮
            if (_config.ShowCancelButton)
            {
                CancelButton.Content = _config.GetLocalizedCancelButtonText(_currentLanguage);
                CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }

            // 添加右侧按钮（从右到左，索引0是最右侧的Primary）
            var visibleCount = Math.Min(_config.VisibleButtonCount, _config.Buttons.Count);
            visibleCount = Math.Max(1, Math.Min(4, visibleCount));

            for (int i = 0; i < visibleCount && i < _config.Buttons.Count; i++)
            {
                var buttonConfig = _config.Buttons[i];
                var button = CreateButton(buttonConfig, i);

                // 插入到最前面，因为我们是从右到左排列
                RightButtonPanel.Children.Insert(0, button);
                _actionButtons.Insert(0, button);
            }
        }

        /// <summary>
        /// 创建按钮
        /// </summary>
        private Button CreateButton(DialogButton config, int index)
        {
            var button = new Button
            {
                Content = config.GetLocalizedText(_currentLanguage),
                Tag = config,
                IsEnabled = config.IsEnabled
            };

            // 设置样式
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

            // 点击事件
            button.Click += (s, e) => OnButtonClick(config, index);

            return button;
        }

        /// <summary>
        /// 按钮点击处理
        /// </summary>
        private void OnButtonClick(DialogButton buttonConfig, int index)
        {
            _result.ClickedButton = buttonConfig;
            _result.ButtonTag = buttonConfig.Tag;
            _result.Result = (DialogResult)(index + 2); // Button1 = 2, Button2 = 3, etc.

            // 执行回调
            buttonConfig.OnClick?.Invoke(buttonConfig);

            // 关闭对话框
            if (buttonConfig.CloseOnClick)
            {
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        private void SetupPosition()
        {
            Loaded += (s, e) =>
            {
                switch (_config.PositionMode)
                {
                    case DialogPositionMode.CenterScreen:
                        CenterOnScreen();
                        break;

                    case DialogPositionMode.CenterOwner:
                        if (_config.OwnerWindow != null)
                        {
                            Owner = _config.OwnerWindow;
                            CenterOnOwner();
                        }
                        else
                        {
                            CenterOnScreen();
                        }
                        break;

                    case DialogPositionMode.Custom:
                        if (_config.CustomPosition.HasValue)
                        {
                            Left = _config.CustomPosition.Value.X;
                            Top = _config.CustomPosition.Value.Y;
                        }
                        else
                        {
                            CenterOnScreen();
                            Left += _config.CustomOffset.X;
                            Top += _config.CustomOffset.Y;
                        }
                        break;
                }
            };
        }

        /// <summary>
        /// 居中于屏幕
        /// </summary>
        private void CenterOnScreen()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            Left = (screenWidth - ActualWidth) / 2;
            Top = (screenHeight - ActualHeight) / 2;
        }

        /// <summary>
        /// 居中于父窗口
        /// </summary>
        private void CenterOnOwner()
        {
            if (Owner == null) return;

            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
        }

        /// <summary>
        /// 窗口拖动
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_config.IsDraggable && e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 键盘事件
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _config.CloseOnEscape)
            {
                _result.Result = Shared.Class.DialogResult.Cancel;
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                // 触发主按钮
                if (_config.Buttons.Count > 0)
                {
                    OnButtonClick(_config.Buttons[0], 0);
                }
            }
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result.Result = Shared.Class.DialogResult.Cancel;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _result.Result = Shared.Class.DialogResult.Cancel;
            DialogResult = false;
            Close();
        }

        #region 静态便捷方法

        /// <summary>
        /// 显示对话框（模态）
        /// </summary>
        public static DialogCallbackResult Show(DialogConfig config)
        {
            var dialog = new PCOPhobosDialog(config);

            if (config.IsModal)
            {
                dialog.ShowDialog();
            }
            else
            {
                dialog.Show();
            }

            return dialog.DialogCallbackResult;
        }

        /// <summary>
        /// 显示对话框（异步回调）
        /// </summary>
        public static void ShowAsync(DialogConfig config, Action<DialogCallbackResult>? callback = null)
        {
            var dialog = new PCOPhobosDialog(config);

            dialog.Closed += (s, e) =>
            {
                callback?.Invoke(dialog.DialogCallbackResult);
            };

            if (config.IsModal)
            {
                dialog.ShowDialog();
            }
            else
            {
                dialog.Show();
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
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
        /// 显示信息对话框
        /// </summary>
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
        /// 显示警告对话框
        /// </summary>
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
        /// 显示错误对话框
        /// </summary>
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
        /// 显示是/否对话框
        /// </summary>
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
        /// 显示是/否/取消对话框
        /// </summary>
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
    }
}