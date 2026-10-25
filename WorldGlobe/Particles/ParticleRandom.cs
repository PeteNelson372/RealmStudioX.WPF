namespace RealmStudioX.WPF.WorldGlobe.Particles;

internal static class ParticleRandom
{
    public static double Sample(
        int seed,
        long particleIndex,
        int channel)
    {
        unchecked
        {
            ulong value =
                (ulong)(uint)seed
                ^ ((ulong)particleIndex * 0x9E3779B97F4A7C15UL)
                ^ ((ulong)(uint)channel * 0xBF58476D1CE4E5B9UL);

            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30))
                    * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27))
                    * 0x94D049BB133111EBUL;
            value ^= value >> 31;

            return (value >> 11) * (1.0 / 9007199254740992.0);
        }
    }
}