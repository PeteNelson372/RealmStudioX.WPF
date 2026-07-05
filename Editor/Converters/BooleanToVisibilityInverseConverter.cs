using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class BooleanToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return !(value is Visibility visibility &&
                     visibility == Visibility.Visible);
        }
    }
}
