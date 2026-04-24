using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class ColorToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
