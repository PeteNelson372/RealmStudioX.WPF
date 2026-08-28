using RealmStudioX.WPF.ViewModels.Main;
using System.Windows.Controls;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MainTabs.xaml
    /// </summary>
    public partial class MainTabs : System.Windows.Controls.UserControl
    {
        public event EventHandler? TabSelectionChanged;

        public MainWindowViewModel ViewModel { get; }

        public MainTabs()
        {
            InitializeComponent();

            ViewModel = (MainWindowViewModel)DataContext;

            MainTabControl.SelectionChanged += (s, e) => TabSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectTab(string header)
        {
            foreach (TabItem tab in MainTabControl.Items)
            {
                if (tab.Header?.ToString() == header)
                {
                    MainTabControl.SelectedItem = tab;
                    break;
                }
            }
        }
    }
}
