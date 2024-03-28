// Created on 2024-03-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class LineInterfaceTest {
	const float TestTolerance = 0.0001f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateUnboundedIntersectionDistances() {
		void AssertPair(Location l1, Direction d1, Location l2, Direction d2, (float First, float Second)? expectation) {
			var thisLine = new Line(l1, d1);
			var otherLine = new Line(l2, d2);

			if (expectation == null) {
				Assert.IsNull(ILine.CalculateUnboundedIntersectionDistanceOnThisLine(thisLine, otherLine));
				Assert.IsNull(ILine.CalculateUnboundedIntersectionDistancesOnBothLines(thisLine, otherLine));
				Assert.IsNull(ILine.CalculateUnboundedIntersectionDistanceOnThisLine(otherLine, thisLine));
				Assert.IsNull(ILine.CalculateUnboundedIntersectionDistancesOnBothLines(otherLine, thisLine));
			}
			else {
				Assert.AreEqual(
					expectation.Value.First,
					ILine.CalculateUnboundedIntersectionDistanceOnThisLine(thisLine, otherLine),
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.Second,
					ILine.CalculateUnboundedIntersectionDistanceOnThisLine(otherLine, thisLine),
					TestTolerance
				);
				var bothActual = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(thisLine, otherLine)!;
				Assert.AreEqual(
					expectation.Value.First,
					bothActual.Value.ThisDistance,
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.Second,
					bothActual.Value.OtherDistance,
					TestTolerance
				);
				bothActual = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(otherLine, thisLine)!;
				Assert.AreEqual(
					expectation.Value.Second,
					bothActual.Value.ThisDistance,
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.First,
					bothActual.Value.OtherDistance,
					TestTolerance
				);
			}

			
		}

		AssertPair(
			(0f, 0f, 0f), Direction.Up,
			(0f, 0f, 0f), Direction.Right,
			(0f, 0f)
		);
		AssertPair(
			(0f, 10f, 0f), Direction.Up,
			(10f, 0f, 0f), Direction.Right,
			(-10f, 10f)
		);
		AssertPair(
			(0f, -10f, 0f), Direction.Up,
			(-10f, 0f, 0f), Direction.Right,
			(10f, -10f)
		);
		AssertPair(
			(1f, 1f, 0f), (-1f, -1f, 0f),
			(-1f, 1f, 0f), (1f, -1f, 0f),
			(MathF.Sqrt(2f), MathF.Sqrt(2f))
		);
		AssertPair(
			(4f, 4f, 0f), (-1f, -1f, 0f),
			(2f, -2f, 0f), (1f, -1f, 0f),
			(MathF.Sqrt(32f), -MathF.Sqrt(8f))
		);
		AssertPair(
			(0f, 0f, 0f), Direction.Right,
			(0f, 1f, 0f), Direction.Left,
			null
		);
		AssertPair(
			(1f, 1f, 1f), (1f, 1f, 1f),
			(3f, 3f, 3f), (-1f, -1f, -1f),
			null
		);
		AssertPair(
			Location.Origin, (1f, 1f, 1f),
			Location.Origin, (-1f, -1f, -1f),
			null
		);
		AssertPair(
			(1f, 1f, 1f), (1f, 1f, 1f),
			(1f, 3f, 1f), (-1f, -1f, -1f),
			null
		);
	}
}