using RealmStudioX.WPF.Editor.UserInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Dialogs
{
    public partial class ColorQuickPick : Window, INotifyPropertyChanged
    {
        public bool ColorWasSelected { get; private set; }

        public List<SolidColorBrush> ColorGrid { get; } = new();
        public List<SolidColorBrush> GrayScaleRow { get; } = new();

        public List<SolidColorBrush> QuickColors { get; } = new();

        private Color _selectedColor;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ColorQuickPick(Color initialColor)
        {
            InitializeComponent();

            QuickColors = ColorPaletteGenerator.GenerateQuickRow();
            GrayScaleRow = ColorPaletteGenerator.GenerateGrayScaleRow();
            ColorGrid = ColorPaletteGenerator.GenerateCompactPalette();

            DataContext = this;
        }


        private void Color_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.Background is SolidColorBrush brush)
            {
                SelectedColor = brush.Color;
                ColorWasSelected = true;

                Close(); // ✔ correct for non-modal
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ColorWasSelected = false;
            Close();
        }

        private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Focus(); // ensures Deactivated fires reliably
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);

            if (IsVisible)
                Close();
        }
    }
}