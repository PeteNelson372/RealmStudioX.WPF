using System.Reflection;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    public static class ColorPalettes
    {
        public static IReadOnlyList<Color> HtmlColors { get; }

        static ColorPalettes()
        {
            HtmlColors = [.. typeof(Colors)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => (Color)p.GetValue(null)!)];
        }
    }
}
