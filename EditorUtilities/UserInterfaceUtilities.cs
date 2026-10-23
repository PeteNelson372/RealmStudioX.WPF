using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Properties;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace RealmStudioX.WPF.EditorUtilities
{
    public static class UserInterfaceUtilities
    {
        public static SKImage? CloneImage(SKImage? source)
        {
            if (source == null)
                return null;

            using SKBitmap bitmap = SKBitmap.FromImage(source);

            return SKImage.FromBitmap(bitmap);
        }

        internal static MapComponent2D? CreateScaledTransformedDrawnComponent(MapComponent2D dmc, float scaleX, float scaleY, float deltaX, float deltaY, float mapWidth, float mapHeight)
        {
            MapComponent2D? newDmc = null;

            if (dmc is DrawnArrow arrow)
            {
                IShapeState state = arrow.CaptureState();

                DrawnArrow newDrawnArrow = new();

                newDrawnArrow.RestoreState(state);

                newDrawnArrow.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnArrow.TopLeft = new SKPoint(Math.Max(0, Math.Min(arrow.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(arrow.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnArrow.BottomRight = new SKPoint(Math.Max(0, Math.Min(arrow.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(arrow.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnArrow.Bounds = new SKRect(newDrawnArrow.TopLeft.X, newDrawnArrow.TopLeft.Y, newDrawnArrow.BottomRight.X, newDrawnArrow.BottomRight.Y);

                newDmc = newDrawnArrow;
            }
            else if (dmc is DrawnDiamond diamond)
            {
                IShapeState state = diamond.CaptureState();

                DrawnDiamond newDrawnDiamond = new();

                newDrawnDiamond.RestoreState(state);

                newDrawnDiamond.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnDiamond.TopLeft = new SKPoint(Math.Max(0, Math.Min(diamond.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(diamond.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnDiamond.BottomRight = new SKPoint(Math.Max(0, Math.Min(diamond.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(diamond.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnDiamond.Bounds = new SKRect(newDrawnDiamond.TopLeft.X, newDrawnDiamond.TopLeft.Y, newDrawnDiamond.BottomRight.X, newDrawnDiamond.BottomRight.Y);

                newDmc = newDrawnDiamond;
            }
            else if (dmc is DrawnEllipse ellipse)
            {
                IShapeState state = ellipse.CaptureState();

                DrawnEllipse newDrawnEllipse = new();

                newDrawnEllipse.RestoreState(state);

                newDrawnEllipse.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnEllipse.TopLeft = new SKPoint(Math.Max(0, Math.Min(ellipse.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(ellipse.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnEllipse.BottomRight = new SKPoint(Math.Max(0, Math.Min(ellipse.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(ellipse.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnEllipse.Bounds = new SKRect(newDrawnEllipse.TopLeft.X, newDrawnEllipse.TopLeft.Y, newDrawnEllipse.BottomRight.X, newDrawnEllipse.BottomRight.Y);

                newDmc = newDrawnEllipse;
            }
            else if (dmc is DrawnFivePointStar fivePointStar)
            {
                IShapeState state = fivePointStar.CaptureState();

                DrawnFivePointStar newFivePointStar = new();

                newFivePointStar.RestoreState(state);

                newFivePointStar.BrushSize *= (int)((scaleX + scaleY) / 2);

                newFivePointStar.Center = new SKPoint(Math.Max(0, Math.Min(fivePointStar.Center.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(fivePointStar.Center.Y * scaleY + deltaY, mapHeight)));

                newFivePointStar.Radius = fivePointStar.Radius * ((scaleX + scaleY) / 2);

                SKRect bounds = new(
                    newFivePointStar.Center.X - newFivePointStar.Radius,
                    newFivePointStar.Center.Y - newFivePointStar.Radius,
                    newFivePointStar.Center.X + newFivePointStar.Radius,
                    newFivePointStar.Center.Y + newFivePointStar.Radius);

                newFivePointStar.Bounds = bounds;

                newDmc = newFivePointStar;
            }
            else if (dmc is DrawnRegularPolygon poly)
            {
                IShapeState state = poly.CaptureState();

                DrawnRegularPolygon newDrawnRegularPolygon = new();

                newDrawnRegularPolygon.RestoreState(state);

                newDrawnRegularPolygon.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnRegularPolygon.TopLeft = new SKPoint(Math.Max(0, Math.Min(poly.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(poly.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnRegularPolygon.BottomRight = new SKPoint(Math.Max(0, Math.Min(poly.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(poly.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnRegularPolygon.Bounds = new SKRect(newDrawnRegularPolygon.TopLeft.X, newDrawnRegularPolygon.TopLeft.Y, newDrawnRegularPolygon.BottomRight.X, newDrawnRegularPolygon.BottomRight.Y);

                newDmc = newDrawnRegularPolygon;

            }
            else if (dmc is DrawnPolygon polygon)
            {
                IShapeState state = polygon.CaptureState();

                DrawnPolygon newDrawnPolygon = new();

                newDrawnPolygon.RestoreState(state);

                newDrawnPolygon.BrushSize *= (int)((scaleX + scaleY) / 2);

                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    newDrawnPolygon.Points.Add(new SKPoint(Math.Max(0, Math.Min(polygon.Points[i].X * scaleX + deltaX, mapWidth)), Math.Max(0, Math.Min(polygon.Points[i].Y * scaleY + deltaY, mapHeight))));
                }

                using SKPath path = Utilities.BuildPath(newDrawnPolygon.Points);

                path.GetBounds(out SKRect bounds);

                newDrawnPolygon.Bounds = bounds;

                newDmc = newDrawnPolygon;
            }
            else if (dmc is DrawnRectangle rectangle)
            {
                IShapeState state = rectangle.CaptureState();

                DrawnRectangle newDrawnRectangle = new();

                newDrawnRectangle.RestoreState(state);

                newDrawnRectangle.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnRectangle.TopLeft = new SKPoint(Math.Max(0, Math.Min(rectangle.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(rectangle.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnRectangle.BottomRight = new SKPoint(Math.Max(0, Math.Min(rectangle.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(rectangle.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnRectangle.Bounds = new SKRect(newDrawnRectangle.TopLeft.X, newDrawnRectangle.TopLeft.Y, newDrawnRectangle.BottomRight.X, newDrawnRectangle.BottomRight.Y);
                
                newDmc = newDrawnRectangle;
            }
            else if (dmc is DrawnSixPointStar sixPointStar)
            {
                IShapeState state = sixPointStar.CaptureState();

                DrawnSixPointStar newSixPointStar = new();

                newSixPointStar.RestoreState(state);

                newSixPointStar.BrushSize *= (int)((scaleX + scaleY) / 2);

                newSixPointStar.Center = new SKPoint(Math.Max(0, Math.Min(sixPointStar.Center.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(sixPointStar.Center.Y * scaleY + deltaY, mapHeight)));

                newSixPointStar.Radius = sixPointStar.Radius * ((scaleX + scaleY) / 2);

                SKRect bounds = new(
                    newSixPointStar.Center.X - newSixPointStar.Radius,
                    newSixPointStar.Center.Y - newSixPointStar.Radius,
                    newSixPointStar.Center.X + newSixPointStar.Radius,
                    newSixPointStar.Center.Y + newSixPointStar.Radius);

                newSixPointStar.Bounds = bounds;

                newDmc = newSixPointStar;
            }
            else if (dmc is DrawnStamp stamp)
            {
                IShapeState state = stamp.CaptureState();

                DrawnStamp newDrawnStamp = new();

                newDrawnStamp.RestoreState(state);

                newDrawnStamp.TopLeft = new SKPoint(Math.Max(0, Math.Min(stamp.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(stamp.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnStamp.Scale = stamp.Scale * ((scaleX + scaleY) / 2);

                newDrawnStamp.Bounds = new SKRect(newDrawnStamp.TopLeft.X, newDrawnStamp.TopLeft.Y,
                    newDrawnStamp.TopLeft.X + newDrawnStamp.StampImage.Width, newDrawnStamp.TopLeft.Y + newDrawnStamp.StampImage.Height);

                newDmc = newDrawnStamp;
            }
            else if (dmc is DrawnTriangle triangle)
            {
                IShapeState state = triangle.CaptureState();

                DrawnTriangle newDrawnTriangle = new();

                newDrawnTriangle.RestoreState(state);

                newDrawnTriangle.BrushSize *= (int)((scaleX + scaleY) / 2);

                newDrawnTriangle.TopLeft = new SKPoint(Math.Max(0, Math.Min(triangle.TopLeft.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(triangle.TopLeft.Y * scaleY + deltaY, mapHeight)));

                newDrawnTriangle.BottomRight = new SKPoint(Math.Max(0, Math.Min(triangle.BottomRight.X * scaleX + deltaX, mapWidth)),
                    Math.Max(0, Math.Min(triangle.BottomRight.Y * scaleY + deltaY, mapHeight)));

                newDrawnTriangle.Bounds = new SKRect(newDrawnTriangle.TopLeft.X, newDrawnTriangle.TopLeft.Y, newDrawnTriangle.BottomRight.X, newDrawnTriangle.BottomRight.Y);

                newDmc = newDrawnTriangle;
            }
            else if (dmc is DrawingErase erase)
            {
                DrawingErase newDrawingErase = new()
                {
                    BrushSize = (int)(erase.BrushSize * ((scaleX + scaleY) / 2)),
                    IsSelected = false,
                };

                for (int i = 0; i < erase.Points.Count; i++)
                {
                    newDrawingErase.Points.Add(new SKPoint(Math.Max(0, Math.Min(erase.Points[i].X * scaleX + deltaX, mapWidth)), Math.Max(0, Math.Min(erase.Points[i].Y * scaleY + deltaY, mapHeight))));
                }

                using SKPath path = Utilities.BuildPath(newDrawingErase.Points);

                path.GetBounds(out SKRect bounds);

                newDrawingErase.Bounds = bounds;

                newDmc = newDrawingErase;

            }
            else if (dmc is PaintedLine paintedLine)
            {
                PaintedLine newPaintedLine = new()
                {
                    Brush = paintedLine.Brush,
                    IsSelected = false,
                };

                if (paintedLine.Brush != null)
                {
                    newPaintedLine.Brush!.BrushSize = (int)(paintedLine.Brush!.BrushSize * ((scaleX + scaleY) / 2));
                }

                for (int i = 0; i < paintedLine.Points.Count; i++)
                {
                    newPaintedLine.Points.Add(new SKPoint(Math.Max(0, Math.Min(paintedLine.Points[i].X * scaleX + deltaX, mapWidth)), Math.Max(0, Math.Min(paintedLine.Points[i].Y * scaleY + deltaY, mapHeight))));
                }

                using SKPath path = Utilities.BuildPath(newPaintedLine.Points);

                path.GetBounds(out SKRect bounds);

                newPaintedLine.Bounds = bounds;

                newDmc = newPaintedLine;
            }
            else if (dmc is DrawnLine drawnLine)
            {
                DrawnLine newDrawnLine = new()
                {
                    LineColor = drawnLine.LineColor,
                    BrushSize = (int)(drawnLine.BrushSize * ((scaleX + scaleY) / 2)),
                    IsSelected = false,
                };

                for (int i = 0; i < drawnLine.Points.Count; i++)
                {
                    newDrawnLine.Points.Add(new SKPoint(Math.Max(0, Math.Min(drawnLine.Points[i].X * scaleX + deltaX, mapWidth)), Math.Max(0, Math.Min(drawnLine.Points[i].Y * scaleY + deltaY, mapHeight))));
                }

                using SKPath path = Utilities.BuildPath(newDrawnLine.Points);

                path.GetBounds(out SKRect bounds);

                newDrawnLine.Bounds = bounds;

                newDmc = newDrawnLine;
            }

            return newDmc;
        }

        public static bool IsValidFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            fileName = fileName.Trim();

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            if (fileName.EndsWith(' ') || fileName.EndsWith('.'))
                return false;

            string[] reservedNames =
            [
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            ];

            return !reservedNames.Contains(fileName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Positions a window relative to an anchor point on a FrameworkElement.
        /// </summary>
        /// <param name="window">The window to position.</param>
        /// <param name="control">The control to position relative to.</param>
        /// <param name="anchor">
        /// Anchor point within the control, in control coordinates.
        /// (0,0) = upper-left.
        /// </param>
        /// <param name="offsetX">
        /// Horizontal offset from the anchor point, in device-independent pixels.
        /// </param>
        /// <param name="offsetY">
        /// Vertical offset from the anchor point, in device-independent pixels.
        /// </param>
        public static void PositionWindowRelativeToControl(
            Window window,
            FrameworkElement control,
            Point anchor,
            double offsetX = 0,
            double offsetY = 0)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(control);

            PresentationSource? source =
                PresentationSource.FromVisual(control);

            if (source == null)
            {
                return;
            }

            // Convert the control anchor point to screen coordinates.
            Point screenPoint = control.PointToScreen(anchor);

            // Convert device pixels to WPF device-independent units.
            Matrix transform = source.CompositionTarget.TransformFromDevice;

            Point screen = transform.Transform(screenPoint);

            // Measure the window so SizeToContent windows have a valid size.
            window.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Size desired = window.DesiredSize;

            double width = desired.Width;
            double height = desired.Height;

            window.WindowStartupLocation = WindowStartupLocation.Manual;

            window.Left = screen.X + offsetX;
            window.Top = screen.Y + offsetY;

            // Keep the entire window on-screen.
            Rect workArea = SystemParameters.WorkArea;

            if (window.Left < workArea.Left)
                window.Left = workArea.Left;

            if (window.Top < workArea.Top)
                window.Top = workArea.Top;

            if (window.Left + width > workArea.Right)
                window.Left = workArea.Right - width;

            if (window.Top + height > workArea.Bottom)
                window.Top = workArea.Bottom - height;
        }

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

        internal static string GetZipFileFilter()
        {
            return "Zip File (*.zip)|*.zip" +
                "|All Files (*.*)|*.*";
        }

        internal static string GetRealmStudioMapXmlFileFilter()
        {
            return "Realm Studio Map (*.rsmx)|*.rsmx" +
                "|All Files (*.*)|*.*";
        }
    }

}