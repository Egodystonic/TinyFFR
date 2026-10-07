// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Numerics;

namespace Egodystonic.TinyFFR.World.Camera;

[TestFixture]
class CameraUtilsTest {
	const float TestTolerance = 0.001f;
	
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProjectionMatrix() {
		CameraUtils.CalculatePerspectiveProjectionMatrix(
			nearPlaneDistance: 0.15f,
			farPlaneDistance: 5000f,
			verticalFov: 60f,
			aspectRatio: 1920f / 1080f,
			out var result
		);

		AssertToleranceEquals(
			new Matrix4x4(0.974f, 0f, 0f, 0f, 0f, 1.732f, 0f, 0f, 0f, 0f, -1f, -1f, 0f, 0f, -0.3f, 0f),
			result,
			TestTolerance
		);
		
		CameraUtils.CalculateOrthographicProjectionMatrix(
			nearPlaneDistance: 0.15f,
			farPlaneDistance: 5000f,
			orthographicHeight: 2f,
			aspectRatio: 1920f / 1080f,
			out result
		);

		AssertToleranceEquals(
			new Matrix4x4(0.563f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, -1f, 1f),
			result,
			TestTolerance
		);
	}
	
	[Test]
	public void ShouldCorrectlyCalculateModelMatrix() {
		CameraUtils.CalculateModelMatrix(
			position: new Location(1f, 2f, 3f),
			viewDirection: new Direction(-1f, -2f, -3f),
			upDirection: new Direction(-1f, 2f, 3f),
			out var result
		);

		AssertToleranceEquals(
			new Matrix4x4(0f, 0.429f, -0.286f, 0f, -0.496f, 0.0763f, 0.115f, 0f, 0.267f, 0.535f, 0.802f, 0f, 1f, 2f, 3f, 1f),
			result,
			TestTolerance
		);
	}
	
	[Test]
	public void ShouldCorrectlyCalculateViewMatrix() {
		CameraUtils.CalculateModelMatrix(
			position: new Location(1f, 2f, 3f),
			viewDirection: new Direction(-1f, -2f, -3f),
			upDirection: new Direction(-1f, 2f, 3f),
			out var modelMat
		);

		CameraUtils.CalculateViewMatrix(
			position: new Location(1f, 2f, 3f),
			viewDirection: new Direction(-1f, -2f, -3f),
			upDirection: new Direction(-1f, 2f, 3f),
			out var result
		);

		Matrix4x4.Invert(modelMat, out var expectation);
		AssertToleranceEquals(
			expectation,
			result,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateCameraRelativeOrientationDirection() {
		static Direction Calc(Orientation o, Direction view, Direction up) => CameraUtils.CalculateCameraRelativeOrientationDirection(o, view, up);

		var view = Direction.Forward;
		var up = Direction.Up;
		AssertToleranceEquals(Direction.Left, Calc(Orientation.Left, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Right, Calc(Orientation.Right, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Up, Calc(Orientation.Up, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Down, Calc(Orientation.Down, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Calc(Orientation.Forward, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Backward, Calc(Orientation.Backward, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 1f, 0f), Calc(Orientation.LeftUp, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, -1f, 0f), Calc(Orientation.RightDown, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(0f, 1f, 1f), Calc(Orientation.UpForward, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 1f, 1f), Calc(Orientation.LeftUpForward, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, -1f, -1f), Calc(Orientation.RightDownBackward, view, up), TestTolerance);

		view = Direction.Up;
		up = Direction.Backward;
		AssertToleranceEquals(Direction.Up, Calc(Orientation.Forward, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Down, Calc(Orientation.Backward, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Backward, Calc(Orientation.Up, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Calc(Orientation.Down, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Left, Calc(Orientation.Left, view, up), TestTolerance);
		AssertToleranceEquals(Direction.Right, Calc(Orientation.Right, view, up), TestTolerance);

		view = new Direction(-1f, -2f, -3f);
		up = new Direction(-1f, 2f, 3f);
		AssertToleranceEquals(view, Calc(Orientation.Forward, view, up), TestTolerance);
		AssertToleranceEquals(-view, Calc(Orientation.Backward, view, up), TestTolerance);
		AssertToleranceEquals(up, Calc(Orientation.Up, view, up), TestTolerance);
		AssertToleranceEquals(-up, Calc(Orientation.Down, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(0f, -6f, 4f), Calc(Orientation.Left, view, up), TestTolerance);
		AssertToleranceEquals(new Direction(0f, 6f, -4f), Calc(Orientation.Right, view, up), TestTolerance);

		Assert.AreEqual(Direction.None, Calc(Orientation.None, Direction.Forward, Direction.Up));
		Assert.AreEqual(Direction.None, Calc(Orientation.None, view, up));
	}

	[Test]
	public void ShouldCorrectlyCalculateViewportWorldSize() {
		var ortho = CameraUtils.CalculateOrthographicViewportWorldSize(orthographicHeight: 9f, aspectRatio: 16f / 9f);
		AssertToleranceEquals(16f, ortho.X, TestTolerance);
		AssertToleranceEquals(9f, ortho.Y, TestTolerance);

		var square = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(90f, 90f, 10f);
		AssertToleranceEquals(20f, square.X, TestTolerance);
		AssertToleranceEquals(20f, square.Y, TestTolerance);

		var atOne = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(90f, 60f, 1f);
		AssertToleranceEquals(2f, atOne.X, TestTolerance);
		AssertToleranceEquals(2f * MathF.Tan(MathF.PI / 6f), atOne.Y, TestTolerance);
		var atThree = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(90f, 60f, 3f);
		AssertToleranceEquals(atOne.X * 3f, atThree.X, TestTolerance);
		AssertToleranceEquals(atOne.Y * 3f, atThree.Y, TestTolerance);

		var viaTangents = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistanceFromFovTangents(
			MathF.Tan(MathF.PI * 0.25f),
			MathF.Tan(MathF.PI / 6f),
			3f
		);
		AssertToleranceEquals(atThree.X, viaTangents.X, TestTolerance);
		AssertToleranceEquals(atThree.Y, viaTangents.Y, TestTolerance);

		var atZero = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(90f, 60f, 0f);
		AssertToleranceEquals(0f, atZero.X, TestTolerance);
		AssertToleranceEquals(0f, atZero.Y, TestTolerance);
		var behind = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(90f, 60f, -5f);
		AssertToleranceEquals(0f, behind.X, TestTolerance);
		AssertToleranceEquals(0f, behind.Y, TestTolerance);

		const float VerticalFov = 60f;
		const float AspectRatio = 16f / 9f;
		const float Depth = 7f;
		var horizontalFov = Angle.FromRadians(2f * MathF.Atan(AspectRatio * MathF.Tan(Angle.FromDegrees(VerticalFov).Radians * 0.5f)));

		static Ray EdgeRay(float ndcX) => CameraUtils.CreateRayFromPerspectiveCameraParameters(
			cameraPosition: Location.Origin,
			cameraViewDirection: Direction.Forward,
			cameraUpDirection: Direction.Up,
			nearPlaneDistance: 0.1f,
			farPlaneDistance: 1000f,
			verticalFov: VerticalFov,
			aspectRatio: AspectRatio,
			normalizedNearPlaneCoordinate: new XYPair<float>(ndcX, 0f)
		);

		static Location PointAtDepth(Ray r, float depth) {
			var startDepth = r.StartPoint.AsVect().Dot(Direction.Forward);
			return r.UnboundedLocationAtDistance((depth - startDepth) / r.Direction.Dot(Direction.Forward));
		}

		var span = PointAtDepth(EdgeRay(-1f), Depth).DistanceFrom(PointAtDepth(EdgeRay(1f), Depth));
		var expected = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance(horizontalFov, VerticalFov, Depth);
		AssertToleranceEquals(span, expected.X, TestTolerance);
	}

	static void AssertNearPlaneCoordEquals(XYPair<float> expected, XYPair<float>? actual) {
		Assert.IsTrue(actual.HasValue, $"Expected {expected} but was null.");
		AssertToleranceEquals(expected.X, actual!.Value.X, TestTolerance);
		AssertToleranceEquals(expected.Y, actual.Value.Y, TestTolerance);
	}

	[Test]
	public void ShouldRoundTripNearPlaneProjectionWithRayCreation() {
		var random = new Random(1234);
		float Next(float min, float max) => min + (float) random.NextDouble() * (max - min);

		for (var i = 0; i < 500; ++i) {
			var position = new Location(Next(-10f, 10f), Next(-10f, 10f), Next(-10f, 10f));
			var view = Direction.Random();
			var up = view.AnyOrthogonal();
			var near = Next(0.05f, 1f);
			var far = Next(100f, 1000f);
			var aspect = Next(0.5f, 3f);
			var coord = new XYPair<float>(Next(-0.99f, 0.99f), Next(-0.99f, 0.99f));
			var distance = Next(1f, 50f);
			var fov = Angle.FromDegrees(Next(30f, 110f));
			var orthoHeight = Next(1f, 20f);

			var perspectiveLocation = CameraUtils.CreateRayFromPerspectiveCameraParameters(position, view, up, near, far, fov, aspect, coord).UnboundedLocationAtDistance(distance);
			CameraUtils.CalculateModelMatrix(position, view, up, out var modelMat);
			CameraUtils.CalculatePerspectiveProjectionMatrix(near, far, fov, aspect, out var perspectiveMat);
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToPerspectiveCameraNearPlane(position, view, up, near, far, fov, aspect, perspectiveLocation));
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToPerspectiveCameraNearPlane(in modelMat, in perspectiveMat, perspectiveLocation));
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToPerspectiveCameraNearPlaneClamped(position, view, up, near, far, fov, aspect, perspectiveLocation, out var wasClamped));
			Assert.IsFalse(wasClamped);
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToPerspectiveCameraNearPlaneClamped(in modelMat, in perspectiveMat, perspectiveLocation));

			var orthographicLocation = CameraUtils.CreateRayFromOrthographicCameraParameters(position, view, up, near, far, orthoHeight, aspect, coord).UnboundedLocationAtDistance(distance);
			CameraUtils.CalculateOrthographicProjectionMatrix(near, far, orthoHeight, aspect, out var orthographicMat);
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToOrthographicCameraNearPlane(position, view, up, near, far, orthoHeight, aspect, orthographicLocation));
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToOrthographicCameraNearPlane(in modelMat, in orthographicMat, orthographicLocation));
			AssertNearPlaneCoordEquals(coord, CameraUtils.ProjectOnToOrthographicCameraNearPlaneClamped(position, view, up, near, far, orthoHeight, aspect, orthographicLocation, out wasClamped));
			Assert.IsFalse(wasClamped);
		}
	}

	[Test]
	public void ShouldReturnNullForLocationsOutsideCameraView() {
		XYPair<float>? Perspective(Location l) => CameraUtils.ProjectOnToPerspectiveCameraNearPlane(Location.Origin, Direction.Forward, Direction.Up, 0.1f, 1000f, 60f, 16f / 9f, l);
		XYPair<float>? Orthographic(Location l) => CameraUtils.ProjectOnToOrthographicCameraNearPlane(Location.Origin, Direction.Forward, Direction.Up, 0.1f, 1000f, 4f, 16f / 9f, l);

		Assert.IsNull(Perspective(Location.Origin + Direction.Backward * 5f));
		Assert.IsNull(Perspective(Location.Origin + Direction.Forward * 5f + Direction.Right * 100f));
		Assert.IsNull(Perspective(Location.Origin + Direction.Forward * 5f + Direction.Down * 100f));
		Assert.IsNull(Orthographic(Location.Origin + Direction.Backward * 5f));
		Assert.IsNull(Orthographic(Location.Origin + Direction.Forward * 5f + Direction.Right * 4f));

		AssertNearPlaneCoordEquals(new XYPair<float>(0f, 0f), Perspective(Location.Origin + Direction.Forward * 5f));
		AssertNearPlaneCoordEquals(new XYPair<float>(0f, 0f), Orthographic(Location.Origin + Direction.Forward * 5f));
		Assert.Greater(Perspective(Location.Origin + Direction.Forward * 5f + Direction.Right * 1f)!.Value.X, 0f);
		Assert.Greater(Perspective(Location.Origin + Direction.Forward * 5f + Direction.Up * 1f)!.Value.Y, 0f);
		AssertNearPlaneCoordEquals(new XYPair<float>(0.5f / (2f * 16f / 9f) * 2f, 0f), Orthographic(Location.Origin + Direction.Forward * 5f + Direction.Right * 1f));
	}

	[Test]
	public void ShouldClampLocationsOutsideCameraViewRadiallyToEdge() {
		XYPair<float> Perspective(Location l, out bool c) => CameraUtils.ProjectOnToPerspectiveCameraNearPlaneClamped(Location.Origin, Direction.Forward, Direction.Up, 0.1f, 1000f, 60f, 16f / 9f, l, out c);
		XYPair<float> Orthographic(Location l, out bool c) => CameraUtils.ProjectOnToOrthographicCameraNearPlaneClamped(Location.Origin, Direction.Forward, Direction.Up, 0.1f, 1000f, 4f, 16f / 9f, l, out c);

		var result = Perspective(Location.Origin + Direction.Forward * 5f + Direction.Right * 0.1f, out var wasClamped);
		Assert.IsFalse(wasClamped);
		Assert.Less(result.X, 1f);

		result = Perspective(Location.Origin + Direction.Forward * 5f + Direction.Right * 100f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(1f, 0f), result);

		result = Perspective(Location.Origin + Direction.Forward * 5f + Direction.Right * 100f + Direction.Up * 10f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertToleranceEquals(1f, result.X, TestTolerance);
		Assert.Greater(result.Y, 0f);
		Assert.Less(result.Y, 1f);

		result = Perspective(Location.Origin + Direction.Forward * 5f + Direction.Up * 100f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(0f, 1f), result);

		result = Perspective(Location.Origin + Direction.Backward * 5f + Direction.Right * 3f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(1f, 0f), result);

		result = Perspective(Location.Origin + Direction.Backward * 5f + Direction.Left * 0.01f + Direction.Down * 3f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertToleranceEquals(-1f, result.Y, TestTolerance);
		Assert.Less(result.X, 0f);

		result = Perspective(Location.Origin + Direction.Backward * 5f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(0f, -1f), result);

		result = Orthographic(Location.Origin + Direction.Forward * 5f + Direction.Left * 100f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(-1f, 0f), result);

		result = Orthographic(Location.Origin + Direction.Backward * 5f + Direction.Right * 1f, out wasClamped);
		Assert.IsTrue(wasClamped);
		AssertNearPlaneCoordEquals(new XYPair<float>(1f, 0f), result);
	}
}