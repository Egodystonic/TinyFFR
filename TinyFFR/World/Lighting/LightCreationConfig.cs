// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct LightCreationConfig {
	public static readonly float DefaultInitialBrightness = 1f;
	public static readonly Location DefaultInitialPosition = Location.Origin;
	public static readonly ColorVect DefaultInitialColor = StandardColor.White;

	public ReadOnlySpan<char> Name { get; init; }

	public Location InitialPosition { get; init; } = DefaultInitialPosition;

	public ColorVect InitialColor { get; init; } = DefaultInitialColor;

	public float InitialBrightness { get; init; } = DefaultInitialBrightness;

	public LightCreationConfig() { }

	internal void ThrowIfInvalid() {
		/* no op */
	}
}

public readonly ref struct PointLightCreationConfig {
	public static readonly float DefaultInitialMaxIlluminationRadius = 15f;

	public float InitialMaxIlluminationRadius { get; init; } = DefaultInitialMaxIlluminationRadius;

	#region Base Config
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	public static readonly Location DefaultInitialPosition = LightCreationConfig.DefaultInitialPosition;
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	public LightCreationConfig BaseConfig { get; private init; } = new();

	public Location InitialPosition {
		get => BaseConfig.InitialPosition;
		init => BaseConfig = BaseConfig with { InitialPosition = value };
	}

	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public PointLightCreationConfig() { }
	public PointLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}
}

public readonly ref struct SpotLightCreationConfig {
	public static readonly float DefaultInitialMaxIlluminationDistance = 15f;
	public static readonly bool DefaultIsHighAccuracy = false;
	public static readonly Direction DefaultInitialConeDirection = Direction.Down;
	public static readonly Angle DefaultInitialConeAngle = 70f;
	public static readonly Angle DefaultInitialIntenseBeamAngle = 15f;

	public float InitialMaxIlluminationDistance { get; init; } = DefaultInitialMaxIlluminationDistance;
	public bool IsHighAccuracy { get; init; } = DefaultIsHighAccuracy;
	public Direction InitialConeDirection { get; init; } = DefaultInitialConeDirection;
	public Angle InitialConeAngle { get; init; } = DefaultInitialConeAngle;
	public Angle InitialIntenseBeamAngle { get; init; } = DefaultInitialIntenseBeamAngle;

	#region Base Config
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	public static readonly Location DefaultInitialPosition = LightCreationConfig.DefaultInitialPosition;
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	public LightCreationConfig BaseConfig { get; private init; } = new();

	public Location InitialPosition {
		get => BaseConfig.InitialPosition;
		init => BaseConfig = BaseConfig with { InitialPosition = value };
	}

	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public SpotLightCreationConfig() { }
	public SpotLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}
}