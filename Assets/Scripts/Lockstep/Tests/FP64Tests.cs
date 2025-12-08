using UnityEngine;
using NUnit.Framework;
using Lockstep.Math.Impl;
using System;

namespace Lockstep.Tests
{
    /// <summary>
    /// FP64 fixed point arithmetic tests
    /// </summary>
    [TestFixture]
    public class FP64Tests
    {
        private const float EPSILON = 0.001f;

        #region Creation and Conversion

        [Test]
        public void FromInt_CreatesCorrectValue()
        {
            var fp = FP64.FromInt(5);
            Assert.AreEqual(5, fp.ToInt());
            Assert.AreEqual(5.0f, fp.ToFloat(), EPSILON);
        }

        [Test]
        public void FromFloat_CreatesCorrectValue()
        {
            var fp = FP64.FromFloat(3.14f);
            Assert.AreEqual(3.14f, fp.ToFloat(), EPSILON);
        }

        [Test]
        public void FromDouble_CreatesCorrectValue()
        {
            var fp = FP64.FromDouble(2.71828);
            Assert.AreEqual(2.71828, fp.ToDouble(), EPSILON);
        }

        [Test]
        public void FromRaw_PreservesValue()
        {
            long rawValue = 12345678L;
            var fp = FP64.FromRaw(rawValue);
            Assert.AreEqual(rawValue, fp.RawValue);
        }

        [Test]
        public void NegativeValues_WorkCorrectly()
        {
            var fp = FP64.FromFloat(-5.5f);
            Assert.AreEqual(-5.5f, fp.ToFloat(), EPSILON);
        }

        #endregion

        #region Basic Operations

        [Test]
        public void Addition_WorksCorrectly()
        {
            var a = FP64.FromFloat(3.5f);
            var b = FP64.FromFloat(2.5f);
            var result = a + b;
            Assert.AreEqual(6.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Subtraction_WorksCorrectly()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.FromFloat(3.0f);
            var result = a - b;
            Assert.AreEqual(2.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Multiplication_WorksCorrectly()
        {
            var a = FP64.FromFloat(3.0f);
            var b = FP64.FromFloat(4.0f);
            var result = a * b;
            Assert.AreEqual(12.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Multiplication_WithFractions_WorksCorrectly()
        {
            var a = FP64.FromFloat(2.5f);
            var b = FP64.FromFloat(4.0f);
            var result = a * b;
            Assert.AreEqual(10.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Division_WorksCorrectly()
        {
            var a = FP64.FromFloat(10.0f);
            var b = FP64.FromFloat(2.0f);
            var result = a / b;
            Assert.AreEqual(5.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Division_WithFractions_WorksCorrectly()
        {
            var a = FP64.FromFloat(7.0f);
            var b = FP64.FromFloat(2.0f);
            var result = a / b;
            Assert.AreEqual(3.5f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Division_ByZero_ThrowsException()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.Zero;
            Assert.Throws<DivideByZeroException>(() => { var result = a / b; });
        }

        [Test]
        public void Division_LargeValues_NoOverflow()
        {
            // Large value division (overflow prevention test)
            var a = FP64.FromFloat(1000.0f);
            var b = FP64.FromFloat(10.0f);
            var result = a / b;
            Assert.AreEqual(100.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Modulo_WorksCorrectly()
        {
            var a = FP64.FromFloat(7.0f);
            var b = FP64.FromFloat(3.0f);
            var result = a % b;
            Assert.AreEqual(1.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Modulo_ByZero_ReturnsZero()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.Zero;
            var result = a % b;
            Assert.AreEqual(FP64.Zero, result);
        }

        [Test]
        public void Negation_WorksCorrectly()
        {
            var a = FP64.FromFloat(5.0f);
            var result = -a;
            Assert.AreEqual(-5.0f, result.ToFloat(), EPSILON);
        }

        #endregion

        #region Comparison Operations

        [Test]
        public void Equality_WorksCorrectly()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.FromFloat(5.0f);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Inequality_WorksCorrectly()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.FromFloat(3.0f);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void LessThan_WorksCorrectly()
        {
            var a = FP64.FromFloat(3.0f);
            var b = FP64.FromFloat(5.0f);
            Assert.IsTrue(a < b);
            Assert.IsFalse(b < a);
        }

        [Test]
        public void GreaterThan_WorksCorrectly()
        {
            var a = FP64.FromFloat(5.0f);
            var b = FP64.FromFloat(3.0f);
            Assert.IsTrue(a > b);
            Assert.IsFalse(b > a);
        }

        #endregion

        #region Math Functions

        [Test]
        public void Abs_PositiveValue_ReturnsSame()
        {
            var a = FP64.FromFloat(5.0f);
            Assert.AreEqual(5.0f, FP64.Abs(a).ToFloat(), EPSILON);
        }

        [Test]
        public void Abs_NegativeValue_ReturnsPositive()
        {
            var a = FP64.FromFloat(-5.0f);
            Assert.AreEqual(5.0f, FP64.Abs(a).ToFloat(), EPSILON);
        }

        [Test]
        public void Sqrt_PerfectSquare_WorksCorrectly()
        {
            var a = FP64.FromFloat(16.0f);
            var result = FP64.Sqrt(a);
            Assert.AreEqual(4.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Sqrt_NonPerfectSquare_WorksCorrectly()
        {
            var a = FP64.FromFloat(2.0f);
            var result = FP64.Sqrt(a);
            Assert.AreEqual(1.414f, result.ToFloat(), 0.01f);
        }

        [Test]
        public void Sqrt_Zero_ReturnsZero()
        {
            var result = FP64.Sqrt(FP64.Zero);
            Assert.AreEqual(FP64.Zero, result);
        }

        [Test]
        public void Sqrt_SmallValue_WorksCorrectly()
        {
            var a = FP64.FromFloat(0.25f);
            var result = FP64.Sqrt(a);
            Assert.AreEqual(0.5f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Sqrt_LargeValue_NoOverflow()
        {
            var a = FP64.FromFloat(10000.0f);
            var result = FP64.Sqrt(a);
            Assert.AreEqual(100.0f, result.ToFloat(), 0.1f);
        }

        [Test]
        public void Sqrt_NegativeValue_ThrowsException()
        {
            var a = FP64.FromFloat(-1.0f);
            Assert.Throws<ArgumentException>(() => FP64.Sqrt(a));
        }

        [Test]
        public void Min_ReturnsSmaller()
        {
            var a = FP64.FromFloat(3.0f);
            var b = FP64.FromFloat(5.0f);
            Assert.AreEqual(a, FP64.Min(a, b));
        }

        [Test]
        public void Max_ReturnsLarger()
        {
            var a = FP64.FromFloat(3.0f);
            var b = FP64.FromFloat(5.0f);
            Assert.AreEqual(b, FP64.Max(a, b));
        }

        [Test]
        public void Clamp_InRange_ReturnsSame()
        {
            var value = FP64.FromFloat(5.0f);
            var result = FP64.Clamp(value, FP64.FromFloat(0.0f), FP64.FromFloat(10.0f));
            Assert.AreEqual(5.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Clamp_BelowMin_ReturnsMin()
        {
            var value = FP64.FromFloat(-5.0f);
            var result = FP64.Clamp(value, FP64.FromFloat(0.0f), FP64.FromFloat(10.0f));
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Clamp_AboveMax_ReturnsMax()
        {
            var value = FP64.FromFloat(15.0f);
            var result = FP64.Clamp(value, FP64.FromFloat(0.0f), FP64.FromFloat(10.0f));
            Assert.AreEqual(10.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Lerp_AtZero_ReturnsA()
        {
            var a = FP64.FromFloat(0.0f);
            var b = FP64.FromFloat(10.0f);
            var result = FP64.Lerp(a, b, FP64.Zero);
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Lerp_AtOne_ReturnsB()
        {
            var a = FP64.FromFloat(0.0f);
            var b = FP64.FromFloat(10.0f);
            var result = FP64.Lerp(a, b, FP64.One);
            Assert.AreEqual(10.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Lerp_AtHalf_ReturnsMidpoint()
        {
            var a = FP64.FromFloat(0.0f);
            var b = FP64.FromFloat(10.0f);
            var result = FP64.Lerp(a, b, FP64.Half);
            Assert.AreEqual(5.0f, result.ToFloat(), EPSILON);
        }

        #endregion

        #region Trigonometric Functions

        [Test]
        public void Sin_Zero_ReturnsZero()
        {
            var result = FP64.Sin(FP64.Zero);
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Sin_HalfPi_ReturnsOne()
        {
            var result = FP64.Sin(FP64.HalfPi);
            Assert.AreEqual(1.0f, result.ToFloat(), 0.01f);
        }

        [Test]
        public void Cos_Zero_ReturnsOne()
        {
            var result = FP64.Cos(FP64.Zero);
            Console.WriteLine($"Cos(0) = {result.ToFloat()}");
            Assert.AreEqual(1.0f, result.ToFloat(), EPSILON);
        }

        [Test]
        public void Cos_Pi_ReturnsMinusOne()
        {
            var result = FP64.Cos(FP64.Pi);
            Console.WriteLine($"Cos(Pi) = {result.ToFloat()}");
            Assert.AreEqual(-1.0f, result.ToFloat(), 0.01f);
        }

        [Test]
        public void Atan2_QuadrantI_WorksCorrectly()
        {
            var pts = new (double y, double x)[] { (1,1), (1,-1), (-1,1), (-1,-1), (10,-1), (-10,-1) };
            foreach (var p in pts) {
                var y = FP64.FromDouble(p.y);
                var x = FP64.FromDouble(p.x);
                var my = FP64.Atan2(y, x).ToDouble();
                var sys = Mathf.Atan2((float)p.y, (float)p.x);
                Assert.AreEqual(my, sys, 0.05f);

                Console.WriteLine($"{p}: FP64={my:F6}, System={sys:F6}");
            }
        }

        [Test]
        public void Atan2_Zero_ReturnsZero()
        {
            var result = FP64.Atan2(FP64.Zero, FP64.One);
            Assert.AreEqual(0.0f, result.ToFloat(), EPSILON);
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void SameOperations_ProduceSameResults()
        {
            // Same operation should always produce the same result (determinism)
            for (int i = 0; i < 100; i++)
            {
                var a = FP64.FromFloat(3.14159f);
                var b = FP64.FromFloat(2.71828f);

                var result1 = (a * b) / (a + b);
                var result2 = (a * b) / (a + b);

                Assert.AreEqual(result1.RawValue, result2.RawValue,
                    "Same operation should always produce the same result");
            }
        }

        [Test]
        public void ChainedOperations_AreDeterministic()
        {
            var a = FP64.FromFloat(10.0f);
            var b = FP64.FromFloat(3.0f);

            // Complex operation chain
            var result1 = FP64.Sqrt(a * a + b * b);
            var result2 = FP64.Sqrt(a * a + b * b);

            Assert.AreEqual(result1.RawValue, result2.RawValue);
        }

        #endregion
    }
}
