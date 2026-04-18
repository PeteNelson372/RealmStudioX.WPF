using System.Windows.Controls;

namespace RealmStudioX.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainTabs.xaml
    /// </summary>
    public partial class MainTabs : System.Windows.Controls.UserControl
    {
        public event EventHandler? TabSelectionChanged;

        public MainTabs()
        {
            InitializeComponent();

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
