// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct SimpleMaterialCreationConfig : IConfigStruct<SimpleMaterialCreationConfig> {
	public required Texture ColorMap { get; init; }
	public Texture? EmissiveMap { get; init; }

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public SimpleMaterialCreationConfig() { }
	public SimpleMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
	}

	public static int GetHeapStorageFormattedLength(in SimpleMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfNullableResource() // EmissiveMap
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in SimpleMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteAndAllocateNullableResource(ref dest, src.EmissiveMap);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static SimpleMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new SimpleMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			EmissiveMap = SerializationReadNullableResource<Texture>(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
		SerializationDisposeNullableResourceHandle(src[SerializationSizeOfResource()..]);
	}
}