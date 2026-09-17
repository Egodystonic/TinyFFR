// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Represents a scene object that is rendered with a <see cref="Material"/>.
/// </summary>
/// <remarks>
/// A material describes what an object's surface is made of — its colour, how rough or shiny it is, how metallic it looks, and so on — and is therefore what
/// determines how the object responds to the lights around it.
/// </remarks>
public interface IMaterialUsingSceneObject {
	/// <summary>
	/// The material this object's surface is rendered with.
	/// </summary>
	Material Material { get; set; }
	/// <summary>
	/// A handle for adjusting this object's per-instance material effects, or <see langword="null"/> if its current <see cref="Material"/> does not support them.
	/// </summary>
	/// <remarks>
	/// Per-instance effects let one object's appearance be altered without affecting anything else sharing the same material. Because support depends on the
	/// material currently assigned, this returns <see langword="null"/> whenever <see cref="Material"/> has been set to one that does not offer them, and the value
	/// should be re-read after changing the material rather than cached. This can be enabled via <see cref="MaterialCreationConfig.EnablePerInstanceEffects"/>.
	/// </remarks>
	MaterialEffectController? MaterialEffects { get; }
	/// <summary>
	/// Sets the render colour for this object to <paramref name="baseColor"/>.
	/// </summary>
	/// <remarks>
	/// This also changes the <see cref="Material"/> for this object to the <see cref="IMaterialBuilder.DefaultMaterial">DefaultMaterial</see> if is not already.
	/// </remarks>
	/// <param name="baseColor">The colour to paint this object.</param>
	void SetDefaultMaterialBaseColor(ColorVect baseColor);
	/// <summary>
	/// Sets the shading style for this object.
	/// </summary>
	/// <remarks>
	/// This also changes the <see cref="Material"/> for this object to the <see cref="IMaterialBuilder.DefaultMaterial">DefaultMaterial</see> if is not already.
	/// </remarks>
	/// <param name="style">The shading style to draw this object with.</param>
	void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style);
}