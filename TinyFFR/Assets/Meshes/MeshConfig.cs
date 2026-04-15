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
	public bool FixCommonExportErrors { get; init; } = true;
	public bool OptimizeForGpu { get; init; } = true;
	public bool CorrectFlippedOrientation { get; init; } = true;
	public bool LoadSkeletalAnimationDataIfPresent { get; init; } = true;
	public int? SubMeshIndex { get; init; } = null;
	public float? AnimationTicksPerSecondOverride { get; init; } = null;

	public MeshReadConfig() { }

	internal void ThrowIfInvalid() {
		if (SubMeshIndex < 0) throw new ArgumentOutOfRangeException(nameof(SubMeshIndex), SubMeshIndex, $"Sub-mesh index must be null or non-negative.");
	}

	public static int GetHeapStorageFormattedLength(in MeshReadConfig src) {
		return	SerializationSizeOfBool() // FixCommonExportErrors
			+	SerializationSizeOfBool() // OptimizeForGpu
			+	SerializationSizeOfBool() // CorrectFlippedOrientation
			+	SerializationSizeOfBool() // LoadSkeletalAnimationDataIfPresent
			+	SerializationSizeOfNullableInt() // SubMeshIndex
			+	SerializationSizeOfNullableFloat(); // AnimationTicksPerSecondOverride
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshReadConfig src) {
		SerializationWriteBool(ref dest, src.FixCommonExportErrors);
		SerializationWriteBool(ref dest, src.OptimizeForGpu);
		SerializationWriteBool(ref dest, src.CorrectFlippedOrientation);
		SerializationWriteBool(ref dest, src.LoadSkeletalAnimationDataIfPresent);
		SerializationWriteNullableInt(ref dest, src.SubMeshIndex);
		SerializationWriteNullableFloat(ref dest, src.AnimationTicksPerSecondOverride);
	}
	public static MeshReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshReadConfig {
			FixCommonExportErrors = SerializationReadBool(ref src),
			OptimizeForGpu = SerializationReadBool(ref src),
			CorrectFlippedOrientation = SerializationReadBool(ref src),
			LoadSkeletalAnimationDataIfPresent = SerializationReadBool(ref src),
			SubMeshIndex = SerializationReadNullableInt(ref src),
			AnimationTicksPerSecondOverride = SerializationReadNullableFloat(ref src)
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
	public static readonly float DefaultBoundingBoxAdditionalMargin = 0.03f;
	
	public bool FlipTriangles { get; init; } = false;
	public bool InvertTextureU { get; init; } = false;
	public bool InvertTextureV { get; init; } = false;
	public Vect OriginTranslation { get; init; } = Vect.Zero;
	public float LinearRescalingFactor { get; init; } = 1f;
	public PositionedCuboid? BoundingBoxOverride { get; init; } = null;
	public float BoundingBoxAdditionalMargin { get; init; } = DefaultBoundingBoxAdditionalMargin;
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
			+	SerializationSizeOfNullable<PositionedCuboid>() // BoundingBoxOverride
			+	SerializationSizeOfFloat() // BoundingBoxAdditionalMargin
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshCreationConfig src) {
		SerializationWriteBool(ref dest, src.FlipTriangles);
		SerializationWriteBool(ref dest, src.InvertTextureU);
		SerializationWriteBool(ref dest, src.InvertTextureV);
		SerializationWrite(ref dest, src.OriginTranslation);
		SerializationWriteFloat(ref dest, src.LinearRescalingFactor);
		SerializationWriteNullable(ref dest, src.BoundingBoxOverride);
		SerializationWriteFloat(ref dest, src.BoundingBoxAdditionalMargin);
		SerializationWriteString(ref dest, src.Name);
	}
	public static MeshCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshCreationConfig {
			FlipTriangles = SerializationReadBool(ref src),
			InvertTextureU = SerializationReadBool(ref src),
			InvertTextureV = SerializationReadBool(ref src),
			OriginTranslation = SerializationRead<Vect>(ref src),
			LinearRescalingFactor = SerializationReadFloat(ref src),
			BoundingBoxOverride = SerializationReadNullable<PositionedCuboid>(ref src),
			BoundingBoxAdditionalMargin = SerializationReadFloat(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}