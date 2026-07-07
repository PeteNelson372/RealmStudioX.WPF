using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace RealmStudioX.WPF.Editor.Services
{
    public class PaintService
    {
        private readonly EditorController _editor;
        private readonly AssetManager _assetManager;
        private readonly CommandService _commands;

        public ObservableCollection<BrushPatternItem> BrushPatterns { get; } = [];

        public PaintSettings Settings { get; } = new();

        private long _lastPaintTimestamp;
        private PaintedLine? _currentPaintedLine = null;
        private DrawingErase? _currentDrawingErase = null;

        private int _mapWidth;
        private int _mapHeight;

        public PaintService(AssetManager assetManager, EditorController editor, CommandService commands)
        {
            _assetManager = assetManager;
            _editor = editor;
            _commands = commands;

            BuildBrushPatterns();
        }

        public void BeginStroke(SKPoint strokePoint)
        {
            if (Settings.SelectedBrush == null 
                || Settings.SelectedBrushPattern == null
                || Settings.SelectedBrushPattern.BrushDefinition == null
                || !Settings.IsBrushReady)
            {
                return;
            }

            _currentPaintedLine = new PaintedLine
            {
                Brush = Settings.SelectedBrush,
                DefaultSpacing = Settings.SelectedBrushPattern.BrushDefinition.BrushSpacing,
                BrushSpacing = Settings.BrushSpacing,
                RandomRotation = Settings.SelectedBrushPattern.BrushDefinition.RandomRotation,
            };

            _currentPaintedLine.Initialize(_mapWidth, _mapHeight);

            if (_editor.ActiveDrawingLayer != null && _editor.ActiveDrawingLayer.MapLayerOrder == MapBuilder.LANDDRAWINGLAYER)
            {
                // build the clip path for the painted line to ensure it doesn't go outside the land area
                _currentPaintedLine.RequiresLandformClipping = true;
            }

            if (_editor.ActiveDrawingLayer != null && _editor.ActiveDrawingLayer.MapLayerOrder == MapBuilder.WATERDRAWINGLAYER)
            {
                // build the clip path for the painted line to ensure it doesn't go outside the water systems
                _currentPaintedLine.RequiresWaterSystemClipping = true;
            }


            long now = Environment.TickCount64;

            _lastPaintTimestamp = now;

            _currentPaintedLine.AddPoint(strokePoint);
        }
    
        public void ContinueStroke(SKPoint strokePoint)
        {
            if (_currentPaintedLine != null)
            {
                long now = Environment.TickCount64;

                float deltaTime = (now - _lastPaintTimestamp) / 1000f;

                _lastPaintTimestamp = now;

                _currentPaintedLine.AddPoint(strokePoint);
            }
        }
    
        public void EndStroke(SKPoint strokePoint)
        {
            if (_currentPaintedLine != null)
            {
                if (strokePoint != _currentPaintedLine.Points[^1])
                {
                    _currentPaintedLine.AddPoint(strokePoint);
                }

                _currentPaintedLine.FinalizeStroke();

                if (_editor.ActiveDrawingLayer != null)
                {
                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentPaintedLine);

                    _commands.ActiveCommands.Execute(cmd);
                }

                _editor.ActiveDrawingLayer?.InvalidateAllTiles();
                _currentPaintedLine = null;

                _editor.RequestRedraw();
            }
        }

        public void Reset()
        {
            _currentPaintedLine?.Dispose();
            _currentPaintedLine = null;
        }

        public void SetMapDimensions(int width, int height)
        {
            _mapWidth = width;
            _mapHeight = height;
        }


        public void RenderCurrentLine(SKCanvas canvas)
        {
            if (_currentPaintedLine != null)
            {
                SKPath? clipPath = null;

                if (_currentPaintedLine.RequiresLandformClipping)
                {
                    clipPath = _editor.Scene!.GetLandClipPath();
                }

                if ( _currentPaintedLine.RequiresWaterSystemClipping)
                {
                    clipPath = _editor.Scene!.GetWaterSystemClipPath();
                }

                _currentPaintedLine.Render(canvas, null, clipPath);
            }
        }

        private void BuildBrushPatterns()
        {
            List<MapBrush> brushes = _assetManager.MapBrushes;

            foreach (MapBrush brush in brushes)
            {
                var bpi = CreateBrushPattern(brush);

                if (bpi != null)
                {
                    BrushPatterns.Add(bpi);
                }
            }

            Settings.SelectedBrushPattern = BrushPatterns.FirstOrDefault(b => string.Equals(
                b.Name,
                "Soft Round",
                StringComparison.OrdinalIgnoreCase));
        }

        private BrushPatternItem? CreateBrushPattern(MapBrush brush)
        {
            if (brush.BrushBitmaps != null && brush.BrushBitmaps.Count > 0)
            {
                return new BrushPatternItem
                {
                    Name = brush.BrushName,

                    BrushDefinition = brush,

                    PreviewImage = brush.BrushBitmaps[0]?.Copy().ToImageSource()
                };
            }

            return null;
        }

        internal void BeginErase(SKPoint worldPoint)
        {
            _currentDrawingErase = new DrawingErase
            {
                BrushSize = Settings.BrushSize,
            };

            _currentDrawingErase.AddPoint(worldPoint);

            if (_editor.ActiveDrawingLayer != null)
            {
                Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawingErase);
                _commands.ActiveCommands.Execute(cmd);
            }
        }

        internal void ContinueErase(SKPoint worldPoint)
        {
            if (_currentDrawingErase != null)
            {
                SKRect oldEraseBounds = _currentDrawingErase.Bounds;

                _currentDrawingErase.AddPoint(worldPoint);

                SKRect newEraseBounds = _currentDrawingErase.Bounds;

                if (_editor.ActiveDrawingLayer != null)
                {
                    _editor.ActiveDrawingLayer.UpdateShapeTiles(_currentDrawingErase, oldEraseBounds, newEraseBounds);
                    _editor.ActiveDrawingLayer.InvalidateAllTiles();
                }
            }
        }

        internal void EndErase(SKPoint worldPoint)
        {
            _currentDrawingErase = null;
        }
    }


}
