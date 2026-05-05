using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class SymbolHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPrimary = values[0] is bool p && p;
            bool isSecondary = values[1] is bool s && s;

            if (isPrimary)
                return new SolidColorBrush(Colors.SkyBlue); // darker blue

            if (isSecondary)
                return new SolidColorBrush(Colors.AliceBlue); // lighter blue

            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
