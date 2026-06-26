using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Controls;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for PixelEditDialog.xaml
    /// </summary>
    public partial class PixelEditDialog : ModalDialog
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public PixelEditDialog()
        {
            InitializeComponent();
        }

        bool _isSelectingColor = false;
        bool _isDrawing = false;

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void SelectPixelColorButton_Click(object sender, RoutedEventArgs e)
        {
            _isSelectingColor = true;
            Cursor = Cursors.Cross;
        }

        private void PixelEditorImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            if (_isSelectingColor)
            {
                Point p = e.GetPosition(PixelEditorImageCtrl);
                var (x, y) = GetPixelCoordinate(p);

                SKColor currentColor = vm.WorkingBitmap.GetPixel(x, y);
                vm.PixelColor = currentColor.ToColor();

                _isSelectingColor = false;
                Cursor = Cursors.Arrow;
            }
            else
            {
                _isDrawing = true;

                PixelEditorImageCtrl.CaptureMouse();

                EditPixel(e.GetPosition(PixelEditorImageCtrl));
            }
        }

        private void PixelEditorImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(PixelEditorImageCtrl);

            UpdateHoverPixel(p);

            if (_isDrawing)
            {
                EditPixel(p);
            }
        }

        private void PixelEditorImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;

            PixelEditorImageCtrl.ReleaseMouseCapture();
        }

        private (int x, int y) GetPixelCoordinate(Point p)
        {
            double scaleX = PixelEditorImageCtrl.ActualWidth / 32.0;

            double scaleY = PixelEditorImageCtrl.ActualHeight / 32.0;

            int pixelX = (int)(p.X / scaleX);
            int pixelY = (int)(p.Y / scaleY);

            pixelX = Math.Clamp(pixelX, 0, 31);
            pixelY = Math.Clamp(pixelY, 0, 31);

            return (pixelX, pixelY);
        }

        private void EditPixel(Point mousePosition)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            var (x, y) = GetPixelCoordinate(mousePosition);

            SetPixel(x, y);
        }

        public void SetPixel(int x, int y)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            SKColor currentColor = vm.WorkingBitmap.GetPixel(x, y);

            vm.WorkingBitmap.SetPixel(x, y, vm.PixelColor.ToSKColor());

            SKPoint editPoint = new(x + vm.EditLocation.X, y + vm.EditLocation.Y);

            PixelEdit pixelEdit = new()
            {
                Location = editPoint,
                OriginalColor = currentColor,
                NewColor = vm.PixelColor.ToSKColor(),
            };

            vm.PixelEdits.Add(pixelEdit);

            PixelEditorImageCtrl.Source = vm.WorkingBitmap.ToImageSource();
        }

        private void UpdateHoverPixel(Point p)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            var (x, y) = GetPixelCoordinate(p);

            vm.HoverPixelText = $"{x}, {y}";

            SKColor color = vm.WorkingBitmap.GetPixel(x, y);

            vm.OriginalColorText = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}{color.Alpha:X2}";

            vm.CurrentColorText = $"#{vm.PixelColor.R:X2}{vm.PixelColor.G:X2}{vm.PixelColor.B:X2}{vm.PixelColor.A:X2}";
        }

        private void ClearChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            for (int i = vm.PixelEdits.Count - 1; i >= 0; i--)
            {
                var edit = vm.PixelEdits[i];
                float x = edit.Location.X - vm.EditLocation.X;
                float y = edit.Location.Y - vm.EditLocation.Y;

                SKColor originalColor = edit.OriginalColor;

                vm.WorkingBitmap.SetPixel((int)x, (int)y, originalColor);
                PixelEditorImageCtrl.Source = vm.WorkingBitmap.ToImageSource();
            }

            PixelEditorImageCtrl.Source = vm.WorkingBitmap.ToImageSource();

            vm.PixelEdits.Clear();
        }

        private void PixelColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.PixelColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.PixelColor = color;
            };

            wm.Show(dialog);
        }

        private void PixelColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not PixelEditorViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.PixelColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.PixelColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
