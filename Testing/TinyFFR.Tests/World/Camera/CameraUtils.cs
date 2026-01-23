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
		CameraUtils.CalculateProjectionMatrix(
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
}