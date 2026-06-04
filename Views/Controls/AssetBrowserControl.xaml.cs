using System.Windows;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for AssetBrowserControl.xaml
    /// </summary>
    public partial class AssetBrowserControl : System.Windows.Controls.UserControl
    {
        public AssetBrowserControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PreviewScaleProperty =
            DependencyProperty.Register(
                nameof(PreviewScale),
                typeof(float),
                typeof(AssetBrowserControl),
                new PropertyMetadata(1.0f));

        public float PreviewScale
        {
            get => (float)GetValue(PreviewScaleProperty);
            set => SetValue(PreviewScaleProperty, value);
        }

        public static readonly DependencyProperty PreviewRotationProperty =
            DependencyProperty.Register(
                nameof(PreviewRotation),
                typeof(float),
                typeof(AssetBrowserControl),
                new PropertyMetadata(0.0f));

        public float PreviewRotation
        {
            get => (float)GetValue(PreviewRotationProperty);
            set => SetValue(PreviewRotationProperty, value);
        }

        public static readonly DependencyProperty PreviewOpacityProperty =
            DependencyProperty.Register(
                nameof(PreviewOpacity),
                typeof(float),
                typeof(AssetBrowserControl),
                new PropertyMetadata(1.0f));

        public float PreviewOpacity
        {
            get => (float)GetValue(PreviewOpacityProperty);
            set => SetValue(PreviewOpacityProperty, value);
        }
    }
}
