using RealmStudioX.WPF.Editor.UserInterface;
using System.ComponentModel;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

public class ColorSelectionViewModel : INotifyPropertyChanged
{
    private bool _isUpdating;
    private Color _currentColor;

    private double _h; // 0–360
    private double _s; // 0–1
    private double _v; // 0–1

    public bool IsEditingHex { get; set; }
    public bool IsEditingRgb { get; set; }
    public bool IsEditingHsv { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // =========================
    // CURRENT COLOR (SOURCE OF TRUTH)
    // =========================

    public Color CurrentColor
    {
        get => _currentColor;
        set
        {
            if (_currentColor == value) return;

            _currentColor = value;
            OnPropertyChanged(nameof(CurrentColor));

            UpdateFromColor();
        }
    }

    public SolidColorBrush CurrentBrush => new SolidColorBrush(_currentColor);

    // =========================
    // RGB (DERIVED + EDITABLE)
    // =========================

    // =========================
    // RGB INPUT (TEXTBOXES)
    // =========================

    private string _rInput = "";
    public string RInput
    {
        get => _rInput;
        set { _rInput = value; OnPropertyChanged(nameof(RInput)); }
    }

    private string _gInput = "";
    public string GInput
    {
        get => _gInput;
        set { _gInput = value; OnPropertyChanged(nameof(GInput)); }
    }

    private string _bInput = "";
    public string BInput
    {
        get => _bInput;
        set { _bInput = value; OnPropertyChanged(nameof(BInput)); }
    }

    private string _aInput = "";
    public string AInput
    {
        get => _aInput;
        set { _aInput = value; OnPropertyChanged(nameof(AInput)); }
    }

    public byte R
    {
        get => _currentColor.R;
        set => SetRgb(value, _currentColor.G, _currentColor.B);
    }

    public byte G
    {
        get => _currentColor.G;
        set => SetRgb(_currentColor.R, value, _currentColor.B);
    }

    public byte B
    {
        get => _currentColor.B;
        set => SetRgb(_currentColor.R, _currentColor.G, value);
    }

    public byte A
    {
        get => _currentColor.A;
        set => SetArgb(value, _currentColor.R, _currentColor.G, _currentColor.B);
    }

    private void SetRgb(byte r, byte g, byte b)
    {
        CurrentColor = Color.FromArgb(_currentColor.A, r, g, b);
    }

    private void SetArgb(byte a, byte r, byte g, byte b)
    {
        CurrentColor = Color.FromArgb(a, r, g, b);
    }

    public void CommitRgb()
    {
        if (byte.TryParse(RInput, out var r) &&
            byte.TryParse(GInput, out var g) &&
            byte.TryParse(BInput, out var b) &&
            byte.TryParse(AInput, out var a))
        {
            CurrentColor = Color.FromArgb(a, r, g, b);
        }
        else
        {
            // revert
            RInput = R.ToString();
            GInput = G.ToString();
            BInput = B.ToString();
            AInput = A.ToString();
        }

        IsEditingRgb = false;
    }

    // =========================
    // HSV (PRIMARY INPUT)
    // =========================

    // =========================
    // HSV INPUT (TEXTBOXES)
    // =========================

    private string _hInput = "";
    public string HInput
    {
        get => _hInput;
        set { _hInput = value; OnPropertyChanged(nameof(HInput)); }
    }

    private string _sInput = "";
    public string SInput
    {
        get => _sInput;
        set { _sInput = value; OnPropertyChanged(nameof(SInput)); }
    }

    private string _vInput = "";
    public string VInput
    {
        get => _vInput;
        set { _vInput = value; OnPropertyChanged(nameof(VInput)); }
    }

    public double H
    {
        get => _h;
        set
        {
            if (Math.Abs(_h - value) < 0.1) return;

            _h = value;
            OnPropertyChanged(nameof(H));

            UpdateFromHsv();
        }
    }

    public double S
    {
        get => _s;
        set
        {
            if (Math.Abs(_s - value) < 0.0001) return;

            _s = value;
            OnPropertyChanged(nameof(S));
            OnPropertyChanged(nameof(SPercent));

            UpdateFromHsv();
        }
    }

    public double V
    {
        get => _v;
        set
        {
            if (Math.Abs(_v - value) < 0.0001) return;

            _v = value;
            OnPropertyChanged(nameof(V));
            OnPropertyChanged(nameof(VPercent));

            UpdateFromHsv();
        }
    }

    public double SPercent
    {
        get => _s * 100.0;
        set => S = value / 100.0;
    }

    public double VPercent
    {
        get => _v * 100.0;
        set => V = value / 100.0;
    }

    public void CommitHsv()
    {
        if (double.TryParse(HInput, out var h) &&
            double.TryParse(SInput, out var s) &&
            double.TryParse(VInput, out var v))
        {
            H = Math.Clamp(h, 0, 360);
            S = Math.Clamp(s / 100.0, 0, 1);
            V = Math.Clamp(v / 100.0, 0, 1);
        }
        else
        {
            // revert
            HInput = H.ToString("F1");
            SInput = (S * 100).ToString("F1");
            VInput = (V * 100).ToString("F1");
        }

        IsEditingHsv = false;
    }

    // =========================
    // ALPHA (UI FRIENDLY)
    // =========================

    public double APercent
    {
        get => _currentColor.A / 255.0 * 100.0;
        set
        {
            byte a = (byte)(value / 100.0 * 255.0);
            A = a;
            OnPropertyChanged(nameof(APercent));
        }
    }

    // =========================
    // HEX
    // =========================

    public string Hex
    {
        get => $"#{_currentColor.A:X2}{_currentColor.R:X2}{_currentColor.G:X2}{_currentColor.B:X2}";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                if (!value.StartsWith("#"))
                {
                    value = "#" + value;
                }

                var color = (Color)ColorConverter.ConvertFromString(value);
                CurrentColor = color;
            }
            catch
            {
                // Ignore invalid input (do NOT overwrite textbox)
            }
        }
    }

    private string _hexInput = "";

    public string HexInput
    {
        get => _hexInput;
        set
        {
            _hexInput = value;
            OnPropertyChanged(nameof(HexInput));
        }
    }

    public void CommitHex()
    {
        if (string.IsNullOrWhiteSpace(_hexInput))
        {
            HexInput = FormatCurrentColor();
            IsEditingHex = false;
            return;
        }

        if (TryParseHex(_hexInput, out var color))
        {
            CurrentColor = color;
            HexInput = FormatCurrentColor();
        }
        else
        {
            // revert on invalid input
            HexInput = FormatCurrentColor();
        }

        IsEditingHex = false;
    }

    private static bool TryParseHex(string input, out Color color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string hex = input.Trim();

        // Remove leading '#'
        if (hex.StartsWith('#'))
            hex = hex.Substring(1);

        // Expand shorthand
        if (hex.Length == 3) // RGB
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
            hex = "FF" + hex; // assume full alpha
        }
        else if (hex.Length == 4) // ARGB
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }
        else if (hex.Length == 6) // RRGGBB
        {
            hex = "FF" + hex;
        }
        else if (hex.Length != 8)
        {
            return false;
        }

        // Now must be AARRGGBB
        if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
            return false;

        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);

        color = Color.FromArgb(a, r, g, b);
        return true;
    }

    private string FormatCurrentColor()
    {
        return $"#{_currentColor.A:X2}{_currentColor.R:X2}{_currentColor.G:X2}{_currentColor.B:X2}";
    }

    // =========================
    // CONVERSIONS
    // =========================

    private void UpdateFromHsv()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        // Preserve alpha
        var rgb = ColorHelper.HsvToRgb(_h, _s, _v);
        _currentColor = Color.FromArgb(_currentColor.A, rgb.R, rgb.G, rgb.B);

        NotifyColorDependents();

        _isUpdating = false;
    }

    private void UpdateFromColor()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        var (h, s, v) = ColorHelper.RgbToHsv(_currentColor);

        _h = h;
        _s = s;
        _v = v;

        OnPropertyChanged(nameof(H));
        OnPropertyChanged(nameof(S));
        OnPropertyChanged(nameof(V));
        OnPropertyChanged(nameof(SPercent));
        OnPropertyChanged(nameof(VPercent));

        NotifyColorDependents();

        _isUpdating = false;
    }

    private void NotifyColorDependents()
    {
        OnPropertyChanged(nameof(CurrentColor));
        OnPropertyChanged(nameof(CurrentBrush));
        OnPropertyChanged(nameof(R));
        OnPropertyChanged(nameof(G));
        OnPropertyChanged(nameof(B));
        OnPropertyChanged(nameof(A));
        OnPropertyChanged(nameof(APercent));
        OnPropertyChanged(nameof(Hex));

        // HEX
        if (!IsEditingHex)
        {
            _hexInput = FormatCurrentColor();
            OnPropertyChanged(nameof(HexInput));
        }

        // RGB
        if (!IsEditingRgb)
        {
            _rInput = R.ToString();
            _gInput = G.ToString();
            _bInput = B.ToString();
            _aInput = A.ToString();

            OnPropertyChanged(nameof(RInput));
            OnPropertyChanged(nameof(GInput));
            OnPropertyChanged(nameof(BInput));
            OnPropertyChanged(nameof(AInput));
        }

        // HSV
        if (!IsEditingHsv)
        {
            _hInput = H.ToString("F1");
            _sInput = (S * 100).ToString("F1");
            _vInput = (V * 100).ToString("F1");

            OnPropertyChanged(nameof(HInput));
            OnPropertyChanged(nameof(SInput));
            OnPropertyChanged(nameof(VInput));
        }
    }
}