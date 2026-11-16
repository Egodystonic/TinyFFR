// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct ExportableRenderSurfaceCreationConfig : IConfigStruct<ExportableRenderSurfaceCreationConfig> {
	public static readonly XYPair<int> DefaultDimensions = (960, 540);
	public const int MaxDimensionXY = RenderOutputBufferCreationConfig.MaxTextureDimensionXY;
	public const int MinDimensionXY = RenderOutputBufferCreationConfig.MinTextureDimensionXY;

	public XYPair<int> Dimensions { get; init; } = DefaultDimensions;

	public ReadOnlySpan<char> Name { get; init; }

	public ExportableRenderSurfaceCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(ExportableRenderSurfaceCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Dimensions.Clamp(new(MinDimensionXY), new(MaxDimensionXY)) != Dimensions) {
			ThrowArgException(Dimensions, $"must have both X and Y values between {MinDimensionXY} and {MaxDimensionXY}.");
		}
	}

	public static int GetHeapStorageFormattedLength(in ExportableRenderSurfaceCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>()
			+	SerializationSizeOfString(src.Name);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ExportableRenderSurfaceCreationConfig src) {
		SerializationWrite(ref dest, src.Dimensions);
		SerializationWriteString(ref dest, src.Name);
	}
	public static ExportableRenderSurfaceCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Dimensions = SerializationRead<XYPair<int>>(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
