using System.Globalization;
using System.Windows.Data;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class BooleanToOpacityConverter : IValueConverter
    {
        public double TrueOpacity { get; set; } = 1.0;

        public double FalseOpacity { get; set; } = 0.25;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? TrueOpacity : FalseOpacity;
            }

            return FalseOpacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double opacity)
            {
                return opacity >= (TrueOpacity + FalseOpacity) / 2.0;
            }

            return false;
        }
    }
}
