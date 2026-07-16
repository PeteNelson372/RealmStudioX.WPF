using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    public class LayoutPathTool(EditorController editor) : IToolEditor, IDisposable
    {
        private readonly EditorController _editor = editor;
        private readonly List<SKPoint> _layoutPathPoints = [];
        private SKPoint _firstWorldPoint;

        private SKPath? _layoutPath;

        public SKPath? LayoutPath
        {
            get { return _layoutPath; }
            set { _layoutPath = value; }
        }

        private bool disposedValue;


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

        }

        public void OnMouseDown(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editor.CurrentDrawingMode == MapDrawingMode.DrawArcLayoutPath)
                {
                    _layoutPathPoints.Clear();

                    _firstWorldPoint = state.WorldPoint;
                    _layoutPath = new();

                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.DrawFreeformLayoutPath)
                {
                    _layoutPathPoints.Clear();
                    _layoutPathPoints.Add(state.WorldPoint);

                    _layoutPath = new();
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editor.CurrentDrawingMode == MapDrawingMode.DrawArcLayoutPath)
                {
                    _layoutPath?.Reset();
                    _layoutPath = CreateNewArcPath(state.WorldPoint, _firstWorldPoint);

                    _editor.RequestRedraw();
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.DrawFreeformLayoutPath)
                {

                    _layoutPathPoints.Add(state.WorldPoint);

                    _layoutPath?.Reset();
                    _layoutPath = Utilities.BuildPath2(_layoutPathPoints);

                    _editor.RequestRedraw();
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editor.CurrentDrawingMode == MapDrawingMode.DrawArcLayoutPath)
                {
                    _layoutPath?.Reset();
                    _layoutPath = CreateNewArcPath(state.WorldPoint, _firstWorldPoint);

                    _editor.RequestRedraw();
                }
                else if (_editor.CurrentDrawingMode == MapDrawingMode.DrawFreeformLayoutPath)
                {
                    _layoutPathPoints.Add(state.WorldPoint);

                    _layoutPath?.Reset();
                    _layoutPath = Utilities.BuildPath2(_layoutPathPoints);

                    _editor.RequestRedraw();
                }
            }
        }

        public void OnMouseWheel(PointerState state)
        {

        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
        }

        internal static SKPath CreateNewArcPath(SKPoint currentWorldPoint, SKPoint previousWorldPoint)
        {
            SKPathBuilder arcPathBuilder = new();

            if (currentWorldPoint.Y > previousWorldPoint.Y)
            {
                // start on the left and drag right and down to draw an arc downward (open part of the arc facing down)
                SKRect r = new(previousWorldPoint.X, previousWorldPoint.Y, currentWorldPoint.X, currentWorldPoint.Y);
                arcPathBuilder.AddArc(r, 180, 180);
            }
            else
            {
                // start on the right and drag left and up to draw an arc upward (open part of the arc facing up)
                SKRect r = new(currentWorldPoint.X, currentWorldPoint.Y, previousWorldPoint.X, previousWorldPoint.Y);
                arcPathBuilder.AddArc(r, 180, 180);
            }

            var newArcPath = arcPathBuilder.Snapshot();
            arcPathBuilder.Detach();
            arcPathBuilder.Dispose();

            return newArcPath;
        }

        public void ClearLayoutPath()
        {
            _layoutPath?.Dispose();
            _layoutPath = null;
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
        // ~AlignmentPathTool()
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
