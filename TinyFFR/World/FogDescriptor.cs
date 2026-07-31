// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public enum FogDensity {
	Moderate,
	Thick,
	VeryThick,
	Thin,
	VeryThin
}

public readonly record struct FogDescriptor {
	public static readonly ColorVect DefaultColor = new(0.75f, 0.75f, 0.75f, 0.75f);
	public ColorVect Color { get; init; } = DefaultColor;
	public float DensityMultiplier { get; init; } = 1f;
	public float StartDistance { get; init; } = 3f;
	public float GroundLayerHeight { get; init; } = 0f;
	public float SkywardDensityFalloffMultiplier { get; init; } = 1f;
	public Direction SkywardDirection { get; init; } = Direction.Up;
	public float DirectionalLightScatteringStrengthMultiplier { get; init; } = 1f;
	public bool OccludesBackdrop { get; init; } = false;

	public FogDescriptor() { }
	public FogDescriptor(FogDensity density) : this(density, DefaultColor) { }
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
		}
	}
}