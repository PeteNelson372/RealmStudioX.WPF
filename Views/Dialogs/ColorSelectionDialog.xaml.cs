using RealmStudioX.WPF.ViewModels.Controls;
using System.Windows;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ColorSelectionDialog.xaml
    /// </summary>
    public partial class ColorSelectionDialog : Window
    {
        public ColorSelectionViewModel ViewModel { get; }

        public Color SelectedColor => ViewModel.CurrentColor;

        public ColorSelectionDialog(Color initialColor)
        {
            InitializeComponent();

            ViewModel = new ColorSelectionViewModel
            {
                CurrentColor = initialColor
            };

            DataContext = ViewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
