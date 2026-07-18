// Created on 2026-07-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class CameraLockedObjectsTest {
	const float TestTolerance = 1E-3f;
	static readonly XYPair<float> TestSize = new(3f, 2f);
	static readonly Location TestPosition = new(1f, 2f, 3f);
	static readonly Location TestCameraPosition = new(-4f, 7f, 12f);

	static readonly Orientation2D[] AllAnchors = [
		Orientation2D.None, Orientation2D.Right, Orientation2D.UpRight, Orientation2D.Up, Orientation2D.UpLeft,
		Orientation2D.Left, Orientation2D.DownLeft, Orientation2D.Down, Orientation2D.DownRight
	];

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static Transform CreateCanonicalQuadTransform(Orientation2D anchor) {
		return QuadMesh.CalculateTransformForStandardQuadMesh(TestPosition, TestSize, Direction.Forward, Direction.Up, anchor);
	}

	[Test]
	public void AnchorOffsetHelperShouldMatchTransformCalculatorBaking() {
		foreach (var anchor in AllAnchors) {
			var facing = new Direction(1f, 1f, 0.5f);
			var transform = QuadMesh.CalculateTransformForStandardQuadMesh(TestPosition, TestSize, facing, null, anchor);
			var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, anchor);
			AssertToleranceEquals(TestPosition.AsVect() + offset * transform.Rotation, transform.Translation, TestTolerance);
		}
	}

	[Test]
	public void FreeBillboardShouldFaceCameraPosition() {
		foreach (var anchor in AllAnchors) {
			var curTransform = CreateCanonicalQuadTransform(anchor);
			var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, anchor);

			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, Direction.None, TestCameraPosition, Direction.Up, out var result));

			var expectedFacing = TestPosition.DirectionTo(TestCameraPosition);
			AssertToleranceEquals(expectedFacing, Direction.Forward * result.Rotation, TestTolerance);
			AssertToleranceEquals(curTransform.Scaling, result.Scaling, TestTolerance);
		}
	}

	[Test]
	public void TextCanonicalFacingShouldPointTowardCamera() {
		var curTransform = CreateCanonicalQuadTransform(Orientation2D.None);
		var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, Orientation2D.None);

		Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Backward, Direction.None, TestCameraPosition, Direction.Up, out var result));

		AssertToleranceEquals(TestPosition.DirectionTo(TestCameraPosition), Direction.Backward * result.Rotation, TestTolerance);
	}

	[Test]
	public void BillboardRotationShouldKeepAnchorPointStationary() {
		foreach (var anchor in AllAnchors) {
			var curTransform = CreateCanonicalQuadTransform(anchor);
			var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, anchor);

			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, Direction.None, TestCameraPosition, Direction.Up, out var result));
			AssertToleranceEquals(TestPosition.AsVect(), result.Translation - offset * result.Rotation, TestTolerance);

			// Second tick from a different camera position must still pivot about the same anchor point
			var secondCameraPosition = new Location(20f, -6f, 1f);
			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(result, offset, Direction.Forward, Direction.None, secondCameraPosition, Direction.Up, out var secondResult));
			AssertToleranceEquals(TestPosition.AsVect(), secondResult.Translation - offset * secondResult.Rotation, TestTolerance);
		}
	}

	[Test]
	public void AxisLockedBillboardShouldPreserveAxisAndFaceCameraAsCloselyAsPossible() {
		var lockedAxis = Direction.Up;
		foreach (var anchor in AllAnchors) {
			var curTransform = CreateCanonicalQuadTransform(anchor);
			var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, anchor);

			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, lockedAxis, TestCameraPosition, Direction.Up, out var result));

			AssertToleranceEquals(lockedAxis, Direction.Up * result.Rotation, TestTolerance);
			var expectedFacing = TestPosition.DirectionTo(TestCameraPosition).OrthogonalizedAgainst(lockedAxis);
			Assert.IsNotNull(expectedFacing);
			AssertToleranceEquals(expectedFacing.Value, Direction.Forward * result.Rotation, TestTolerance);
		}
	}

	[Test]
	public void ShouldSkipDegenerateConfigurations() {
		var curTransform = CreateCanonicalQuadTransform(Orientation2D.None);
		var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, Orientation2D.None);

		// Camera exactly at the anchor point
		Assert.IsFalse(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, Direction.None, TestPosition, Direction.Up, out var unchanged));
		AssertToleranceEquals(curTransform, unchanged, TestTolerance);

		// Camera directly along the locked axis
		var cameraAlongAxis = TestPosition + Direction.Up * 10f;
		Assert.IsFalse(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, Direction.Up, cameraAlongAxis, Direction.Up, out unchanged));
		AssertToleranceEquals(curTransform, unchanged, TestTolerance);
	}

	[Test]
	public void ShouldBeStableAcrossRepeatedTicks() {
		foreach (var anchor in AllAnchors) {
			var curTransform = CreateCanonicalQuadTransform(anchor);
			var offset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(TestSize, anchor);

			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(curTransform, offset, Direction.Forward, Direction.None, TestCameraPosition, Direction.Up, out var firstResult));
			Assert.IsTrue(LocalSceneBuilder.TryCalculateCameraLockedTransform(firstResult, offset, Direction.Forward, Direction.None, TestCameraPosition, Direction.Up, out var secondResult));
			AssertToleranceEquals(firstResult, secondResult, TestTolerance);
		}
	}
}
