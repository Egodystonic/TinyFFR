// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Scene;

public readonly struct Scene : IDisposableResource<Scene, SceneHandle, ISceneImplProvider> {
	readonly SceneHandle _handle;
	readonly ISceneImplProvider _impl;

	internal ISceneImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Scene>();
	internal SceneHandle Handle => _handle;

	ISceneImplProvider IResource<SceneHandle, ISceneImplProvider>.Implementation => Implementation;
	SceneHandle IResource<SceneHandle, ISceneImplProvider>.Handle => Handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public ISceneCameraBuilder CameraBuilder {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCameraBuilder(_handle);
	}

	public ISceneObjectBuilder ObjectBuilder {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetObjectBuilder(_handle);
	}

	internal Scene(SceneHandle handle, ISceneImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Scene IResource<Scene>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Scene(rawHandle, impl as ISceneImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Camera CreateCamera() => CameraBuilder.CreateCamera();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Camera CreateCamera(Location initialPosition, Direction initialViewDirection) => CameraBuilder.CreateCamera(initialPosition, initialViewDirection);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Camera CreateCamera(in CameraCreationConfig config) => CameraBuilder.CreateCamera(in config);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ModelInstance CreateModelInstance() => ObjectBuilder.CreateModelInstance();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ModelInstance CreateModelInstance(in ModelInstanceCreationConfig config) => ObjectBuilder.CreateModelInstance(in config);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanLength() => Implementation.GetNameSpanLength(_handle);

	public override string ToString() => $"Scene {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(Scene other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Scene other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Scene left, Scene right) => left.Equals(right);
	public static bool operator !=(Scene left, Scene right) => !left.Equals(right);
	#endregion
}