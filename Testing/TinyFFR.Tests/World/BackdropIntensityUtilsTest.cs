// Created on 2026-10-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.IO.Compression;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class BackdropIntensityUtilsTest {
	const float TestTolerance = 0.001f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static byte[] ReadMetroIbl() => File.ReadAllBytes(CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx));
	static byte[] ReadBuiltInIbl(string name) => EmbeddedResourceResolver.GetResource("Assets.builtin_backdrop_" + name + "_ibl.zip").AsSpan.ToArray();

	static float Luminance(float r, float g, float b) => r * 0.2126f + g * 0.7152f + b * 0.0722f;

	static byte[] StripKeyValueData(byte[] ktx) {
		var keyValueLength = (int) BinaryPrimitives.ReadUInt32LittleEndian(ktx.AsSpan(60));
		var result = new byte[ktx.Length - keyValueLength];
		ktx.AsSpan(0, 64).CopyTo(result);
		BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(60), 0U);
		ktx.AsSpan(64 + keyValueLength).CopyTo(result.AsSpan(64));
		return result;
	}

	[Test]
	public void ShouldReadSphericalHarmonicsFromShippedAssets() {
		var expectations = new (byte[] Data, float UpwardRadiance)[] {
			(ReadMetroIbl(), Luminance(0.858912f + 0.672763f + 0.0334103f + 0.0997694f, 0.609787f + 0.399878f + 0.00773796f + 0.0253849f, 0.427791f + 0.164341f - 0.0187937f - 0.0489236f)),
			(ReadBuiltInIbl("clouds"), Luminance(0.645668f + 0.627358f + 0.00640369f + 0.11849f, 0.697179f + 0.649411f + 0.00424346f + 0.118113f, 0.815733f + 0.665912f - 0.000190963f + 0.108034f)),
			(ReadBuiltInIbl("starfield"), Luminance(0.0285351f - 0.00503726f + 0.000179406f + 0.0000162435f, 0.0325565f - 0.00289796f - 0.000104211f - 0.0000333572f, 0.0388633f + 0.000336481f - 0.00000628891f - 0.000114557f))
		};

		foreach (var (data, expectedUpwardRadiance) in expectations) {
			Assert.IsTrue(BackdropIntensityUtils.TryReadKtxHeader(data, out var header));
			Assert.IsTrue(BackdropIntensityUtils.TryReadSphericalHarmonicsUpwardRadiance(data, header, out var upwardRadiance));
			Assert.AreEqual(expectedUpwardRadiance, upwardRadiance, 0.0001f);
			Assert.AreEqual(BackdropIntensityUtils.NormalizedUpwardRadianceToLux(expectedUpwardRadiance), BackdropIntensityUtils.MeasureLuxAtUnitIntensity(data), 0.1f);
		}
	}

	[Test]
	public void PixelIntegrationShouldAgreeWithSphericalHarmonics() {
		foreach (var data in new[] { ReadMetroIbl(), ReadBuiltInIbl("clouds"), ReadBuiltInIbl("starfield") }) {
			Assert.IsTrue(BackdropIntensityUtils.TryReadKtxHeader(data, out var header));
			Assert.IsTrue(BackdropIntensityUtils.TryReadSphericalHarmonicsUpwardRadiance(data, header, out var fromSphericalHarmonics));
			Assert.IsTrue(BackdropIntensityUtils.TryIntegrateUpwardRadiance(data, header, out var fromPixels));
			Assert.AreEqual(fromSphericalHarmonics, fromPixels, fromSphericalHarmonics * 0.1f);

			var stripped = StripKeyValueData(data);
			Assert.IsTrue(BackdropIntensityUtils.TryReadKtxHeader(stripped, out var strippedHeader));
			Assert.IsFalse(BackdropIntensityUtils.TryReadSphericalHarmonicsUpwardRadiance(stripped, strippedHeader, out _));
			Assert.AreEqual(BackdropIntensityUtils.NormalizedUpwardRadianceToLux(fromPixels), BackdropIntensityUtils.MeasureLuxAtUnitIntensity(stripped), 0.1f);
		}
	}

	[Test]
	public void ShouldFallBackToTypicalBackdropForUnmeasurableData() {
		Assert.AreEqual(BackdropIntensityUtils.TypicalLuxAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ReadOnlySpan<byte>.Empty));
		Assert.AreEqual(BackdropIntensityUtils.TypicalLuxAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(new byte[1000]));
		Assert.AreEqual(BackdropIntensityUtils.TypicalLuxAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity("not a ktx file at all, just some text"u8));

		var data = StripKeyValueData(ReadMetroIbl());
		BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28), 0x8E8CU);
		Assert.AreEqual(BackdropIntensityUtils.TypicalLuxAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(data));

		var truncated = StripKeyValueData(ReadMetroIbl())[..200];
		Assert.AreEqual(BackdropIntensityUtils.TypicalLuxAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(truncated));

		var corruptHarmonics = ReadMetroIbl();
		var shValueIndex = corruptHarmonics.AsSpan(64).IndexOf("sh\0"u8) + 64 + 3;
		corruptHarmonics[shValueIndex] = (byte) 'x';
		Assert.IsTrue(BackdropIntensityUtils.TryReadKtxHeader(corruptHarmonics, out var corruptHeader));
		Assert.IsFalse(BackdropIntensityUtils.TryReadSphericalHarmonicsUpwardRadiance(corruptHarmonics, corruptHeader, out _));
		Assert.IsTrue(BackdropIntensityUtils.TryIntegrateUpwardRadiance(corruptHarmonics, corruptHeader, out var fromPixels));
		Assert.AreEqual(BackdropIntensityUtils.NormalizedUpwardRadianceToLux(fromPixels), BackdropIntensityUtils.MeasureLuxAtUnitIntensity(corruptHarmonics), 0.1f);
	}

	[Test]
	public void ShouldCorrectlyDecodePackedSmallFloats() {
		Assert.AreEqual(0f, BackdropIntensityUtils.DecodeUnsignedSmallFloat(0U, 6));
		Assert.AreEqual(1f, BackdropIntensityUtils.DecodeUnsignedSmallFloat(15U << 6, 6));
		Assert.AreEqual(1.5f, BackdropIntensityUtils.DecodeUnsignedSmallFloat((15U << 6) | 32U, 6));
		Assert.AreEqual(2f, BackdropIntensityUtils.DecodeUnsignedSmallFloat(16U << 5, 5));
		Assert.AreEqual(0.25f, BackdropIntensityUtils.DecodeUnsignedSmallFloat(13U << 5, 5));
		Assert.AreEqual(MathF.ScaleB(0.5f, -14), BackdropIntensityUtils.DecodeUnsignedSmallFloat(32U, 6));
		Assert.AreEqual(0f, BackdropIntensityUtils.DecodeUnsignedSmallFloat(31U << 6, 6));

		Span<byte> texel = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(texel, (15U << 6) | ((15U << 6) << 11) | ((15U << 5) << 22));
		Assert.AreEqual(1f, BackdropIntensityUtils.DecodeTexelLuminance(texel, BackdropIntensityUtils.KtxTexelFormat.R11G11B10Float), TestTolerance);

		Span<byte> halfTexel = stackalloc byte[8];
		BinaryPrimitives.WriteHalfLittleEndian(halfTexel, (Half) 2f);
		BinaryPrimitives.WriteHalfLittleEndian(halfTexel[2..], (Half) 2f);
		BinaryPrimitives.WriteHalfLittleEndian(halfTexel[4..], (Half) 2f);
		Assert.AreEqual(2f, BackdropIntensityUtils.DecodeTexelLuminance(halfTexel, BackdropIntensityUtils.KtxTexelFormat.Rgba16Float), TestTolerance);

		Assert.AreEqual(1f, BackdropIntensityUtils.DecodeTexelLuminance([255, 255, 255, 255], BackdropIntensityUtils.KtxTexelFormat.Srgba8), TestTolerance);
		Assert.AreEqual(ColorVect.SrgbToLinear(0.5f), BackdropIntensityUtils.DecodeTexelLuminance([128, 128, 128], BackdropIntensityUtils.KtxTexelFormat.Srgb8), 0.01f);
		Assert.AreEqual(128f / 255f, BackdropIntensityUtils.DecodeTexelLuminance([128, 128, 128], BackdropIntensityUtils.KtxTexelFormat.Rgb8Unorm), TestTolerance);
	}

	[Test]
	public void ShouldMeasureAndConvertColorBackdrops() {
		Assert.AreEqual(MathF.PI * BackdropIntensityUtils.NativeIntensityAtUnitIntensity, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ColorVect.WhiteOpaque), TestTolerance);
		Assert.AreEqual(MathF.PI * BackdropIntensityUtils.NativeIntensityAtUnitIntensity * 0.5f, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(new ColorVect(0.5f, 0.5f, 0.5f)), TestTolerance);
		Assert.AreEqual(0f, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ColorVect.BlackOpaque));

		Assert.AreEqual(1f, BackdropIntensityUtils.LuxToIntensity(10_000f, BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ColorVect.BlackOpaque)));
		Assert.AreEqual(1f, BackdropIntensityUtils.LuxToIntensity(10_000f, Single.NaN));
	}

	[Test]
	public void ConversionsShouldBeQuadraticAndHandleInvalidInputs() {
		const float LuxAtUnitIntensity = 800f;
		Assert.AreEqual(1f, BackdropIntensityUtils.LuxToIntensity(800f, LuxAtUnitIntensity), TestTolerance);
		Assert.AreEqual(2f, BackdropIntensityUtils.LuxToIntensity(3_200f, LuxAtUnitIntensity), TestTolerance);
		Assert.AreEqual(0.25f, BackdropIntensityUtils.LuxToIntensity(50f, LuxAtUnitIntensity), TestTolerance);
		Assert.AreEqual(3_200f, BackdropIntensityUtils.IntensityToLux(2f, LuxAtUnitIntensity), TestTolerance);
		Assert.AreEqual(3f, BackdropIntensityUtils.LuxToIntensity(BackdropIntensityUtils.IntensityToLux(3f, LuxAtUnitIntensity), LuxAtUnitIntensity), TestTolerance);

		foreach (var invalid in new[] { -1f, Single.NegativeZero, Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity }) {
			Assert.AreEqual(0f, BackdropIntensityUtils.LuxToIntensity(invalid, LuxAtUnitIntensity));
			Assert.AreEqual(0f, BackdropIntensityUtils.IntensityToLux(invalid, LuxAtUnitIntensity));
			Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(invalid));
		}
		Assert.AreEqual(Scene.MaxBrightness, BackdropIntensityUtils.SanitizeIntensity(Scene.MaxBrightness * 10f));
		Assert.AreEqual(Scene.MaxBrightness, BackdropIntensityUtils.LuxToIntensity(Single.MaxValue, 1f));
	}

	[Test]
	public void LuxConversionsShouldMatchNativeIntensityMapping() {
		const float LuxAtUnitIntensity = 1_000f;
		foreach (var intensity in new[] { 0.1f, 0.5f, 1f, 2f, 7.5f, 140f }) {
			var nativeIlluminanceFactor = BackdropIntensityUtils.ToNativeIntensity(intensity) / BackdropIntensityUtils.NativeIntensityAtUnitIntensity;
			Assert.AreEqual(nativeIlluminanceFactor, BackdropIntensityUtils.IntensityToLux(intensity, LuxAtUnitIntensity) / LuxAtUnitIntensity, nativeIlluminanceFactor * TestTolerance);
		}
		foreach (var lux in new[] { 10f, 600f, 10_000f, 100_000f }) {
			var intensity = BackdropIntensityUtils.LuxToIntensity(lux, LuxAtUnitIntensity);
			Assert.AreEqual(lux / LuxAtUnitIntensity, BackdropIntensityUtils.ToNativeIntensity(intensity) / BackdropIntensityUtils.NativeIntensityAtUnitIntensity, lux / LuxAtUnitIntensity * TestTolerance);
		}
	}

	[Test]
	public void LoadedBackdropsShouldExposeMeasuredLux() {
		using var factory = new LocalTinyFfrFactory();
		using var metro = factory.AssetLoader.LoadPreprocessedBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx), CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx));
		Assert.AreEqual(BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ReadMetroIbl()), metro.MeasuredLux, TestTolerance);
		Assert.AreEqual(metro.MeasuredLux * 4f, metro.IntensityToLux(2f), TestTolerance);
		Assert.AreEqual(0.5f, metro.LuxToIntensity(metro.MeasuredLux * 0.25f), TestTolerance);

		using var scene = factory.SceneBuilder.CreateScene();
		Assert.AreEqual(BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ReadBuiltInIbl("clouds")), scene.Implementation.GetBuiltInBackdropMeasuredLux(BuiltInSceneBackdrop.Clouds), TestTolerance);
		Assert.AreEqual(BackdropIntensityUtils.MeasureLuxAtUnitIntensity(ReadBuiltInIbl("starfield")), scene.Implementation.GetBuiltInBackdropMeasuredLux(BuiltInSceneBackdrop.Starfield), TestTolerance);

		foreach (var preset in Enum.GetValues<SceneBackdropBrightnessPreset>()) {
			Assert.DoesNotThrow(() => scene.SetBackdrop(metro, preset));
			Assert.DoesNotThrow(() => scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, preset));
			Assert.DoesNotThrow(() => scene.SetBackdrop(BuiltInSceneBackdrop.None, preset));
			Assert.DoesNotThrow(() => scene.SetBackdrop(StandardColor.White, preset));
			Assert.DoesNotThrow(() => scene.SetBackdrop(ColorVect.BlackOpaque, preset));
			Assert.DoesNotThrow(() => scene.SetBackdropWithoutIndirectLighting(metro, preset));
		}
		scene.RemoveBackdrop();
	}
}
