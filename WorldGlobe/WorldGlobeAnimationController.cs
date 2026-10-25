namespace RealmStudioX.WPF.WorldGlobe;

internal sealed class WorldGlobeAnimationController
{
    private readonly List<IWorldGlobeAnimatedObject> _animatedObjects = [];

    private TimeSpan? _lastRenderingTime;

    public bool IsEnabled { get; set; }

    public double TimeSeconds { get; private set; }

    public void Register(IWorldGlobeAnimatedObject animatedObject)
    {
        ArgumentNullException.ThrowIfNull(animatedObject);

        if (_animatedObjects.Contains(animatedObject))
            return;

        _animatedObjects.Add(animatedObject);
        animatedObject.ResetAnimation();
    }

    public void Unregister(IWorldGlobeAnimatedObject animatedObject)
    {
        ArgumentNullException.ThrowIfNull(animatedObject);

        _animatedObjects.Remove(animatedObject);
    }

    /// <summary>
    /// Advances the timeline using WPF's rendering timestamp.
    /// Call this once per rendered frame while animation is enabled.
    /// </summary>
    public WorldGlobeAnimationFrame Advance(TimeSpan renderingTime)
    {
        if (_lastRenderingTime is null)
        {
            _lastRenderingTime = renderingTime;

            var initialFrame = new WorldGlobeAnimationFrame(
                TimeSeconds, 0.0);

            if (IsEnabled)
                Dispatch(initialFrame);

            return initialFrame;
        }

        double deltaSeconds =
            (renderingTime - _lastRenderingTime.Value).TotalSeconds;

        _lastRenderingTime = renderingTime;

        // Ignore invalid or backwards rendering timestamps.
        deltaSeconds = Math.Max(0.0, deltaSeconds);

        if (!IsEnabled)
            deltaSeconds = 0.0;

        if (deltaSeconds > 0.0)
            TimeSeconds += deltaSeconds;

        var frame = new WorldGlobeAnimationFrame(
            TimeSeconds, deltaSeconds);

        if (IsEnabled)
            Dispatch(frame);

        return frame;
    }

    /// <summary>
    /// Advances the scene to an explicit animation time.
    /// This also works when interactive animation is paused.
    /// Useful later for deterministic video-frame rendering.
    /// </summary>
    public WorldGlobeAnimationFrame SetTime(double timeSeconds)
    {
        if (!double.IsFinite(timeSeconds) || timeSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds),
                "Animation time must be finite and non-negative.");
        }

        // If asked to go backwards, reset the objects and replay
        // forward from the beginning to the requested timestamp.
        if (timeSeconds < TimeSeconds)
            Reset();

        double deltaSeconds = timeSeconds - TimeSeconds;

        TimeSeconds = timeSeconds;

        // Prevent the next live rendering callback from including
        // time that elapsed before this explicit timeline update.
        _lastRenderingTime = null;

        var frame = new WorldGlobeAnimationFrame(
            TimeSeconds, deltaSeconds);

        Dispatch(frame);

        return frame;
    }

    public void ResetClock()
    {
        _lastRenderingTime = null;
    }

    public void Reset()
    {
        TimeSeconds = 0.0;
        _lastRenderingTime = null;

        foreach (IWorldGlobeAnimatedObject animatedObject
                 in _animatedObjects.ToArray())
        {
            animatedObject.ResetAnimation();
        }
    }

    private void Dispatch(WorldGlobeAnimationFrame frame)
    {
        // Use a snapshot so registration changes during an update
        // cannot invalidate the enumeration.
        foreach (IWorldGlobeAnimatedObject animatedObject
                 in _animatedObjects.ToArray())
        {
            animatedObject.AdvanceAnimation(frame);
        }
    }
}