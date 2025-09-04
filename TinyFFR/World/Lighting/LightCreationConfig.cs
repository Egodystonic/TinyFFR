// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct LightCreationConfig : IConfigStruct<LightCreationConfig> {
	public static readonly float DefaultInitialBrightness = 1f;
	public static readonly ColorVect DefaultInitialColor = StandardColor.White;
	public static readonly bool DefaultCastsShadows = false;

	public ReadOnlySpan<char> Name { get; init; }

	public ColorVect InitialColor { get; init; } = DefaultInitialColor;

	public float InitialBrightness { get; init; } = DefaultInitialBrightness;

	public bool CastsShadows { get; init; } = DefaultCastsShadows;

	public LightCreationConfig() { }

	internal void ThrowIfInvalid() {
		/* no op */
	}

	public static int GetHeapStorageFormattedLength(in LightCreationConfig src) {
		return	SerializationSizeOf(src.Name)
			+	SerializationSizeOf(src.InitialColor)
			+	SerializationSizeOf(src.InitialBrightness)
			+	SerializationSizeOf(src.CastsShadows);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in LightCreationConfig src) {
		SerializationWrite(ref dest, src.Name);
		SerializationWrite(ref dest, src.InitialColor);
		SerializationWrite(ref dest, src.InitialBrightness);
		SerializationWrite(ref dest, src.CastsShadows);
	}
	public static LightCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new() {
			Name = SerializationReadString(ref src),
			InitialColor = SerializationRead<ColorVect>(ref src),
			InitialBrightness = SerializationReadFloat(ref src),
			CastsShadows = SerializationReadBool(ref src)
		};
	}
}

public readonly ref struct PointLightCreationConfig : IConfigStruct<PointLightCreationConfig> {
	public static readonly Location DefaultInitialPosition = Location.Origin;
	public static readonly float DefaultInitialMaxIlluminationRadius = 15f;

	public float InitialMaxIlluminationRadius { get; init; } = DefaultInitialMaxIlluminationRadius;
	public Location InitialPosition { get; init; } = DefaultInitialPosition;
	
	#region Base Config
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	public LightCreationConfig BaseConfig { get; private init; } = new();

	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
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

	public static int GetHeapStorageFormattedLength(in PointLightCreationConfig src) {
		return	SerializationSizeOf(src.InitialMaxIlluminationRadius)
			+	SerializationSizeOf(src.InitialPosition)
			+	SerializationSizeOf(src.BaseConfig);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in PointLightCreationConfig src) {
		SerializationWrite(ref dest, src.InitialMaxIlluminationRadius);
		SerializationWrite(ref dest, src.InitialPosition);
		SerializationWrite(ref dest, src.BaseConfig);
	}
	public static PointLightCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new() {
			InitialMaxIlluminationRadius = SerializationReadFloat(ref src),
			InitialPosition = SerializationRead<Location>(ref src),
			BaseConfig = SerializationReadSubConfig<LightCreationConfig>(ref src)
		};
	}
}

public readonly ref struct SpotLightCreationConfig : IConfigStruct<SpotLightCreationConfig> {
	public static readonly Location DefaultInitialPosition = Location.Origin;
	public static readonly float DefaultInitialMaxIlluminationDistance = 15f;
	public static readonly bool DefaultIsHighQuality = false;
	public static readonly Direction DefaultInitialConeDirection = Direction.Down;
	public static readonly Angle DefaultInitialConeAngle = 70f;
	public static readonly Angle DefaultInitialIntenseBeamAngle = 15f;

	public Location InitialPosition { get; init; } = DefaultInitialPosition;
	public float InitialMaxIlluminationDistance { get; init; } = DefaultInitialMaxIlluminationDistance;
	public bool IsHighQuality { get; init; } = DefaultIsHighQuality;
	public Direction InitialConeDirection { get; init; } = DefaultInitialConeDirection;
	public Angle InitialConeAngle { get; init; } = DefaultInitialConeAngle;
	public Angle InitialIntenseBeamAngle { get; init; } = DefaultInitialIntenseBeamAngle;

	#region Base Config
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	public LightCreationConfig BaseConfig { get; private init; } = new();

	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
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

	public static int GetHeapStorageFormattedLength(in SpotLightCreationConfig src) {
		return	SerializationSizeOf(src.InitialPosition)
			+	SerializationSizeOf(src.InitialMaxIlluminationDistance)
			+	SerializationSizeOf(src.IsHighQuality)
			+	SerializationSizeOf(src.InitialConeDirection)
			+	SerializationSizeOf(src.InitialConeAngle)
			+	SerializationSizeOf(src.InitialIntenseBeamAngle)
			+	SerializationSizeOf(src.BaseConfig);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in SpotLightCreationConfig src) {
		SerializationWrite(ref dest, src.InitialPosition);
		SerializationWrite(ref dest, src.InitialMaxIlluminationDistance);
		SerializationWrite(ref dest, src.IsHighQuality);
		SerializationWrite(ref dest, src.InitialConeDirection);
		SerializationWrite(ref dest, src.InitialConeAngle);
		SerializationWrite(ref dest, src.InitialIntenseBeamAngle);
		SerializationWrite(ref dest, src.BaseConfig);
	}
	public static SpotLightCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new() {
			InitialPosition = SerializationRead<Location>(ref src),
			InitialMaxIlluminationDistance = SerializationReadFloat(ref src),
			IsHighQuality = SerializationReadBool(ref src),
			InitialConeDirection = SerializationRead<Direction>(ref src),
			InitialConeAngle = SerializationRead<Angle>(ref src),
			InitialIntenseBeamAngle = SerializationRead<Angle>(ref src),
			BaseConfig = SerializationReadSubConfig<LightCreationConfig>(ref src)
		};
	}
}

public readonly ref struct DirectionalLightCreationConfig : IConfigStruct<DirectionalLightCreationConfig> {
	public static readonly bool DefaultShowSunDisc = false;
	public static readonly Direction DefaultInitialDirection = new(0f, -1f, 0.3f);

	public bool ShowSunDisc { get; init; } = DefaultShowSunDisc;
	public Direction InitialDirection { get; init; } = DefaultInitialDirection;

	#region Base Config
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	public LightCreationConfig BaseConfig { get; private init; } = new();

	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
	}

	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public DirectionalLightCreationConfig() { }
	public DirectionalLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in DirectionalLightCreationConfig src) {
		return	SerializationSizeOf(src.ShowSunDisc)
			+	SerializationSizeOf(src.InitialDirection)
			+	SerializationSizeOf(src.BaseConfig);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in DirectionalLightCreationConfig src) {
		SerializationWrite(ref dest, src.ShowSunDisc);
		SerializationWrite(ref dest, src.InitialDirection);
		SerializationWrite(ref dest, src.BaseConfig);
	}
	public static DirectionalLightCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new() {
			ShowSunDisc = SerializationReadBool(ref src),
			InitialDirection = SerializationRead<Direction>(ref src),
			BaseConfig = SerializationReadSubConfig<LightCreationConfig>(ref src)
		};
	}
}