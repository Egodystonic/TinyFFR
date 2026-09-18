using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Identifies one of the texture map slots on a material that can be blended towards a second texture at runtime via a
/// model instance's <see cref="MaterialEffectController"/>.
/// </summary>
public enum MaterialEffectMapType {
	/// <summary>
	/// The material's colour (albedo/diffuse) map, which supplies the base colour of the surface.
	/// </summary>
	Color = 0,
#pragma warning disable CA1069 // Duplicate constants -- is deliberate, ORM/ORMR is set the same way internally
	/// <summary>
	/// The material's occlusion/roughness/metallic map.
	/// </summary>
	/// <remarks>
	/// ORM and ORMR maps occupy the same slot, so this value and <see cref="OcclusionRoughnessMetallicReflectance"/> are
	/// interchangeable.
	/// </remarks>
	OcclusionRoughnessMetallic = 1,
	/// <summary>
	/// The material's occlusion/roughness/metallic/reflectance map.
	/// </summary>
	/// <remarks>
	/// ORM and ORMR maps occupy the same slot, so this value and <see cref="OcclusionRoughnessMetallic"/> are
	/// interchangeable.
	/// </remarks>
	OcclusionRoughnessMetallicReflectance = 1,
#pragma warning restore CA1069
	/// <summary>
	/// The material's emissive map, which makes parts of the surface appear to glow with their own light.
	/// </summary>
	Emissive = 2,
	/// <summary>
	/// The material's absorption-transmission map, which controls how light passes through a transmissive (see-through) surface.
	/// </summary>
	AbsorptionTransmission = 3
}
