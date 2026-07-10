using OpenTK;
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using SkiaSharp;


namespace RealmStudioX.WPF.Editor.Services
{
    public class AlignmentService
    {
        private readonly EditorController _editor;
        private readonly SelectionService _selectionService;
        private readonly CommandService _commands;

        public AlignmentService(EditorController editor, SelectionService selection, CommandService commands)
        {
            _editor = editor;
            _selectionService = selection;
            _commands = commands;
        }

        private SKPath? _alignmentPath = null;

        public SKPath? AlignmentPath
        {
            get { return _alignmentPath; }
            set { _alignmentPath = value; }
        }

        public void AlignLeft()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float left = _selectionService.PrimarySelection.Bounds.Left;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable)
                    {
                        UpdatePosition(alignable, left, AlignmentType.Left);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);
        
            }
            catch { }
        }

        public void AlignCenter()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float center = _selectionService.PrimarySelection.Bounds.MidX;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable && alignable != _selectionService.PrimarySelection)
                    {
                        UpdatePosition(alignable, center, AlignmentType.Center);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

            }
            catch { }
        }

        public void AlignRight()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float right = _selectionService.PrimarySelection.Bounds.Right;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable && alignable != _selectionService.PrimarySelection)
                    {
                        UpdatePosition(alignable, right, AlignmentType.Right);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

            }
            catch { }
        }

        public void AlignTop()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float top = _selectionService.PrimarySelection.Bounds.Top;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable && alignable != _selectionService.PrimarySelection)
                    {
                        UpdatePosition(alignable, top, AlignmentType.Top);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

            }
            catch { }
        }

        public void AlignMiddle()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float middle = _selectionService.PrimarySelection.Bounds.MidY;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable && alignable != _selectionService.PrimarySelection)
                    {
                        UpdatePosition(alignable, middle, AlignmentType.Middle);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

            }
            catch { }
        }

        public void AlignBottom()
        {
            if (_selectionService.PrimarySelection == null || _selectionService.SelectionCount < 2)
            {
                return;
            }

            Cmd_ModifyObjects cmd = BeginModification();

            float bottom = _selectionService.PrimarySelection.Bounds.Bottom;

            try
            {
                for (int i = 0; i < _selectionService.SelectionCount; i++)
                {
                    MapComponent2D alignable = (MapComponent2D)_selectionService.SelectedObjects[i];

                    if (alignable != null && alignable is IAlignable && alignable != _selectionService.PrimarySelection)
                    {
                        UpdatePosition(alignable, bottom, AlignmentType.Bottom);
                    }
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

            }
            catch { }
        }

        public void AlignToPath()
        {
            MessageBox.Show("align to path");


        }

        public void DistributeAlongPath()
        {
            MessageBox.Show("distribute along path");

            if (_selectionService.SelectionCount < 2)
            {
                return;
            }
        }

        private void UpdatePosition(MapComponent2D component, float position, AlignmentType alignment)
        {
            if (component is MapSymbol ms)
            {
                UpdateSymbolLocation(ms, position, alignment);
                return;
            }

            if (component is PlacedMapBox box)
            {
                UpdateBoxLocation(box, position, alignment);
                return;
            }

            if (component is MapLabel label)
            {
                UpdateLabelLocation(label, position, alignment);
                return;
            }

            if (component is IDrawnMapComponent dmc)
            {
                UpdateDrawnComponentLocation(dmc, position, alignment);
                return;
            }
        }


        private void UpdateDrawnComponentLocation(IDrawnMapComponent dmc, float position, AlignmentType alignment)
        {
            if (dmc is IRectangularShape rs)
            {
                UpdateTopLeftBottomRight(rs, position, alignment);
                return;
            }

            if (dmc is ICenterRadiusShape cs)
            {
                UpdateCenterRadius(cs, position, alignment);
                return;
            }

            if (dmc is IPointListShape pls)
            {
                UpdatePointList(pls, position, alignment);
                return;
            }

            if (dmc is IPositionImageShape ims)
            {
                UpdatePositionImage(ims, position, alignment);
                return;
            }
        }

        private void UpdateTopLeftBottomRight(IRectangularShape shape, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        Translate(shape, position - shape.TopLeft.X, 0);
                    }
                    break;

                case AlignmentType.Center:
                    {
                        float currentCenter =
                            (shape.TopLeft.X + shape.BottomRight.X) / 2f;

                        Translate(shape, position - currentCenter, 0);
                    }
                    break;

                case AlignmentType.Right:
                    {
                        Translate(shape, position - shape.BottomRight.X, 0);
                    }
                    break;

                case AlignmentType.Top:
                    {
                        Translate(shape, 0, position - shape.TopLeft.Y);
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        float currentMiddle =
                            (shape.TopLeft.Y + shape.BottomRight.Y) / 2f;

                        Translate(shape, 0, position - currentMiddle);
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        Translate(shape, 0, position - shape.BottomRight.Y);
                    }
                    break;
            }
        }

        private static void Translate(IRectangularShape shape, float dx, float dy)
        {
            shape.TopLeft = new SKPoint(
                shape.TopLeft.X + dx,
                shape.TopLeft.Y + dy);

            shape.BottomRight = new SKPoint(
                shape.BottomRight.X + dx,
                shape.BottomRight.Y + dy);
        }

        private void UpdateCenterRadius(ICenterRadiusShape shape, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        Translate(shape, position - (shape.Center.X - shape.Radius), 0);
                    }
                    break;

                case AlignmentType.Center:
                    {
                        Translate(shape, position - shape.Center.X, 0);
                    }
                    break;

                case AlignmentType.Right:
                    {
                        Translate(shape, position - (shape.Center.X + shape.Radius), 0);
                    }
                    break;

                case AlignmentType.Top:
                    {
                        Translate(shape, 0, position - (shape.Center.Y - shape.Radius));
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        Translate(shape, 0, position - shape.Center.Y);
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        Translate(shape, 0, position - (shape.Center.Y + shape.Radius));
                    }
                    break;
            }
        }

        private static void Translate(ICenterRadiusShape shape, float dx, float dy)
        {
            // translate for objects with location/size represented by center and radius
            shape.Center = new SKPoint(
                shape.Center.X + dx,
                shape.Center.Y + dy);
        }

        private void UpdatePointList(IPointListShape shape, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        Translate(shape, position - shape.Bounds.Left, 0);
                    }
                    break;

                case AlignmentType.Center:
                    {
                        Translate(shape, position - shape.Bounds.MidX, 0);
                    }
                    break;

                case AlignmentType.Right:
                    {
                        Translate(shape, position - shape.Bounds.Right, 0);
                    }
                    break;

                case AlignmentType.Top:
                    {
                        Translate(shape, 0, position - shape.Bounds.Top);
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        Translate(shape, 0, position - shape.Bounds.MidY);
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        Translate(shape, 0, position - shape.Bounds.Bottom);
                    }
                    break;
            }
        }

        private static void Translate(IPointListShape shape, float dx, float dy)
        {
            for (int i = 0; i < shape.Points.Count; i++)
            {
                SKPoint p = shape.Points[i];

                shape.Points[i] = new SKPoint(
                    p.X + dx,
                    p.Y + dy);
            }
        }

        private void UpdatePositionImage(IPositionImageShape shape, float position, AlignmentType alignment)
        {
            float width = shape.StampImage.Width * shape.Scale;
            float height = shape.StampImage.Height * shape.Scale;

            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        Translate(shape, position - shape.TopLeft.X, 0);
                    }
                    break;

                case AlignmentType.Center:
                    {
                        float currentCenter = shape.TopLeft.X + width / 2f;
                        Translate(shape, position - currentCenter, 0);
                    }
                    break;

                case AlignmentType.Right:
                    {
                        float currentRight = shape.TopLeft.X + width;
                        Translate(shape, position - currentRight, 0);
                    }
                    break;

                case AlignmentType.Top:
                    {
                        Translate(shape, 0, position - shape.TopLeft.Y);
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        float currentMiddle = shape.TopLeft.Y + height / 2f;
                        Translate(shape, 0, position - currentMiddle);
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        float currentBottom = shape.TopLeft.Y + height;
                        Translate(shape, 0, position - currentBottom);
                    }
                    break;
            }
        }

        private static void Translate(IPositionImageShape shape, float dx, float dy)
        {
            shape.TopLeft = new SKPoint(
                shape.TopLeft.X + dx,
                shape.TopLeft.Y + dy);
        }

        private void UpdateBoxLocation(PlacedMapBox box, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                {
                    float newCenterX = position + box.Bounds.Width / 2;

                    SKPoint newCenterLocation = new(newCenterX, box.Location.Y);
                    box.Location = newCenterLocation;
                }
                break;
            case AlignmentType.Center:
                {
                    SKPoint newCenterLocation = new(position, box.Location.Y);
                    box.Location = newCenterLocation;
                }
                break;
            case AlignmentType.Right:
                {
                    float newCenterX = position - box.Bounds.Width / 2;
                    SKPoint newCenterLocation = new(newCenterX, box.Location.Y);
                    box.Location = newCenterLocation;
                }
                break;
            case AlignmentType.Top:
                {
                    float newCenterY = position + box.Bounds.Height / 2;

                    SKPoint newCenterLocation = new(box.Location.X, newCenterY);
                    box.Location = newCenterLocation;
                }
                break;
            case AlignmentType.Middle:
                {
                    float newCenterY = position;

                    SKPoint newCenterLocation = new(box.Location.X, newCenterY);
                    box.Location = newCenterLocation;
                }
                break;
            case AlignmentType.Bottom:
                {
                    float newCenterY = position - box.Bounds.Height / 2;

                    SKPoint newCenterLocation = new(box.Location.X, newCenterY);
                    box.Location = newCenterLocation;
                }
                break;
            }
        }

        private void UpdateLabelLocation(MapLabel label, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                position + label.Bounds.Width / 2f,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Center:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                position,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Right:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                position - label.Bounds.Width / 2f,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Top:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                label.Location.X,
                                position + label.Bounds.Height / 2f));
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                label.Location.X,
                                position));
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        MoveLabel(
                            label,
                            new SKPoint(
                                label.Location.X,
                                position - label.Bounds.Height / 2f));
                    }
                    break;
            }

            label.BoundsModified = true;
        }

        private static void MoveLabel(MapLabel label, SKPoint newLocation)
        {
            SKPoint oldLocation = label.Location;

            float deltaX = newLocation.X - oldLocation.X;
            float deltaY = newLocation.Y - oldLocation.Y;

            label.Location = newLocation;

            if (label.CurvePath != null)
            {
                label.CurvePath.Offset(deltaX, deltaY);
            }
        }

        private void UpdateSymbolLocation(MapSymbol symbol, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        float newCenterX = position + symbol.Bounds.Width / 2;

                        SKPoint newCenterLocation = new(newCenterX, symbol.Location.Y);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
                case AlignmentType.Center:
                    {
                        SKPoint newCenterLocation = new(position, symbol.Location.Y);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
                case AlignmentType.Right:
                    {
                        float newCenterX = position - symbol.Bounds.Width / 2;
                        SKPoint newCenterLocation = new(newCenterX, symbol.Location.Y);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
                case AlignmentType.Top:
                    {
                        float newCenterY = position + symbol.Bounds.Height / 2;

                        SKPoint newCenterLocation = new(symbol.Location.X, newCenterY);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
                case AlignmentType.Middle:
                    {
                        float newCenterY = position;

                        SKPoint newCenterLocation = new(symbol.Location.X, newCenterY);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
                case AlignmentType.Bottom:
                    {
                        float newCenterY = position - symbol.Bounds.Height / 2;

                        SKPoint newCenterLocation = new(symbol.Location.X, newCenterY);
                        symbol.Location = newCenterLocation;

                        UpdateBoundsPosition(symbol, newCenterLocation, alignment);
                    }
                    break;
            }
        }

        private void UpdateBoundsPosition(MapComponent2D component, SKPoint centerLocation, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                case AlignmentType.Center:
                case AlignmentType.Right:
                    {
                        float newLeft = centerLocation.X - component.Bounds.Width / 2;
                        SKRect newBounds = new(newLeft, component.Bounds.Top, newLeft + component.Bounds.Width, component.Bounds.Bottom);
                        component.Bounds = newBounds;
                    }
                    break;
                case AlignmentType.Top:
                case AlignmentType.Middle:
                case AlignmentType.Bottom:
                    {
                        float newTop = centerLocation.Y - component.Bounds.Height / 2;
                        SKRect newBounds = new(component.Bounds.Left, newTop, component.Bounds.Right, newTop + component.Bounds.Height);
                        component.Bounds = newBounds;
                    }
                    break;
            }
        }

        private Cmd_ModifyObjects BeginModification()
        {
            var cmd = new Cmd_ModifyObjects(_editor.Scene!.Map);

            cmd.CaptureBefore(_selectionService.SelectedObjects.OfType<MapComponent2D>());

            return cmd;
        }

        private void EndModification(Cmd_ModifyObjects cmd)
        {
            cmd.CaptureAfter(_selectionService.SelectedObjects.OfType<MapComponent2D>());

            _commands.ActiveCommands.Execute(cmd);
        }
    }

    public class AlignmentOptions
    {

    }
}
