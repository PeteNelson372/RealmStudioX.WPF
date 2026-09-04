using System.Globalization;
using System.Windows.Data;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class HypsometricActualHeightConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values.Length < 4)
                return string.Empty;

            if (values[0] is not float normalizedHeight)
                return string.Empty;

            if (values[1] is not float minimumHeight)
                return string.Empty;

            if (values[2] is not float maximumHeight)
                return string.Empty;

            float actualHeight;

            if (normalizedHeight < 0.0f)
            {
                actualHeight =
                    normalizedHeight *
                    MathF.Abs(minimumHeight);
            }
            else
            {
                actualHeight =
                    normalizedHeight *
                    maximumHeight;
            }

            return $"{actualHeight:0.##}";
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}