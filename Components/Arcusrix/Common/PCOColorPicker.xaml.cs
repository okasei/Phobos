using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Phobos.Class.Theme;

namespace Phobos.Components.Arcusrix.Common
{
    public partial class PCOColorPicker : UserControl
    {
        public string ColorHex
        {
            get => (string)GetValue(ColorHexProperty);
            set => SetValue(ColorHexProperty, value);
        }

        public static readonly DependencyProperty ColorHexProperty = DependencyProperty.Register(
            nameof(ColorHex), typeof(string), typeof(PCOColorPicker), new PropertyMetadata("#FFFFFF", OnColorHexChanged));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
            nameof(LabelText), typeof(string), typeof(PCOColorPicker), new PropertyMetadata("Color", OnLabelTextChanged));

        public event EventHandler<string>? ColorChanged;

        public PCOColorPicker()
        {
            InitializeComponent();
        }

        private static void OnColorHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PCOColorPicker)d;
            var newHex = (string)e.NewValue;
            ctrl.HexText.Text = newHex;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(newHex);
                ctrl.ColorPreview.Background = new SolidColorBrush(color);
            }
            catch
            { }
        }

        private static void OnLabelTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (PCOColorPicker)d;
            ctrl.Label.Text = (string)e.NewValue;
        }

        private void ColorPreview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Open popup and initialize sliders
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(ColorHex);
                SliderR.Value = color.R;
                SliderG.Value = color.G;
                SliderB.Value = color.B;
            }
            catch { }
            UpdatePopupPreview();
            PickerPopup.IsOpen = true;
        }

        private void HexText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = HexText.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(text);
                ColorPreview.Background = new SolidColorBrush(color);
                // update sliders
                try
                {
                    SliderR.Value = color.R;
                    SliderG.Value = color.G;
                    SliderB.Value = color.B;
                    ValueR.Text = color.R.ToString();
                    ValueG.Text = color.G.ToString();
                    ValueB.Text = color.B.ToString();
                }
                catch { }
                if (!string.Equals(text, ColorHex, StringComparison.OrdinalIgnoreCase))
                {
                    ColorHex = text;
                    ColorChanged?.Invoke(this, text);
                }
            }
            catch { }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderR == null || SliderG == null || SliderB == null) return;
            ValueR.Text = ((int)SliderR.Value).ToString();
            ValueG.Text = ((int)SliderG.Value).ToString();
            ValueB.Text = ((int)SliderB.Value).ToString();
            UpdatePopupPreview();
        }

        private void UpdatePopupPreview()
        {
            try
            {
                var r = (byte)SliderR.Value;
                var g = (byte)SliderG.Value;
                var b = (byte)SliderB.Value;
                var c = Color.FromRgb(r, g, b);
                PopupPreview.Fill = new SolidColorBrush(c);
            }
            catch { }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var r = (byte)SliderR.Value;
            var g = (byte)SliderG.Value;
            var b = (byte)SliderB.Value;
            var c = Color.FromRgb(r, g, b);
            var hex = PCThemeLoader.ColorToHex(c);
            ColorHex = hex;
            ColorChanged?.Invoke(this, hex);
            PickerPopup.IsOpen = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            PickerPopup.IsOpen = false;
        }
    }
}
