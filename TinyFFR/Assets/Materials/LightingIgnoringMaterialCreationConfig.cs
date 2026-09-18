// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Describes a simple material that shows its colour map exactly as given, unaffected by any light in the scene.
/// </summary>
/// <remarks>
/// <para>
/// Because nothing about the lighting matters, a surface using one of these looks the same however the scene is lit. That makes it
/// the right choice for diagnostic overlays, non-realistic workflows, and anything that must stay legible regardless of what is
/// going on around it.
/// </para>
/// <para>
/// Only a colour map is supported. Where the colour map has an alpha channel, the surface always blends with the scene rather
/// than being masked.
/// </para>
/// </remarks>
public readonly ref struct LightingIgnoringMaterialCreationConfig : IConfigStruct<LightingIgnoringMaterialCreationConfig> {
	static char[]? _colorMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the colour map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through
	/// the material builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> ColorMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _colorMapParameterString, LocalShaderPackageConstants.LightingIgnoringMaterialShader.ParamColorMap);

	/// <summary>
	/// The texture supplying the surface's colour. This property is required. Can be three or four channel.
	/// </summary>
	public required Texture ColorMap { get; init; }

	/// <summary>
	/// The settings common to every kind of material, such as its name.
	/// </summary>
	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	/// <summary>
	/// The name to give the material. May be left empty.
	/// </summary>
	/// <remarks>
	/// This is <see cref="MaterialCreationConfig.Name"/> on <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}
	/// <summary>
	/// Whether to allow objects using this material to alter it individually at runtime. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Enabling this lets each object using the material transform its textures or blend them towards a second set. It makes
	/// the material markedly more expensive to render whether or not any object actually uses those effects, so leave it off
	/// unless it is needed. This is <see cref="MaterialCreationConfig.EnablePerInstanceEffects"/> on
	/// <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public bool EnablePerInstanceEffects {
		get => BaseConfig.EnablePerInstanceEffects;
		init => BaseConfig = BaseConfig with { EnablePerInstanceEffects = value };
	}

	/// <summary>
	/// Constructs a new <see cref="LightingIgnoringMaterialCreationConfig"/> with default values for every property except <see cref="ColorMap"/>, which must be supplied.
	/// </summary>
	public LightingIgnoringMaterialCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="LightingIgnoringMaterialCreationConfig"/> from the given common settings.
	/// </summary>
	/// <param name="baseConfig">The value for <see cref="BaseConfig"/>.</param>
	public LightingIgnoringMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in LightingIgnoringMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in LightingIgnoringMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc />
	public static LightingIgnoringMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new LightingIgnoringMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
	}
}