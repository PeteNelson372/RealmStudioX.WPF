using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class MeasureTool(
            CommandManager commands,
            IAssetProvider assets,
            MapLayer targetLayer,
            MapScene scene,
            EditorState editorState,
            FontManager fontManager,
            IRedrawRequester redraw,
            IMeasureSettings settings) : IToolEditor, IDisposable
    {

        private bool disposedValue;
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private readonly FontManager _fontManager = fontManager;
        private readonly IRedrawRequester _redraw = redraw;
        private readonly IMeasureSettings _settings = settings;

        private MapMeasure? _currentMeasure = null;
        private SKPoint _prevMouseWorldPoint;

        public void Activate()
        {

        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseDown(PointerState state)
        {
            if (state.Button is EditorMouseButton.Left && _editorState.CurrentDrawingMode == MapDrawingMode.DrawMapMeasure)
            {
                if (_currentMeasure == null)
                {
                    _currentMeasure = new()
                    {
                        MapAreaUnits = _scene.Map.MapAreaUnits,
                        MapPixelWidth = _scene.Map.MapAreaWidth / _scene.Map.MapWidth,
                        MapPixelHeight = _scene.Map.MapAreaHeight / _scene.Map.MapHeight,
                        MeasureArea = _settings.MeasureArea,
                        MeasureLineColor = _settings.MeasureColor.ToSKColor(),
                        UseMapUnits = _settings.UseScaleUnits,
                    };

                    MapLayer measureLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.MEASURELAYER);
                    measureLayer.Add(_currentMeasure);
                }

                _currentMeasure.MeasurePoints.Add(state.WorldPoint);
                _prevMouseWorldPoint = state.WorldPoint;
            }

            if (state.Button is EditorMouseButton.Right)
            {
                if (_currentMeasure != null)
                {
                    _editorState.CurrentDrawingMode = MapDrawingMode.None;

                    _currentMeasure.MeasurePoints.Add(state.WorldPoint);

                    float lineLength = SKPoint.Distance(_prevMouseWorldPoint, state.WorldPoint);
                    _currentMeasure.TotalMeasureLength += lineLength;
                    _currentMeasure.RenderValue = true;

                    _currentMeasure = null;
                }

            }

            _redraw.RequestRedraw();
        }

        public void OnMouseMove(PointerState state)
        {
        }

        public void OnMouseUp(PointerState state)
        {
            if (_currentMeasure != null)
            {
                if (!_currentMeasure.MeasurePoints.Contains(_prevMouseWorldPoint))
                {
                    _currentMeasure.MeasurePoints.Add(_prevMouseWorldPoint);
                }

                _currentMeasure.MeasurePoints.Add(state.WorldPoint);

                float lineLength = SKPoint.Distance(_prevMouseWorldPoint, state.WorldPoint);
                _currentMeasure.TotalMeasureLength += lineLength;
            }

            _prevMouseWorldPoint = state.WorldPoint;
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_currentMeasure == null)
            {
                return;
            }

            try
            {
                if (_currentMeasure.MeasureArea && _currentMeasure.MeasurePoints.Count > 1)
                {
                    SKPathBuilder builder = new();

                    builder.MoveTo(_currentMeasure.MeasurePoints.First());

                    for (int i = 1; i < _currentMeasure.MeasurePoints.Count; i++)
                    {
                        builder.LineTo(_currentMeasure.MeasurePoints[i]);
                    }

                    builder.LineTo(world);
                    builder.Close();

                    var path = builder.Snapshot();
                    builder.Detach();
                    builder.Dispose();

                    canvas.DrawPath(path, _currentMeasure.MeasureAreaPaint);
                }
                else
                {
                    canvas.DrawLine(_prevMouseWorldPoint, world, _currentMeasure.MeasureLinePaint);
                }

                // render measure value and units
                SKPoint measureValuePoint = new(world.X + 30, world.Y + 20);
                float lineLength = SKPoint.Distance(_prevMouseWorldPoint, world);
                float totalLength = _currentMeasure.TotalMeasureLength + lineLength;

                _currentMeasure.RenderDistanceLabel(canvas, measureValuePoint, totalLength);

                if (_currentMeasure.MeasureArea && _currentMeasure.MeasurePoints.Count > 1)
                {
                    // temporarily add the point at the mouse position
                    _currentMeasure.MeasurePoints.Add(world);

                    // calculate the polygon area
                    float area = RealmStudioShapeRenderingLib.Utilities.CalculatePolygonArea(_currentMeasure.MeasurePoints);

                    // remove the temporarily added point
                    _currentMeasure.MeasurePoints.RemoveAt(_currentMeasure.MeasurePoints.Count - 1);

                    // display the area label
                    SKPoint measureAreaPoint = new(world.X + 30, world.Y + 40);

                    _currentMeasure.RenderAreaLabel(canvas, measureAreaPoint, area);
                }
            }
            catch { }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WindroseTool()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
