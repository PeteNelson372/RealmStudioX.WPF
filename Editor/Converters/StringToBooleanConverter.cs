using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
