using System;
using System.Numerics;

namespace xpTURN.Lockstep.Math.Impl
{
    /// <summary>
    /// Fixed-point 3D vector implementation
    /// </summary>
    [Serializable]
    public struct FPVector3 : IFixedVector3, IEquatable<FPVector3>
    {
        public FP64 x;
        public FP64 y;
        public FP64 z;

        public static readonly FPVector3 Zero = new FPVector3(FP64.Zero, FP64.Zero, FP64.Zero);
        public static readonly FPVector3 One = new FPVector3(FP64.One, FP64.One, FP64.One);
        public static readonly FPVector3 Up = new FPVector3(FP64.Zero, FP64.One, FP64.Zero);
        public static readonly FPVector3 Down = new FPVector3(FP64.Zero, -FP64.One, FP64.Zero);
        public static readonly FPVector3 Left = new FPVector3(-FP64.One, FP64.Zero, FP64.Zero);
        public static readonly FPVector3 Right = new FPVector3(FP64.One, FP64.Zero, FP64.Zero);
        public static readonly FPVector3 Forward = new FPVector3(FP64.Zero, FP64.Zero, FP64.One);
        public static readonly FPVector3 Back = new FPVector3(FP64.Zero, FP64.Zero, -FP64.One);

        public FPVector3(FP64 x, FP64 y, FP64 z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public FPVector3(int x, int y, int z)
        {
            this.x = FP64.FromInt(x);
            this.y = FP64.FromInt(y);
            this.z = FP64.FromInt(z);
        }

        public FPVector3(float x, float y, float z)
        {
            this.x = FP64.FromFloat(x);
            this.y = FP64.FromFloat(y);
            this.z = FP64.FromFloat(z);
        }

#if UNITY_2021_1_OR_NEWER
        public static FPVector3 FromVector3(UnityEngine.Vector3 v)
        {
            return new FPVector3(FP64.FromFloat(v.x), FP64.FromFloat(v.y), FP64.FromFloat(v.z));
        }
#endif

        // IFixedVector3 implementation
        public IFixedPoint X => x;
        public IFixedPoint Y => y;
        public IFixedPoint Z => z;

        public IFixedPoint SqrMagnitude
        {
            get
            {
                BigInteger xRaw = (BigInteger)x.RawValue;
                BigInteger yRaw = (BigInteger)y.RawValue;
                BigInteger zRaw = (BigInteger)z.RawValue;
                
                BigInteger result = xRaw * xRaw + yRaw * yRaw + zRaw * zRaw;
                result = result >> 32;  // Fixed-point format adjustment
                
                if (result > long.MaxValue) result = long.MaxValue;
                if (result < long.MinValue) result = long.MinValue;
                
                return FP64.FromRaw((long)result);
            }
        }

        public IFixedPoint Magnitude => FP64.Sqrt((FP64)SqrMagnitude);

        public IFixedVector3 Normalized
        {
            get
            {
                FP64 mag = (FP64)Magnitude;
                if (mag == FP64.Zero)
                    return Zero;
                return new FPVector3(x / mag, y / mag, z / mag);
            }
        }

        public FPVector3 normalized => (FPVector3)Normalized;

        public FP64 sqrMagnitude => (FP64)SqrMagnitude;
        public FP64 magnitude => (FP64)Magnitude;

#if UNITY_2021_1_OR_NEWER
        public UnityEngine.Vector3 ToVector3()
        {
            return new UnityEngine.Vector3(x.ToFloat(), y.ToFloat(), z.ToFloat());
        }
#endif

        /// <summary>
        /// Convert to float array [x, y, z]
        /// </summary>
        public float[] ToFloatArray()
        {
            return new float[] { x.ToFloat(), y.ToFloat(), z.ToFloat() };
        }

        // Operator overloading
        public static FPVector3 operator +(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static FPVector3 operator -(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static FPVector3 operator -(FPVector3 a)
        {
            return new FPVector3(-a.x, -a.y, -a.z);
        }

        public static FPVector3 operator *(FPVector3 a, FP64 scalar)
        {
            return new FPVector3(a.x * scalar, a.y * scalar, a.z * scalar);
        }

        public static FPVector3 operator *(FP64 scalar, FPVector3 a)
        {
            return new FPVector3(a.x * scalar, a.y * scalar, a.z * scalar);
        }

        public static FPVector3 operator /(FPVector3 a, FP64 scalar)
        {
            if (scalar == FP64.Zero)
                return Zero;
            return new FPVector3(a.x / scalar, a.y / scalar, a.z / scalar);
        }

        public static bool operator ==(FPVector3 a, FPVector3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(FPVector3 a, FPVector3 b)
        {
            return !(a == b);
        }

        // Static methods
        public static FP64 Dot(FPVector3 a, FPVector3 b)
        {
            BigInteger aXRaw = (BigInteger)a.x.RawValue;
            BigInteger aYRaw = (BigInteger)a.y.RawValue;
            BigInteger aZRaw = (BigInteger)a.z.RawValue;
            BigInteger bXRaw = (BigInteger)b.x.RawValue;
            BigInteger bYRaw = (BigInteger)b.y.RawValue;
            BigInteger bZRaw = (BigInteger)b.z.RawValue;
            
            BigInteger result = aXRaw * bXRaw + aYRaw * bYRaw + aZRaw * bZRaw;
            result = result >> 32;  // Fixed-point format adjustment
            
            if (result > long.MaxValue) result = long.MaxValue;
            if (result < long.MinValue) result = long.MinValue;
            
            return FP64.FromRaw((long)result);
        }

        public static FPVector3 Cross(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        public static FP64 Distance(FPVector3 a, FPVector3 b)
        {
            return (a - b).magnitude;
        }

        public static FP64 SqrDistance(FPVector3 a, FPVector3 b)
        {
            return (a - b).sqrMagnitude;
        }

        public static FPVector3 Lerp(FPVector3 a, FPVector3 b, FP64 t)
        {
            t = FP64.Clamp01(t);
            return new FPVector3(
                FP64.LerpUnclamped(a.x, b.x, t),
                FP64.LerpUnclamped(a.y, b.y, t),
                FP64.LerpUnclamped(a.z, b.z, t)
            );
        }

        public static FPVector3 LerpUnclamped(FPVector3 a, FPVector3 b, FP64 t)
        {
            return new FPVector3(
                FP64.LerpUnclamped(a.x, b.x, t),
                FP64.LerpUnclamped(a.y, b.y, t),
                FP64.LerpUnclamped(a.z, b.z, t)
            );
        }

        public static FPVector3 MoveTowards(FPVector3 current, FPVector3 target, FP64 maxDistanceDelta)
        {
            FPVector3 diff = target - current;
            FP64 dist = diff.magnitude;

            if (dist <= maxDistanceDelta || dist == FP64.Zero)
                return target;

            return current + diff / dist * maxDistanceDelta;
        }

        public static FPVector3 Reflect(FPVector3 direction, FPVector3 normal)
        {
            FP64 dot2 = FP64.FromInt(2) * Dot(direction, normal);
            return direction - normal * dot2;
        }

        public static FPVector3 Project(FPVector3 vector, FPVector3 onNormal)
        {
            FP64 sqrMag = onNormal.sqrMagnitude;
            if (sqrMag == FP64.Zero)
                return Zero;

            FP64 dot = Dot(vector, onNormal);
            return onNormal * dot / sqrMag;
        }

        public static FPVector3 ProjectOnPlane(FPVector3 vector, FPVector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        public static FP64 Angle(FPVector3 from, FPVector3 to)
        {
            FP64 denominator = from.magnitude * to.magnitude;
            if (denominator == FP64.Zero)
                return FP64.Zero;

            FP64 dot = FP64.Clamp(Dot(from, to) / denominator, -FP64.One, FP64.One);
            return FP64.Acos(dot) * FP64.Rad2Deg;
        }

        public static FPVector3 ClampMagnitude(FPVector3 vector, FP64 maxLength)
        {
            FP64 sqrMag = vector.sqrMagnitude;
            if (sqrMag > maxLength * maxLength)
            {
                FP64 mag = FP64.Sqrt(sqrMag);
                if (mag == FP64.Zero)
                    return Zero;
                return vector / mag * maxLength;
            }
            return vector;
        }

        public static FPVector3 Min(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(FP64.Min(a.x, b.x), FP64.Min(a.y, b.y), FP64.Min(a.z, b.z));
        }

        public static FPVector3 Max(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(FP64.Max(a.x, b.x), FP64.Max(a.y, b.y), FP64.Max(a.z, b.z));
        }

        public static FPVector3 Scale(FPVector3 a, FPVector3 b)
        {
            return new FPVector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        // XY, XZ plane conversion
        public FPVector2 ToXY()
        {
            return new FPVector2(x, y);
        }

        public FPVector2 ToXZ()
        {
            return new FPVector2(x, z);
        }

        // IEquatable implementation
        public bool Equals(FPVector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is FPVector3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
}
