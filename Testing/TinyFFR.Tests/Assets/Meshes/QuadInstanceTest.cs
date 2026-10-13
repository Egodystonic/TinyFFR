// Created on 2026-10-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class QuadInstanceTest {
	const float TestTolerance = 0.001f;
	static readonly Location TestPosition = new(1f, 2f, 3f);
	static readonly Location TestNewPosition = new(-4f, 0.5f, 7f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static void AssertLocation(Location expected, Location actual, string message) {
		Assert.IsTrue(expected.Equals(actual, TestTolerance), $"{message}: expected {expected}, was {actual}");
	}

	[Test]
	public void CameraLockedQuadPositionShouldBeAnchorPoint() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateQuad();
		using var material = factory.MaterialBuilder.CreateTestMaterial();

		foreach (var scalingMode in new[] { CameraLockedScalingMode.Standard, CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio }) {
			foreach (var anchor in Enum.GetValues<Orientation2D>()) {
				var label = $"{scalingMode}/{anchor}";
				using var quad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 0.5f), positionAnchor: anchor, scalingMode: scalingMode);
				AssertLocation(TestPosition, quad.Position, label + " at creation");
				AssertLocation(TestPosition, ((SceneObject) quad).Position, label + " via SceneObject");

				quad.Position = TestNewPosition;
				AssertLocation(TestNewPosition, quad.Position, label + " after setting Position");

				quad.Scaling = new XYPair<float>(3f, 1.5f);
				AssertLocation(TestNewPosition, quad.Position, label + " after setting Scaling");
				Assert.AreEqual(new XYPair<float>(3f, 1.5f), quad.Scaling);
				quad.ScaleBy(2f);
				AssertLocation(TestNewPosition, quad.Position, label + " after ScaleBy");
				quad.AdjustScaleBy(-0.5f);
				AssertLocation(TestNewPosition, quad.Position, label + " after AdjustScaleBy");
				quad.MoveBy(new Vect(1f, 0f, 0f));
				AssertLocation(TestNewPosition + new Vect(1f, 0f, 0f), quad.Position, label + " after MoveBy");
			}
		}
	}

	[Test]
	public void CameraLockedQuadAnchorShouldMatchUnderlyingCornerPlacement() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateQuad();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 1f), positionAnchor: Orientation2D.None);
		AssertLocation(TestPosition, quad.UnderlyingQuadInstance.Position, "centred quad's underlying position");

		using var anchored = factory.ObjectBuilder.CreateCameraLockedQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 1f), positionAnchor: Orientation2D.Up);
		AssertLocation(TestPosition, anchored.Position, "anchored quad's position");
		Assert.AreEqual(0.5f, anchored.Position.DistanceFrom(anchored.UnderlyingQuadInstance.Position), TestTolerance);
	}

	[Test]
	public void PositionShouldBeAnchorPoint() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateQuad();
		using var material = factory.MaterialBuilder.CreateTestMaterial();

		foreach (var anchor in Enum.GetValues<Orientation2D>()) {
			var label = anchor.ToString();
			using var quad = factory.ObjectBuilder.CreateQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 0.5f), positionAnchor: anchor);
			Assert.AreEqual(anchor, quad.PositionAnchor);
			AssertLocation(TestPosition, quad.Position, label + " at creation");
			AssertLocation(TestPosition, ((SceneObject) quad).Position, label + " via SceneObject");

			quad.Position = TestNewPosition;
			AssertLocation(TestNewPosition, quad.Position, label + " after setting Position");

			quad.Scaling = new XYPair<float>(3f, 1.5f);
			AssertLocation(TestNewPosition, quad.Position, label + " after setting Scaling");
			Assert.AreEqual(new XYPair<float>(3f, 1.5f), quad.Scaling);
			quad.ScaleBy(2f);
			AssertLocation(TestNewPosition, quad.Position, label + " after ScaleBy");
			quad.AdjustScaleBy(-0.5f);
			AssertLocation(TestNewPosition, quad.Position, label + " after AdjustScaleBy");

			quad.Rotation = 30f % Direction.Up;
			AssertLocation(TestNewPosition, quad.Position, label + " after setting Rotation");
			quad.RotateBy(45f % Direction.Forward);
			AssertLocation(TestNewPosition, quad.Position, label + " after RotateBy");

			quad.MoveBy(new Vect(1f, 0f, 0f));
			AssertLocation(TestNewPosition + new Vect(1f, 0f, 0f), quad.Position, label + " after MoveBy");

			quad.SetTransform(TestPosition, new XYPair<float>(1f, 1f), Direction.Up, Direction.Forward, Orientation2D.None);
			Assert.AreEqual(Orientation2D.None, quad.PositionAnchor);
			AssertLocation(TestPosition, quad.Position, label + " after SetTransform with no anchor");
			AssertLocation(TestPosition, quad.Transform.Translation.AsLocation(), label + " centre after SetTransform with no anchor");

			quad.SetTransform(TestNewPosition, new XYPair<float>(1f, 1f), Direction.Backward, null, anchor);
			Assert.AreEqual(anchor, quad.PositionAnchor);
			AssertLocation(TestNewPosition, quad.Position, label + " after SetTransform with anchor");
		}
	}

	[Test]
	public void AnchoredPositionShouldBeOffsetFromCentre() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateQuad();
		using var material = factory.MaterialBuilder.CreateTestMaterial();

		using var quad = factory.ObjectBuilder.CreateQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 1f), positionAnchor: Orientation2D.DownLeft);
		AssertLocation(TestPosition, quad.Position, "anchored position");
		Assert.AreEqual(MathF.Sqrt(1f + 0.25f), quad.Position.DistanceFrom(quad.Transform.Translation.AsLocation()), TestTolerance);

		using var configured = factory.ObjectBuilder.CreateQuadInstance(mesh, material, new ModelInstanceCreationConfig { InitialTransform = QuadMesh.CalculateTransformForStandardQuadMesh(TestPosition, new XYPair<float>(2f, 1f), Direction.Backward, null, Orientation2D.Up) }, Orientation2D.Up);
		Assert.AreEqual(Orientation2D.Up, configured.PositionAnchor);
		AssertLocation(TestPosition, configured.Position, "config overload position");
	}

	[Test]
	public void CameraLockedQuadShouldUseOwnAnchorOverAnchoredQuad() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateQuad();
		using var material = factory.MaterialBuilder.CreateTestMaterial();

		using var quad = factory.ObjectBuilder.CreateQuadInstance(mesh, material, position: TestPosition, size: new XYPair<float>(2f, 1f), positionAnchor: Orientation2D.DownLeft);
		var centre = quad.Transform.Translation.AsLocation();
		var locked = CameraLockedQuadInstance.FromPreviouslyAllocatedUnderlyingQuadInstance(quad, Direction.None, Orientation2D.None, CameraLockedScalingMode.Standard, CameraLockStyle.FaceCameraPlane);
		AssertLocation(centre, locked.Position, "camera-locked position with no lock anchor");

		locked.Position = TestNewPosition;
		AssertLocation(TestNewPosition, locked.Position, "camera-locked position after setting");
		AssertLocation(TestNewPosition, quad.Transform.Translation.AsLocation(), "underlying centre after setting camera-locked position");
	}
	
	[Test]
	public void PlacementShouldKeepAnchorFixedAcrossFramesAndDistances() {
		var planeFacingDirection = Direction.Backward;
		var anchorPoint = new Location(0.5f, 0.2f, 5f);
		var storedScaling = new XYPair<float>(0.2f, 0.1f);

		foreach (var lockStyle in Enum.GetValues<CameraLockStyle>()) {
			foreach (var upright in new[] { Direction.None, Direction.Up }) {
				foreach (var anchor in Enum.GetValues<Orientation2D>()) {
					var label = $"{lockStyle}/{upright}/{anchor}";
					var stored = QuadMesh.CalculateTransformForStandardQuadMesh(anchorPoint, storedScaling, Direction.Backward, Direction.Up, anchor);
					var storedOffset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(storedScaling, anchor);

					foreach (var worldMultiplier in new[] { 1f, 4f, 11.5f, 0.3f, 7f }) {
						var cameraPosition = new Location(0f, 0f, 5f - worldMultiplier);
						var worldScaling = new XYPair<float>(storedScaling.X * worldMultiplier, storedScaling.Y * worldMultiplier);
						var worldOffset = QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(worldScaling, anchor);

						LocalSceneBuilder.CalculateCameraLockedTransforms(
							in stored,
							new Vect(worldScaling.X, worldScaling.Y, 1f),
							storedOffset,
							worldOffset,
							lockStyle,
							upright,
							cameraPosition,
							Direction.Up,
							planeFacingDirection,
							out var newStored,
							out var world
						);

						var storedAnchor = (newStored.Translation - Rotation.Rotate(storedOffset, newStored.RotationQuaternion)).AsLocation();
						var worldAnchor = (world.Translation - Rotation.Rotate(worldOffset, world.RotationQuaternion)).AsLocation();
						AssertLocation(anchorPoint, storedAnchor, label + $" stored anchor at x{worldMultiplier}");
						AssertLocation(anchorPoint, worldAnchor, label + $" drawn anchor at x{worldMultiplier}");
						Assert.AreEqual(newStored.RotationQuaternion, world.RotationQuaternion);
						Assert.AreEqual(new Vect(storedScaling.X, storedScaling.Y, 1f), newStored.Scaling);

						if (lockStyle == CameraLockStyle.FaceCameraPosition && upright == Direction.None) {
							var facing = Rotation.Rotate(Direction.Backward, world.RotationQuaternion);
							Assert.IsTrue(facing.Equals(anchorPoint.DirectionTo(cameraPosition), TestTolerance), label + " should face camera position");
						}
						stored = newStored;
					}
				}
			}
		}
	}
}
