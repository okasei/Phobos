using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// PCOInputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PCOInputDialog : Window
    {
        public string InputValue { get; private set; } = string.Empty;

        public PCOInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            TitleText.Text = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;

            InputTextBox.Focus();
            InputTextBox.SelectAll();

            // 支持回车键确认
            InputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    OkButton_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == Key.Escape)
                {
                    CancelButton_Click(this, new RoutedEventArgs());
                }
            };
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputTextBox.Text.Trim();
            PlayCloseAnimation(() =>
            {
                DialogResult = true;
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

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        public static string? Show(Window owner, string title, string prompt, string defaultValue = "")
        {
            var dialog = new PCOInputDialog(title, prompt, defaultValue)
            {
                Owner = owner
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.InputValue;
            }

            return null;
        }
    }
}