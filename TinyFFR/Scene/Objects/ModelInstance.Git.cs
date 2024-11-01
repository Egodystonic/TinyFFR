// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Scene;

public readonly struct ModelInstance : IDisposableResource<ModelInstance, ModelInstanceHandle, IModelInstanceImplProvider>, ITransformedSceneObject {
	readonly ModelInstanceHandle _handle;
	readonly IModelInstanceImplProvider _impl;

	internal IModelInstanceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ModelInstance>();
	internal ModelInstanceHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ModelInstance)) : _handle;

	IModelInstanceImplProvider IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Implementation => Implementation;
	ModelInstanceHandle IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Handle => Handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTransform(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTransform(_handle, value);
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetRotation(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetRotation(_handle, value);
	}
	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetScaling(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetScaling(_handle, value);
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

	public void Scale(float scalar) => Implementation.Scale(_handle, scalar);
	public void Scale(Vect vect) => Implementation.Scale(_handle, vect);
	public void Rotate(Rotation rotation) => Implementation.Rotate(_handle, rotation);
	public void Move(Vect translation) => Implementation.Translate(_handle, translation);

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