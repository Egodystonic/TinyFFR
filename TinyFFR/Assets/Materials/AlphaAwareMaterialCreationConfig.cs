// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct AlphaAwareMaterialCreationConfig : IConfigStruct<AlphaAwareMaterialCreationConfig> {
	public static readonly AlphaMaterialType DefaultType = AlphaMaterialType.Standard;

	public required Texture ColorMap { get; init; }
	public required Texture NormalMap { get; init; }
	public required Texture OrmMap { get; init; }
	public AlphaMaterialType Type { get; init; } = DefaultType;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public AlphaAwareMaterialCreationConfig() { }
	public AlphaAwareMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
		if (NormalMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(NormalMap));
		if (OrmMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(OrmMap));
		if (!Enum.IsDefined(Type)) throw new InvalidOperationException($"'{nameof(Type)}' is not a valid {nameof(AlphaMaterialType)} value ({Type}).");
	}

	public static int GetHeapStorageFormattedLength(in AlphaAwareMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfResource() // NormalMap
			+	SerializationSizeOfResource() // OrmMap
			+	SerializationSizeOfInt() // Type
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in AlphaAwareMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteAndAllocateResource(ref dest, src.NormalMap);
		SerializationWriteAndAllocateResource(ref dest, src.OrmMap);
		SerializationWriteInt(ref dest, (int) src.Type);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static AlphaAwareMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new AlphaAwareMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			NormalMap = SerializationReadResource<Texture>(ref src),
			OrmMap = SerializationReadResource<Texture>(ref src),
			Type = (AlphaMaterialType) SerializationReadInt(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeResourceHandle(src[SerializationSizeOfResource()..]);
		SerializationDisposeResourceHandle(src[(SerializationSizeOfResource() * 2)..]);
	}
}