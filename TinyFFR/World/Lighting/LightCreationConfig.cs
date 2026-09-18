// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Configuration common to creating a light of any kind.
/// </summary>
public readonly ref struct LightCreationConfig : IConfigStruct<LightCreationConfig> {
	/// <summary>
	/// The default value for <see cref="InitialBrightness"/>: <c>1f</c>.
	/// </summary>
	public static readonly float DefaultInitialBrightness = 1f;
	/// <summary>
	/// The default value for <see cref="InitialColor"/>: <see cref="StandardColor.White"/>.
	/// </summary>
	public static readonly ColorVect DefaultInitialColor = StandardColor.White;
	/// <summary>
	/// The default value for <see cref="CastsShadows"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultCastsShadows = false;

	/// <summary>
	/// Optional name for the new light.
	/// </summary>
	/// <summary>
	/// Optional name for the new light.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// The colour the new light should emit. Defaults to <see cref="DefaultInitialColor"/>.
	/// </summary>
	/// <summary>
	/// The colour the new light should emit. Defaults to <see cref="DefaultInitialColor"/>.
	/// </summary>
	public ColorVect InitialColor { get; init; } = DefaultInitialColor;

	/// <summary>
	/// How much light the new light should emit, where <c>1f</c> is the default strength for its kind. Defaults to <see cref="DefaultInitialBrightness"/>.
	/// </summary>
	/// <summary>
	/// How much light the new light should emit, where <c>1f</c> is the default strength for its kind. Defaults to <see cref="DefaultInitialBrightness"/>.
	/// </summary>
	public float InitialBrightness { get; init; } = DefaultInitialBrightness;

	/// <summary>
	/// Whether objects lit by the new light should cast shadows from it. Defaults to <see cref="DefaultCastsShadows"/>.
	/// </summary>
	/// <summary>
	/// Whether objects lit by the new light should cast shadows from it. Defaults to <see cref="DefaultCastsShadows"/>.
	/// </summary>
	public bool CastsShadows { get; init; } = DefaultCastsShadows;

	/// <summary>
	/// Constructs a new <see cref="LightCreationConfig"/> with default values for every setting.
	/// </summary>
	public LightCreationConfig() { }

	internal void ThrowIfInvalid() {
		/* no op */
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in LightCreationConfig src) {
		return	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOf<ColorVect>() // InitialColor
			+	SerializationSizeOfFloat() // InitialBrightness
			+	SerializationSizeOfBool(); // CastsShadows
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in LightCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWrite(ref dest, src.InitialColor);
		SerializationWriteFloat(ref dest, src.InitialBrightness);
		SerializationWriteBool(ref dest, src.CastsShadows);
	}
	/// <inheritdoc/>
	public static LightCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Name = SerializationReadString(ref src),
			InitialColor = SerializationRead<ColorVect>(ref src),
			InitialBrightness = SerializationReadFloat(ref src),
			CastsShadows = SerializationReadBool(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Configuration for creating a new <see cref="PointLight"/>.
/// </summary>
public readonly ref struct PointLightCreationConfig : IConfigStruct<PointLightCreationConfig> {
	/// <summary>
	/// The default value for <see cref="InitialPosition"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location DefaultInitialPosition = Location.Origin;
	/// <summary>
	/// The default value for <see cref="InitialMaxIlluminationRadius"/>: <c>15f</c> metres.
	/// </summary>
	public static readonly float DefaultInitialMaxIlluminationRadius = 15f;

	/// <summary>
	/// How far the new light should reach, in metres. Defaults to <see cref="DefaultInitialMaxIlluminationRadius"/>.
	/// </summary>
	public float InitialMaxIlluminationRadius { get; init; } = DefaultInitialMaxIlluminationRadius;
	/// <summary>
	/// Where the new light should be. Defaults to <see cref="DefaultInitialPosition"/>.
	/// </summary>
	public Location InitialPosition { get; init; } = DefaultInitialPosition;
	
	#region Base Config
	/// <summary>
	/// The default value for <see cref="InitialBrightness"/>: <c>1f</c>.
	/// </summary>
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	/// <summary>
	/// The default value for <see cref="InitialColor"/>: <see cref="StandardColor.White"/>.
	/// </summary>
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	/// <summary>
	/// The default value for <see cref="CastsShadows"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	/// <summary>
	/// The configuration common to all lights, regardless of kind.
	/// </summary>
	public LightCreationConfig BaseConfig { get; private init; } = new();

	/// <summary>
	/// The colour the new light should emit. Defaults to <see cref="DefaultInitialColor"/>.
	/// </summary>
	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	/// <summary>
	/// How much light the new light should emit, where <c>1f</c> is the default strength for its kind. Defaults to <see cref="DefaultInitialBrightness"/>.
	/// </summary>
	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	/// <summary>
	/// Whether objects lit by the new light should cast shadows from it. Defaults to <see cref="DefaultCastsShadows"/>.
	/// </summary>
	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
	}

	/// <summary>
	/// Optional name for the new light.
	/// </summary>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Constructs a new <see cref="PointLightCreationConfig"/> with default values for every setting.
	/// </summary>
	public PointLightCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="PointLightCreationConfig"/> that takes its common settings from <paramref name="baseConfig"/> and uses default values for the point light-specific ones.
	/// </summary>
	/// <param name="baseConfig">The configuration to use as this config's <see cref="BaseConfig"/>.</param>
	public PointLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in PointLightCreationConfig src) {
		return	SerializationSizeOfFloat() // InitialMaxIlluminationRadius
			+	SerializationSizeOf<Location>() // InitialPosition
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in PointLightCreationConfig src) {
		SerializationWriteFloat(ref dest, src.InitialMaxIlluminationRadius);
		SerializationWrite(ref dest, src.InitialPosition);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc/>
	public static PointLightCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			InitialMaxIlluminationRadius = SerializationReadFloat(ref src),
			InitialPosition = SerializationRead<Location>(ref src),
			BaseConfig = SerializationReadSubConfig<LightCreationConfig>(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Configuration for creating a new <see cref="SpotLight"/>.
/// </summary>
public readonly ref struct SpotLightCreationConfig : IConfigStruct<SpotLightCreationConfig> {
	/// <summary>
	/// The default value for <see cref="InitialPosition"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location DefaultInitialPosition = Location.Origin;
	/// <summary>
	/// The default value for <see cref="InitialMaxIlluminationDistance"/>: <c>15f</c> metres.
	/// </summary>
	public static readonly float DefaultInitialMaxIlluminationDistance = 15f;
	/// <summary>
	/// The default value for <see cref="IsHighQuality"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultIsHighQuality = false;
	/// <summary>
	/// The default value for <see cref="InitialConeDirection"/>: <see cref="Direction.Down"/>.
	/// </summary>
	public static readonly Direction DefaultInitialConeDirection = Direction.Down;
	/// <summary>
	/// The default value for <see cref="InitialConeAngle"/>: <c>70°</c>.
	/// </summary>
	public static readonly Angle DefaultInitialConeAngle = 70f;
	/// <summary>
	/// The default value for <see cref="InitialIntenseBeamAngle"/>: <c>15°</c>.
	/// </summary>
	public static readonly Angle DefaultInitialIntenseBeamAngle = 15f;

	/// <summary>
	/// Where the new light should be. Defaults to <see cref="DefaultInitialPosition"/>.
	/// </summary>
	public Location InitialPosition { get; init; } = DefaultInitialPosition;
	/// <summary>
	/// How far down its cone the new light should reach, in metres. Defaults to <see cref="DefaultInitialMaxIlluminationDistance"/>.
	/// </summary>
	public float InitialMaxIlluminationDistance { get; init; } = DefaultInitialMaxIlluminationDistance;
	/// <summary>
	/// Whether the new light should be rendered at higher quality, at the cost of performance. Defaults to <see cref="DefaultIsHighQuality"/>.
	/// </summary>
	public bool IsHighQuality { get; init; } = DefaultIsHighQuality;
	/// <summary>
	/// Which way the new light should point. Defaults to <see cref="DefaultInitialConeDirection"/>.
	/// </summary>
	public Direction InitialConeDirection { get; init; } = DefaultInitialConeDirection;
	/// <summary>
	/// How wide the new light’s cone should be, measured as its full width. Defaults to <see cref="DefaultInitialConeAngle"/>.
	/// </summary>
	public Angle InitialConeAngle { get; init; } = DefaultInitialConeAngle;
	/// <summary>
	/// How wide the fully-lit centre of the new light’s cone should be, measured as its full width. Defaults to <see cref="DefaultInitialIntenseBeamAngle"/>.
	/// </summary>
	public Angle InitialIntenseBeamAngle { get; init; } = DefaultInitialIntenseBeamAngle;

	#region Base Config
	/// <summary>
	/// The default value for <see cref="InitialBrightness"/>: <c>1f</c>.
	/// </summary>
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	/// <summary>
	/// The default value for <see cref="InitialColor"/>: <see cref="StandardColor.White"/>.
	/// </summary>
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	/// <summary>
	/// The default value for <see cref="CastsShadows"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	/// <summary>
	/// The configuration common to all lights, regardless of kind.
	/// </summary>
	public LightCreationConfig BaseConfig { get; private init; } = new();

	/// <summary>
	/// The colour the new light should emit. Defaults to <see cref="DefaultInitialColor"/>.
	/// </summary>
	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	/// <summary>
	/// How much light the new light should emit, where <c>1f</c> is the default strength for its kind. Defaults to <see cref="DefaultInitialBrightness"/>.
	/// </summary>
	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	/// <summary>
	/// Whether objects lit by the new light should cast shadows from it. Defaults to <see cref="DefaultCastsShadows"/>.
	/// </summary>
	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
	}

	/// <summary>
	/// Optional name for the new light.
	/// </summary>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Constructs a new <see cref="SpotLightCreationConfig"/> with default values for every setting.
	/// </summary>
	public SpotLightCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="SpotLightCreationConfig"/> that takes its common settings from <paramref name="baseConfig"/> and uses default values for the spot light-specific ones.
	/// </summary>
	/// <param name="baseConfig">The configuration to use as this config's <see cref="BaseConfig"/>.</param>
	public SpotLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in SpotLightCreationConfig src) {
		return	SerializationSizeOf<Location>() // InitialPosition
			+	SerializationSizeOfFloat() // InitialMaxIlluminationDistance
			+	SerializationSizeOfBool() // IsHighQuality
			+	SerializationSizeOf<Direction>() // InitialConeDirection
			+	SerializationSizeOf<Angle>() // InitialConeAngle
			+	SerializationSizeOf<Angle>() // InitialIntenseBeamAngle
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in SpotLightCreationConfig src) {
		SerializationWrite(ref dest, src.InitialPosition);
		SerializationWriteFloat(ref dest, src.InitialMaxIlluminationDistance);
		SerializationWriteBool(ref dest, src.IsHighQuality);
		SerializationWrite(ref dest, src.InitialConeDirection);
		SerializationWrite(ref dest, src.InitialConeAngle);
		SerializationWrite(ref dest, src.InitialIntenseBeamAngle);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc/>
	public static SpotLightCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
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
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Configuration for creating a new <see cref="DirectionalLight"/>.
/// </summary>
public readonly ref struct DirectionalLightCreationConfig : IConfigStruct<DirectionalLightCreationConfig> {
	/// <summary>
	/// The default value for <see cref="ShowSunDisc"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultShowSunDisc = false;
	/// <summary>
	/// The default value for <see cref="InitialDirection"/>: mostly downward and slightly forward, approximating afternoon sunlight.
	/// </summary>
	public static readonly Direction DefaultInitialDirection = new(0f, -1f, 0.3f);

	/// <summary>
	/// Whether the new light should draw a visible disc in the sky where it comes from. Defaults to <see cref="DefaultShowSunDisc"/>.
	/// </summary>
	public bool ShowSunDisc { get; init; } = DefaultShowSunDisc;
	/// <summary>
	/// The direction the new light’s rays should travel in. Defaults to <see cref="DefaultInitialDirection"/>.
	/// </summary>
	public Direction InitialDirection { get; init; } = DefaultInitialDirection;

	#region Base Config
	/// <summary>
	/// The default value for <see cref="InitialBrightness"/>: <c>1f</c>.
	/// </summary>
	public static readonly float DefaultInitialBrightness = LightCreationConfig.DefaultInitialBrightness;
	/// <summary>
	/// The default value for <see cref="InitialColor"/>: <see cref="StandardColor.White"/>.
	/// </summary>
	public static readonly ColorVect DefaultInitialColor = LightCreationConfig.DefaultInitialColor;
	/// <summary>
	/// The default value for <see cref="CastsShadows"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultCastsShadows = LightCreationConfig.DefaultCastsShadows;
	/// <summary>
	/// The configuration common to all lights, regardless of kind.
	/// </summary>
	public LightCreationConfig BaseConfig { get; private init; } = new();

	/// <summary>
	/// The colour the new light should emit. Defaults to <see cref="DefaultInitialColor"/>.
	/// </summary>
	public ColorVect InitialColor {
		get => BaseConfig.InitialColor;
		init => BaseConfig = BaseConfig with { InitialColor = value };
	}

	/// <summary>
	/// How much light the new light should emit, where <c>1f</c> is the default strength for its kind. Defaults to <see cref="DefaultInitialBrightness"/>.
	/// </summary>
	public float InitialBrightness {
		get => BaseConfig.InitialBrightness;
		init => BaseConfig = BaseConfig with { InitialBrightness = value };
	}

	/// <summary>
	/// Whether objects lit by the new light should cast shadows from it. Defaults to <see cref="DefaultCastsShadows"/>.
	/// </summary>
	public bool CastsShadows {
		get => BaseConfig.CastsShadows;
		init => BaseConfig = BaseConfig with { CastsShadows = value };
	}

	/// <summary>
	/// Optional name for the new light.
	/// </summary>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Constructs a new <see cref="DirectionalLightCreationConfig"/> with default values for every setting.
	/// </summary>
	public DirectionalLightCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="DirectionalLightCreationConfig"/> that takes its common settings from <paramref name="baseConfig"/> and uses default values for the directional light-specific ones.
	/// </summary>
	/// <param name="baseConfig">The configuration to use as this config's <see cref="BaseConfig"/>.</param>
	public DirectionalLightCreationConfig(LightCreationConfig baseConfig) => BaseConfig = baseConfig;
	#endregion

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in DirectionalLightCreationConfig src) {
		return	SerializationSizeOfBool() // ShowSunDisc
			+	SerializationSizeOf<Direction>() // InitialDirection
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in DirectionalLightCreationConfig src) {
		SerializationWriteBool(ref dest, src.ShowSunDisc);
		SerializationWrite(ref dest, src.InitialDirection);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc/>
	public static DirectionalLightCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			ShowSunDisc = SerializationReadBool(ref src),
			InitialDirection = SerializationRead<Direction>(ref src),
			BaseConfig = SerializationReadSubConfig<LightCreationConfig>(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}