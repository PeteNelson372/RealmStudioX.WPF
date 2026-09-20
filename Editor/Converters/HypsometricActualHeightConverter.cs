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
                // There is no negative elevation range.
                if (minimumHeight >= 0.0f)
                    actualHeight = 0.0f;
                else
                    actualHeight = normalizedHeight * MathF.Abs(minimumHeight);
            }
            else
            {
                // There is no positive elevation range.
                if (maximumHeight <= 0.0f)
                    actualHeight = 0.0f;
                else
                    actualHeight = normalizedHeight * maximumHeight;
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