// Created on 2025-11-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Numerics;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TexelRgba32Test {
	static readonly TexelRgba32 TestTexel = new(11, 22, 33, 44);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(11, TestTexel.R);
		Assert.AreEqual(22, TestTexel.G);
		Assert.AreEqual(33, TestTexel.B);
		Assert.AreEqual(44, TestTexel.A);

		Assert.AreEqual(TestTexel, new TexelRgba32(new TexelRgb24(11, 22, 33), 44));
		Assert.AreEqual(TestTexel, new TexelRgba32(ColorVect.FromRgba32(11, 22, 33, 44)));
		Assert.AreEqual(TestTexel, TexelRgba32.ConvertFrom(ColorVect.FromRgba32(11, 22, 33, 44)));
	}

	[Test]
	public void ShouldCorrectlyConvert() {
		Assert.AreEqual(new TexelRgb24(11, 22, 33), TestTexel.ToRgb24());
		Assert.AreEqual(ColorVect.FromRgba32(11, 22, 33, 44), TestTexel.ToColorVect());
		Assert.AreEqual(TestTexel, TexelRgba32.FromByteComponents(11, 22, 33, 44));
		Assert.AreEqual(TestTexel, TexelRgba32.FromNormalizedFloats(11f / 255f, 22f / 255f, 33f / 255f, 44f / 255f));
		Assert.AreEqual(TexelRgba32.FromNormalizedFloats(11f / 255f, 22f / 255f, 33f / 255f, 44f / 255f), TexelRgba32.FromNormalizedFloats((Real) 11f / 255f, (Real) 22f / 255f, (Real) 33f / 255f, (Real) 44f / 255f));
		Assert.AreEqual(new Vector4(11f / 255f, 22f / 255f, 33f / 255f, 44f / 255f), TestTexel.ToNormalizedFloats());
	}

	[Test]
	public void ShouldCorrectlyImplementIndexing() {
		Assert.AreEqual(11, TestTexel[0]);
		Assert.AreEqual(22, TestTexel[1]);
		Assert.AreEqual(33, TestTexel[2]);
		Assert.AreEqual(44, TestTexel[3]);
		Assert.AreEqual(11, TestTexel[ColorChannel.R]);
		Assert.AreEqual(22, TestTexel[ColorChannel.G]);
		Assert.AreEqual(33, TestTexel[ColorChannel.B]);
		Assert.AreEqual(44, TestTexel[ColorChannel.A]);

		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[-1]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[4]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[(ColorChannel) (-1)]);
	}

	[Test]
	public void ShouldCorrectlySerializeToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<TexelRgba32>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestTexel);
		ByteSpanSerializationTestUtils.AssertBytes(TestTexel, 11, 22, 33, 44);
	}

	[Test]
	public void ShouldCorrectlyInvertChannels() {
		Assert.AreEqual(TestTexel with { R = Byte.MaxValue - 11 }, TestTexel.WithInvertedChannelIfPresent(0));
		Assert.AreEqual(TestTexel with { G = Byte.MaxValue - 22 }, TestTexel.WithInvertedChannelIfPresent(1));
		Assert.AreEqual(TestTexel with { B = Byte.MaxValue - 33 }, TestTexel.WithInvertedChannelIfPresent(2));
		Assert.AreEqual(TestTexel with { A = Byte.MaxValue - 44 }, TestTexel.WithInvertedChannelIfPresent(3));
	}

	[Test]
	public void ShouldCorrectlySwizzle() {
		Assert.AreEqual(
			new TexelRgba32(TestTexel.G, TestTexel.B, TestTexel.A, TestTexel.R),
			TestTexel.SwizzlePresentChannels(
				ColorChannel.G,
				ColorChannel.B,
				ColorChannel.A,
				ColorChannel.R
			)
		);
	}
}