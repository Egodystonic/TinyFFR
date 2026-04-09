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
	public void ShouldCorrectlyDecomposeMatrixToTransform() {
		AssertToleranceEquals(Transform.None, GetBestGuessTransformFromMatrix(Matrix4x4.Identity), TestTolerance);

		var translationVect = new Vect(3f, -5f, 7f);
		AssertToleranceEquals(
			new Transform(translation: translationVect), 
			GetBestGuessTransformFromMatrix(Matrix4x4.CreateTranslation(translationVect.ToVector3())), 
			TestTolerance
		);
		AssertToleranceEquals(
			translationVect, 
			GetTranslationFromMatrix(Matrix4x4.CreateTranslation(translationVect.ToVector3())), 
			TestTolerance
		);
		
		var rotationQuat = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.3f));
		AssertToleranceEquals(
			new Transform(translation: Vect.Zero, rotationQuaternion: rotationQuat, scaling: Vect.One), 
			GetBestGuessTransformFromMatrix(Matrix4x4.CreateFromQuaternion(rotationQuat)), 
			TestTolerance
		);
		AssertToleranceEquals(
			rotationQuat, 
			GetBestGuessRotationFromMatrix(Matrix4x4.CreateFromQuaternion(rotationQuat)), 
			TestTolerance
		);
		
		var scalingVect = new Vect(2f, 3f, 0.5f);
		AssertToleranceEquals(
			new Transform(scaling: scalingVect), 
			GetBestGuessTransformFromMatrix(Matrix4x4.CreateScale(scalingVect.ToVector3())), 
			TestTolerance
		);
		AssertToleranceEquals(
			scalingVect, 
			GetBestGuessScalingFromMatrix(Matrix4x4.CreateScale(scalingVect.ToVector3())), 
			TestTolerance
		);

		var combinedMat = Matrix4x4.CreateScale(scalingVect.ToVector3()) * Matrix4x4.CreateFromQuaternion(rotationQuat) * Matrix4x4.CreateTranslation(translationVect.ToVector3());
		AssertToleranceEquals(
			new Transform(scaling: scalingVect, rotationQuaternion: rotationQuat, translation: translationVect), 
			GetBestGuessTransformFromMatrix(combinedMat), 
			TestTolerance
		);
		AssertToleranceEquals(
			translationVect, 
			GetTranslationFromMatrix(combinedMat), 
			TestTolerance
		);
		AssertToleranceEquals(
			rotationQuat, 
			GetBestGuessRotationFromMatrix(combinedMat), 
			TestTolerance
		);
		AssertToleranceEquals(
			scalingVect, 
			GetBestGuessScalingFromMatrix(combinedMat), 
			TestTolerance
		);

		var negScaleMat = Matrix4x4.CreateScale(-1f, 2f, 3f);
		var negScaleResult = GetBestGuessTransformFromMatrix(negScaleMat);
		AssertToleranceEquals(Vect.Zero, negScaleResult.Translation, TestTolerance);
		AssertToleranceEquals(6f, MathF.Abs(negScaleResult.Scaling.X * negScaleResult.Scaling.Y * negScaleResult.Scaling.Z), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyForceInvertMatrix() {
		AssertToleranceEquals(Matrix4x4.Identity, ForceInvertMatrix(Matrix4x4.Identity), TestTolerance);

		var translationMat = Matrix4x4.CreateTranslation(3f, -5f, 7f);
		Matrix4x4.Invert(translationMat, out var expectedTranslationInverse);
		AssertToleranceEquals(expectedTranslationInverse, ForceInvertMatrix(translationMat), TestTolerance);

		var rotationQuat = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.3f));
		var rotationMat = Matrix4x4.CreateFromQuaternion(rotationQuat);
		Matrix4x4.Invert(rotationMat, out var expectedRotationInverse);
		AssertToleranceEquals(expectedRotationInverse, ForceInvertMatrix(rotationMat), TestTolerance);

		var scalingMat = Matrix4x4.CreateScale(2f, 3f, 0.5f);
		Matrix4x4.Invert(scalingMat, out var expectedScalingInverse);
		AssertToleranceEquals(expectedScalingInverse, ForceInvertMatrix(scalingMat), TestTolerance);

		var combinedMat = Matrix4x4.CreateScale(2f, 3f, 0.5f) * Matrix4x4.CreateFromQuaternion(rotationQuat) * Matrix4x4.CreateTranslation(3f, -5f, 7f);
		Matrix4x4.Invert(combinedMat, out var expectedCombinedInverse);
		AssertToleranceEquals(expectedCombinedInverse, ForceInvertMatrix(combinedMat), TestTolerance);

		var zeroScaleMat = Matrix4x4.CreateScale(0f, 2f, 2f);
		Assert.DoesNotThrow(() => Console.WriteLine(ForceInvertMatrix(zeroScaleMat).ToStringDescriptive()));

		var allZeroScaleMat = Matrix4x4.CreateScale(0f, 0f, 0f);
		Assert.DoesNotThrow(() => Console.WriteLine(ForceInvertMatrix(allZeroScaleMat).ToStringDescriptive()));

		for (var i = 0; i < 10000; ++i) {
			var t = new Transform(Vect.Random(), Rotation.Random(), Vect.Random(new(0.5f, 0.5f, 0.5f), new(2f, 2f, 2f)));
			var mat = t.ToMatrix();
			Matrix4x4.Invert(mat, out var expectedInverse);
			AssertToleranceEquals(expectedInverse, ForceInvertMatrix(mat), TestTolerance);
		}
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