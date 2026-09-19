// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Describes how a <see cref="StandardMaterialCreationConfig"/>'s colour map alpha channel is interpreted when the colour-map
/// texture is four-channel.
/// </summary>
/// <remarks>
/// This only matters for materials whose colour map actually has an alpha channel; a fully opaque map looks the same either way.
/// </remarks>
public enum StandardMaterialAlphaMode {
	/// <summary>
	/// Alpha simply switches texels on and off: any texel below 40% alpha is not drawn at all, and every other texel is drawn
	/// fully opaque. This is the default for standard materials.
	/// </summary>
	/// <remarks>
	/// This is the cheaper of the two modes and is what you want for surfaces that are solid but with holes in them, such as
	/// foliage or a chain-link fence. It also does not require alpha-ordering which can be prone to "flickering".
	/// </remarks>
	MaskOnly,
	/// <summary>
	/// Alpha genuinely blends the surface with whatever is behind it, so partially-transparent texels tint the scene rather than
	/// disappearing.
	/// </summary>
	/// <remarks>
	/// This is more expensive to render than <see cref="MaskOnly"/>, and expects the colour map's alpha to be premultiplied.
	/// </remarks>
	FullBlending,
}

/// <summary>
/// Describes a physically-based material for an opaque surface, which is what most objects in a scene use.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="ColorMap"/> is required; every other map refines how the surface responds to light and may be left
/// <see langword="null"/>. Each map supplied makes the material more expensive to render, so supply only those that are
/// actually needed; colour, normal and ORM covers most surfaces.
/// </para>
/// <para>
/// Every map type is supported except absorption-transmission, which is what <see cref="TransmissiveMaterialCreationConfig"/>
/// is for.
/// </para>
/// </remarks>
public readonly ref struct StandardMaterialCreationConfig : IConfigStruct<StandardMaterialCreationConfig> {
	/// <summary>
	/// The default value for <see cref="AlphaMode"/>: <see cref="StandardMaterialAlphaMode.MaskOnly"/>.
	/// </summary>
	public static readonly StandardMaterialAlphaMode DefaultAlphaMode = StandardMaterialAlphaMode.MaskOnly;

	static char[]? _colorMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the colour map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> ColorMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _colorMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamColorMap);

	static char[]? _normalMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the normal map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> NormalMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _normalMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamNormalMap);

	static char[]? _occlusionRoughnessMetallicReflectanceMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the ORM/ORMR map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> OcclusionRoughnessMetallicReflectanceMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _occlusionRoughnessMetallicReflectanceMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamOrmMap);
	/// <summary>
	/// The name the shader uses for the ORM/ORMR map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> OcclusionRoughnessMetallicMapParameterString => OcclusionRoughnessMetallicReflectanceMapParameterString;

	static char[]? _anisotropyMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the anisotropy map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> AnisotropyMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _anisotropyMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamAnisotropyMap);

	static char[]? _emissiveMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the emissive map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> EmissiveMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _emissiveMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamEmissiveMap);

	static char[]? _clearCoatMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the clearcoat map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> ClearCoatMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _clearCoatMapParameterString, LocalShaderPackageConstants.StandardMaterialShader.ParamClearCoatMap);

	/// <summary>
	/// The texture supplying the surface's base colour. This property is required.
	/// </summary>
	/// <remarks>
	/// This is the only map a material must have; every other one refines how the surface responds to light rather than what
	/// colour it is.
	/// </remarks>
	public required Texture ColorMap { get; init; }
	/// <summary>
	/// The texture describing the surface's small-scale bumps and grooves, or <see langword="null"/> for a perfectly smooth surface. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// This is what makes light catch on detail that the geometry itself does not have (the mortar lines in brickwork, say,
	/// without modelling each brick).
	/// </remarks>
	public Texture? NormalMap { get; init; }
	/// <summary>
	/// The texture supplying occlusion, roughness and metallic values, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// ORM and ORMR maps occupy the same slot, so this and <see cref="OcclusionRoughnessMetallicReflectanceMap"/> are the same
	/// property under two names. Supplying a three-channel ORM map leaves reflectance at its default.
	/// </remarks>
	public Texture? OcclusionRoughnessMetallicMap {
		get => OcclusionRoughnessMetallicReflectanceMap;
		init => OcclusionRoughnessMetallicReflectanceMap = value;
	}
	/// <summary>
	/// The texture supplying occlusion, roughness, metallic and reflectance values, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// ORM and ORMR maps occupy the same slot, so this and <see cref="OcclusionRoughnessMetallicMap"/> are the same property
	/// under two names; either a three-channel or a four-channel texture is accepted here.
	/// </remarks>
	public Texture? OcclusionRoughnessMetallicReflectanceMap { get; init; }
	/// <summary>
	/// The texture describing where the surface reflects light unevenly in different directions, as brushed metal does, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	public Texture? AnisotropyMap { get; init; }
	/// <summary>
	/// The texture describing where the surface appears to glow with its own light, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// Either a three-channel or a four-channel texture is accepted. A four-channel texture's fourth channel sets how strongly
	/// each texel glows; a three-channel texture glows at full strength wherever it is not black.
	/// </remarks>
	public Texture? EmissiveMap { get; init; }
	/// <summary>
	/// The texture describing a thin glossy layer over the top of the surface, like lacquer or wax, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	public Texture? ClearCoatMap { get; init; }
	
	/// <summary>
	/// How the colour map's alpha channel is interpreted. Defaults to <see cref="DefaultAlphaMode"/>: <see cref="StandardMaterialAlphaMode.MaskOnly"/>.
	/// </summary>
	/// <remarks>
	/// This only matters for a colour map that actually has an alpha channel.
	/// </remarks>
	public StandardMaterialAlphaMode AlphaMode { get; init; } = DefaultAlphaMode;

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
	/// Constructs a new <see cref="StandardMaterialCreationConfig"/> with default values for every property except <see cref="ColorMap"/>, which must be supplied.
	/// </summary>
	public StandardMaterialCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="StandardMaterialCreationConfig"/> from the given common settings.
	/// </summary>
	/// <param name="baseConfig">The value for <see cref="BaseConfig"/>.</param>
	public StandardMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (!Enum.IsDefined(AlphaMode)) throw new ArgumentOutOfRangeException(nameof(AlphaMode), AlphaMode, null);
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in StandardMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfNullableResource() // NormalMap
			+	SerializationSizeOfNullableResource() // OcclusionRoughnessMetallicReflectanceMap
			+	SerializationSizeOfNullableResource() // AnisotropyMap
			+	SerializationSizeOfNullableResource() // EmissiveMap
			+	SerializationSizeOfNullableResource() // ClearCoatMap
			+	SerializationSizeOfInt() // AlphaMode
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in StandardMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.NormalMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.OcclusionRoughnessMetallicReflectanceMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.AnisotropyMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.EmissiveMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.ClearCoatMap);
		SerializationWriteInt(ref dest, (int) src.AlphaMode);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc />
	public static StandardMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new StandardMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			NormalMap = SerializationReadNullableResource<Texture>(ref src),
			OcclusionRoughnessMetallicReflectanceMap = SerializationReadNullableResource<Texture>(ref src),
			AnisotropyMap = SerializationReadNullableResource<Texture>(ref src),
			EmissiveMap = SerializationReadNullableResource<Texture>(ref src),
			ClearCoatMap = SerializationReadNullableResource<Texture>(ref src),
			AlphaMode = (StandardMaterialAlphaMode) SerializationReadInt(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeNullableResourceHandle(src[SerializationSizeOfResource()..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 1))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 2))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 3))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 4))..]);
	}
}