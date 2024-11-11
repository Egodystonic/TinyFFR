// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Scene;

public interface ICameraImplProvider : IDisposableResourceImplProvider<CameraHandle> {
	public Location GetPosition(CameraHandle handle);
	public void SetPosition(CameraHandle handle, Location newPosition);
	public Direction GetViewDirection(CameraHandle handle);
	public void SetViewDirection(CameraHandle handle, Direction newDirection);
	public Direction GetUpDirection(CameraHandle handle);
	public void SetUpDirection(CameraHandle handle, Direction newDirection);

	public Angle GetHorizontalFieldOfView(CameraHandle handle);
	public void SetHorizontalFieldOfView(CameraHandle handle, Angle newFov);
	public Angle GetVerticalFieldOfView(CameraHandle handle);
	public void SetVerticalFieldOfView(CameraHandle handle, Angle newFov);

	public float GetNearPlaneDistance(CameraHandle handle);
	public void SetNearPlaneDistance(CameraHandle handle, float newDistance);
	public float GetFarPlaneDistance(CameraHandle handle);
	public void SetFarPlaneDistance(CameraHandle handle, float newDistance);

	public void GetProjectionMatrix(CameraHandle handle, out Matrix4x4 outMatrix);
	public void SetProjectionMatrix(CameraHandle handle, in Matrix4x4 newMatrix);
	public void GetModelMatrix(CameraHandle handle, out Matrix4x4 outMatrix);
	public void SetModelMatrix(CameraHandle handle, in Matrix4x4 newMatrix);
	public void GetViewMatrix(CameraHandle handle, out Matrix4x4 outMatrix);
	public void SetViewMatrix(CameraHandle handle, in Matrix4x4 newMatrix);

	void Translate(CameraHandle handle, Vect translation);
	void Rotate(CameraHandle handle, Rotation rotation);
}