using System;
using System.Numerics;

namespace xpTURN.Lockstep.Math.Impl
{
    /// <summary>
    /// 64-bit fixed-point number implementation
    /// 32.32 format (upper 32 bits: integer part, lower 32 bits: fractional part)
    /// </summary>
    [Serializable]
    public struct FP64 : IFixedPoint, IEquatable<FP64>, IComparable<FP64>
    {
        public const int FRACTIONAL_BITS = 32;
        public const long ONE = 1L << FRACTIONAL_BITS;
        public const long HALF = ONE >> 1;

        public static readonly FP64 Zero = new FP64(0);
        public static readonly FP64 One = new FP64(ONE);
        public static readonly FP64 Half = new FP64(HALF);
        public static readonly FP64 MinValue = new FP64(long.MinValue);
        public static readonly FP64 MaxValue = new FP64(long.MaxValue);
        public static readonly FP64 Pi = FromDouble(3.14159265358979323846);
        public static readonly FP64 TwoPi = FromDouble(6.28318530717958647692);
        public static readonly FP64 HalfPi = FromDouble(1.57079632679489661923);
        public static readonly FP64 Deg2Rad = FromDouble(0.01745329251994329577);
        public static readonly FP64 Rad2Deg = FromDouble(57.29577951308232087680);
        public static readonly FP64 Epsilon = new FP64(1);

        // CORDIC tables for rotation mode (computing sin/cos)
        // atan(2^-i) angles precomputed in FP64
        private static readonly FP64[] CordicAngles = CreateCordicAngles();
        // CORDIC gain: K = ∏√(1 + 2^-2i) ≈ 1.6467... (computed accurately from table)
        private static readonly FP64 CordicGain = ComputeCordicGain();

        private static FP64[] CreateCordicAngles()
        {
            var angles = new FP64[32];
            for (int i = 0; i < angles.Length; i++)
            {
                angles[i] = FromDouble(System.Math.Atan(System.Math.Pow(2.0, -i)));
            }
            return angles;
        }

        private static FP64 ComputeCordicGain()
        {
            // K = ∏√(1 + 2^-2i) for i=0..31
            // In fixed-point: multiply FP64(1 + 2^-2i) iteratively and take sqrt
            FP64 gain = One;
            for (int i = 0; i < 32; i++)
            {
                FP64 factor = One + FromRaw(One.RawValue >> (2 * i));
                gain = gain * Sqrt(factor);
            }
            return gain;
        }

        // LUT-based sin/cos tables: precomputed values for angles in [-PI/2, PI/2]
        // Step size: 0.001 radians (~1571 entries)
        private const double LutStepRadians = 0.001;
        private const int LutSize = (int)((System.Math.PI / 2.0 + 0.0005) / LutStepRadians) + 1; // ~1572
        private static readonly FP64[] SinLut = CreateSinLut();
        private static readonly FP64[] CosLut = CreateCosLut();

        private static FP64[] CreateSinLut()
        {
            var lut = new FP64[LutSize];
            for (int i = 0; i < LutSize; i++)
            {
                double angle = i * LutStepRadians;
                lut[i] = FromDouble(System.Math.Sin(angle));
            }
            return lut;
        }

        private static FP64[] CreateCosLut()
        {
            var lut = new FP64[LutSize];
            for (int i = 0; i < LutSize; i++)
            {
                double angle = i * LutStepRadians;
                lut[i] = FromDouble(System.Math.Cos(angle));
            }
            return lut;
        }

        // CORDIC arctan table (atan(2^-i)) precomputed in FP64
        private static readonly FP64[] AtanTable = CreateAtanTable();

        private static FP64[] CreateAtanTable()
        {
            var table = new FP64[32];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = FromDouble(System.Math.Atan(System.Math.Pow(2.0, -i)));
            }
            return table;
        }

        private long _rawValue;

        public long RawValue => _rawValue;

        private FP64(long rawValue)
        {
            _rawValue = rawValue;
        }

        private FP64(IFixedPoint fixedPoint)
        {
            _rawValue = fixedPoint.RawValue;
        }

        public static FP64 FromRaw(long rawValue)
        {
            return new FP64(rawValue);
        }

        public static FP64 FromInt(int value)
        {
            return new FP64((long)value << FRACTIONAL_BITS);
        }

        public static FP64 FromFloat(float value)
        {
            return new FP64((long)(value * ONE));
        }

        public static FP64 FromDouble(double value)
        {
            return new FP64((long)(value * ONE));
        }

        public float ToFloat()
        {
            return (float)_rawValue / ONE;
        }

        public double ToDouble()
        {
            return (double)_rawValue / ONE;
        }

        public int ToInt()
        {
            return (int)(_rawValue >> FRACTIONAL_BITS);
        }

        // Operator overloading
        public static FP64 operator +(FP64 a, FP64 b)
        {
            return new FP64(a._rawValue + b._rawValue);
        }

        public static FP64 operator -(FP64 a, FP64 b)
        {
            return new FP64(a._rawValue - b._rawValue);
        }

        public static FP64 operator -(FP64 a)
        {
            return new FP64(-a._rawValue);
        }

        public static FP64 operator *(FP64 a, FP64 b)
        {
            return SafeMultiply(a, b);
        }

        public static FP64 operator /(FP64 a, FP64 b)
        {
            if (b._rawValue == 0)
                throw new DivideByZeroException();

            // Safe fixed-point division
            // result_raw = (a_raw << FRACTIONAL_BITS) / b_raw
            // Use BigInteger to handle 128-bit intermediate calculation
            BigInteger num = (BigInteger)a._rawValue << FRACTIONAL_BITS;
            BigInteger den = (BigInteger)b._rawValue;
            BigInteger q = BigInteger.Divide(num, den);

            // Overflow prevention: clamp result to long range
            if (q > long.MaxValue) q = long.MaxValue;
            if (q < long.MinValue) q = long.MinValue;

            return new FP64((long)q);
        }

        public static FP64 operator %(FP64 a, FP64 b)
        {
            if (b._rawValue == 0)
                return Zero;
            return new FP64(a._rawValue % b._rawValue);
        }

        public static bool operator ==(FP64 a, FP64 b)
        {
            return a._rawValue == b._rawValue;
        }

        public static bool operator !=(FP64 a, FP64 b)
        {
            return a._rawValue != b._rawValue;
        }

        public static bool operator <(FP64 a, FP64 b)
        {
            return a._rawValue < b._rawValue;
        }

        public static bool operator >(FP64 a, FP64 b)
        {
            return a._rawValue > b._rawValue;
        }

        public static bool operator <=(FP64 a, FP64 b)
        {
            return a._rawValue <= b._rawValue;
        }

        public static bool operator >=(FP64 a, FP64 b)
        {
            return a._rawValue >= b._rawValue;
        }

        // Implicit conversion
        public static implicit operator FP64(int value)
        {
            return FromInt(value);
        }

        // Math functions
        public static FP64 Abs(FP64 value)
        {
            return value._rawValue < 0 ? new FP64(-value._rawValue) : value;
        }

        public static FP64 Floor(FP64 value)
        {
            return new FP64(value._rawValue & ~(ONE - 1));
        }

        public static FP64 Ceiling(FP64 value)
        {
            long fractional = value._rawValue & (ONE - 1);
            if (fractional == 0)
                return value;
            return new FP64((value._rawValue & ~(ONE - 1)) + ONE);
        }

        public static FP64 Round(FP64 value)
        {
            long fractional = value._rawValue & (ONE - 1);
            if (fractional >= HALF)
                return Ceiling(value);
            return Floor(value);
        }

        public static FP64 Min(FP64 a, FP64 b)
        {
            return a._rawValue < b._rawValue ? a : b;
        }

        public static FP64 Max(FP64 a, FP64 b)
        {
            return a._rawValue > b._rawValue ? a : b;
        }

        public static FP64 Clamp(FP64 value, FP64 min, FP64 max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static FP64 Clamp01(FP64 value)
        {
            return Clamp(value, Zero, One);
        }

        public static FP64 Lerp(FP64 a, FP64 b, FP64 t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static FP64 LerpUnclamped(FP64 a, FP64 b, FP64 t)
        {
            return a + (b - a) * t;
        }

        public static int Sign(FP64 value)
        {
            if (value._rawValue > 0) return 1;
            if (value._rawValue < 0) return -1;
            return 0;
        }

        public static FP64 Sqrt(FP64 value)
        {
            if (value._rawValue < 0)
                throw new ArgumentException("Cannot calculate square root of negative number");

            if (value._rawValue == 0)
                return Zero;

            // Use ulong to prevent overflow
            ulong n = (ulong)value._rawValue;
            ulong result;

            // Use different algorithms based on value size
            if (n < (1UL << 32))
            {
                // Small value: scale up for precision then Newton-Raphson
                ulong scaled = n << 32;
                result = scaled;
                ulong x1;

                while (true)
                {
                    x1 = (result + scaled / result) >> 1;
                    if (x1 >= result)
                        break;
                    result = x1;
                }
            }
            else
            {
                // Large value: calculate square root first then scale to prevent overflow
                // sqrt(n * 2^32) = sqrt(n) * 2^16
                result = n;
                ulong x1;

                while (true)
                {
                    x1 = (result + n / result) >> 1;
                    if (x1 >= result)
                        break;
                    result = x1;
                }

                // Scale adjustment: left shift by 16 bits
                result <<= 16;
            }

            return new FP64((long)result);
        }

        // Trigonometric functions (Taylor series based)
        public static FP64 Sin(FP64 angle)
        {
            angle = NormalizeAngle(angle);
            
            // Convert to floating-point for calculation
            double rad = angle.ToDouble();
            
            // sin(-x) = -sin(x)
            bool negate = false;
            if (rad < 0)
            {
                rad = -rad;
                negate = true;
            }
            
            // Within [0, π] range
            // sin(π - x) = sin(x), so reduce to [0, π/2]
            if (rad > System.Math.PI / 2)
            {
                rad = System.Math.PI - rad;
            }
            
            // Now rad ∈ [0, π/2], use LUT
            double index = rad / LutStepRadians;
            int idx = (int)index;
            double frac = index - idx;
            
            // Range check
            if (idx < 0) idx = 0;
            if (idx >= LutSize - 1) idx = LutSize - 2;
            
            // Linear interpolation
            double v0 = SinLut[idx].ToDouble();
            double v1 = SinLut[idx + 1].ToDouble();
            double result = v0 + (v1 - v0) * frac;
            
            if (negate)
                result = -result;
            
            return FromDouble(result);
        }

        public static FP64 Cos(FP64 angle)
        {
            // Normalize range to [-π, π]
            angle = NormalizeAngle(angle);

            // Convert to floating-point for calculation (for precision)
            double rad = angle.ToDouble();
            
            // cos is an even function: cos(-x) = cos(x)
            if (rad < 0)
                rad = -rad;
            
            // Within [0, π] range
            // cos(π - x) = -cos(x), so reduce to [0, π/2]
            bool negate = false;
            if (rad > System.Math.PI / 2)
            {
                rad = System.Math.PI - rad;
                negate = true;
            }
            
            // Now rad ∈ [0, π/2], use LUT
            double index = rad / LutStepRadians;
            int idx = (int)index;
            double frac = index - idx;
            
            // Range check
            if (idx < 0) idx = 0;
            if (idx >= LutSize - 1) idx = LutSize - 2;
            
            // Linear interpolation
            double v0 = CosLut[idx].ToDouble();
            double v1 = CosLut[idx + 1].ToDouble();
            double result = v0 + (v1 - v0) * frac;
            
            if (negate)
                result = -result;
            
            return FromDouble(result);
        }

        /// <summary>
        /// CORDIC-based Sin calculation (more refined implementation)
        /// Works for all angle ranges with precision verification included
        /// </summary>
        public static FP64 SinCordic(FP64 angle)
        {
            var (_, sin) = CordicRotation(angle);
            return sin;
        }

        /// <summary>
        /// CORDIC-based Cos calculation (more refined implementation)
        /// Works for all angle ranges with precision verification included
        /// </summary>
        public static FP64 CosCordic(FP64 angle)
        {
            var (cos, _) = CordicRotation(angle);
            return cos;
        }

        /// <summary>
        /// Precision verification: compare FP64 and System.Math results
        /// </summary>
        public static double VerifyPrecision(FP64 fpValue, double expectedValue)
        {
            double fpDouble = fpValue.ToDouble();
            return System.Math.Abs(fpDouble - expectedValue);
        }

        /// <summary>
        /// CORDIC convergence verification
        /// Calculate Sin/Cos at various angles and collect error statistics
        /// </summary>
        public static (double maxError, double avgError, int passCount) VerifyCordicConvergence(
            double[] testAngles, double tolerance = 1e-6)
        {
            double maxError = 0;
            double sumError = 0;
            int passCount = 0;

            foreach (var angle in testAngles)
            {
                var fpAngle = FromDouble(angle);
                
                // Sin verification
                double fpSin = SinCordic(fpAngle).ToDouble();
                double sysSin = System.Math.Sin(angle);
                double sinError = System.Math.Abs(fpSin - sysSin);
                
                // Cos verification
                double fpCos = CosCordic(fpAngle).ToDouble();
                double sysCos = System.Math.Cos(angle);
                double cosError = System.Math.Abs(fpCos - sysCos);

                double angleError = System.Math.Max(sinError, cosError);
                maxError = System.Math.Max(maxError, angleError);
                sumError += angleError;

                if (angleError <= tolerance)
                    passCount++;
            }

            double avgError = testAngles.Length > 0 ? sumError / testAngles.Length : 0;
            return (maxError, avgError, passCount);
        }

        /// <summary>
        /// CORDIC Atan2 verification
        /// </summary>
        public static (double maxError, double avgError) VerifyCordicAtan2(
            (double y, double x)[] testPoints)
        {
            double maxError = 0;
            double sumError = 0;

            foreach (var (y, x) in testPoints)
            {
                var fpY = FromDouble(y);
                var fpX = FromDouble(x);
                
                double fpAtan2 = Atan2(fpY, fpX).ToDouble();
                double sysAtan2 = System.Math.Atan2(y, x);
                double error = System.Math.Abs(fpAtan2 - sysAtan2);

                maxError = System.Math.Max(maxError, error);
                sumError += error;
            }

            double avgError = testPoints.Length > 0 ? sumError / testPoints.Length : 0;
            return (maxError, avgError);
        }

        private static (FP64 cos, FP64 sin) CordicRotation(FP64 angle)
        {
            // Normalize range to [-π, π]
            FP64 reducedAngle = NormalizeAngle(angle);
            bool cosNegate = false;
            bool sinNegate = false;

            // Reduce range to [0, π/2]
            // CORDIC only works correctly in this range
            
            if (reducedAngle < Zero)
            {
                // [-π, 0] → [0, π]
                reducedAngle = -reducedAngle;
                // cos(-x) = cos(x), sin(-x) = -sin(x)
                sinNegate = true;
            }
            
            if (reducedAngle > HalfPi)
            {
                // (π/2, π] → [0, π/2]
                // cos(π - x) = -cos(x)
                // sin(π - x) = sin(x)
                reducedAngle = Pi - reducedAngle;
                cosNegate = true;
            }

            // Now reducedAngle ∈ [0, π/2]
            // Perform CORDIC rotation
            FP64 x = One;
            FP64 y = Zero;
            FP64 z = reducedAngle;

            for (int i = 0; i < CordicAngles.Length; i++)
            {
                FP64 xShift = FromRaw(x.RawValue >> i);
                FP64 yShift = FromRaw(y.RawValue >> i);
                FP64 angle_i = CordicAngles[i];

                if (z.RawValue >= 0)
                {
                    x = x - yShift;
                    y = y + xShift;
                    z = z - angle_i;
                }
                else
                {
                    x = x + yShift;
                    y = y - xShift;
                    z = z + angle_i;
                }
            }

            // CORDIC result: (x ≈ K cos(θ), y ≈ K sin(θ))
            // K = ∏√(1 + 2^-2i) ≈ 1.6467...
            FP64 cosResult = x / CordicGain;
            FP64 sinResult = y / CordicGain;

            // Apply quadrant correction
            if (cosNegate)
                cosResult = -cosResult;
            if (sinNegate)
                sinResult = -sinResult;

            return (cosResult, sinResult);
        }

        public static FP64 Acos(FP64 x)
        {
            // Clamp if x is outside [-1, 1] range
            if (x <= -One)
                return Pi;
            if (x >= One)
                return Zero;

            // acos(x) = atan2(sqrt(1 - x²), x)
            FP64 oneMinusX2 = One - x * x;
            FP64 sqrtPart = Sqrt(oneMinusX2);
            return Atan2(sqrtPart, x);
        }

        public static FP64 Tan(FP64 angle)
        {
            FP64 cos = Cos(angle);
            if (cos == Zero)
                return MaxValue;
            return Sin(angle) / cos;
        }

        public static FP64 Atan2(FP64 y, FP64 x)
        {
            // CORDIC vectoring mode: calculate angle of (x, y) vector
            // Pure fixed-point, fast convergence, supports all quadrants

            // Special case: both x and y are 0
            if (x == Zero && y == Zero)
                return Zero;

            // Special case: y-axis (x = 0)
            if (x == Zero)
            {
                if (y > Zero) return HalfPi;
                if (y < Zero) return -HalfPi;
                return Zero;
            }

            // Special case: negative x on y-axis (exactly π)
            if (y == Zero && x.RawValue < 0)
                return Pi;

            // Working copies
            FP64 vx = x;
            FP64 vy = y;
            FP64 angle = Zero;

            // Step 1: Quadrant correction
            // CORDIC vectoring assumes vx >= 0, so pre-rotate if vx < 0
            if (vx.RawValue < 0)
            {
                vx = -vx;
                vy = -vy;
                angle = Pi;
            }

            // Step 2: Range reduction (optional but improves convergence speed)
            // If magnitude is very large, normalize by bit shifting (angle remains unchanged)
            // Reduce if ||(vx, vy)|| is very large
            int shifts = 0;
            while ((vx.RawValue > (1L << 40)) || (vy.RawValue > (1L << 40)))
            {
                vx = FromRaw(vx.RawValue >> 1);
                vy = FromRaw(vy.RawValue >> 1);
                shifts++;
            }

            // Step 3: CORDIC vectoring iteration
            // Each iteration: rotate vx, vy so vy → 0 converges
            // Result: angle ≈ atan2(y, x)
            for (int i = 0; i < AtanTable.Length; i++)
            {
                // Bit shift for 2^-i scaling
                FP64 vxShift = FromRaw(vx.RawValue >> i);
                FP64 vyShift = FromRaw(vy.RawValue >> i);
                FP64 angle_i = AtanTable[i];

                if (vy.RawValue > 0)
                {
                    // vy > 0: rotate clockwise (-atan(2^-i))
                    // Rotate vector towards vy → 0
                    vx = vx + vyShift;
                    vy = vy - vxShift;
                    angle = angle + angle_i;
                }
                else
                {
                    // vy <= 0: rotate counter-clockwise (+atan(2^-i))
                    vx = vx - vyShift;
                    vy = vy + vxShift;
                    angle = angle - angle_i;
                }
            }

            // Step 4: Final normalization
            angle = NormalizeAngle(angle);
            return angle;
        }

        private static FP64 NormalizeAngle(FP64 angle)
        {
            while (angle > Pi)
                angle = angle - TwoPi;
            while (angle < -Pi)
                angle = angle + TwoPi;
            return angle;
        }

        /// <summary>
        /// Helper method to prevent fixed-point multiplication overflow using BigInteger
        /// </summary>
        public static FP64 SafeMultiply(FP64 a, FP64 b)
        {
            BigInteger aBig = (BigInteger)a.RawValue;
            BigInteger bBig = (BigInteger)b.RawValue;
            BigInteger product = aBig * bBig;
            BigInteger result = product >> FP64.FRACTIONAL_BITS;
            
            // 
            if (result < long.MinValue || result > long.MaxValue)
                UnityEngine.Debug.LogError($"Multiplication overflow in FP64 : {a.ToDouble()} * {b.ToDouble()}");

            // Overflow prevention
            if (result > long.MaxValue) result = long.MaxValue;
            if (result < long.MinValue) result = long.MinValue;
            
            return FP64.FromRaw((long)result);
        }

        // IEquatable, IComparable implementation
        public bool Equals(FP64 other)
        {
            return _rawValue == other._rawValue;
        }

        public override bool Equals(object obj)
        {
            return obj is FP64 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _rawValue.GetHashCode();
        }

        public int CompareTo(FP64 other)
        {
            return _rawValue.CompareTo(other._rawValue);
        }

        public override string ToString()
        {
            return ToDouble().ToString("F4");
        }

        public string ToString(string format)
        {
            return ToDouble().ToString(format);
        }
    }
}
