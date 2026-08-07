// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum TransmissiveMaterialQuality {
	FullReflectionsAndRefraction,
	SkyboxOnlyReflectionsAndRefraction,
}

public enum TransmissiveMaterialAlphaMode {
	FullBlending,
	MaskOnly,
}

public readonly ref struct TransmissiveMaterialCreationConfig : IConfigStruct<TransmissiveMaterialCreationConfig> {
	public static readonly float DefaultRefractionThickness = 0.1f;
	public static readonly TransmissiveMaterialAlphaMode DefaultAlphaMode = TransmissiveMaterialAlphaMode.FullBlending;
	public static readonly TransmissiveMaterialQuality DefaultQuality = TransmissiveMaterialQuality.FullReflectionsAndRefraction;

	static char[]? _colorMapParameterString = null;
	public static ReadOnlySpan<char> ColorMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _colorMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamColorMap);

	static char[]? _absorptionTransmissionMapParameterString = null;
	public static ReadOnlySpan<char> AbsorptionTransmissionMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _absorptionTransmissionMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamAbsorptionTransmissionMap);

	static char[]? _normalMapParameterString = null;
	public static ReadOnlySpan<char> NormalMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _normalMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamNormalMap);

	static char[]? _occlusionRoughnessMetallicReflectanceMapParameterString = null;
	public static ReadOnlySpan<char> OcclusionRoughnessMetallicReflectanceMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _occlusionRoughnessMetallicReflectanceMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamOrmMap);

	static char[]? _anisotropyMapParameterString = null;
	public static ReadOnlySpan<char> AnisotropyMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _anisotropyMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamAnisotropyMap);

	static char[]? _emissiveMapParameterString = null;
	public static ReadOnlySpan<char> EmissiveMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _emissiveMapParameterString, LocalShaderPackageConstants.TransmissiveMaterialShader.ParamEmissiveMap);

	public required Texture ColorMap { get; init; }
	public required Texture AbsorptionTransmissionMap { get; init; }
	public Texture? NormalMap { get; init; }
	public Texture? OcclusionRoughnessMetallicReflectanceMap { get; init; }
	public Texture? AnisotropyMap { get; init; }
	public Texture? EmissiveMap { get; init; }
	public float RefractionThickness { get; init; } = DefaultRefractionThickness;
	public TransmissiveMaterialQuality Quality { get; init; } = DefaultQuality;
	public TransmissiveMaterialAlphaMode AlphaMode { get; init; } = DefaultAlphaMode;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}
	public bool EnablePerInstanceEffects {
		get => BaseConfig.EnablePerInstanceEffects;
		init => BaseConfig = BaseConfig with { EnablePerInstanceEffects = value };
	}

	public TransmissiveMaterialCreationConfig() { }
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
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeResourceHandle(src[SerializationSizeOfResource()..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() * 2)..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 1))..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 2))..]);
		SerializationDisposeNullableResourceHandle(src[((SerializationSizeOfResource() * 2) + (SerializationSizeOfNullableResource() * 3))..]);
	}
}