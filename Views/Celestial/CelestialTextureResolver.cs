using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialTextureResolver : IDisposable
    {
        private const int MapTextureEdgeInset = 3;

        private readonly MainWindowViewModel _mainViewModel;

        private readonly Dictionary<string, BitmapSource> _textureCache =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, BitmapSource> _mapTextureCache =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _cacheLock = new();
        private readonly object _mapRenderLock = new();

        private bool _disposed;

        public CelestialTextureResolver(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel
                ?? throw new ArgumentNullException(nameof(mainViewModel));

            _mainViewModel.ProjectManager.ProjectChanged +=
                ProjectManager_ProjectChanged;
        }

        // ---------------------------------------------------------
        // Public API
        // ---------------------------------------------------------

        public ImageSource? Resolve(
            CelestialTextureSource? textureSource)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (textureSource is null)
                return null;

            return textureSource.SourceType switch
            {
                CelestialTextureSourceType.None =>
                    null,

                CelestialTextureSourceType.RealmMap =>
                    ResolveRealmMap(textureSource.MapId),

                CelestialTextureSourceType.Asset =>
                    LoadCachedTexture(textureSource.AssetPath, isAsset: true),

                CelestialTextureSourceType.BitmapFile =>
                    LoadCachedTexture(textureSource.FilePath, isAsset: false),

                CelestialTextureSourceType.Procedural =>
                    ResolveProcedural(textureSource),

                _ => null
            };
        }

        // ---------------------------------------------------------
        // Realm Map Resolution
        // ---------------------------------------------------------

        private BitmapSource? ResolveRealmMap(string? mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
                return null;

            RealmStudioProject? project =
                _mainViewModel.ProjectManager.CurrentProject;

            if (project is null)
                return null;

            MapProjectEntry? entry = project.Maps.FirstOrDefault(
                item =>
                    string.Equals(
                        item.MapId,
                        mapId,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        item.Map.MapId,
                        mapId,
                        StringComparison.OrdinalIgnoreCase));

            if (entry?.Map is null)
                return null;

            string cacheKey =
                !string.IsNullOrWhiteSpace(entry.MapId)
                    ? entry.MapId
                    : !string.IsNullOrWhiteSpace(entry.Map.MapId)
                        ? entry.Map.MapId
                        : mapId;

            // Serialize map rendering so multiple celestial bodies
            // cannot simultaneously render through the shared context.
            lock (_mapRenderLock)
            {
                lock (_cacheLock)
                {
                    if (_mapTextureCache.TryGetValue(
                        cacheKey,
                        out BitmapSource? cachedTexture))
                    {
                        return cachedTexture;
                    }
                }

                using SKBitmap? renderedTexture =
                    CreateMapTexture(entry.Map);

                if (renderedTexture is null)
                    return null;

                if (renderedTexture.ToImageSource()
                    is not BitmapSource bitmapSource)
                {
                    return null;
                }

                if (bitmapSource.CanFreeze && !bitmapSource.IsFrozen)
                    bitmapSource.Freeze();

                lock (_cacheLock)
                {
                    _mapTextureCache[cacheKey] = bitmapSource;
                }

                return bitmapSource;
            }
        }

        // ---------------------------------------------------------
        // Map Rendering
        // ---------------------------------------------------------

        private SKBitmap CreateMapTexture(RealmStudioMap map)
        {
            ArgumentNullException.ThrowIfNull(map);

            int width = map.MapWidth;
            int height = map.MapHeight;

            if (width <= MapTextureEdgeInset * 2 ||
                height <= MapTextureEdgeInset * 2)
            {
                throw new InvalidOperationException(
                    "The map is too small to create a celestial texture.");
            }

            RenderContext renderContext =
                _mainViewModel.RenderContext
                ?? throw new InvalidOperationException(
                    "The rendering context has not been initialized.");

            // Create an independent rendering scene.
            // This does not modify the active editor scene or camera.
            MapScene scene = new(map)
            {
                RenderContext = renderContext
            };

            // Render the map without editor overlays.
            using SKBitmap renderedMap = new(
                new SKImageInfo(width, height));

            using (SKCanvas renderCanvas = new(renderedMap))
            {
                renderCanvas.Clear(SKColors.Transparent);

                scene.RenderFor3DGlobeView(renderCanvas);
            }

            // Create the final texture at the original map dimensions.
            SKBitmap texture = new(
                new SKImageInfo(width, height));

            try
            {
                using SKCanvas textureCanvas = new(texture);

                textureCanvas.Clear(SKColors.Transparent);

                // Exclude the outer three pixels on each edge.
                SKRect sourceRect = new(
                    MapTextureEdgeInset,
                    MapTextureEdgeInset,
                    width - MapTextureEdgeInset,
                    height - MapTextureEdgeInset);

                // Expand the cropped image back to the original dimensions.
                SKRect destinationRect = new(0, 0, width, height);

                textureCanvas.DrawBitmap(
                    renderedMap,
                    sourceRect,
                    destinationRect,
                    SKSamplingOptions.Default);

                return texture;
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }

        // ---------------------------------------------------------
        // Asset / Bitmap File Resolution
        // ---------------------------------------------------------

        private BitmapSource? LoadCachedTexture(
            string? texturePath,
            bool isAsset)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            string resolvedPath = texturePath.Trim();

            if (isAsset && !Path.IsPathRooted(resolvedPath))
            {
                resolvedPath = WorldGlobeAssets.GetPath(resolvedPath);
            }

            if (!File.Exists(resolvedPath))
                return null;

            resolvedPath = Path.GetFullPath(resolvedPath);

            lock (_cacheLock)
            {
                if (_textureCache.TryGetValue(
                    resolvedPath,
                    out BitmapSource? cachedTexture))
                {
                    return cachedTexture;
                }

                BitmapImage bitmap = new();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(
                    resolvedPath,
                    UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                _textureCache[resolvedPath] = bitmap;

                return bitmap;
            }
        }

        // ---------------------------------------------------------
        // Procedural Resolution
        // ---------------------------------------------------------

        private ImageSource? ResolveProcedural(
            CelestialTextureSource textureSource)
        {
            ArgumentNullException.ThrowIfNull(textureSource);

            // Procedural texture generation will be implemented through
            // a generator registry keyed by GeneratorId.
            //
            // GeneratorParameters will be supplied to the registered
            // generator when that infrastructure is introduced.

            return null;
        }

        // ---------------------------------------------------------
        // Cache Management
        // ---------------------------------------------------------

        public void InvalidateMapTexture(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
                return;

            lock (_cacheLock)
            {
                _mapTextureCache.Remove(mapId);
            }
        }

        public void InvalidateAllMapTextures()
        {
            lock (_cacheLock)
            {
                _mapTextureCache.Clear();
            }
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _textureCache.Clear();
                _mapTextureCache.Clear();
            }
        }

        private void ProjectManager_ProjectChanged(
            object? sender,
            EventArgs e)
        {
            // A different project may contain different maps with
            // identical IDs. Never retain map textures across projects.
            InvalidateAllMapTextures();
        }

        // ---------------------------------------------------------
        // Disposal
        // ---------------------------------------------------------

        public void Dispose()
        {
            if (_disposed)
                return;

            _mainViewModel.ProjectManager.ProjectChanged -=
                ProjectManager_ProjectChanged;

            ClearCache();

            _disposed = true;
        }
    }
}