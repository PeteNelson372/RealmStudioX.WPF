using SkiaSharp;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.Converters
{
    public class ContrastingBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush DarkBrush =
            new(Color.FromRgb(32, 32, 32));

        private static readonly SolidColorBrush LightBrush =
            new(Color.FromRgb(245, 245, 245));

        static ContrastingBrushConverter()
        {
            DarkBrush.Freeze();
            LightBrush.Freeze();
        }

        public object Convert(object value,
                              Type targetType,
                              object parameter,
                              CultureInfo culture)
        {
            if (value is not SKColor color)
                return DarkBrush;

            double luminance =
                0.299 * color.Red +
                0.587 * color.Green +
                0.114 * color.Blue;

            return luminance < 145
                ? LightBrush
                : DarkBrush;
        }

        public object ConvertBack(object value,
                                  Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
