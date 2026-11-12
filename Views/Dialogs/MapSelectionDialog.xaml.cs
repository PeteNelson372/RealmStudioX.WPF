
using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.ViewModels.Panels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RealmStudioX.WPF.Views.Dialogs;

public partial class MapSelectionDialog : Window
{
    public ObservableCollection<ProjectMapTileViewModel> MapTiles { get; }
        = [];

    public ProjectMapTileViewModel? SelectedMapTile =>
        MapList.SelectedItem as ProjectMapTileViewModel;

    public RealmStudioMap? SelectedMap =>
        SelectedMapTile?.MapProjectEntry.Map;

    public string? SelectedMapId
    {
        get
        {
            MapProjectEntry? entry = SelectedMapTile?.MapProjectEntry;

            if (entry is null)
                return null;

            return !string.IsNullOrWhiteSpace(entry.MapId)
                ? entry.MapId
                : entry.Map.MapId;
        }
    }

    public MapSelectionDialog(
        RealmStudioProject project,
        CommandService commandService,
        string? currentMapId = null,
        Window? owner = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(commandService);

        InitializeComponent();

        DataContext = this;

        if (owner is not null)
            Owner = owner;

        ProjectMapTileViewModel? initialSelection = null;

        foreach (MapProjectEntry entry in project.Maps)
        {
            if (entry.Map is null)
                continue;

            ProjectMapTileViewModel tile =
                new(entry, commandService);

            MapTiles.Add(tile);

            if (!string.IsNullOrWhiteSpace(currentMapId) &&
                (string.Equals(
                    entry.MapId,
                    currentMapId,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    entry.Map.MapId,
                    currentMapId,
                    StringComparison.OrdinalIgnoreCase)))
            {
                initialSelection = tile;
            }
        }

        NoMapsText.Visibility = MapTiles.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (initialSelection is not null)
        {
            MapList.SelectedItem = initialSelection;
            MapList.ScrollIntoView(initialSelection);
        }

        SelectButton.IsEnabled = MapList.SelectedItem is not null;
    }

    private void MapList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled =
            MapList.SelectedItem is ProjectMapTileViewModel;
    }

    private void MapList_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (MapList.SelectedItem is ProjectMapTileViewModel)
        {
            DialogResult = true;
        }
    }

    private void Select_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (MapList.SelectedItem is ProjectMapTileViewModel)
        {
            DialogResult = true;
        }
    }
}
