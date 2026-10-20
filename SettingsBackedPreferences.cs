using RealmStudioX.Infrastructure;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealmStudioX.WPF;

public sealed class SettingsBackedPreferences : IApplicationPreferences
{
    private readonly Properties.Settings _settings = Properties.Settings.Default;

    private const string DefaultThemeNameValue = "Medieval Quest";

    public event PropertyChangedEventHandler? PropertyChanged;

    #region General

    public string DefaultThemeName => DefaultThemeNameValue;

    public StringCollection MeasurementUnits => _settings.MeasurementUnits;

    public string DefaultMeasurementUnits
    {
        get => _settings.DefaultMeasurementUnits;
        set => SetSetting(_settings.DefaultMeasurementUnits, value, v => _settings.DefaultMeasurementUnits = v);
    }

    #endregion

    #region Directory Settings

    public string RootRealmStudioXDirectory
    {
        get => _settings.RootRealmStudioXDirectory;
        set => SetSetting(_settings.RootRealmStudioXDirectory, value, v => _settings.RootRealmStudioXDirectory = v);
    }

    public string RootRealmsDirectory
    {
        get => _settings.RootRealmsDirectory;
        set => SetSetting(_settings.RootRealmsDirectory, value, v => _settings.RootRealmsDirectory = v);
    }

    public string RootAssetDirectory
    {
        get => _settings.RootAssetDirectory;
        set => SetSetting(_settings.RootAssetDirectory, value, v => _settings.RootAssetDirectory = v);
    }

    public string DefaultModelsDirectory
    {
        get => _settings.DefaultModelsDirectory;
        set => SetSetting(_settings.DefaultModelsDirectory, value, v => _settings.DefaultModelsDirectory = v);
    }

    public string DefaultSymbolDirectory
    {
        get => _settings.DefaultSymbolDirectory;
        set => SetSetting(_settings.DefaultSymbolDirectory, value, v => _settings.DefaultSymbolDirectory = v);
    }

    public string SymbolTagsFilePath
    {
        get => _settings.SymbolTagsFilePath;
        set => SetSetting(_settings.SymbolTagsFilePath, value, v => _settings.SymbolTagsFilePath = v);
    }

    public string StructureSynonymsFilePath
    {
        get => _settings.StructureSynonymsFilePath;
        set => SetSetting(_settings.StructureSynonymsFilePath, value, v => _settings.StructureSynonymsFilePath = v);
    }

    public string TerrainSynonymsFilePath
    {
        get => _settings.TerrainSynonymsFilePath;
        set => SetSetting(_settings.TerrainSynonymsFilePath, value, v => _settings.TerrainSynonymsFilePath = v);
    }

    public string VegetationSynonymsFilePath
    {
        get => _settings.VegetationSynonymsFilePath;
        set => SetSetting(_settings.VegetationSynonymsFilePath, value, v => _settings.VegetationSynonymsFilePath = v);
    }

    public string LastFileDirectory
    {
        get => _settings.LastFileDirectory;
        set => SetSetting(_settings.LastFileDirectory, value, v => _settings.LastFileDirectory = v);
    }

    #endregion

    #region Map Defaults

    public int DefaultMapWidth
    {
        get => _settings.DefaultMapWidth;
        set => SetSetting(_settings.DefaultMapWidth, value, v => _settings.DefaultMapWidth = v);
    }

    public int DefaultMapHeight
    {
        get => _settings.DefaultMapHeight;
        set => SetSetting(_settings.DefaultMapHeight, value, v => _settings.DefaultMapHeight = v);
    }

    public string SelectedMapAreaUnits
    {
        get => _settings.SelectedMapAreaUnits;
        set => SetSetting(_settings.SelectedMapAreaUnits, value, v => _settings.SelectedMapAreaUnits = v);
    }

    #endregion

    #region Autosave

    public bool AutoSaveEnabled
    {
        get => _settings.AutoSaveEnabled;
        set => SetSetting(_settings.AutoSaveEnabled, value, v => _settings.AutoSaveEnabled = v);
    }

    public string AutosaveDirectory
    {
        get => _settings.AutosaveDirectory;
        set => SetSetting(_settings.AutosaveDirectory, value, v => _settings.AutosaveDirectory = v);
    }

    public int AutosaveInterval
    {
        get => _settings.AutosaveInterval;
        set => SetSetting(_settings.AutosaveInterval, value, v => _settings.AutosaveInterval = v);
    }

    // UI bounds for the autosave interval; these are not persisted settings.
    public int MinSaveInterval => 5;

    public int MaxSaveInterval => 30;

    #endregion

    #region Export and Notifications

    public bool PlaySoundEnabled
    {
        get => _settings.PlaySoundEnabled;
        set => SetSetting(_settings.PlaySoundEnabled, value, v => _settings.PlaySoundEnabled = v);
    }

    public string SelectedSaveSound
    {
        get => _settings.SelectedSaveSound;
        set => SetSetting(_settings.SelectedSaveSound, value, v => _settings.SelectedSaveSound = v);
    }

    public string DefaultExportFormat
    {
        get => _settings.DefaultExportFormat;
        set => SetSetting(_settings.DefaultExportFormat, value, v => _settings.DefaultExportFormat = v);
    }

    #endregion

    #region Updates and World Anvil

    public bool CheckUpdatesEnabled
    {
        get => _settings.CheckUpdatesEnabled;
        set => SetSetting(_settings.CheckUpdatesEnabled, value, v => _settings.CheckUpdatesEnabled = v);
    }

    public bool EnableWAIntegration
    {
        get => _settings.EnableWAIntegration;
        set => SetSetting(_settings.EnableWAIntegration, value, v => _settings.EnableWAIntegration = v);
    }

    public string WorldAnvilApiToken
    {
        get => _settings.WorldAnvilApiToken;
        set => SetSetting(_settings.WorldAnvilApiToken, value, v => _settings.WorldAnvilApiToken = v);
    }

    #endregion

    public void Save()
    {
        _settings.Save();
    }

    private void SetSetting<T>(
        T currentValue,
        T newValue,
        Action<T> update,
        [CallerMemberName] string? propertyName = null,
        string[]? additionalPropertyNames = null)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return;
        }

        update(newValue);

        OnPropertyChanged(propertyName);

        if (additionalPropertyNames is not null)
        {
            foreach (string additionalName in additionalPropertyNames)
            {
                OnPropertyChanged(additionalName);
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}