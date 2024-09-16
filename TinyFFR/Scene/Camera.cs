// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Scene;

public readonly unsafe struct Camera : IEquatable<Camera>, IDisposable {
	readonly CameraAssetHandle _handle;
	readonly ICameraAssetImplProvider _impl;

	ICameraAssetImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Camera>();

	internal Camera(CameraAssetHandle handle, ICameraAssetImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Camera {(IsDisposed ? "(Disposed)" : $"\"{}\"")}";

	public bool Equals(Camera other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Camera other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Camera left, Camera right) => left.Equals(right);
	public static bool operator !=(Camera left, Camera right) => !left.Equals(right);
}