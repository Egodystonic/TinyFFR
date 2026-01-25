// Created on 2026-01-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

public static class CameraUtils {
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
	
	public static void CalculateOrthographicProjectionMatrix(float nearPlaneDistance, float farPlaneDistance, float orthographicHeight, float aspectRatio, out Matrix4x4 dest) {
		var frustumLength = farPlaneDistance - nearPlaneDistance;

		dest = new Matrix4x4(
			2f / (orthographicHeight * aspectRatio),			0f,									0f,																	0f,
			0f,													2f / orthographicHeight,			0f,																	0f,
			0f,													0f,									-2f / frustumLength,												0f,
			0f,													0f,									-(farPlaneDistance + nearPlaneDistance) / frustumLength,			1f
		);
	}
	
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
	
	public static void CalculateViewMatrix(Location position, Direction viewDirection, Direction upDirection, out Matrix4x4 dest) {
		CalculateModelMatrix(position, viewDirection, upDirection, out var modelMat);
		Matrix4x4.Invert(modelMat, out dest);
	}
	
	// Maintainer's note:
	// I believe marking these as "in" may actually be counterproductive to performance as Matrix4x4 is mutable and 
	// this forces the compiler to make a defensive copy. However I'm keeping them here as I'm hoping the compiler
	// (either today or in the future) can prove that the inputted arguments are not mutated and therefore elide the copy 
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
	
	public static Ray CreateRayFromPerspectiveCameraParameters(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return CreateRayFromPerspectiveCameraParameters(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}
	
	// Maintainer's note:
	// I believe marking these as "in" may actually be counterproductive to performance as Matrix4x4 is mutable and 
	// this forces the compiler to make a defensive copy. However I'm keeping them here as I'm hoping the compiler
	// (either today or in the future) can prove that the inputted arguments are not mutated and therefore elide the copy 
	public static Ray CreateRayFromOrthographicCameraParameters(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, XYPair<float> normalizedNearPlaneCoordinate) {
		Matrix4x4.Invert(projectionMatrix, out var projectionInverseMatrix);

		var projectedNdc = Vector4.Transform(new Vector4(normalizedNearPlaneCoordinate.X, normalizedNearPlaneCoordinate.Y, -1f, 1f), projectionInverseMatrix);
		projectedNdc /= projectedNdc.W;
		var pixelWorldLocation = Vector4.Transform(projectedNdc, modelMatrix);
		var dir = Vector3.TransformNormal(new Vector3(0f, 0f, -1f), modelMatrix);
		return new Ray(Location.FromVector3(pixelWorldLocation.AsVector3()), Direction.FromVector3(dir));
	}
	
	public static Ray CreateRayFromOrthographicCameraParameters(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculatePerspectiveProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return CreateRayFromOrthographicCameraParameters(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}
}