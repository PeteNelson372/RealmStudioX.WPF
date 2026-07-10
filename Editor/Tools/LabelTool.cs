using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Panels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows.Input;
using CommandManager = RealmStudioX.Core.CommandManager;

namespace RealmStudioX.WPF.Editor.Tools
{
    internal class LabelTool(
        CommandManager commands,
        IAssetProvider assets,
        MapLayer targetLayer,
        MapScene scene,
        EditorController editor,
        FontManager fontManager,
        IRedrawRequester redraw,
        ILabelSettings labelSettings) : IToolEditor, IKeyHandler, IDisposable
    {
        // -------------------------------------------------
        // Dependencies
        // -------------------------------------------------

        private readonly CommandManager _commands = commands;
        private readonly MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly MapScene _scene = scene;
        private readonly EditorController _editor = editor;
        private readonly FontManager _fontManager = fontManager;
        private readonly ILabelSettings _labelSettings = labelSettings;
        private readonly IRedrawRequester _redraw = redraw;

        private LabelEditSession? _editSession;
        public LabelEditSession? EditSession => _editSession;
        public bool IsEditing => _editSession != null;

        private bool disposedValue;

        private SKColor _fontColor = Color.FromArgb(61, 53, 30).ToSKColor();
        private FontStyleModel _fontStyleModel = new();
        private float _glowStrength;
        private SKColor _glowColor = SKColors.White;
        private SKColor _outlineColor = Color.FromArgb(161, 214, 202, 171).ToSKColor();
        private float _outlineWidth;
        private float _rotation;

        private MapLabel? _editingOriginal;

        // caret blinking
        private readonly System.Windows.Forms.Timer _uiTimer = new();
        private DateTime _lastTick;
        private double _caretTime;
        private bool _caretVisible = true;

        public void Activate()
        {
            _uiTimer!.Interval = 16; // ~60 FPS
            _uiTimer.Tick += (s, e) =>
            {
                var now = DateTime.UtcNow;
                double deltaSeconds = (now - _lastTick).TotalSeconds;
                _lastTick = now;

                Update(deltaSeconds);

                _redraw.RequestRedraw();
            };

            _lastTick = DateTime.UtcNow;
            _uiTimer.Start();
        }

        public void Cancel()
        {
            if (_editingOriginal != null)
            {
                _editingOriginal.IsEditing = false;
            }

            _editSession = null;
            _editingOriginal = null;

            _uiTimer?.Stop();

            _redraw.RequestRedraw();
        }

        public void Deactivate()
        {
            _uiTimer?.Stop();
        }

        public void OnMouseDown(PointerState state)
        {
            if (!_uiTimer.Enabled)
            {
                Activate();
            }

            if (_editSession != null)
            {
                CommitLabel();
                _editSession = null;
            }

            if (state.Button == EditorMouseButton.Left)
            {
                if (_editor.CurrentDrawingMode == MapDrawingMode.DrawLabel)
                {
                    _editSession = new LabelEditSession
                    {
                        Text = string.Empty,
                        Location = state.WorldPoint,
                        Rotation = _labelSettings.Rotation,
                        Scale = _labelSettings.LabelScale,
                        Mirror = false,
                        FontColor = _labelSettings.LabelColor.ToSKColor(),
                        HasOutline = false,     // this value is not used presently; if the outline width > 0, it is rendered
                        OutlineWidth = _labelSettings.OutlineWidth,
                        OutlineColor = _labelSettings.OutlineColor.ToSKColor(),
                        HasGlow = false,        // this value is not used presently; if the glow strength > 0, it is rendered
                        GlowStrength = _labelSettings.GlowStrength,
                        GlowColor = _labelSettings.GlowColor.ToSKColor(),
                        CurvePath = null,
                        FontStyle = _labelSettings.FontStyle.Clone(),
                    };

                    Redraw();
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
        }

        public void OnMouseUp(PointerState state)
        {
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public bool OnKeyDown(Key key)
        {
            if (_editSession == null)
            {
                return false;
            }

            if (key == Key.Enter)
            {
                CommitLabel();
                return true;
            }

            if (key == Key.Escape)
            {
                Cancel();
                return true;
            }

            if (key == Key.Home)
            {
                _editSession.CaretIndex = 0;
                return true;
            }

            if (key == Key.End)
            {
                _editSession.CaretIndex = _editSession.Text.Length;
                return true;
            }

            if (key == Key.Back && _editSession.CaretIndex > 0)
            {
                _editSession.Text = _editSession.Text.Remove(_editSession.CaretIndex - 1, 1);
                _editSession.CaretIndex--;

                SyncEditingLabelToModel();

                ((EditorController)_redraw).Scene!.TransformWidget.UpdateWidgetGeometry();

                _redraw.RequestRedraw();

                return true;
            }

            if (key == Key.Left)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    _editSession.CaretIndex = MoveToPreviousWord(_editSession.Text, _editSession.CaretIndex);
                }
                else if (_editSession.CaretIndex > 0)
                {
                    _editSession.CaretIndex--;
                }

                SyncEditingLabelToModel();

                ((EditorController)_redraw).Scene!.TransformWidget.UpdateWidgetGeometry();

                _redraw.RequestRedraw();

                return true;
            }

            if (key == Key.Right)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    _editSession.CaretIndex = MoveToNextWord(_editSession.Text, _editSession.CaretIndex);
                }
                else if (_editSession.CaretIndex < _editSession.Text.Length)
                {
                    _editSession.CaretIndex++;
                }

                SyncEditingLabelToModel();

                ((EditorController)_redraw).Scene!.TransformWidget.UpdateWidgetGeometry();

                _redraw.RequestRedraw();

                return true;
            }

            return false;
        }

        public bool OnKeyPress(char c)
        {
            if (_editSession == null)
            {
                return false;
            }

            if (char.IsControl(c))
            {
                return false;
            }

            _editSession.Text = _editSession.Text.Insert(_editSession.CaretIndex, c.ToString());
            _editSession.CaretIndex++;

            SyncEditingLabelToModel();

            ((EditorController)_redraw).Scene!.TransformWidget.UpdateWidgetGeometry();

            _redraw.RequestRedraw();
            return true;
        }

        private void SyncEditingLabelToModel()
        {
            if (_editSession == null || _editingOriginal == null)
            {
                return;
            }

            var typeface = _fontManager.GetTypeface(_editSession.FontStyle);
            using var font = new SKFont(typeface, _editSession.FontStyle.Size);

            font.MeasureText(_editSession.Text, out SKRect bounds);

            float centerOffsetY = (bounds.Top + bounds.Bottom) * 0.5f;

            var label = _editingOriginal;

            label.Text = _editSession.Text;
            label.Location = _editSession.Location;
            label.Rotation = _editSession.Rotation;
            label.Scale = _editSession.Scale;
            label.Mirror = _editSession.Mirror;
            label.FontStyle = _editSession.FontStyle.Clone();
            label.FontColor = _editSession.FontColor;

            label.HasOutline = _editSession.HasOutline;
            label.OutlineWidth = _editSession.OutlineWidth;
            label.OutlineColor = _editSession.OutlineColor;

            label.HasGlow = _editSession.HasGlow;
            label.GlowStrength = _editSession.GlowStrength;
            label.GlowColor = _editSession.GlowColor;

            label.CurvePath = _editSession.CurvePath;

            label.BoundsModified = true;
        }

        private static int MoveToNextWord(string text, int index)
        {
            int len = text.Length;

            if (index >= len)
                return len;

            // Step 1: skip current word (if inside one)
            while (index < len && IsWordChar(text[index]))
                index++;

            // Step 2: skip separators
            while (index < len && !IsWordChar(text[index]))
                index++;

            return index;
        }

        private static int MoveToPreviousWord(string text, int index)
        {
            if (index <= 0)
                return 0;

            index--; // move left first

            // Step 1: skip separators
            while (index > 0 && !IsWordChar(text[index]))
                index--;

            // Step 2: skip word characters
            while (index > 0 && IsWordChar(text[index - 1]))
                index--;

            return index;
        }

        public void BeginEdit(MapLabel label, SKPoint clickPoint)
        {
            var typeface = _fontManager.GetTypeface(label.FontStyle);
            using var font = new SKFont(typeface, label.FontStyle.Size);

            int caret = label.GetCaretIndex(clickPoint, font);

            _editSession = new LabelEditSession
            {
                Text = label.Text,
                CaretIndex = caret,
                Location = label.Location,
                FontStyle = label.FontStyle.Clone(),
                FontColor = label.FontColor,
                Rotation = label.Rotation,
                Scale = label.Scale,
                Mirror = label.Mirror,
                HasOutline = label.HasOutline,
                OutlineWidth = label.OutlineWidth,
                OutlineColor = label.OutlineColor,
                HasGlow = label.HasGlow,
                GlowStrength = label.GlowStrength,
                GlowColor = label.GlowColor,
                CurvePath = label.CurvePath
            };

            _editingOriginal = label;

            // hide original
            label.IsEditing = true;
        }

        public void EnsureEditCommitted()
        {
            if (_editSession != null)
            {
                CommitLabel();
            }
        }

        private void CommitLabel()
        {
            if (_editSession == null)
                return;

            if (!string.IsNullOrWhiteSpace(_editSession.Text))
            {
                var anchor = _editSession.Location; // already center-based

                if (_editingOriginal != null)
                {
                    var label = _editingOriginal;

                    label.Text = _editSession.Text;
                    label.Location = anchor;

                    label.Rotation = _editSession.Rotation;
                    label.Scale = _editSession.Scale;
                    label.Mirror = _editSession.Mirror;

                    label.FontStyle = _editSession.FontStyle.Clone();
                    label.FontColor = _editSession.FontColor;

                    label.HasOutline = _editSession.HasOutline;
                    label.OutlineWidth = _editSession.OutlineWidth;
                    label.OutlineColor = _editSession.OutlineColor;

                    label.HasGlow = _editSession.HasGlow;
                    label.GlowStrength = _editSession.GlowStrength;
                    label.GlowColor = _editSession.GlowColor;

                    label.CurvePath = (_editor.LayoutTool?.LayoutPath != null && _editor.LayoutTool?.LayoutPath.PointCount > 3)
                        ? new SKPath(_editor.LayoutTool?.LayoutPath)
                        : _editSession.CurvePath;

                    if (label.CurvePath != null && label.CurvePath.PointCount > 3)
                    {
                        var typeface = _fontManager.GetTypeface(label.FontStyle);
                        using var font = new SKFont(typeface, label.FontStyle.Size);

                        var center = ComputeCurveTextCenter(label.Text, font, label.CurvePath);

                        if (!float.IsNaN(center.X) && !float.IsNaN(center.Y))
                        {
                            label.Location = center;
                        }
                    }

                    label.BoundsModified = true;
                    label.IsEditing = false;
                }
                else
                {
                    var label = new MapLabel
                    {
                        Text = _editSession.Text,
                        Location = anchor,

                        Rotation = _editSession.Rotation,
                        Scale = _editSession.Scale,
                        Mirror = _editSession.Mirror,

                        FontStyle = _editSession.FontStyle.Clone(),
                        FontColor = _editSession.FontColor,

                        HasOutline = _editSession.HasOutline,
                        OutlineWidth = _editSession.OutlineWidth,
                        OutlineColor = _editSession.OutlineColor,

                        HasGlow = _editSession.HasGlow,
                        GlowStrength = _editSession.GlowStrength,
                        GlowColor = _editSession.GlowColor,

                        CurvePath = (_editor.LayoutTool?.LayoutPath != null && _editor.LayoutTool?.LayoutPath.PointCount > 3)
                            ? new SKPath(_editor.LayoutTool?.LayoutPath)
                            : _editSession.CurvePath,

                        BoundsModified = true
                    };

                    if (label.CurvePath != null && label.CurvePath.PointCount > 3)
                    {
                        var typeface = _fontManager.GetTypeface(label.FontStyle);
                        using var font = new SKFont(typeface, label.FontStyle.Size);

                        var center = ComputeCurveTextCenter(label.Text, font, label.CurvePath);

                        if (!float.IsNaN(center.X) && !float.IsNaN(center.Y))
                        {
                            label.Location = center;
                        }
                    }

                    var layer = MapBuilder.GetMapLayerByIndex(_scene.Map, MapBuilder.LABELLAYER);
                    Cmd_ModifyLabels cmd = new(layer);

                    cmd.RegisterNewLabel(label);

                    _commands.Execute(cmd);
                }
            }

            _editSession = null;
            _editingOriginal = null;

            _editor.LayoutTool?.ClearLayoutPath();

            _redraw.RequestRedraw();
        }

        private SKPoint ComputeCurveTextCenter(string text, SKFont font, SKPath path)
        {
            using var blob = SKTextBlob.CreatePathPositioned(
                text,
                font,
                path,
                SKTextAlign.Center,
                SKPoint.Empty
            );

            if (blob == null)
                return SKPoint.Empty;

            var b = blob.Bounds;

            return new SKPoint(
                b.MidX,
                b.MidY
            );
        }

        public void ApplyGeneratedName(string generatedName)
        {
            if (_editSession != null && !string.IsNullOrEmpty(generatedName))
            {
                _editSession.Text = generatedName;
                SyncEditingLabelToModel();
                _redraw.RequestRedraw();
            }
        }

        public static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '\'';
        }

        public void Redraw()
        {
            _redraw.RequestRedraw();
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {
            if (_editSession != null)
            {
                DrawEditingLabel(canvas, _editSession);
            }
        }

        void Update(double deltaSeconds)
        {
            if (_editSession == null)
            {
                return;
            }

            _caretTime += deltaSeconds;

            if (_caretTime >= 0.5)
            {
                _caretTime = 0;
                _caretVisible = !_caretVisible;
            }
        }

        private void DrawEditingLabel(SKCanvas canvas, LabelEditSession edit)
        {
            var typeface = _fontManager.GetTypeface(edit.FontStyle);
            using var font = new SKFont(typeface, edit.FontStyle.Size);

            using var paint = new SKPaint
            {
                Color = edit.FontColor,
                IsAntialias = true
            };

            // -------------------------------------------------
            // Match MapLabel bounds model
            // -------------------------------------------------
            SKRect bounds;

            if (!string.IsNullOrEmpty(edit.Text))
            {
                font.MeasureText(edit.Text, out bounds);
            }
            else
            {
                font.MeasureText("Mg", out bounds);
            }

            // APPLY SAME INFLATION AS MapLabel
            float inflateX = MathF.Max(5, bounds.Width * 0.01f);
            float inflateY = MathF.Max(5, bounds.Height * 0.01f);

            bounds.Inflate(inflateX, inflateY);

            // -------------------------------------------------
            // CENTER-CENTER anchoring (matches MapLabel)
            // -------------------------------------------------
            float centerOffsetX = (bounds.Left + bounds.Right) * 0.5f;
            float centerOffsetY = (bounds.Top + bounds.Bottom) * 0.5f;

            float x = edit.Location.X - centerOffsetX;
            float y = edit.Location.Y - centerOffsetY;

            // -------------------------------------------------
            // Draw text
            // -------------------------------------------------
            canvas.DrawText(edit.Text, x, y, font, paint);

            // -------------------------------------------------
            // Caret
            // -------------------------------------------------
            if (edit.CaretIndex < 0)
                edit.CaretIndex = 0;

            if (edit.CaretIndex > edit.Text.Length)
                edit.CaretIndex = edit.Text.Length;

            string beforeCaret = edit.Text[..edit.CaretIndex];

            float caretX = x + font.MeasureText(beforeCaret);

            float caretTop = y + bounds.Top;
            float caretBottom = y + bounds.Bottom;

            if (_caretVisible)
            {
                canvas.DrawLine(
                    caretX,
                    caretTop,
                    caretX,
                    caretBottom,
                    PaintObjects.LabelEditCaretPaint);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _uiTimer.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LabelTool()
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