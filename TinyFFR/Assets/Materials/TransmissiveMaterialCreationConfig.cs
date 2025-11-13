// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum TransmissiveMaterialQuality {
	SkyboxReflectionsOnly = LocalShaderPackageConstants.TransmissiveMaterialShaderConstants.RefractionQualityVariant.Disabled,
	SkyboxReflectionsAndRefraction = LocalShaderPackageConstants.TransmissiveMaterialShaderConstants.RefractionQualityVariant.Low,
	TrueReflectionsAndRefraction = LocalShaderPackageConstants.TransmissiveMaterialShaderConstants.RefractionQualityVariant.High,
}

public enum TransmissiveMaterialAlphaMode {
	MaskOnly = LocalShaderPackageConstants.StandardMaterialShaderConstants.AlphaModeVariant.AlphaOn,
	FullBlending = LocalShaderPackageConstants.StandardMaterialShaderConstants.AlphaModeVariant.AlphaOnBlended,
}

public readonly ref struct TransmissiveMaterialCreationConfig : IConfigStruct<TransmissiveMaterialCreationConfig> {
	public required Texture ColorMap { get; init; }
	public required Texture AbsorptionTransmissionMap { get; init; }
	public Texture? NormalMap { get; init; }
	public Texture? OcclusionRoughnessMetallicReflectanceMap { get; init; }
	public Texture? AnisotropyMap { get; init; }
	public Texture? EmissiveMap { get; init; }
	public float RefractionThickness { get; init; } = 0.1f;
	public TransmissiveMaterialQuality Quality { get; init; }
	public TransmissiveMaterialAlphaMode AlphaMode { get; init; } = TransmissiveMaterialAlphaMode.MaskOnly;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public TransmissiveMaterialCreationConfig() { }
	public TransmissiveMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (AbsorptionTransmissionMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(AbsorptionTransmissionMap));
		if (!Enum.IsDefined(Quality)) throw new ArgumentOutOfRangeException(nameof(Quality), Quality, null);
		if (!Enum.IsDefined(AlphaMode)) throw new ArgumentOutOfRangeException(nameof(AlphaMode), AlphaMode, null);
		MaterialCreationConfig.ThrowIfTextureIsNotCorrectTexelType(AnisotropyMap, TexelType.Rgba32);
		MaterialCreationConfig.ThrowIfTextureIsNotCorrectTexelType(EmissiveMap, TexelType.Rgba32);
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