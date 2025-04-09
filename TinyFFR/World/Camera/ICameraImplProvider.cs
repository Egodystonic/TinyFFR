// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ICameraImplProvider : IDisposableResourceImplProvider<Camera> {
	public Location GetPosition(ResourceHandle<Camera> handle);
	public void SetPosition(ResourceHandle<Camera> handle, Location newPosition);
	public Direction GetViewDirection(ResourceHandle<Camera> handle);
	public void SetViewDirection(ResourceHandle<Camera> handle, Direction newDirection);
	public Direction GetUpDirection(ResourceHandle<Camera> handle);
	public void SetUpDirection(ResourceHandle<Camera> handle, Direction newDirection);
	public void SetViewAndUpDirection(ResourceHandle<Camera> handle, Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality);

	public Angle GetHorizontalFieldOfView(ResourceHandle<Camera> handle);
	public void SetHorizontalFieldOfView(ResourceHandle<Camera> handle, Angle newFov);
	public Angle GetVerticalFieldOfView(ResourceHandle<Camera> handle);
	public void SetVerticalFieldOfView(ResourceHandle<Camera> handle, Angle newFov);
	public float GetAspectRatio(ResourceHandle<Camera> handle);
	public void SetAspectRatio(ResourceHandle<Camera> handle, float newRatio);

	public float GetNearPlaneDistance(ResourceHandle<Camera> handle);
	public void SetNearPlaneDistance(ResourceHandle<Camera> handle, float newDistance);
	public float GetFarPlaneDistance(ResourceHandle<Camera> handle);
	public void SetFarPlaneDistance(ResourceHandle<Camera> handle, float newDistance);

	public void GetProjectionMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	public void SetProjectionMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);
	public void GetModelMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	public void SetModelMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);
	public void GetViewMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	public void SetViewMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);

	void Translate(ResourceHandle<Camera> handle, Vect translation);
	void Rotate(ResourceHandle<Camera> handle, Rotation rotation);
}