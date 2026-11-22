// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct ExportableRenderSurfaceCreationConfig : IConfigStruct<ExportableRenderSurfaceCreationConfig> {
	public static readonly XYPair<int> DefaultDimensions = (960, 540);
	public const int MaxDimensionXY = RenderOutputBufferCreationConfig.MaxTextureDimensionXY;
	public const int MinDimensionXY = RenderOutputBufferCreationConfig.MinTextureDimensionXY;

	public static ExportableRenderSurfaceHandleType DefaultHandleType {
		get {
			if (OperatingSystem.IsWindows()) return ExportableRenderSurfaceHandleType.D3D11TextureNtHandle;
			if (OperatingSystem.IsMacOS()) return ExportableRenderSurfaceHandleType.IOSurfaceRef;
			return ExportableRenderSurfaceHandleType.VulkanOpaqueFileDescriptor;
		}
	}

	public XYPair<int> Dimensions { get; init; } = DefaultDimensions;

	public ExportableRenderSurfaceHandleType HandleType { get; init; } = DefaultHandleType;

	public ReadOnlySpan<char> Name { get; init; }

	public ExportableRenderSurfaceCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(ExportableRenderSurfaceCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Dimensions.Clamp(new(MinDimensionXY), new(MaxDimensionXY)) != Dimensions) {
			ThrowArgException(Dimensions, $"must have both X and Y values between {MinDimensionXY} and {MaxDimensionXY}.");
		}
		if (HandleType == ExportableRenderSurfaceHandleType.None || !Enum.IsDefined(HandleType)) {
			ThrowArgException(HandleType, $"must be a defined value other than {nameof(ExportableRenderSurfaceHandleType.None)}.");
		}
	}

	public static int GetHeapStorageFormattedLength(in ExportableRenderSurfaceCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>()
			+	SerializationSizeOfInt()
			+	SerializationSizeOfString(src.Name);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ExportableRenderSurfaceCreationConfig src) {
		SerializationWrite(ref dest, src.Dimensions);
		SerializationWriteInt(ref dest, (int) src.HandleType);
		SerializationWriteString(ref dest, src.Name);
	}
	public static ExportableRenderSurfaceCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Dimensions = SerializationRead<XYPair<int>>(ref src),
			HandleType = (ExportableRenderSurfaceHandleType) SerializationReadInt(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
