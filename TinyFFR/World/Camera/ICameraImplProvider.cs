// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Camera"/> resources.
/// </summary>
public interface ICameraImplProvider : IDisposableResourceImplProvider<Camera> {
	/// <summary>
	/// Invoked via <see cref="Camera.Position"/>.
	/// </summary>
	public Location GetPosition(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.Position"/>.
	/// </summary>
	public void SetPosition(ResourceHandle<Camera> handle, Location newPosition);
	/// <summary>
	/// Invoked via <see cref="Camera.ViewDirection"/>.
	/// </summary>
	public Direction GetViewDirection(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.ViewDirection"/>.
	/// </summary>
	public void SetViewDirection(ResourceHandle<Camera> handle, Direction newDirection);
	/// <summary>
	/// Invoked via <see cref="Camera.UpDirection"/>.
	/// </summary>
	public Direction GetUpDirection(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.UpDirection"/>.
	/// </summary>
	public void SetUpDirection(ResourceHandle<Camera> handle, Direction newDirection);
	/// <summary>
	/// Invoked via <see cref="Camera.SetViewAndUpDirection"/>.
	/// </summary>
	public void SetViewAndUpDirection(ResourceHandle<Camera> handle, Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality);

	/// <summary>
	/// Invoked via <see cref="Camera.HorizontalFieldOfView"/>.
	/// </summary>
	public Angle GetHorizontalFieldOfView(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.HorizontalFieldOfView"/>.
	/// </summary>
	public void SetHorizontalFieldOfView(ResourceHandle<Camera> handle, Angle newFov);
	/// <summary>
	/// Invoked via <see cref="Camera.VerticalFieldOfView"/>.
	/// </summary>
	public Angle GetVerticalFieldOfView(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.VerticalFieldOfView"/>.
	/// </summary>
	public void SetVerticalFieldOfView(ResourceHandle<Camera> handle, Angle newFov);
	/// <summary>
	/// Invoked via <see cref="Camera.OrthographicHeight"/>.
	/// </summary>
	public float GetOrthographicHeight(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.OrthographicHeight"/>.
	/// </summary>
	public void SetOrthographicHeight(ResourceHandle<Camera> handle, float newHeight);
	/// <summary>
	/// Invoked via <see cref="Camera.AspectRatio"/>.
	/// </summary>
	public float GetAspectRatio(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.AspectRatio"/>.
	/// </summary>
	public void SetAspectRatio(ResourceHandle<Camera> handle, float newRatio);
	
	/// <summary>
	/// Invoked via <see cref="Camera.Exposure"/>.
	/// </summary>
	public float GetExposure(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.Exposure"/>.
	/// </summary>
	public void SetExposure(ResourceHandle<Camera> handle, float newExposure);
	/// <summary>
	/// Invoked via <see cref="Camera.SetExposure(float, float, float)"/>.
	/// </summary>
	public void SetExposure(ResourceHandle<Camera> handle, float aperture, float shutterSpeed, float sensitivity);
	
	/// <summary>
	/// Invoked via <see cref="Camera.FocusDistance"/>.
	/// </summary>
	public float? GetFocusDistance(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.FocusDistance"/>.
	/// </summary>
	public void SetFocusDistance(ResourceHandle<Camera> handle, float? newFocusDistance);

	/// <summary>
	/// Invoked via <see cref="Camera.NearPlaneDistance"/>.
	/// </summary>
	public float GetNearPlaneDistance(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.NearPlaneDistance"/>.
	/// </summary>
	public void SetNearPlaneDistance(ResourceHandle<Camera> handle, float newDistance);
	/// <summary>
	/// Invoked via <see cref="Camera.FarPlaneDistance"/>.
	/// </summary>
	public float GetFarPlaneDistance(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.FarPlaneDistance"/>.
	/// </summary>
	public void SetFarPlaneDistance(ResourceHandle<Camera> handle, float newDistance);
	/// <summary>
	/// Invoked via <see cref="Camera.ProjectionType"/>.
	/// </summary>
	public CameraProjectionType GetProjectionType(ResourceHandle<Camera> handle);
	/// <summary>
	/// Invoked via <see cref="Camera.ProjectionType"/>.
	/// </summary>
	public void SetProjectionType(ResourceHandle<Camera> handle, CameraProjectionType newProjectionType);

	/// <summary>
	/// Invoked via <see cref="Camera.GetProjectionMatrix()"/>.
	/// </summary>
	public void GetProjectionMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	/// <summary>
	/// Invoked via <see cref="Camera.SetProjectionMatrix"/>.
	/// </summary>
	public void SetProjectionMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);
	/// <summary>
	/// Invoked via <see cref="Camera.GetModelMatrix()"/>.
	/// </summary>
	public void GetModelMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	/// <summary>
	/// Invoked via <see cref="Camera.SetModelMatrix"/>.
	/// </summary>
	public void SetModelMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);
	/// <summary>
	/// Invoked via <see cref="Camera.GetViewMatrix()"/>.
	/// </summary>
	public void GetViewMatrix(ResourceHandle<Camera> handle, out Matrix4x4 outMatrix);
	/// <summary>
	/// Invoked via <see cref="Camera.SetViewMatrix"/>.
	/// </summary>
	public void SetViewMatrix(ResourceHandle<Camera> handle, in Matrix4x4 newMatrix);

	/// <summary>
	/// Invoked via <see cref="Camera.MoveBy"/>.
	/// </summary>
	void Translate(ResourceHandle<Camera> handle, Vect translation);
	/// <summary>
	/// Invoked via <see cref="Camera.RotateBy(Rotation)"/>.
	/// </summary>
	void Rotate(ResourceHandle<Camera> handle, Rotation rotation);
	/// <summary>
	/// Invoked via <see cref="Camera.RotateBy(Quaternion)"/>.
	/// </summary>
	void Rotate(ResourceHandle<Camera> handle, Quaternion rotationQuaternion);
	
	/// <summary>
	/// Invoked via <see cref="Camera.CreateRayFromNearPlane(XYPair{float})"/>.
	/// </summary>
	Ray CreateRayFromNearPlane(ResourceHandle<Camera> handle, XYPair<float> normalizedNearPlaneCoord);
}