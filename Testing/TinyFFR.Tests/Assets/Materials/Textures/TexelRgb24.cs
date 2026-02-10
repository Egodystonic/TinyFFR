// Created on 2025-11-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Numerics;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TexelRgb24Test {
	static readonly TexelRgb24 TestTexel = new(11, 22, 33);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(11, TestTexel.R);
		Assert.AreEqual(22, TestTexel.G);
		Assert.AreEqual(33, TestTexel.B);

		Assert.AreEqual(TestTexel, new TexelRgb24(new TexelRgba32(11, 22, 33, 44)));
		Assert.AreEqual(TestTexel, new TexelRgb24(ColorVect.FromRgb24(11, 22, 33)));
		Assert.AreEqual(TestTexel, TexelRgb24.ConvertFrom(ColorVect.FromRgb24(11, 22, 33)));
	}

	[Test]
	public void ShouldCorrectlyConvert() {
		Assert.AreEqual(new TexelRgba32(11, 22, 33, Byte.MaxValue), TestTexel.ToRgba32());
		Assert.AreEqual(ColorVect.FromRgb24(11, 22, 33), TestTexel.ToColorVect());
		Assert.AreEqual(TestTexel, TexelRgb24.FromByteComponents(11, 22, 33));
		Assert.AreEqual(TestTexel, TexelRgb24.FromNormalizedFloats(11f / 255f, 22f / 255f, 33f / 255f));
		Assert.AreEqual(TexelRgb24.FromNormalizedFloats(11f / 255f, 22f / 255f, 33f / 255f), TexelRgb24.FromNormalizedFloats((Real) 11f / 255f, (Real) 22f / 255f, (Real) 33f / 255f));
		Assert.AreEqual(new Vector3(11f / 255f, 22f / 255f, 33f / 255f), TestTexel.ToNormalizedFloats());
	}

	[Test]
	public void ShouldCorrectlyImplementIndexing() {
		Assert.AreEqual(11, TestTexel[0]);
		Assert.AreEqual(22, TestTexel[1]);
		Assert.AreEqual(33, TestTexel[2]);
		Assert.AreEqual(11, TestTexel[ColorChannel.R]);
		Assert.AreEqual(22, TestTexel[ColorChannel.G]);
		Assert.AreEqual(33, TestTexel[ColorChannel.B]);

		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[-1]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[3]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[ColorChannel.A]);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestTexel[(ColorChannel) (-1)]);
	}

	[Test]
	public void ShouldCorrectlySerializeToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<TexelRgb24>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestTexel);
		ByteSpanSerializationTestUtils.AssertBytes(TestTexel, 11, 22, 33);
	}

	[Test]
	public void ShouldCorrectlyInvertChannels() {
		Assert.AreEqual(TestTexel with { R = Byte.MaxValue - 11 }, TestTexel.WithInvertedChannelIfPresent(0));
		Assert.AreEqual(TestTexel with { G = Byte.MaxValue - 22 }, TestTexel.WithInvertedChannelIfPresent(1));
		Assert.AreEqual(TestTexel with { B = Byte.MaxValue - 33 }, TestTexel.WithInvertedChannelIfPresent(2));
		Assert.AreEqual(TestTexel, TestTexel.WithInvertedChannelIfPresent(3));
	}

	[Test]
	public void ShouldCorrectlySwizzle() {
		Assert.AreEqual(
			new TexelRgb24(TestTexel.G, TestTexel.B, TestTexel.R),
			TestTexel.SwizzlePresentChannels(
				ColorChannel.G,
				ColorChannel.B,
				ColorChannel.R,
				ColorChannel.A
			)
		);
	}

	[Test]
	public void ShouldCorrectlyBlend() {
		var start = new TexelRgb24(50, 80, 100);
		var end = new TexelRgb24(150, 180, 200);

		Assert.AreEqual(start, TexelRgb24.Blend(start, end, 0f));
		Assert.AreEqual(end, TexelRgb24.Blend(start, end, 1f));
		Assert.AreEqual(new TexelRgb24(100, 130, 150), TexelRgb24.Blend(start, end, 0.5f));
		Assert.AreEqual(new TexelRgb24(0, 30, 50), TexelRgb24.Blend(start, end, -0.5f));
		Assert.AreEqual(new TexelRgb24(200, 230, 250), TexelRgb24.Blend(start, end, 1.5f));
		Assert.AreEqual(
			new TexelRgb24(60, 150, 225),
			TexelRgb24.Blend(new TexelRgb24(10, 100, 200), new TexelRgb24(110, 200, 250), 0.5f)
		);
		
		Assert.AreEqual(start, TexelRgb24.Blend(start, start, 0f));
		Assert.AreEqual(start, TexelRgb24.Blend(start, start, 0.5f));
		Assert.AreEqual(start, TexelRgb24.Blend(start, start, 1f));
		Assert.AreEqual(start, TexelRgb24.Blend(start, start, -0.5f));
		Assert.AreEqual(start, TexelRgb24.Blend(start, start, 1.5f));
	}
}