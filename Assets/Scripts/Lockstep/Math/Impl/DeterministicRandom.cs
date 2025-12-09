using System;

namespace xpTURN.Lockstep.Math.Impl
{
    /// <summary>
    /// Deterministic random number generator implementation (Xorshift128+ algorithm)
    /// </summary>
    [Serializable]
    public class DeterministicRandom : IDeterministicRandom
    {
        private ulong _state0;
        private ulong _state1;
        private int _seed;

        public int Seed => _seed;

        public DeterministicRandom() : this(Environment.TickCount)
        {
        }

        public DeterministicRandom(int seed)
        {
            SetSeed(seed);
        }

        public void SetSeed(int seed)
        {
            _seed = seed;

            // Generate initial state with SplitMix64
            ulong z = (ulong)seed;

            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            _state0 = z ^ (z >> 31);

            z = _state0 + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            _state1 = z ^ (z >> 31);
        }

        public long GetState()
        {
            // Encode two states into one long (simple version)
            return (long)(_state0 ^ _state1);
        }

        public void SetState(long state)
        {
            // Restore state (reinitialize with seed then advance to state)
            // In actual implementation, two ulongs should be stored
            SetSeed((int)state);
        }

        /// <summary>
        /// Save/restore both state values
        /// </summary>
        public (ulong state0, ulong state1) GetFullState()
        {
            return (_state0, _state1);
        }

        public void SetFullState(ulong state0, ulong state1)
        {
            _state0 = state0;
            _state1 = state1;
        }

        private ulong NextUInt64()
        {
            // Xorshift128+
            ulong s1 = _state0;
            ulong s0 = _state1;
            ulong result = s0 + s1;

            _state0 = s0;
            s1 ^= s1 << 23;
            _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);

            return result;
        }

        public int NextInt()
        {
            return (int)(NextUInt64() & 0x7FFFFFFF);
        }

        public int NextInt(int min, int max)
        {
            if (min >= max)
                return min;

            ulong range = (ulong)(max - min);
            return min + (int)(NextUInt64() % range);
        }

        #region Interface return (boxing occurs - for legacy compatibility)

        public IFixedPoint NextFixed()
        {
            return NextFP64();
        }

        public IFixedPoint NextFixed(IFixedPoint min, IFixedPoint max)
        {
            return NextFP64((FP64)min, (FP64)max);
        }

        public IFixedVector2 NextInsideUnitCircle()
        {
            return NextInsideUnitCircleFP();
        }

        public IFixedVector3 NextInsideUnitSphere()
        {
            return NextInsideUnitSphereFP();
        }

        public IFixedVector2 NextDirection2D()
        {
            return NextDirection2DFP();
        }

        public IFixedVector3 NextDirection3D()
        {
            return NextDirection3DFP();
        }

        #endregion

        #region GC-Free versions (concrete type return)

        /// <summary>
        /// Fixed point between 0 ~ 1 (GC-Free)
        /// </summary>
        public FP64 NextFP64()
        {
            return FP64.FromRaw((long)(NextUInt64() & 0xFFFFFFFF));
        }

        /// <summary>
        /// Fixed point between min ~ max (GC-Free)
        /// </summary>
        public FP64 NextFP64(FP64 min, FP64 max)
        {
            FP64 t = NextFP64();
            return FP64.LerpUnclamped(min, max, t);
        }

        /// <summary>
        /// Random point inside unit circle (GC-Free)
        /// </summary>
        public FPVector2 NextInsideUnitCircleFP()
        {
            // Rejection sampling
            while (true)
            {
                FP64 x = FP64.FromRaw((long)(NextUInt64() % (2 * FP64.ONE)) - FP64.ONE);
                FP64 y = FP64.FromRaw((long)(NextUInt64() % (2 * FP64.ONE)) - FP64.ONE);

                FP64 sqrMag = x * x + y * y;
                if (sqrMag <= FP64.One)
                {
                    return new FPVector2(x, y);
                }
            }
        }

        /// <summary>
        /// Random point inside unit sphere (GC-Free)
        /// </summary>
        public FPVector3 NextInsideUnitSphereFP()
        {
            // Rejection sampling
            while (true)
            {
                FP64 x = FP64.FromRaw((long)((long)NextUInt64() % (2 * FP64.ONE)) - FP64.ONE);
                FP64 y = FP64.FromRaw((long)((long)NextUInt64() % (2 * FP64.ONE)) - FP64.ONE);
                FP64 z = FP64.FromRaw((long)((long)NextUInt64() % (2 * FP64.ONE)) - FP64.ONE);

                FP64 sqrMag = x * x + y * y + z * z;
                if (sqrMag <= FP64.One)
                {
                    return new FPVector3(x, y, z);
                }
            }
        }

        /// <summary>
        /// 2D random direction vector (GC-Free)
        /// </summary>
        public FPVector2 NextDirection2DFP()
        {
            FP64 angle = NextFP64() * FP64.TwoPi;
            return new FPVector2(FP64.Cos(angle), FP64.Sin(angle));
        }

        /// <summary>
        /// 3D random direction vector - uniform distribution (GC-Free)
        /// </summary>
        public FPVector3 NextDirection3DFP()
        {
            // Generate uniformly distributed direction vector
            // theta: [0, 2π) azimuth angle
            FP64 theta = NextFP64() * FP64.TwoPi;
            
            // z: [-1, 1] uniform distribution (cos(phi) must be uniform for spherical uniform distribution)
            FP64 z = NextFP64() * FP64.FromInt(2) - FP64.One;
            
            // sinPhi = sqrt(1 - z²)
            FP64 sinPhi = FP64.Sqrt(FP64.One - z * z);
            
            return new FPVector3(
                sinPhi * FP64.Cos(theta),
                sinPhi * FP64.Sin(theta),
                z
            );
        }

        #endregion

        public bool NextBool()
        {
            return (NextUInt64() & 1) == 1;
        }

        public bool NextChance(int percent)
        {
            return NextInt(0, 100) < percent;
        }

        /// <summary>
        /// Weight-based random index selection
        /// </summary>
        public int NextWeighted(int[] weights)
        {
            if (weights == null || weights.Length == 0)
                return -1;

            int total = 0;
            foreach (int w in weights)
                total += w;

            if (total <= 0)
                return NextInt(0, weights.Length);

            int roll = NextInt(0, total);
            int cumulative = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return i;
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// </summary>
        public void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
