
using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;
using System.Windows.Input;
using Color = System.Windows.Media.Color;
using Cursor = System.Windows.Input.Cursor;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace RealmStudioX.WPF.Views
{
    public partial class EyedropperOverlayWindow : Window
    {
        public event Action<Color>? ColorPicked;
        public event Action? Cancelled;

        private Cursor _cursor;

        public EyedropperOverlayWindow(Cursor cursor)
        {
            InitializeComponent();

            _cursor = cursor;
            this.Cursor = _cursor;

            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            KeyDown += OnKeyDown;

            Focusable = true;
            Loaded += (_, __) => Keyboard.Focus(this);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Optional: live preview later
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = System.Windows.Forms.Control.MousePosition;

            var color = ColorHelper.GetScreenColor(p.X, p.Y);

            ColorPicked?.Invoke(color);

            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancelled?.Invoke();
                Close();
            }
        }
    }
}