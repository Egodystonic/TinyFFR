// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct RenderOutputBufferCreationConfig : IConfigStruct<RenderOutputBufferCreationConfig> {
	public static readonly XYPair<int> DefaultTextureDimensions = (2560, 1440);
	public const int MaxTextureDimensionXY = 32_768;
	public const int MinTextureDimensionXY = 1;

	public XYPair<int> TextureDimensions { get; init; } = DefaultTextureDimensions;

	public ReadOnlySpan<char> Name { get; init; }

	public RenderOutputBufferCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(RenderOutputBufferCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (TextureDimensions.Clamp(new(MinTextureDimensionXY), new(MaxTextureDimensionXY)) != TextureDimensions) {
			ThrowArgException(TextureDimensions, $"must have both X and Y values between {MinTextureDimensionXY} and {MaxTextureDimensionXY}.");
		}
	}

	public static int GetHeapStorageFormattedLength(in RenderOutputBufferCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>() // TextureDimensions
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderOutputBufferCreationConfig src) {
		SerializationWrite(ref dest, src.TextureDimensions);
		SerializationWriteString(ref dest, src.Name);
	}
	public static RenderOutputBufferCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			TextureDimensions = SerializationRead<XYPair<int>>(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}