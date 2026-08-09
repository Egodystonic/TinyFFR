// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct ColorKeyedMaterialCreationConfig : IConfigStruct<ColorKeyedMaterialCreationConfig> {
	static char[]? _keyMapParameterString = null;
	public static ReadOnlySpan<char> KeyMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _keyMapParameterString, LocalShaderPackageConstants.ColorKeyedMaterialShader.ParamKeyMap);

	public required Texture KeyMap { get; init; }
	public bool BlendOutputAlphaWithScene { get; init; } = false;

	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public ColorKeyedMaterialCreationConfig() { }
	public ColorKeyedMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (KeyMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(KeyMap));
	}

	public static int GetHeapStorageFormattedLength(in ColorKeyedMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // KeyMap
			+	SerializationSizeOfBool() // OutputIncludesAlphaChannel
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ColorKeyedMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.KeyMap);
		SerializationWriteBool(ref dest, src.BlendOutputAlphaWithScene);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static ColorKeyedMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ColorKeyedMaterialCreationConfig {
			KeyMap = SerializationReadResource<Texture>(ref src),
			BlendOutputAlphaWithScene = SerializationReadBool(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
	}
}
