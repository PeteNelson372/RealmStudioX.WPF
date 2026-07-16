using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Controls;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for FontSelectionControl.xaml
    /// </summary>
    public partial class FontSelectionControl : UserControl
    {
        private SKGLControl _skExampleTextControl;

        private FontSelectionViewModel ViewModel => (FontSelectionViewModel)DataContext;

        public FontSelectionControl()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;

            _skExampleTextControl = new SKGLControl
            {
                Dock = System.Windows.Forms.DockStyle.Fill
            };

            _skExampleTextControl.PaintSurface += OnExampleTextPaintSurface;
            ExampleTextFormsHost.Child = _skExampleTextControl;

            _skExampleTextControl.Visible = true;

            Loaded += (_, _) =>
            {
                ViewModel?.RefreshFontSelection();
                _skExampleTextControl.Refresh();
            };
        }

        private void OnExampleTextPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
        {
            if (ViewModel == null)
                return;

            if (ViewModel.SelectedFontFamily == null)
                return;

            if (string.IsNullOrWhiteSpace(ViewModel.SelectedFontFamily))
                return;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.WhiteSmoke);

            FontDecorations decorations = FontDecorations.None;

            if (ViewModel.IsUnderline)
                decorations |= FontDecorations.Underline;

            if (ViewModel.IsSuperscript)
                decorations |= FontDecorations.Superscript;

            if (ViewModel.IsSubscript)
                decorations |= FontDecorations.Subscript;

            FontStyleModel fm = new()
            {
                Family = ViewModel.SelectedFontFamily,
                Size = ViewModel.SelectedFontSize,
                Bold = ViewModel.IsBold,
                Italic = ViewModel.IsItalic,
                Decorations = decorations
            };

            var typeface = ViewModel.FontManager.GetTypeface(fm);

            using var font = new SKFont(typeface, fm.Size);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black
            };

            string text = "The quick brown fox jumps over the lazy dog";

            // Measure
            var bounds = new SKRect();
            font.MeasureText(text, out bounds);

            float x = (e.Info.Width - bounds.Width) / 2 - bounds.Left;
            float y = (e.Info.Height - bounds.Height) / 2 - bounds.Top;

            canvas.Save();

            // Draw text
            canvas.DrawText(text, 10, y, SKTextAlign.Left, font, paint);

            // Underline (manual)
            if (fm.Decorations.HasFlag(FontDecorations.Underline))
            {
                float underlineY = y + font.Metrics.Descent * 0.5f;

                canvas.DrawLine(
                    x,
                    underlineY,
                    x + bounds.Width,
                    underlineY,
                    paint);
            }

            canvas.Restore();
        }

        private void OnDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FontSelectionViewModel.SelectedFontFamily):
                case nameof(FontSelectionViewModel.SelectedFontSize):
                case nameof(FontSelectionViewModel.IsBold):
                case nameof(FontSelectionViewModel.IsItalic):
                case nameof(FontSelectionViewModel.IsUnderline):
                case nameof(FontSelectionViewModel.IsSuperscript):
                case nameof(FontSelectionViewModel.IsSubscript):

                    _skExampleTextControl?.Refresh();
                    break;
            }
        }
    }
}
