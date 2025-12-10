// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum StandardMaterialAlphaMode {
	MaskOnly,
	FullBlending,
}

public readonly ref struct StandardMaterialCreationConfig : IConfigStruct<StandardMaterialCreationConfig> {
	public static readonly StandardMaterialAlphaMode DefaultAlphaMode = StandardMaterialAlphaMode.MaskOnly;

	public required Texture ColorMap { get; init; }
	public Texture? NormalMap { get; init; }
	public Texture? OcclusionRoughnessMetallicMap {
		get => OcclusionRoughnessMetallicReflectanceMap;
		init => OcclusionRoughnessMetallicReflectanceMap = value;
	}
	public Texture? OcclusionRoughnessMetallicReflectanceMap { get; init; }
	public Texture? AnisotropyMap { get; init; }
	public Texture? EmissiveMap { get; init; }
	public Texture? ClearCoatMap { get; init; }
	public StandardMaterialAlphaMode AlphaMode { get; init; } = DefaultAlphaMode;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}
	public bool EnablePerInstanceEffects {
		get => BaseConfig.EnablePerInstanceEffects;
		init => BaseConfig = BaseConfig with { EnablePerInstanceEffects = value };
	}

	public StandardMaterialCreationConfig() { }
	public StandardMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		MaterialCreationConfig.ThrowIfTextureIsNotCorrectTexelType(EmissiveMap, TexelType.Rgba32);
		if (!Enum.IsDefined(AlphaMode)) throw new ArgumentOutOfRangeException(nameof(AlphaMode), AlphaMode, null);
	}

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
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeNullableResourceHandle(src[SerializationSizeOfResource()..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 1))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 2))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 3))..]);
		SerializationDisposeNullableResourceHandle(src[(SerializationSizeOfResource() + (SerializationSizeOfNullableResource() * 4))..]);
	}
}