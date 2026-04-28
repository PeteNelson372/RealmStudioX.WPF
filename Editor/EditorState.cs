using RealmStudioShapeRenderingLib;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealmStudioX.WPF.Editor
{
    public class EditorState : INotifyPropertyChanged
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

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
