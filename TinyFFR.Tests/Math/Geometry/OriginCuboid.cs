// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class OriginCuboidTest {
	const float TestTolerance = 0.01f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area+of+cuboid+with+width+7.2+height+13.6+depth+1.4
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);
		Assert.AreEqual(7.2f, cuboid.Width, TestTolerance);
		Assert.AreEqual(13.6f, cuboid.Height, TestTolerance);
		Assert.AreEqual(1.4f, cuboid.Depth, TestTolerance);
		Assert.AreEqual(7.2f / 2f, cuboid.HalfWidth, TestTolerance);
		Assert.AreEqual(13.6f / 2f, cuboid.HalfHeight, TestTolerance);
		Assert.AreEqual(1.4f / 2f, cuboid.HalfDepth, TestTolerance);
		Assert.AreEqual(254.08f, cuboid.SurfaceArea, TestTolerance);
		Assert.AreEqual(137.088f, cuboid.Volume, TestTolerance);
	}

	[Test]
	public void StaticFactoriesShouldCorrectlyConstruct() {
		AssertToleranceEquals(
			new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f),
			OriginCuboid.FromHalfDimensions(7.2f / 2f, 13.6f / 2f, 1.4f / 2f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyModifyWithInitProperties() {
		void AssertCuboid(OriginCuboid input, float expectedWidth, float expectedHeight, float expectedDepth) {
			AssertToleranceEquals(new OriginCuboid(expectedWidth, expectedHeight, expectedDepth), input, TestTolerance);
		}
		
		var startingValue = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		AssertCuboid(startingValue with { Width = 10f }, 10f, startingValue.Height, startingValue.Depth);
		AssertCuboid(startingValue with { Height = 10f }, startingValue.Width, 10f, startingValue.Depth);
		AssertCuboid(startingValue with { Depth = 10f }, startingValue.Width, startingValue.Height, 10f);
		AssertCuboid(startingValue with { HalfWidth = 10f }, 20f, startingValue.Height, startingValue.Depth);
		AssertCuboid(startingValue with { HalfHeight = 10f }, startingValue.Width, 20f, startingValue.Depth);
		AssertCuboid(startingValue with { HalfDepth = 10f }, startingValue.Width, startingValue.Height, 20f);

		Assert.AreEqual(400f, (startingValue with { SurfaceArea = 400f }).SurfaceArea, TestTolerance);
		Assert.AreEqual(100f, (startingValue with { SurfaceArea = 100f }).SurfaceArea, TestTolerance);
		Assert.AreEqual(300f, (startingValue with { Volume = 300f }).Volume, TestTolerance);
		Assert.AreEqual(100f, (startingValue with { Volume = 100f }).Volume, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new OriginCuboid(width: 7.2f * 3f, height: 13.6f * 3f, depth: 1.4f * 3f), 
			new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f).ScaledBy(3f), 
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateVertexLocations() {
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		AssertToleranceEquals(new(3.6f, 6.8f, 0.7f), cuboid.GetCorner(DiagonalOrientation3D.LeftUpForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, -0.7f), cuboid.GetCorner(DiagonalOrientation3D.LeftUpBackward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0.7f), cuboid.GetCorner(DiagonalOrientation3D.LeftDownForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, -0.7f), cuboid.GetCorner(DiagonalOrientation3D.LeftDownBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0.7f), cuboid.GetCorner(DiagonalOrientation3D.RightUpForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, -0.7f), cuboid.GetCorner(DiagonalOrientation3D.RightUpBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0.7f), cuboid.GetCorner(DiagonalOrientation3D.RightDownForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, -0.7f), cuboid.GetCorner(DiagonalOrientation3D.RightDownBackward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => cuboid.GetCorner(DiagonalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSidePlanes() {
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		AssertToleranceEquals(new Plane(Direction.Left, new(3.6f, 0f, 0f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Left), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Right, new(-3.6f, 0f, 0f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Right), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Up, new(0f, 6.8f, 0f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Up), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, new(0f, -6.8f, 0f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Down), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Forward, new(0f, 0f, 0.7f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Forward), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Backward, new(0f, 0f, -0.7f)), cuboid.GetSurfacePlane(CardinalOrientation3D.Backward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => cuboid.GetSurfacePlane(CardinalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateEdges() {
		const float W = 0.5f * 7.2f;
		const float H = 0.5f * 13.6f;
		const float D = 0.5f * 1.4f;
		var cuboid = new OriginCuboid(W * 2f, H * 2f, D * 2f);

		void AssertOrientation(IntercardinalOrientation3D orientation, Location expectedLinePointA, Location expectedLinePointB) {
			Assert.IsTrue(cuboid.GetEdge(orientation).EqualsDisregardingDirection(new(expectedLinePointA, expectedLinePointB), TestTolerance));
		}

		AssertOrientation(IntercardinalOrientation3D.UpForward, new(W, H, D), new(-W, H, D));
		AssertOrientation(IntercardinalOrientation3D.UpBackward, new(W, H, -D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.DownForward, new(W, -H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.DownBackward, new(W, -H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation3D.LeftForward, new(W, H, D), new(W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.LeftBackward, new(W, H, -D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightForward, new(-W, H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.RightBackward, new(-W, H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation3D.LeftUp, new(W, H, D), new(W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.LeftDown, new(W, -H, D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightUp, new(-W, H, D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightDown, new(-W, -H, D), new(-W, -H, -D));

		Assert.Throws<ArgumentOutOfRangeException>(() => cuboid.GetEdge(IntercardinalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyReturnDimensionFromAxis() {
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(7.2f, cuboid.GetDimension(Axis.X), TestTolerance);
		Assert.AreEqual(13.6f, cuboid.GetDimension(Axis.Y), TestTolerance);
		Assert.AreEqual(1.4f, cuboid.GetDimension(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = cuboid.GetDimension(Axis.None));

		Assert.AreEqual(0.5f * 7.2f, cuboid.GetHalfDimension(Axis.X), TestTolerance);
		Assert.AreEqual(0.5f * 13.6f, cuboid.GetHalfDimension(Axis.Y), TestTolerance);
		Assert.AreEqual(0.5f * 1.4f, cuboid.GetHalfDimension(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = cuboid.GetHalfDimension(Axis.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSideSurfaceAreas() {
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(13.6f * 1.4f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Left), TestTolerance);
		Assert.AreEqual(13.6f * 1.4f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Right), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Up), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Down), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Forward), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, cuboid.GetSideSurfaceArea(CardinalOrientation3D.Backward), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = cuboid.GetSideSurfaceArea(CardinalOrientation3D.None));
	}
	
	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "OriginCuboid[Width 7.2 | Height 13.6 | Depth 1.4]";
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(Expectation, cuboid.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		cuboid.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}
	
	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "OriginCuboid[Width 7.2 | Height 13.6 | Depth 1.4]";
		var expectation = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(expectation, OriginCuboid.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, OriginCuboid.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(expectation, result);
	}
	
	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		var cuboid = new OriginCuboid(width: 7.2f, height: 13.6f, depth: 1.4f);
		Assert.AreEqual(3, OriginCuboid.ConvertToSpan(cuboid).Length);
		Assert.AreEqual(7.2f / 2f, OriginCuboid.ConvertToSpan(cuboid)[0]);
		Assert.AreEqual(13.6f / 2f, OriginCuboid.ConvertToSpan(cuboid)[1]);
		Assert.AreEqual(1.4f / 2f, OriginCuboid.ConvertToSpan(cuboid)[2]);
		Assert.AreEqual(cuboid, OriginCuboid.ConvertFromSpan(OriginCuboid.ConvertToSpan(cuboid)));
	}
	
	[Test]
	public void ShouldCorrectlyInterpolate() {
		var a = new OriginCuboid(5f, 10f, 20f);
		var b = new OriginCuboid(15f, 30f, 60f);
		Assert.AreEqual(new OriginCuboid(10f, 20f, 40f), OriginCuboid.Interpolate(a, b, 0.5f));
		Assert.AreEqual(new OriginCuboid(5f, 10f, 20f), OriginCuboid.Interpolate(a, b, 0f));
		Assert.AreEqual(new OriginCuboid(15f, 30f, 60f), OriginCuboid.Interpolate(a, b, 1f));
		Assert.AreEqual(new OriginCuboid(20f, 40f, 80f), OriginCuboid.Interpolate(a, b, 1.5f));
		Assert.AreEqual(new OriginCuboid(0f, 0f, 0f), OriginCuboid.Interpolate(a, b, -0.5f));
	}
	
	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;
		var a = new OriginCuboid(5f, 10f, 20f);
		var b = new OriginCuboid(15f, 30f, 60f);

		for (var i = 0; i < NumIterations; ++i) {
			var val = OriginCuboid.CreateNewRandom(a, b);
			Assert.GreaterOrEqual(val.Width, a.Width);
			Assert.Less(val.Width, b.Width);
			Assert.GreaterOrEqual(val.Height, a.Height);
			Assert.Less(val.Height, b.Height);
			Assert.GreaterOrEqual(val.Depth, a.Depth);
			Assert.Less(val.Depth, b.Depth);

			val = OriginCuboid.CreateNewRandom();
			Assert.GreaterOrEqual(val.Width, OriginCuboid.DefaultRandomMin);
			Assert.Less(val.Width, OriginCuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.Height, OriginCuboid.DefaultRandomMin);
			Assert.Less(val.Height, OriginCuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.Depth, OriginCuboid.DefaultRandomMin);
			Assert.Less(val.Depth, OriginCuboid.DefaultRandomMax);
		}
	}
}