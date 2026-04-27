using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Editor
{
    public class EditorState
    {
        private MapDrawingMode _currentDrawingMode;

        public MapDrawingMode CurrentDrawingMode
        {
            get => _currentDrawingMode;
            set
            {
                if (_currentDrawingMode == value)
                    return;

                var previous = _currentDrawingMode;
                _currentDrawingMode = value;

                DrawingModeChanged?.Invoke(previous, _currentDrawingMode);
            }
        }

        public event Action<MapDrawingMode, MapDrawingMode>? DrawingModeChanged;
    }
}
