// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct Camera : IDisposableResource<Camera, ICameraImplProvider>, IPositionedSceneObject, IOrientedSceneObject {
	public static readonly Angle FieldOfViewMin = Angle.Zero;
	public static readonly Angle FieldOfViewMax = Angle.FullCircle;
	public static readonly float NearPlaneDistanceMin = 1E-5f;
	public static readonly float NearFarPlaneDistanceRatioMax = 1E6f;

	readonly ResourceHandle<Camera> _handle;
	readonly ICameraImplProvider _impl;

	internal ICameraImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Camera>();
	internal ResourceHandle<Camera> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Camera)) : _handle;

	ICameraImplProvider IResource<Camera, ICameraImplProvider>.Implementation => Implementation;
	ResourceHandle<Camera> IResource<Camera>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Direction ViewDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetViewDirection(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetViewDirection(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetViewDirection(Direction direction) => ViewDirection = direction;

	public Direction UpDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUpDirection(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUpDirection(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetUpDirection(Direction direction) => UpDirection = direction;

	public Angle HorizontalFieldOfView {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHorizontalFieldOfView(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetHorizontalFieldOfView(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHorizontalFieldOfView(Angle fov) => HorizontalFieldOfView = fov;

	public Angle VerticalFieldOfView {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetVerticalFieldOfView(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetVerticalFieldOfView(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetVerticalFieldOfView(Angle fov) => VerticalFieldOfView = fov;

	public float NearPlaneDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetNearPlaneDistance(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetNearPlaneDistance(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetNearPlaneDistance(float distance) => NearPlaneDistance = distance;

	public float FarPlaneDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFarPlaneDistance(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFarPlaneDistance(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFarPlaneDistance(float distance) => FarPlaneDistance = distance;

	Rotation IOrientedSceneObject.Rotation {
		get => Rotation.FromStartAndEndDirection(Direction.Forward, ViewDirection);
		set => ViewDirection = Direction.Forward * value;
	}

	internal Camera(ResourceHandle<Camera> handle, ICameraImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Camera IResource<Camera>.CreateFromHandleAndImpl(ResourceHandle<Camera> handle, IResourceImplProvider impl) {
		return new Camera(handle, impl as ICameraImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetProjectionMatrix() {
		Implementation.GetProjectionMatrix(_handle, out var result);
		return result;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetProjectionMatrix(out Matrix4x4 outProjectionMatrix) => Implementation.GetProjectionMatrix(_handle, out outProjectionMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetProjectionMatrix(in Matrix4x4 newProjectionMatrix) => Implementation.SetProjectionMatrix(_handle, newProjectionMatrix);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetModelMatrix() {
		Implementation.GetModelMatrix(_handle, out var result);
		return result;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetModelMatrix(out Matrix4x4 outModelMatrix) => Implementation.GetModelMatrix(_handle, out outModelMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetModelMatrix(in Matrix4x4 newModelMatrix) => Implementation.SetModelMatrix(_handle, newModelMatrix);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetViewMatrix() {
		Implementation.GetViewMatrix(_handle, out var result);
		return result;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetViewMatrix(out Matrix4x4 outViewMatrix) => Implementation.GetViewMatrix(_handle, out outViewMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetViewMatrix(in Matrix4x4 newViewMatrix) => Implementation.SetViewMatrix(_handle, newViewMatrix);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => Implementation.Translate(_handle, translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => Implementation.Rotate(_handle, rotation);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Camera {(IsDisposed ? "(Disposed)" : $"\"{Name}\" (Position {Position}, View Direction {ViewDirection.ToStringDescriptive()})")}";

	#region Equality
	public bool Equals(Camera other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Camera other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Camera left, Camera right) => left.Equals(right);
	public static bool operator !=(Camera left, Camera right) => !left.Equals(right);
	#endregion
}