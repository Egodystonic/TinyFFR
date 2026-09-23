// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Creation Config for general processing in the local builder when creating the resource

/// <summary>
/// Controls how a model file's contents are interpreted as they are read from disc.
/// </summary>
/// <remarks>
/// A model file holds meshes and textures together, so this nests the configuration for each alongside the settings that only
/// apply to whole-file loading.
/// </remarks>
public readonly ref struct ModelReadConfig : IConfigStruct<ModelReadConfig> {
	/// <summary>
	/// The default value for <see cref="ModelReadConfig.HandleUriEscapedStrings"/>: <see langword="false"/>.
	/// </summary>
	public static readonly bool DefaultHandleUriEscapedStrings = false;
	/// <summary>
	/// The default value for <see cref="ModelReadConfig.EmissiveStrengthScalar"/>: <c>0.05f</c>.
	/// </summary>
	public static readonly float DefaultEmissiveStrengthScalar = 0.05f;
	/// <summary>
	/// The default value for <see cref="ModelReadConfig.EmissiveStrengthCap"/>: <c>1f</c>.
	/// </summary>
	public static readonly float DefaultEmissiveStrengthCap = 1f;
	/// <summary>
	/// The default value for <see cref="ModelReadConfig.EmbeddedTextureMapScalingStrategy"/>: <see cref="TextureCombinationScalingStrategy.PixelUpscale"/>.
	/// </summary>
	public static readonly TextureCombinationScalingStrategy DefaultEmbeddedTextureMapScalingStrategy = TextureCombinationScalingStrategy.PixelUpscale;
	/// <summary>
	/// How the file's mesh data is interpreted as it is read.
	/// </summary>
	public MeshReadConfig MeshConfig { get; init; } = new();
	/// <summary>
	/// How the file's embedded texture data is interpreted as it is read.
	/// </summary>
	public TextureReadConfig TextureConfig { get; init; } = new();
	/// <summary>
	/// Whether to decode percent-escaped characters in the file's embedded paths and names. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Some exporters write paths in the escaped form used by web addresses, in which a space appears as <c>%20</c>. This requires
	/// using the security-aware decoding API built in to .NET, which also allocates garbage.
	/// </remarks>
	public bool HandleUriEscapedStrings { get; init; } = DefaultHandleUriEscapedStrings;
	/// <summary>
	/// A factor applied to emissive strengths read from the file. Defaults to <see cref="DefaultEmissiveStrengthScalar"/>: <c>0.05f</c>.
	/// </summary>
	/// <remarks>
	/// Some formats (most notably glTF) record emissive strength on a scale that does not correspond to TinyFFR's, so a file's values
	/// are scaled down to keep glowing surfaces from overwhelming the scene. Raise this if imported emissive surfaces look too dim;
	/// lower it if they seem too bright.
	/// </remarks>
	public float EmissiveStrengthScalar { get; init; } = DefaultEmissiveStrengthScalar;
	/// <summary>
	/// The largest emissive strength any imported material may take. Defaults to <see cref="DefaultEmissiveStrengthCap"/>: <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// This guards against a single badly-authored material washing out the whole scene.
	/// </remarks>
	public float EmissiveStrengthCap { get; init; } = DefaultEmissiveStrengthCap;
	/// <summary>
	/// How embedded textures smaller than the combined output are enlarged when several are packed together.
	/// Defaults to <see cref="DefaultEmbeddedTextureMapScalingStrategy"/>: <see cref="TextureCombinationScalingStrategy.PixelUpscale"/>.
	/// </summary>
	public TextureCombinationScalingStrategy EmbeddedTextureMapScalingStrategy { get; init; } = DefaultEmbeddedTextureMapScalingStrategy;
	
	/// <summary>
	/// Constructs a new <see cref="ModelReadConfig"/> with default values for every property.
	/// </summary>
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

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in ModelReadConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfBool() // HandleUriEscapedStrings
			+	SerializationSizeOfFloat() // EmissiveStrengthScalar
			+	SerializationSizeOfFloat() // EmissiveStrengthCap
			+	SerializationSizeOfInt(); // EmbeddedTextureMapScalingStrategy
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelReadConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteBool(ref dest, src.HandleUriEscapedStrings);
		SerializationWriteFloat(ref dest, src.EmissiveStrengthScalar);
		SerializationWriteFloat(ref dest, src.EmissiveStrengthCap);
		SerializationWriteInt(ref dest, (int) src.EmbeddedTextureMapScalingStrategy);
	}
	/// <inheritdoc />
	public static ModelReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelReadConfig {
			MeshConfig = SerializationReadSubConfig<MeshReadConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureReadConfig>(ref src),
			HandleUriEscapedStrings = SerializationReadBool(ref src),
			EmissiveStrengthScalar = SerializationReadFloat(ref src),
			EmissiveStrengthCap = SerializationReadFloat(ref src),
			EmbeddedTextureMapScalingStrategy = (TextureCombinationScalingStrategy) SerializationReadInt(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<MeshReadConfig>(ref src);
		SerializationDisposeSubConfig<TextureReadConfig>(ref src);
	}
}

/// <summary>
/// Controls how the meshes, textures and materials found in a model file are created.
/// </summary>
public readonly ref struct ModelCreationConfig : IConfigStruct<ModelCreationConfig> {
	/// <summary>
	/// How the meshes found in the file are created.
	/// </summary>
	public MeshCreationConfig MeshConfig { get; init; } = new();
	/// <summary>
	/// How the textures found in the file are created.
	/// </summary>
	/// <remarks>
	/// A note on the required <see cref="TextureCreationConfig.DataType"/> property: The value set here
	/// will only ever apply to "isolated" textures that have no meaning or assignment to the materials/models
	/// contained within the composite file and no semantic meaning otherwise attached. Otherwise, the value
	/// you set here will largely be ignored &amp; overridden according to the actual data type of each
	/// embedded or referenced texture (in most composite files this applied to every texture).
	/// </remarks>
	public TextureCreationConfig TextureConfig { get; init; } = new() { DataType = TextureDataType.LinearData };
	/// <summary>
	/// The name to give the resulting resource group. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }
	
	/// <summary>
	/// Constructs a new <see cref="ModelCreationConfig"/> with default values for every property.
	/// </summary>
	public ModelCreationConfig() { }

	internal void ThrowIfInvalid() {
		MeshConfig.ThrowIfInvalid();
		TextureConfig.ThrowIfInvalid();
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in ModelCreationConfig src) {
		return  SerializationSizeOfSubConfig(src.MeshConfig) // MeshConfig
			+	SerializationSizeOfSubConfig(src.TextureConfig) // TextureConfig
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.MeshConfig);
		SerializationWriteSubConfig(ref dest, src.TextureConfig);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc />
	public static ModelCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ModelCreationConfig {
			MeshConfig = SerializationReadSubConfig<MeshCreationConfig>(ref src),
			TextureConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	/// <inheritdoc />
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
