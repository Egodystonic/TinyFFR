// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Describes how much of the scene a transmissive surface is allowed to reflect and refract.
/// </summary>
/// <remarks>
/// A renderer's own quality settings can disable reflections altogether, in which case they override whichever
/// value is chosen here.
/// </remarks>
public enum TransmissiveMaterialQuality {
	/// <summary>
	/// The surface reflects and refracts the other objects in the scene as well as its backdrop. This is the default.
	/// </summary>
	FullReflectionsAndRefraction,
	/// <summary>
	/// The surface reflects and refracts only the scene's backdrop, ignoring the objects in it.
	/// </summary>
	/// <remarks>
	/// Considerably cheaper to render than <see cref="FullReflectionsAndRefraction"/>, and often indistinguishable from it for
	/// surfaces that mostly show the sky.
	/// </remarks>
	SkyboxOnlyReflectionsAndRefraction,
}

/// <summary>
/// Describes how a <see cref="TransmissiveMaterialCreationConfig"/>'s colour map alpha channel is interpreted when the surface is drawn.
/// </summary>
/// <remarks>
/// <para>
/// This only matters for materials whose colour map actually has an alpha channel; a fully opaque map looks the same either way.
/// </para>
/// <para>
/// Note that this has nothing to do with the see-through effect a transmissive
/// material produces — that comes from the absorption-transmission map, not from alpha on the colour map.
/// </para>
/// </remarks>
public enum TransmissiveMaterialAlphaMode {
	/// <summary>
	/// Alpha genuinely blends the surface with whatever is behind it, so partially-transparent texels tint the scene rather than disappearing. This is the default for transmissive materials.
	/// </summary>
	/// <remarks>
	/// This is more expensive to render than <see cref="MaskOnly"/>, and expects the colour map's alpha to be premultiplied.
	/// </remarks>
	FullBlending,
	/// <summary>
	/// Alpha simply switches texels on and off: any texel below 40% alpha is not drawn at all, and every other texel is drawn fully opaque.
	/// </summary>
	/// <remarks>
	/// This is the cheaper of the two modes and is what you want for surfaces that are solid but with holes in them, such as
	/// foliage or a chain-link fence. It also does not require alpha-ordering which can be prone to "flickering".
	/// </remarks>
	MaskOnly,
}

/// <summary>
/// Describes a physically-based material for a surface that light passes through, such as glass, water or a gemstone.
/// </summary>
/// <remarks>
/// <para>
/// Use this wherever a surface must genuinely refract or reflect its surroundings in real time, rather than merely being
/// translucent. <see cref="ColorMap"/> and <see cref="AbsorptionTransmissionMap"/> are both required; every other map is
/// optional.
/// </para>
/// <para>
/// Transmissive materials are markedly more expensive to render than standard ones, and stacking several transmissive objects
/// in front of each other is not currently supported — doing so tends to make one or more of them disappear behind the others.
/// </para>
/// </remarks>
public readonly ref struct TransmissiveMaterialCreationConfig : IConfigStruct<TransmissiveMaterialCreationConfig> {
	/// <summary>
	/// The default value for <see cref="RefractionThickness"/>: <c>0.1f</c>.
	/// </summary>
	public static readonly float DefaultRefractionThickness = 0.1f;
	/// <summary>
	/// The default value for <see cref="AlphaMode"/>: <see cref="TransmissiveMaterialAlphaMode.FullBlending"/>.
	/// </summary>
	public static readonly TransmissiveMaterialAlphaMode DefaultAlphaMode = TransmissiveMaterialAlphaMode.FullBlending;
	/// <summary>
	/// The default value for <see cref="Quality"/>: <see cref="TransmissiveMaterialQuality.FullReflectionsAndRefraction"/>.
	/// </summary>
	public static readonly TransmissiveMaterialQuality DefaultQuality = TransmissiveMaterialQuality.FullReflectionsAndRefraction;

	static char[]? _colorMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the colour map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> ColorMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _colorMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamColorMap);

	static char[]? _absorptionTransmissionMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the absorption-transmission map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> AbsorptionTransmissionMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _absorptionTransmissionMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamAbsorptionTransmissionMap);

	static char[]? _normalMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the normal map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> NormalMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _normalMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamNormalMap);

	static char[]? _occlusionRoughnessMetallicReflectanceMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the ORMR map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> OcclusionRoughnessMetallicReflectanceMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _occlusionRoughnessMetallicReflectanceMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamOrmMap);

	static char[]? _anisotropyMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the anisotropy map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> AnisotropyMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _anisotropyMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamAnisotropyMap);

	static char[]? _emissiveMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the emissive map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through the material
	/// builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> EmissiveMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _emissiveMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamEmissiveMap);

	/// <summary>
	/// The texture supplying the surface's base colour. This property is required.
	/// </summary>
	/// <remarks>
	/// This tints whatever passes through and reflects off the surface; it is not what makes the surface see-through, which is
	/// <see cref="AbsorptionTransmissionMap"/>'s job.
	/// </remarks>
	public required Texture ColorMap { get; init; }
	/// <summary>
	/// The texture describing how light passes through the surface: Which colours it absorbs, and how much light gets through at all. This property is required.
	/// </summary>
	/// <remarks>
	/// The colour channels give the absorption, so what is <i>not</i> absorbed is what is seen through the surface — an absorption
	/// of pure yellow lets only blue light through. The fourth channel gives how much light gets through at all, where the maximum
	/// value is fully transparent and zero is opaque.
	/// </remarks>
	public required Texture AbsorptionTransmissionMap { get; init; }
	/// <summary>
	/// The texture describing the surface's small-scale bumps and grooves, or <see langword="null"/> for a perfectly smooth surface. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// On a transmissive surface this distorts what is seen through it as well as how light reflects off it, which is what makes
	/// old or textured glass look the way it does. Expected in the OpenGL convention.
	/// </remarks>
	public Texture? NormalMap { get; init; }
	/// <summary>
	/// The texture supplying occlusion, roughness, metallic and reflectance values, or <see langword="null"/> for none. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// Unlike a standard material, this must be a four-channel ORMR texture; a three-channel ORM texture is rejected. Most
	/// transmissive surfaces want reflectance data anyway.
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
	/// Must be a four-channel texture, the fourth channel being how strongly each texel glows.
	/// </remarks>
	public Texture? EmissiveMap { get; init; }
	/// <summary>
	/// How thick the surface's material is modelled as being, in metres. Defaults to <see cref="DefaultRefractionThickness"/>: <c>0.1f</c>. Must be positive.
	/// </summary>
	/// <remarks>
	/// Together with <see cref="AbsorptionTransmissionMap"/> this determines how much light is lost passing through: the further
	/// light has to travel through the material, the more of it is absorbed. Set it to roughly how far a typical ray would travel
	/// inside the object before emerging — metres to centimetres for something solid like an acrylic block, centimetres to
	/// millimetres for something thin like a pane of glass.
	/// </remarks>
	public float RefractionThickness { get; init; } = DefaultRefractionThickness;
	/// <summary>
	/// How much of the scene the surface is allowed to reflect and refract. Defaults to <see cref="DefaultQuality"/>: <see cref="TransmissiveMaterialQuality.FullReflectionsAndRefraction"/>.
	/// </summary>
	/// <remarks>
	/// A renderer's own quality settings can disable reflections and refractions altogether, which overrides whatever is set here.
	/// </remarks>
	public TransmissiveMaterialQuality Quality { get; init; } = DefaultQuality;
	/// <summary>
	/// How the colour map's alpha channel is interpreted. Defaults to <see cref="DefaultAlphaMode"/>: <see cref="TransmissiveMaterialAlphaMode.FullBlending"/>.
	/// </summary>
	/// <remarks>
	/// This is unrelated to the surface's see-through effect, which comes from <see cref="AbsorptionTransmissionMap"/>.
	/// </remarks>
	public TransmissiveMaterialAlphaMode AlphaMode { get; init; } = DefaultAlphaMode;

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
	/// Enabling this lets each object using the material transform its textures or blend them towards a second set. It makes the
	/// material markedly more expensive to render whether or not any object actually uses those effects, so leave it off unless it
	/// is needed. This is <see cref="MaterialCreationConfig.EnablePerInstanceEffects"/> on <see cref="BaseConfig"/>, surfaced
	/// here for convenience.
	/// </remarks>
	public bool EnablePerInstanceEffects {
		get => BaseConfig.EnablePerInstanceEffects;
		init => BaseConfig = BaseConfig with { EnablePerInstanceEffects = value };
	}

	/// <summary>
	/// Constructs a new <see cref="TransmissiveMaterialCreationConfig"/> with default values for every property except <see cref="ColorMap"/> and <see cref="AbsorptionTransmissionMap"/>, which must be supplied.
	/// </summary>
	public TransmissiveMaterialCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="TransmissiveMaterialCreationConfig"/> from the given common settings.
	/// </summary>
	/// <param name="baseConfig">The value for <see cref="BaseConfig"/>.</param>
	public TransmissiveMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (AbsorptionTransmissionMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(AbsorptionTransmissionMap));
		if (!Enum.IsDefined(Quality)) throw new ArgumentOutOfRangeException(nameof(Quality), Quality, null);
		if (!Enum.IsDefined(AlphaMode)) throw new ArgumentOutOfRangeException(nameof(AlphaMode), AlphaMode, null);
		MaterialCreationConfig.ThrowIfTextureIsNotCorrectTexelType(EmissiveMap, TexelType.Rgba32);
		MaterialCreationConfig.ThrowIfTextureIsNotCorrectTexelType(OcclusionRoughnessMetallicReflectanceMap, TexelType.Rgba32);
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TransmissiveMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfResource() // AbsorptionTransmissionMap
			+	SerializationSizeOfNullableResource() // NormalMap
			+	SerializationSizeOfNullableResource() // OcclusionRoughnessMetallicReflectanceMap
			+	SerializationSizeOfNullableResource() // AnisotropyMap
			+	SerializationSizeOfNullableResource() // EmissiveMap
			+	SerializationSizeOfFloat() // RefractionThickness
			+	SerializationSizeOfInt() // Quality
			+	SerializationSizeOfInt() // AlphaMode
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TransmissiveMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteAndAllocateResource(ref dest, src.AbsorptionTransmissionMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.NormalMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.OcclusionRoughnessMetallicReflectanceMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.AnisotropyMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.EmissiveMap);
		SerializationWriteFloat(ref dest, src.RefractionThickness);
		SerializationWriteInt(ref dest, (int) src.Quality);
		SerializationWriteInt(ref dest, (int) src.AlphaMode);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc />
	public static TransmissiveMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TransmissiveMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			AbsorptionTransmissionMap = SerializationReadResource<Texture>(ref src),
			NormalMap = SerializationReadNullableResource<Texture>(ref src),
			OcclusionRoughnessMetallicReflectanceMap = SerializationReadNullableResource<Texture>(ref src),
			AnisotropyMap = SerializationReadNullableResource<Texture>(ref src),
			EmissiveMap = SerializationReadNullableResource<Texture>(ref src),
			RefractionThickness = SerializationReadFloat(ref src),
			Quality = (TransmissiveMaterialQuality) SerializationReadInt(ref src),
			AlphaMode = (TransmissiveMaterialAlphaMode) SerializationReadInt(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeResourceHandle(src[SerializationSizeOfResource()..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() * 2)..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 1))..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 2))..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 3))..]);
	}
}