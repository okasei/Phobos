using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Phobos.Converter
{
    public class WidthAndHeightToRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 检查传入的值数组是否有效
            if (values == null || values.Length < 2)
                return new Rect(0, 0, 0, 0); // 返回一个默认的Rect

            // 2. 检查每个值是否为有效的 double 类型，或者是绑定未就绪的标记
            if (values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue ||
                !(values[0] is double) ||
                !(values[1] is double))
            {
                // 当绑定值无效时，返回一个“空”或默认的矩形，或者返回DependencyProperty.UnsetValue让绑定系统重试
                return DependencyProperty.UnsetValue;
            }

            // 3. 安全地进行类型转换
            double width = (double)values[0];
            double height = (double)values[1];

            // 4. （可选）确保数值合理
            if (width >= 0 && height >= 0)
            {
                return new Rect(0, 0, width, height);
            }

            return new Rect(0, 0, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
