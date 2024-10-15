// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Scene;

public readonly struct ModelInstance : IDisposableResource<ModelInstance, ModelInstanceHandle, IModelInstanceImplProvider> {
	readonly ModelInstanceHandle _handle;
	readonly IModelInstanceImplProvider _impl;

	internal IModelInstanceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ModelInstance>();
	internal ModelInstanceHandle Handle => _handle;

	IModelInstanceImplProvider IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Implementation => Implementation;
	ModelInstanceHandle IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Handle => Handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMaterial(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMaterial(_handle, value);
	}

	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMesh(_handle, value);
	}

	internal ModelInstance(ModelInstanceHandle handle, IModelInstanceImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static ModelInstance IResource<ModelInstance>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new ModelInstance(rawHandle, impl as IModelInstanceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanLength() => Implementation.GetNameSpanLength(_handle);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Model Instance {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(ModelInstance other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ModelInstance other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(ModelInstance left, ModelInstance right) => left.Equals(right);
	public static bool operator !=(ModelInstance left, ModelInstance right) => !left.Equals(right);
	#endregion
}