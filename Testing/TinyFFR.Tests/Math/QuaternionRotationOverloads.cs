// Created on 2026-07-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;

namespace Egodystonic.TinyFFR;

// Every quaternion-accepting rotation overload must produce the same result as its Rotation-accepting counterpart.
[TestFixture]
class QuaternionRotationOverloadsTest {
	const float TestTolerance = 0.001f;

	static readonly Rotation[] TestRotations = {
		Rotation.None,
		90f % Direction.Up,
		-90f % Direction.Up,
		37f % Direction.Left,
		180f % Direction.Forward,
		123f % new Direction(1f, 2f, -3f),
		-45f % new Direction(-4f, 1f, 2f)
	};

	static readonly Location TestPivot = new(3f, -2f, 7f);

	static IEnumerable<(Rotation Rotation, Quaternion Quaternion)> RotationPairs() {
		foreach (var rot in TestRotations) yield return (rot, rot.ToQuaternion());
	}

	[Test]
	public void VectAndDirectionShouldMatch() {
		var vect = new Vect(1f, -2f, 3f);
		var dir = new Direction(2f, 3f, -1f);

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(vect.RotatedBy(rot), vect.RotatedBy(quat), TestTolerance);
			AssertToleranceEquals(dir.RotatedBy(rot), dir.RotatedBy(quat), TestTolerance);
		}
	}

	[Test]
	public void LocationShouldMatch() {
		var loc = new Location(4f, -1f, 2f);

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(loc.RotatedAroundOriginBy(rot), loc.RotatedAroundOriginBy(quat), TestTolerance);
			AssertToleranceEquals(loc.RotatedBy(rot, TestPivot), loc.RotatedBy(quat, TestPivot), TestTolerance);
			AssertToleranceEquals(
				((IRotatable<Location>) loc).RotatedBy(rot),
				((IRotatable<Location>) loc).RotatedBy(quat),
				TestTolerance
			);
		}
	}

	[Test]
	public void TransformShouldMatch() {
		var transform = new Transform(new Vect(1f, 2f, 3f), 40f % Direction.Left, new Vect(2f, 2f, 2f));

		foreach (var (rot, quat) in RotationPairs()) {
			var expected = ((IRotatable<Transform>) transform).RotatedBy(rot);
			var actual = ((IRotatable<Transform>) transform).RotatedBy(quat);
			AssertToleranceEquals(expected.Translation, actual.Translation, TestTolerance);
			AssertToleranceEquals(expected.Scaling, actual.Scaling, TestTolerance);
			Assert.IsTrue(expected.Rotation.IsEquivalentForAllDirectionsTo(actual.Rotation, TestTolerance));
		}
	}

	[Test]
	public void LineShouldMatch() {
		var line = new Line(new Location(1f, 2f, 3f), new Direction(1f, 1f, 0f));

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(line.RotatedBy(rot), line.RotatedBy(quat), TestTolerance);
			AssertToleranceEquals(line.RotatedAroundOriginBy(rot), line.RotatedAroundOriginBy(quat), TestTolerance);
			AssertToleranceEquals(line.RotatedBy(rot, TestPivot), line.RotatedBy(quat, TestPivot), TestTolerance);
			AssertToleranceEquals(line.RotatedBy(rot, 2.5f), line.RotatedBy(quat, 2.5f), TestTolerance);
		}
	}

	[Test]
	public void RayShouldMatch() {
		var ray = new Ray(new Location(-1f, 0f, 4f), new Direction(0f, 1f, 1f));

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(ray.RotatedBy(rot), ray.RotatedBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedAroundOriginBy(rot), ray.RotatedAroundOriginBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedBy(rot, TestPivot), ray.RotatedBy(quat, TestPivot), TestTolerance);
			AssertToleranceEquals(ray.RotatedBy(rot, 2.5f), ray.RotatedBy(quat, 2.5f), TestTolerance);
		}
	}

	[Test]
	public void BoundedRayShouldMatch() {
		var ray = new BoundedRay(new Location(1f, 1f, 1f), new Location(4f, -2f, 3f));

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(ray.RotatedAroundStartBy(rot), ray.RotatedAroundStartBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedAroundEndBy(rot), ray.RotatedAroundEndBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedAroundMiddleBy(rot), ray.RotatedAroundMiddleBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedAroundOriginBy(rot), ray.RotatedAroundOriginBy(quat), TestTolerance);
			AssertToleranceEquals(ray.RotatedBy(rot, TestPivot), ray.RotatedBy(quat, TestPivot), TestTolerance);
			AssertToleranceEquals(ray.RotatedBy(rot, 2.5f), ray.RotatedBy(quat, 2.5f), TestTolerance);
			AssertToleranceEquals(
				((IRotatable<BoundedRay>) ray).RotatedBy(rot),
				((IRotatable<BoundedRay>) ray).RotatedBy(quat),
				TestTolerance
			);
		}
	}

	[Test]
	public void PlaneShouldMatch() {
		var plane = new Plane(new Direction(1f, 2f, 3f), new Location(0f, 4f, 0f));

		foreach (var (rot, quat) in RotationPairs()) {
			AssertToleranceEquals(plane.RotatedAroundOriginBy(rot), plane.RotatedAroundOriginBy(quat), TestTolerance);
			AssertToleranceEquals(plane.RotatedBy(rot, TestPivot), plane.RotatedBy(quat, TestPivot), TestTolerance);
			AssertToleranceEquals(
				((IRotatable<Plane>) plane).RotatedBy(rot),
				((IRotatable<Plane>) plane).RotatedBy(quat),
				TestTolerance
			);
		}
	}

	[Test]
	public void PositionedRotatedCuboidShouldMatch() {
		var cuboid = new PositionedRotatedCuboid(2f, 3f, 4f, new Location(1f, 1f, 1f), 25f % Direction.Up);

		foreach (var (rot, quat) in RotationPairs()) {
			var expected = cuboid.RotatedBy(rot);
			var actual = cuboid.RotatedBy(quat);
			AssertToleranceEquals(expected.Position, actual.Position, TestTolerance);
			Assert.IsTrue(expected.Rotation.IsEquivalentForAllDirectionsTo(actual.Rotation, TestTolerance));
		}
	}

	[Test]
	public void RotationCombinationShouldMatch() {
		var start = 31f % new Direction(1f, -2f, 4f);

		foreach (var (rot, quat) in RotationPairs()) {
			var expected = start.CombinedAndNormalizedWith(rot);
			var actual = start.CombinedAndNormalizedWith(quat);
			Assert.IsTrue(expected.IsEquivalentForAllDirectionsTo(actual, TestTolerance));
		}
	}
}
