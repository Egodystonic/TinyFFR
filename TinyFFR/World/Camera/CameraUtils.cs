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
	/// Equivalent to <see cref="CalculatePerspectiveViewportWorldSizeAtDistance"/>, but skipping the trigonometry when the tangents are already to hand — worth using when calculating this every frame.
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
	public static Ray CreateRayFromOrthographicCameraParameters(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateOrthographicProjectionMatrix(nearPlaneDistance, farPlaneDistance, orthographicHeight, aspectRatio, out var projMat);
		return CreateRayFromOrthographicCameraParameters(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}
	
	/// <summary>
	/// Converts a single exposure multiplier in to the aperture, shutter speed and sensitivity that would produce it.
	/// </summary>
	/// <remarks>
	/// The conversion behind <see cref="Camera.Exposure"/>; the inverse of what <see cref="Camera.SetExposure(float, float, float)"/> does.
	/// </remarks>
	/// <param name="exposure">The exposure value to convert. Clamped to the permitted range, and updated in place to the clamped value.</param>
	/// <param name="aperture">When this method returns, contains the equivalent aperture as an f-number.</param>
	/// <param name="shutterSpeed">When this method returns, contains the equivalent shutter speed, in seconds.</param>
	/// <param name="sensitivity">When this method returns, contains the equivalent sensitivity as an ISO value.</param>
	public static void ConvertBasicExposureValueToGranularValues(ref float exposure, out float aperture, out float shutterSpeed, out float sensitivity) {
		const float LowestExposureAperture = 32f; // Higher value = less exposure
		const float LowestExposureShutterSpeed = 1f / 4000f; // Lower value = less exposure
		const float LowestExposureSensitivity = 100f; // Lower value = less exposure
		const float HighestExposureAperture = 2f; // Lower value = more exposure
		const float HighestExposureShutterSpeed = 1f / 25f; // Higher value = more exposure
		const float HighestExposureSensitivity = 750f; // Higher value = more exposure
		
		exposure = ((Real) exposure).Clamp(Camera.ExposureMin, Camera.ExposureMax);
		
		if (exposure < 1f) {
			var distance = ((Real) exposure).RemapRange((Camera.ExposureDefault, Camera.ExposureMin), (0f, 1f));
			aperture = Real.Interpolate(Camera.ApertureDefault, LowestExposureAperture, distance);
			shutterSpeed = Real.Interpolate(Camera.ShutterSpeedDefault, LowestExposureShutterSpeed, distance);
			sensitivity = Real.Interpolate(Camera.SensitivityDefault, LowestExposureSensitivity, distance);
		}
		else {
			var distance = ((Real) exposure).RemapRange((Camera.ExposureDefault, Camera.ExposureMax), (0f, 1f));
			aperture = Real.Interpolate(Camera.ApertureDefault, HighestExposureAperture, distance);
			shutterSpeed = Real.Interpolate(Camera.ShutterSpeedDefault, HighestExposureShutterSpeed, distance);
			sensitivity = Real.Interpolate(Camera.SensitivityDefault, HighestExposureSensitivity, distance);
		}
	}
}