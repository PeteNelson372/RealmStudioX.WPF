using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Services
{
    public class HeightMapManager
    {
        private HeightMapPanelViewModel heightMapViewModel;

        public HeightMapManager(HeightMapPanelViewModel heightMapViewModel)
        {
            this.heightMapViewModel = heightMapViewModel;
        }

        public static void AddMapImagesToHeightMapLayer(RealmStudioMap map)
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

        /*
        public void RenderHeightMap(RealmStudioMap map, SKCanvas renderCanvas, SKRect? selectedArea)
        {
            MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.LANDFORMLAYER);
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            if (heightMapLayer.Shapes.Count == 0)
            {
                return;
            }

            MapHeightMap? heightMap = (MapHeightMap)heightMapLayer.Shapes[0];

            renderCanvas.DrawRect(new SKRect(1, 1, map.MapWidth, map.MapHeight), PaintObjects.LandformAreaSelectPaint);

            SKPathBuilder pathBuilder = new();

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

            if (heightMapViewModel.ShowContourLines)
            {
                using SKPaint ContourPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = heightMapViewModel.LineColor.ToSKColor(),
                    StrokeWidth = heightMapViewModel.ContourLineWidth,
                    IsAntialias = true
                };

                using SKPaint MajorContourPaint = new()
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
                    ContourPaint,
                    MajorContourPaint);
            }

            if (selectedArea != null)
            {
                renderCanvas.DrawRect((SKRect)selectedArea, PaintObjects.LandformAreaSelectPaint);
            }
        }
        */
    }
}
