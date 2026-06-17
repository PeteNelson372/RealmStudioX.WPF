using RealmStudioX.WPF.Properties;
using SkiaSharp;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.EditorUtilities
{
    public static class UserInterfaceUtilities
    {
        public static string SelectBitmapFile()
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Image File",               
                Filter = GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = Settings.Default.LastFileDirectory
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                Settings.Default.LastFileDirectory = Path.GetDirectoryName(dialog.FileName);
                Settings.Default.Save();

                return dialog.FileName;
            }

            return string.Empty;
        }

        public static ImageSource? ToImageSource(this SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.IsEmpty)
            {
                return null;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze(); // IMPORTANT for performance

            return bmp;
        }

        public static BitmapImage LoadBitmapImage(string path)
        {
            BitmapImage image = new();

            image.BeginInit();

            image.CacheOption =
                BitmapCacheOption.OnLoad;

            image.UriSource =
                new Uri(
                    Path.GetFullPath(path));

            image.EndInit();

            image.Freeze();

            return image;
        }

        internal static SKBitmap[] SliceNinePatchBitmap(
            SKBitmap bitmap,
            SKRectI center)
        {
            SKBitmap[] slices = new SKBitmap[9];

            SKRectI[] rects =
            [
                // A
                new SKRectI(
                0,
                0,
                center.Left,
                center.Top),

                // B
                new SKRectI(
                    center.Left,
                    0,
                    center.Right,
                    center.Top),

                // C
                new SKRectI(
                    center.Right,
                    0,
                    bitmap.Width,
                    center.Top),

                // D
                new SKRectI(
                    0,
                    center.Top,
                    center.Left,
                    center.Bottom),

                // E
                new SKRectI(
                    center.Left,
                    center.Top,
                    center.Right,
                    center.Bottom),

                // F
                new SKRectI(
                    center.Right,
                    center.Top,
                    bitmap.Width,
                    center.Bottom),

                // G
                new SKRectI(
                    0,
                    center.Bottom,
                    center.Left,
                    bitmap.Height),

                // H
                new SKRectI(
                    center.Left,
                    center.Bottom,
                    center.Right,
                    bitmap.Height),

                // I
                new SKRectI(
                    center.Right,
                    center.Bottom,
                    bitmap.Width,
                    bitmap.Height)
            ];

            for (int i = 0; i < 9; i++)
            {
                SKBitmap subset = new();

                bitmap.ExtractSubset(
                    subset,
                    rects[i]);

                slices[i] = subset;
            }

            return slices;
        }

        public static ImageSource CreateThumbnail(
            string imagePath,
            int maxWidth = 120,
            int maxHeight = 80)
        {
            using SKBitmap original =
                SKBitmap.Decode(imagePath);

            if (original == null)
            {
                throw new Exception(
                    $"Failed to load bitmap: {imagePath}");
            }

            float scale =
                Math.Min(
                    (float)maxWidth / original.Width,
                    (float)maxHeight / original.Height);

            int thumbWidth =
                Math.Max(
                    (int)(original.Width * scale),
                    1);

            int thumbHeight =
                Math.Max(
                    (int)(original.Height * scale),
                    1);

            using SKBitmap thumbnail =
                original.Resize(
                    new SKImageInfo(
                        thumbWidth,
                        thumbHeight),
                    SKSamplingOptions.Default);

            using SKImage image =
                SKImage.FromBitmap(thumbnail);

            using SKData data =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

            using MemoryStream ms =
                new(data.ToArray());

            BitmapImage bitmapImage = new();

            bitmapImage.BeginInit();
            bitmapImage.CacheOption =
                BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();

            bitmapImage.Freeze();

            return bitmapImage;
        }

        internal static string GetImageFilter()
        {
            return
                "All Files (*.*)|*.*" +
                "|All Pictures (*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico)" +
                    "|*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico" +
                "|Windows Enhanced Metafile (*.emf)|*.emf" +
                "|Windows Metafile (*.wmf)|*.wmf" +
                "|JPEG File Interchange Format (*.jpg;*.jpeg;*.jfif;*.jpe)|*.jpg;*.jpeg;*.jfif;*.jpe" +
                "|Portable Network Graphics (*.png)|*.png" +
                "|Bitmap Image File (*.bmp;*.dib;*.rle)|*.bmp;*.dib;*.rle" +
                "|Graphics Interchange Format (*.gif)|*.gif" +
                "|Compressed Windows Enhanced Metafile (*.emz)|*.emz" +
                "|Compressed Windows MetaFile (*.wmz)|*.wmz" +
                "|Tag Image File Format (*.tif;*.tiff)|*.tif;*.tiff" +
                "|Scalable Vector Graphics (*.svg)|*.svg" +
                "|Icon (*.ico)|*.ico";
        }

        internal static string GetCommonImageFilter()
        {
            return
                "All Image Files (*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif)" +
                    "|*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;" +
                "|JPEG File Interchange Format (*.jpg;*.jpeg;*.jfif;*.jpe)|*.jpg;*.jpeg;*.jfif;*.jpe" +
                "|Portable Network Graphics (*.png)|*.png" +
                "|Bitmap Image File (*.bmp;*.dib;*.rle)|*.bmp;*.dib;*.rle" +
                "|Graphics Interchange Format (*.gif)|*.gif" +
                "|All Files (*.*)|*.*";
        }
    }

}