using RealmStudioShapeRenderingLib;
using SkiaSharp;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Editor.Services
{
    public class HeightMapManager
    {
        public static void AddMapImagesToHeightMapLayer(RealmStudioMap map)
        {
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            MapHeightMap? heightMap = null;

            foreach (MapComponent2D comp in heightMapLayer.Shapes)
            {
                // heightmap has already been added
                if (comp is MapHeightMap mhm)
                {
                    heightMap = mhm;
                    break;
                }
            }

            heightMapLayer.Clear();

            if (heightMap == null)
            {
                MapHeightMap newHeightMap = CreateHeightMap(map.MapWidth, map.MapHeight);

                heightMapLayer.Add(newHeightMap);
            }
            else
            {
                heightMapLayer.Add(heightMap);
            }
        }

        public static MapHeightMap CreateHeightMap(int width, int height)
        {
            using SKBitmap b2 = new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            b2.Erase(SKColors.Transparent);

            MapHeightMap heightMap = new();
            heightMap.Initialize(width, height);

            return heightMap;
        }

        internal static void RenderHeightMap(RealmStudioMap map, SKCanvas renderCanvas, SKRect? selectedArea)
        {
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            renderCanvas.DrawRect(new SKRect(1, 1, map.MapWidth, map.MapHeight), PaintObjects.LandformAreaSelectPaint);

            SKPathBuilder pathBuilder = new SKPathBuilder();

            for (int i = 0; i < landformLayer.Shapes.Count; i++)
            {
                if (landformLayer.Shapes[i] is Landform l)
                {
                    l.RenderLandformForHeightMap(map, renderCanvas);
                    pathBuilder.AddPath(l.PerimeterPath);
                }
            }

            renderCanvas.ClipPath(pathBuilder.Snapshot());

            pathBuilder.Detach();
            pathBuilder.Dispose();

            foreach (MapComponent2D c in heightMapLayer.Shapes)
            {
                if (c is MapHeightMap mhm && mhm.HeightMapBitmap != null)
                {
                    renderCanvas.DrawBitmap(mhm.HeightMapBitmap, 0, 0, SKSamplingOptions.Default);
                    break;
                }
            }

            if (selectedArea != null)
            {
                renderCanvas.DrawRect((SKRect)selectedArea, PaintObjects.LandformAreaSelectPaint);
            }
        }

        internal static void ChangeHeightMapAreaHeight(RealmStudioMap? map, MapHeightMap activeHeightMap, SKPoint mapPoint, float brushRadius, float changeAmount)
        {
            ArgumentNullException.ThrowIfNull(map);

            float[,]? heightMap = activeHeightMap.HeightMap;

            SKBitmap? heightMapBitmap = activeHeightMap.HeightMapBitmap;

            if (heightMapBitmap != null && heightMap != null)
            {
                ApplyHeightBrush(mapPoint.X, mapPoint.Y, brushRadius, heightMap, changeAmount);
                //activeHeightMap.RebuildHeightMapBitmap();

                int left = (int)Math.Max(1, mapPoint.X - brushRadius);
                int right = (int)Math.Min(map.MapWidth - 2, mapPoint.X + brushRadius);
                int top = (int)Math.Max(1, mapPoint.Y - brushRadius);
                int bottom = (int)Math.Min(map.MapHeight - 2, mapPoint.Y + brushRadius);

                MapHeightMap.UpdateHeightMapBitmap(heightMapBitmap, heightMap, left, top, right, bottom);
            }            
        }

        private static void ApplyHeightBrush(
            float centerX,
            float centerY,
            float radius,
            float[,] heightMap,
            float changeAmount)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            float radiusSquared = radius * radius;

            int left = (int)Math.Max(1, centerX - radius);
            int right = (int)Math.Min(width - 2, centerX + radius);
            int top = (int)Math.Max(1, centerY - radius);
            int bottom = (int)Math.Min(height - 2, centerY + radius);

            for (int y = top; y <= bottom; y++)
            {
                int dy = (int)(y - centerY);
                int dySquared = dy * dy;

                for (int x = left; x <= right; x++)
                {
                    int dx = (int)(x - centerX);

                    if (dx * dx + dySquared > radiusSquared)
                        continue;

                    float value = heightMap[x, y];

                    // Increase/decrease the height.
                    value += changeAmount;

                    // Calculate the 3x3 average.
                    float average =
                        heightMap[x - 1, y - 1] +
                        heightMap[x, y - 1] +
                        heightMap[x + 1, y - 1] +

                        heightMap[x - 1, y] +
                        value +
                        heightMap[x + 1, y] +

                        heightMap[x - 1, y + 1] +
                        heightMap[x, y + 1] +
                        heightMap[x + 1, y + 1];

                    value = average / 9.0f;

                    heightMap[x, y] = Math.Clamp(value, 35.0f, 255.0f);
                }
            }
        }

        private static void UpdateHeightMapBitmap(
            SKBitmap bitmap,
            float[,] heightMap,
            int left,
            int top,
            int right,
            int bottom)
        {
            using SKPixmap? pixmap = bitmap.PeekPixels();

            if (pixmap == null)
                return;

            IntPtr pixels = pixmap.GetPixels();
            int rowBytes = pixmap.RowBytes;

            for (int y = top; y <= bottom; y++)
            {
                IntPtr row = pixels + (y * rowBytes);

                for (int x = left; x <= right; x++)
                {
                    byte value = (byte)Math.Clamp(
                        MathF.Round(heightMap[x, y]),
                        35.0f,
                        255.0f);

                    int offset = x * 4;

                    System.Runtime.InteropServices.Marshal.WriteByte(
                        row + offset,
                        value);

                    System.Runtime.InteropServices.Marshal.WriteByte(
                        row + offset + 1,
                        value);

                    System.Runtime.InteropServices.Marshal.WriteByte(
                        row + offset + 2,
                        value);

                    System.Runtime.InteropServices.Marshal.WriteByte(
                        row + offset + 3,
                        255);
                }
            }
        }
    }
}
