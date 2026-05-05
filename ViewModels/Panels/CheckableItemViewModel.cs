using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealmStudioX.WPF.ViewModels.Panels
{
    public class CheckableItemViewModel<T> : INotifyPropertyChanged
    {
        public T Value { get; }

        private readonly Func<T, string> _displaySelector;

        public string Name => _displaySelector(Value);

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public CheckableItemViewModel(T value, Func<T, string>? displaySelector = null)
        {
            Value = value;
            _displaySelector = displaySelector ?? (v => v?.ToString() ?? string.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
