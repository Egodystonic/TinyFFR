// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly record struct MaterialEffectController {
	readonly ModelInstance _attachedModelInstance;

	public MaterialEffectController(ModelInstance attachedModelInstance) => _attachedModelInstance = attachedModelInstance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTransform(Transform2D newTransform) {
		_attachedModelInstance.SetMaterialEffectTransform(newTransform);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTexture(MaterialEffectMapType mapType, Texture texture) {
		_attachedModelInstance.SetEffectBlendTexture(mapType, texture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendDistance(MaterialEffectMapType mapType, float distance) {
		_attachedModelInstance.SetEffectBlendDistance(mapType, distance);
	}
}

public readonly struct ModelInstance : IDisposableResource<ModelInstance, IModelInstanceImplProvider>, ITransformedSceneObject {
	readonly ResourceHandle<ModelInstance> _handle;
	readonly IModelInstanceImplProvider _impl;

	internal IModelInstanceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ModelInstance>();
	internal ResourceHandle<ModelInstance> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ModelInstance)) : _handle;

	IModelInstanceImplProvider IResource<ModelInstance, IModelInstanceImplProvider>.Implementation => Implementation;
	ResourceHandle<ModelInstance> IResource<ModelInstance>.Handle => Handle;

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

	public MaterialEffectController? MaterialEffects {
		get {
			if (!Material.SupportsPerInstanceEffects) return null;
			return new MaterialEffectController(this);
		}
	}

	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMesh(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMesh(Mesh mesh) => Mesh = mesh;

	internal ModelInstance(ResourceHandle<ModelInstance> handle, IModelInstanceImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ModelInstance IResource<ModelInstance>.CreateFromHandleAndImpl(ResourceHandle<ModelInstance> handle, IResourceImplProvider impl) {
		return new ModelInstance(handle, impl as IModelInstanceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);
	public void RotateBy(Rotation rotation) => Implementation.RotateBy(_handle, rotation);
	public void ScaleBy(float scalar) => Implementation.ScaleBy(_handle, scalar);
	public void ScaleBy(Vect vect) => Implementation.ScaleBy(_handle, vect);
	public void AdjustScaleBy(float scalar) => Implementation.AdjustScaleBy(_handle, scalar);
	public void AdjustScaleBy(Vect vect) => Implementation.AdjustScaleBy(_handle, vect);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetMaterialEffectTransform(Transform2D newTransform) => Implementation.SetMaterialEffectTransform(_handle, newTransform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendTexture(MaterialEffectMapType mapType, Texture texture) => Implementation.SetMaterialEffectBlendTexture(_handle, mapType, texture);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendDistance(MaterialEffectMapType mapType, float distance) => Implementation.SetMaterialEffectBlendDistance(_handle, mapType, distance);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetAnimationTimePoint<TAnimationData>(TAnimationData animationData, float timePoint) where TAnimationData : IAnimationData => Implementation.SetAnimationTimePoint(_handle, animationData, timePoint);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Model Instance {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(ModelInstance other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ModelInstance other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(ModelInstance left, ModelInstance right) => left.Equals(right);
	public static bool operator !=(ModelInstance left, ModelInstance right) => !left.Equals(right);
	#endregion
}