using RealmStudioX.WPF.Editor.UserInterface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Controls
{
    public partial class CompactPaletteControl : UserControl
    {
        public CompactPaletteControl()
        {
            InitializeComponent();

            var colors = new List<SolidColorBrush>();

            colors.AddRange(ColorPaletteGenerator.GenerateQuickRow());
            colors.AddRange(ColorPaletteGenerator.GenerateGrayScaleRow());
            colors.AddRange(ColorPaletteGenerator.GenerateCompactPalette());

            Colors = colors;
        }

        // Colors collection
        public List<SolidColorBrush> Colors
        {
            get => (List<SolidColorBrush>)GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        public static readonly DependencyProperty ColorsProperty =
            DependencyProperty.Register(nameof(Colors),
                typeof(List<SolidColorBrush>),
                typeof(CompactPaletteControl),
                new PropertyMetadata(null));

        // SelectedColor (bindable)
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor),
                typeof(Color),
                typeof(CompactPaletteControl),
                new FrameworkPropertyMetadata(default(Color),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // Event (optional but useful)
        public event Action<Color>? ColorSelected;

        private void Color_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.Background is SolidColorBrush brush)
            {
                border.Focus();
                SelectedColor = brush.Color;
                ColorSelected?.Invoke(brush.Color);
            }
        }
    }
}
