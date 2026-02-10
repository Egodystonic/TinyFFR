// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

#pragma warning disable CA1710 // "Rename to ModelInstanceStack/Queue/List etc" -- Compiler is being overly aggressive because this implements IReadOnlyCollection; but in this case 'Group' is better aligned with other names in TinyFFR
public readonly struct ModelInstanceGroup : ITransformedSceneObject, IDisposable, IStringSpanNameEnabled, IReadOnlyCollection<ModelInstance>, IEquatable<ModelInstanceGroup> {
#pragma warning restore CA1710
	public ResourceGroup UnderlyingResourceGroup { get; }
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance> Instances { get; }
	public int Count { get; }
	
	ModelInstance? FirstInstance => Count > 0 ? Instances[0] : null;

	public ModelInstanceGroup(ResourceGroup underlyingResourceGroup) {
		if (!underlyingResourceGroup.IsSealed) throw new ArgumentException("Resource group must be sealed.", nameof(underlyingResourceGroup));
		UnderlyingResourceGroup = underlyingResourceGroup;
		Instances = UnderlyingResourceGroup.ModelInstances;
		Count = Instances.Count;
	}

	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Transform ?? Transform.None;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetTransform(value);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Position ?? Location.Origin;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetPosition(value);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Rotation ?? Rotation.None;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetRotation(value);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Scaling ?? Vect.One;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetScaling(value);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;

	public void MoveBy(Vect translation) {
		for (var i = 0; i < Count; ++i) Instances[i].MoveBy(translation);
	}
	public void RotateBy(Rotation rotation) {
		for (var i = 0; i < Count; ++i) Instances[i].RotateBy(rotation);
	}
	public void ScaleBy(float scalar) {
		for (var i = 0; i < Count; ++i) Instances[i].ScaleBy(scalar);
	}
	public void ScaleBy(Vect vect) {
		for (var i = 0; i < Count; ++i) Instances[i].ScaleBy(vect);
	}
	public void AdjustScaleBy(float scalar) {
		for (var i = 0; i < Count; ++i) Instances[i].AdjustScaleBy(scalar);
	}
	public void AdjustScaleBy(Vect vect) {
		for (var i = 0; i < Count; ++i) Instances[i].AdjustScaleBy(vect);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingResourceGroup.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingResourceGroup.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingResourceGroup.CopyName(destinationBuffer);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<ModelInstance> IEnumerable<ModelInstance>.GetEnumerator() => GetEnumerator();
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance>.Enumerator GetEnumerator() => Instances.GetEnumerator();

	#region Disposal
	public void Dispose() => UnderlyingResourceGroup.Dispose();
	public void Dispose(bool disposeContainedInstances) => UnderlyingResourceGroup.Dispose(disposeContainedInstances);
	#endregion

	public override string ToString() => $"Model Instance " + UnderlyingResourceGroup;

	#region Equality
	public bool Equals(ModelInstanceGroup other) => UnderlyingResourceGroup.Equals(other.UnderlyingResourceGroup);
	public override bool Equals(object? obj) => obj is ModelInstanceGroup other && Equals(other);
	public override int GetHashCode() => UnderlyingResourceGroup.GetHashCode();
	public static bool operator ==(ModelInstanceGroup left, ModelInstanceGroup right) => left.Equals(right);
	public static bool operator !=(ModelInstanceGroup left, ModelInstanceGroup right) => !left.Equals(right);
	#endregion
}