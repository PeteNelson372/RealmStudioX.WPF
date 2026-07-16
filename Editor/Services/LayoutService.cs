using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using RealmStudioX.WPF.Models.UserInterface;
using RealmStudioX.WPF.ViewModels.Main;
using SkiaSharp;


namespace RealmStudioX.WPF.Editor.Services
{
    public class LayoutService
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly EditorController _editor;
        private readonly SelectionService _selectionService;
        private readonly CommandService _commands;

        public LayoutService(MainWindowViewModel mainWindowViewModel, EditorController editor, SelectionService selection, CommandService commands)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _editor = editor;
            _selectionService = selection;
            _commands = commands;
        }

        public LayoutOptions Layout => _mainWindowViewModel.Layout;

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

        public void LayoutOnPath()
        {
            if (_selectionService.SelectionCount < 2 || _mainWindowViewModel.LayoutTool?.LayoutPath == null)
            {
                return;
            }

            if (_mainWindowViewModel.LayoutTool.LayoutPath.Handle == 0
                || _mainWindowViewModel.LayoutTool.LayoutPath == null
                || _mainWindowViewModel.LayoutTool.LayoutPath.IsEmpty)
            {
                return;
            }

            SKPath layoutPath = _mainWindowViewModel.LayoutTool.LayoutPath;
            LayoutOptions layout = _mainWindowViewModel.Layout;

            List<IAlignable>? layoutObjects = BuildLayoutObjectsList();

            if (layoutObjects != null && layoutObjects.Count != 0)
            {
                Cmd_ModifyObjects cmd = BeginModification();

                float usableLength = MeasurePath(layoutPath, layout);

                if (usableLength <= 0)
                {
                    return;
                }

                List<float> distances = CalculateDistances(usableLength, layoutObjects.Count, layout);

                List<PathSample> pathSamples = SamplePath(layoutPath, distances);

                for (int i = 0; i < pathSamples.Count; i++)
                {
                    PathSample sample = pathSamples[i];
                    SKPoint position = ApplyOffset(sample.Position, sample.Tangent, layout.Offset);

                    MapComponent2D alignable = (MapComponent2D)layoutObjects[i];

                    UpdatePosition(alignable, position, layout, sample.RotationDegrees);
                }

                EndModification(cmd);

                _selectionService.ClearSelection(_editor.Scene);

                _editor.SetDrawingMode(MapDrawingMode.None);

                _mainWindowViewModel.LayoutTool.ClearLayoutPath();

            }
        }

        private void UpdatePosition(MapComponent2D component, SKPoint location, LayoutOptions layout, float rotation = 0)
        {
            switch (component)
            {
                case MapSymbol symbol:
                    UpdateMapSymbol(symbol, location, rotation, layout);
                    break;

                case MapLabel label:
                    UpdateMapLabel(label, location, rotation, layout);
                    break;

                case IRectangularShape rect:
                    UpdateTopLeftBottomRight(rect, location, rotation, layout);
                    break;

                case ICenterRadiusShape centerRadius:
                    UpdateCenterRadius(centerRadius, location, rotation, layout);
                    break;

                case IPointListShape pointList:
                    UpdatePointList(pointList, location, rotation, layout);
                    break;

                case IPositionImageShape image:
                    UpdatePositionImage(image, location, rotation, layout);
                    break;
            }
        }

        private static float GetLayoutRotation(float angle, LayoutOptions layout)
        {
            if (!layout.FollowPathRotation)
                return float.NaN; // or some sentinel

            if (layout.KeepUpright)
            {
                if (angle > 90f)
                    angle -= 180f;
                else if (angle < -90f)
                    angle += 180f;
            }

            return angle;
        }

        private static void UpdateMapSymbol(MapSymbol symbol, SKPoint position, float? rotation, LayoutOptions layout)
        {
            symbol.Location = position;

            if (rotation.HasValue && symbol is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }

            float newLeft = symbol.Location.X - symbol.Bounds.Width / 2;
            float newTop = symbol.Location.Y - symbol.Bounds.Height / 2;

            SKRect newBounds = new(newLeft, newTop, newLeft + symbol.Bounds.Width, newTop + symbol.Bounds.Height);
            symbol.Bounds = newBounds;
        }

        private static void UpdateMapLabel(MapLabel label, SKPoint position, float? rotation, LayoutOptions layout)
        {
            label.MoveTo(position);

            if (rotation.HasValue && label is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }

            label.BoundsModified = true;
        }

        private static void UpdatePositionImage(IPositionImageShape shape, SKPoint position, float? rotation, LayoutOptions layout)
        {
            float width =
                shape.StampImage.Width * shape.Scale;

            float height =
                shape.StampImage.Height * shape.Scale;

            shape.TopLeft = new(
                position.X - width / 2f,
                position.Y - height / 2f);

            if (rotation.HasValue && shape is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }
        }

        private static void UpdatePointList(IPointListShape shape, SKPoint position, float? rotation, LayoutOptions layout)
        {
            SKPoint center = GetCentroid(shape.Points);

            Translate(
                shape,
                position.X - center.X,
                position.Y - center.Y);

            if (rotation.HasValue && shape is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }
        }

        private static SKPoint GetCentroid(List<SKPoint> points)
        {
            if (points == null || points.Count == 0)
                return SKPoint.Empty;

            float sumX = 0;
            float sumY = 0;

            foreach (SKPoint point in points)
            {
                sumX += point.X;
                sumY += point.Y;
            }

            return new SKPoint(
                sumX / points.Count,
                sumY / points.Count);
        }

        private static void UpdateTopLeftBottomRight(IRectangularShape shape, SKPoint position, float? rotation, LayoutOptions layout)
        {
            float centerX =
                (shape.TopLeft.X + shape.BottomRight.X) / 2f;

            float centerY =
                (shape.TopLeft.Y + shape.BottomRight.Y) / 2f;

            Translate(
                shape,
                position.X - centerX,
                position.Y - centerY);

            if (rotation.HasValue && shape is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }
        }

        private static void UpdateCenterRadius(ICenterRadiusShape shape, SKPoint position, float? rotation, LayoutOptions layout)
        {
            shape.Center = position;

            if (rotation.HasValue && shape is IRotatable rotatable)
            {
                float angle = GetLayoutRotation(rotation.Value, layout);

                if (!float.IsNaN(angle))
                    rotatable.Rotation = angle;
            }
        }

        private static SKPoint ApplyOffset(SKPoint position, SKPoint tangent, double offset)
        {
            SKPoint normal = new(-tangent.Y, tangent.X);

            float length =
                MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y);

            if (length > 0)
            {
                normal.X /= length;
                normal.Y /= length;
            }

            return new SKPoint(
                position.X + normal.X * (float)offset,
                position.Y + normal.Y * (float)offset);
        }

        private static List<PathSample> SamplePath(SKPath path, IReadOnlyList<float> distances)
        {
            List<PathSample> samples = [];

            using SKPathMeasure measure = new(path, false);

            foreach (float distance in distances)
            {
                measure.GetPositionAndTangent(
                    distance,
                    out SKPoint position,
                    out SKPoint tangent);

                samples.Add(new PathSample
                {
                    Distance = distance,
                    Position = position,
                    Tangent = tangent
                });
            }

            return samples;
        }

        private float MeasurePath(SKPath layoutPath, LayoutOptions layout)
        {
            using SKPathMeasure measure = new(layoutPath, false);

            float pathLength = measure.Length;

            float start = (float)layout.StartOffset;
            float end = pathLength - (float)layout.EndOffset;

            if (end <= start)
            {
                return 0;
            }

            float usableLength = end - start;

            return usableLength;
        }

        private static List<float> CalculateDistances(float pathLength, int objectCount, LayoutOptions options)
        {
            List<float> distances = new();

            if (objectCount == 0)
                return distances;

            float start = (float)options.StartOffset;
            float end = pathLength - (float)options.EndOffset;

            if (end <= start)
                return distances;

            float usable = end - start;

            switch (options.Distribution)
            {
                case PlacementStrategy.Even:
                    {
                        if (objectCount == 1)
                        {
                            distances.Add(start + usable / 2f);
                        }
                        else
                        {
                            float step = usable / (objectCount - 1);

                            for (int i = 0; i < objectCount; i++)
                            {
                                distances.Add(start + i * step);
                            }
                        }
                    }
                    break;

                case PlacementStrategy.Random:
                    {

                    }
                    break;
            }

            return distances;
        }

        private List<IAlignable>? BuildLayoutObjectsList()
        {
            List<IAlignable> objects = [];

            foreach (object obj in _selectionService.SelectedObjects)
            {
                if (obj is IAlignable positionable &&
                    obj != _mainWindowViewModel.LayoutTool.LayoutPath)
                {
                    objects.Add(positionable);
                }
            }

            if (objects.Count == 0)
            {
                return null;
            }

            return objects;
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

        private static void UpdateLabelLocation(MapLabel label, float position, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Left:
                    {
                        label.MoveTo(new SKPoint(
                                position + label.Bounds.Width / 2f,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Center:
                    {
                        label.MoveTo(new SKPoint(
                                position,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Right:
                    {
                        label.MoveTo(new SKPoint(
                                position - label.Bounds.Width / 2f,
                                label.Location.Y));
                    }
                    break;

                case AlignmentType.Top:
                    {
                        label.MoveTo(new SKPoint(
                                label.Location.X,
                                position + label.Bounds.Height / 2f));
                    }
                    break;

                case AlignmentType.Middle:
                    {
                        label.MoveTo(new SKPoint(
                                label.Location.X,
                                position));
                    }
                    break;

                case AlignmentType.Bottom:
                    {
                        label.MoveTo(new SKPoint(
                                label.Location.X,
                                position - label.Bounds.Height / 2f));
                    }
                    break;
            }

            label.BoundsModified = true;
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

    public sealed class PathSample
    {
        public float Distance { get; init; }

        public SKPoint Position { get; init; }

        public SKPoint Tangent { get; init; }

        public float RotationDegrees =>
            MathF.Atan2(Tangent.Y, Tangent.X) * 180f / MathF.PI;

        public SKPoint Normal
        {
            get
            {
                SKPoint n = new(-Tangent.Y, Tangent.X);

                float len = MathF.Sqrt(n.X * n.X + n.Y * n.Y);

                if (len > 0)
                {
                    n.X /= len;
                    n.Y /= len;
                }

                return n;
            }
        }
    }
}
