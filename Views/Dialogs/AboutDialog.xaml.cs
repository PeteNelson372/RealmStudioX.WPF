using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.Models.Map;
using RealmStudioX.WPF.ViewModels.Dialogs;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : ModalDialog, INotifyPropertyChanged
    {
        public override string WindowId { get; } = Guid.NewGuid().ToString();

        public ResizeMapResult? Result { get; private set; }

        private double _aspectRatio = 1920.0 / 1080.0;
        private bool _lockAspect = true;

        public MainWindowViewModel ViewModel { get; private set; }

        public AboutDialog(MainWindowViewModel vm)
        {
            InitializeComponent();

            ViewModel = vm;

            DataContext = ViewModel;
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Hyperlink link)
                return;

            if (link.NavigateUri == null)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = link.NavigateUri.AbsoluteUri,
                UseShellExecute = true
            });
        }

        private void ImageLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image image ||
                image.Tag is not string url ||
                string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
