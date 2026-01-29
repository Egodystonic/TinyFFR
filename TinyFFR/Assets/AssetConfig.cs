// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct AssetReadConfig : IConfigStruct<AssetReadConfig> {
	public MeshReadConfig MeshConfig { get; init; } = new();
	public TextureReadConfig TextureConfig { get; init; } = new();
	public bool SkipUnusedMaterials { get; init; } = true;
	
	public AssetReadConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in AssetReadConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfBool(); // SkipUnusedMaterials
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in AssetReadConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteBool(ref dest, src.SkipUnusedMaterials);
	}
	public static AssetReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new AssetReadConfig {
			MeshConfig = SerializationReadSubConfig<MeshReadConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureReadConfig>(ref src),
			SkipUnusedMaterials = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var meshConfigLength = SerializationSizeOfSubConfig(converted.MeshConfig);
		var meshConfigSubSpan = src[..meshConfigLength];
		var textureConfigSubSpan = src[meshConfigLength..];
		MeshReadConfig.DisposeAllocatedHeapStorage(meshConfigSubSpan);
		TextureReadConfig.DisposeAllocatedHeapStorage(textureConfigSubSpan);
	}
}

public readonly ref struct AssetCreationConfig : IConfigStruct<AssetCreationConfig> {
	public MeshCreationConfig MeshConfig { get; init; } = new();
	public TextureCreationConfig TextureConfig { get; init; } = new() { IsLinearColorspace = true };
	public ReadOnlySpan<char> Name { get; init; }
	
	public AssetCreationConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in AssetCreationConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in AssetCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteString(ref dest, src.Name);
	}
	public static AssetCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new AssetCreationConfig {
			MeshConfig = SerializationReadSubConfig<MeshCreationConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var meshConfigLength = SerializationSizeOfSubConfig(converted.MeshConfig);
		var meshConfigSubSpan = src[..meshConfigLength];
		var textureConfigSubSpan = src[meshConfigLength..];
		MeshCreationConfig.DisposeAllocatedHeapStorage(meshConfigSubSpan);
		TextureCreationConfig.DisposeAllocatedHeapStorage(textureConfigSubSpan);
	}
}