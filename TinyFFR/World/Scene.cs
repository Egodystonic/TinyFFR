// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct Scene : IDisposableResource<Scene, SceneHandle, ISceneImplProvider> {
	readonly SceneHandle _handle;
	readonly ISceneImplProvider _impl;

	internal ISceneImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Scene>();
	internal SceneHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Scene)) : _handle;

	ISceneImplProvider IResource<SceneHandle, ISceneImplProvider>.Implementation => Implementation;
	SceneHandle IResource<SceneHandle, ISceneImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Name {
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
	public void Add(ModelInstance modelInstance) => Implementation.Add(_handle, modelInstance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(ModelInstance modelInstance) => Implementation.Remove(_handle, modelInstance);

	public override string ToString() => $"Scene {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
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