// Created on 2025-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR;

public static class ImageUtils {
	const int MaxFilePathLength = 1024;
	static readonly ThreadLocal<InteropStringBuffer> _threadLocalStringBuffer = new(() => new InteropStringBuffer(MaxFilePathLength, true), trackAllValues: false);
	static readonly ArrayPool<byte> _dataBufferPool = ArrayPool<byte>.Shared;
	static ReadOnlySpan<byte> DummyFileData => "tinyffr"u8;

	public readonly record struct BitmapSaveConfig(bool IncludeAlphaChannel, bool FlipVertical, bool FlipHorizontal);

	public static void SaveBitmap<TTexel>(ReadOnlySpan<char> filePath, XYPair<int> dimensions, ReadOnlySpan<TTexel> texels) where TTexel : unmanaged, ITexel<TTexel, byte> {
		SaveBitmap(filePath, dimensions, texels, new(IncludeAlphaChannel: false, FlipVertical: false, FlipHorizontal: false));
	}
	public static unsafe void SaveBitmap<TTexel>(ReadOnlySpan<char> filePath, XYPair<int> dimensions, ReadOnlySpan<TTexel> texels, BitmapSaveConfig config) where TTexel : unmanaged, ITexel<TTexel, byte> {
		if (dimensions.X <= 0 || dimensions.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Texture must have positive width and height.");
		}
		if (dimensions.Area > texels.Length) {
			throw new ArgumentException($"Dimensions indicated a texture area of {dimensions.X}x{dimensions.Y} = {dimensions.Area} texels, " +
										$"but given texel span had length of only {texels.Length}.", nameof(texels));
		}
		
		var dataContainsAlpha = TTexel.BlitType switch {
			TexelType.Rgb24 => false,
			TexelType.Rgba32 => true,
			_ => throw new ArgumentException($"Can not save data of texel type '{typeof(TTexel).Name}' as its blit type is '{TTexel.BlitType}'.", nameof(texels))
		};
		var filePathStr = filePath.ToString();

		// This section firstly makes sure the file doesn't exist before invoking stb_image_write (which may or may not handle overwrite well)
		// It also ensures we have permissions to write to the location (and that the location is valid)-- if not we can at least spit out an
		//	exception here from managed .NET land where the detail is likely to be more useful.
		try {
			if (!File.Exists(filePathStr)) File.WriteAllBytes(filePathStr, DummyFileData);
			File.Delete(filePathStr);
		}
		catch (Exception e) {
			throw new InvalidOperationException($"Could not access file at '{filePathStr}' due to an IO error.", e);
		}

		var interopStringBuffer = _threadLocalStringBuffer.Value!;
		_ = interopStringBuffer.ConvertFromUtf16OrThrowIfBufferTooSmall(filePath, $"Given file path '{filePathStr}' was too long (max length is {MaxFilePathLength} chars).");

		var reverseVertical = !config.FlipVertical; // By default we flip vertical (because the convention is inverse to BMP), so invert the config
		var reverseHorizontal = config.FlipHorizontal;

		// In this specific case, we can blit the data straight across without any copying or manipulation
		if (dataContainsAlpha == config.IncludeAlphaChannel && !reverseVertical && !reverseHorizontal) {
			fixed (TTexel* dataPtr = texels) {
				WriteTexelsToDisk(
					in interopStringBuffer.BufferRef,
					dimensions.X,
					dimensions.Y,
					TTexel.SerializationByteSpanLength,
					dataPtr
				).ThrowIfFailure();
			}
			return;
		}

		// Otherwise we need to manipulate the data first before passing it
		static void CopyAlreadyCorrectBlitType<T>(XYPair<int> size, ReadOnlySpan<T> src, Span<T> dest, bool reverseVertical, bool reverseHorizontal) {
			switch ((reverseHorizontal, reverseVertical)) {
				case (true, false): {
					var ySizeLessOne = size.Y - 1;
					for (var y = 0; y < size.Y; ++y) {
						src[(y * size.X)..((y + 1) * size.X)].CopyTo(dest[((ySizeLessOne - (y + 1)) * size.X)..((ySizeLessOne - y) * size.X)]);
					}
					break;
				}
				case (true, true): {
					var xSizeLessOne = size.X - 1;
					var ySizeLessOne = size.Y - 1;
					for (var y = 0; y < size.Y; ++y) {
						var rowStart = y * size.X;
						var rowStartInverted = (ySizeLessOne - y) * size.X;
						for (var x = 0; x < size.X; ++x) {
							dest[rowStart + x] = src[rowStartInverted + (xSizeLessOne - x)];
						}
					}
					break;
				}
				case (false, true): {
					var xSizeLessOne = size.X - 1;
					for (var y = 0; y < size.Y; ++y) {
						var rowStart = y * size.X;
						for (var x = 0; x < size.X; ++x) {
							dest[rowStart + x] = src[rowStart + (xSizeLessOne - x)];
						}
					}
					break;
				}
				default:
					throw new InvalidOperationException("Expected to reverse one or both directions.");
			}
		}
		static void CopyAndConvertBlitType<TSource>(XYPair<int> size, ReadOnlySpan<TSource> src, Span<byte> dest, bool reverseVertical, bool reverseHorizontal, bool destExpectsAlpha) where TSource : unmanaged, ITexel<TSource, byte> {
			var xSizeLessOne = size.X - 1;
			var ySizeLessOne = size.Y - 1;

			if (destExpectsAlpha) {
				var rgbaDest = MemoryMarshal.Cast<byte, TexelRgba32>(dest);
				for (var y = 0; y < size.Y; ++y) {
					var rowStartDest = y * size.X;
					var rowStartSrc = reverseVertical ? (ySizeLessOne - y) * size.X : rowStartDest;
					for (var x = 0; x < size.X; ++x) {
						var srcTexel = src[rowStartSrc + (reverseHorizontal ? xSizeLessOne - x : x)];
						rgbaDest[rowStartDest + x] = new TexelRgba32(srcTexel[0], srcTexel[1], srcTexel[2], Byte.MaxValue);
					}
				}
			}
			else {
				var rgbDest = MemoryMarshal.Cast<byte, TexelRgb24>(dest);
				for (var y = 0; y < size.Y; ++y) {
					var rowStartDest = y * size.X;
					var rowStartSrc = reverseVertical ? (ySizeLessOne - y) * size.X : rowStartDest;
					for (var x = 0; x < size.X; ++x) {
						var srcTexel = src[rowStartSrc + (reverseHorizontal ? xSizeLessOne - x : x)];
						rgbDest[rowStartDest + x] = new TexelRgb24(srcTexel[0], srcTexel[1], srcTexel[2]);
					}
				}
			}
		}

		var bytesPerTexel = config.IncludeAlphaChannel ? TexelRgba32.TexelSizeBytes : TexelRgb24.TexelSizeBytes;
		var dataBuffer = _dataBufferPool.Rent(bytesPerTexel * dimensions.Area);
		try {
			if ((config.IncludeAlphaChannel && TTexel.BlitType == TexelType.Rgba32) || (!config.IncludeAlphaChannel && TTexel.BlitType == TexelType.Rgb24)) {
				CopyAlreadyCorrectBlitType(
					dimensions, 
					texels[..dimensions.Area], 
					MemoryMarshal.Cast<byte, TTexel>(dataBuffer.AsSpan()), 
					reverseVertical, 
					reverseHorizontal
				);
			}
			else {
				CopyAndConvertBlitType(
					dimensions,
					texels[..dimensions.Area],
					dataBuffer.AsSpan(),
					reverseVertical,
					reverseHorizontal,
					config.IncludeAlphaChannel
				);
			}

			fixed (byte* dataPtr = dataBuffer.AsSpan()) {
				WriteTexelsToDisk(
					in interopStringBuffer.BufferRef,
					dimensions.X,
					dimensions.Y,
					bytesPerTexel,
					dataPtr
				).ThrowIfFailure();
			}
		}
		finally {
			_dataBufferPool.Return(dataBuffer);
		}
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "write_texels_to_disk")]
	static extern unsafe InteropResult WriteTexelsToDisk(
		ref readonly byte utf8FileNameBufferPtr,
		int width,
		int height,
		int bytesPerTexel,
		void* dataPtr
	);
	#endregion
}