// Created on 2026-08-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Identifies a way of storing a texture on the GPU in compressed form.
/// </summary>
/// <remarks>
/// <para>
/// A compressed texture occupies a fraction of the video memory of an uncompressed one and is faster for the GPU to read, at the
/// cost of some loss of image quality and a one-off cost at load time to perform the compression. The formats here are the
/// "block compression" family, which work by encoding each 4x4 block of texels together.
/// </para>
/// </remarks>
public enum TextureCompressionFormat {
	/// <summary>
	/// No compression; the texture is stored exactly as supplied.
	/// </summary>
	None = 0,
	/// <summary>
	/// Three-channel colour compression with no alpha channel, at the highest compression ratio of these formats.
	/// </summary>
	/// <remarks>
	/// Suitable for opaque colour textures where memory matters more than fidelity. Any alpha channel in the source is discarded.
	/// </remarks>
	Bc1Srgb = 1,
	/// <summary>
	/// Four-channel colour compression including a full alpha channel, at half the compression ratio of <see cref="Bc1Srgb"/>.
	/// </summary>
	Bc3Srgb = 2,
	/// <summary>
	/// Two-channel compression, used for textures whose red and green channels carry data rather than colour.
	/// </summary>
	/// <remarks>
	/// This is the format used for normal maps and similar data textures: the third component of a unit-length vector can be
	/// reconstructed from the other two, so only two channels need storing.
	/// </remarks>
	Bc5 = 3,
	/// <summary>
	/// Four-channel colour compression at the best quality of these formats, for data that represents colour.
	/// </summary>
	Bc7Srgb = 4,
	/// <summary>
	/// The same compression as <see cref="Bc7Srgb"/>, but for textures whose channels carry data rather than colour and which
	/// must therefore not be interpreted as colour values.
	/// </summary>
	Bc7Linear = 5
}

/// <summary>
/// Compresses and decompresses texture data, and reports which compressed formats the current machine supports.
/// </summary>
/// <remarks>
/// The library compresses textures itself when asked to, so this class is mostly provided as a utility for advanced users.
/// </remarks>
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
	
	/// <summary>
	/// Returns whether the current machine's GPU can use the given compression format.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Support varies between machines, so a format must be tested before being relied upon. <see cref="TextureCompressionFormat.None"/> is always supported.
	/// </para>
	/// <para>
	/// Most modern hardware supports every compression format, so this check is somewhat of a formality.
	/// </para>
	/// </remarks>
	/// <param name="format">The format to test.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is not a recognised format.</exception>
	public static bool FormatIsSupported(TextureCompressionFormat format) {
		if (format == TextureCompressionFormat.None) return true;
		if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format), format, null);
		var flag = 1U << (((int) format) - 1);
		return (flag & _supportFlags) == flag;
	}
	
	/// <summary>
	/// Chooses the compression format best suited to a texture with the given characteristics.
	/// </summary>
	/// <remarks>
	/// This is what the library uses itself when a texture is created with a compression quality set, so calling it tells you
	/// what a given texture would end up as. A texture that allows dynamic writes, or that asks for no compression, yields
	/// <see cref="TextureCompressionFormat.None"/>.
	/// </remarks>
	/// <param name="sourceTextureDimensions">The texture's width and height, in texels.</param>
	/// <param name="texelType">The layout of the texture's texels.</param>
	/// <param name="dataType">What the texture's texels represent.</param>
	/// <param name="compressionQuality">How aggressively to compress, or <see langword="null"/> not to compress at all.</param>
	/// <param name="textureAllowsDynamicWrites">Whether the texture's contents may be overwritten after creation, which rules out compression entirely.</param>
	/// <param name="includesMipMapGeneration">Whether the texture carries successively smaller copies of itself.</param>
	/// <param name="includeUnsupportedFormats">Whether to return the ideal format even if this machine can not use it. Leaving this <see langword="false"/> falls back to a supported format instead.</param>
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
	internal const int BlockDimension = 4;
	internal const int MinEffortLevel = 1;
	internal const int MaxEffortLevel = 5;
	
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
	
	/// <summary>
	/// Returns how many bytes a texture of the given size occupies once compressed.
	/// </summary>
	/// <param name="dimensions">The texture's width and height, in texels.</param>
	/// <param name="format">Which compressed format the data is in.</param>
	/// <param name="includeMipChain">Whether to include the successively smaller copies of the texture as well as the full-size one.</param>
	public static int GetCompressedSizeBytes(XYPair<int> dimensions, TextureCompressionFormat format, bool includeMipChain) {
		return GetCompressedSizeBytes(dimensions, format, includeMipChain ? TextureUtils.GetMipLevelCount(dimensions) : 1);
	}

	/// <summary>
	/// Returns how many bytes the first few sizes of a compressed texture occupy.
	/// </summary>
	/// <param name="dimensions">The texture's width and height, in texels.</param>
	/// <param name="format">Which compressed format the data is in.</param>
	/// <param name="levelCount">How many successively smaller copies to include, counting the full-size original as one.</param>
	public static int GetCompressedSizeBytes(XYPair<int> dimensions, TextureCompressionFormat format, int levelCount) {
		if (format == TextureCompressionFormat.None) {
			throw new ArgumentOutOfRangeException(nameof(format), format, "Can not calculate compressed size for an uncompressed format.");
		}
		var result = 0;
		for (var i = 0; i < levelCount; ++i) {
			result += GetMipLevelSizeBytes(TextureUtils.GetMipLevelDimensions(dimensions, i), format);
		}
		return result;
	}
	#endregion

	#region Compression
	internal static int ConvertCompressionQualityToEffortInteger(Quality compressionQuality) => ((int) compressionQuality) + 3;
	
	/// <summary>
	/// Compresses a texture's texels in to the given buffer.
	/// </summary>
	/// <remarks>
	/// Compression is slow relative to simply uploading a texture; and the speed depends on the selected <paramref name="quality"/>.
	/// </remarks>
	/// <typeparam name="TTexel">The type of the texels being compressed.</typeparam>
	/// <param name="texels">The texels to compress, laid out row by row.</param>
	/// <param name="dimensions">The texture's width and height, in texels.</param>
	/// <param name="format">Which compressed format to produce. Must be one this machine supports.</param>
	/// <param name="quality">How much effort to spend; higher qualities take longer and lose less detail.</param>
	/// <param name="dataType">What the texels represent, which determines how they are combined.</param>
	/// <param name="includeMipMapGeneration">Whether to also produce the successively smaller copies of the texture.</param>
	/// <param name="destination">The buffer to write the compressed data in to. Size it from <c>GetCompressedSizeBytes</c> first.</param>
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

	/// <summary>
	/// Decompresses previously compressed texture data back in to texels.
	/// </summary>
	/// <remarks>
	/// Compression is lossy, so the result is not identical to what was compressed.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="blocks">The compressed data.</param>
	/// <param name="dimensions">The texture's width and height, in texels.</param>
	/// <param name="format">Which compressed format the data is in.</param>
	/// <param name="destination">The buffer to write the texels in to. Must hold at least <c>dimensions.X * dimensions.Y</c> entries.</param>
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
