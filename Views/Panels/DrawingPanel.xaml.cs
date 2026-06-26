using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Panels
{
    /// <summary>
    /// Interaction logic for DrawingPanel.xaml
    /// </summary>
    public partial class DrawingPanel : System.Windows.Controls.UserControl
    {
        public DrawingPanel()
        {
            InitializeComponent();

            MapLayerList.ItemsSource = new List<DrawableMapLayerItem>
            {
                new () { Name="BASE",               Index=MapBuilder.BASELAYER,},
                new () { Name="OCEANDRAWING",       Index=MapBuilder.OCEANDRAWINGLAYER,},
                new () { Name="WINDROSE",           Index=MapBuilder.WINDROSELAYER,},
                new () { Name="ABOVEOCEANGRID",     Index=MapBuilder.ABOVEOCEANGRIDLAYER,},
                new () { Name="COASTLINE",          Index=MapBuilder.LANDCOASTLINELAYER,},
                new () { Name="LANDFORM",           Index=MapBuilder.LANDFORMLAYER,},
                new () { Name="LANDDRAWING",        Index=MapBuilder.LANDDRAWINGLAYER,},
                new () { Name="WATER",              Index=MapBuilder.WATERLAYER,},
                new () { Name="WATERDRAWING",       Index=MapBuilder.WATERDRAWINGLAYER,},
                new () { Name="BELOWSYMBOLSGRID",   Index=MapBuilder.BELOWSYMBOLSGRIDLAYER,},
                new () { Name="PATHLOWER",          Index=MapBuilder.PATHLOWERLAYER,},
                new () { Name="SYMBOLS",            Index=MapBuilder.SYMBOLLAYER,},
                new () { Name="PATHUPPER",          Index=MapBuilder.PATHUPPERLAYER,},
                new () { Name="REGION",             Index=MapBuilder.REGIONLAYER,},
                new () { Name="REGIONOVERLAY",      Index=MapBuilder.REGIONOVERLAYLAYER,},
                new () { Name="GRID",               Index=MapBuilder.DEFAULTGRIDLAYER,},
                new () { Name="BOXES",              Index=MapBuilder.BOXLAYER},
                new () { Name="LABELS",             Index=MapBuilder.LABELLAYER,},
                new () { Name="OVERLAY",            Index=MapBuilder.OVERLAYLAYER,},
                new () { Name="FRAME",              Index=MapBuilder.FRAMELAYER,},
                new () { Name="USERDRAWING",        Index=MapBuilder.DRAWINGLAYER,},
                new () { Name="VIGNETTE",           Index=MapBuilder.VIGNETTELAYER,},
            };
        }



        private void LineBrushSizeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Slider slider)
            {
                return;
            }

            Track? track =
                slider.Template.FindName(
                    "PART_Track",
                    slider) as Track;

            if (track == null)
            {
                return;
            }

            track.Thumb.DragCompleted += LineBrushSizeSlider_DragCompleted;
        }

        private void LineBrushSizeSlider_DragCompleted(object? sender, DragCompletedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            vm.UpdateDrawingParameters();
            vm.UpdatePreparedBrush();
        }

        private void SelectShapeFillType_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && btn.Tag is DrawingFillType type)
            {
                vm.SelectedShapeFillType = type;
            }

            DropDownButton.IsChecked = false;
        }

        private void DrawColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.DrawingColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.DrawingColor = color;
            };

            wm.Show(dialog);
        }

        private void DrawColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.DrawingColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.DrawingColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }

        private void FillColor_LeftClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            ColorSelectionDialog dialog = WindowBuilder.BuildColorSelectionDialog(vm.FillColor, Window.GetWindow(this));

            WindowManager wm = ((App)Application.Current).WindowManager;

            dialog.ColorSelected += color =>
            {
                vm.FillColor = color;
            };

            wm.Show(dialog);
        }

        private void FillColor_RightClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not DrawingPanelViewModel vm)
                return;

            ColorQuickPick dialog = WindowBuilder.BuildColorQuickPick(vm.FillColor, Window.GetWindow(this), (Button)sender);

            WindowManager wm = ((App)Application.Current).WindowManager;

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.FillColor = dialog.SelectedColor;
                }
            };

            wm.Show(dialog);
        }
    }
}
