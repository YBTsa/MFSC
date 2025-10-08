// StringToBrushConverter.cs
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MFSC.Helpers
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorName)
            {
                // 支持颜色名称（如 "Red"）或十六进制（如 "#FF0000"）
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(colorName)!;
                }
                catch
                {
                    return Brushes.Transparent; // 默认值
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}