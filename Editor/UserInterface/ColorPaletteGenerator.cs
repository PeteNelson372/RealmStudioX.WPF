using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public static class ColorPaletteGenerator
    {
        public static List<SolidColorBrush> GenerateCompactPalette()
        {
            var list = new List<SolidColorBrush>();

            int cols = 16;

            var baseColors = new List<Color>
            {
                Colors.Red,
                Colors.Orange,
                Colors.Gold,
                Colors.Yellow,
                Colors.YellowGreen,
                Colors.Green,
                Colors.Teal,
                Colors.Cyan,
                Colors.SkyBlue,
                Colors.Blue,
                Colors.Indigo,
                Colors.Violet
            };

            foreach (var baseColor in baseColors)
            {
                for (int c = 0; c < cols; c++)
                {
                    double t = (c + 1) / (double)(cols + 1);

                    Color color;

                    if (t < 0.5)
                    {
                        double t2 = t / 0.5;
                        color = Lerp(Colors.Black, baseColor, t2);
                    }
                    else
                    {
                        double t2 = (t - 0.5) / 0.5;
                        color = Lerp(baseColor, Colors.White, t2);
                    }

                    list.Add(new SolidColorBrush(color));
                }
            }

            return list;
        }

        public static List<SolidColorBrush> GenerateGrayScaleRow()
        {
            var list = new List<SolidColorBrush>();

            int cols = 16;

            for (int i = 0; i < cols; i++)
            {
                byte v = (byte)(i * 255 / (cols - 1));
                list.Add(new SolidColorBrush(Color.FromRgb(v, v, v)));
            }

            return list;
        }

        public static List<SolidColorBrush> GenerateQuickRow()
        {
            return new List<SolidColorBrush>
            {
                new SolidColorBrush(Colors.Black),
                new SolidColorBrush(Colors.Maroon),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(Colors.Olive),
                new SolidColorBrush(Colors.Navy),
                new SolidColorBrush(Colors.Purple),
                new SolidColorBrush(Colors.Teal),
                new SolidColorBrush(Colors.Silver),
                new SolidColorBrush(Colors.Gray),
                new SolidColorBrush(Colors.Red),
                new SolidColorBrush(Colors.Lime),
                new SolidColorBrush(Colors.Yellow),
                new SolidColorBrush(Colors.Blue),
                new SolidColorBrush(Colors.Fuchsia),
                new SolidColorBrush(Colors.Aqua),
                new SolidColorBrush(Colors.White)
            };
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }
    }
}
