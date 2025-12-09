namespace xpTURN.Lockstep.Math
{
    /// <summary>
    /// 2D fixed point vector interface
    /// </summary>
    public interface IFixedVector2
    {
        IFixedPoint X { get; }
        IFixedPoint Y { get; }

        /// <summary>
        /// Vector length (squared)
        /// </summary>
        IFixedPoint SqrMagnitude { get; }

        /// <summary>
        /// Vector length
        /// </summary>
        IFixedPoint Magnitude { get; }

        /// <summary>
        /// Normalized vector
        /// </summary>
        IFixedVector2 Normalized { get; }

        /// <summary>
        /// Convert to Unity Vector2 (for rendering)
        /// </summary>
        UnityEngine.Vector2 ToVector2();
    }

    /// <summary>
    /// 3D fixed point vector interface
    /// </summary>
    public interface IFixedVector3
    {
        IFixedPoint X { get; }
        IFixedPoint Y { get; }
        IFixedPoint Z { get; }

        /// <summary>
        /// Vector length (squared)
        /// </summary>
        IFixedPoint SqrMagnitude { get; }

        /// <summary>
        /// Vector length
        /// </summary>
        IFixedPoint Magnitude { get; }

        /// <summary>
        /// Normalized vector
        /// </summary>
        IFixedVector3 Normalized { get; }

        /// <summary>
        /// Convert to Unity Vector3 (for rendering)
        /// </summary>
        UnityEngine.Vector3 ToVector3();
    }

    /// <summary>
    /// Vector math utility interface
    /// </summary>
    public interface IFixedVectorMath
    {
        /// <summary>
        /// Add two vectors
        /// </summary>
        IFixedVector2 Add(IFixedVector2 a, IFixedVector2 b);
        IFixedVector3 Add(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Subtract two vectors
        /// </summary>
        IFixedVector2 Subtract(IFixedVector2 a, IFixedVector2 b);
        IFixedVector3 Subtract(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Scalar multiplication
        /// </summary>
        IFixedVector2 Scale(IFixedVector2 v, IFixedPoint scalar);
        IFixedVector3 Scale(IFixedVector3 v, IFixedPoint scalar);

        /// <summary>
        /// Dot product
        /// </summary>
        IFixedPoint Dot(IFixedVector2 a, IFixedVector2 b);
        IFixedPoint Dot(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Cross product (3D only)
        /// </summary>
        IFixedVector3 Cross(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Distance between two vectors
        /// </summary>
        IFixedPoint Distance(IFixedVector2 a, IFixedVector2 b);
        IFixedPoint Distance(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Distance between two vectors (squared)
        /// </summary>
        IFixedPoint SqrDistance(IFixedVector2 a, IFixedVector2 b);
        IFixedPoint SqrDistance(IFixedVector3 a, IFixedVector3 b);

        /// <summary>
        /// Linear interpolation
        /// </summary>
        IFixedVector2 Lerp(IFixedVector2 a, IFixedVector2 b, IFixedPoint t);
        IFixedVector3 Lerp(IFixedVector3 a, IFixedVector3 b, IFixedPoint t);
    }
}
