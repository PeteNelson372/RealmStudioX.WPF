using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections;

namespace RealmStudioX.WPF.Editor.Services
{
    public class HeightMapManager
    {
        private HeightMapPanelViewModel heightMapViewModel;

        private BitArray? _landformMask;
        public BitArray? LandformMask => _landformMask;

        private int _landformMaskWidth;
        public int LandformMaskWidth => _landformMaskWidth;

        private int _landformMaskHeight;
        public int LandformMaskHeight => _landformMaskHeight;

        public HeightMapManager(HeightMapPanelViewModel heightMapViewModel)
        {
            this.heightMapViewModel = heightMapViewModel;
        }

        public static MapHeightMap AddHeightMapToHeightMapLayer(RealmStudioMap map)
        {
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
                heightMap = CreateHeightMap(map.MapWidth, map.MapHeight);
                heightMapLayer.Add(heightMap);
            }
            else
            {
                heightMapLayer.Add(heightMap);
            }

            return heightMap;
        }

        public static MapHeightMap CreateHeightMap(int width, int height)
        {
            using SKBitmap b2 = new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            b2.Erase(SKColors.Transparent);

            MapHeightMap heightMap = new();
            heightMap.Initialize(width, height);

            return heightMap;
        }

        public static void SetHeightMapPalette(RealmStudioMap map, HypsometricPalette palette)
        {
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            if (heightMapLayer.Shapes.Count == 0)
            {
                return;
            }


            MapHeightMap? heightMap = (MapHeightMap)heightMapLayer.Shapes[0];

            heightMap.HeightMapPalette = palette;

            heightMap.RebuildHypsometricColorLookup();
        }

        public void RenderHeightMap(
            RealmStudioMap map,
            SKCanvas renderCanvas,
            SKRect? selectedArea)
        {
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);

            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            if (heightMapLayer.Shapes.Count == 0)
                return;

            if (heightMapLayer.Shapes[0] is not MapHeightMap heightMap)
            {
                return;
            }

            // Start with the background outside the landforms.
            renderCanvas.DrawRect(
                new SKRect(1, 1, map.MapWidth, map.MapHeight),
                PaintObjects.LandformAreaSelectPaint);

            SKPathBuilder pathBuilder = new();

            for (int i = 0; i < landformLayer.Shapes.Count; i++)
            {
                if (landformLayer.Shapes[i] is Landform landform)
                {
                    pathBuilder.AddPath(landform.PerimeterPath);
                    //landformPath.AddPath(landform.PerimeterPath);
                }
            }

            SKPath landformPath = pathBuilder.Snapshot();

            pathBuilder.Detach();
            pathBuilder.Dispose();

            renderCanvas.Save();

            // Restrict the heightmap to the actual map.
            renderCanvas.ClipRect(
                new SKRect(
                    0,
                    0,
                    map.MapWidth,
                    map.MapHeight));

            // Restrict the heightmap to the landforms.
            if (!landformPath.IsEmpty)
            {
                renderCanvas.ClipPath(landformPath);

                for (int i = 0; i < landformLayer.Shapes.Count; i++)
                {
                    if (landformLayer.Shapes[i] is Landform lf)
                    {
                        lf.RenderLandformHeightMap(renderCanvas);
                    }
                }
            }

            landformPath.Dispose();

            renderCanvas.Restore();

            // Draw landform boundaries/selection information.
            for (int i = 0; i < landformLayer.Shapes.Count; i++)
            {
                if (landformLayer.Shapes[i] is Landform landform)
                {
                    renderCanvas.DrawPath(
                        landform.PerimeterPath,
                        PaintObjects.LandformHeightMapOutlinePaint);

                    if (landform.IsSelected)
                    {
                        landform.PerimeterPath.GetBounds(
                            out SKRect boundsRect);

                        renderCanvas.DrawRect(
                            boundsRect,
                            PaintObjects.LandformSelectPaint);
                    }
                }
            }

            if (heightMapViewModel.ShowContourLines)
            {
                using SKPaint contourPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = heightMapViewModel.LineColor.ToSKColor(),
                    StrokeWidth = heightMapViewModel.ContourLineWidth,
                    IsAntialias = true
                };

                using SKPaint majorContourPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = heightMapViewModel.MajorLineColor.ToSKColor(),
                    StrokeWidth = heightMapViewModel.MajorLineWidth,
                    IsAntialias = true
                };

                heightMap.RenderContours(
                    renderCanvas,
                    heightMapViewModel.ContourInterval,
                    heightMapViewModel.MajorContourInterval,
                    contourPaint,
                    majorContourPaint);
            }

            if (selectedArea != null)
            {
                renderCanvas.DrawRect(
                    (SKRect)selectedArea,
                    PaintObjects.LandformAreaSelectPaint);
            }
        }

        internal void InitializeHeightMapPanel(RealmStudioMap map, HypsometricPalette selectedPalette)
        {
            MapHeightMap heightMap = AddHeightMapToHeightMapLayer(map);

            // set the height map palette
            SetHeightMapPalette(map, selectedPalette);

            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);

            heightMap.RebuildHypsometricColorLookup();

            foreach (MapComponent2D shape in landformLayer.Shapes)
            {
                if (shape is Landform landform)
                {
                    landform.RebuildHeightMapBitmap(heightMap);
                }
            }

            BuildLandformMask(map);
        }

        private void BuildLandformMask(RealmStudioMap map)
        {
            _landformMask = null;
            _landformMaskWidth = 0;
            _landformMaskHeight = 0;

            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);

            if (map == null ||
                landformLayer.Shapes.Count == 0)
            {
                return;
            }

            _landformMaskWidth =
                map.MapWidth;

            _landformMaskHeight =
                map.MapHeight;

            int pixelCount =
                checked(
                    _landformMaskWidth *
                    _landformMaskHeight);

            BitArray mask =
                new(pixelCount);

            /*
             * Rasterize each landform once when the HeightMapTool
             * becomes active. SKPath.Contains() is deliberately kept
             * out of the painting hot path.
             */
            foreach (MapComponent2D shape in landformLayer.Shapes)
            {
                if (shape is not Landform landform)
                {
                    continue;
                }

                landform.PerimeterPath.GetBounds(
                    out SKRect bounds);

                int left =
                    Math.Max(
                        0,
                        (int)Math.Floor(bounds.Left));

                int top =
                    Math.Max(
                        0,
                        (int)Math.Floor(bounds.Top));

                int right =
                    Math.Min(
                        _landformMaskWidth - 1,
                        (int)Math.Ceiling(bounds.Right));

                int bottom =
                    Math.Min(
                        _landformMaskHeight - 1,
                        (int)Math.Ceiling(bounds.Bottom));

                if (left > right ||
                    top > bottom)
                {
                    continue;
                }

                for (int y = top;
                     y <= bottom;
                     y++)
                {
                    int rowIndex =
                        y * _landformMaskWidth;

                    for (int x = left;
                         x <= right;
                         x++)
                    {
                        int index =
                            rowIndex + x;

                        if (mask[index])
                            continue;

                        if (landform.PerimeterPath.Contains(x, y))
                            mask[index] = true;
                    }
                }
            }

            _landformMask = mask;
        }

    }
}
