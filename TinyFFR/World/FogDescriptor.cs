// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A preset describing how thick a scene's fog should be.
/// </summary>
/// <remarks>
/// Each value is shorthand for a whole set of <see cref="FogDescriptor"/> values chosen to look coherent together; construct a <see cref="FogDescriptor"/> from one
/// of these and then adjust individual properties if you want to depart from the preset.
/// </remarks>
public enum FogDensity {
	/// <summary>
	/// A middling amount of fog. This is the preset that matches <see cref="FogDescriptor"/>'s own default values.
	/// </summary>
	Moderate,
	/// <summary>
	/// Dense fog that begins close to the camera and hides the scene's backdrop.
	/// </summary>
	Thick,
	/// <summary>
	/// The densest preset: fog begins almost at the camera and hides the scene's backdrop entirely.
	/// </summary>
	VeryThick,
	/// <summary>
	/// Light fog that begins further from the camera, suggesting haze rather than obscuring the scene.
	/// </summary>
	Thin,
	/// <summary>
	/// The faintest preset; a barely-perceptible haze in the far distance.
	/// </summary>
	VeryThin
}

/// <summary>
/// Describes the fog in a scene: a haze that thickens with distance, obscuring far-away objects.
/// </summary>
/// <remarks>
/// As well as its obvious use for weather, fog is a useful way to give a scene a sense of depth and scale, and to hide the point at which distant geometry stops
/// being drawn. The simplest way to configure it is to construct one of these from a <see cref="FogDensity"/> preset and adjust from there.
/// </remarks>
public readonly record struct FogDescriptor {
	/// <summary>
	/// The default value for <see cref="Color"/>: a translucent grey, <c>(0.75, 0.75, 0.75, 0.75)</c>.
	/// </summary>
	public static readonly ColorVect DefaultColor = new(0.75f, 0.75f, 0.75f, 0.75f);
	/// <summary>
	/// The colour of the fog. Defaults to <see cref="DefaultColor"/>.
	/// </summary>
	/// <remarks>
	/// The colour's alpha component sets how completely the fog can obscure what is behind it, so a fully opaque colour eventually hides distant objects altogether
	/// whilst a translucent one only ever tints them.
	/// </remarks>
	public ColorVect Color { get; init; } = DefaultColor;
	/// <summary>
	/// How quickly the fog thickens with distance, where <c>1f</c> is the standard rate. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Larger values make objects disappear in to the fog over a shorter distance; smaller values stretch that transition out.
	/// </remarks>
	public float DensityMultiplier { get; init; } = 1f;
	/// <summary>
	/// How far from the camera the fog begins, in metres. Defaults to <c>3f</c>.
	/// </summary>
	/// <remarks>
	/// Nothing closer to the camera than this is affected at all, which keeps nearby objects looking crisp no matter how thick the fog is further out.
	/// </remarks>
	public float StartDistance { get; init; } = 3f;
	/// <summary>
	/// The height at which the fog is thickest, measured along <see cref="SkywardDirection"/>. Defaults to <c>0f</c>.
	/// </summary>
	/// <remarks>
	/// Together with <see cref="SkywardDensityFalloffMultiplier"/> this produces fog that pools at a particular altitude and thins out above it, in the way morning
	/// mist sits in a valley.
	/// </remarks>
	public float GroundLayerHeight { get; init; } = 0f;
	/// <summary>
	/// How quickly the fog thins out above <see cref="GroundLayerHeight"/>, where <c>1f</c> is the standard rate. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Larger values confine the fog to a shallower layer near the ground; smaller values let it extend further upwards. A value of <c>0f</c> removes the height
	/// falloff altogether, giving fog of the same thickness at every altitude.
	/// </remarks>
	public float SkywardDensityFalloffMultiplier { get; init; } = 1f;
	/// <summary>
	/// Which way is "up" for the purposes of <see cref="GroundLayerHeight"/> and <see cref="SkywardDensityFalloffMultiplier"/>. Defaults to <see cref="Direction.Up"/>.
	/// </summary>
	public Direction SkywardDirection { get; init; } = Direction.Up;
	/// <summary>
	/// How strongly directional lights appear to scatter through the fog, where <c>1f</c> is the standard amount. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Scattering is what makes fog glow when you look towards the sun and stay flat and grey when you look away from it. Raising this exaggerates that effect;
	/// setting it to <c>0f</c> makes the fog look the same in every direction.
	/// </remarks>
	public float DirectionalLightScatteringStrengthMultiplier { get; init; } = 1f;
	/// <summary>
	/// Whether the fog is drawn over the scene's backdrop as well as over the objects in it. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// The backdrop is effectively infinitely distant, so when this is <see langword="true"/> the fog covers it completely, replacing the sky with a wall of fog.
	/// Leave it <see langword="false"/> for fog that objects fade in to whilst the sky remains visible above.
	/// </remarks>
	public bool OccludesBackdrop { get; init; } = false;

	/// <summary>
	/// Constructs a new <see cref="FogDescriptor"/> with default values for every property.
	/// </summary>
	public FogDescriptor() { }
	/// <summary>
	/// Constructs a new <see cref="FogDescriptor"/> configured according to the given density preset, using <see cref="DefaultColor"/>.
	/// </summary>
	/// <param name="density">The preset to base this descriptor's properties on.</param>
	public FogDescriptor(FogDensity density) : this(density, DefaultColor) { }
	/// <summary>
	/// Constructs a new <see cref="FogDescriptor"/> configured according to the given density preset, using the given colour.
	/// </summary>
	/// <remarks>
	/// Each preset sets the alpha component of <see cref="Color"/> itself, so only the colour component of <paramref name="color"/> is used.
	/// </remarks>
	/// <param name="density">The preset to base this descriptor's properties on.</param>
	/// <param name="color">The colour to use for the fog.</param>
	public FogDescriptor(FogDensity density, ColorVect color) : this() {
		switch (density) {
			case FogDensity.VeryThin: {
				Color = color with { Alpha = 0.55f };
				DensityMultiplier = 0.5f;
				StartDistance = 7.5f;
				DirectionalLightScatteringStrengthMultiplier = 0.333f;
				break;
			}
			case FogDensity.Thin: {
				Color = color with { Alpha = 0.65f };
				DensityMultiplier = 0.75f;
				StartDistance = 5f;
				DirectionalLightScatteringStrengthMultiplier = 0.666f;
				break;
			}
			case FogDensity.Thick: {
				Color = color with { Alpha = 0.85f };
				DensityMultiplier = 1.25f;
				StartDistance = 1.25f;
				DirectionalLightScatteringStrengthMultiplier = 1.5f;
				SkywardDensityFalloffMultiplier = 0.5f;
				OccludesBackdrop = true;
				break;
			}
			case FogDensity.VeryThick: {
				Color = color with { Alpha = 1f };
				DensityMultiplier = 1.5f;
				StartDistance = 0.5f;
				DirectionalLightScatteringStrengthMultiplier = 2f;
				SkywardDensityFalloffMultiplier = 0.2f;
				OccludesBackdrop = true;
				break;
			}
			default: {
				Color = color with { Alpha = DefaultColor.Alpha };
				break;
			}
		}
	}
}