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

	public OneToManyEnumerator<SceneHandle, Camera> Cameras {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCameras(_handle);
	}

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Scene(SceneHandle handle, ISceneImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Scene IResource<Scene>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Scene(rawHandle, impl as ISceneImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddCamera(Camera camera) => Implementation.AddCamera(_handle, camera);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveCamera(Camera camera) => Implementation.RemoveCamera(_handle, camera);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ModelInstance AddModelInstance() => ObjectBuilder.AddModelInstance();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ModelInstance AddModelInstance(in ModelInstanceCreationConfig config) => ObjectBuilder.AddModelInstance(in config);

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