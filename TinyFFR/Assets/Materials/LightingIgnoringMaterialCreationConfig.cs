// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct LightingIgnoringMaterialCreationConfig : IConfigStruct<LightingIgnoringMaterialCreationConfig> {
	public required Texture ColorMap { get; init; }

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}
	public bool EnablePerInstanceEffects {
		get => BaseConfig.EnablePerInstanceEffects;
		init => BaseConfig = BaseConfig with { EnablePerInstanceEffects = value };
	}

	public LightingIgnoringMaterialCreationConfig() { }
	public LightingIgnoringMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (ColorMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(ColorMap));
	}

	public static int GetHeapStorageFormattedLength(in LightingIgnoringMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // ColorMap
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in LightingIgnoringMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.ColorMap);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static LightingIgnoringMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new LightingIgnoringMaterialCreationConfig {
			ColorMap = SerializationReadResource<Texture>(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
	}
}