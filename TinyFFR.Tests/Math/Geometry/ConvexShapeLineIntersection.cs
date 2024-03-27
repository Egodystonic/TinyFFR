// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class ConvexShapeLineIntersectionTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConstructFromTwoPotentiallyNullArgs() {
		Assert.AreEqual(
			new ConvexShapeLineIntersection(Location.Origin, null),
			ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(Location.Origin, null)
		);
		Assert.AreEqual(
			new ConvexShapeLineIntersection(Location.Origin, null),
			ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(null, Location.Origin)
		);

		Assert.AreEqual(
			new ConvexShapeLineIntersection(Location.Origin, Location.Origin),
			ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(Location.Origin, Location.Origin)
		);

		Assert.AreEqual(
			null,
			ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(null, null)
		);
	}
}