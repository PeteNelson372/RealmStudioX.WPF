
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial
{
    /// <summary>
    /// Finds an initial position for a newly-created celestial body
    /// that avoids intersecting or visually obscuring existing bodies.
    /// 
    /// Placement.Center is returned in the coordinate space of the
    /// specified parent node.
    /// </summary>
    public sealed class CelestialBodyPlacementService
    {
        private const double MinimumWorldGap = 0.025;
        private const double MinimumScreenGap = 8.0;

        private const int CandidateDirections = 32;
        private const int SearchRings = 12;

        private readonly record struct BodySphere(
            CelestialBodyNode Node,
            Point3D Center,
            double Radius);

        private readonly record struct CameraFrame(
            Point3D Position,
            Vector3D Forward,
            Vector3D Right,
            Vector3D Up,
            bool IsPerspective,
            double PerspectiveHalfAngleTangent,
            double OrthographicHalfWidth,
            double OrthographicHalfHeight,
            double ViewportWidth,
            double ViewportHeight)
        {
            public bool CanProject =>
                ViewportWidth > 1.0 &&
                ViewportHeight > 1.0 &&
                (IsPerspective
                    ? PerspectiveHalfAngleTangent > 0.0
                    : OrthographicHalfWidth > 0.0 &&
                      OrthographicHalfHeight > 0.0);
        }

        private readonly record struct ScreenSphere(
            double X,
            double Y,
            double Radius);

        /// <summary>
        /// Finds a suitable position for a new body.
        /// </summary>
        /// <param name="hierarchyRoot">
        /// Root of the complete celestial hierarchy.
        /// </param>
        /// <param name="parent">
        /// Parent under which the new body will be inserted.
        /// </param>
        /// <param name="newBodyRadius">
        /// The new body's Placement.Size.
        /// </param>
        /// <param name="camera">
        /// Current viewport camera.
        /// </param>
        /// <param name="viewportWidth">
        /// Current viewport width in WPF device-independent units.
        /// </param>
        /// <param name="viewportHeight">
        /// Current viewport height in WPF device-independent units.
        /// </param>
        public Point3D FindAvailablePosition(
            CelestialBodyNode hierarchyRoot,
            CelestialBodyNode parent,
            double newBodyRadius,
            Camera? camera,
            double viewportWidth,
            double viewportHeight)
        {
            ArgumentNullException.ThrowIfNull(hierarchyRoot);
            ArgumentNullException.ThrowIfNull(parent);

            newBodyRadius = Math.Abs(newBodyRadius);

            if (!TryBuildNodePath(hierarchyRoot, parent, out List<CelestialBodyNode> parentPath))
            {
                throw new InvalidOperationException(
                    "The specified parent is not part of the celestial hierarchy.");
            }

            if (!TryTransformToWorld(parentPath, new Point3D(), out Point3D parentWorldCenter))
            {
                throw new InvalidOperationException(
                    "Unable to calculate the parent's world-space position.");
            }

            CameraFrame frame = CreateCameraFrame(
                camera,
                viewportWidth,
                viewportHeight);

            List<BodySphere> existingBodies = [];

            CollectBodySpheres(
                hierarchyRoot,
                hierarchyRoot,
                existingBodies);

            double parentRadius = Math.Abs(parent.Placement.Size);

            double minimumDistance =
                parentRadius +
                newBodyRadius +
                MinimumWorldGap;

            // Search outward from the parent. The camera-facing plane
            // makes the new body visible beside its parent.
            for (int ring = 0; ring < SearchRings; ring++)
            {
                double distance =
                    minimumDistance * (1.0 + ring * 0.5);

                for (int direction = 0;
                     direction < CandidateDirections;
                     direction++)
                {
                    double angle =
                        (Math.PI * 2.0 * direction / CandidateDirections);

                    Vector3D offset =
                        frame.Right * (Math.Cos(angle) * distance) +
                        frame.Up * (Math.Sin(angle) * distance);

                    Point3D candidateWorldCenter =
                        parentWorldCenter + offset;

                    if (!IsCandidateClear(
                        candidateWorldCenter,
                        newBodyRadius,
                        existingBodies,
                        frame))
                    {
                        continue;
                    }

                    if (!TryTransformFromWorld(
                        parentPath,
                        candidateWorldCenter,
                        out Point3D parentRelativePosition))
                    {
                        continue;
                    }

                    return parentRelativePosition;
                }
            }

            // Fallback: place the body beyond the outermost existing
            // body, along the camera's right vector. This guarantees
            // spatial clearance even in a very crowded hierarchy.
            double maximumExtent = 0.0;

            foreach (BodySphere sphere in existingBodies)
            {
                double distance =
                    (sphere.Center - parentWorldCenter).Length;

                maximumExtent = Math.Max(
                    maximumExtent,
                    distance + sphere.Radius);
            }

            double fallbackDistance =
                maximumExtent +
                parentRadius +
                newBodyRadius +
                MinimumWorldGap;

            Point3D fallbackWorldCenter =
                parentWorldCenter +
                frame.Right * fallbackDistance;

            if (!TryTransformFromWorld(
                parentPath,
                fallbackWorldCenter,
                out Point3D fallbackPosition))
            {
                throw new InvalidOperationException(
                    "Unable to convert the calculated body position into parent-relative coordinates.");
            }

            return fallbackPosition;
        }

        private static bool IsCandidateClear(
            Point3D candidateCenter,
            double candidateRadius,
            IReadOnlyList<BodySphere> existingBodies,
            CameraFrame frame)
        {
            ScreenSphere? candidateScreenSphere = null;

            if (frame.CanProject &&
                TryProjectSphere(
                    candidateCenter,
                    candidateRadius,
                    frame,
                    out ScreenSphere projected))
            {
                candidateScreenSphere = projected;
            }

            foreach (BodySphere existing in existingBodies)
            {
                double requiredDistance =
                    candidateRadius +
                    existing.Radius +
                    MinimumWorldGap;

                double actualDistanceSquared =
                    (candidateCenter - existing.Center).LengthSquared;

                if (actualDistanceSquared <
                    requiredDistance * requiredDistance)
                {
                    return false;
                }

                if (candidateScreenSphere is null ||
                    !frame.CanProject)
                {
                    continue;
                }

                if (!TryProjectSphere(
                    existing.Center,
                    existing.Radius,
                    frame,
                    out ScreenSphere existingScreenSphere))
                {
                    continue;
                }

                ScreenSphere candidate = candidateScreenSphere.Value;

                double dx = candidate.X - existingScreenSphere.X;
                double dy = candidate.Y - existingScreenSphere.Y;

                double requiredScreenDistance =
                    candidate.Radius +
                    existingScreenSphere.Radius +
                    MinimumScreenGap;

                if ((dx * dx) + (dy * dy) <
                    requiredScreenDistance * requiredScreenDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryProjectSphere(
            Point3D center,
            double radius,
            CameraFrame frame,
            out ScreenSphere projected)
        {
            projected = default;

            Vector3D delta = center - frame.Position;

            double depth = Vector3D.DotProduct(
                delta,
                frame.Forward);

            // Bodies behind the camera do not obscure the candidate.
            if (depth <= 0.001)
            {
                return false;
            }

            double horizontal =
                Vector3D.DotProduct(delta, frame.Right);

            double vertical =
                Vector3D.DotProduct(delta, frame.Up);

            double screenX;
            double screenY;
            double screenRadius;

            if (frame.IsPerspective)
            {
                double halfHeight =
                    depth * frame.PerspectiveHalfAngleTangent;

                double halfWidth =
                    halfHeight *
                    (frame.ViewportWidth / frame.ViewportHeight);

                screenX =
                    (0.5 + horizontal / (2.0 * halfWidth)) *
                    frame.ViewportWidth;

                screenY =
                    (0.5 - vertical / (2.0 * halfHeight)) *
                    frame.ViewportHeight;

                screenRadius =
                    radius / halfHeight *
                    (frame.ViewportHeight / 2.0);
            }
            else
            {
                screenX =
                    (0.5 + horizontal /
                        (2.0 * frame.OrthographicHalfWidth)) *
                    frame.ViewportWidth;

                screenY =
                    (0.5 - vertical /
                        (2.0 * frame.OrthographicHalfHeight)) *
                    frame.ViewportHeight;

                screenRadius =
                    radius / frame.OrthographicHalfHeight *
                    (frame.ViewportHeight / 2.0);
            }

            projected = new ScreenSphere(
                screenX,
                screenY,
                screenRadius);

            return true;
        }

        private static CameraFrame CreateCameraFrame(
            Camera? camera,
            double viewportWidth,
            double viewportHeight)
        {
            // A predictable fallback for cases where the viewer has
            // not initialized its camera yet.
            if (camera is not ProjectionCamera projectionCamera)
            {
                return new CameraFrame(
                    new Point3D(),
                    new Vector3D(0, 0, -1),
                    new Vector3D(1, 0, 0),
                    new Vector3D(0, 1, 0),
                    false,
                    0.0,
                    0.0,
                    0.0,
                    viewportWidth,
                    viewportHeight);
            }

            Transform3D cameraTransform =
                projectionCamera.Transform ?? Transform3D.Identity;

            Point3D cameraPosition =
                cameraTransform.Transform(projectionCamera.Position);

            Vector3D forward =
                cameraTransform.Transform(projectionCamera.LookDirection);

            Vector3D up =
                cameraTransform.Transform(projectionCamera.UpDirection);

            forward = NormalizeOrDefault(
                forward,
                new Vector3D(0, 0, -1));

            Vector3D right = Vector3D.CrossProduct(forward, up);

            if (right.LengthSquared < 0.000001)
            {
                right = Vector3D.CrossProduct(
                    forward,
                    new Vector3D(0, 1, 0));
            }

            right = NormalizeOrDefault(
                right,
                new Vector3D(1, 0, 0));

            up = NormalizeOrDefault(
                Vector3D.CrossProduct(right, forward),
                new Vector3D(0, 1, 0));

            if (projectionCamera is PerspectiveCamera perspectiveCamera)
            {
                double halfAngleTangent = Math.Tan(
                    perspectiveCamera.FieldOfView *
                    Math.PI / 360.0);

                return new CameraFrame(
                    cameraPosition,
                    forward,
                    right,
                    up,
                    true,
                    halfAngleTangent,
                    0.0,
                    0.0,
                    viewportWidth,
                    viewportHeight);
            }

            if (projectionCamera is OrthographicCamera orthographicCamera)
            {
                double aspect =
                    viewportHeight > 0.0
                        ? viewportWidth / viewportHeight
                        : 1.0;

                double halfWidth =
                    orthographicCamera.Width / 2.0;

                double halfHeight =
                    aspect > 0.0
                        ? halfWidth / aspect
                        : halfWidth;

                return new CameraFrame(
                    cameraPosition,
                    forward,
                    right,
                    up,
                    false,
                    0.0,
                    halfWidth,
                    halfHeight,
                    viewportWidth,
                    viewportHeight);
            }

            return new CameraFrame(
                cameraPosition,
                forward,
                right,
                up,
                false,
                0.0,
                0.0,
                0.0,
                viewportWidth,
                viewportHeight);
        }

        private static Vector3D NormalizeOrDefault(
            Vector3D vector,
            Vector3D fallback)
        {
            if (vector.LengthSquared < 0.000001)
            {
                return fallback;
            }

            vector.Normalize();
            return vector;
        }

        private static void CollectBodySpheres(
            CelestialBodyNode hierarchyRoot,
            CelestialBodyNode current,
            List<BodySphere> results)
        {
            if (TryBuildNodePath(
                hierarchyRoot,
                current,
                out List<CelestialBodyNode> path) &&
                TryTransformToWorld(
                    path,
                    new Point3D(),
                    out Point3D worldCenter))
            {
                results.Add(new BodySphere(
                    current,
                    worldCenter,
                    Math.Abs(current.Placement.Size)));
            }

            foreach (CelestialBodyNode child in current.Children)
            {
                CollectBodySpheres(
                    hierarchyRoot,
                    child,
                    results);
            }
        }

        private static bool TryBuildNodePath(
            CelestialBodyNode root,
            CelestialBodyNode target,
            out List<CelestialBodyNode> path)
        {
            path = [];

            return FindPath(root, target, path);
        }

        private static bool FindPath(
            CelestialBodyNode current,
            CelestialBodyNode target,
            List<CelestialBodyNode> path)
        {
            path.Add(current);

            if (ReferenceEquals(current, target))
            {
                return true;
            }

            foreach (CelestialBodyNode child in current.Children)
            {
                if (FindPath(child, target, path))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// Applies the same transform order used by
        /// CelestialSystemRenderer.BuildBodyNode:
        /// LocalTransform followed by Placement.Center,
        /// repeated from the hierarchy root to the target node.
        /// </summary>
        private static bool TryTransformToWorld(
            IReadOnlyList<CelestialBodyNode> path,
            Point3D localPoint,
            out Point3D worldPoint)
        {
            worldPoint = localPoint;

            // The path is root -> target, but the target's local
            // transform must be applied first, followed by its ancestors.
            for (int i = path.Count - 1; i >= 0; i--)
            {
                CelestialBodyNode node = path[i];

                if (node.LocalTransform is not null)
                {
                    worldPoint =
                        node.LocalTransform.Transform(worldPoint);
                }

                worldPoint += (Vector3D)node.Placement.Center;
            }

            return true;
        }

        private static bool TryTransformFromWorld(
            IReadOnlyList<CelestialBodyNode> path,
            Point3D worldPoint,
            out Point3D localPoint)
        {
            localPoint = worldPoint;

            // Reverse the world transform: start at the root and
            // work down toward the target parent.
            foreach (CelestialBodyNode node in path)
            {
                localPoint -= (Vector3D)node.Placement.Center;

                if (node.LocalTransform is null)
                {
                    continue;
                }

                Matrix3D inverse = node.LocalTransform.Value;

                if (!inverse.HasInverse)
                {
                    return false;
                }

                inverse.Invert();
                localPoint = inverse.Transform(localPoint);
            }

            return true;
        }

    }
}
