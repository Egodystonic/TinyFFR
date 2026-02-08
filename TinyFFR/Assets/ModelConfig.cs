// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct ModelReadConfig : IConfigStruct<ModelReadConfig> {
	public static readonly bool DefaultHandleUriEscapedStrings = false;
	public static readonly float DefaultGltfEmissiveStrengthScalar = 0.05f;
	public static readonly float DefaultEmissiveStrengthCap = 1f;
	public MeshReadConfig MeshConfig { get; init; } = new();
	public TextureReadConfig TextureConfig { get; init; } = new();
	public bool HandleUriEscapedStrings { get; init; } = DefaultHandleUriEscapedStrings;
	public float GltfEmissiveStrengthScalar { get; init; } = DefaultGltfEmissiveStrengthScalar;
	public float EmissiveStrengthCap { get; init; } = DefaultEmissiveStrengthCap;
	
	public ModelReadConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
		if (EmissiveStrengthCap is > 1f or < 0f) {
			throw new ArgumentException("Emissive strength cap must be between 0 and 1.", nameof(EmissiveStrengthCap));
		}
	}

	public static int GetHeapStorageFormattedLength(in ModelReadConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfBool() // HandleUriEscapedStrings
			+	SerializationSizeOfFloat() // GltfEmissiveStrengthScalar
			+	SerializationSizeOfFloat(); // EmissiveStrengthCap
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelReadConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteBool(ref dest, src.HandleUriEscapedStrings);
		SerializationWriteFloat(ref dest, src.GltfEmissiveStrengthScalar);
		SerializationWriteFloat(ref dest, src.EmissiveStrengthCap);
	}
	public static ModelReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelReadConfig {
			MeshConfig = SerializationReadSubConfig<MeshReadConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureReadConfig>(ref src),
			HandleUriEscapedStrings = SerializationReadBool(ref src),
			GltfEmissiveStrengthScalar = SerializationReadFloat(ref src),
			EmissiveStrengthCap = SerializationReadFloat(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var meshConfigLength = SerializationSizeOfSubConfig(converted.MeshConfig);
		var textureConfigLength = SerializationSizeOfSubConfig(converted.TextureConfig);
		var meshConfigSubSpan = src[..meshConfigLength];
		var textureConfigSubSpan = src[meshConfigLength..][..textureConfigLength];
		MeshReadConfig.DisposeAllocatedHeapStorage(meshConfigSubSpan);
		TextureReadConfig.DisposeAllocatedHeapStorage(textureConfigSubSpan);
	}
}

public readonly ref struct ModelCreationConfig : IConfigStruct<ModelCreationConfig> {
	public MeshCreationConfig MeshConfig { get; init; } = new();
	public TextureCreationConfig TextureConfig { get; init; } = new() { IsLinearColorspace = true };
	public ReadOnlySpan<char> Name { get; init; }
	
	public ModelCreationConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in ModelCreationConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteString(ref dest, src.Name);
	}
	public static ModelCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelCreationConfig {
			MeshConfig = SerializationReadSubConfig<MeshCreationConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var meshConfigLength = SerializationSizeOfSubConfig(converted.MeshConfig);
		var textureConfigLength = SerializationSizeOfSubConfig(converted.TextureConfig);
		var meshConfigSubSpan = src[..meshConfigLength];
		var textureConfigSubSpan = src[meshConfigLength..][..textureConfigLength];
		MeshCreationConfig.DisposeAllocatedHeapStorage(meshConfigSubSpan);
		TextureCreationConfig.DisposeAllocatedHeapStorage(textureConfigSubSpan);
	}
}