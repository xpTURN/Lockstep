namespace xpTURN.Lockstep.Math
{
    /// <summary>
    /// Fixed point number interface
    /// Used instead of floating point for deterministic math operations
    /// </summary>
    public interface IFixedPoint
    {
        /// <summary>
        /// Raw integer value
        /// </summary>
        long RawValue { get; }

        /// <summary>
        /// Convert to float (for debug/rendering, do not use in deterministic operations)
        /// </summary>
        float ToFloat();

        /// <summary>
        /// Convert to double (for debug/rendering, do not use in deterministic operations)
        /// </summary>
        double ToDouble();

        /// <summary>
        /// Convert to integer
        /// </summary>
        int ToInt();
    }

    /// <summary>
    /// Fixed point math utility interface
    /// </summary>
    public interface IFixedMath
    {
        /// <summary>
        /// Add two fixed point numbers
        /// </summary>
        IFixedPoint Add(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Subtract two fixed point numbers
        /// </summary>
        IFixedPoint Subtract(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Multiply two fixed point numbers
        /// </summary>
        IFixedPoint Multiply(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Divide two fixed point numbers
        /// </summary>
        IFixedPoint Divide(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Square root
        /// </summary>
        IFixedPoint Sqrt(IFixedPoint value);

        /// <summary>
        /// Absolute value
        /// </summary>
        IFixedPoint Abs(IFixedPoint value);

        /// <summary>
        /// Minimum value
        /// </summary>
        IFixedPoint Min(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Maximum value
        /// </summary>
        IFixedPoint Max(IFixedPoint a, IFixedPoint b);

        /// <summary>
        /// Sine (angle in radians)
        /// </summary>
        IFixedPoint Sin(IFixedPoint angle);

        /// <summary>
        /// Cosine (angle in radians)
        /// </summary>
        IFixedPoint Cos(IFixedPoint angle);

        /// <summary>
        /// Arc tangent 2
        /// </summary>
        IFixedPoint Atan2(IFixedPoint y, IFixedPoint x);

        /// <summary>
        /// Clamp value
        /// </summary>
        IFixedPoint Clamp(IFixedPoint value, IFixedPoint min, IFixedPoint max);

        /// <summary>
        /// Linear interpolation
        /// </summary>
        IFixedPoint Lerp(IFixedPoint a, IFixedPoint b, IFixedPoint t);
    }
}
