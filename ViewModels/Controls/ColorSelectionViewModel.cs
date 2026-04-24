using RealmStudioX.WPF.Editor.UserInterface;
using System.ComponentModel;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class ColorSelectionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isUpdating;

        // Core color (single source of truth)
        private Color _currentColor = Colors.White;
        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                if (_currentColor == value) return;

                _currentColor = value;
                OnPropertyChanged(nameof(CurrentColor));
                OnPropertyChanged(nameof(CurrentBrush));

                UpdateFromColor();
            }
        }

        public SolidColorBrush CurrentBrush => new SolidColorBrush(CurrentColor);

        // RGB
        private byte _r, _g, _b;

        public byte R
        {
            get => _r;
            set
            {
                if (_r == value) return;
                _r = value;
                OnPropertyChanged(nameof(R));
                UpdateFromRgb();
            }
        }

        public byte G
        {
            get => _g;
            set
            {
                if (_g == value) return;
                _g = value;
                OnPropertyChanged(nameof(G));
                UpdateFromRgb();
            }
        }

        public byte B
        {
            get => _b;
            set
            {
                if (_b == value) return;
                _b = value;
                OnPropertyChanged(nameof(B));
                UpdateFromRgb();
            }
        }

        private byte _a = 255;

        public byte A
        {
            get => _a;
            set
            {
                if (_a == value) return;

                _a = value;
                OnPropertyChanged(nameof(A));
                UpdateFromArgb();
            }
        }

        private void UpdateFromArgb()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            CurrentColor = Color.FromArgb(_a, _r, _g, _b);

            _isUpdating = false;
        }

        // HEX
        private string _hex = "#FFFFFF";
        public string Hex
        {
            get => _hex;
            set
            {
                if (_hex == value) return;
                _hex = value;
                OnPropertyChanged(nameof(Hex));
                UpdateFromHex();
            }
        }

        // HSL
        private double _h, _s, _l;

        public double H
        {
            get => _h;
            set
            {
                if (Math.Abs(_h - value) < 0.001) return;
                _h = value;
                OnPropertyChanged(nameof(H));
                UpdateFromHsl();
            }
        }

        public double S
        {
            get => _s * 100.0;
            set
            {
                double newValue = value / 100.0;
                if (Math.Abs(_s - newValue) < 0.001) return;

                _s = newValue;
                OnPropertyChanged(nameof(S));
                UpdateFromHsl();
            }
        }

        public double L
        {
            get => _l * 100.0;
            set
            {
                double newValue = value / 100.0;
                if (Math.Abs(_l - newValue) < 0.001) return;

                _l = newValue;
                OnPropertyChanged(nameof(L));
                UpdateFromHsl();
            }
        }

        // =========================
        // Update flows
        // =========================

        private void UpdateFromColor()
        {
            _a = CurrentColor.A;
            _r = CurrentColor.R;
            _g = CurrentColor.G;
            _b = CurrentColor.B;

            (double h, double s, double l) = ColorHelper.RgbToHsl(CurrentColor);

            _h = h;
            _s = s;
            _l = l;

            _hex = $"#{_a:X2}{_r:X2}{_g:X2}{_b:X2}";

            NotifyAll();
        }

        private void UpdateFromRgb()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            CurrentColor = Color.FromRgb(_r, _g, _b);

            _isUpdating = false;
        }

        private void UpdateFromHex()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            if (ColorHelper.TryParseHex(_hex, out var color))
            {
                CurrentColor = color;
            }

            _isUpdating = false;
        }

        private void UpdateFromHsl()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            CurrentColor = ColorHelper.HslToRgb(_h, _s, _l);

            _isUpdating = false;
        }

        private void NotifyAll()
        {
            OnPropertyChanged(nameof(A));
            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));
            OnPropertyChanged(nameof(H));
            OnPropertyChanged(nameof(S));
            OnPropertyChanged(nameof(L));
            OnPropertyChanged(nameof(Hex));
            OnPropertyChanged(nameof(CurrentBrush));
        }
    }
}
