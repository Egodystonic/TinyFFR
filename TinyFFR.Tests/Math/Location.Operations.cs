// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class LocationTest {
	[Test]
	public void ShouldCorrectlyCombineWithVect() {
		void AssertCombination(Location loc, Vect vec, Location expectedAdditiveResult, Location expectedSubtractiveResult) {
			AssertToleranceEquals(expectedAdditiveResult, loc + vec, TestTolerance);
			AssertToleranceEquals(loc + vec, vec + loc, TestTolerance);
			AssertToleranceEquals(vec + loc, loc.MovedBy(vec), TestTolerance);
			AssertToleranceEquals(expectedSubtractiveResult, loc - vec, TestTolerance);
			AssertToleranceEquals(loc - vec, loc.MovedBy(-vec), TestTolerance);
		}

		AssertCombination(Location.Origin, Vect.Zero, Location.Origin, Location.Origin);
		AssertCombination(Location.Origin, new(1f, -2f, 3.5f), new(1f, -2f, 3.5f), new(-1f, 2f, -3.5f));
		AssertCombination(OneTwoNegThree, Vect.Zero, OneTwoNegThree, OneTwoNegThree);
		AssertCombination(OneTwoNegThree, new(0.1f, -0.2f, 0.3f), new(1.1f, 1.8f, -2.7f), new(0.9f, 2.2f, -3.3f));
	}

	[Test]
	public void ShouldCorrectlyCreateVectsBetweenLocations() {
		void AssertCombination(Location startPoint, Location endPoint, Vect fromStartToEndExpectation) {
			AssertToleranceEquals(fromStartToEndExpectation, startPoint >> endPoint, TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint << endPoint, TestTolerance);
			AssertToleranceEquals(-(startPoint >> endPoint), endPoint >> startPoint, TestTolerance);
			AssertToleranceEquals(-(endPoint >> startPoint), startPoint >> endPoint, TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, endPoint - startPoint, TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint - endPoint, TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, startPoint.GetVectTo(endPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, endPoint.GetVectTo(startPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint.GetVectFrom(endPoint), TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, endPoint.GetVectFrom(startPoint), TestTolerance);
		}

		AssertCombination(Location.Origin, Location.Origin, Vect.Zero);
		AssertCombination(Location.Origin, OneTwoNegThree, new(1f, 2f, -3f));
		AssertCombination(OneTwoNegThree, Location.Origin, new(-1f, -2f, 3f));
		AssertCombination(new(0.5f, -14f, 7.6f), new(9.2f, 17f, -0.1f), new(8.7f, 31f, -7.7f));
	}

	[Test]
	public void ShouldCorrectlyReturnDirectionBetweenLocations() {
		void AssertCombination(Location startPoint, Location endPoint, Direction fromStartToEndExpectation) {
			AssertToleranceEquals(fromStartToEndExpectation, startPoint.GetDirectionTo(endPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint.GetDirectionFrom(endPoint), TestTolerance);
		}

		AssertCombination(Location.Origin, Location.Origin, Direction.None);
		AssertCombination(Location.Origin, OneTwoNegThree, new(1f, 2f, -3f));
		AssertCombination(OneTwoNegThree, Location.Origin, new(-1f, -2f, 3f));
		AssertCombination(new(0.5f, -14f, 7.6f), new(9.2f, 17f, -0.1f), new(8.7f, 31f, -7.7f));
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(OneTwoNegThree, Location.Interpolate(OneTwoNegThree, Location.Origin, 0f), TestTolerance); 
		AssertToleranceEquals(Location.Origin, Location.Interpolate(OneTwoNegThree, Location.Origin, 1f), TestTolerance); 
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * 0.5f), Location.Interpolate(OneTwoNegThree, Location.Origin, 0.5f), TestTolerance); 
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * 2f), Location.Interpolate(OneTwoNegThree, Location.Origin, -1f), TestTolerance); 
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * -1f), Location.Interpolate(OneTwoNegThree, Location.Origin, 2f), TestTolerance); 

		var testList = new List<Location>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];

				for (var f = -1f; f <= 2f; f += 0.1f) {
					AssertToleranceEquals(new(Single.Lerp(start.X, end.X, f), Single.Lerp(start.Y, end.Y, f), Single.Lerp(start.Z, end.Z, f)), Location.Interpolate(start, end, f), TestTolerance);
				}
			}
		}
	}
}