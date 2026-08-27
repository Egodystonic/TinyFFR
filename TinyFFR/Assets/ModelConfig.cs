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
	public static readonly TextureCombinationScalingStrategy DefaultEmbeddedTextureMapScalingStrategy = TextureCombinationScalingStrategy.PixelUpscale;
	public MeshReadConfig MeshConfig { get; init; } = new();
	public TextureReadConfig TextureConfig { get; init; } = new();
	public bool HandleUriEscapedStrings { get; init; } = DefaultHandleUriEscapedStrings;
	public float GltfEmissiveStrengthScalar { get; init; } = DefaultGltfEmissiveStrengthScalar;
	public float EmissiveStrengthCap { get; init; } = DefaultEmissiveStrengthCap;
	public TextureCombinationScalingStrategy EmbeddedTextureMapScalingStrategy { get; init; } = DefaultEmbeddedTextureMapScalingStrategy;
	
	public ModelReadConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
		if (EmissiveStrengthCap is > 1f or < 0f) {
			throw new ArgumentException("Emissive strength cap must be between 0 and 1.", nameof(EmissiveStrengthCap));
		}
		if (!Enum.IsDefined(EmbeddedTextureMapScalingStrategy)) {
			throw new ArgumentOutOfRangeException(nameof(EmbeddedTextureMapScalingStrategy), EmbeddedTextureMapScalingStrategy, null);
		}
	}

	public static int GetHeapStorageFormattedLength(in ModelReadConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfBool() // HandleUriEscapedStrings
			+	SerializationSizeOfFloat() // GltfEmissiveStrengthScalar
			+	SerializationSizeOfFloat() // EmissiveStrengthCap
			+	SerializationSizeOfInt(); // EmbeddedTextureMapScalingStrategy
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelReadConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteBool(ref dest, src.HandleUriEscapedStrings);
		SerializationWriteFloat(ref dest, src.GltfEmissiveStrengthScalar);
		SerializationWriteFloat(ref dest, src.EmissiveStrengthCap);
		SerializationWriteInt(ref dest, (int) src.EmbeddedTextureMapScalingStrategy);
	}
	public static ModelReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelReadConfig {
			MeshConfig = SerializationReadSubConfig<MeshReadConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureReadConfig>(ref src),
			HandleUriEscapedStrings = SerializationReadBool(ref src),
			GltfEmissiveStrengthScalar = SerializationReadFloat(ref src),
			EmissiveStrengthCap = SerializationReadFloat(ref src),
			EmbeddedTextureMapScalingStrategy = (TextureCombinationScalingStrategy) SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<MeshReadConfig>(ref src);
		SerializationDisposeSubConfig<TextureReadConfig>(ref src);
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
		SerializationDisposeSubConfig<MeshCreationConfig>(ref src);
		SerializationDisposeSubConfig<TextureCreationConfig>(ref src);
	}
}

readonly ref struct ModelLoadConfig : IConfigStruct<ModelLoadConfig> {
	public ModelCreationConfig CreationConfig { get; init; } = new();
	public ModelReadConfig ReadConfig { get; init; } = new();

	public ModelLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		ReadConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in ModelLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.ReadConfig); // ReadConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.ReadConfig);
	}
	public static ModelLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelLoadConfig {
			CreationConfig = SerializationReadSubConfig<ModelCreationConfig>(ref src),
			ReadConfig = SerializationReadSubConfig<ModelReadConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<ModelCreationConfig>(ref src);
		SerializationDisposeSubConfig<ModelReadConfig>(ref src);
	}
}
