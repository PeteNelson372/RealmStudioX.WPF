
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Reproducible initialization parameters for the Sun's corona animation.
/// Both the WPF and SharpDX renderers use the same parameter generation.
/// </summary>
internal readonly struct SunCoronaAnimationParameters
{
    private const uint InitialRandomState = 0x53554E43; // "SUNC"

    public Vector3D RotationAxis { get; }
    public Vector3D WobbleAxisA { get; }
    public Vector3D WobbleAxisB { get; }

    public double RotationDirection { get; }

    public double WobbleAmplitudeA { get; }
    public double WobbleAmplitudeB { get; }

    public double WobbleFrequencyA { get; }
    public double WobbleFrequencyB { get; }

    public double WobblePhaseA { get; }
    public double WobblePhaseB { get; }

    private SunCoronaAnimationParameters(
        Vector3D rotationAxis,
        Vector3D wobbleAxisA,
        Vector3D wobbleAxisB,
        double rotationDirection,
        double wobbleAmplitudeA,
        double wobbleAmplitudeB,
        double wobbleFrequencyA,
        double wobbleFrequencyB,
        double wobblePhaseA,
        double wobblePhaseB)
    {
        RotationAxis = rotationAxis;
        WobbleAxisA = wobbleAxisA;
        WobbleAxisB = wobbleAxisB;

        RotationDirection = rotationDirection;

        WobbleAmplitudeA = wobbleAmplitudeA;
        WobbleAmplitudeB = wobbleAmplitudeB;

        WobbleFrequencyA = wobbleFrequencyA;
        WobbleFrequencyB = wobbleFrequencyB;

        WobblePhaseA = wobblePhaseA;
        WobblePhaseB = wobblePhaseB;
    }

    public static SunCoronaAnimationParameters CreateDefault()
    {
        DeterministicRandom random = new(InitialRandomState);

        return new SunCoronaAnimationParameters(
            NextRotationAxis(ref random),
            NextRotationAxis(ref random),
            NextRotationAxis(ref random),

            random.NextUnit() < 0.5 ? -1.0 : 1.0,

            1.0 + random.NextUnit() * 2.0,  // 1–3 degrees
            0.5 + random.NextUnit() * 1.5,  // 0.5–2 degrees

            0.35 + random.NextUnit() * 0.45,
            0.65 + random.NextUnit() * 0.65,

            random.NextUnit() * Math.PI * 2.0,
            random.NextUnit() * Math.PI * 2.0);
    }

    private static Vector3D NextRotationAxis(
        ref DeterministicRandom random)
    {
        Vector3D axis;

        do
        {
            axis = new Vector3D(
                random.NextUnit() * 2.0 - 1.0,
                random.NextUnit() * 2.0 - 1.0,
                random.NextUnit() * 2.0 - 1.0);
        }
        while (axis.LengthSquared < 0.0001);

        axis.Normalize();

        return axis;
    }

    /// <summary>
    /// Xorshift32 deterministic pseudo-random number generator.
    /// </summary>
    private struct DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(uint seed)
        {
            _state = seed == 0 ? InitialRandomState : seed;
        }

        public double NextUnit()
        {
            uint x = _state;

            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;

            _state = x == 0 ? InitialRandomState : x;

            return (_state + 0.5) / (uint.MaxValue + 1.0);
        }
    }
}
