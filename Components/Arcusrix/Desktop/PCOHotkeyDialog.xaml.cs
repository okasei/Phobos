using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Phobos.Manager.Hotkey;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// 快捷键绑定对话框
    /// </summary>
    public partial class PCOHotkeyDialog : Window
    {
        private ModifierKeys _currentModifiers = ModifierKeys.None;
        private Key _currentKey = Key.None;
        private string? _existingHotkey;

        /// <summary>
        /// 对话框结果 - 绑定的快捷键字符串
        /// </summary>
        public string? Result { get; private set; }

        /// <summary>
        /// 创建快捷键绑定对话框
        /// </summary>
        /// <param name="existingHotkey">现有的快捷键（用于编辑）</param>
        public PCOHotkeyDialog(string? existingHotkey = null)
        {
            InitializeComponent();
            _existingHotkey = existingHotkey;
            Loaded += PCOHotkeyDialog_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayOpenAnimation();
        }

        private void PCOHotkeyDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置本地化文本
            DialogTitle.Text = DesktopLocalization.Get(DesktopLocalization.Hotkey_Title);
            HintLabel.Text = DesktopLocalization.Get(DesktopLocalization.Hotkey_Hint);
            ClearButton.Content = DesktopLocalization.Get(DesktopLocalization.Hotkey_Clear);
            CancelButton.Content = DesktopLocalization.Get(DesktopLocalization.Shortcut_Cancel);
            SaveButton.Content = DesktopLocalization.Get(DesktopLocalization.Shortcut_Save);

            // 如果有现有快捷键，解析并显示
            if (!string.IsNullOrEmpty(_existingHotkey))
            {
                var hotkeyInfo = HotkeyInfo.Parse(_existingHotkey);
                if (hotkeyInfo != null)
                {
                    _currentModifiers = hotkeyInfo.Modifiers;
                    _currentKey = hotkeyInfo.Key;
                    UpdateHotkeyDisplay();
                }
            }
            else
            {
                HotkeyDisplay.Text = DesktopLocalization.Get(DesktopLocalization.Hotkey_PressKey);
            }
        }

        /// <summary>
        /// 捕获按键
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // 获取实际按下的键（忽略修饰键的Key变体）
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // 如果只按了修饰键，不更新主键
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // 忽略一些系统键
            if (key == Key.Escape)
            {
                PlayCloseAnimation(() =>
                {
                    DialogResult = false;
                    Close();
                });
                return;
            }

            // 获取当前修饰键状态
            _currentModifiers = Keyboard.Modifiers;
            _currentKey = key;

            // 必须至少有一个修饰键（除了功能键）
            if (_currentModifiers == ModifierKeys.None &&
                !(key >= Key.F1 && key <= Key.F24))
            {
                Service.Arcusrix.PSDialogService.Warning(
                    DesktopLocalization.Get(DesktopLocalization.Hotkey_NeedModifier),
                    DesktopLocalization.Get(DesktopLocalization.Dialog_Error),
                    true,
                    this);
                return;
            }

            UpdateHotkeyDisplay();
        }

        /// <summary>
        /// 更新快捷键显示
        /// </summary>
        private void UpdateHotkeyDisplay()
        {
            if (_currentKey == Key.None)
            {
                HotkeyDisplay.Text = DesktopLocalization.Get(DesktopLocalization.Hotkey_PressKey);
                return;
            }

            var hotkeyInfo = new HotkeyInfo
            {
                Modifiers = _currentModifiers,
                Key = _currentKey
            };

            HotkeyDisplay.Text = hotkeyInfo.GetDisplayString();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _currentModifiers = ModifierKeys.None;
            _currentKey = Key.None;
            HotkeyDisplay.Text = DesktopLocalization.Get(DesktopLocalization.Hotkey_None);
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentKey == Key.None)
            {
                // 清除快捷键
                Result = string.Empty;
            }
            else
            {
                var hotkeyInfo = new HotkeyInfo
                {
                    Modifiers = _currentModifiers,
                    Key = _currentKey
                };
                Result = hotkeyInfo.ToStorageString();
            }

            PlayCloseAnimation(() =>
            {
                DialogResult = true;
                Close();
            });
        }

        #region 动画

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

        #endregion

        /// <summary>
        /// 显示对话框并返回结果
        /// </summary>
        public static string? ShowDialog(Window owner, string? existingHotkey = null)
        {
            var dialog = new PCOHotkeyDialog(existingHotkey)
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
