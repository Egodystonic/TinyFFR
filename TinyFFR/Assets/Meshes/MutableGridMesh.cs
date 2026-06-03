// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MutableGridMesh : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridMesh> {
	public Mesh UnderlyingMesh { get; } 
	public XYPair<int> GridDimensions { get; }
	public XYPair<float> CellSize { get; }

	internal MutableGridMesh(Mesh underlyingMesh, XYPair<int> gridDimensions, XYPair<float> cellSize) {
		UnderlyingMesh = underlyingMesh;
		GridDimensions = gridDimensions;
		CellSize = cellSize;
	}
	
	public int GetVertexIndex(XYPair<int> coordinate, bool clampCoord = true) {
		
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingMesh.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingMesh.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingMesh.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingMesh.Dispose();

	public override string ToString() => $"{GridDimensions.X}x{GridDimensions.Y} Mutable Grid {UnderlyingMesh}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Mesh(MutableGridMesh operand) => operand.UnderlyingMesh;

	#region Equality
	public bool Equals(MutableGridMesh other) => UnderlyingMesh.Equals(other.UnderlyingMesh);
	public override bool Equals(object? obj) => obj is MutableGridMesh other && Equals(other);
	public override int GetHashCode() => UnderlyingMesh.GetHashCode();
	public static bool operator ==(MutableGridMesh left, MutableGridMesh right) => left.Equals(right);
	public static bool operator !=(MutableGridMesh left, MutableGridMesh right) => !left.Equals(right);
	#endregion
}

public readonly struct MutableGridInstance : IDisposable, IStringSpanNameEnabled, IEquatable<MutableGridInstance> {
	public ModelInstance UnderlyingModelInstance { get; }
	public MutableGridMesh ParentGridMesh { get; init; }

	public MutableGridInstance(ModelInstance underlyingModelInstance, MutableGridMesh parentGridMesh) {
		UnderlyingModelInstance = underlyingModelInstance;
		ParentGridMesh = parentGridMesh;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	public MaterialEffectController? MaterialEffects {
		get {
			if (!Material.SupportsPerInstanceEffects) return null;
			return new MaterialEffectController(this);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetMaterialEffectTransform(Transform2D newTransform) => Implementation.SetMaterialEffectTransform(_handle, newTransform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendTexture(MaterialEffectMapType mapType, Texture texture) => Implementation.SetMaterialEffectBlendTexture(_handle, mapType, texture);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendDistance(MaterialEffectMapType mapType, float distance) => Implementation.SetMaterialEffectBlendDistance(_handle, mapType, distance);
	
	// TODO above and also vertex mutation
	// TODO and then separately to keep it simple, dynamic texture alteration (if exists)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingModelInstance.Dispose();

	public override string ToString() => $"{ParentGridMesh.GridDimensions.X}x{ParentGridMesh.GridDimensions.Y} Mutable Grid {UnderlyingModelInstance}";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ModelInstance(MutableGridInstance operand) => operand.UnderlyingModelInstance;

	#region Equality
	public bool Equals(MutableGridInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is MutableGridInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(MutableGridInstance left, MutableGridInstance right) => left.Equals(right);
	public static bool operator !=(MutableGridInstance left, MutableGridInstance right) => !left.Equals(right);
	#endregion
}