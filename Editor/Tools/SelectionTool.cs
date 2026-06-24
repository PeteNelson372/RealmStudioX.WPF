using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.Services;
using SkiaSharp;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class SelectionTool : IToolEditor, IDisposable
    {
        private EditorController _editor;
        private SelectionService _selectionService;

        public SelectionTool(EditorController editor, SelectionService selectionService)
        {
            _editor = editor;
            _selectionService = selectionService;
        }

        private bool disposedValue;

        public void Activate()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(PointerState state)
        {
            if (_editor.Scene != null)
            {
                _selectionService.SelectAt(_editor.Scene.Map, state.WorldPoint, 4, state.Modifiers == InputModifiers.Shift);
            }
        }

        public void OnMouseMove(PointerState state)
        {

        }

        public void OnMouseUp(PointerState state)
        {

        }

        public void OnMouseDoubleClick(PointerState state)
        {

        }

        public void OnMouseWheel(PointerState state)
        {

        }

        public void Cancel()
        {

        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            // no op
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
        // ~SelectionTool()
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
