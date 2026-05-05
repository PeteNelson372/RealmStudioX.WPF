using RealmStudioX.WPF.ViewModels.Panels;
using System.Windows;
using System.Windows.Input;
using static RealmStudioX.WPF.ViewModels.Panels.SymbolsPanelViewModel;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for SymbolsToolPanel.xaml
    /// </summary>
    public partial class SymbolsToolPanel : System.Windows.Controls.UserControl
    {
        public SymbolsToolPanel()
        {
            InitializeComponent();
        }

        private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SymbolsPanelViewModel vm)
                return;

            if (sender is FrameworkElement fe &&
                fe.DataContext is SymbolGridItem item)
            {
                var def = item.SymbolDefinition;

                if (e.ChangedButton == MouseButton.Left)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (vm.Editor.SymbolSelectionService.PrimarySelectedSymbol != null)
                        {
                            vm.Editor.SymbolSelectionService.ToggleSecondarySelectedSymbol(def);
                        }
                    }
                    else
                    {
                        vm.Editor.SymbolSelectionService.SetPrimarySelectedSymbol(def);
                        vm.Editor.SymbolSelectionService.ClearSecondary();                        
                    }
                }
            }
        }
    }
}
