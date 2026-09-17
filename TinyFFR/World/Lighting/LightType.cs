// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Identifies which kind of light a <see cref="Light"/> actually is.
/// </summary>
public enum LightType {
	/// <summary>
	/// The light is not one of the kinds named by this enum or is invalid/unspecified.
	/// </summary>
	Unknown,
	/// <summary>
	/// A <see cref="PointLight"/>: light radiating outward in every direction from a single point, like a bare bulb.
	/// </summary>
	Point,
	/// <summary>
	/// A <see cref="SpotLight"/>: light radiating from a single point but confined to a cone, like a torch or a stage spotlight.
	/// </summary>
	Spot,
	/// <summary>
	/// A <see cref="DirectionalLight"/>: light arriving from one direction across the whole scene, like sunlight.
	/// </summary>
	Directional
}