using System.IO;
using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.EditorUtilities;

internal static class WorldGlobeAssets
{
    private static readonly string AssetDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "world_globe");

    public static string GetPath(string fileName)
    {
        string path = Path.Combine(AssetDirectory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"World globe asset not found: {fileName}", path);
        }

        return path;
    }

    public static byte[] ReadBytes(string fileName)
    {
        return File.ReadAllBytes(GetPath(fileName));
    }

    public static BitmapImage LoadBitmap(string fileName)
    {
        BitmapImage bitmap = new();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(GetPath(fileName), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}