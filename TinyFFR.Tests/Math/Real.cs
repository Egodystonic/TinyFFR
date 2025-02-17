// Created on 2025-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class RealTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvert() {
		Assert.AreEqual(123f, (float) (Real) 123f);
	}

	[Test]
	public void ShouldCorrectlyDelegateImplementedMethodsToSingle() {
		void AssertReturn<T>(Func<dynamic, T> func) {
			var realRes = func((Real) (-123.456f));
			if (realRes is Real real) Assert.AreEqual((float) (object) func(-123.456f)!, real.AsFloat);
			else Assert.AreEqual(func(-123.456f), realRes);
		}

		AssertReturn(v => v.ToString("N3", new CultureInfo("de-DE")));
		AssertReturn(v => -v);
		AssertReturn(v => v + v);
		AssertReturn(v => v - v);
		AssertReturn(v => v * v);
		AssertReturn(v => v / v);
		AssertReturn(v => v.CompareTo(v + 1f));
		AssertReturn(v => v > (v + 1f));
		AssertReturn(v => v < (v + 1f));
		AssertReturn(v => v >= (v + 1f));
		AssertReturn(v => v <= (v + 1f));
		AssertReturn(v => v == (v + 1f));
		AssertReturn(v => v != (v + 1f));

		var floatDest = new char[100];
		var floatRes = (-123.456f).TryFormat(floatDest.AsSpan(), out var floatCharsWritten, "N3", new CultureInfo("de-DE"));
		var realDest = new char[100];
		var realRes = ((Real) (-123.456f)).TryFormat(realDest.AsSpan(), out var realCharsWritten, "N3", new CultureInfo("de-DE"));

		Assert.AreEqual(floatRes, realRes);
		Assert.AreEqual(floatCharsWritten, realCharsWritten);
		Assert.IsTrue(floatDest.SequenceEqual(realDest));

		Assert.AreEqual(Single.Parse("123.456", CultureInfo.InvariantCulture), (float) Real.Parse("123.456", CultureInfo.InstalledUICulture));
		Assert.AreEqual(Single.TryParse("123.456", CultureInfo.InvariantCulture, out var f), Real.TryParse("123.456", CultureInfo.InstalledUICulture, out var r));
		Assert.AreEqual(Single.Parse("123.456".AsSpan(), CultureInfo.InvariantCulture), (float) Real.Parse("123.456".AsSpan(), CultureInfo.InstalledUICulture));
		Assert.AreEqual(Single.TryParse("123.456".AsSpan(), CultureInfo.InvariantCulture, out var fS), Real.TryParse("123.456".AsSpan(), CultureInfo.InstalledUICulture, out var rS));
		Assert.AreEqual(f, r.AsFloat);
		Assert.AreEqual(fS, rS.AsFloat);

		var testReal = (Real) 123.456f;
		Assert.AreEqual(-testReal, testReal.Negated);
		Assert.AreEqual(-testReal, ((IInvertible<Real>) testReal).Inverted);
		Assert.AreEqual(testReal + 32f, testReal.Plus(32f));
		Assert.AreEqual(testReal - 32f, testReal.Minus(32f));
		Assert.AreEqual(testReal, testReal + Real.AdditiveIdentity);
		Assert.AreEqual(testReal, testReal * Real.MultiplicativeIdentity);
		Assert.AreEqual(testReal * 1.2f, testReal.MultipliedBy(1.2f));
		Assert.AreEqual(testReal / 1.2f, testReal.DividedBy(1.2f));
		Assert.AreEqual(1f / testReal, testReal.Reciprocal!.Value);
		Assert.AreEqual(null, Real.Zero.Reciprocal);
	}

	[Test]
	public void ShouldCorrectlyImplementToleranceEquality() {
		Assert.AreEqual(true, ((Real) 123f).Equals(123f, 0f));
		Assert.AreEqual(true, ((Real) 123f).Equals(122f, 1.1f));
		Assert.AreEqual(false, ((Real) 123f).Equals(122f, 0.9f));
	}

	[Test]
	public void ShouldCorrectlySerializeToFromSpan() {
		var dest = (Span<byte>) stackalloc byte[4];
		Real.SerializeToBytes(dest, 123.456f);
		Assert.AreEqual(123.456f, BinaryPrimitives.ReadSingleLittleEndian(dest));
		Assert.AreEqual(123.456f, (float) Real.DeserializeFromBytes(dest));
	}

	[Test]
	public void ShouldCorrectlyImplementRandom() {
		const int NumIterations = 100_000;

		for (var i = 0; i < NumIterations; ++i) {
			var r = Real.Random();
			Assert.GreaterOrEqual(r.AsFloat, 0f);
			Assert.Less(r.AsFloat, 1f);

			r = Real.Random(-2f, 2f);
			Assert.GreaterOrEqual(r.AsFloat, -2f);
			Assert.Less(r.AsFloat, 2f);

			r = Real.RandomInclusive(-2f, 2f);
			Assert.GreaterOrEqual(r.AsFloat, -2f);
			Assert.LessOrEqual(r.AsFloat, 2f);

			r = Real.RandomZeroToOneInclusive();
			Assert.GreaterOrEqual(r.AsFloat, 0f);
			Assert.LessOrEqual(r.AsFloat, 1f);

			r = Real.RandomNegOneToOneInclusive();
			Assert.GreaterOrEqual(r.AsFloat, -1f);
			Assert.LessOrEqual(r.AsFloat, 1f);
		}
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		Assert.AreEqual(0f, Real.Interpolate(0f, 1f, 0f).AsFloat);
		Assert.AreEqual(0.3f, Real.Interpolate(0f, 1f, 0.3f).AsFloat);
		Assert.AreEqual(1f, Real.Interpolate(0f, 1f, 1f).AsFloat);
		Assert.AreEqual(2f, Real.Interpolate(0f, 1f, 2f).AsFloat);
		Assert.AreEqual(-1f, Real.Interpolate(0f, 1f, -1f).AsFloat);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		Assert.AreEqual((Real) 0f, ((Real) 0f).Clamp(0f, 1f));
		Assert.AreEqual((Real) 0f, ((Real) (-1f)).Clamp(0f, 1f));
		Assert.AreEqual((Real) 1f, ((Real) 1f).Clamp(0f, 1f));
		Assert.AreEqual((Real) 1f, ((Real) 2f).Clamp(0f, 1f));
		Assert.AreEqual((Real) 0.5f, ((Real) 0.5f).Clamp(0f, 1f));

		Assert.AreEqual((Real) 0f, ((Real) 0f).Clamp(1f, 0f));
		Assert.AreEqual((Real) 0f, ((Real) (-1f)).Clamp(1f, 0f));
		Assert.AreEqual((Real) 1f, ((Real) 1f).Clamp(1f, 0f));
		Assert.AreEqual((Real) 1f, ((Real) 2f).Clamp(1f, 0f));
		Assert.AreEqual((Real) 0.5f, ((Real) 0.5f).Clamp(1f, 0f));
	}
}