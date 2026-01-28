// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

public readonly struct Model : IDisposableResource<Model, IModelImplProvider> {
	readonly ResourceHandle<Model> _handle;
	readonly IModelImplProvider _impl;

	internal ResourceHandle<Model> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Model)) : _handle;
	internal IModelImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Model>();

	IModelImplProvider IResource<Model, IModelImplProvider>.Implementation => Implementation;
	ResourceHandle<Model> IResource<Model>.Handle => Handle;

	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
	}
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMaterial(_handle);
	}
	public IndirectEnumerable<Model, Texture> Textures {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTextures(_handle);
	}

	internal Model(ResourceHandle<Model> handle, IModelImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Model IResource<Model>.CreateFromHandleAndImpl(ResourceHandle<Model> handle, IResourceImplProvider impl) {
		return new Model(handle, impl as IModelImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Model {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(Model other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Model other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Model left, Model right) => left.Equals(right);
	public static bool operator !=(Model left, Model right) => !left.Equals(right);
	#endregion
}