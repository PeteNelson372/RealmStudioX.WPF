using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class ZoomToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return $"{d * 100:0}%";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
