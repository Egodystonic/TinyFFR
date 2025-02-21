// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class CuboidTest {
	const float TestTolerance = 0.01f;
	// Half extents will be:									 3.6f		   6.8f			 0.7f
	static readonly Cuboid TestCuboid = new(width: 7.2f, height: 13.6f, depth: 1.4f);

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
			Cuboid.FromHalfDimensions(7.2f / 2f, 13.6f / 2f, 1.4f / 2f),
			TestTolerance
		);
		Assert.AreEqual(new Cuboid(3f, 3f, 3f), new Cuboid(3f));
	}

	[Test]
	public void ShouldCorrectlyModifyWithInitProperties() {
		void AssertCuboid(Cuboid input, float expectedWidth, float expectedHeight, float expectedDepth) {
			AssertToleranceEquals(new Cuboid(expectedWidth, expectedHeight, expectedDepth), input, TestTolerance);
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
	public void ShouldCorrectlyDeterminePhysicalValidity() {
		Assert.AreEqual(true, new Cuboid(1f, 1f, 1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(-1f, 1f, 1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, -1f, 1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, -1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(0f, 1f, 1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 0f, 1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, 0f).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, Single.NaN).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, Single.NegativeZero).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, Single.PositiveInfinity).IsPhysicallyValid);
		Assert.AreEqual(false, new Cuboid(1f, 1f, Single.NegativeInfinity).IsPhysicallyValid);
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
		const string Expectation = "Cuboid[Width 7.2 | Height 13.6 | Depth 1.4]";

		Assert.AreEqual(Expectation, TestCuboid.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestCuboid.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}
	
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Cuboid[Width 7.2 | Height 13.6 | Depth 1.4]";

		Assert.AreEqual(TestCuboid, Cuboid.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Cuboid.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestCuboid, result);
	}
	
	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Cuboid>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestCuboid);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestCuboid, TestCuboid.Width, TestCuboid.Height, TestCuboid.Depth);
	}
	
	[Test]
	public void ShouldCorrectlyInterpolate() {
		var a = new Cuboid(5f, 10f, 20f);
		var b = new Cuboid(15f, 30f, 60f);
		Assert.AreEqual(new Cuboid(10f, 20f, 40f), Cuboid.Interpolate(a, b, 0.5f));
		Assert.AreEqual(new Cuboid(5f, 10f, 20f), Cuboid.Interpolate(a, b, 0f));
		Assert.AreEqual(new Cuboid(15f, 30f, 60f), Cuboid.Interpolate(a, b, 1f));
		Assert.AreEqual(new Cuboid(20f, 40f, 80f), Cuboid.Interpolate(a, b, 1.5f));
		Assert.AreEqual(new Cuboid(0f, 0f, 0f), Cuboid.Interpolate(a, b, -0.5f));
	}
	
	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;
		var a = new Cuboid(5f, 10f, 20f);
		var b = new Cuboid(15f, 30f, 60f);

		for (var i = 0; i < NumIterations; ++i) {
			var val = Cuboid.Random(a, b);
			Assert.GreaterOrEqual(val.Width, a.Width);
			Assert.Less(val.Width, b.Width);
			Assert.GreaterOrEqual(val.Height, a.Height);
			Assert.Less(val.Height, b.Height);
			Assert.GreaterOrEqual(val.Depth, a.Depth);
			Assert.Less(val.Depth, b.Depth);

			val = Cuboid.Random();
			Assert.GreaterOrEqual(val.HalfWidth, Cuboid.DefaultRandomMin);
			Assert.Less(val.HalfWidth, Cuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.HalfHeight, Cuboid.DefaultRandomMin);
			Assert.Less(val.HalfHeight, Cuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.HalfDepth, Cuboid.DefaultRandomMin);
			Assert.Less(val.HalfDepth, Cuboid.DefaultRandomMax);
		}
	}

	[Test]
	public void ShouldCorrectlyEnumerateCorners() {
		Assert.AreEqual(OrientationUtils.AllDiagonals.Length, TestCuboid.Corners.Count);

		var count = 0;
		foreach (var corner in TestCuboid.Corners) {
			Assert.AreEqual(TestCuboid.CornerAt(OrientationUtils.AllDiagonals[count]), corner);
			++count;
		}

		Assert.AreEqual(8, count);
	}

	[Test]
	public void ShouldCorrectlyEnumerateEdges() {
		Assert.AreEqual(OrientationUtils.AllIntercardinals.Length, TestCuboid.Edges.Count);

		var count = 0;
		foreach (var edge in TestCuboid.Edges) {
			Assert.AreEqual(TestCuboid.EdgeAt(OrientationUtils.AllIntercardinals[count]), edge);
			++count;
		}

		Assert.AreEqual(12, count);
	}

	[Test]
	public void ShouldCorrectlyEnumerateSides() {
		Assert.AreEqual(OrientationUtils.AllCardinals.Length, TestCuboid.Sides.Count);

		var count = 0;
		foreach (var side in TestCuboid.Sides) {
			Assert.AreEqual(TestCuboid.SideAt(OrientationUtils.AllCardinals[count]), side);
			++count;
		}

		Assert.AreEqual(6, count);
	}

	[Test]
	public void ShouldCorrectlyEnumerateCentroids() {
		Assert.AreEqual(OrientationUtils.AllCardinals.Length, TestCuboid.Centroids.Count);

		var count = 0;
		foreach (var side in TestCuboid.Centroids) {
			Assert.AreEqual(TestCuboid.CentroidAt(OrientationUtils.AllCardinals[count]), side);
			++count;
		}

		Assert.AreEqual(6, count);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new Cuboid(width: 7.2f * 3f, height: 13.6f * 3f, depth: 1.4f * 3f),
			TestCuboid.ScaledBy(3f),
			TestTolerance
		);

		AssertToleranceEquals(
			new Cuboid(width: 7.2f * -1f, height: 13.6f * 0.5f, depth: 1.4f * 2f),
			TestCuboid.ScaledBy((-1f, 0.5f, 2f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateCornerLocations() {
		AssertToleranceEquals(new(3.6f, 6.8f, 0.7f), TestCuboid.CornerAt(DiagonalOrientation.LeftUpForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, -0.7f), TestCuboid.CornerAt(DiagonalOrientation.LeftUpBackward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0.7f), TestCuboid.CornerAt(DiagonalOrientation.LeftDownForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, -0.7f), TestCuboid.CornerAt(DiagonalOrientation.LeftDownBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0.7f), TestCuboid.CornerAt(DiagonalOrientation.RightUpForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, -0.7f), TestCuboid.CornerAt(DiagonalOrientation.RightUpBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0.7f), TestCuboid.CornerAt(DiagonalOrientation.RightDownForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, -0.7f), TestCuboid.CornerAt(DiagonalOrientation.RightDownBackward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.CornerAt(DiagonalOrientation.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSurfacePlanes() {
		AssertToleranceEquals(new Plane(Direction.Left, (3.6f, 0f, 0f)), TestCuboid.SideAt(CardinalOrientation.Left), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Right, (-3.6f, 0f, 0f)), TestCuboid.SideAt(CardinalOrientation.Right), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Up, (0f, 6.8f, 0f)), TestCuboid.SideAt(CardinalOrientation.Up), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, (0f, -6.8f, 0f)), TestCuboid.SideAt(CardinalOrientation.Down), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Forward, (0f, 0f, 0.7f)), TestCuboid.SideAt(CardinalOrientation.Forward), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Backward, (0f, 0f, -0.7f)), TestCuboid.SideAt(CardinalOrientation.Backward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.SideAt(CardinalOrientation.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateCentroids() {
		AssertToleranceEquals(new Location(3.6f, 0f, 0f), TestCuboid.CentroidAt(CardinalOrientation.Left), TestTolerance);
		AssertToleranceEquals(new Location(-3.6f, 0f, 0f), TestCuboid.CentroidAt(CardinalOrientation.Right), TestTolerance);
		AssertToleranceEquals(new Location(0f, 6.8f, 0f), TestCuboid.CentroidAt(CardinalOrientation.Up), TestTolerance);
		AssertToleranceEquals(new Location(0f, -6.8f, 0f), TestCuboid.CentroidAt(CardinalOrientation.Down), TestTolerance);
		AssertToleranceEquals(new Location(0f, 0f, 0.7f), TestCuboid.CentroidAt(CardinalOrientation.Forward), TestTolerance);
		AssertToleranceEquals(new Location(0f, 0f, -0.7f), TestCuboid.CentroidAt(CardinalOrientation.Backward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.CentroidAt(CardinalOrientation.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateEdges() {
		const float W = 0.5f * 7.2f;
		const float H = 0.5f * 13.6f;
		const float D = 0.5f * 1.4f;
		var cuboid = new Cuboid(W * 2f, H * 2f, D * 2f);

		void AssertOrientation(IntercardinalOrientation orientation, Location expectedLinePointA, Location expectedLinePointB) {
			Assert.IsTrue(cuboid.EdgeAt(orientation).EqualsDisregardingDirection(new(expectedLinePointA, expectedLinePointB), TestTolerance));
		}

		AssertOrientation(IntercardinalOrientation.UpForward, new(W, H, D), new(-W, H, D));
		AssertOrientation(IntercardinalOrientation.UpBackward, new(W, H, -D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation.DownForward, new(W, -H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation.DownBackward, new(W, -H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation.LeftForward, new(W, H, D), new(W, -H, D));
		AssertOrientation(IntercardinalOrientation.LeftBackward, new(W, H, -D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation.RightForward, new(-W, H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation.RightBackward, new(-W, H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation.LeftUp, new(W, H, D), new(W, H, -D));
		AssertOrientation(IntercardinalOrientation.LeftDown, new(W, -H, D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation.RightUp, new(-W, H, D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation.RightDown, new(-W, -H, D), new(-W, -H, -D));

		Assert.Throws<ArgumentOutOfRangeException>(() => cuboid.EdgeAt(IntercardinalOrientation.None));
	}

	[Test]
	public void ShouldCorrectlyReturnDimensionOfGivenAxis() {
		Assert.AreEqual(7.2f, TestCuboid.GetExtent(Axis.X), TestTolerance);
		Assert.AreEqual(13.6f, TestCuboid.GetExtent(Axis.Y), TestTolerance);
		Assert.AreEqual(1.4f, TestCuboid.GetExtent(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestCuboid.GetExtent(Axis.None));

		Assert.AreEqual(0.5f * 7.2f, TestCuboid.GetHalfExtent(Axis.X), TestTolerance);
		Assert.AreEqual(0.5f * 13.6f, TestCuboid.GetHalfExtent(Axis.Y), TestTolerance);
		Assert.AreEqual(0.5f * 1.4f, TestCuboid.GetHalfExtent(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestCuboid.GetHalfExtent(Axis.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSideSurfaceAreas() {
		Assert.AreEqual(13.6f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Left), TestTolerance);
		Assert.AreEqual(13.6f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Right), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Up), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Down), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Forward), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, TestCuboid.GetSideSurfaceArea(CardinalOrientation.Backward), TestTolerance);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = TestCuboid.GetSideSurfaceArea(CardinalOrientation.None));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(1f, TestCuboid.DistanceFrom((4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 0f, 1.7f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((-4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, -7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 0f, -1.7f)), TestTolerance);

		Assert.AreEqual(MathF.Sqrt(2f), TestCuboid.DistanceFrom((4.6f, -7.8f, 0.7f)), TestTolerance);

		// Squared
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((0f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((5.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((0, 8.8f, 0f)), TestTolerance);
		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((0, 0f, 2.7f)), TestTolerance);
		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((-5.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((0, -8.8f, 0f)), TestTolerance);
		Assert.AreEqual(4f, TestCuboid.DistanceSquaredFrom((0, 0f, -2.7f)), TestTolerance);

		Assert.AreEqual(2f, TestCuboid.DistanceSquaredFrom((4.6f, -7.8f, 0.7f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		Assert.AreEqual(0.7f, TestCuboid.SurfaceDistanceFrom((0f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 0f, 1.7f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((-4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, -7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 0f, -1.7f)), TestTolerance);

		Assert.AreEqual(MathF.Sqrt(2f), TestCuboid.SurfaceDistanceFrom((4.6f, -7.8f, 0.7f)), TestTolerance);

		Assert.AreEqual(1f, new Cuboid(20f, 40f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(1f, new Cuboid(20f, 40f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(11f, new Cuboid(200f, 40f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(11f, new Cuboid(200f, 40f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(21f, new Cuboid(200f, 400f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(21f, new Cuboid(200f, 400f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);

		// Squared
		Assert.AreEqual(0.7f * 0.7f, TestCuboid.SurfaceDistanceSquaredFrom((0f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((0, 7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((0, 0f, 1.7f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((-4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((0, -7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceSquaredFrom((0, 0f, -1.7f)), TestTolerance);

		Assert.AreEqual(2f, TestCuboid.SurfaceDistanceSquaredFrom((4.6f, -7.8f, 0.7f)), TestTolerance);

		Assert.AreEqual(1f, new Cuboid(20f, 40f, 60f).SurfaceDistanceSquaredFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(1f, new Cuboid(20f, 40f, 60f).SurfaceDistanceSquaredFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(121f, new Cuboid(200f, 40f, 60f).SurfaceDistanceSquaredFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(121f, new Cuboid(200f, 40f, 60f).SurfaceDistanceSquaredFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(21f * 21f, new Cuboid(200f, 400f, 60f).SurfaceDistanceSquaredFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(21f * 21f, new Cuboid(200f, 400f, 60f).SurfaceDistanceSquaredFrom((-9f, -9f, -9f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocations() {
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((3.6f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 6.8f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, 0.7f)));
		Assert.AreEqual(true, TestCuboid.Contains((-3.6f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, -6.8f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, -0.7f)));

		Assert.AreEqual(true, TestCuboid.Contains((-3.6f, -6.8f, -0.7f)));
		Assert.AreEqual(true, TestCuboid.Contains((3.6f, 6.8f, 0.7f)));

		Assert.AreEqual(false, TestCuboid.Contains((4.6f, 0f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 7.8f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 0f, 1.7f)));
		Assert.AreEqual(false, TestCuboid.Contains((-4.6f, 0f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, -7.8f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 0f, -1.7f)));

		Assert.AreEqual(false, TestCuboid.Contains((4.6f, -7.8f, 0.7f)));
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocations() {
		AssertToleranceEquals((0f, 0f, 0f), TestCuboid.PointClosestTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.PointClosestTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.PointClosestTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.PointClosestTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.PointClosestTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.PointClosestTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.PointClosestTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.PointClosestTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.PointClosestTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.PointClosestTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.PointClosestTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.PointClosestTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.PointClosestTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.PointClosestTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.PointClosestTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestSurfacePointToLocations() {
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.SurfacePointClosestTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.SurfacePointClosestTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.SurfacePointClosestTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.SurfacePointClosestTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.SurfacePointClosestTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineLineIntersection() {
		ConvexShapeLineIntersection? intersection;

		// Line
		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);


		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);


		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);


		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f)));
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);




		// Ray
		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);



		// BoundedRay
		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 1000f));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new BoundedRay(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f) * 1000f));
		Assert.IsNull(intersection);




		// Line, Fast
		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);


		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.Second, TestTolerance);


		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.Second, TestTolerance);


		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f)));
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection.Value.Second, TestTolerance);




		// Ray, Fast
		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);



		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.Second, TestTolerance);



		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.Second, TestTolerance);


		intersection = TestCuboid.FastIntersectionWith(new Ray(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection.Value.Second, TestTolerance);




		// BoundedRay, Fast
		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection.Value.Second, TestTolerance);



		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection.Value.Second, TestTolerance);




		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.Second, TestTolerance);

		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection.Value.Second, TestTolerance);




		intersection = TestCuboid.FastIntersectionWith(new BoundedRay(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 1000f));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection.Value.Second, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersection() {
		// Line
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f))));


		// Ray
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f))));


		// BoundedRay
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(new BoundedRay(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f) * 1000f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToLines() {
		// Line
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.PointClosestTo(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.PointClosestTo(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));

		// Ray
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.PointClosestTo(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.PointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.PointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.PointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.PointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.PointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.PointClosestTo(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.PointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.PointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.PointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnLines() {
		// Line
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0f, 108.55f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((11.45f, 0, 8.55f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		// Ray
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		// Line
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new BoundedRay(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);





		// Line Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.DistanceSquaredFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.DistanceSquaredFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f * 11.1016f, TestCuboid.DistanceSquaredFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f * 153.3801f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f * 153.3801f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.DistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToLines() {
		// Line
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		// Ray
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.SurfacePointClosestTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.SurfacePointClosestTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.SurfacePointClosestTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f).Flipped), TestTolerance);

		AssertToleranceEquals((0.2887f, 0.2887f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals((-0.2887f, -0.2887f, -0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals((0.2887f, 0.2887f, 0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.2887f, -0.2887f, -0.7f), TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.SurfacePointClosestTo(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnLines() {
		// Line
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0f, 108.55f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((11.45f, 0, 8.55f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		// Ray
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f).Flipped), TestTolerance);

		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * 0.5f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * -0.5f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * 0.5f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * -0.5f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);

		var longCuboid = new Cuboid(1000f, 10f, 1f);
		var line = new BoundedRay(new Location(-485f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line), TestTolerance);
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line.Flipped), TestTolerance);
		line = new BoundedRay(new Location(-495f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line), TestTolerance);
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line.Flipped), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		// Line
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new BoundedRay(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);


		// Line Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f * 11.1016f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f * 153.3801f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay Squared
		Assert.AreEqual(64.0638f * 64.0638f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f * 51.2652f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f * 152.5229f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f * 153.3801f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(MathF.Pow(0.7f - 0.2887f, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(MathF.Pow(0.7f - 0.2887f, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(MathF.Pow(0.7f - 0.2887f, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(MathF.Pow(0.7f - 0.2887f, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfWidth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfHeight, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(MathF.Pow(100f - TestCuboid.HalfDepth, 2f), TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(100f, TestCuboid.SurfaceDistanceSquaredFrom(new BoundedRay(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);

		var longCuboid = new Cuboid(1000f, 10f, 1f);
		var line = new BoundedRay(new Location(-485f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line), TestTolerance);
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line.Flipped), TestTolerance);
		Assert.AreEqual(0.01f, longCuboid.SurfaceDistanceSquaredFrom(line), TestTolerance);
		Assert.AreEqual(0.01f, longCuboid.SurfaceDistanceSquaredFrom(line.Flipped), TestTolerance);
		line = new BoundedRay(new Location(-495f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line), TestTolerance);
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line.Flipped), TestTolerance);
		Assert.AreEqual(0.01f, longCuboid.SurfaceDistanceSquaredFrom(line), TestTolerance);
		Assert.AreEqual(0.01f, longCuboid.SurfaceDistanceSquaredFrom(line.Flipped), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			AssertToleranceEquals(
				TestCuboid.CornerAt(orientation),
				TestCuboid.PointClosestTo(new Plane(orientation.ToDirection(), 1000f)),
				TestTolerance
			);
			AssertToleranceEquals(
				TestCuboid.CornerAt(orientation),
				TestCuboid.SurfacePointClosestTo(new Plane(orientation.ToDirection(), 1000f)),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var edge = TestCuboid.EdgeAt(orientation);
			Assert.AreEqual(
				0f,
				edge.DistanceFrom(TestCuboid.PointClosestTo(new Plane(orientation.ToDirection(), 1000f))),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				edge.DistanceFrom(TestCuboid.SurfacePointClosestTo(new Plane(orientation.ToDirection(), 1000f))),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);

			foreach (var axis in OrientationUtils.AllAxes) {
				if (axis == orientation.GetAxis()) {
					Assert.AreEqual(
						TestCuboid.GetHalfExtent(axis) * orientation.GetAxisSign(),
						TestCuboid.PointClosestTo(plane)[axis],
						TestTolerance
					);
					Assert.AreEqual(
						TestCuboid.GetHalfExtent(axis) * orientation.GetAxisSign(),
						TestCuboid.SurfacePointClosestTo(plane)[axis],
						TestTolerance
					);
				}
				else {
					Assert.GreaterOrEqual(
						TestCuboid.PointClosestTo(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.PointClosestTo(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
					Assert.GreaterOrEqual(
						TestCuboid.SurfacePointClosestTo(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.SurfacePointClosestTo(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
				}
			}
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);

			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.PointClosestTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.SurfacePointClosestTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.PointClosestTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.SurfacePointClosestTo(plane)),
				TestTolerance
			);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var corner = TestCuboid.CornerAt(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			AssertToleranceEquals(
				orientation.ToDirection() * plane.DistanceFrom(corner) + corner,
				TestCuboid.ClosestPointOn(plane),
				TestTolerance
			);
			AssertToleranceEquals(
				orientation.ToDirection() * plane.DistanceFrom(corner) + corner,
				TestCuboid.ClosestPointToSurfaceOn(plane),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var edge = TestCuboid.EdgeAt(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var projectedEdge = edge.ProjectedOnTo(plane);
			Assert.AreEqual(
				0f,
				projectedEdge.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				projectedEdge.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);

			foreach (var axis in OrientationUtils.AllAxes) {
				if (axis == orientation.GetAxis()) {
					Assert.AreEqual(
						(orientation.ToDirection() * 1000f)[axis],
						TestCuboid.ClosestPointOn(plane)[axis],
						TestTolerance
					);
					Assert.AreEqual(
						(orientation.ToDirection() * 1000f)[axis],
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						TestTolerance
					);
				}
				else {
					Assert.GreaterOrEqual(
						TestCuboid.ClosestPointOn(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.ClosestPointOn(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
					Assert.GreaterOrEqual(
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
				}
			}
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);

			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		void AssertDistance(float expectedSignedDistance, Plane plane) {
			Assert.AreEqual(
				expectedSignedDistance,
				TestCuboid.SignedDistanceFrom(plane),
				TestTolerance
			);
			Assert.AreEqual(
				MathF.Abs(expectedSignedDistance),
				TestCuboid.DistanceFrom(plane),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var corner = TestCuboid.CornerAt(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			AssertDistance(-plane.DistanceFrom(corner), plane);
			AssertDistance(plane.DistanceFrom(corner), plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var edge = TestCuboid.EdgeAt(orientation);
			AssertDistance(-plane.DistanceFrom(edge), plane);
			AssertDistance(plane.DistanceFrom(edge), plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var expectedSignedDistance = -(1000f - TestCuboid.GetHalfExtent(orientation.GetAxis()));
			AssertDistance(expectedSignedDistance, plane);
			AssertDistance(-expectedSignedDistance, plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);
			AssertDistance(0f, plane);
			AssertDistance(0f, plane.Flipped);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleWithLines() {
		void AssertAngle(Angle? expectation, Ray ray) {
			var boundedRay = new BoundedRay(ray.StartPoint, ray.Direction * (ray.StartPoint.DistanceFromOrigin() * 2f));

			AssertToleranceEquals(expectation, TestCuboid.IncidentAngleWith(ray), TestTolerance);
			AssertToleranceEquals(expectation, TestCuboid.IncidentAngleWith(boundedRay), TestTolerance);
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestCuboid.FastIncidentAngleWith(ray), TestTolerance);
				AssertToleranceEquals(expectation, TestCuboid.FastIncidentAngleWith(boundedRay), TestTolerance);
			}
		}

		AssertAngle(Angle.Zero, new((-100f, 0f, 0f), (1f, 0f, 0f)));
		AssertAngle(Angle.Zero, new((100f, 0f, 0f), (-1f, 0f, 0f)));
		AssertAngle(Angle.Zero, new((0f, -100f, 0f), (0f, 1f, 0f)));
		AssertAngle(Angle.Zero, new((0f, 100f, 0f), (0f, -1f, 0f)));
		AssertAngle(Angle.Zero, new((0f, 0f, -100f), (0f, 0f, 1f)));
		AssertAngle(Angle.Zero, new((0f, 0f, 100f), (0f, 0f, -1f)));

		AssertAngle(Angle.EighthCircle, new((TestCuboid.HalfWidth + 100f, 100f, 0f), (-1f, -1f, 0f)));
		AssertAngle(Angle.EighthCircle, new((0f, TestCuboid.HalfHeight + 100f, 100f), (0f, -1f, -1f)));
		AssertAngle(Angle.EighthCircle, new((100f, 0f, TestCuboid.HalfDepth + 100f), (-1f, 0f, -1f)));
		AssertAngle(Angle.EighthCircle, new((-TestCuboid.HalfWidth - 100f, -100f, 0f), (1f, 1f, 0f)));
		AssertAngle(Angle.EighthCircle, new((0f, -TestCuboid.HalfHeight - 100f, -100f), (0f, 1f, 1f)));
		AssertAngle(Angle.EighthCircle, new((-100f, 0f, -TestCuboid.HalfDepth - 100f), (1f, 0f, 1f)));

		AssertAngle(null, new((TestCuboid.HalfWidth + TestTolerance, 0f, 0f), (1f, 0f, 0f)));
		AssertAngle(null, new((-TestCuboid.HalfWidth - TestTolerance, 0f, 0f), (-1f, 0f, 0f)));
		AssertAngle(null, new((0f, TestCuboid.HalfHeight + TestTolerance, 0f), (0f, 1f, 0f)));
		AssertAngle(null, new((0f, -TestCuboid.HalfHeight - TestTolerance, 0f), (0f, -1f, 0f)));
		AssertAngle(null, new((0f, 0f, TestCuboid.HalfDepth + TestTolerance), (0f, 0f, 1f)));
		AssertAngle(null, new((0f, 0f, -TestCuboid.HalfDepth - TestTolerance), (0f, 0f, -1f)));

		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (-1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (0f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 0f, 1f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.IncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (-1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (0f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 0f, 1f))), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestCuboid.FastIncidentAngleWith(new Ray((0f, 0f, 0f), (0f, 0f, -1f))), TestTolerance);

		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(-TestCuboid.HalfWidth + TestTolerance, 0f, 0f), new Location(TestCuboid.HalfWidth - TestTolerance, 0f, 0f))));
		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, -TestCuboid.HalfHeight + TestTolerance, 0f), new Location(0f, TestCuboid.HalfHeight - TestTolerance, 0f))));
		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, 0f, -TestCuboid.HalfDepth + TestTolerance), new Location(0f, 0f, TestCuboid.HalfDepth - TestTolerance))));
		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(-TestCuboid.HalfWidth + TestTolerance, 0f, 0f), new Location(TestCuboid.HalfWidth - TestTolerance, 0f, 0f)).Flipped));
		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, -TestCuboid.HalfHeight + TestTolerance, 0f), new Location(0f, TestCuboid.HalfHeight - TestTolerance, 0f)).Flipped));
		Assert.IsNull(TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, 0f, -TestCuboid.HalfDepth + TestTolerance), new Location(0f, 0f, TestCuboid.HalfDepth - TestTolerance)).Flipped));

		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f)).Flipped), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f)).Flipped), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 1f, 1f), new Location(0f, TestCuboid.HalfHeight + 1f, -1f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 1f, 1f), new Location(0f, TestCuboid.HalfHeight + 1f, -1f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 1f, 1f), new Location(0f, TestCuboid.HalfHeight + 1f, -1f)).Flipped), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 1f, 1f), new Location(0f, TestCuboid.HalfHeight + 1f, -1f)).Flipped), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(1f, 0f, TestCuboid.HalfDepth - 1f), new Location(-1f, 0f, TestCuboid.HalfDepth + 1f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(1f, 0f, TestCuboid.HalfDepth - 1f), new Location(-1f, 0f, TestCuboid.HalfDepth + 1f))), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.IncidentAngleWith(new BoundedRay(new Location(1f, 0f, TestCuboid.HalfDepth - 1f), new Location(-1f, 0f, TestCuboid.HalfDepth + 1f)).Flipped), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestCuboid.FastIncidentAngleWith(new BoundedRay(new Location(1f, 0f, TestCuboid.HalfDepth - 1f), new Location(-1f, 0f, TestCuboid.HalfDepth + 1f)).Flipped), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectLines() {
		void AssertReflection(Ray? expectation, Ray ray) {
			var boundedRay = new BoundedRay(ray.StartPoint, ray.Direction * 1000f);

			AssertToleranceEquals(expectation, TestCuboid.ReflectionOf(ray), TestTolerance);
			AssertToleranceEquals(expectation?.ToBoundedRay(1000f - ray.StartPoint.DistanceFrom(expectation!.Value.StartPoint)), TestCuboid.ReflectionOf(boundedRay), TestTolerance);
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestCuboid.FastReflectionOf(ray), TestTolerance);
				AssertToleranceEquals(expectation?.ToBoundedRay(1000f - ray.StartPoint.DistanceFrom(expectation!.Value.StartPoint)), TestCuboid.FastReflectionOf(boundedRay), TestTolerance);
			}
		}

		AssertReflection(new Ray((-TestCuboid.HalfWidth, 0f, 0f), (-1f, 0f, 0f)), new((-100f, 0f, 0f), (1f, 0f, 0f)));
		AssertReflection(new Ray((TestCuboid.HalfWidth, 0f, 0f), (1f, 0f, 0f)), new((100f, 0f, 0f), (-1f, 0f, 0f)));
		AssertReflection(new Ray((0f, -TestCuboid.HalfHeight, 0f), (0f, -1f, 0f)), new((0f, -100f, 0f), (0f, 1f, 0f)));
		AssertReflection(new Ray((0f, TestCuboid.HalfHeight, 0f), (0f, 1f, 0f)), new((0f, 100f, 0f), (0f, -1f, 0f)));
		AssertReflection(new Ray((0f, 0f, -TestCuboid.HalfDepth), (0f, 0f, -1f)), new((0f, 0f, -100f), (0f, 0f, 1f)));
		AssertReflection(new Ray((0f, 0f, TestCuboid.HalfDepth), (0f, 0f, 1f)), new((0f, 0f, 100f), (0f, 0f, -1f)));

		AssertReflection(new((TestCuboid.HalfWidth, 0f, 0f), (1f, -1f, 0f)), new((TestCuboid.HalfWidth + 100f, 100f, 0f), (-1f, -1f, 0f)));
		AssertReflection(new((0f, TestCuboid.HalfHeight, 0f), (0f, 1f, -1f)), new((0f, TestCuboid.HalfHeight + 100f, 100f), (0f, -1f, -1f)));
		AssertReflection(new((0f, 0f, TestCuboid.HalfDepth), (-1f, 0f, 1f)), new((100f, 0f, TestCuboid.HalfDepth + 100f), (-1f, 0f, -1f)));
		AssertReflection(new((-TestCuboid.HalfWidth, 0f, 0f), (-1f, 1f, 0f)), new((-TestCuboid.HalfWidth - 100f, -100f, 0f), (1f, 1f, 0f)));
		AssertReflection(new((0f, -TestCuboid.HalfHeight, 0f), (0f, -1f, 1f)), new((0f, -TestCuboid.HalfHeight - 100f, -100f), (0f, 1f, 1f)));
		AssertReflection(new((0f, 0f, -TestCuboid.HalfDepth), (1f, 0f, -1f)), new((-100f, 0f, -TestCuboid.HalfDepth - 100f), (1f, 0f, 1f)));

		AssertReflection(null, new((TestCuboid.HalfWidth + TestTolerance, 0f, 0f), (1f, 0f, 0f)));
		AssertReflection(null, new((-TestCuboid.HalfWidth - TestTolerance, 0f, 0f), (-1f, 0f, 0f)));
		AssertReflection(null, new((0f, TestCuboid.HalfHeight + TestTolerance, 0f), (0f, 1f, 0f)));
		AssertReflection(null, new((0f, -TestCuboid.HalfHeight - TestTolerance, 0f), (0f, -1f, 0f)));
		AssertReflection(null, new((0f, 0f, TestCuboid.HalfDepth + TestTolerance), (0f, 0f, 1f)));
		AssertReflection(null, new((0f, 0f, -TestCuboid.HalfDepth - TestTolerance), (0f, 0f, -1f)));

		AssertToleranceEquals(new Ray((TestCuboid.HalfWidth, 0f, 0f), (-1f, 0f, 0f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((-TestCuboid.HalfWidth, 0f, 0f), (1f, 0f, 0f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (-1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, TestCuboid.HalfHeight, 0f), (0f, -1f, 0f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, -TestCuboid.HalfHeight, 0f), (0f, 1f, 0f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (0f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, 0f, TestCuboid.HalfDepth), (0f, 0f, -1f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (0f, 0f, 1f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, 0f, -TestCuboid.HalfDepth), (0f, 0f, 1f)), TestCuboid.ReflectionOf(new Ray((0f, 0f, 0f), (0f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals(new Ray((TestCuboid.HalfWidth, 0f, 0f), (-1f, 0f, 0f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((-TestCuboid.HalfWidth, 0f, 0f), (1f, 0f, 0f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (-1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, TestCuboid.HalfHeight, 0f), (0f, -1f, 0f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, -TestCuboid.HalfHeight, 0f), (0f, 1f, 0f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (0f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, 0f, TestCuboid.HalfDepth), (0f, 0f, -1f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (0f, 0f, 1f))), TestTolerance);
		AssertToleranceEquals(new Ray((0f, 0f, -TestCuboid.HalfDepth), (0f, 0f, 1f)), TestCuboid.FastReflectionOf(new Ray((0f, 0f, 0f), (0f, 0f, -1f))), TestTolerance);

		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(-TestCuboid.HalfWidth + TestTolerance, 0f, 0f), new Location(TestCuboid.HalfWidth - TestTolerance, 0f, 0f))));
		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, -TestCuboid.HalfHeight + TestTolerance, 0f), new Location(0f, TestCuboid.HalfHeight - TestTolerance, 0f))));
		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, 0f, -TestCuboid.HalfDepth + TestTolerance), new Location(0f, 0f, TestCuboid.HalfDepth - TestTolerance))));
		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(-TestCuboid.HalfWidth + TestTolerance, 0f, 0f), new Location(TestCuboid.HalfWidth - TestTolerance, 0f, 0f)).Flipped));
		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, -TestCuboid.HalfHeight + TestTolerance, 0f), new Location(0f, TestCuboid.HalfHeight - TestTolerance, 0f)).Flipped));
		Assert.IsNull(TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, 0f, -TestCuboid.HalfDepth + TestTolerance), new Location(0f, 0f, TestCuboid.HalfDepth - TestTolerance)).Flipped));

		AssertToleranceEquals(new BoundedRay(new Location(TestCuboid.HalfWidth, 0f, 0f), new Location(TestCuboid.HalfWidth - 1f, -1f, 0f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(TestCuboid.HalfWidth, 0f, 0f), new Location(TestCuboid.HalfWidth - 1f, -1f, 0f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(TestCuboid.HalfWidth, 0f, 0f), new Location(TestCuboid.HalfWidth + 1f, 1f, 0f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f)).Flipped), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(TestCuboid.HalfWidth, 0f, 0f), new Location(TestCuboid.HalfWidth + 1f, 1f, 0f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(TestCuboid.HalfWidth - 1f, 1f, 0f), new Location(TestCuboid.HalfWidth + 1f, -1f, 0f)).Flipped), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestCuboid.HalfHeight, 0f), new Location(0f, TestCuboid.HalfHeight - 0.3f, -0.3f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 0.3f, 0.3f), new Location(0f, TestCuboid.HalfHeight + 0.3f, -0.3f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestCuboid.HalfHeight, 0f), new Location(0f, TestCuboid.HalfHeight - 0.3f, -0.3f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 0.3f, 0.3f), new Location(0f, TestCuboid.HalfHeight + 0.3f, -0.3f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestCuboid.HalfHeight, 0f), new Location(0f, TestCuboid.HalfHeight + 0.3f, 0.3f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 0.3f, 0.3f), new Location(0f, TestCuboid.HalfHeight + 0.3f, -0.3f)).Flipped), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestCuboid.HalfHeight, 0f), new Location(0f, TestCuboid.HalfHeight + 0.3f, 0.3f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(0f, TestCuboid.HalfHeight - 0.3f, 0.3f), new Location(0f, TestCuboid.HalfHeight + 0.3f, -0.3f)).Flipped), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 0f, TestCuboid.HalfDepth), new Location(-0.3f, 0f, TestCuboid.HalfDepth - 0.3f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(0.3f, 0f, TestCuboid.HalfDepth - 0.3f), new Location(-0.3f, 0f, TestCuboid.HalfDepth + 0.3f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 0f, TestCuboid.HalfDepth), new Location(-0.3f, 0f, TestCuboid.HalfDepth - 0.3f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(0.3f, 0f, TestCuboid.HalfDepth - 0.3f), new Location(-0.3f, 0f, TestCuboid.HalfDepth + 0.3f))), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 0f, TestCuboid.HalfDepth), new Location(0.3f, 0f, TestCuboid.HalfDepth + 0.3f)), TestCuboid.ReflectionOf(new BoundedRay(new Location(0.3f, 0f, TestCuboid.HalfDepth - 0.3f), new Location(-0.3f, 0f, TestCuboid.HalfDepth + 0.3f)).Flipped), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 0f, TestCuboid.HalfDepth), new Location(0.3f, 0f, TestCuboid.HalfDepth + 0.3f)), TestCuboid.FastReflectionOf(new BoundedRay(new Location(0.3f, 0f, TestCuboid.HalfDepth - 0.3f), new Location(-0.3f, 0f, TestCuboid.HalfDepth + 0.3f)).Flipped), TestTolerance);
	}
}