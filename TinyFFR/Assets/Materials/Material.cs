// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// The appearance of a surface: i.e. which textures it uses and thus how it responds to light. Created via the factory's <see cref="IMaterialBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// A material describes appearance only, never shape; pairing it with a <see cref="Meshes.Mesh"/> gives a
/// <see cref="Model"/>. One material can be shared by any number of objects, which is markedly cheaper than giving each its
/// own.
/// </para>
/// <para>
/// Materials are assembled from textures by the material builder. Dispose a material when nothing uses it any more, and before
/// disposing the textures it uses.
/// </para>
/// </remarks>
public readonly struct Material : IDisposableResource<Material, IMaterialImplProvider> {
	readonly ResourceHandle<Material> _handle;
	readonly IMaterialImplProvider _impl;

	internal ResourceHandle<Material> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Material)) : _handle;
	internal IMaterialImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Material>();

	IMaterialImplProvider IResource<Material, IMaterialImplProvider>.Implementation => Implementation;
	ResourceHandle<Material> IResource<Material>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// Whether objects using this material can alter it individually at runtime.
	/// </summary>
	/// <remarks>
	/// This is fixed when the material is created. When it is <see langword="false"/>, <see cref="ModelInstance.MaterialEffects"/>
	/// will always return <c>null</c>.
	/// </remarks>
	public bool SupportsPerInstanceEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSupportsPerInstanceEffects(_handle);
	}
	
	/// <summary>
	/// Whether this material's colours are selected by a key texture rather than taken directly from a colour map.
	/// </summary>
	/// <remarks>
	/// When <c>true</c>, model instances can use <see cref="ModelInstance.SetKeyedMaterialColor"/> to set their individual colour values.
	/// </remarks>
	public bool SupportsColorKeying {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSupportsColorKeying(_handle);
	}
	
	/// <summary>
	/// Returns <c>true</c> if this is the <see cref="IMaterialBuilder.DefaultMaterial"/>.
	/// </summary>
	public bool IsDefault {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsDefault(_handle);
	}

	internal Material(ResourceHandle<Material> handle, IMaterialImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <summary>
	/// Returns the texture for the given map-type (<paramref name="parameterName"/>) this material was created with, or <see langword="null"/> if that parameter has no texture.
	/// </summary>
	/// <remarks>
	/// Parameter names are available as constants on each material creation config, such as
	/// <see cref="StandardMaterialCreationConfig.ColorMapParameterString"/> or <see cref="TransmissiveMaterialCreationConfig.AbsorptionTransmissionMapParameterString"/>, etc.
	/// </remarks>
	/// <param name="parameterName">The shader parameter to look up.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Texture? TryGetAssociatedTexture(ReadOnlySpan<char> parameterName) => Implementation.TryGetAssociatedTexture(_handle, parameterName);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Material Duplicate() => Implementation.Duplicate(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectTransform(Transform2D transform) => Implementation.SetEffectTransform(_handle, transform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendTexture(MaterialEffectMapType mapType, Texture blendTex) => Implementation.SetEffectBlendTexture(_handle, mapType, blendTex);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendDistance(MaterialEffectMapType mapType, float distance) => Implementation.SetEffectBlendDistance(_handle, mapType, distance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectOpacity(float opacity) => Implementation.SetEffectOpacity(_handle, opacity);

	internal void SetScissorRect(XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions) => Implementation.SetScissorRect(_handle, viewportRelativeBottomLeftOffset, dimensions);
	internal void ClearScissorRect() => Implementation.ClearScissorRect(_handle);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetKeyedColor(ColorChannel key, ColorVect color) => Implementation.SetKeyedColor(_handle, key, color);

	static Material IResource<Material>.CreateFromHandleAndImpl(ResourceHandle<Material> handle, IResourceImplProvider impl) {
		return new Material(handle, impl as IMaterialImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Material> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Material> IResource<Material>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this material, releasing its GPU resources.
	/// </summary>
	/// <remarks>
	/// Every object using this material must be disposed first. The textures the material uses are not disposed, and must be
	/// disposed afterwards.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Material {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Material other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Material other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given materials are the same material.
	/// </summary>
	/// <param name="left">The first material to compare.</param>
	/// <param name="right">The second material to compare.</param>
	public static bool operator ==(Material left, Material right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given materials are different materials.
	/// </summary>
	/// <param name="left">The first material to compare.</param>
	/// <param name="right">The second material to compare.</param>
	public static bool operator !=(Material left, Material right) => !left.Equals(right);
	#endregion
}