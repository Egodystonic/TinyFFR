// Created on 2026-08-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum TextureCompressionFormat {
	None = 0,
	Bc1Srgb = 1,
	Bc3Srgb = 2,
	Bc5 = 3,
	Bc7Srgb = 4,
	Bc7Linear = 5
}

public static unsafe class TextureCompressor {
	#region Format Selection
	static readonly TextureCompressionFormat[] _allCompressionFormatEnumValues = Enum.GetValues<TextureCompressionFormat>();
	static readonly HeapPool _compressionPool = new();
	static uint _supportFlags = 0;

	internal static void AscertainCompressionSupport() {
		var result = 0U;
		foreach (var format in _allCompressionFormatEnumValues) {
			if (format == TextureCompressionFormat.None) continue;
			IsTextureCompressionFormatSupported((int) format, out var supported).ThrowIfFailure();
			if (supported) result |= 1U << (((int) format) - 1);
		}

		_supportFlags = result;
	}
	
	public static bool FormatIsSupported(TextureCompressionFormat format) {
		if (format == TextureCompressionFormat.None) return true;
		if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format), format, null);
		var flag = 1U << (((int) format) - 1);
		return (flag & _supportFlags) == flag;
	}
	
	public static TextureCompressionFormat GetRecommendedFormat(XYPair<int> sourceTextureDimensions, TexelType texelType, TextureDataType dataType, Quality? compressionQuality, bool textureAllowsDynamicWrites, bool includesMipMapGeneration, bool includeUnsupportedFormats = false) {
		if (textureAllowsDynamicWrites || compressionQuality is not { } cq) return TextureCompressionFormat.None;
		
		var result = (dataTextureType: dataType, cq, sourceTexelType: texelType) switch {
			(TextureDataType.LinearDataUnitVector, _, _) => TextureCompressionFormat.Bc5,
			(TextureDataType.LinearDataTwoChannelMax, _, _) => TextureCompressionFormat.Bc5,
			(TextureDataType.LinearData, _, _) => TextureCompressionFormat.Bc7Linear,
			(TextureDataType.ColorSrgb, Quality.VeryLow, TexelType.Rgb24) => TextureCompressionFormat.Bc1Srgb,
			(TextureDataType.ColorSrgb, Quality.VeryLow, _) => TextureCompressionFormat.Bc3Srgb,
			(TextureDataType.ColorSrgb, _, _) => TextureCompressionFormat.Bc7Srgb,
			_ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
		};
		
		if (!includeUnsupportedFormats && !FormatIsSupported(result)) {
			switch (result) {
				case TextureCompressionFormat.Bc7Srgb:
					var fallback = texelType == TexelType.Rgb24 ? TextureCompressionFormat.Bc1Srgb : TextureCompressionFormat.Bc3Srgb;
					result = FormatIsSupported(fallback) ? fallback : TextureCompressionFormat.None;
					break;
				case TextureCompressionFormat.Bc5:
					result = FormatIsSupported(TextureCompressionFormat.Bc7Linear) ? TextureCompressionFormat.Bc7Linear : TextureCompressionFormat.None;
					break;
				default:
					result = TextureCompressionFormat.None;
					break;
			}
		}
		
		if (result == TextureCompressionFormat.None) return TextureCompressionFormat.None;
		
		var compressedSize = GetCompressedSizeBytes(sourceTextureDimensions, result, includesMipMapGeneration);
		var uncompressedSize = GetNonCompressedSizeBytes(sourceTextureDimensions, texelType == TexelType.Rgb24 ? TexelRgb24.TexelSizeBytes : TexelRgba32.TexelSizeBytes, includesMipMapGeneration);
		return compressedSize < uncompressedSize ? result : TextureCompressionFormat.None;
	}
	#endregion
	
	#region Metrics
	public const int BlockDimension = 4;
	public const int MinEffortLevel = 1;
	public const int MaxEffortLevel = 5;
	
	internal static int GetFormatBlockSizeBytes(TextureCompressionFormat format) => format switch {
		TextureCompressionFormat.None => 0,
		TextureCompressionFormat.Bc1Srgb => 8,
		TextureCompressionFormat.Bc3Srgb => 16,
		TextureCompressionFormat.Bc5 => 16,
		TextureCompressionFormat.Bc7Srgb => 16,
		TextureCompressionFormat.Bc7Linear => 16,
		_ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown compression format.")
	};

	internal static int GetBlockCount(XYPair<int> dimensions) {
		if (dimensions.X < 1 || dimensions.Y < 1) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Dimensions X and Y must both be positive.");
		}
		var blocksWide = (dimensions.X + BlockDimension - 1) / BlockDimension;
		var blocksHigh = (dimensions.Y + BlockDimension - 1) / BlockDimension;
		return blocksWide * blocksHigh;
	}
	
	internal static int GetMipLevelSizeBytes(XYPair<int> dimensions, TextureCompressionFormat format) => GetBlockCount(dimensions) * GetFormatBlockSizeBytes(format);

	internal static int GetNonCompressedSizeBytes(XYPair<int> dimensions, int texelSizeBytes, bool includeMipChain) {
		var levelCount = includeMipChain ? TextureUtils.GetMipLevelCount(dimensions) : 1;
		var result = 0;
		for (var i = 0; i < levelCount; ++i) {
			result += TextureUtils.GetMipLevelDimensions(dimensions, i).Area * texelSizeBytes;
		}
		return result;
	}
	
	public static int GetCompressedSizeBytes(XYPair<int> dimensions, TextureCompressionFormat format, bool includeMipChain) {
		if (format == TextureCompressionFormat.None) {
			throw new ArgumentOutOfRangeException(nameof(format), format, "Can not calculate compressed size for an uncompressed format.");
		}
		var numMipLevels = includeMipChain ? TextureUtils.GetMipLevelCount(dimensions) : 1;
		var result = 0;
		for (var i = 0; i < numMipLevels; ++i) {
			result += GetMipLevelSizeBytes(TextureUtils.GetMipLevelDimensions(dimensions, i), format);
		}
		return result;
	}
	#endregion

	#region Compression
	internal static int ConvertCompressionQualityToEffortInteger(Quality compressionQuality) => ((int) compressionQuality) + 3;
	
	public static void Compress<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, TextureCompressionFormat format, Quality quality, TextureDataType dataType, bool includeMipMapGeneration, Span<byte> destination) where TTexel : unmanaged, ITexel<TTexel> {
		if (dimensions.X <= 0 || dimensions.Y <= 0) throw new ArgumentException($"Both X and Y must be positive (was {dimensions}).", nameof(dimensions));
		if (!Enum.IsDefined(format) || format == TextureCompressionFormat.None) throw new ArgumentOutOfRangeException(nameof(format), format, null);
		if (!Enum.IsDefined(quality)) throw new ArgumentOutOfRangeException(nameof(quality), quality, null);

		var baseTexelCount = dimensions.Area;
		if (texels.Length < baseTexelCount) {
			throw new ArgumentException(
				$"Texture dimensions are {dimensions.X}x{dimensions.Y}, requiring a texel span of length {baseTexelCount} or greater, " +
				$"but actual span length was {texels.Length}.",
				nameof(texels)
			);
		}
		
		var levelCount = includeMipMapGeneration ? TextureUtils.GetMipLevelCount(dimensions) : 1;
		var effortLevel = ConvertCompressionQualityToEffortInteger(quality);

		// This is a swappable buffer used to swap data between them for mip level generation
		using var bufferA = _compressionPool.ThreadSafeWrapper.Borrow<TexelRgba32>(baseTexelCount);
		using var bufferB = _compressionPool.ThreadSafeWrapper.Borrow<TexelRgba32>(baseTexelCount);
		var mipLevelSource = bufferA.Span;
		var mipLevelTarget = bufferB.Span;

		if (TTexel.BlitType == TexelType.Rgba32) {
			MemoryMarshal.Cast<TTexel, TexelRgba32>(texels[..baseTexelCount]).CopyTo(mipLevelSource);
		}
		else if (!TTexel.TryCoerceSpan(texels[..baseTexelCount], mipLevelSource[..baseTexelCount])) {
			throw new InvalidOperationException($"Texel type '{typeof(TTexel).Name}' must be coercible to {nameof(TexelRgba32)} for compression.");
		}

		var sourceDimensions = dimensions;
		var destinationOffset = 0;

		for (var level = 0; level < levelCount; ++level) {
			if (level > 0) {
				var levelDimensions = TextureUtils.GetMipLevelDimensions(dimensions, level);
				TextureUtils.GenerateNextMipLevel(mipLevelSource[..sourceDimensions.Area], sourceDimensions, mipLevelTarget[..levelDimensions.Area], dataType);
				var previousSource = mipLevelSource;
				mipLevelSource = mipLevelTarget;
				mipLevelTarget = previousSource;
				sourceDimensions = levelDimensions;
			}

			var levelSizeBytes = GetMipLevelSizeBytes(sourceDimensions, format);
			CompressSingleLevel(mipLevelSource[..sourceDimensions.Area], sourceDimensions, format, effortLevel, destination.Slice(destinationOffset, levelSizeBytes));
			destinationOffset += levelSizeBytes;
		}
	}

	static void CompressSingleLevel(ReadOnlySpan<TexelRgba32> texels, XYPair<int> dimensions, TextureCompressionFormat format, int effortLevel, Span<byte> destination) {
		fixed (TexelRgba32* sourcePtr = texels)
		fixed (byte* destinationPtr = destination) {
			CompressTextureLevel(
				sourcePtr,
				(uint) dimensions.X,
				(uint) dimensions.Y,
				(int) format,
				effortLevel,
				destinationPtr,
				destination.Length
			).ThrowIfFailure();
		}
	}

	public static void Decompress<TTexel>(ReadOnlySpan<byte> blocks, XYPair<int> dimensions, TextureCompressionFormat format, Span<TTexel> destination) where TTexel : unmanaged, ITexel<TTexel> {
		if (dimensions.X <= 0 || dimensions.Y <= 0) throw new ArgumentException($"Both X and Y must be positive (was {dimensions}).", nameof(dimensions));
		if (!Enum.IsDefined(format) || format == TextureCompressionFormat.None) throw new ArgumentOutOfRangeException(nameof(format), format, null);

		var texelCount = dimensions.Area;
		if (destination.Length < texelCount) {
			throw new ArgumentException(
				$"Texture dimensions are {dimensions.X}x{dimensions.Y}, requiring a texel span of length {texelCount} or greater, " +
				$"but actual span length was {destination.Length}.",
				nameof(destination)
			);
		}

		var requiredBlockBytes = GetMipLevelSizeBytes(dimensions, format);
		if (blocks.Length < requiredBlockBytes) {
			throw new ArgumentException(
				$"Decompressing a {dimensions.X}x{dimensions.Y} {format} texture requires {requiredBlockBytes} bytes of block data, " +
				$"but only {blocks.Length} were supplied.",
				nameof(blocks)
			);
		}

		if (TTexel.BlitType == TexelType.Rgba32) {
			DecompressSingleLevel(blocks, dimensions, format, MemoryMarshal.Cast<TTexel, TexelRgba32>(destination[..texelCount]));
			return;
		}

		using var scratchBuffer = _compressionPool.ThreadSafeWrapper.Borrow<TexelRgba32>(texelCount);
		var scratchSpan = scratchBuffer.Span[..texelCount];
		DecompressSingleLevel(blocks, dimensions, format, scratchSpan);
		if (!TTexel.TryCoerceSpanFrom(scratchSpan, destination[..texelCount], mergeWithExistingDestinationData: false)) {
			throw new InvalidOperationException($"Texel type '{typeof(TTexel).Name}' must be coercible from {nameof(TexelRgba32)} for decompression.");
		}
	}

	static void DecompressSingleLevel(ReadOnlySpan<byte> blocks, XYPair<int> dimensions, TextureCompressionFormat format, Span<TexelRgba32> destination) {
		fixed (byte* sourcePtr = blocks)
		fixed (TexelRgba32* destinationPtr = destination) {
			DecompressTextureLevel(
				sourcePtr,
				blocks.Length,
				(uint) dimensions.X,
				(uint) dimensions.Y,
				(int) format,
				destinationPtr,
				destination.Length * sizeof(TexelRgba32)
			).ThrowIfFailure();
		}
	}
	#endregion 
	
	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "is_texture_compression_format_supported")]
	static extern InteropResult IsTextureCompressionFormatSupported(int formatId, out InteropBool outSupported);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "compress_texture_level")]
	static extern InteropResult CompressTextureLevel(TexelRgba32* srcRgbaPtr, uint width, uint height, int formatId, int effortLevel, void* destPtr, int destLen);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "decompress_texture_level")]
	static extern InteropResult DecompressTextureLevel(byte* srcBlocksPtr, int srcLen, uint width, uint height, int formatId, TexelRgba32* destRgbaPtr, int destLen);
	#endregion
}
