using MaterialDesignThemes.Wpf;
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.IO;

namespace RealmStudioX.WPF.Editor.Services
{
    public class ThemeManager
    {
        public ThemeManager(AssetManager assetManager)
        {
            _assetManager = assetManager;
            LoadThemeNames();
        }

        private AssetManager _assetManager;

        public ObservableCollection<MapTheme> Themes { get; } = [];

        public ObservableCollection<string> ThemeNames { get; } = [];

        private readonly string _themesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RealmStudioX", "Assets", "Themes");

        public MapTheme? DefaultTheme { get; set; }

        private bool _applyToBackground = true;
        public bool ApplyToBackground
        {
            get => _applyToBackground;
            set => _applyToBackground = value;
        }

        private bool _applyToOcean = true;
        public bool ApplyToOcean
        {
            get => _applyToOcean;
            set => _applyToOcean = value;
        }

        private bool _applyToLandforms = true;
        public bool ApplyToLandforms
        {
            get => _applyToLandforms;
            set => _applyToLandforms = value;
        }

        private bool _applyToFreshwater = true;
        public bool ApplyToFreshwater
        {
            get => _applyToFreshwater;
            set => _applyToFreshwater = value;
        }

        private bool _applyToPaths = true;
        public bool ApplyToPaths
        {
            get => _applyToPaths;
            set => _applyToPaths = value;
        }

        private bool _applyToSymbolColors = true;
        public bool ApplyToSymbolColors
        {
            get => _applyToSymbolColors;
            set => _applyToSymbolColors = value;
        }

        private bool _applyToLabels = true;
        public bool ApplyToLabels
        {
            get => _applyToLabels;
            set => _applyToLabels = value;
        }

        private bool _applyToLabelPresets = true;
        public bool ApplyToLabelPresets
        {
            get => _applyToLabelPresets;
            set => _applyToLabelPresets = value;
        }

        public string ThemesFolder
        {
            get { return _themesFolder; }
        }

        public void LoadThemeNames()
        {
            if (!Directory.Exists(_themesFolder))
                return;

            var files = Directory.GetFiles(_themesFolder, "*" + RealmStudioFileFormat.RealmStudioThemeExtension);

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    string themeName = Path.GetFileNameWithoutExtension(file);
                    ThemeNames.Add(themeName);
                }
            }
        }

        public MapTheme? LoadThemeByName(string name)
        {
            if (!Directory.Exists(_themesFolder))
                return null;

            var files = Directory.GetFiles(_themesFolder, "*" + RealmStudioFileFormat.RealmStudioThemeExtension);

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        string themeName = Path.GetFileNameWithoutExtension(file);

                        if (themeName == name)
                        {
                            MapTheme? theme = MapFileMethods.ReadThemeFromXml(file);
                            return theme;
                        }
                    }
                    catch (Exception ex)
                    {
                        RealmStudioXLogger.Exception($"Error loading theme: {file}", ex);
                    }
                }
            }

            return null;
        }


        public void LoadThemes()
        {
            if (!Directory.Exists(_themesFolder))
                return;

            var files = Directory.GetFiles(_themesFolder, "*" + RealmStudioFileFormat.RealmStudioThemeExtension);

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        MapTheme? theme = MapFileMethods.ReadThemeFromXml(file);

                        if (theme != null)
                        {
                            Themes.Add(theme);

                            if (theme.IsDefaultTheme)
                            {
                                DefaultTheme = theme;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RealmStudioXLogger.Exception($"Error loading theme: {file}", ex);
                    }
                }
            }
        }

        public void ResolveThemeAssets(MapTheme theme, MainWindowViewModel mainWindowViewModel)
        {
            theme.LabelFont = new FontStyleModel()
            {
                Family = theme.LabelFontFamily ?? "Arial",
                Size = theme.LabelFontSize,
                Bold = theme.LabelFontBold,
                Italic = theme.LabelFontItalic,
            };

            // build the label preset fonts
            foreach (LabelPreset lp in theme.LabelPresets)
            {
                ResolveLabelPresetFont(lp);
            }
        }

        public void ResolveLabelPresetFont(LabelPreset lp)
        {
            lp.LabelFont = new FontStyleModel()
            {
                Family = lp.LabelFontFamily ?? "Arial",
                Size = lp.LabelFontSize,
                Bold = lp.LabelFontBold,
                Italic = lp.LabelFontItalic,
            };
        }

        public SKImage? ResolvedThemeTextureAsset(string textureId, float? scale, float? opacity)
        {
            SKImage? textureImage = _assetManager.GetImage(textureId);

            if (textureImage != null)
            {
                SKBitmap textureBitmap = SKBitmap.FromImage(textureImage);

                float bitmapScale = (float)(scale != null ? scale : 1.0f);

                SKBitmap resizedBitmap = Utilities.ScaleSKBitmap(textureBitmap, bitmapScale);

                float bitmapOpacity = (float)(opacity != null ? opacity : 1.0f);

                SKBitmap opacitySetBitmap = Utilities.SetBitmapOpacity(resizedBitmap, bitmapOpacity);

                return SKImage.FromBitmap(opacitySetBitmap);
            }

            return null;
        }

        public void ApplyTheme(MapTheme theme, MainWindowViewModel mainWindowViewModel)
        {
            if (ApplyToBackground)
            {
                // background
                if (!string.IsNullOrEmpty(theme.BackgroundTextureId))
                {
                    mainWindowViewModel.BackgroundViewModel.TextureBrowser.SetById(theme.BackgroundTextureId);

                    TextureFillRequest fillBackgroundRequest = new()
                    {
                        TextureId = theme.BackgroundTextureId,
                        Scale = theme.BackgroundTextureScale,
                        Mirror = theme.MirrorBackgroundTexture,
                    };

                    mainWindowViewModel.Editor.FillBackground(fillBackgroundRequest);
                }

                mainWindowViewModel.BackgroundViewModel.TextureScale = theme.BackgroundTextureScale;
                mainWindowViewModel.BackgroundViewModel.MirrorTexture = theme.MirrorBackgroundTexture;

                mainWindowViewModel.BackgroundViewModel.VignetteColor = theme.VignetteColor.ToColor();
                mainWindowViewModel.BackgroundViewModel.VignetteStrength = theme.VignetteStrength;
                mainWindowViewModel.BackgroundViewModel.VignetteType = theme.VignetteShape;
            }

            if (ApplyToOcean)
            {
                // ocean
                if (!string.IsNullOrEmpty(theme.OceanTextureId))
                {
                    mainWindowViewModel.OceanViewModel.TextureBrowser.SetById(theme.OceanTextureId);

                    TextureFillRequest applyOceanTextureRequest = new()
                    {
                        TextureId = theme.OceanTextureId,
                        Scale = theme.OceanTextureScale,
                        Opacity = theme.OceanTextureOpacity,
                        Mirror = theme.MirrorOceanTexture
                    };

                    mainWindowViewModel.Editor.ApplyOceanTexture(applyOceanTextureRequest);
                }

                mainWindowViewModel.OceanViewModel.TextureScale = theme.OceanTextureScale;
                mainWindowViewModel.OceanViewModel.TextureOpacity = theme.OceanTextureOpacity;
                mainWindowViewModel.OceanViewModel.MirrorTexture = theme.MirrorOceanTexture;
            }

            if (ApplyToLandforms)
            {
                // landforms
                if (!string.IsNullOrEmpty(theme.LandformTextureId))
                {
                    mainWindowViewModel.LandformViewModel.TextureBrowser.SetById(theme.LandformTextureId);
                }

                mainWindowViewModel.LandformViewModel.TextureFill = theme.UseLandformTextureBackground;

                mainWindowViewModel.LandformViewModel.LandformOutlineColor = theme.LandformOutlineColor.ToColor();
                mainWindowViewModel.LandformViewModel.LandformBackgroundColor = theme.LandformBackgroundColor.ToColor();
                mainWindowViewModel.LandformViewModel.LandformOutlineWidth = theme.LandformOutlineWidth;
                mainWindowViewModel.LandformViewModel.SelectedCoastlineStyle = theme.CoastlineStyle;
                mainWindowViewModel.LandformViewModel.CoastlineColor = theme.CoastlineColor.ToColor();
                mainWindowViewModel.LandformViewModel.CoastlineEffectDistance = theme.CoastlineEffectDistance;
                mainWindowViewModel.LandformViewModel.LandformShadingDepth = theme.LandformShadingDepth;
            }

            if (ApplyToFreshwater)
            {
                // freshwater
                mainWindowViewModel.WaterViewModel.DeepWaterColor = theme.DeepWaterColor.ToColor();
                mainWindowViewModel.WaterViewModel.ShallowWaterColor = theme.ShallowWaterColor.ToColor();
                mainWindowViewModel.WaterViewModel.ShorelineColor = theme.ShorelineColor.ToColor();
            }

            if (ApplyToPaths)
            {
                // paths
                mainWindowViewModel.PathViewModel.PathColor = theme.PathColor.ToColor();
            }

            if (ApplyToLabels)
            {
                // labels
                mainWindowViewModel.LabelsViewModel.LabelColor = theme.LabelColor.ToColor();
                mainWindowViewModel.LabelsViewModel.OutlineColor = theme.LabelOutlineColor.ToColor();
                mainWindowViewModel.LabelsViewModel.GlowColor = theme.LabelGlowColor.ToColor();

                mainWindowViewModel.LabelsViewModel.FontStyle = theme.LabelFont;
                mainWindowViewModel.FontPanelViewModel.SelectedFontFamily = theme.LabelFontFamily;
                mainWindowViewModel.FontPanelViewModel.SelectedFontSize = (int)theme.LabelFontSize;
                mainWindowViewModel.FontPanelViewModel.IsBold = theme.LabelFontBold;
                mainWindowViewModel.FontPanelViewModel.IsItalic = theme.LabelFontItalic;
            }

            if (ApplyToLabelPresets)
            {
                // add label presets
                foreach (LabelPreset lp in theme.LabelPresets)
                {
                    mainWindowViewModel.LabelsViewModel.LabelPresets.Add(lp);
                }
            }


            if (ApplyToSymbolColors)
            {
                // symbol colors
                mainWindowViewModel.SymbolsViewModel.SymbolColor1 = theme.CustomSymbolColors[0].ToColor();
                mainWindowViewModel.SymbolsViewModel.SymbolColor2 = theme.CustomSymbolColors[1].ToColor();
                mainWindowViewModel.SymbolsViewModel.SymbolColor3 = theme.CustomSymbolColors[2].ToColor();
            }
        }
    }
}
