// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class CuboidTest {
	const float TestTolerance = 0.01f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area+of+cuboid+with+width+7.2+height+13.6+depth+1.4
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);
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
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new Cuboid(width: 7.2f * 3f, height: 13.6f * 3f, depth: 1.4f * 3f), 
			new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f).ScaledBy(3f), 
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateLocations() {
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		// 8 vertices
		AssertToleranceEquals(new(3.6f, 6.8f, 0.7f),	cuboid[Orientation3D.LeftUpForward],		TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, -0.7f),	cuboid[Orientation3D.LeftUpBackward],		TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0.7f),	cuboid[Orientation3D.LeftDownForward],		TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, -0.7f),	cuboid[Orientation3D.LeftDownBackward],		TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0.7f),	cuboid[Orientation3D.RightUpForward],		TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, -0.7f),	cuboid[Orientation3D.RightUpBackward],		TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0.7f),	cuboid[Orientation3D.RightDownForward],		TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, -0.7f),	cuboid[Orientation3D.RightDownBackward],	TestTolerance);

		// 6 side centre-points
		AssertToleranceEquals(new(3.6f, 0f, 0f),		cuboid[Orientation3D.Left],					TestTolerance);
		AssertToleranceEquals(new(-3.6f, 0f, 0f),		cuboid[Orientation3D.Right],				TestTolerance);
		AssertToleranceEquals(new(0f, 6.8f, 0f),		cuboid[Orientation3D.Up],					TestTolerance);
		AssertToleranceEquals(new(0f, -6.8f, 0f),		cuboid[Orientation3D.Down],					TestTolerance);
		AssertToleranceEquals(new(0f, 0f, 0.7f),		cuboid[Orientation3D.Forward],				TestTolerance);
		AssertToleranceEquals(new(0f, 0f, -0.7f),		cuboid[Orientation3D.Backward],				TestTolerance);

		// 12 edge centre-points
		AssertToleranceEquals(new(3.6f, 0f, 0.7f),		cuboid[Orientation3D.LeftForward],			TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, 0f),		cuboid[Orientation3D.LeftUp],				TestTolerance);
		AssertToleranceEquals(new(3.6f, 0f, -0.7f),		cuboid[Orientation3D.LeftBackward],			TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0f),		cuboid[Orientation3D.LeftDown],				TestTolerance);
		AssertToleranceEquals(new(-3.6f, 0f, 0.7f),		cuboid[Orientation3D.RightForward],			TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0f),		cuboid[Orientation3D.RightUp],				TestTolerance);
		AssertToleranceEquals(new(-3.6f, 0f, -0.7f),	cuboid[Orientation3D.RightBackward],		TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0f),	cuboid[Orientation3D.RightDown],			TestTolerance);
		AssertToleranceEquals(new(0f, 6.8f, 0.7f),		cuboid[Orientation3D.UpForward],			TestTolerance);
		AssertToleranceEquals(new(0f, 6.8f, -0.7f),		cuboid[Orientation3D.UpBackward],			TestTolerance);
		AssertToleranceEquals(new(0f, -6.8f, 0.7f),		cuboid[Orientation3D.DownForward],			TestTolerance);
		AssertToleranceEquals(new(0f, -6.8f, -0.7f),	cuboid[Orientation3D.DownBackward],			TestTolerance);

		// Centre
		AssertToleranceEquals(Location.Origin, cuboid[Orientation3D.None], TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReturnDimensionFromAxis() {
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);
		
		Assert.AreEqual(7.2f, cuboid[Axis.X], TestTolerance);
		Assert.AreEqual(13.6f, cuboid[Axis.Y], TestTolerance);
		Assert.AreEqual(1.4f, cuboid[Axis.Z], TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = cuboid[Axis.None]);
	}

	[Test]
	public void ShouldCorrectlyCalculateSideSurfaceAreas() {
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

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
		const string Expectation = "Cuboid[Width 7.2 | Height 13.6 | Depth 1.4]";
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(Expectation, cuboid.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		cuboid.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}
	
	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Cuboid[Width 7.2 | Height 13.6 | Depth 1.4]";
		var expectation = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);

		Assert.AreEqual(expectation, Cuboid.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Cuboid.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(expectation, result);
	}
	
	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		var cuboid = new Cuboid(width: 7.2f, height: 13.6f, depth: 1.4f);
		Assert.AreEqual(3, Cuboid.ConvertToSpan(cuboid).Length);
		Assert.AreEqual(7.2f / 2f, Cuboid.ConvertToSpan(cuboid)[0]);
		Assert.AreEqual(13.6f / 2f, Cuboid.ConvertToSpan(cuboid)[1]);
		Assert.AreEqual(1.4f / 2f, Cuboid.ConvertToSpan(cuboid)[2]);
		Assert.AreEqual(cuboid, Cuboid.ConvertFromSpan(Cuboid.ConvertToSpan(cuboid)));
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
			var val = Cuboid.CreateNewRandom(a, b);
			Assert.GreaterOrEqual(val.Width, a.Width);
			Assert.Less(val.Width, b.Width);
			Assert.GreaterOrEqual(val.Height, a.Height);
			Assert.Less(val.Height, b.Height);
			Assert.GreaterOrEqual(val.Depth, a.Depth);
			Assert.Less(val.Depth, b.Depth);

			val = Cuboid.CreateNewRandom();
			Assert.GreaterOrEqual(val.Width, Cuboid.DefaultRandomMin);
			Assert.Less(val.Width, Cuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.Height, Cuboid.DefaultRandomMin);
			Assert.Less(val.Height, Cuboid.DefaultRandomMax);
			Assert.GreaterOrEqual(val.Depth, Cuboid.DefaultRandomMin);
			Assert.Less(val.Depth, Cuboid.DefaultRandomMax);
		}
	}
}