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
}