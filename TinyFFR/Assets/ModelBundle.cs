// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// Every texture, material, mesh and model loaded from one composite file, plus the skeleton and animations its meshes share (if any).
/// </summary>
/// <remarks>
/// <para>
/// Bundles are returned by <c>IAssetLoader.LoadBundledAsset</c>. Pass one to <c>IObjectBuilder.CreateModelInstances</c> to create a
/// <see cref="World.ModelInstanceGroup"/> containing an instance of every model in it; the bundle's <see cref="Animations"/> can then be played on that whole group.
/// </para>
/// <para>
/// A bundle is a thin wrapper around a sealed <see cref="ResourceGroup"/> (see <see cref="UnderlyingResourceGroup"/>), which owns every resource in it. Disposing
/// the bundle disposes all of them.
/// </para>
/// </remarks>
public readonly struct ModelBundle : IDisposable, IStringSpanNameEnabled, IEquatable<ModelBundle>, IResourceWrapper<ModelBundle, ResourceGroup> {
	/// <summary>
	/// The <see cref="ResourceGroup"/> backing this bundle, which is what actually owns the contained resources.
	/// </summary>
	public ResourceGroup UnderlyingResourceGroup { get; }

	/// <summary>
	/// Every texture in this bundle.
	/// </summary>
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Texture> Textures {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Textures;
	}
	/// <summary>
	/// Every material in this bundle.
	/// </summary>
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Material> Materials {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Materials;
	}
	/// <summary>
	/// Every mesh in this bundle.
	/// </summary>
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Mesh> Meshes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingResourceGroup.Meshes;
	}
	/// <summary>
	/// Every model (pairing of one of this bundle's meshes with one of its materials) in this bundle.
	/// </summary>
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
	/// <summary>
	/// The animations shared by this bundle's skinned meshes, addressable by name, by position or by kind.
	/// </summary>
	/// <remarks>
	/// This is empty if the file had no skeletal animation data. Play these on the <see cref="World.ModelInstanceGroup"/> created from this bundle to animate every part
	/// together.
	/// </remarks>
	public MeshGroupAnimationIndex Animations => AnimationTable.Animations;
	/// <summary>
	/// The skeleton shared by this bundle's skinned meshes.
	/// </summary>
	/// <remarks>
	/// This has no joints if the file had no skeletal animation data.
	/// </remarks>
	public MeshGroupSkeleton Skeleton => AnimationTable.Skeleton;

	/// <summary>
	/// Constructs a new <see cref="ModelBundle"/> over an existing resource group.
	/// </summary>
	/// <remarks>
	/// This is mainly useful for restoring a bundle from a baked asset file: pass the group returned by <see cref="IAssetLoader.LoadBakedResourceGroup"/>. The
	/// bundle's animations are taken from the first <see cref="MeshGroupAnimationTable"/> in the group, if there is one.
	/// </remarks>
	/// <param name="underlyingResourceGroup">The resource group whose resources this bundle should expose.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="underlyingResourceGroup"/> has not been sealed.</exception>
	public ModelBundle(ResourceGroup underlyingResourceGroup) {
		if (!underlyingResourceGroup.IsSealed) throw new ArgumentException("Resource group must be sealed.", nameof(underlyingResourceGroup));
		UnderlyingResourceGroup = underlyingResourceGroup;
	}

	static ModelBundle IResourceWrapper<ModelBundle, ResourceGroup>.Wrap(ResourceGroup resource) => new(resource);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingResourceGroup.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingResourceGroup.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingResourceGroup.CopyName(destinationBuffer);

	/// <summary>
	/// Disposes this bundle and every resource in it.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingResourceGroup.Dispose();

	/// <inheritdoc />
	public override string ToString() => $"Model Bundle " + UnderlyingResourceGroup;

	#region Equality
	/// <inheritdoc />
	public bool Equals(ModelBundle other) => UnderlyingResourceGroup.Equals(other.UnderlyingResourceGroup);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ModelBundle other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingResourceGroup.GetHashCode();
	/// <summary>
	/// <see cref="Equals(ModelBundle)"/>
	/// </summary>
	public static bool operator ==(ModelBundle left, ModelBundle right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ModelBundle)"/>
	/// </summary>
	public static bool operator !=(ModelBundle left, ModelBundle right) => !left.Equals(right);
	#endregion
}
