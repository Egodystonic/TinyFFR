// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Provides the implementation behind <see cref="Material"/>.
/// </summary>
public interface IMaterialImplProvider : IDisposableResourceImplProvider<Material> {
	/// <summary>
	/// Invoked via <see cref="Material.SupportsPerInstanceEffects"/>.
	/// </summary>
	bool GetSupportsPerInstanceEffects(ResourceHandle<Material> handle);
	/// <summary>
	/// Invoked via <see cref="Material.SupportsColorKeying"/>.
	/// </summary>
	bool GetSupportsColorKeying(ResourceHandle<Material> handle);
	/// <summary>
	/// Invoked via <see cref="Material.IsDefault"/>.
	/// </summary>
	bool GetIsDefault(ResourceHandle<Material> handle);
	/// <summary>
	/// Invoked internally to give an object its own copy of a shared material, so that per-object effects do not alter the original.
	/// </summary>
	Material Duplicate(ResourceHandle<Material> handle);
	/// <summary>
	/// Invoked via <see cref="Material.TryGetAssociatedTexture"/>.
	/// </summary>
	Texture? TryGetAssociatedTexture(ResourceHandle<Material> handle, ReadOnlySpan<char> parameterName);
	/// <summary>
	/// Invoked via <see cref="World.MaterialEffectController.SetTransform"/>.
	/// </summary>
	void SetEffectTransform(ResourceHandle<Material> handle, Transform2D newTransform);
	/// <summary>
	/// Invoked via <see cref="World.MaterialEffectController.SetBlendTexture"/>.
	/// </summary>
	void SetEffectBlendTexture(ResourceHandle<Material> handle, MaterialEffectMapType mapType, Texture mapTexture);
	/// <summary>
	/// Invoked via <see cref="World.MaterialEffectController.SetBlendDistance"/>.
	/// </summary>
	void SetEffectBlendDistance(ResourceHandle<Material> handle, MaterialEffectMapType mapType, float distance);
	/// <summary>
	/// Invoked internally to set how transparent an object using this material is drawn.
	/// </summary>
	void SetEffectOpacity(ResourceHandle<Material> handle, float opacity);
	/// <summary>
	/// Invoked internally to restrict an object's drawing to a rectangular region of the viewport.
	/// </summary>
	void SetScissorRect(ResourceHandle<Material> handle, XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions);
	/// <summary>
	/// Invoked internally to remove a previously set drawing restriction.
	/// </summary>
	void ClearScissorRect(ResourceHandle<Material> handle);
	/// <summary>
	/// Invoked internally to set the colour that one key of a colour-keyed material resolves to.
	/// </summary>
	void SetKeyedColor(ResourceHandle<Material> handle, ColorChannel key, ColorVect color);
}