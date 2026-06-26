using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace RealmStudioX
{
    internal class SymbolTool(
        EditorController editor,
        CommandManager commands,
        IAssetProvider assets,
        MapLayer targetLayer,
        MapScene scene,
        SymbolSelectionService symbolSelectionService,
        SymbolImageCache imageCache,
        EditorState editorState,
        ISymbolSettings symbolSettings) : IToolEditor, IDisposable
    {
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly EditorController _editor = editor;
        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly SymbolSelectionService _symbolSelectionService = symbolSelectionService;
        private readonly SymbolImageCache _imageCache = imageCache;
        private readonly EditorState _editorState = editorState;
        private readonly ISymbolSettings _symbolSettings = symbolSettings;

        private RenderContext _renderContext = null!;

        public RenderContext RenderContext
        {
            get => _renderContext;
            set
            {
                if (_renderContext != null)
                {
                    throw new InvalidOperationException("RenderContext can only be set once.");
                }
                _renderContext = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        private SKPoint _lastPlacementPoint;

        private string _symbolFilterText = string.Empty;

        private readonly HashSet<MapSymbol> _erasedThisStroke = [];
        private readonly HashSet<MapSymbol> _paintedThisStroke = [];

        private IUndoableCommand? _activeCommand;

        float _minSpacing = 4f;     // dense
        float _maxSpacing = 40f;    // sparse

        private readonly List<(SKPoint pos, float radius)> _brushPlaced = [];

        private bool disposedValue;

        public void Activate()
        {

        }

        public void Cancel()
        {

        }

        public void Deactivate()
        {

        }

        public void OnMouseDown(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolPlace)
                {
                    _activeCommand = new Cmd_ModifySymbols(_layer);

                    if (!_symbolSettings.UseAreaBrush)
                    {
                        MapSymbolDefinition? def;

                        if (_symbolSettings.RandomizeSymbolColors)
                        {
                            def = GetRandomSelectedSymbol();
                        }
                        else
                        {
                            def = _symbolSelectionService.PrimarySelectedSymbol;
                        }

                        if (def == null)
                        {
                            return;
                        }

                        var symbol = CreateSymbolInstance(def, state.WorldPoint);

                        _layer.Add(symbol);
                        ((Cmd_ModifySymbols)_activeCommand).RegisterNewSymbol(symbol);
                    }
                    else
                    {
                        PlaceSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                        _lastPlacementPoint = state.WorldPoint;
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolErase)
                {
                    _activeCommand = new Cmd_ModifySymbols(_layer);
                    EraseSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolColor)
                {
                    if (_symbolSettings.UseAreaBrush)
                    {
                        _activeCommand = new Cmd_ModifySymbols(_layer);
                        PaintSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                    }
                    else
                    {
                        PaintSymbolAtLocation(state.WorldPoint);
                    }
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            if (state.Button == EditorMouseButton.Left)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolPlace)
                {
                    if (_symbolSettings.UseAreaBrush)
                    {
                        float spacing = GetPlacementSpacing();

                        var delta = SKPoint.Distance(state.WorldPoint, _lastPlacementPoint);

                        if (delta >= spacing)
                        {
                            PlaceSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                            _lastPlacementPoint = state.WorldPoint;
                        }
                    }
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolErase)
                {
                    _activeCommand = new Cmd_ModifySymbols(_layer);
                    EraseSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                }
                else if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolColor)
                {
                    if (_symbolSettings.UseAreaBrush)
                    {
                        PaintSymbolsInBrush(state.WorldPoint, _symbolSettings.AreaBrushSize);
                    }
                    else
                    {
                        PaintSymbolAtLocation(state.WorldPoint);
                    }
                }
            }
        }

        public void OnMouseUp(PointerState state)
        {
            _brushPlaced.Clear();

            _erasedThisStroke.Clear();
            _paintedThisStroke.Clear();

            if (_activeCommand != null)
            {
                _commands.Execute(_activeCommand);
                _activeCommand = null;
            }
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        private readonly Random _rng = new();

        private MapSymbolDefinition? GetRandomSelectedSymbol()
        {
            var primary = _symbolSelectionService.PrimarySelectedSymbol;
            var secondary = _symbolSelectionService.SecondarySelectedSymbols;

            // Nothing selected at all
            if (primary == null && (secondary == null || secondary.Count == 0))
            {
                return null;
            }

            // Only primary exists: deterministic
            if (primary != null && (secondary == null || secondary.Count == 0))
            {
                return primary;
            }

            // Build combined selection pool: Primary + Secondary
            int secondaryCount = secondary?.Count ?? 0;
            int total = (primary != null ? 1 : 0) + secondaryCount;

            int index = _rng.Next(total);

            // Primary occupies slot 0 (if it exists)
            if (primary != null)
            {
                if (index == 0)
                {
                    return primary;
                }

                return secondary![index - 1];
            }

            // Edge case: no primary, only secondary (should never happen)
            return secondary![index];
        }

        private void PlaceSymbolsInBrush(SKPoint center, float brushRadius)
        {
            _brushPlaced.Clear();

            int targetCount = EstimateCapacity(brushRadius);

            const int maxAttemptsPerSymbol = 15;

            for (int i = 0; i < targetCount; i++)
            {
                var def = GetRandomSelectedSymbol();
                if (def == null)
                {
                    return;
                }

                float radius = GetExclusionRadius(def);

                bool placed = false;

                for (int attempt = 0; attempt < maxAttemptsPerSymbol; attempt++)
                {
                    var offset = RandomPointInCircle(brushRadius);

                    var candidate = new SKPoint(
                        center.X + offset.X,
                        center.Y + offset.Y);

                    if (IsFarEnough(candidate, radius) && _activeCommand != null)
                    {
                        _brushPlaced.Add((candidate, radius));

                        var symbol = CreateSymbolInstance(def, candidate);

                        _layer.Enqueue(symbol);

                        ((Cmd_ModifySymbols)_activeCommand!).RegisterNewSymbol(symbol);

                        placed = true;
                        break;
                    }
                }

                // Early exit if saturated
                if (!placed)
                {
                    break;
                }
            }

        }

        private void EraseSymbolsInBrush(SKPoint worldPos, int areaBrushSize)
        {
            float radius = areaBrushSize / 2f;

            var brushBounds = new SKRect(
                worldPos.X - radius,
                worldPos.Y - radius,
                worldPos.X + radius,
                worldPos.Y + radius);

            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER);

            var candidates = symbolLayer.QuerySymbolsInRadius(worldPos, radius);

            foreach (var symbol in candidates)
            {
                if (_erasedThisStroke.Contains(symbol))
                {
                    continue;
                }

                symbolLayer.Remove(symbol);

                ((Cmd_ModifySymbols)_activeCommand!).RegisterRemovedSymbol((MapSymbol)symbol);

                _erasedThisStroke.Add((MapSymbol)symbol);
            }
        }


        public void PaintSelectedSymbol(ISelectable selectedShape, SKColor newColor)
        {
            if (selectedShape != null && selectedShape is MapSymbol ms)
            {
                MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER);

                Cmd_ModifySymbols paintCommand = new(_layer);

                // --- CAPTURE BEFORE ---
                var before = (MapSymbolState)ms.CaptureState();

                // --- APPLY MODIFICATION ---

                if (ms.SymbolDefinition.BaseColorType == MapSymbolBaseColorType.GrayScale)
                {
                    if (_symbolSettings.RandomizeSymbolColors)
                    {
                        newColor = Randomize(newColor);
                    }

                    ms.TintColor = newColor;
                }
                else if (ms.SymbolDefinition.BaseColorType == MapSymbolBaseColorType.RGBMask)
                {
                    // RGB Mask symbols are colored with the values set in the tool, which are
                    // updated by the SymbolMediator
                    ms.CustomSymbolColors[0] = _symbolSettings.RandomizeSymbolColors ? Randomize(_symbolSettings.SymbolColor1.ToSKColor()) : _symbolSettings.SymbolColor1.ToSKColor();
                    ms.CustomSymbolColors[1] = _symbolSettings.RandomizeSymbolColors ? Randomize(_symbolSettings.SymbolColor2.ToSKColor()) : _symbolSettings.SymbolColor2.ToSKColor();
                    ms.CustomSymbolColors[2] = _symbolSettings.RandomizeSymbolColors ? Randomize(_symbolSettings.SymbolColor3.ToSKColor()) : _symbolSettings.SymbolColor3.ToSKColor();
                }

                // --- CAPTURE AFTER ---
                var after = (MapSymbolState)ms.CaptureState();

                paintCommand.RegisterModifiedSymbol(ms, before, after);

                symbolLayer.InvalidateSymbol(ms);

                _commands.Execute(paintCommand!);
            }
        }

        private void PaintSymbolAtLocation(SKPoint worldPos)
        {
            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER);

            // TODO: refactor
            _editor.SelectionService!.SelectAt(_scene.Map, worldPos, 4, false);

            foreach (var symbol in _editor.SelectionService!.SelectedObjects)
            {
                if (symbol != null && symbol is MapSymbol ms)
                {
                    Cmd_ModifySymbols paintCommand = new(_layer);

                    // --- CAPTURE BEFORE ---
                    var before = (MapSymbolState)ms.CaptureState();

                    // --- APPLY MODIFICATION ---
                    ApplyColorToSymbol(ms);

                    // --- CAPTURE AFTER ---
                    var after = (MapSymbolState)ms.CaptureState();

                    paintCommand.RegisterModifiedSymbol(ms, before, after);

                    symbolLayer.InvalidateSymbol(ms);

                    _commands.Execute(paintCommand!);
                }
            }
        }

        private void PaintSymbolsInBrush(SKPoint worldPos, int areaBrushSize)
        {
            float radius = areaBrushSize / 2f;

            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.SYMBOLLAYER);

            var candidates = symbolLayer.QuerySymbolsInRadius(worldPos, radius);

            foreach (var symbol in candidates)
            {
                if (_paintedThisStroke.Contains(symbol))
                {
                    continue;
                }

                var mapSymbol = (MapSymbol)symbol;

                if (mapSymbol.SymbolDefinition.SymbolType != _symbolSettings.SelectedSymbolType)
                {
                    continue;
                }

                // --- CAPTURE BEFORE ---
                var before = (MapSymbolState)mapSymbol.CaptureState();

                // --- APPLY MODIFICATION ---
                ApplyColorToSymbol(mapSymbol);

                // --- CAPTURE AFTER ---
                var after = (MapSymbolState)mapSymbol.CaptureState();

                ((Cmd_ModifySymbols)_activeCommand!).RegisterModifiedSymbol(mapSymbol, before, after);

                _paintedThisStroke.Add(mapSymbol);

                symbolLayer.InvalidateSymbol(mapSymbol);
            }
        }

        private void ApplyColorToSymbol(MapSymbol symbol)
        {
            var def = symbol.SymbolDefinition;

            if (def == null)
            {
                return;
            }

            switch (def.BaseColorType)
            {
                case MapSymbolBaseColorType.GrayScale:
                    symbol.TintColor = _symbolSettings.SymbolColor1.ToSKColor();
                    break;

                case MapSymbolBaseColorType.RGBMask:
                    if (_symbolSettings.RandomizeSymbolColors)
                    {
                        symbol.CustomSymbolColors[0] = Randomize(_symbolSettings.SymbolColor1.ToSKColor());
                        symbol.CustomSymbolColors[1] = Randomize(_symbolSettings.SymbolColor2.ToSKColor());
                        symbol.CustomSymbolColors[2] = Randomize(_symbolSettings.SymbolColor3.ToSKColor());
                    }
                    else
                    {
                        symbol.CustomSymbolColors[0] = _symbolSettings.SymbolColor1.ToSKColor();
                        symbol.CustomSymbolColors[1] = _symbolSettings.SymbolColor2.ToSKColor();
                        symbol.CustomSymbolColors[2] = _symbolSettings.SymbolColor3.ToSKColor();
                    }
                    break;

                case MapSymbolBaseColorType.FullColor:
                    // no-op
                    break;
            }
        }

        private SKColor Randomize(SKColor baseColor)
        {
            float variation = 0.1f;

            byte Clamp(float v) => (byte)Math.Clamp(v, 0, 255);

            return new SKColor(
                Clamp(baseColor.Red * (1f + (float)(_rng.NextDouble() - 0.5) * variation)),
                Clamp(baseColor.Green * (1f + (float)(_rng.NextDouble() - 0.5) * variation)),
                Clamp(baseColor.Blue * (1f + (float)(_rng.NextDouble() - 0.5) * variation)),
                baseColor.Alpha
            );
        }

        private int EstimateCapacity(float brushRadius)
        {
            float avgRadius = GetAverageExclusionRadius();

            float brushArea = MathF.PI * brushRadius * brushRadius;
            float symbolArea = MathF.PI * avgRadius * avgRadius;

            const float packingEfficiency = 0.65f;

            float maxCount = (brushArea / symbolArea) * packingEfficiency;

            // Density scales count
            float target = maxCount * MathF.Pow((float)_symbolSettings.SymbolPlacementDensity, 2.0f);

            return Math.Max(1, (int)MathF.Floor(target));
        }

        private float GetAverageExclusionRadius()
        {
            var primary = _symbolSelectionService.PrimarySelectedSymbol;
            var secondary = _symbolSelectionService.SecondarySelectedSymbols;

            float total = 0f;
            int count = 0;

            if (primary != null)
            {
                total += GetExclusionRadius(primary);
                count++;
            }

            if (secondary != null)
            {
                foreach (var s in secondary)
                {
                    total += GetExclusionRadius(s);
                    count++;
                }
            }

            return count > 0 ? total / count : 1f;
        }

        private float GetExclusionRadius(MapSymbolDefinition def)
        {
            var resource = _imageCache.Get(def.SymbolFilePath);

            float width = 0;
            float height = 0;

            if (resource is BitmapResource br)
            {
                width = br.Image.Width;
                height = br.Image.Height;
            }
            else if (resource is SvgResource vr)
            {
                width = vr.Image.Width;
                height = vr.Image.Height;
            }

            float baseRadius = 0.5f * MathF.Max(width, height);

            baseRadius *= (float)_symbolSettings.SymbolScale;

            return baseRadius;
        }

        private bool IsFarEnough(SKPoint candidate, float radius)
        {
            foreach (var (pos, otherRadius) in _brushPlaced)
            {
                float minDist = radius + otherRadius;
                if (SKPoint.DistanceSquared(candidate, pos) < (minDist * minDist))
                {
                    return false;
                }
            }

            return true;
        }

        private float GetPlacementSpacing()
        {
            // Invert because higher rate = smaller spacing
            return Utilities.Lerp(_maxSpacing / _scene.Camera.Zoom, _minSpacing / _scene.Camera.Zoom, (float)_symbolSettings.SymbolPlacementRate);
        }

        private SKPoint RandomPointInCircle(float radius)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
            float r = radius * MathF.Sqrt((float)_rng.NextDouble());

            return new SKPoint(
                r * MathF.Cos(angle),
                r * MathF.Sin(angle));
        }

        private MapSymbol CreateSymbolInstance(MapSymbolDefinition def, SKPoint worldPos)
        {
            // Ensure resource is loaded (and bounds initialized)
            var resource = _imageCache.Get(def.SymbolFilePath);

            SKRect localBounds = new(-resource!.Bounds.Width / 2, -resource!.Bounds.Height / 2, resource!.Bounds.Width / 2, resource!.Bounds.Height / 2);

            if (localBounds.IsEmpty)
            {
                throw new InvalidOperationException("Map Symbol Local Bounds cannot be empty.");
            }

            MapSymbol newSymbol = new()
            {
                SymbolDefinition = def,

                Name = string.Empty,

                CustomSymbolColors =
                [
                    _symbolSettings.SymbolColor1.ToSKColor(),
                    _symbolSettings.SymbolColor2.ToSKColor(),
                    _symbolSettings.SymbolColor3.ToSKColor()
                ],

                LocalBounds = localBounds,
                Location = worldPos,
                Scale = (float)_symbolSettings.SymbolScale,
                Rotation = _symbolSettings.SymbolRotation,
                Mirror = _symbolSettings.MirrorSymbol,
            };

            newSymbol.UpdateBounds();

            return newSymbol;
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            using (RenderContextScope.Begin(RenderContext))
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolErase)
                {
                    canvas.DrawCircle(
                        world,
                        _symbolSettings.AreaBrushSize / 2f,
                        PaintObjects.CursorCirclePaint);

                    return;
                }

                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolColor && _symbolSettings.UseAreaBrush)
                {
                    canvas.DrawCircle(
                        world,
                        _symbolSettings.AreaBrushSize / 2f,
                        PaintObjects.CursorCirclePaint);

                    return;
                }

                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolPlace && _symbolSettings.UseAreaBrush)
                {
                    canvas.DrawCircle(
                        world,
                        _symbolSettings.AreaBrushSize / 2f,
                        PaintObjects.CursorCirclePaint);

                    return;
                }

                if (_editorState.CurrentDrawingMode == MapDrawingMode.SymbolPlace && !_symbolSettings.UseAreaBrush)
                {
                    var def = _symbolSelectionService.PrimarySelectedSymbol;

                    if (def == null)
                    {
                        return;
                    }

                    // create a temporary symbol instance just for rendering the cursor preview
                    MapSymbol newSymbol = CreateSymbolInstance(def, world);                      
                    newSymbol.Render(canvas, null);
                }
            }            
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SymbolTool()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
