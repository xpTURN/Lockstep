namespace xpTURN.Lockstep.Math
{
    /// <summary>
    /// Deterministic random number generator interface
    /// Generates same random sequence with same seed
    /// </summary>
    public interface IDeterministicRandom
    {
        /// <summary>
        /// Current seed value
        /// </summary>
        int Seed { get; }

        /// <summary>
        /// Set seed (initialize)
        /// </summary>
        void SetSeed(int seed);

        /// <summary>
        /// Get current state (for save/restore)
        /// </summary>
        long GetState();

        /// <summary>
        /// Restore state
        /// </summary>
        void SetState(long state);

        /// <summary>
        /// Next integer random (0 ~ int.MaxValue)
        /// </summary>
        int NextInt();

        /// <summary>
        /// Integer random in range (min inclusive, max exclusive)
        /// </summary>
        int NextInt(int min, int max);

        /// <summary>
        /// Fixed point random (0 ~ 1)
        /// </summary>
        IFixedPoint NextFixed();

        /// <summary>
        /// Fixed point random in range
        /// </summary>
        IFixedPoint NextFixed(IFixedPoint min, IFixedPoint max);

        /// <summary>
        /// Boolean random
        /// </summary>
        bool NextBool();

        /// <summary>
        /// Probability-based boolean (percent: 0~100)
        /// </summary>
        bool NextChance(int percent);

        /// <summary>
        /// Random 2D point inside unit circle
        /// </summary>
        IFixedVector2 NextInsideUnitCircle();

        /// <summary>
        /// Random 3D point inside unit sphere
        /// </summary>
        IFixedVector3 NextInsideUnitSphere();

        /// <summary>
        /// Random 2D direction on unit circle
        /// </summary>
        IFixedVector2 NextDirection2D();

        /// <summary>
        /// Random 3D direction on unit sphere
        /// </summary>
        IFixedVector3 NextDirection3D();
    }
}
