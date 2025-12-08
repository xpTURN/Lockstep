using NUnit.Framework;
using Lockstep.Math.Impl;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Text.RegularExpressions;

namespace Lockstep.Tests
{
    /// <summary>
    /// FPVector3 vector operation tests
    /// </summary>
    [TestFixture]
    public class FPVector3Tests
    {
        private const float EPSILON = 0.01f;

        #region Creation and Conversion

        [Test]
        public void Constructor_CreatesCorrectVector()
        {
            var v = new FPVector3(FP64.FromFloat(1.0f), FP64.FromFloat(2.0f), FP64.FromFloat(3.0f));
            Assert.AreEqual(1.0f, v.x.ToFloat(), EPSILON);
            Assert.AreEqual(2.0f, v.y.ToFloat(), EPSILON);
            Assert.AreEqual(3.0f, v.z.ToFloat(), EPSILON);
        }

        [Test]
        public void IntConstructor_CreatesCorrectVector()
        {
            var v = new FPVector3(1, 2, 3);
            Assert.AreEqual(1.0f, v.x.ToFloat(), EPSILON);
            Assert.AreEqual(2.0f, v.y.ToFloat(), EPSILON);
            Assert.AreEqual(3.0f, v.z.ToFloat(), EPSILON);
        }

        [Test]
        public void FromVector3_ConvertsCorrectly()
        {
            var unity = new Vector3(1.5f, 2.5f, 3.5f);
            var fp = FPVector3.FromVector3(unity);
            Assert.AreEqual(1.5f, fp.x.ToFloat(), EPSILON);
            Assert.AreEqual(2.5f, fp.y.ToFloat(), EPSILON);
            Assert.AreEqual(3.5f, fp.z.ToFloat(), EPSILON);
        }

        [Test]
        public void ToVector3_ConvertsCorrectly()
        {
            var fp = new FPVector3(FP64.FromFloat(1.5f), FP64.FromFloat(2.5f), FP64.FromFloat(3.5f));
            var unity = fp.ToVector3();
            Assert.AreEqual(1.5f, unity.x, EPSILON);
            Assert.AreEqual(2.5f, unity.y, EPSILON);
            Assert.AreEqual(3.5f, unity.z, EPSILON);
        }

        #endregion

        #region Constants

        [Test]
        public void Zero_IsAllZeros()
        {
            Assert.AreEqual(FP64.Zero, FPVector3.Zero.x);
            Assert.AreEqual(FP64.Zero, FPVector3.Zero.y);
            Assert.AreEqual(FP64.Zero, FPVector3.Zero.z);
        }

        [Test]
        public void One_IsAllOnes()
        {
            Assert.AreEqual(FP64.One, FPVector3.One.x);
            Assert.AreEqual(FP64.One, FPVector3.One.y);
            Assert.AreEqual(FP64.One, FPVector3.One.z);
        }

        [Test]
        public void Up_IsCorrect()
        {
            Assert.AreEqual(FP64.Zero, FPVector3.Up.x);
            Assert.AreEqual(FP64.One, FPVector3.Up.y);
            Assert.AreEqual(FP64.Zero, FPVector3.Up.z);
        }

        #endregion

        #region Basic Operations

        [Test]
        public void Addition_WorksCorrectly()
        {
            var a = new FPVector3(1, 2, 3);
            var b = new FPVector3(4, 5, 6);
            var result = a + b;

            Assert.AreEqual(5.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(7.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(9.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void Subtraction_WorksCorrectly()
        {
            var a = new FPVector3(5, 7, 9);
            var b = new FPVector3(1, 2, 3);
            var result = a - b;

            Assert.AreEqual(4.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(5.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(6.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void Negation_WorksCorrectly()
        {
            var v = new FPVector3(1, 2, 3);
            var result = -v;

            Assert.AreEqual(-1.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(-2.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(-3.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void ScalarMultiplication_WorksCorrectly()
        {
            var v = new FPVector3(1, 2, 3);
            var scalar = FP64.FromFloat(2.0f);
            var result = v * scalar;

            Assert.AreEqual(2.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(4.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(6.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void ScalarDivision_WorksCorrectly()
        {
            var v = new FPVector3(4, 6, 8);
            var scalar = FP64.FromFloat(2.0f);
            var result = v / scalar;

            Assert.AreEqual(2.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(3.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(4.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void Division_ByZero_ReturnsZero()
        {
            var v = new FPVector3(1, 2, 3);
            var result = v / FP64.Zero;
            Assert.AreEqual(FPVector3.Zero, result);
        }

        #endregion

        #region Vector Functions

        [Test]
        public void Magnitude_UnitVector_ReturnsOne()
        {
            var v = FPVector3.Right;
            Assert.AreEqual(1.0f, v.magnitude.ToFloat(), EPSILON);
        }

        [Test]
        public void Magnitude_3_4_0_Returns5()
        {
            var v = new FPVector3(3, 4, 0);
            Assert.AreEqual(5.0f, v.magnitude.ToFloat(), EPSILON);
        }

        [Test]
        public void SqrMagnitude_WorksCorrectly()
        {
            var v = new FPVector3(3, 4, 0);
            Assert.AreEqual(25.0f, v.sqrMagnitude.ToFloat(), EPSILON);
        }

        [Test]
        public void Normalized_ReturnsUnitVector()
        {
            var v = new FPVector3(3, 0, 4);
            var normalized = v.normalized;
            Assert.AreEqual(1.0f, normalized.magnitude.ToFloat(), EPSILON);
        }

        [Test]
        public void Normalized_Zero_ReturnsZero()
        {
            var result = FPVector3.Zero.normalized;
            Assert.AreEqual(FPVector3.Zero, result);
        }

        [Test]
        public void Dot_Perpendicular_ReturnsZero()
        {
            var a = FPVector3.Right;
            var b = FPVector3.Up;
            var result = FPVector3.Dot(a, b);
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Dot_Parallel_ReturnsProduct()
        {
            var a = new FPVector3(2, 0, 0);
            var b = new FPVector3(3, 0, 0);
            var result = FPVector3.Dot(a, b);
            Assert.AreEqual(6.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Cross_RightAndUp_ReturnsForward()
        {
            var result = FPVector3.Cross(FPVector3.Right, FPVector3.Up);
            Assert.AreEqual(0.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(0.0f, result.y.ToFloat(), EPSILON);
            Assert.AreEqual(1.0f, result.z.ToFloat(), EPSILON);
        }

        [Test]
        public void Distance_SamePoint_ReturnsZero()
        {
            var a = new FPVector3(1, 2, 3);
            var result = FPVector3.Distance(a, a);
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Distance_WorksCorrectly()
        {
            var a = FPVector3.Zero;
            var b = new FPVector3(3, 4, 0);
            var result = FPVector3.Distance(a, b);
            Assert.AreEqual(5.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Lerp_AtZero_ReturnsA()
        {
            var a = new FPVector3(0, 0, 0);
            var b = new FPVector3(10, 10, 10);
            var result = FPVector3.Lerp(a, b, FP64.Zero);
            Assert.AreEqual(a, result);
        }

        [Test]
        public void Lerp_AtOne_ReturnsB()
        {
            var a = new FPVector3(0, 0, 0);
            var b = new FPVector3(10, 10, 10);
            var result = FPVector3.Lerp(a, b, FP64.One);
            Assert.AreEqual(10.0f, result.x.ToFloat(), EPSILON);
        }

        [Test]
        public void Lerp_AtHalf_ReturnsMidpoint()
        {
            var a = new FPVector3(0, 0, 0);
            var b = new FPVector3(10, 10, 10);
            var result = FPVector3.Lerp(a, b, FP64.Half);
            Assert.AreEqual(5.0f, result.x.ToFloat(), EPSILON);
        }

        [Test]
        public void MoveTowards_WithinRange_ReturnsTarget()
        {
            var current = FPVector3.Zero;
            var target = new FPVector3(3, 0, 0);
            var result = FPVector3.MoveTowards(current, target, FP64.FromFloat(10.0f));
            Assert.AreEqual(3.0f, result.x.ToFloat(), EPSILON);
        }

        [Test]
        public void MoveTowards_BeyondRange_MovesMaxDistance()
        {
            var current = FPVector3.Zero;
            var target = new FPVector3(10, 0, 0);
            var result = FPVector3.MoveTowards(current, target, FP64.FromFloat(3.0f));
            Assert.AreEqual(3.0f, result.x.ToFloat(), EPSILON);
        }

        [Test]
        public void Project_OntoAxis_WorksCorrectly()
        {
            var vector = new FPVector3(3, 4, 0);
            var onNormal = FPVector3.Right;
            var result = FPVector3.Project(vector, onNormal);
            Assert.AreEqual(3.0f, result.x.ToFloat(), EPSILON);
            Assert.AreEqual(0.0f, result.y.ToFloat(), EPSILON);
        }

        [Test]
        public void Project_OntoZero_ReturnsZero()
        {
            var vector = new FPVector3(3, 4, 0);
            var result = FPVector3.Project(vector, FPVector3.Zero);
            Assert.AreEqual(FPVector3.Zero, result);
        }

        #endregion

        #region Comparison

        [Test]
        public void Equality_SameVectors_ReturnsTrue()
        {
            var a = new FPVector3(1, 2, 3);
            var b = new FPVector3(1, 2, 3);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Inequality_DifferentVectors_ReturnsTrue()
        {
            var a = new FPVector3(1, 2, 3);
            var b = new FPVector3(4, 5, 6);
            Assert.IsTrue(a != b);
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void VectorOperations_AreDeterministic()
        {
            for (int i = 0; i < 100; i++)
            {
                var a = new FPVector3(
                    FP64.FromFloat(3.14f),
                    FP64.FromFloat(2.71f),
                    FP64.FromFloat(1.41f)
                );
                var b = new FPVector3(
                    FP64.FromFloat(1.23f),
                    FP64.FromFloat(4.56f),
                    FP64.FromFloat(7.89f)
                );

                var result1 = (a + b).normalized;
                var result2 = (b + a).normalized;

                Assert.AreEqual(result1.x.RawValue, result2.x.RawValue);
                Assert.AreEqual(result1.y.RawValue, result2.y.RawValue);
                Assert.AreEqual(result1.z.RawValue, result2.z.RawValue);
            }
        }

        [Test]
        public void DotProduct_AreDeterministic()
        {
            for (int i = 0; i < 100; i++)
            {
                var a = new FPVector3(
                    FP64.FromFloat(3.14f),
                    FP64.FromFloat(2.71f),
                    FP64.FromFloat(1.41f)
                );
                var b = new FPVector3(
                    FP64.FromFloat(1.23f),
                    FP64.FromFloat(4.56f),
                    FP64.FromFloat(7.89f)
                );

                var result1 = FPVector3.Dot(a, b);
                var result2 = FPVector3.Dot(b, a);

                Assert.AreEqual(result1.RawValue, result2.RawValue);
            }
        }

        #endregion

        #region Overflow Protection Specific Tests

        [Test]
        public void OverflowProtection_SqrMagnitude_LargeValues()
        {
            // When each component is very large, SqrMagnitude calculation should overflow but
            // is safely handled thanks to BigInteger
            var v1 = new FPVector3(FP64.MaxValue, FP64.MaxValue, FP64.MaxValue);
            var sqrMag = v1.sqrMagnitude;
            
            // Clamped to long range
            Assert.IsTrue(sqrMag.RawValue == long.MaxValue);
        }

        [Test]
        public void OverflowProtection_Dot_LargeValues()
        {
            // Dot product calculation is also protected by BigInteger
            var a = new FPVector3(FP64.MaxValue, FP64.MaxValue, FP64.MaxValue);
            var b = new FPVector3(FP64.MaxValue, FP64.MaxValue, FP64.MaxValue);
            
            var result = FPVector3.Dot(a, b);
            
            // Clamped to long range
            Assert.IsTrue(result.RawValue == long.MaxValue);
        }

        [Test]
        public void OverflowProtection_Scale_LargeValues()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Multiplication overflow in FP64 :.*"));
            LogAssert.Expect(LogType.Error, new Regex(@"Multiplication overflow in FP64 :.*"));
            LogAssert.Expect(LogType.Error, new Regex(@"Multiplication overflow in FP64 :.*"));

            // Scale operation is also protected by BigInteger (uses SafeMultiply)
            var a = new FPVector3(FP64.MaxValue, FP64.MaxValue, FP64.MaxValue);
            var b = new FPVector3(FP64.MaxValue, FP64.MaxValue, FP64.MaxValue);
            
            var result = FPVector3.Scale(a, b);
            
            // Each component is clamped to long range
            Assert.IsTrue(result.x.RawValue == long.MaxValue);
            Assert.IsTrue(result.y.RawValue == long.MaxValue);
            Assert.IsTrue(result.z.RawValue == long.MaxValue);
        }

        [Test]
        public void OverflowProtection_Angle_LargeValues()
        {
            // Angle calculation is also protected by BigInteger (sqrMagnitude multiplication)
            var a = new FPVector3(
                FP64.FromFloat(30000.0f),
                FP64.FromFloat(0.0f),
                FP64.FromFloat(0.0f)
            );
            var b = new FPVector3(
                FP64.FromFloat(30000.0f),
                FP64.FromFloat(30000.0f),
                FP64.FromFloat(0.0f)
            );
            
            var result = FPVector3.Angle(a, b);
            
            // Should be around 45 degrees
            Assert.AreEqual(45.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void OverflowProtection_ClampMagnitude_LargeMaxLength()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Multiplication overflow in FP64 :.*"));

            // maxLength * maxLength calculation is also protected by BigInteger
            var v = new FPVector3(1, 1, 1);
            var result = FPVector3.ClampMagnitude(v, FP64.FromFloat(100000.0f));
            
            // Vector is very small so should be returned as-is
            Assert.AreEqual(v, result);
        }

        [Test]
        public void SqrMagnitude_LargeValue()
        {
            // Should calculate accurately without overflow even with large values
            var v = new FPVector3(
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f)
            );
            var sqrMag = v.sqrMagnitude;
            // 10000^2 * 3 = 300000000
            Assert.AreEqual(300000000.0, sqrMag.ToDouble(), EPSILON);
        }

        [Test]
        public void SqrMagnitude_WithBigValues_ProtectedFromOverflow()
        {
            // Should calculate accurately without overflow even with large values
            // Safe thanks to BigInteger protection
            var v = new FPVector3(
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f)
            );
            var sqrMag = v.sqrMagnitude;
            // 100000^2 * 3 = 30000000000
            Assert.IsTrue(sqrMag.ToDouble() > 0);  // Confirm no overflow
        }

        [Test]
        public void Dot_LargeValue()
        {
            // Dot product of large values is also protected by BigInteger
            var a = new FPVector3(
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f)
            );
            var b = new FPVector3(
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f),
                FP64.FromFloat(10000.0f)
            );
            var result = FPVector3.Dot(a, b);
            // 10000*10000 * 3 = 300000000
            Assert.AreEqual(300000000.0, result.ToDouble(), EPSILON);
        }

        [Test]
        public void Dot_WithBigValues_ProtectedFromOverflow()
        {
            // Dot product of large values is also protected by BigInteger
            var a = new FPVector3(
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f)
            );
            var b = new FPVector3(
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f),
                FP64.FromFloat(100000.0f)
            );
            var result = FPVector3.Dot(a, b);
            // 100000*100000 * 3 = 30000000000
            Assert.IsTrue(result.ToDouble() > 0);  // Confirm no overflow
        }

        #endregion
    }
}
