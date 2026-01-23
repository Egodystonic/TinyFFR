// Created on 2023-10-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

[TestFixture]
class MathUtilsTest {
	const float TestTolerance = 0.001f;

	[Test]
	public void ShouldCorrectlyCalculateTrueModulus() {
		Assert.AreEqual(100L % 30L, TrueModulus(100L, 30L));
		Assert.AreEqual(20L, TrueModulus(-100L, 30L));
		Assert.AreEqual(-20L, TrueModulus(100L, -30L));
		Assert.AreEqual(-(100L % 30L), TrueModulus(-100L, -30L));

		Assert.AreEqual(10f % 15f, TrueModulus(10f, 15f));
		Assert.AreEqual(5f, TrueModulus(-10f, 15f));
		Assert.AreEqual(-5f, TrueModulus(10f, -15f));
		Assert.AreEqual(-(10f % 15f), TrueModulus(-10f, -15f));

		Assert.AreEqual(0, TrueModulus(40, 20));
		Assert.AreEqual(0, TrueModulus(-40, 20));
		Assert.AreEqual(0, TrueModulus(40, -20));
		Assert.AreEqual(0, TrueModulus(-40, -20));
	}

	[Test]
	public void ShouldCorrectlyNormalizeOrZeroAnyVector() {
		Assert.AreEqual(Vector4.Normalize(new Vector4(1f, 2f, 3f, 4f)), NormalizeOrZero(new Vector4(1f, 2f, 3f, 4f)));
		Assert.AreEqual(Vector4.Zero, NormalizeOrZero(new Vector4(0f, 0f, 0f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyNormalizeOrIdentityAnyQuaternion() {
		Assert.AreEqual(Quaternion.Normalize(new Quaternion(1f, 2f, 3f, 4f)), NormalizeOrIdentity(new Quaternion(1f, 2f, 3f, 4f)));
		Assert.AreEqual(Quaternion.Identity, NormalizeOrIdentity(new Quaternion(0f, 0f, 0f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineFloatPositivityAndFiniteness() {
		Assert.AreEqual(true, 1f.IsPositiveAndFinite());
		Assert.AreEqual(false, 0f.IsPositiveAndFinite());
		Assert.AreEqual(false, (-1f).IsPositiveAndFinite());
		Assert.AreEqual(false, Single.PositiveInfinity.IsPositiveAndFinite());
		Assert.AreEqual(false, Single.NegativeInfinity.IsPositiveAndFinite());
		Assert.AreEqual(false, Single.NegativeZero.IsPositiveAndFinite());
		Assert.AreEqual(false, Single.NaN.IsPositiveAndFinite());
	}

	[Test]
	public void ShouldCorrectlyDetermineFloatNonNegativityAndFiniteness() {
		Assert.AreEqual(true, 1f.IsNonNegativeAndFinite());
		Assert.AreEqual(true, 0f.IsNonNegativeAndFinite());
		Assert.AreEqual(false, (-1f).IsNonNegativeAndFinite());
		Assert.AreEqual(false, Single.PositiveInfinity.IsNonNegativeAndFinite());
		Assert.AreEqual(false, Single.NegativeInfinity.IsNonNegativeAndFinite());
		Assert.AreEqual(true, Single.NegativeZero.IsNonNegativeAndFinite());
		Assert.AreEqual(false, Single.NaN.IsNonNegativeAndFinite());
	}

	[Test]
	public void ShouldCorrectlyImplementSafeAbs() {
		Assert.AreEqual(0, SafeAbs(0));
		Assert.AreEqual(1, SafeAbs(-1));
		Assert.AreEqual(1, SafeAbs(1));
		Assert.AreEqual(Int32.MaxValue, SafeAbs(Int32.MaxValue));
		Assert.AreEqual(Int32.MaxValue, SafeAbs(Int32.MinValue));

		Assert.AreEqual(0L, SafeAbs(0L));
		Assert.AreEqual(1L, SafeAbs(-1L));
		Assert.AreEqual(1L, SafeAbs(1L));
		Assert.AreEqual(Int64.MaxValue, SafeAbs(Int64.MaxValue));
		Assert.AreEqual(Int64.MaxValue, SafeAbs(Int64.MinValue));
	}

	[Test]
	public void ShouldCorrectlyRemapRanges() {
		AssertToleranceEquals((Real) 50f, ((Real) 0.5f).RemapRange(new(0f, 1f), new(0f, 100f)), TestTolerance);
		AssertToleranceEquals((Real) 0.5f, ((Real) 50f).RemapRange(new(0f, 100f), new(0f, 1f)), TestTolerance);
		AssertToleranceEquals(-(Real) 50f, (-(Real) 0.5f).RemapRange(new(0f, 1f), new(0f, 100f)), TestTolerance);
		AssertToleranceEquals(-(Real) 0.5f, (-(Real) 50f).RemapRange(new(0f, 100f), new(0f, 1f)), TestTolerance);
		AssertToleranceEquals((Real) 50f, ((Real) 0.5f).RemapRange(new(-0f, -1f), new(-0f, -100f)), TestTolerance);
		AssertToleranceEquals((Real) 0.5f, ((Real) 50f).RemapRange(new(-0f, -100f), new(-0f, -1f)), TestTolerance);
		AssertToleranceEquals(-(Real) 50f, (-(Real) 0.5f).RemapRange(new(-0f, -1f), new(-0f, -100f)), TestTolerance);
		AssertToleranceEquals(-(Real) 0.5f, (-(Real) 50f).RemapRange(new(-0f, -100f), new(-0f, -1f)), TestTolerance);
		
		AssertToleranceEquals((Real) 200f, ((Real) 2f).RemapRange(new(0f, 1f), new(0f, 100f)), TestTolerance);
		AssertToleranceEquals((Real) 2f, ((Real) 200f).RemapRange(new(0f, 100f), new(0f, 1f)), TestTolerance);
		AssertToleranceEquals((Real) 0f, ((Real) 0f).RemapRange(new(0f, 100f), new(0f, 1f)), TestTolerance);
		
		AssertToleranceEquals((Real) 6f, ((Real) 2f).RemapRange(new(-2f, 2f), new(2f, 6f)), TestTolerance);
		AssertToleranceEquals((Real) 2f, (-(Real) 2f).RemapRange(new(-2f, 2f), new(2f, 6f)), TestTolerance);
	}
}