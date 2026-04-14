// Created on 2026-04-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class TranslatedAndRotatedShapeTest {
	const float TestTolerance = 0.01f;
	static readonly Cuboid TestCuboid = new(4f, 6f, 2f);
	static readonly Rotation TestRotation = new(90f, Direction.Up);
	static readonly TranslatedAndRotatedConvexShape<Cuboid> TestShape = new(TestCuboid, new Vect(1f, -2f, 3f), TestRotation);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(TestCuboid, TestShape.BaseShape);
		Assert.AreEqual(new Vect(1f, -2f, 3f), TestShape.Translation);
		Assert.AreEqual(TestRotation, TestShape.Rotation);
	}

	[Test]
	public void ShouldCorrectlyTransformBetweenWorldAndShapeSpace() {
		AssertToleranceEquals(Location.Origin, TestShape.TransformToShapeSpace(new Location(1f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -2f, 3f), TestShape.TransformToWorldSpace(Location.Origin), TestTolerance);	
		AssertToleranceEquals(new Location(0f, 0f, 1f), TestShape.TransformToShapeSpace(new Location(2f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -2f, 1f), TestShape.TransformToWorldSpace(new Location(2f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(-2f, 0f, 0f), TestShape.TransformToShapeSpace(new Location(1f, -2f, 5f)), TestTolerance);
		AssertToleranceEquals(new Location(1.5f, -3f, 4f), TestShape.TransformToWorldSpace(TestShape.TransformToShapeSpace(new Location(1.5f, -3f, 4f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDeterminePhysicalValidity() {
		Assert.AreEqual(true, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), Vect.Zero, Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(true, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), Vect.Zero, TestRotation).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(-1f, 1f, 1f), Vect.Zero, Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(0f), Vect.Zero, Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(Single.NaN), Vect.Zero, Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(Single.PositiveInfinity), Vect.Zero, Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), new Vect(Single.NaN, 0f, 0f), Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), new Vect(0f, Single.PositiveInfinity, 0f), Rotation.None).IsPhysicallyValid);
		Assert.AreEqual(false, new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), Vect.Zero, new Rotation(Single.NaN, Direction.Up)).IsPhysicallyValid);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Cuboid[Width 4.0 | Height 6.0 | Depth 2.0] rotated by 90.0° around <0.0, 1.0, 0.0> @ <1.0, -2.0, 3.0>";
		Assert.AreEqual(Expectation, TestShape.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestShape.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Cuboid[Width 4.0 | Height 6.0 | Depth 2.0] rotated by 90.0° around <0.0, 1.0, 0.0> @ <1.0, -2.0, 3.0>";
		var parsed = TranslatedAndRotatedConvexShape<Cuboid>.Parse(Input, CultureInfo.InvariantCulture);
		AssertToleranceEquals(TestShape.BaseShape, parsed.BaseShape, TestTolerance);
		AssertToleranceEquals(TestShape.Translation, parsed.Translation, TestTolerance);
		AssertToleranceEquals(TestShape.Rotation, parsed.Rotation, TestTolerance);
		Assert.AreEqual(true, TranslatedAndRotatedConvexShape<Cuboid>.TryParse(Input, CultureInfo.InvariantCulture, out parsed));
		AssertToleranceEquals(TestShape.BaseShape, parsed.BaseShape, TestTolerance);
		AssertToleranceEquals(TestShape.Translation, parsed.Translation, TestTolerance);
		AssertToleranceEquals(TestShape.Rotation, parsed.Rotation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestShape);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestShape);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(
			TestShape,
			TestShape.BaseShape.Width, TestShape.BaseShape.Height, TestShape.BaseShape.Depth,
			TestShape.Translation.X, TestShape.Translation.Y, TestShape.Translation.Z,
			TestRotation.Axis.X, TestRotation.Axis.Y, TestRotation.Axis.Z, TestRotation.Angle.Radians
		);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(2f, 4f, 2f), new Vect(0f, 0f, 0f), Rotation.None);
		var end = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(6f, 8f, 4f), new Vect(10f, 10f, 10f), new Rotation(180f, Direction.Up));

		var mid = TranslatedAndRotatedConvexShape<Cuboid>.Interpolate(start, end, 0.5f);
		AssertToleranceEquals(new Cuboid(4f, 6f, 3f), mid.BaseShape, TestTolerance);
		AssertToleranceEquals(new Vect(5f, 5f, 5f), mid.Translation, TestTolerance);

		AssertToleranceEquals(start.BaseShape, TranslatedAndRotatedConvexShape<Cuboid>.Interpolate(start, end, 0f).BaseShape, TestTolerance);
		AssertToleranceEquals(start.Translation, TranslatedAndRotatedConvexShape<Cuboid>.Interpolate(start, end, 0f).Translation, TestTolerance);
		AssertToleranceEquals(end.BaseShape, TranslatedAndRotatedConvexShape<Cuboid>.Interpolate(start, end, 1f).BaseShape, TestTolerance);
		AssertToleranceEquals(end.Translation, TranslatedAndRotatedConvexShape<Cuboid>.Interpolate(start, end, 1f).Translation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(2f), new Vect(-5f, -5f, -5f), new Rotation(10f, Direction.Up));
		var max = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(8f), new Vect(5f, 5f, 5f), new Rotation(100f, Direction.Up));
		var mid = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(4f), new Vect(0f, 0f, 0f), new Rotation(50f, Direction.Up));
		var midClamped = mid.Clamp(min, max);
		AssertToleranceEquals(mid.BaseShape, midClamped.BaseShape, TestTolerance);
		AssertToleranceEquals(mid.Translation, midClamped.Translation, TestTolerance);
		AssertToleranceEquals(mid.Rotation, midClamped.Rotation, TestTolerance);

		var aboveMax = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(10f), new Vect(10f, 10f, 10f), new Rotation(200f, Direction.Up));
		var aboveClamped = aboveMax.Clamp(min, max);
		AssertToleranceEquals(max.BaseShape, aboveClamped.BaseShape, TestTolerance);
		AssertToleranceEquals(max.Translation, aboveClamped.Translation, TestTolerance);
		AssertToleranceEquals(max.Rotation, aboveClamped.Rotation, TestTolerance);

		var belowMin = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(1f), new Vect(-10f, -10f, -10f), new Rotation(0f, Direction.Up));
		var belowClamped = belowMin.Clamp(min, max);
		AssertToleranceEquals(min.BaseShape, belowClamped.BaseShape, TestTolerance);
		AssertToleranceEquals(min.Translation, belowClamped.Translation, TestTolerance);
		AssertToleranceEquals(min.Rotation, belowClamped.Rotation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var min = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(2f), new Vect(-10f, -10f, -10f), new Rotation(10f, Direction.Up));
		var max = new TranslatedAndRotatedConvexShape<Cuboid>(new Cuboid(8f), new Vect(10f, 10f, 10f), new Rotation(100f, Direction.Up));
		for (var i = 0; i < NumIterations; ++i) {
			var val = TranslatedAndRotatedConvexShape<Cuboid>.Random(min, max);
			Assert.GreaterOrEqual(val.BaseShape.HalfWidth, 1f);
			Assert.Less(val.BaseShape.HalfWidth, 4f);
			Assert.GreaterOrEqual(val.Translation.X, -10f);
			Assert.Less(val.Translation.X, 10f);
			Assert.GreaterOrEqual(val.Translation.Y, -10f);
			Assert.Less(val.Translation.Y, 10f);
			Assert.GreaterOrEqual(val.Translation.Z, -10f);
			Assert.Less(val.Translation.Z, 10f);
		}
	}

	[Test]
	public void ShouldCorrectlyScale() {
		var scaled = TestShape.ScaledBy(3f);
		AssertToleranceEquals(TestCuboid * 3f, scaled.BaseShape, TestTolerance);
		Assert.AreEqual(TestShape.Translation, scaled.Translation);
		Assert.AreEqual(TestShape.Rotation, scaled.Rotation);
	}

	[Test]
	public void ShouldCorrectlyRotate() {
		var additionalRotation = new Rotation(45f, Direction.Forward);

		var rotatedByMethod = TestShape.RotatedBy(additionalRotation);
		var rotatedByOpRight = TestShape * additionalRotation;
		var rotatedByOpLeft = additionalRotation * TestShape;

		Assert.AreEqual(rotatedByMethod, rotatedByOpRight);
		Assert.AreEqual(rotatedByMethod, rotatedByOpLeft);

		Assert.AreEqual(TestShape.BaseShape, rotatedByMethod.BaseShape);
		Assert.AreEqual(TestShape.Translation, rotatedByMethod.Translation);
		AssertToleranceEquals(TestRotation + additionalRotation, rotatedByMethod.Rotation, TestTolerance);
		AssertToleranceEquals(new Direction(0.5f, 0.5f, 0f), Direction.Forward * rotatedByMethod.Rotation, TestTolerance);

		AssertToleranceEquals(TestShape.Rotation, TestShape.RotatedBy(Rotation.None).Rotation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyMoveByVect() {
		var moveVect = new Vect(5f, 10f, -3f);

		var moved = TestShape.MovedBy(moveVect);
		Assert.AreEqual(TestShape.BaseShape, moved.BaseShape);
		Assert.AreEqual(TestShape.Rotation, moved.Rotation);
		AssertToleranceEquals(TestShape.Translation.Plus(moveVect), moved.Translation, TestTolerance);

		Assert.AreEqual(moved, TestShape + moveVect);
		Assert.AreEqual(moved, moveVect + TestShape);

		var movedBack = moved - moveVect;
		AssertToleranceEquals(TestShape.Translation, movedBack.Translation, TestTolerance);
		Assert.AreEqual(TestShape.BaseShape, movedBack.BaseShape);
		Assert.AreEqual(TestShape.Rotation, movedBack.Rotation);
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherLocationIsContained() {
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -2f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1.5f, -3f, 4f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1.99f, -2f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, 0.99f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -2f, 4.99f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(0.01f, -2f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -4.99f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -2f, 1.01f)));
		
		Assert.AreEqual(false, TestShape.Contains(new Location(2.01f, -2f, 3f)));
		Assert.AreEqual(false, TestShape.Contains(new Location(1f, 1.01f, 3f)));
		Assert.AreEqual(false, TestShape.Contains(new Location(1f, -2f, 0.99f)));
		Assert.AreEqual(false, TestShape.Contains(new Location(10f, 10f, 10f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfBoundedRay() {
		Assert.AreEqual(true, TestShape.Contains(new BoundedRay(new Location(1f, -2f, 3f), Direction.Up * 1f)));
		Assert.AreEqual(true, TestShape.Contains(new BoundedRay(new Location(0.5f, -4f, 2f), new Location(1.5f, 0f, 4f))));
		Assert.AreEqual(false, TestShape.Contains(new BoundedRay(new Location(1f, -2f, 3f), Direction.Up * 5f)));
		Assert.AreEqual(false, TestShape.Contains(new BoundedRay(new Location(5f, 5f, 5f), Direction.Right * 1f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Location(1f, -2f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Location(1f, 0f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Location(1f, 1f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Location(2f, -2f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Location(1f, -2f, 5f)));
		
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Location(1f, 2f, 3f)), TestTolerance);
		AssertToleranceEquals(2f, TestShape.DistanceFrom(new Location(1f, -7f, 3f)), TestTolerance);
		AssertToleranceEquals(2f, TestShape.DistanceFrom(new Location(4f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(2f, TestShape.DistanceFrom(new Location(1f, -2f, 7f)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Location(1f, -2f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceSquaredFromLocations() {
		Assert.AreEqual(0f, TestShape.DistanceSquaredFrom(new Location(1f, -2f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceSquaredFrom(new Location(1f, 0f, 3f)));
		AssertToleranceEquals(1f, TestShape.DistanceSquaredFrom(new Location(1f, 2f, 3f)), TestTolerance);
		AssertToleranceEquals(4f, TestShape.DistanceSquaredFrom(new Location(4f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(4f, TestShape.DistanceSquaredFrom(new Location(1f, -7f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		AssertToleranceEquals(1f, TestShape.SurfaceDistanceFrom(new Location(1f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(new Location(1f, 1f, 3f)), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(new Location(2f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.SurfaceDistanceFrom(new Location(1f, 2f, 3f)), TestTolerance);
		AssertToleranceEquals(2f, TestShape.SurfaceDistanceFrom(new Location(4f, -2f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		AssertToleranceEquals(new Location(1f, -2f, 3f), TestShape.PointClosestTo(new Location(1f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1.5f, -3f, 4f), TestShape.PointClosestTo(new Location(1.5f, -3f, 4f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 1f, 3f), TestShape.PointClosestTo(new Location(1f, 2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(2f, -2f, 3f), TestShape.PointClosestTo(new Location(4f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -2f, 5f), TestShape.PointClosestTo(new Location(1f, -2f, 7f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -2f, 3f), TestShape.PointClosestTo(new Location(-1f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -2f, 1f), TestShape.PointClosestTo(new Location(1f, -2f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(2f, 1f, 5f), TestShape.PointClosestTo(new Location(4f, 2f, 7f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToGivenLocation() {
		AssertToleranceEquals(new Location(1f, 1f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, 1f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 1f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, 2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(2f, 1f, 5f), TestShape.SurfacePointClosestTo(new Location(4f, 2f, 7f)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.SurfacePointClosestTo(new Location(1f, -2f, 3f)).DistanceFrom(TestShape.Translation.AsLocation()), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, -2f, 3f), Direction.Up)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, -2f, 3f), Direction.Right)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, -2f, 3f), Direction.Forward)));
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Line((1f, 2f, 3f), Direction.Forward)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Line((1f, -6f, 3f), Direction.Forward)), TestTolerance);

		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, 5f, 3f), Direction.Down)));
		AssertToleranceEquals(4f, TestShape.DistanceFrom(new Ray((1f, 5f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((5f, -2f, 3f), Direction.Right)));
		AssertToleranceEquals(3f, TestShape.DistanceFrom(new Ray((5f, -2f, 3f), Direction.Left)), TestTolerance);
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay(new Location(0.5f, -3f, 2f), new Location(1.5f, 0f, 4f))));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f)));
		AssertToleranceEquals(3f, TestShape.DistanceFrom(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 1f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(new Line((1f, -2f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.SurfaceDistanceFrom(new Line((1f, 2f, 3f), Direction.Forward)), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, 5f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals(4f, TestShape.SurfaceDistanceFrom(new Ray((1f, 5f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f)), TestTolerance);
		AssertToleranceEquals(1f, TestShape.SurfaceDistanceFrom(new BoundedRay(new Location(1f, -2f, 2f), new Location(1f, -3f, 2f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLine() {
		AssertToleranceEquals(new Location(1f, -2f, 3f), TestShape.PointClosestTo(new Line((1f, -2f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals(2f, TestShape.PointClosestTo(new Line((4f, -2f, 3f), Direction.Up)).X, TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(TestShape.PointClosestTo(new Line((4f, -2f, 3f), Direction.Up))), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(TestShape.PointClosestTo(new Ray((1f, 5f, 3f), Direction.Down))), TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(TestShape.PointClosestTo(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 100f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLine() {
		AssertToleranceEquals(new Location(1f, -2f, 3f), TestShape.ClosestPointOn(new Line((1f, -2f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals(4f, TestShape.ClosestPointOn(new Line((4f, -2f, 3f), Direction.Up)).X, TestTolerance);
		AssertToleranceEquals(3f, TestShape.ClosestPointOn(new Line((4f, -2f, 3f), Direction.Up)).Z, TestTolerance);
		AssertToleranceEquals(0f, TestShape.SurfaceDistanceFrom(TestShape.ClosestPointOn(new Ray((1f, 5f, 3f), Direction.Down))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToLine() {
		var result = TestShape.SurfacePointClosestTo(new Line((1f, -2f, 3f), Direction.Up));
		Assert.IsTrue(result.Equals((1f, 1f, 3f), TestTolerance) || result.Equals((1f, -5f, 3f), TestTolerance));
		
		result = TestShape.SurfacePointClosestTo(new Line((4f, -2f, 3f), Direction.Up));
		AssertToleranceEquals(2f, result.X, TestTolerance);

		result = TestShape.SurfacePointClosestTo(new Ray((1f, 5f, 3f), Direction.Down));
		AssertToleranceEquals(1f, result.Y, TestTolerance);
		
		result = TestShape.SurfacePointClosestTo(new BoundedRay(new Location(1f, -4.2f, 2f), new Location(1f, -3f, 2f)));
		AssertToleranceEquals(new Location(1f, -5f, 2f), result, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLineToSurface() {
		var result = TestShape.ClosestPointToSurfaceOn(new Line((1f, -2f, 3f), Direction.Up));
		Assert.IsTrue(result.Equals((1f, 1f, 3f), TestTolerance) || result.Equals((1f, -5f, 3f), TestTolerance));

		AssertToleranceEquals(4f, TestShape.ClosestPointToSurfaceOn(new Line((4f, -2f, 3f), Direction.Up)).X, TestTolerance);
		AssertToleranceEquals(3f, TestShape.ClosestPointToSurfaceOn(new Line((4f, -2f, 3f), Direction.Up)).Z, TestTolerance);
		
		result = TestShape.ClosestPointToSurfaceOn(new BoundedRay(new Location(1f, -4.2f, 2f), new Location(1f, -3f, 2f)));
		AssertToleranceEquals(new Location(1f, -4.2f, 2f), result, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		Assert.IsTrue(TestShape.IsIntersectedBy(new Line((1f, -2f, 3f), Direction.Up)));
		Assert.IsTrue(TestShape.IsIntersectedBy(new Line((1f, -2f, 3f), Direction.Right)));
		Assert.IsTrue(TestShape.IsIntersectedBy(new Line((1f, -2f, 3f), Direction.Forward)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new Line((1f, 3f, 3f), Direction.Right)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new Line((5f, 5f, 5f), Direction.Up)));

		Assert.IsTrue(TestShape.IsIntersectedBy(new Ray((1f, 5f, 3f), Direction.Down)));
		Assert.IsTrue(TestShape.IsIntersectedBy(new Ray((5f, -2f, 3f), Direction.Right)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new Ray((1f, 5f, 3f), Direction.Up)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new Ray((5f, -2f, 3f), Direction.Left)));

		Assert.IsTrue(TestShape.IsIntersectedBy(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 3f)));
		Assert.IsFalse(TestShape.IsIntersectedBy(new BoundedRay(new Location(5f, 5f, 5f), Direction.Right * 100f)));
	}

	[Test]
	public void ShouldCorrectlyFindLineIntersections() {
		ConvexShapeLineIntersection result;

		result = TestShape.IntersectionWith(new Line((1f, -2f, 3f), Direction.Right))!.Value;
		AssertToleranceEquals(new Location(2f, -2f, 3f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(0f, -2f, 3f), result.Second!.Value, TestTolerance);

		result = TestShape.IntersectionWith(new Line((1f, -2f, 3f), Direction.Up))!.Value;
		AssertToleranceEquals(new Location(1f, -5f, 3f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.Second!.Value, TestTolerance);

		result = TestShape.IntersectionWith(new Line((1f, -2f, 3f), Direction.Forward))!.Value;
		AssertToleranceEquals(new Location(1f, -2f, 1f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(1f, -2f, 5f), result.Second!.Value, TestTolerance);

		Assert.IsFalse(TestShape.IntersectionWith(new Line((1f, 3f, 3f), Direction.Right)).HasValue);

		result = TestShape.IntersectionWith(new Ray((1f, 5f, 3f), Direction.Down))!.Value;
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(1f, -5f, 3f), result.Second!.Value, TestTolerance);

		Assert.IsFalse(TestShape.IntersectionWith(new Ray((1f, 5f, 3f), Direction.Up)).HasValue);

		result = TestShape.IntersectionWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 20f))!.Value;
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(1f, -5f, 3f), result.Second!.Value, TestTolerance);

		Assert.IsFalse(TestShape.IntersectionWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 3f)).HasValue);
		
		result = TestShape.IntersectionWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 5f))!.Value;
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.First, TestTolerance);
		Assert.IsFalse(result.Second.HasValue);

		result = TestShape.FastIntersectionWith(new Line((1f, -2f, 3f), Direction.Right));
		AssertToleranceEquals(new Location(2f, -2f, 3f), result.First, TestTolerance);
		AssertToleranceEquals(new Location(0f, -2f, 3f), result.Second!.Value, TestTolerance);

		result = TestShape.FastIntersectionWith(new Ray((1f, 5f, 3f), Direction.Down));
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.First, TestTolerance);

		result = TestShape.FastIntersectionWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 20f));
		AssertToleranceEquals(new Location(1f, 1f, 3f), result.First, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, 1f, 3f))));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -5f, 3f))));

		AssertToleranceEquals(9f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, 10f, 3f))), TestTolerance);
		AssertToleranceEquals(9f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -14f, 3f))), TestTolerance);
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Plane(Direction.Left, (-1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals(1f, TestShape.DistanceFrom(new Plane(Direction.Left, (3f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlanes() {
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, 1f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -5f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, 10f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -14f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestShape.RelationshipTo(new Plane(Direction.Right, (3f, 0f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestShape.RelationshipTo(new Plane(Direction.Right, (-1f, 0f, 0f))));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlanes() {
		AssertToleranceEquals(0f, TestShape.DistanceFrom(TestShape.PointClosestTo(new Plane(Direction.Up, (1f, -2f, 3f)))), TestTolerance);

		AssertToleranceEquals(1f, TestShape.PointClosestTo(new Plane(Direction.Up, (1f, 10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(0f, TestShape.DistanceFrom(TestShape.PointClosestTo(new Plane(Direction.Up, (1f, 10f, 3f)))), TestTolerance);

		AssertToleranceEquals(2f, TestShape.PointClosestTo(new Plane(Direction.Left, (5f, 0f, 0f))).X, TestTolerance);
		AssertToleranceEquals(0f, TestShape.DistanceFrom(TestShape.PointClosestTo(new Plane(Direction.Left, (5f, 0f, 0f)))), TestTolerance);

		AssertToleranceEquals(5f, TestShape.PointClosestTo(new Plane(Direction.Forward, (1f, -2f, 8f))).Z, TestTolerance);
		AssertToleranceEquals(0f, TestShape.DistanceFrom(TestShape.PointClosestTo(new Plane(Direction.Forward, (1f, -2f, 8f)))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlanes() {
		AssertToleranceEquals(0f, TestShape.DistanceFrom(TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, -2f, 3f)))), TestTolerance);

		AssertToleranceEquals(10f, TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, 10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(0f, new Plane(Direction.Up, (1f, 10f, 3f)).DistanceFrom(TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, 10f, 3f)))), TestTolerance);

		AssertToleranceEquals(5f, TestShape.ClosestPointOn(new Plane(Direction.Left, (5f, 0f, 0f))).X, TestTolerance);
		AssertToleranceEquals(0f, new Plane(Direction.Left, (5f, 0f, 0f)).DistanceFrom(TestShape.ClosestPointOn(new Plane(Direction.Left, (5f, 0f, 0f)))), TestTolerance);

		AssertToleranceEquals(8f, TestShape.ClosestPointOn(new Plane(Direction.Forward, (1f, -2f, 8f))).Z, TestTolerance);
		AssertToleranceEquals(0f, new Plane(Direction.Left, (5f, 0f, 0f)).DistanceFrom(TestShape.ClosestPointOn(new Plane(Direction.Left, (5f, 0f, 0f)))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToPlanes() {
		AssertToleranceEquals(1f, TestShape.SurfacePointClosestTo(new Plane(Direction.Up, (1f, 10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(2f, TestShape.SurfacePointClosestTo(new Plane(Direction.Left, (5f, 0f, 0f))).X, TestTolerance);
		AssertToleranceEquals(-5f, TestShape.SurfacePointClosestTo(new Plane(Direction.Down, (1f, -10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(1f, TestShape.SurfacePointClosestTo(new Plane(Direction.Backward, (1f, -2f, -2f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnPlanes() {
		AssertToleranceEquals(10f, TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Up, (1f, 10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(5f, TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Left, (5f, 0f, 0f))).X, TestTolerance);
		AssertToleranceEquals(-10f, TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Down, (1f, -10f, 3f))).Y, TestTolerance);
		AssertToleranceEquals(-2f, TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Backward, (1f, -2f, -2f))).Z, TestTolerance);
	}


	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleWithLines() {
		const float LocalTestTolerance = 0.5f;

		AssertToleranceEquals(Angle.Zero, TestShape.IncidentAngleWith(new Ray((1f, 5f, 3f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.FastIncidentAngleWith(new Ray((1f, 5f, 3f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.IncidentAngleWith(new Ray((5f, -2f, 3f), Direction.Right)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.FastIncidentAngleWith(new Ray((5f, -2f, 3f), Direction.Right)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.IncidentAngleWith(new Ray((1f, -2f, 8f), Direction.Backward)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.FastIncidentAngleWith(new Ray((1f, -2f, 8f), Direction.Backward)), LocalTestTolerance);
		Assert.IsNull(TestShape.IncidentAngleWith(new Ray((1f, 5f, 3f), Direction.Up)));

		AssertToleranceEquals(Angle.Zero, TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f)), LocalTestTolerance);
		AssertToleranceEquals(Angle.Zero, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f)), LocalTestTolerance);

		Assert.IsNull(TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 3f)));
		
		AssertToleranceEquals(Angle.EighthCircle, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, 1.1f, 3f), new Direction(1f, -1f, 0f) * 10f)), LocalTestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectLines() {
		const float LocalTestTolerance = 0.5f;

		var reflection = TestShape.ReflectionOf(new Ray((1f, 5f, 3f), Direction.Down));
		Assert.IsNotNull(reflection);
		AssertToleranceEquals(new Location(1f, 1f, 3f), reflection!.Value.StartPoint, LocalTestTolerance);
		AssertToleranceEquals(Direction.Up, reflection.Value.Direction, LocalTestTolerance);

		reflection = TestShape.ReflectionOf(new Ray((5f, -2f, 3f), Direction.Right));
		Assert.IsNotNull(reflection);
		AssertToleranceEquals(new Location(2f, -2f, 3f), reflection!.Value.StartPoint, LocalTestTolerance);
		AssertToleranceEquals(Direction.Left, reflection.Value.Direction, LocalTestTolerance);

		reflection = TestShape.ReflectionOf(new Ray((1f, -2f, 8f), Direction.Backward));
		Assert.IsNotNull(reflection);
		AssertToleranceEquals(new Location(1f, -2f, 5f), reflection!.Value.StartPoint, LocalTestTolerance);
		AssertToleranceEquals(Direction.Forward, reflection.Value.Direction, LocalTestTolerance);

		reflection = TestShape.ReflectionOf(new Ray((1f, -2f, -2f), Direction.Forward));
		Assert.IsNotNull(reflection);
		AssertToleranceEquals(new Location(1f, -2f, 1f), reflection!.Value.StartPoint, LocalTestTolerance);
		AssertToleranceEquals(Direction.Backward, reflection.Value.Direction, LocalTestTolerance);

		Assert.IsNull(TestShape.ReflectionOf(new Ray((1f, 5f, 3f), Direction.Up)));

		var fastReflection = TestShape.FastReflectionOf(new Ray((1f, 5f, 3f), Direction.Down));
		AssertToleranceEquals(new Location(1f, 1f, 3f), fastReflection.StartPoint, LocalTestTolerance);
		AssertToleranceEquals(Direction.Up, fastReflection.Direction, LocalTestTolerance);

		var boundedReflection = TestShape.ReflectionOf(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 10f));
		Assert.IsNotNull(boundedReflection);
		AssertToleranceEquals(new Location(1f, 1f, 3f), boundedReflection!.Value.StartPoint, LocalTestTolerance);

		Assert.IsNull(TestShape.ReflectionOf(new BoundedRay(new Location(1f, 5f, 3f), Direction.Down * 3f)));
	}
}
