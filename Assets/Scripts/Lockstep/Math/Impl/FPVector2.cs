using System;
using System.Numerics;

namespace xpTURN.Lockstep.Math.Impl
{
    /// <summary>
    /// Fixed-point 2D vector implementation
    /// </summary>
    [Serializable]
    public struct FPVector2 : IFixedVector2, IEquatable<FPVector2>
    {
        public FP64 x;
        public FP64 y;

        public static readonly FPVector2 Zero = new FPVector2(FP64.Zero, FP64.Zero);
        public static readonly FPVector2 One = new FPVector2(FP64.One, FP64.One);
        public static readonly FPVector2 Up = new FPVector2(FP64.Zero, FP64.One);
        public static readonly FPVector2 Down = new FPVector2(FP64.Zero, -FP64.One);
        public static readonly FPVector2 Left = new FPVector2(-FP64.One, FP64.Zero);
        public static readonly FPVector2 Right = new FPVector2(FP64.One, FP64.Zero);

        public FPVector2(FP64 x, FP64 y)
        {
            this.x = x;
            this.y = y;
        }

        public FPVector2(int x, int y)
        {
            this.x = FP64.FromInt(x);
            this.y = FP64.FromInt(y);
        }

        public FPVector2(float x, float y)
        {
            this.x = FP64.FromFloat(x);
            this.y = FP64.FromFloat(y);
        }

#if UNITY_2021_1_OR_NEWER
        public static FPVector2 FromVector2(UnityEngine.Vector2 v)
        {
            return new FPVector2(FP64.FromFloat(v.x), FP64.FromFloat(v.y));
        }
#endif

        // IFixedVector2 implementation
        public IFixedPoint X => x;
        public IFixedPoint Y => y;

        public IFixedPoint SqrMagnitude
        {
            get
            {
                BigInteger xRaw = (BigInteger)x.RawValue;
                BigInteger yRaw = (BigInteger)y.RawValue;
                
                BigInteger result = xRaw * xRaw + yRaw * yRaw;
                result = result >> 32;  // Fixed-point format adjustment
                
                if (result > long.MaxValue) result = long.MaxValue;
                if (result < long.MinValue) result = long.MinValue;
                
                return FP64.FromRaw((long)result);
            }
        }

        public IFixedPoint Magnitude => FP64.Sqrt((FP64)SqrMagnitude);

        public IFixedVector2 Normalized
        {
            get
            {
                FP64 mag = (FP64)Magnitude;
                if (mag == FP64.Zero)
                    return Zero;
                return new FPVector2(x / mag, y / mag);
            }
        }

        public FPVector2 normalized => (FPVector2)Normalized;

        public FP64 sqrMagnitude => (FP64)SqrMagnitude;
        public FP64 magnitude => (FP64)Magnitude;

#if UNITY_2021_1_OR_NEWER
        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(x.ToFloat(), y.ToFloat());
        }
#endif

        /// <summary>
        /// Convert to float array [x, y]
        /// </summary>
        public float[] ToFloatArray()
        {
            return new float[] { x.ToFloat(), y.ToFloat() };
        }

        // Operator overloading
        public static FPVector2 operator +(FPVector2 a, FPVector2 b)
        {
            return new FPVector2(a.x + b.x, a.y + b.y);
        }

        public static FPVector2 operator -(FPVector2 a, FPVector2 b)
        {
            return new FPVector2(a.x - b.x, a.y - b.y);
        }

        public static FPVector2 operator -(FPVector2 a)
        {
            return new FPVector2(-a.x, -a.y);
        }

        public static FPVector2 operator *(FPVector2 a, FP64 scalar)
        {
            return new FPVector2(a.x * scalar, a.y * scalar);
        }

        public static FPVector2 operator *(FP64 scalar, FPVector2 a)
        {
            return new FPVector2(a.x * scalar, a.y * scalar);
        }

        public static FPVector2 operator /(FPVector2 a, FP64 scalar)
        {
            return new FPVector2(a.x / scalar, a.y / scalar);
        }

        public static bool operator ==(FPVector2 a, FPVector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(FPVector2 a, FPVector2 b)
        {
            return !(a == b);
        }

        // Static methods
        public static FP64 Dot(FPVector2 a, FPVector2 b)
        {
            BigInteger aXRaw = (BigInteger)a.x.RawValue;
            BigInteger aYRaw = (BigInteger)a.y.RawValue;
            BigInteger bXRaw = (BigInteger)b.x.RawValue;
            BigInteger bYRaw = (BigInteger)b.y.RawValue;
            
            BigInteger result = aXRaw * bXRaw + aYRaw * bYRaw;
            result = result >> 32;  // Fixed-point format adjustment
            
            if (result > long.MaxValue) result = long.MaxValue;
            if (result < long.MinValue) result = long.MinValue;
            
            return FP64.FromRaw((long)result);
        }

        public static FP64 Cross(FPVector2 a, FPVector2 b)
        {
            BigInteger aXRaw = (BigInteger)a.x.RawValue;
            BigInteger aYRaw = (BigInteger)a.y.RawValue;
            BigInteger bXRaw = (BigInteger)b.x.RawValue;
            BigInteger bYRaw = (BigInteger)b.y.RawValue;
            
            BigInteger result = aXRaw * bYRaw - aYRaw * bXRaw;
            result = result >> 32;  // Fixed-point format adjustment
            
            if (result > long.MaxValue) result = long.MaxValue;
            if (result < long.MinValue) result = long.MinValue;
            
            return FP64.FromRaw((long)result);
        }

        public static FP64 Distance(FPVector2 a, FPVector2 b)
        {
            return (a - b).magnitude;
        }

        public static FP64 SqrDistance(FPVector2 a, FPVector2 b)
        {
            return (a - b).sqrMagnitude;
        }

        public static FPVector2 Lerp(FPVector2 a, FPVector2 b, FP64 t)
        {
            t = FP64.Clamp01(t);
            return new FPVector2(
                FP64.LerpUnclamped(a.x, b.x, t),
                FP64.LerpUnclamped(a.y, b.y, t)
            );
        }

        public static FPVector2 LerpUnclamped(FPVector2 a, FPVector2 b, FP64 t)
        {
            return new FPVector2(
                FP64.LerpUnclamped(a.x, b.x, t),
                FP64.LerpUnclamped(a.y, b.y, t)
            );
        }

        public static FPVector2 MoveTowards(FPVector2 current, FPVector2 target, FP64 maxDistanceDelta)
        {
            FPVector2 diff = target - current;
            FP64 dist = diff.magnitude;

            if (dist <= maxDistanceDelta || dist == FP64.Zero)
                return target;

            return current + diff / dist * maxDistanceDelta;
        }

        public static FPVector2 Reflect(FPVector2 direction, FPVector2 normal)
        {
            FP64 dot2 = FP64.FromInt(2) * Dot(direction, normal);
            return direction - normal * dot2;
        }

        public static FPVector2 Perpendicular(FPVector2 direction)
        {
            return new FPVector2(-direction.y, direction.x);
        }

        public static FP64 Angle(FPVector2 from, FPVector2 to)
        {
            FP64 denominator = FP64.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator == FP64.Zero)
                return FP64.Zero;

            FP64 dot = FP64.Clamp(Dot(from, to) / denominator, -FP64.One, FP64.One);
            return FP64.Acos(dot) * FP64.Rad2Deg;
        }

        public static FP64 SignedAngle(FPVector2 from, FPVector2 to)
        {
            FP64 angle = Angle(from, to);
            FP64 cross = Cross(from, to);
            return angle * FP64.FromInt(FP64.Sign(cross) == 0 ? 1 : FP64.Sign(cross));
        }

        public static FPVector2 ClampMagnitude(FPVector2 vector, FP64 maxLength)
        {
            FP64 sqrMag = vector.sqrMagnitude;
            if (sqrMag > maxLength * maxLength)
            {
                FP64 mag = FP64.Sqrt(sqrMag);
                return vector / mag * maxLength;
            }
            return vector;
        }

        public static FPVector2 Min(FPVector2 a, FPVector2 b)
        {
            return new FPVector2(FP64.Min(a.x, b.x), FP64.Min(a.y, b.y));
        }

        public static FPVector2 Max(FPVector2 a, FPVector2 b)
        {
            return new FPVector2(FP64.Max(a.x, b.x), FP64.Max(a.y, b.y));
        }

        // IEquatable implementation
        public bool Equals(FPVector2 other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is FPVector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}
