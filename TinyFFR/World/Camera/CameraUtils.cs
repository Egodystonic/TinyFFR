// Created on 2026-01-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

public static class CameraUtils {
	public static void CalculateProjectionMatrix(float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, out Matrix4x4 dest) {
		var near = nearPlaneDistance;
		var far = farPlaneDistance;

		var h = MathF.Tan(verticalFov.Radians * 0.5f) * near;
		var w = h * aspectRatio;

		var left = -w;
		var right = w;
		var bottom = -h;
		var top = h;

		dest = new Matrix4x4(
			(near * 2f) / (right - left),			0f,										0f,										0f,
			0f,										(near * 2f) / (top - bottom),			0f,										0f,
			(right + left) / (right - left),		(top + bottom) / (top - bottom),		-(far + near) / (far - near),			-1f,
			0f,										0f,										-(2f * far * near) / (far - near),		0f
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
	public static Ray CreateRayFromCameraFrustumNearPlane(in Matrix4x4 modelMatrix, in Matrix4x4 projectionMatrix, XYPair<float> normalizedNearPlaneCoordinate) {
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
	
	public static Ray CreateRayFromCameraFrustumNearPlane(Location cameraPosition, Direction cameraViewDirection, Direction cameraUpDirection, float nearPlaneDistance, float farPlaneDistance, Angle verticalFov, float aspectRatio, XYPair<float> normalizedNearPlaneCoordinate) {
		CalculateModelMatrix(cameraPosition, cameraViewDirection, cameraUpDirection, out var modelMat);
		CalculateProjectionMatrix(nearPlaneDistance, farPlaneDistance, verticalFov, aspectRatio, out var projMat);
		return CreateRayFromCameraFrustumNearPlane(in modelMat, in projMat, normalizedNearPlaneCoordinate);
	}
}