// Created on 2025-08-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers;
using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR;

public static class ImageUtils {
	const int MaxFilePathLength = 1024;
	const int Max
	static ReadOnlySpan<byte> DummyFileData => "tinyffr"u8;
	static ThreadLocal<InteropStringBuffer> _threadLocalStringBuffer = new(() => new InteropStringBuffer(MaxFilePathLength, true), trackAllValues: false);
	static ArrayPool<byte> _

	public readonly record struct BitmapSaveConfig(bool IncludeAlphaChannel, bool FlipVertical, bool FlipHorizontal);

	public static void SaveBitmap<TTexel>(ReadOnlySpan<char> filePath, XYPair<int> dimensions, ReadOnlySpan<TTexel> texels) where TTexel : unmanaged, ITexel<TTexel> {
		SaveBitmap(filePath, dimensions, texels, new(IncludeAlphaChannel: false, FlipVertical: false, FlipHorizontal: false));
	}
	public static unsafe void SaveBitmap<TTexel>(ReadOnlySpan<char> filePath, XYPair<int> dimensions, ReadOnlySpan<TTexel> texels, BitmapSaveConfig config) where TTexel : unmanaged, ITexel<TTexel> {
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

		// In this specific case, we can blit the data straight across without any copying or manipulation
		if (dataContainsAlpha == config.IncludeAlphaChannel && config is { FlipVertical: true, FlipHorizontal: false }) {
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
		var dataBuffer = 
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