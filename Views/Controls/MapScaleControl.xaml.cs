using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace RealmStudioX.WPF.Views.Controls
{
    /// <summary>
    /// Interaction logic for MapScaleControl.xaml
    /// </summary>
    public partial class MapScaleControl : System.Windows.Controls.UserControl
    {
        public MapScaleControl()
        {
            InitializeComponent();
        }

        private void SegmentColor1_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.SegmentColor1
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SegmentColor1 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SegmentColor1_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.SegmentColor1
            };

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor1 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void SegmentColor2_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.SegmentColor2
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SegmentColor2 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SegmentColor2_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.SegmentColor2
            };

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor2 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }


        private void SegmentColor3_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.SegmentColor3
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.SegmentColor3 = color;
            };

            colorSelectionWindow.Show();
        }

        private void SegmentColor3_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.SegmentColor3
            };

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.SegmentColor3 = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void FontColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.FontColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.FontColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void FontColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.FontColor
            };

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.FontColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }

        private void NumbersOutlineColor_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var colorSelectionWindow = new ColorSelectionDialog()
            {
                Owner = Window.GetWindow(this),
                InitialColor = vm.NumbersOutlineColor
            };

            colorSelectionWindow.ColorSelected += color =>
            {
                vm.NumbersOutlineColor = color;
            };

            colorSelectionWindow.Show();
        }

        private void NumbersOutlineColor_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MapScaleViewModel vm)
                return;

            var dialog = new ColorQuickPick()
            {
                InitialColor = vm.NumbersOutlineColor
            };

            // Position near button
            var button = (FrameworkElement)sender;
            var pos = button.PointToScreen(new Point(0, button.ActualHeight));

            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Left = pos.X;
            dialog.Top = pos.Y;

            dialog.Owner = Window.GetWindow(this);

            // listen for close result
            dialog.Closed += (_, __) =>
            {
                if (dialog.ColorWasSelected)
                {
                    vm.NumbersOutlineColor = dialog.SelectedColor;
                }
            };

            dialog.Show();
        }
    }
}
