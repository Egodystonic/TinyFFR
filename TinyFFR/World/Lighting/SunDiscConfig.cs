// Created on 2025-05-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Describes the visible disc a <see cref="DirectionalLight"/> draws in the sky, in the way the sun appears as a bright disc rather than merely lighting the scene.
/// </summary>
/// <remarks>
/// Each value scales the corresponding feature relative to its physically-plausible size, so leaving them all at <c>1f</c> gives a sun-like disc; raising them
/// exaggerates it for stylistic effect.
/// </remarks>
public readonly record struct SunDiscConfig {
	/// <summary>
	/// How large the disc itself is, where <c>1f</c> is its natural size. Defaults to <c>1f</c>.
	/// </summary>
	public float Scaling { get; init; } = 1f;
	/// <summary>
	/// How pronounced the halo of light around the disc is, where <c>1f</c> is its natural strength. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// This is the bloom that surrounds a bright light source seen through an atmosphere or a lens. Setting it to <c>0f</c> leaves a hard-edged disc with no glow.
	/// </remarks>
	public float FringingScaling { get; init; } = 1f;
	/// <summary>
	/// How far the halo around the disc spreads outward, where <c>1f</c> is its natural extent. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Where <see cref="FringingScaling"/> controls how bright the halo is, this controls how wide it is.
	/// </remarks>
	public float FringingOuterRadiusScaling { get; init; } = 1f;

	/// <summary>
	/// Constructs a new <see cref="SunDiscConfig"/> with default values for every setting.
	/// </summary>
	public SunDiscConfig() { }
}