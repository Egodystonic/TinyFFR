// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class CuboidDescriptorTest {
	const float TestTolerance = 0.01f;
	// Half extents will be:							 3.6f		    6.8f		 0.7f
	static readonly CuboidDescriptor TestCuboid = new(width: 7.2f, height: 13.6f, depth: 1.4f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area+of+cuboid+with+width+7.2+height+13.6+depth+1.4
		Assert.AreEqual(7.2f, TestCuboid.Width, TestTolerance);
		Assert.AreEqual(13.6f, TestCuboid.Height, TestTolerance);
		Assert.AreEqual(1.4f, TestCuboid.Depth, TestTolerance);
		Assert.AreEqual(7.2f / 2f, TestCuboid.HalfWidth, TestTolerance);
		Assert.AreEqual(13.6f / 2f, TestCuboid.HalfHeight, TestTolerance);
		Assert.AreEqual(1.4f / 2f, TestCuboid.HalfDepth, TestTolerance);
		Assert.AreEqual(254.08f, TestCuboid.SurfaceArea, TestTolerance);
		Assert.AreEqual(137.088f, TestCuboid.Volume, TestTolerance);
	}

	[Test]
	public void StaticFactoriesShouldCorrectlyConstruct() {
		AssertToleranceEquals(
			TestCuboid,
			CuboidDescriptor.FromHalfDimensions(7.2f / 2f, 13.6f / 2f, 1.4f / 2f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyModifyWithInitProperties() {
		void AssertCuboid(CuboidDescriptor input, float expectedWidth, float expectedHeight, float expectedDepth) {
			AssertToleranceEquals(new CuboidDescriptor(expectedWidth, expectedHeight, expectedDepth), input, TestTolerance);
		}
		
		var startingValue = TestCuboid;

		AssertCuboid(startingValue with { Width = 10f }, 10f, startingValue.Height, startingValue.Depth);
		AssertCuboid(startingValue with { Height = 10f }, startingValue.Width, 10f, startingValue.Depth);
		AssertCuboid(startingValue with { Depth = 10f }, startingValue.Width, startingValue.Height, 10f);
		AssertCuboid(startingValue with { HalfWidth = 10f }, 20f, startingValue.Height, startingValue.Depth);
		AssertCuboid(startingValue with { HalfHeight = 10f }, startingValue.Width, 20f, startingValue.Depth);
		AssertCuboid(startingValue with { HalfDepth = 10f }, startingValue.Width, startingValue.Height, 20f);
	}

	[Test]
	public void ShouldCorrectlyModifyCuboidUsingWithMethods() {
		Assert.AreEqual(400f, TestCuboid.WithSurfaceArea(400f).SurfaceArea, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.WithSurfaceArea(100f).SurfaceArea, TestTolerance);
		Assert.AreEqual(300f, TestCuboid.WithVolume(300f).Volume, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.WithVolume(100f).Volume, TestTolerance);

		AssertToleranceEquals(TestCuboid, TestCuboid.WithSurfaceArea(TestCuboid.SurfaceArea), TestTolerance);
		AssertToleranceEquals(TestCuboid, TestCuboid.WithVolume(TestCuboid.Volume), TestTolerance);
		AssertToleranceEquals(TestCuboid, TestCuboid.WithSurfaceArea(TestCuboid.SurfaceArea * 3f).WithSurfaceArea(TestCuboid.SurfaceArea), TestTolerance);
		AssertToleranceEquals(TestCuboid, TestCuboid.WithVolume(TestCuboid.Volume * 3f).WithVolume(TestCuboid.Volume), TestTolerance);
		AssertToleranceEquals(TestCuboid, TestCuboid.WithSurfaceArea(TestCuboid.SurfaceArea * 0.3f).WithSurfaceArea(TestCuboid.SurfaceArea), TestTolerance);
		AssertToleranceEquals(TestCuboid, TestCuboid.WithVolume(TestCuboid.Volume * 0.3f).WithVolume(TestCuboid.Volume), TestTolerance);
	}
	
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "CuboidDescriptor[Width 7.2 | Height 13.6 | Depth 1.4]";

		Assert.AreEqual(Expectation, TestCuboid.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestCuboid.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}
	
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "CuboidDescriptor[Width 7.2 | Height 13.6 | Depth 1.4]";

		Assert.AreEqual(TestCuboid, CuboidDescriptor.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, CuboidDescriptor.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestCuboid, result);
	}
	
	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<CuboidDescriptor>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestCuboid);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestCuboid, TestCuboid.Width, TestCuboid.Height, TestCuboid.Depth);
	}
	
	[Test]
	public void ShouldCorrectlyInterpolate() {
		var a = new CuboidDescriptor(5f, 10f, 20f);
		var b = new CuboidDescriptor(15f, 30f, 60f);
		Assert.AreEqual(new CuboidDescriptor(10f, 20f, 40f), CuboidDescriptor.Interpolate(a, b, 0.5f));
		Assert.AreEqual(new CuboidDescriptor(5f, 10f, 20f), CuboidDescriptor.Interpolate(a, b, 0f));
		Assert.AreEqual(new CuboidDescriptor(15f, 30f, 60f), CuboidDescriptor.Interpolate(a, b, 1f));
		Assert.AreEqual(new CuboidDescriptor(20f, 40f, 80f), CuboidDescriptor.Interpolate(a, b, 1.5f));
		Assert.AreEqual(new CuboidDescriptor(0f, 0f, 0f), CuboidDescriptor.Interpolate(a, b, -0.5f));
	}
	
	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;
		var a = new CuboidDescriptor(5f, 10f, 20f);
		var b = new CuboidDescriptor(15f, 30f, 60f);

		for (var i = 0; i < NumIterations; ++i) {
			var val = CuboidDescriptor.NewRandom(a, b);
			Assert.GreaterOrEqual(val.Width, a.Width);
			Assert.Less(val.Width, b.Width);
			Assert.GreaterOrEqual(val.Height, a.Height);
			Assert.Less(val.Height, b.Height);
			Assert.GreaterOrEqual(val.Depth, a.Depth);
			Assert.Less(val.Depth, b.Depth);

			val = CuboidDescriptor.NewRandom();
			Assert.GreaterOrEqual(val.HalfWidth, CuboidDescriptor.DefaultRandomMin);
			Assert.Less(val.HalfWidth, CuboidDescriptor.DefaultRandomMax);
			Assert.GreaterOrEqual(val.HalfHeight, CuboidDescriptor.DefaultRandomMin);
			Assert.Less(val.HalfHeight, CuboidDescriptor.DefaultRandomMax);
			Assert.GreaterOrEqual(val.HalfDepth, CuboidDescriptor.DefaultRandomMin);
			Assert.Less(val.HalfDepth, CuboidDescriptor.DefaultRandomMax);
		}
	}
}