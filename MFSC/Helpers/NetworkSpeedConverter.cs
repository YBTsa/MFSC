using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace MFSC.Helpers
{
    internal class NetworkSpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int) throw new ArgumentException("ExceptionNetworkSpeedConverterValueMustBeAnInteger");
            int speed = (int)value;
            if (speed <= 1024)
            {
                return $"{speed} B";
            }
            else if (speed <= 1024*1024)
            {
                return $"{(speed / 1024.0):F0} KB";
            }
            else if (speed <= 1024 * 1024 * 1024)
            {
                return $"{(speed / (1024.0 * 1024.0)):F0} MB";
            }
            else
            {
                return $"{(speed / (1024.0 * 1024.0 * 1024.0)):F0} GB";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            return Enum.Parse(typeof(ApplicationTheme), enumString);
        }
    }
}
