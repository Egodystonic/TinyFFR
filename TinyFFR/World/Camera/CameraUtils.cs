// Created on 2026-01-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Static helpers for the calculations a <see cref="Camera"/> performs, exposed so they can be used without a camera resource.
/// </summary>
/// <remarks>
/// Everyday use of a camera does not require any of this; these are for interoperating with other graphics code, for computing camera maths off the main thread, or
/// for working out what a camera <i>would</i> see without creating one.
/// </remarks>
public static class CameraUtils {
	/// <summary>
	/// Converts a direction expressed relative to a camera in to a direction in the world.
	/// </summary>
	/// <remarks>
	/// The calculation behind <see cref="Camera.GetRelativeOrientationDirection"/>.
	/// </remarks>
	/// <param name="orientation">The camera-relative orientation to convert.</param>
	/// <param name="viewDirection">Which way the camera is looking.</param>
	/// <param name="upDirection">Which way is "up" for the camera.</param>
	public static Direction CalculateCameraRelativeOrientationDirection(Orientation orientation, Direction viewDirection, Direction upDirection) {
		var posZ = viewDirection;
		var posY = upDirection;
		var posX = Direction.FromDualOrthogonalization(posY, posZ);
		
		return (posX * orientation.GetAxisSign(Axis.X) + posY * orientation.GetAxisSign(Axis.Y) + posZ * orientation.GetAxisSign(Axis.Z)).Direction;
	}
	
	/// <summary>
	/// Calculates the projection matrix a perspective camera with the given parameters would use.
	/// </summary>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="verticalFov">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="dest">When this method returns, contains the calculated matrix.</param>
	public static void CalculatePerspectiveProjectionMatrix(float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, out Matrix4x4 dest) {
		var frustumLength = farPlaneDistance - nearPlaneDistance;
		var h = MathF.Tan(verticalFov.Radians * 0.5f) * nearPlaneDistance;
		var w = h * aspectRatio;

		dest = new Matrix4x4(
			nearPlaneDistance / w,					0f,											0f,																	0f,
			0f,										nearPlaneDistance / h,						0f,																	0f,
			0f,										0f,											-(farPlaneDistance + nearPlaneDistance) / frustumLength,			-1f,
			0f,										0f,											-(2f * farPlaneDistance * nearPlaneDistance) / frustumLength,		0f
		);
	}
	
	/// <summary>
	/// Calculates the projection matrix an orthographic camera with the given parameters would use.
	/// </summary>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="dest">When this method returns, contains the calculated matrix.</param>
	public static void CalculateOrthographicProjectionMatrix(float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, out Matrix4x4 dest) {
		var frustumLength = farPlaneDistance - nearPlaneDistance;

		dest = new Matrix4x4(
			2f / (orthographicHeight * aspectRatio),			0f,									0f,																	0f,
			0f,													2f / orthographicHeight,			0f,																	0f,
			0f,													0f,									-2f / frustumLength,												0f,
			0f,													0f,									-(farPlaneDistance + nearPlaneDistance) / frustumLength,			1f
		);
	}

	/// <summary>
	/// Calculates how large an area of the world an orthographic camera sees, in metres.
	/// </summary>
	/// <remarks>
	/// Because an orthographic camera does not converge, this is the same at every distance.
	/// </remarks>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<float> CalculateOrthographicViewportWorldSize(float orthographicHeight, float aspectRatio) {
		return new XYPair<float>(orthographicHeight * aspectRatio, orthographicHeight);
	}

	/// <summary>
	/// Calculates how large an area of the world a perspective camera sees at a given distance in front of it, in metres.
	/// </summary>
	/// <remarks>
	/// Useful for sizing something so that it exactly fills the frame at a chosen distance.
	/// </remarks>
	/// <param name="horizontalFieldOfView">How wide an angle of the scene the camera takes in from side to side.</param>
	/// <param name="verticalFieldOfView">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="distanceAlongViewDirection">How far in front of the camera to measure, in metres.</param>
	public static XYPair<float> CalculatePerspectiveViewportWorldSizeAtDistance(Angle horizontalFieldOfView, Angle verticalFieldOfView, float distanceAlongViewDirection) {
		return CalculatePerspectiveViewportWorldSizeAtDistanceFromFovTangents(
			MathF.Tan(horizontalFieldOfView.Radians * 0.5f),
			MathF.Tan(verticalFieldOfView.Radians * 0.5f),
			distanceAlongViewDirection
		);
	}

	/// <summary>
	/// Calculates how large an area of the world a perspective camera sees at a given distance, from pre-computed field-of-view tangents.
	/// </summary>
	/// <remarks>
	/// Equivalent to <see cref="CalculatePerspectiveViewportWorldSizeAtDistance"/>, but skipping the trigonometry when the tangents are already to hand. This is worth using when calculating this every frame.
	/// </remarks>
	/// <param name="halfHorizontalFieldOfViewTangent">The tangent of half the horizontal field of view.</param>
	/// <param name="halfVerticalFieldOfViewTangent">The tangent of half the vertical field of view.</param>
	/// <param name="distanceAlongViewDirection">How far in front of the camera to measure, in metres.</param>
	public static XYPair<float> CalculatePerspectiveViewportWorldSizeAtDistanceFromFovTangents(float halfHorizontalFieldOfViewTangent, float halfVerticalFieldOfViewTangent, float distanceAlongViewDirection) {
		var doubleDistance = MathF.Max(distanceAlongViewDirection, 0f) * 2f;
		return new XYPair<float>(doubleDistance * halfHorizontalFieldOfViewTangent, doubleDistance * halfVerticalFieldOfViewTangent);
	}

	/// <summary>
	/// Calculates the model matrix a camera with the given placement would use; i.e. the matrix describing the camera as an object in the world.
	/// </summary>
	/// <param name="position">Where the camera is.</param>
	/// <param name="viewDirection">Which way the camera is looking.</param>
	/// <param name="upDirection">Which way is "up" for the camera.</param>
	/// <param name="dest">When this method returns, contains the calculated matrix.</param>
	public static void CalculateModelMatrix(Location position, Direction viewDirection, Direction upDirection, out Matrix4x4 dest) {
		var p = position.ToVector3();
		var z = viewDirection.ToVector3();
		var x = Vector3.Cross(z, upDirection.ToVector3());
		var y = Vector3.Cross(x, z);
		z = -z;

		dest = new Matrix4x4(
			m11: x.X, m12: x.Y, m13: x.Z,
			m21: y.X, m22: y.Y, m23: y.Z,
			m31: z.X, m32: z.Y, m33: z.Z,
			m41: p.X,
			m42: p.Y,
			m43: p.Z,
			m14: 0f,
			m24: 0f,
			m34: 0f,
			m44: 1f
		);
	}
	
	/// <summary>
	/// Calculates the view matrix a camera with the given placement would use; i.e. the matrix transforming the world in to the frame of reference of the camera.
	/// </summary>
	/// <param name="position">Where the camera is.</param>
	/// <param name="viewDirection">Which way the camera is looking.</param>
	/// <param name="upDirection">Which way is "up" for the camera.</param>
	/// <param name="dest">When this method returns, contains the calculated matrix.</param>
	public static void CalculateViewMatrix(Location position, Direction viewDirection, Direction upDirection, out Matrix4x4 dest) {
		CalculateModelMatrix(position, viewDirection, upDirection, out var modelMat);
		Matrix4x4.Invert(modelMat, out dest);
	}
	
	// Maintainer's note:
	// I believe marking these as "in" may actually be counterproductive to performance as Matrix4x4 is mutable and 
	// this forces the compiler to make a defensive copy. However I'm keeping them here as I'm hoping the compiler
	// (either today or in the future) can prove that the inputted arguments are not mutated and therefore elide the copy 
	/// <summary>
	/// Calculates the ray travelling out from a point on the near plane of a perspective camera, given its matrices.
	/// </summary>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="normalizedNearPlaneCoordinate">Where on the near plane the ray should start, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges.</param>
	/// <seealso cref="ProjectOnToPerspectiveCameraNearPlane(in Matrix4x4, in Matrix4x4, Location)"/>
	public static Ray CreateRayFromPerspectiveCameraParameters(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, XYPair<float> normalizedNearPlaneCoordinate) {
		var nearPlaneNdcVect = new Vector4(normalizedNearPlaneCoordinate.X, normalizedNearPlaneCoordinate.Y, -1f, 1f);
		var farPlaneNdcVect = new Vector4(normalizedNearPlaneCoordinate.X, normalizedNearPlaneCoordinate.Y, 1f, 1f);
		
		Matrix4x4.Invert(projectionMatrix, out var projectionInverseMatrix);
		
		var nearPlaneViewVect = Vector4.Transform(nearPlaneNdcVect, projectionInverseMatrix);
		var farPlaneViewVect = Vector4.Transform(farPlaneNdcVect, projectionInverseMatrix);
		
		nearPlaneViewVect /= nearPlaneViewVect.W;
		farPlaneViewVect /= farPlaneViewVect.W;
		
		var nearPlaneWorldVect = Vector4.Transform(nearPlaneViewVect, modelMatrix);
		var farPlaneWorldVect = Vector4.Transform(farPlaneViewVect, modelMatrix);
		
		return new Ray(Location.FromVector3(nearPlaneWorldVect.AsVector3()), Direction.FromVector3((farPlaneWorldVect - nearPlaneWorldVect).AsVector3()));
	}
	
	/// <summary>
	/// Calculates the ray travelling out from a point on the near plane of a perspective camera, given its parameters.
	/// </summary>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="verticalFov">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="normalizedNearPlaneCoordinate">Where on the near plane the ray should start, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges.</param>
	/// <seealso cref="ProjectOnToPerspectiveCameraNearPlane(in Matrix4x4, in Matrix4x4, Location)"/>
	public static Ray CreateRayFromPerspectiveCameraParameters(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return CreateRayFromPerspectiveCameraParameters(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}
	
	// Maintainer's note:
	// I believe marking these as "in" may actually be counterproductive to performance as Matrix4x4 is mutable and 
	// this forces the compiler to make a defensive copy. However I'm keeping them here as I'm hoping the compiler
	// (either today or in the future) can prove that the inputted arguments are not mutated and therefore elide the copy 
	/// <summary>
	/// Calculates the ray travelling out from a point on the near plane of an orthographic camera, given its matrices.
	/// </summary>
	/// <remarks>
	/// Unlike a perspective camera, every such ray travels in the same direction; only the start point differs.
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="normalizedNearPlaneCoordinate">Where on the near plane the ray should start, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges.</param>
	/// <seealso cref="ProjectOnToOrthographicCameraNearPlane(in Matrix4x4, in Matrix4x4, Location)"/>
	public static Ray CreateRayFromOrthographicCameraParameters(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, XYPair<float> normalizedNearPlaneCoordinate) {
		Matrix4x4.Invert(projectionMatrix, out var projectionInverseMatrix);

		var projectedNdc = Vector4.Transform(new Vector4(normalizedNearPlaneCoordinate.X, normalizedNearPlaneCoordinate.Y, -1f, 1f), projectionInverseMatrix);
		projectedNdc /= projectedNdc.W;
		var pixelWorldLocation = Vector4.Transform(projectedNdc, modelMatrix);
		var dir = Vector3.TransformNormal(new Vector3(0f, 0f, -1f), modelMatrix);
		return new Ray(Location.FromVector3(pixelWorldLocation.AsVector3()), Direction.FromVector3(dir));
	}
	
	/// <summary>
	/// Calculates the ray travelling out from a point on the near plane of an orthographic camera, given its parameters.
	/// </summary>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="normalizedNearPlaneCoordinate">Where on the near plane the ray should start, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges.</param>
	/// <seealso cref="ProjectOnToOrthographicCameraNearPlane(in Matrix4x4, in Matrix4x4, Location)"/>
	public static Ray CreateRayFromOrthographicCameraParameters(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateOrthographicProjectionMatrix(nearPlaneDistance, farPlaneDistance, orthographicHeight, aspectRatio, out var projMat);
		return CreateRayFromOrthographicCameraParameters(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its matrices; or <see langword="null"/> if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="CreateRayFromPerspectiveCameraParameters(in Matrix4x4, in Matrix4x4, XYPair{float})"/>.
	/// The <paramref name="location"/> is considered outside the camera's view if it is behind the camera, or beyond the edges of its image. The near and far plane distances are not taken in to account.
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards); or <see langword="null"/> if the location is outside the camera's view.</returns>
	public static XYPair<float>? ProjectOnToPerspectiveCameraNearPlane(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location) {
		return ProjectOnToNearPlane(in modelMatrix, in projectionMatrix, location);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its matrices; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToPerspectiveCameraNearPlaneClamped(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location) {
		return ProjectOnToNearPlaneClamped(in modelMatrix, in projectionMatrix, location, out _);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its matrices; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <param name="wasClamped">When this method returns, <see langword="true"/> if the location was outside the camera's view and the result was clamped to the edge of the near plane; otherwise <see langword="false"/>.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToPerspectiveCameraNearPlaneClamped(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location, out bool wasClamped) {
		return ProjectOnToNearPlaneClamped(in modelMatrix, in projectionMatrix, location, out wasClamped);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its parameters; or <see langword="null"/> if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="CreateRayFromPerspectiveCameraParameters(Location, Direction, Direction, float, float, Angle, float, XYPair{float})"/>.
	/// The <paramref name="location"/> is considered outside the camera's view if it is behind the camera, or beyond the edges of its image. The near and far plane distances are not taken in to account.
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="verticalFov">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards); or <see langword="null"/> if the location is outside the camera's view.</returns>
	public static XYPair<float>? ProjectOnToPerspectiveCameraNearPlane(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, Location location) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return ProjectOnToNearPlane(in modelMat, in projMat, location);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its parameters; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="verticalFov">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToPerspectiveCameraNearPlaneClamped(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, Location location) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return ProjectOnToNearPlaneClamped(in modelMat, in projMat, location, out _);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of a perspective camera, given its parameters; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="verticalFov">How wide an angle of the scene the camera takes in from top to bottom.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <param name="wasClamped">When this method returns, <see langword="true"/> if the location was outside the camera's view and the result was clamped to the edge of the near plane; otherwise <see langword="false"/>.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToPerspectiveCameraNearPlaneClamped(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, Location location, out bool wasClamped) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return ProjectOnToNearPlaneClamped(in modelMat, in projMat, location, out wasClamped);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its matrices; or <see langword="null"/> if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="CreateRayFromOrthographicCameraParameters(in Matrix4x4, in Matrix4x4, XYPair{float})"/>.
	/// The <paramref name="location"/> is considered outside the camera's view if it is behind the camera, or beyond the edges of its image. The near and far plane distances are not taken in to account.
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards); or <see langword="null"/> if the location is outside the camera's view.</returns>
	public static XYPair<float>? ProjectOnToOrthographicCameraNearPlane(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location) {
		return ProjectOnToNearPlane(in modelMatrix, in projectionMatrix, location);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its matrices; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToOrthographicCameraNearPlaneClamped(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location) {
		return ProjectOnToNearPlaneClamped(in modelMatrix, in projectionMatrix, location, out _);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its matrices; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="modelMatrix">The model matrix of the camera.</param>
	/// <param name="projectionMatrix">The projection matrix of the camera.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <param name="wasClamped">When this method returns, <see langword="true"/> if the location was outside the camera's view and the result was clamped to the edge of the near plane; otherwise <see langword="false"/>.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToOrthographicCameraNearPlaneClamped(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location, out bool wasClamped) {
		return ProjectOnToNearPlaneClamped(in modelMatrix, in projectionMatrix, location, out wasClamped);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its parameters; or <see langword="null"/> if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="CreateRayFromOrthographicCameraParameters(Location, Direction, Direction, float, float, float, float, XYPair{float})"/>.
	/// The <paramref name="location"/> is considered outside the camera's view if it is behind the camera, or beyond the edges of its image. The near and far plane distances are not taken in to account.
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards); or <see langword="null"/> if the location is outside the camera's view.</returns>
	public static XYPair<float>? ProjectOnToOrthographicCameraNearPlane(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, Location location) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateOrthographicProjectionMatrix(nearPlaneDistance, farPlaneDistance, orthographicHeight, aspectRatio, out var projMat);
		return ProjectOnToNearPlane(in modelMat, in projMat, location);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its parameters; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToOrthographicCameraNearPlaneClamped(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, Location location) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateOrthographicProjectionMatrix(nearPlaneDistance, farPlaneDistance, orthographicHeight, aspectRatio, out var projMat);
		return ProjectOnToNearPlaneClamped(in modelMat, in projMat, location, out _);
	}

	/// <summary>
	/// Calculates where a <paramref name="location"/> in the world appears on the near plane of an orthographic camera, given its parameters; clamped to the edge of the near plane if it is outside the camera's view.
	/// </summary>
	/// <remarks>
	/// When the <paramref name="location"/> is outside the camera's view (behind the camera, or beyond the edges of its image), the returned coordinate is moved from the centre of the near plane towards the location's direction until it reaches the edge.
	/// This means it always lies on the side of the image facing the location, even when the location is behind the camera, which makes it suitable for placing off-screen indicators.
	/// A location directly behind the camera is placed at <c>(0, -1)</c> (the centre of the bottom edge).
	/// </remarks>
	/// <param name="cameraPosition">Where the camera is.</param>
	/// <param name="cameraViewDirection">Which way the camera is looking.</param>
	/// <param name="cameraUpDirection">Which way is "up" for the camera.</param>
	/// <param name="nearPlaneDistance">How close an object may come to the camera and still be drawn, in metres.</param>
	/// <param name="farPlaneDistance">How far away an object may be and still be drawn, in metres.</param>
	/// <param name="orthographicHeight">How tall a slice of the world fills the image, in metres.</param>
	/// <param name="aspectRatio">The width of the image divided by its height.</param>
	/// <param name="location">The location in the world to project.</param>
	/// <param name="wasClamped">When this method returns, <see langword="true"/> if the location was outside the camera's view and the result was clamped to the edge of the near plane; otherwise <see langword="false"/>.</param>
	/// <returns>The point on the near plane, as a fraction of its size: <c>(0, 0)</c> is the centre of the image, and each component runs from <c>-1</c> to <c>1</c> at the edges (with positive Y upwards).</returns>
	public static XYPair<float> ProjectOnToOrthographicCameraNearPlaneClamped(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, Location location, out bool wasClamped) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateOrthographicProjectionMatrix(nearPlaneDistance, farPlaneDistance, orthographicHeight, aspectRatio, out var projMat);
		return ProjectOnToNearPlaneClamped(in modelMat, in projMat, location, out wasClamped);
	}

	static Vector4 CalculateClipSpaceVector(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location, out bool isInFrontOfCamera) {
		Matrix4x4.Invert(modelMatrix, out var viewMatrix);
		var viewVect = Vector4.Transform(new Vector4(location.ToVector3(), 1f), viewMatrix);
		isInFrontOfCamera = viewVect.Z < 0f;
		return Vector4.Transform(viewVect, projectionMatrix);
	}

	static bool TryGetInBoundsNearPlaneCoord(Vector4 clip, bool isInFrontOfCamera, out XYPair<float> result) {
		const float EdgeTolerance = 1E-4f;
		result = default;
		if (!isInFrontOfCamera) return false;
		var ndcX = clip.X / clip.W;
		var ndcY = clip.Y / clip.W;
		if (!(MathF.Abs(ndcX) <= 1f + EdgeTolerance && MathF.Abs(ndcY) <= 1f + EdgeTolerance)) return false;
		result = new XYPair<float>(Math.Clamp(ndcX, -1f, 1f), Math.Clamp(ndcY, -1f, 1f));
		return true;
	}

	static XYPair<float>? ProjectOnToNearPlane(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location) {
		var clip = CalculateClipSpaceVector(in modelMatrix, in projectionMatrix, location, out var isInFrontOfCamera);
		return TryGetInBoundsNearPlaneCoord(clip, isInFrontOfCamera, out var result) ? result : null;
	}

	static XYPair<float> ProjectOnToNearPlaneClamped(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, Location location, out bool wasClamped) {
		const float DegenerateDirectionThreshold = 1E-6f;
		var clip = CalculateClipSpaceVector(in modelMatrix, in projectionMatrix, location, out var isInFrontOfCamera);
		if (TryGetInBoundsNearPlaneCoord(clip, isInFrontOfCamera, out var inBoundsResult)) {
			wasClamped = false;
			return inBoundsResult;
		}

		wasClamped = true;
		var largestComponentMagnitude = MathF.Max(MathF.Abs(clip.X), MathF.Abs(clip.Y));
		if (!(largestComponentMagnitude > DegenerateDirectionThreshold * MathF.Max(1f, MathF.Abs(clip.W)))) return new XYPair<float>(0f, -1f);
		return new XYPair<float>(clip.X / largestComponentMagnitude, clip.Y / largestComponentMagnitude);
	}
}