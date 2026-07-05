using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using SkiaSharp;
using System.Windows.Media;

namespace RealmStudioX.WPF.ViewModels.Painting
{
    public class PaintSettings : ViewModelBase
    {
        private CancellationTokenSource? _prepareBrushCTS;
        private BrushPatternItem? _selectedBrushPattern;

        public bool IsBrushReady { get; private set; } = true;

        public BrushPatternItem? SelectedBrushPattern
        {
            get => _selectedBrushPattern;
            set
            {
                SetProperty(ref _selectedBrushPattern, value);
                _ = UpdatePreparedBrushAsync();
            }
        }

        public PreparedBrush? SelectedBrush { get; set; }

        private SKColor _selectedColor = SKColors.Black;
        public SKColor SelectedColor
        {
            get => _selectedColor;
            set
            {
                SetProperty(ref _selectedColor, value);
                _ = UpdatePreparedBrushAsync();
            }
        }

        private int _brushSize = 10;
        public int BrushSize
        {
            get => _brushSize;
            set
            {
                SetProperty(ref _brushSize, value);
                _ = UpdatePreparedBrushAsync();
            }
        }

        public int MinBrushSpacing { get; } = 1;
        public int MaxBrushSpacing { get; } = 5000;

        private int _brushSpacing = 10;

        public int BrushSpacing
        {
            get => _brushSpacing;
            set
            {
                var clamped = Math.Clamp(value, MinBrushSpacing, MaxBrushSpacing);

                if (_brushSpacing != clamped)
                {
                    SetProperty(ref _brushSpacing, clamped);
                    _ = UpdatePreparedBrushAsync();
                }
            }
        }

        public async Task UpdatePreparedBrushAsync()
        {
            _prepareBrushCTS?.Cancel();

            _prepareBrushCTS = new CancellationTokenSource();

            CancellationToken token = _prepareBrushCTS.Token;

            try
            {
                IsBrushReady = false;

                PreparedBrush brush =
                    await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        return PrepareNewBrush();
                    }, token);

                token.ThrowIfCancellationRequested();

                SelectedBrush = brush;
                
                IsBrushReady = true;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _prepareBrushCTS?.Dispose();
                _prepareBrushCTS = null;

                IsBrushReady = true;
            }

        }

        private PreparedBrush PrepareNewBrush()
        {
            BrushSpacing = SelectedBrushPattern?.BrushDefinition?.BrushSpacing ?? 10;

            PreparedBrush newPreparedBrush = new()
            {
                SourceBrush = SelectedBrushPattern?.BrushDefinition,
                Color = SelectedColor,
                BrushSize = BrushSize,
                BrushSpacing = BrushSpacing,
            };

            AssetInitializer.GetPreparedBrushBitmaps(newPreparedBrush);
            return newPreparedBrush;
        }
    }

    public class BrushPatternItem
    {
        public string Name { get; set; } = "";

        public ImageSource? PreviewImage { get; set; }

        public MapBrush? BrushDefinition { get; set; }
    }
}
