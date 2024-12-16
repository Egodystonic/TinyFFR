// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct ModelInstance : IDisposableResource<ModelInstance, ModelInstanceHandle, IModelInstanceImplProvider>, ITransformedSceneObject {
	readonly ModelInstanceHandle _handle;
	readonly IModelInstanceImplProvider _impl;

	internal IModelInstanceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ModelInstance>();
	internal ModelInstanceHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ModelInstance)) : _handle;

	IModelInstanceImplProvider IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Implementation => Implementation;
	ModelInstanceHandle IResource<ModelInstanceHandle, IModelInstanceImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTransform(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTransform(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetRotation(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetRotation(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetScaling(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetScaling(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;

	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMaterial(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMaterial(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMesh(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMesh(Mesh mesh) => Mesh = mesh;

	internal ModelInstance(ModelInstanceHandle handle, IModelInstanceImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static ModelInstance IResource<ModelInstance>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new ModelInstance(rawHandle, impl as IModelInstanceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);
	public void RotateBy(Rotation rotation) => Implementation.RotateBy(_handle, rotation);
	public void ScaleBy(float scalar) => Implementation.ScaleBy(_handle, scalar);
	public void ScaleBy(Vect vect) => Implementation.ScaleBy(_handle, vect);
	public void AdjustScaleBy(float scalar) => Implementation.AdjustScaleBy(_handle, scalar);
	public void AdjustScaleBy(Vect vect) => Implementation.AdjustScaleBy(_handle, vect);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
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