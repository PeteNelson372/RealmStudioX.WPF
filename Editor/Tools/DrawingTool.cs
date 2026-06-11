using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Controls;
using RealmStudioX.WPF.ViewModels.Panels;
using RealmStudioX.WPF.Views.Dialogs;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Application = System.Windows.Application;

namespace RealmStudioX.WPF.Editor.Tools
{
    public sealed class DrawingTool(
        CommandManager commands,
        IAssetProvider assets,
        EditorController editor,
        MapLayer targetLayer,
        MapScene scene,
        EditorState editorState,
        IDrawingSettings drawingSettings) : IToolEditor, IDisposable
    {
        private readonly CommandManager _commands = commands;
        private MapLayer _layer = targetLayer;
        private readonly IAssetProvider _assets = assets;
        private readonly EditorController _editor = editor;
        private readonly MapScene _scene = scene;
        private readonly EditorState _editorState = editorState;
        private  IDrawingSettings _drawingSettings = drawingSettings;

        private SKPoint _lastMouseWorld;
        private long _lastPaintTimestamp;

        private DrawnLine? _currentDrawnline = null;
        private PaintedLine? _currentPaintedLine = null;
        private PreparedBrush? _currentPreparedBrush = null;
        private DrawnRectangle? _currentDrawnRectangle = null;
        private DrawnEllipse? _currentDrawnEllipse = null;
        private DrawnPolygon? _currentDrawnPolygon = null;
        private DrawnTriangle? _currentDrawnTriangle = null;
        private DrawnDiamond? _currentDrawnDiamond = null;
        private DrawnRegularPolygon? _currentDrawnRegularPolygon = null;
        private DrawnArrow? _currentDrawnArrow = null;
        private DrawnFivePointStar? _currentDrawnFivePointStar = null;
        private DrawnSixPointStar? _currentDrawnSixPointStar = null;
        private DrawingErase? _currentDrawingErase = null;

        public PreparedBrush? CurrentPreparedBrush
        {
            get { return _currentPreparedBrush; } 
            set { _currentPreparedBrush = value; }
        }

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
            _lastMouseWorld = state.WorldPoint;

            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                MapLayer drawLayer = _editor.ActiveDrawingLayer != null ?
                _editor.ActiveDrawingLayer : MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);

                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            _currentDrawnline = new DrawnLine
                            {
                                BrushSize = _drawingSettings.LineBrushSize,
                                LineColor = _drawingSettings.DrawingColor.ToSKColor(),
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                DrawTexture = _drawingSettings.SelectedShapeFillType == DrawingFillType.Texture,
                                TextureId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                                Texture = _drawingSettings.CurrentSelectedTextureId != null ? _drawingSettings.CurrentSelectedTexture : null,
                            };

                            _currentDrawnline.Points.Add(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            if (_drawingSettings.SelectedBrushPattern != null
                                && _drawingSettings.SelectedBrushPattern.BrushDefinition != null)
                            {
                                if (_currentPreparedBrush == null)
                                {
                                    // this will only happen if the user starts painting
                                    // without changing brush type, size, or color
                                    _currentPreparedBrush = new PreparedBrush()
                                    {
                                        SourceBrush = _drawingSettings.SelectedBrushPattern.BrushDefinition,
                                        Color = _drawingSettings.DrawingColor.ToSKColor(),
                                        BrushSize = (int)_drawingSettings.LineBrushSize,
                                        BrushSpacing = _drawingSettings.BrushSpacing,
                                    };

                                    AssetInitializer.GetPreparedBrushBitmaps(_currentPreparedBrush);
                                    CurrentPreparedBrush = _currentPreparedBrush;
                                }

                                _currentPaintedLine = new PaintedLine
                                {
                                    Brush = _currentPreparedBrush,
                                    DefaultSpacing = _drawingSettings.SelectedBrushPattern.BrushDefinition.BrushSpacing,
                                    BrushSpacing = _drawingSettings.BrushSpacing,
                                    RandomRotation = _drawingSettings.SelectedBrushPattern.BrushDefinition.RandomRotation,
                                };

                                _currentPaintedLine.Initialize(_editor.Scene!.Map.MapWidth, _editor.Scene!.Map.MapHeight);

                                long now = Environment.TickCount64;

                                _lastPaintTimestamp = now;

                                _currentPaintedLine.AddPoint(state.WorldPoint);
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingRectangle:
                        {
                            _currentDrawnRectangle = new DrawnRectangle
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                RectangleColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingEllipse:
                        {
                            _currentDrawnEllipse = new DrawnEllipse
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                EllipseColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingPolygon:
                        {
                            if (_currentDrawnPolygon == null)
                            {
                                _currentDrawnPolygon = new DrawnPolygon
                                {
                                    PolygonColor = _drawingSettings.DrawingColor.ToSKColor(),
                                    Rotation = _drawingSettings.ShapeRotation,
                                    TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                    TextureScale = _drawingSettings.TextureScale,
                                    BrushSize = _drawingSettings.LineBrushSize,
                                    FillType = _drawingSettings.SelectedShapeFillType,
                                    FillColor = _drawingSettings.FillColor.ToSKColor(),
                                    FillImage = _drawingSettings.CurrentSelectedTexture,
                                    FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                                };
                            }

                            _currentDrawnPolygon.Points.Add(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingRoundedRectangle:
                        {
                            _currentDrawnRectangle = new DrawnRectangle
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                RectangleColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                                DrawRounded = true,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingTriangle:
                        {
                            _currentDrawnTriangle = new DrawnTriangle
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                TriangleColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingRightTriangle:
                        {
                            _currentDrawnTriangle = new DrawnTriangle
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                TriangleColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                                DrawRight = true,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingDiamond:
                        {
                            _currentDrawnDiamond = new DrawnDiamond
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                DiamondColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingPentagon:
                        {
                            _currentDrawnRegularPolygon = new DrawnRegularPolygon
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                Sides = 5,
                                PolygonColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingHexagon:
                        {
                            _currentDrawnRegularPolygon = new DrawnRegularPolygon
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                Sides = 6,
                                PolygonColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingArrow:
                        {
                            _currentDrawnArrow = new DrawnArrow
                            {
                                TopLeft = state.WorldPoint,
                                BottomRight = state.WorldPoint,
                                ArrowColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingFivePointStar:
                        {
                            _currentDrawnFivePointStar = new DrawnFivePointStar
                            {
                                Center = state.WorldPoint,
                                Radius = 0,
                                StarColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingSixPointStar:
                        {
                            _currentDrawnSixPointStar = new DrawnSixPointStar
                            {
                                Center = state.WorldPoint,
                                Radius = 0,
                                StarColor = _drawingSettings.DrawingColor.ToSKColor(),
                                Rotation = _drawingSettings.ShapeRotation,
                                TextureOpacity = (int)(_drawingSettings.TextureOpacity * 255),
                                TextureScale = _drawingSettings.TextureScale,
                                BrushSize = _drawingSettings.LineBrushSize,
                                FillType = _drawingSettings.SelectedShapeFillType,
                                FillColor = _drawingSettings.FillColor.ToSKColor(),
                                FillImage = _drawingSettings.CurrentSelectedTexture,
                                FillImageId = _drawingSettings.CurrentSelectedTextureId ?? string.Empty,
                            };
                        }
                        break;
                    case MapDrawingMode.DrawingStamp:
                        {
                            PlaceStampAtCursor(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingErase:
                        {
                            _currentDrawingErase = new DrawingErase
                            {
                                BrushSize = _drawingSettings.LineBrushSize,
                            };

                            _currentDrawingErase.AddPoint(state.WorldPoint);

                            Cmd_AddDrawnShape cmd = new(drawLayer, _currentDrawingErase);
                            _commands.Execute(cmd);                            
                        }
                        break;
                    case MapDrawingMode.DrawingPixelEdit:
                        {
                            // snapshop the map and get the pixels at the cursor location
                            using SKBitmap mapBitmap = new((int)_editor.Scene!.WorldBounds.Width, (int)_editor.Scene!.WorldBounds.Height);
                            using SKCanvas canvas = new(mapBitmap);

                            _editor.Scene.RenderForExport(canvas);

                            // Clone the specified area
                            SKBitmap clonedBitmap = Utilities.ExtractRegion(mapBitmap, (int)state.WorldPoint.X - 16, (int)state.WorldPoint.Y - 16, 32, 32);

                            PixelEditorViewModel vm = new(_editor);

                            PixelEditDialog dlg = new(vm)
                            {
                                Owner = Application.Current.MainWindow
                            };

                            vm.WorkingBitmap = clonedBitmap;
                            vm.EditLocation = new SKPoint((int)state.WorldPoint.X - 16, (int)state.WorldPoint.Y - 16);

                            bool? result = dlg.ShowDialog();

                            if (result != null && result == true)
                            {
                                // commit the changes
                                CommitPixelEdits((IPixelEditSettings)vm);
                            }
                        }
                        break;
                }

                drawLayer.InvalidateAllTiles();
            }

            if (state.Button == EditorMouseButton.Right)
            {
                if (_editorState.CurrentDrawingMode == MapDrawingMode.DrawingPolygon)
                {
                    // commit the polygon
                    if (_editor.ActiveDrawingLayer != null && _currentDrawnPolygon != null)
                    {
                        _currentDrawnPolygon.Points.Add(state.WorldPoint);

                        Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnPolygon);
                        _commands.Execute(cmd);
                    }
                    _currentDrawnPolygon = null;
                }
            }
        }

        public void OnMouseMove(PointerState state)
        {
            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                MapLayer drawLayer = _editor.ActiveDrawingLayer != null ?
                _editor.ActiveDrawingLayer : MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);

                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            _currentDrawnline?.Points.Add(state.WorldPoint);
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            if (_currentPaintedLine != null)
                            {
                                long now = Environment.TickCount64;

                                float deltaTime = (now - _lastPaintTimestamp) / 1000f;

                                _lastPaintTimestamp = now;

                                _currentPaintedLine.AddPoint(state.WorldPoint);
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingRectangle:
                        {
                            if (_currentDrawnRectangle != null)
                            {
                                SKRect rect = new(_currentDrawnRectangle.TopLeft.X, _currentDrawnRectangle.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);

                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the rectangle a square
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }

                                _currentDrawnRectangle.BottomRight = new SKPoint(rect.Right, rect.Bottom);
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingEllipse:
                        {
                            if (_currentDrawnEllipse != null)
                            {
                                SKRect rect = new(_currentDrawnEllipse.TopLeft.X, _currentDrawnEllipse.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);
                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the ellipse a circle
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }
                                _currentDrawnEllipse.BottomRight = new SKPoint(rect.Right, rect.Bottom);
                            }
                            break;
                        }
                    case MapDrawingMode.DrawingPolygon:
                        {
                            _editor.RequestRedraw();
                        }
                        break;
                    case MapDrawingMode.DrawingRoundedRectangle:
                        {
                            if (_currentDrawnRectangle != null)
                            {
                                SKRect rect = new(_currentDrawnRectangle.TopLeft.X, _currentDrawnRectangle.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);

                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the rectangle a square
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }

                                _currentDrawnRectangle.BottomRight = new SKPoint(rect.Right, rect.Bottom);
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingTriangle:
                        {
                            if (_currentDrawnTriangle != null)
                            {
                                _currentDrawnTriangle.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingRightTriangle:
                        {
                            if (_currentDrawnTriangle != null)
                            {
                                _currentDrawnTriangle.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingDiamond:
                        {
                            if (_currentDrawnDiamond != null)
                            {
                                _currentDrawnDiamond.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingPentagon:
                        {
                            if (_currentDrawnRegularPolygon != null)
                            {
                                _currentDrawnRegularPolygon.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingHexagon:
                        {
                            if (_currentDrawnRegularPolygon != null)
                            {
                                _currentDrawnRegularPolygon.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingArrow:
                        {
                            if (_currentDrawnArrow != null)
                            {
                                _currentDrawnArrow.BottomRight = state.WorldPoint;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingFivePointStar:
                        {
                            if (_currentDrawnFivePointStar != null)
                            {
                                float radius = SKPoint.Distance(_currentDrawnFivePointStar.Center, state.WorldPoint);
                                _currentDrawnFivePointStar.Radius = radius;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingSixPointStar:
                        {
                            if (_currentDrawnSixPointStar != null)
                            {
                                float radius = SKPoint.Distance(_currentDrawnSixPointStar.Center, state.WorldPoint);
                                _currentDrawnSixPointStar.Radius = radius;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingErase:
                        {
                            if (_currentDrawingErase != null)
                            {
                                SKRect oldEraseBounds = _currentDrawingErase.Bounds;
                                
                                _currentDrawingErase.AddPoint(state.WorldPoint);
                                
                                SKRect newEraseBounds = _currentDrawingErase.Bounds;

                                drawLayer.UpdateShapeTiles(_currentDrawingErase, oldEraseBounds, newEraseBounds);
                                drawLayer.InvalidateAllTiles();
                            }
                            break;
                        }
                }

                
            }

            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseUp(PointerState state)
        {
            bool ctrl = (state.Modifiers & InputModifiers.Control) == InputModifiers.Control;
            bool shift = (state.Modifiers & InputModifiers.Shift) == InputModifiers.Shift;

            if (state.Button == EditorMouseButton.Left)
            {
                MapLayer drawLayer = _editor.ActiveDrawingLayer != null ?
                        _editor.ActiveDrawingLayer : MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);

                switch (_editorState.CurrentDrawingMode)
                {
                    case MapDrawingMode.DrawingLine:
                        {
                            if (_currentDrawnline != null)
                            {
                                _currentDrawnline.Points.Add(state.WorldPoint);

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnline);
                                    _commands.Execute(cmd);
                                }

                                _currentDrawnline = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingPaint:
                        {
                            if (_currentPaintedLine != null)
                            {
                                if (state.WorldPoint != _currentPaintedLine.Points[^1])
                                {
                                    _currentPaintedLine.AddPoint(state.WorldPoint);
                                }

                                _currentPaintedLine.FinalizeStroke();

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentPaintedLine);
                                    _commands.Execute(cmd);
                                }

                                _currentPaintedLine = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingRectangle:
                        {
                            if (_currentDrawnRectangle != null)
                            {
                                SKRect rect = new(_currentDrawnRectangle.TopLeft.X, _currentDrawnRectangle.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);

                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the rectangle a square
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }

                                _currentDrawnRectangle.BottomRight = new SKPoint(rect.Right, rect.Bottom);

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnRectangle);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnRectangle = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingEllipse:
                        {
                            if (_currentDrawnEllipse != null)
                            {
                                SKRect rect = new(_currentDrawnEllipse.TopLeft.X, _currentDrawnEllipse.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);

                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the ellipse a circle
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }

                                _currentDrawnEllipse.BottomRight = new SKPoint(rect.Right, rect.Bottom);

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnEllipse);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnEllipse = null;
                            }
                            break;
                        }
                    case MapDrawingMode.DrawingPolygon:
                        {
                            // no op
                        }
                        break;
                    case MapDrawingMode.DrawingRoundedRectangle:
                        {
                            if (_currentDrawnRectangle != null)
                            {
                                SKRect rect = new(_currentDrawnRectangle.TopLeft.X, _currentDrawnRectangle.TopLeft.Y,
                                    state.WorldPoint.X, state.WorldPoint.Y);

                                if (ctrl)
                                {
                                    // if the ctrl key is pressed, make the rectangle a square
                                    float size = Math.Max(rect.Width, rect.Height);
                                    rect = new SKRect(rect.Left, rect.Top, rect.Left + size, rect.Top + size);
                                }

                                _currentDrawnRectangle.BottomRight = new SKPoint(rect.Right, rect.Bottom);

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnRectangle);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnRectangle = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingTriangle:
                        {
                            if (_currentDrawnTriangle != null)
                            {
                                _currentDrawnTriangle.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnTriangle);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnTriangle = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingRightTriangle:
                        {
                            if (_currentDrawnTriangle != null)
                            {
                                _currentDrawnTriangle.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnTriangle);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnTriangle = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingDiamond:
                        {
                            if (_currentDrawnDiamond != null)
                            {
                                _currentDrawnDiamond.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnDiamond);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnDiamond = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingPentagon:
                        {
                            if (_currentDrawnRegularPolygon != null)
                            {
                                _currentDrawnRegularPolygon.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnRegularPolygon);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnRegularPolygon = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingHexagon:
                        {
                            if (_currentDrawnRegularPolygon != null)
                            {
                                _currentDrawnRegularPolygon.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnRegularPolygon);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnRegularPolygon = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingArrow:
                        {
                            if (_currentDrawnArrow != null)
                            {
                                _currentDrawnArrow.BottomRight = state.WorldPoint;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnArrow);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnArrow = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingFivePointStar:
                        {
                            if (_currentDrawnFivePointStar != null)
                            {
                                float radius = SKPoint.Distance(_currentDrawnFivePointStar.Center, state.WorldPoint);
                                _currentDrawnFivePointStar.Radius = radius;

                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnFivePointStar);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnFivePointStar = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingSixPointStar:
                        {
                            if (_currentDrawnSixPointStar != null)
                            {
                                float radius = SKPoint.Distance(_currentDrawnSixPointStar.Center, state.WorldPoint);
                                _currentDrawnSixPointStar.Radius = radius;
                                if (_editor.ActiveDrawingLayer != null)
                                {
                                    Cmd_AddDrawnShape cmd = new(_editor.ActiveDrawingLayer, _currentDrawnSixPointStar);
                                    _commands.Execute(cmd);
                                }
                                _currentDrawnSixPointStar = null;
                            }
                        }
                        break;
                    case MapDrawingMode.DrawingErase:
                        {
                            _currentDrawingErase = null;
                        }
                        break;
                }

                drawLayer.InvalidateAllTiles();
            }

            _lastMouseWorld = state.WorldPoint;
        }

        public void OnMouseDoubleClick(PointerState state)
        {
            // no action
        }

        public void OnMouseWheel(PointerState state)
        {
            // no action
        }

        public void UpdateDrawingParameters(IDrawingSettings newSettings)
        {
            _drawingSettings = newSettings;
        }

        public void PlaceStampAtCursor(SKPoint currentCursorPoint)
        {
            if (_drawingSettings.StampImage != null &&
                _drawingSettings.StampImage.Width > 0 &&
                _drawingSettings.StampImage.Height > 0)
            {

                SKRect r = new(
                    (float)(currentCursorPoint.X - _editor.Scene!.WorldBounds.Width * _drawingSettings.StampScale / 2f),
                    (float)(currentCursorPoint.Y - _editor.Scene!.WorldBounds.Height * _drawingSettings.StampScale / 2f),
                    (float)(currentCursorPoint.X + _editor.Scene!.WorldBounds.Width * _drawingSettings.StampScale / 2f),
                    (float)(currentCursorPoint.Y + _editor.Scene!.WorldBounds.Height * _drawingSettings.StampScale / 2f));

                using SKBitmap resized = Utilities.ResizeBitmap(_drawingSettings.StampImage.ToSKBitmap(), (int)r.Width, (int)r.Height);

                using SKBitmap stampBitmap = Utilities.SetBitmapOpacity(resized, _drawingSettings.StampOpacity);

                SKRect bounds = new(currentCursorPoint.X - (stampBitmap.Width / 2), currentCursorPoint.Y - (stampBitmap.Height / 2),
                    currentCursorPoint.X + (stampBitmap.Width / 2), currentCursorPoint.Y + (stampBitmap.Height / 2));

                DrawnStamp drawnStamp = new()
                {
                    TopLeft = currentCursorPoint,
                    Opacity = _drawingSettings.StampOpacity,
                    Rotation = (int)_drawingSettings.StampRotation,
                    Scale = _drawingSettings.StampScale,
                    StampImage = SKImage.FromBitmap(stampBitmap),
                    StampPath = _drawingSettings.SelectedStampPath ?? string.Empty,
                    Bounds = bounds,
                    IsSelected = false,
                };

                MapLayer drawLayer = _editor.ActiveDrawingLayer != null ?
                        _editor.ActiveDrawingLayer : MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);

                Cmd_AddDrawnShape cmd = new(drawLayer, drawnStamp);
                _commands.Execute(cmd);
            }
        }

        internal void CommitPixelEdits(IPixelEditSettings pixelEditSettings)
        {
            List<PixelEdit> pixelEdits = pixelEditSettings.PixelEdits;

            DrawnPixelEdits edits = new()
            {
                MapPixelEdits = pixelEdits,
                Bounds = new SKRect(pixelEditSettings.EditLocation.X, pixelEditSettings.EditLocation.Y,
                    pixelEditSettings.EditLocation.X + 32, pixelEditSettings.EditLocation.Y + 32)
            };

            MapLayer drawingLayer = MapBuilder.GetMapLayerByIndex(_editor.Scene!.Map, MapBuilder.DRAWINGLAYER);
            Cmd_AddDrawnShape cmd = new(drawingLayer, edits);
            _commands.Execute(cmd);
        }

        public void RenderOverlay(SKCanvas canvas, SKPoint world)
        {

            _currentDrawnline?.Render(canvas);

            _currentPaintedLine?.Render(canvas);

            _currentDrawnRectangle?.Render(canvas);

            _currentDrawnEllipse?.Render(canvas);

            _currentDrawnPolygon?.Points.Add(world);
            _currentDrawnPolygon?.Render(canvas);
            _currentDrawnPolygon?.Points.RemoveAt(_currentDrawnPolygon.Points.Count - 1);

            _currentDrawnTriangle?.Render(canvas);

            _currentDrawnDiamond?.Render(canvas);

            _currentDrawnRegularPolygon?.Render(canvas);

            _currentDrawnArrow?.Render(canvas);

            _currentDrawnFivePointStar?.Render(canvas);

            _currentDrawnSixPointStar?.Render(canvas);

            // draw the cursor
            if (_editorState.CurrentDrawingMode == MapDrawingMode.DrawingPaint
                || _editorState.CurrentDrawingMode == MapDrawingMode.DrawingLine
                || _editorState.CurrentDrawingMode == MapDrawingMode.DrawingErase)
            {
                var brushRadius = _drawingSettings.LineBrushSize / 2;

                canvas.DrawCircle(
                    world,
                    brushRadius,
                    PaintObjects.CursorCirclePaint);
            }
            else if (_editorState.CurrentDrawingMode == MapDrawingMode.DrawingPixelEdit)
            {
                // draw a square cursor for pixel editing
                SKRect rect = new(world.X - 32, world.Y - 32, world.X + 32, world.Y + 32);
                canvas.DrawRect(rect, PaintObjects.CursorSquarePaint);
            }
            else if (_editorState.CurrentDrawingMode == MapDrawingMode.DrawingStamp)
            {
                // draw a rectangle with the size of the scaled stamp
                if (_drawingSettings.StampImage != null)
                {
                    SKRect r = new(
                        (float)(world.X - _editor.Scene!.WorldBounds.Width * _drawingSettings.StampScale / 2f),
                        (float)(world.Y - _editor.Scene!.WorldBounds.Height * _drawingSettings.StampScale / 2f),
                        (float)(world.X + _editor.Scene!.WorldBounds.Width * _drawingSettings.StampScale / 2f),
                        (float)(world.Y + _editor.Scene!.WorldBounds.Height * _drawingSettings.StampScale / 2f));

                    canvas.DrawRect(r, PaintObjects.CursorSquarePaint);
                }
            }
        }

        private void Dispose(bool disposing)
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
        // ~MapPathTool()
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
