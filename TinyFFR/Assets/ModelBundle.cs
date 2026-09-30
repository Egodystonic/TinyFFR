// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets;

public readonly struct ModelBundle : IDisposable, IStringSpanNameEnabled, IEquatable<ModelBundle>, IResourceWrapper<ModelBundle, ResourceGroup> {
	public ResourceGroup UnderlyingResourceGroup { get; }

	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Texture> Textures {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Textures;
	}
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Material> Materials {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Materials;
	}
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Mesh> Meshes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Meshes;
	}
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> Models {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Models;
	}

	internal MeshGroupAnimationTable AnimationTable {
		get {
			var tables = UnderlyingResourceGroup.AnimationTables;
			return tables.Count > 0 ? tables[0] : MeshGroupAnimationTable.Empty;
		}
	}
	public MeshGroupAnimationIndex Animations => AnimationTable.Animations;
	public MeshGroupSkeleton Skeleton => AnimationTable.Skeleton;

	public ModelBundle(ResourceGroup underlyingResourceGroup) {
		if (!underlyingResourceGroup.IsSealed) throw new ArgumentException("Resource group must be sealed.", nameof(underlyingResourceGroup));
		UnderlyingResourceGroup = underlyingResourceGroup;
	}

	static ModelBundle IResourceWrapper<ModelBundle, ResourceGroup>.Wrap(ResourceGroup resource) => new(resource);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingResourceGroup.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingResourceGroup.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingResourceGroup.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingResourceGroup.Dispose();

	public override string ToString() => $"Model Bundle " + UnderlyingResourceGroup;

	#region Equality
	public bool Equals(ModelBundle other) => UnderlyingResourceGroup.Equals(other.UnderlyingResourceGroup);
	public override bool Equals(object? obj) => obj is ModelBundle other && Equals(other);
	public override int GetHashCode() => UnderlyingResourceGroup.GetHashCode();
	public static bool operator ==(ModelBundle left, ModelBundle right) => left.Equals(right);
	public static bool operator !=(ModelBundle left, ModelBundle right) => !left.Equals(right);
	#endregion
}
