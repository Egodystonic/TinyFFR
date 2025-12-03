// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using static Egodystonic.TinyFFR.Assets.Local.LocalBuiltInTexturePathLibrary;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalBuiltInTexturePathLibraryTest {
	LocalBuiltInTexturePathLibrary _lib;
	HashSet<string> _referencedProperties;

	[SetUp]
	public void SetUpTest() {
		_lib = new();
		_referencedProperties = new();
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		AssertAllProperties();
		
		foreach (var prop in typeof(IBuiltInTexturePathLibrary).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
			Assert.IsTrue(_referencedProperties.Contains(prop.Name), $"Missing test of property '{prop.Name}'.");
		}

		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + MapTexelPrefix));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + "fake"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + MapTexelPrefix + "fake"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "fake"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator + "a"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator + "255" + ByteValueSeparator));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "a"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "a"));
		Assert.AreEqual(null, _lib.GetBuiltInTexel(LocalBuiltInTexturePrefix + ByteValueTexelPrefix + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "255" + ByteValueSeparator + "255"));
	}

	void AssertAllProperties() {
		AssertProperty(new TexelRgb24(ITextureBuilder.DefaultColor), lib => lib.DefaultColorMap);
		AssertProperty(127, 127, 255, lib => lib.DefaultNormalMap);
		AssertProperty(ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultMetallic, lib => lib.DefaultOcclusionRoughnessMetallicMap);
		AssertProperty(ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultReflectance, lib => lib.DefaultOcclusionRoughnessMetallicReflectanceMap);
		AssertProperty(ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, lib => lib.DefaultOcclusionMap);
		AssertProperty(ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, lib => lib.DefaultRoughnessMap);
		AssertProperty(ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, lib => lib.DefaultMetallicMap);
		AssertProperty(ITextureBuilder.DefaultReflectance, ITextureBuilder.DefaultReflectance, ITextureBuilder.DefaultReflectance, lib => lib.DefaultReflectanceMap);
		AssertProperty(new TexelRgba32(new TexelRgb24(ITextureBuilder.DefaultAbsorption), (byte) (ITextureBuilder.DefaultTransmission * Byte.MaxValue)), lib => lib.DefaultAbsorptionTransmissionMap);
		AssertProperty(new TexelRgb24(ITextureBuilder.DefaultAbsorption), lib => lib.DefaultAbsorptionMap);
		AssertProperty(ITextureBuilder.DefaultTransmission, ITextureBuilder.DefaultTransmission, ITextureBuilder.DefaultTransmission, lib => lib.DefaultTransmissionMap);
		AssertProperty(new TexelRgba32(new TexelRgb24(ITextureBuilder.DefaultEmissiveColor), (byte) (ITextureBuilder.DefaultEmissiveIntensity * Byte.MaxValue)), lib => lib.DefaultEmissiveMap);
		AssertProperty(new TexelRgb24(ITextureBuilder.DefaultEmissiveColor), lib => lib.DefaultEmissiveColorMap);
		AssertProperty(ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, lib => lib.DefaultEmissiveIntensityMap);
		AssertProperty(255, 128, 255, lib => lib.DefaultAnisotropyMap);
		AssertProperty(ITextureBuilder.DefaultAnisotropyRadialAngle.Radians, ITextureBuilder.DefaultAnisotropyRadialAngle.Radians, ITextureBuilder.DefaultAnisotropyRadialAngle.Radians, lib => lib.DefaultAnisotropyRadialAngleMap);
		AssertProperty(255, 128, 0, lib => lib.DefaultAnisotropyVectorMap);
		AssertProperty(ITextureBuilder.DefaultAnisotropyStrength, ITextureBuilder.DefaultAnisotropyStrength, ITextureBuilder.DefaultAnisotropyStrength, lib => lib.DefaultAnisotropyStrengthMap);
		AssertProperty(ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatRoughness, 0f, lib => lib.DefaultClearCoatMap);
		AssertProperty(ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, lib => lib.DefaultClearCoatThicknessMap);
		AssertProperty(ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, lib => lib.DefaultClearCoatRoughnessMap);

		byte R(float f) => (byte) MathF.Round(255f * f, MidpointRounding.AwayFromZero);

		AssertProperty(R(1f), R(1f), R(1f), R(1f), lib => lib.Rgba100Percent);
		AssertProperty(R(0.9f), R(0.9f), R(0.9f), R(0.9f), lib => lib.Rgba90Percent);
		AssertProperty(R(0.8f), R(0.8f), R(0.8f), R(0.8f), lib => lib.Rgba80Percent);
		AssertProperty(R(0.7f), R(0.7f), R(0.7f), R(0.7f), lib => lib.Rgba70Percent);
		AssertProperty(R(0.6f), R(0.6f), R(0.6f), R(0.6f), lib => lib.Rgba60Percent);
		AssertProperty(R(0.5f), R(0.5f), R(0.5f), R(0.5f), lib => lib.Rgba50Percent);
		AssertProperty(R(0.4f), R(0.4f), R(0.4f), R(0.4f), lib => lib.Rgba40Percent);
		AssertProperty(R(0.3f), R(0.3f), R(0.3f), R(0.3f), lib => lib.Rgba30Percent);
		AssertProperty(R(0.2f), R(0.2f), R(0.2f), R(0.2f), lib => lib.Rgba20Percent);
		AssertProperty(R(0.1f), R(0.1f), R(0.1f), R(0.1f), lib => lib.Rgba10Percent);
		AssertProperty(R(0f), R(0f), R(0f), R(0f), lib => lib.Rgba0Percent);

		AssertProperty(255, 255, 255, lib => lib.White);
		AssertProperty(0, 0, 0, lib => lib.Black);
		AssertProperty(255, 0, 0, lib => lib.Red);
		AssertProperty(0, 255, 0, lib => lib.Green);
		AssertProperty(0, 0, 255, lib => lib.Blue);
		AssertProperty(255, 255, 0, lib => lib.RedGreen);
		AssertProperty(0, 255, 255, lib => lib.GreenBlue);
		AssertProperty(255, 0, 255, lib => lib.RedBlue);

		AssertProperty(255, 255, 255, 255, lib => lib.WhiteOpaque);
		AssertProperty(0, 0, 0, 255, lib => lib.BlackOpaque);
		AssertProperty(255, 0, 0, 255, lib => lib.RedOpaque);
		AssertProperty(0, 255, 0, 255, lib => lib.GreenOpaque);
		AssertProperty(0, 0, 255, 255, lib => lib.BlueOpaque);
		AssertProperty(255, 255, 0, 255, lib => lib.RedGreenOpaque);
		AssertProperty(0, 255, 255, 255, lib => lib.GreenBlueOpaque);
		AssertProperty(255, 0, 255, 255, lib => lib.RedBlueOpaque);

		AssertProperty(255, 255, 255, 0, lib => lib.WhiteTransparent);
		AssertProperty(0, 0, 0, 0, lib => lib.BlackTransparent);
		AssertProperty(255, 0, 0, 0, lib => lib.RedTransparent);
		AssertProperty(0, 255, 0, 0, lib => lib.GreenTransparent);
		AssertProperty(0, 0, 255, 0, lib => lib.BlueTransparent);
		AssertProperty(255, 255, 0, 0, lib => lib.RedGreenTransparent);
		AssertProperty(0, 255, 255, 0, lib => lib.GreenBlueTransparent);
		AssertProperty(255, 0, 255, 0, lib => lib.RedBlueTransparent);
	}

	void AssertProperty(byte r, byte g, byte b, Func<IBuiltInTexturePathLibrary, ReadOnlySpan<char>> pathSelector, [CallerArgumentExpression(nameof(pathSelector))] string? propNameExpression = null) {
		AssertProperty(new TexelRgb24(r, g, b), pathSelector, propNameExpression);
	}

	void AssertProperty(byte r, byte g, byte b, byte a, Func<IBuiltInTexturePathLibrary, ReadOnlySpan<char>> pathSelector, [CallerArgumentExpression(nameof(pathSelector))] string? propNameExpression = null) {
		AssertProperty(new TexelRgba32(r, g, b, a), pathSelector, propNameExpression);
	}

	void AssertProperty(Real r, Real g, Real b, Func<IBuiltInTexturePathLibrary, ReadOnlySpan<char>> pathSelector, [CallerArgumentExpression(nameof(pathSelector))] string? propNameExpression = null) {
		AssertProperty(TexelRgb24.FromNormalizedFloats(r, g, b), pathSelector, propNameExpression);
	}

	void AssertProperty(Real r, Real g, Real b, Real a, Func<IBuiltInTexturePathLibrary, ReadOnlySpan<char>> pathSelector, [CallerArgumentExpression(nameof(pathSelector))] string? propNameExpression = null) {
		AssertProperty(TexelRgba32.FromNormalizedFloats(r, g, b, a), pathSelector, propNameExpression);
	}

	void AssertProperty<TTexel>(TTexel expectation, Func<IBuiltInTexturePathLibrary, ReadOnlySpan<char>> pathSelector, [CallerArgumentExpression(nameof(pathSelector))] string? propNameExpression = null) {
		var prop = propNameExpression!.Split('.').Last();
		Assert.IsTrue(_referencedProperties.Add(prop));

		var path = pathSelector(_lib);
		var builtin = _lib.GetBuiltInTexel(path);
		Assert.AreEqual(expectation, (object?) builtin?.First ?? (object?) builtin?.Second);
	}
} 