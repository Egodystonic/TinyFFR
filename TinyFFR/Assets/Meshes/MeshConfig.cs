// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Meshes;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Generation Config for live generation of new ones
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct MeshReadConfig : IConfigStruct<MeshReadConfig> {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool FixCommonExportErrors { get; init; } = true;
	public bool OptimizeForGpu { get; init; } = true;

	public MeshReadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(MeshReadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}

	public static int GetHeapStorageFormattedLength(in MeshReadConfig src) {
		return	SerializationSizeOfString(src.FilePath) // FilePath
			+	SerializationSizeOfBool() // FixCommonExportErrors
			+	SerializationSizeOfBool();  // OptimizeForGpu
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshReadConfig src) {
		SerializationWriteString(ref dest, src.FilePath);
		SerializationWriteBool(ref dest, src.FixCommonExportErrors);
		SerializationWriteBool(ref dest, src.OptimizeForGpu);
	}
	public static MeshReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshReadConfig {
			FilePath = SerializationReadString(ref src),
			FixCommonExportErrors = SerializationReadBool(ref src),
			OptimizeForGpu = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct MeshGenerationConfig : IConfigStruct<MeshGenerationConfig> {
	public Transform2D TextureTransform { get; init; } = Transform2D.None;

	public MeshGenerationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	public static int GetHeapStorageFormattedLength(in MeshGenerationConfig src) {
		return SerializationSizeOf<Transform2D>(); // TextureTransform
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshGenerationConfig src) {
		SerializationWrite(ref dest, src.TextureTransform);
	}
	public static MeshGenerationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshGenerationConfig {
			TextureTransform = SerializationRead<Transform2D>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct MeshCreationConfig : IConfigStruct<MeshCreationConfig> {
	public bool FlipTriangles { get; init; } = false;
	public bool InvertTextureU { get; init; } = false;
	public bool InvertTextureV { get; init; } = false;
	public Vect OriginTranslation { get; init; } = Vect.Zero;
	public float LinearRescalingFactor { get; init; } = 1f;
	public ReadOnlySpan<char> Name { get; init; }

	public MeshCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	public static int GetHeapStorageFormattedLength(in MeshCreationConfig src) {
		return	SerializationSizeOfBool() // FlipTriangles
			+	SerializationSizeOfBool() // InvertTextureU
			+	SerializationSizeOfBool() // InvertTextureV
			+	SerializationSizeOf<Vect>() // OriginTranslation
			+	SerializationSizeOfFloat() // LinearRescalingFactor
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshCreationConfig src) {
		SerializationWriteBool(ref dest, src.FlipTriangles);
		SerializationWriteBool(ref dest, src.InvertTextureU);
		SerializationWriteBool(ref dest, src.InvertTextureV);
		SerializationWrite(ref dest, src.OriginTranslation);
		SerializationWriteFloat(ref dest, src.LinearRescalingFactor);
		SerializationWriteString(ref dest, src.Name);
	}
	public static MeshCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshCreationConfig {
			FlipTriangles = SerializationReadBool(ref src),
			InvertTextureU = SerializationReadBool(ref src),
			InvertTextureV = SerializationReadBool(ref src),
			OriginTranslation = SerializationRead<Vect>(ref src),
			LinearRescalingFactor = SerializationReadFloat(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}