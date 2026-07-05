using SkiaSharp;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class SKColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is SKColor color)
            {
                return new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        color.Alpha,
                        color.Red,
                        color.Green,
                        color.Blue));
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
