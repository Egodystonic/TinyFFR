// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IConfigStruct{TSelf}"/> passed to the <see cref="IRendererBuilder"/> when creating an <see cref="ExportableRenderSurface"/>.
/// </summary>
public readonly ref struct ExportableRenderSurfaceCreationConfig : IConfigStruct<ExportableRenderSurfaceCreationConfig> {
	/// <summary>
	/// The default value of <see cref="Dimensions"/>: <c>(960, 540)</c>.
	/// </summary>
	public static readonly XYPair<int> DefaultDimensions = (960, 540);
	/// <summary>
	/// The maximum permitted value for either component of <see cref="Dimensions"/>: <c>32,768</c>.
	/// </summary>
	public const int DimensionsMaxComponent = RenderOutputBufferCreationConfig.MaxTextureDimensionXY;
	/// <summary>
	/// The minimum permitted value for either component of <see cref="Dimensions"/>: <c>1</c>.
	/// </summary>
	public const int DimensionsMinComponent = RenderOutputBufferCreationConfig.MinTextureDimensionXY;

	/// <summary>
	/// The default value of <see cref="RequiredImageHandleType"/> for the current operating system.
	/// </summary>
	/// <remarks>
	/// Typically 
	/// <see cref="ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle"/> on Windows, <see cref="ExportableRenderSurfaceImageHandleType.IOSurfaceRef"/> on macOS,
	/// and <see cref="ExportableRenderSurfaceImageHandleType.VulkanOpaqueFileDescriptor"/> on Linux.
	/// </remarks>
	public static ExportableRenderSurfaceImageHandleType DefaultImageHandleTypeForCurrentOperatingSystem {
		get {
			if (OperatingSystem.IsWindows()) return ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle;
			if (OperatingSystem.IsMacOS()) return ExportableRenderSurfaceImageHandleType.IOSurfaceRef;
			return ExportableRenderSurfaceImageHandleType.VulkanOpaqueFileDescriptor;
		}
	}

	/// <summary>
	/// The initial pixel dimensions of the new <see cref="ExportableRenderSurface"/>. Defaults to <see cref="DefaultDimensions"/>.
	/// </summary>
	/// <remarks>
	/// Both <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> must be between <see cref="DimensionsMinComponent"/> and <see cref="DimensionsMaxComponent"/> (inclusive).
	/// The dimensions can be changed later via <see cref="ExportableRenderSurface.Dimensions"/>.
	/// </remarks>
	public XYPair<int> Dimensions { get; init; } = DefaultDimensions;

	/// <summary>
	/// The kind of handle the new surface's images must be exportable as; i.e. the kind that the intended consumer can import. Defaults to <see cref="DefaultImageHandleTypeForCurrentOperatingSystem"/>.
	/// </summary>
	/// <remarks>
	/// Must not be <see cref="ExportableRenderSurfaceImageHandleType.None"/>. If the current platform or device can not provide this handle type,
	/// <see cref="IRendererBuilder.TryCreateExportableRenderSurface(in ExportableRenderSurfaceCreationConfig)"/> returns <see langword="null"/>.
	/// </remarks>
	public ExportableRenderSurfaceImageHandleType RequiredImageHandleType { get; init; } = DefaultImageHandleTypeForCurrentOperatingSystem;

	/// <summary>
	/// The name given to the new <see cref="ExportableRenderSurface"/>.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Creates a new config object with all default values set.
	/// </summary>
	public ExportableRenderSurfaceCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(ExportableRenderSurfaceCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Dimensions.Clamp(new(DimensionsMinComponent), new(DimensionsMaxComponent)) != Dimensions) {
			ThrowArgException(Dimensions, $"must have both X and Y values between {DimensionsMinComponent} and {DimensionsMaxComponent}.");
		}
		if (RequiredImageHandleType == ExportableRenderSurfaceImageHandleType.None || !Enum.IsDefined(RequiredImageHandleType)) {
			ThrowArgException(RequiredImageHandleType, $"must be a defined value other than {nameof(ExportableRenderSurfaceImageHandleType.None)}.");
		}
	}

	/// <inheritdoc cref="IConfigStruct{TSelf}.GetHeapStorageFormattedLength"/>
	public static int GetHeapStorageFormattedLength(in ExportableRenderSurfaceCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>()
			+	SerializationSizeOfInt()
			+	SerializationSizeOfString(src.Name);
	}
	/// <inheritdoc cref="IConfigStruct{TSelf}.AllocateAndConvertToHeapStorage"/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ExportableRenderSurfaceCreationConfig src) {
		SerializationWrite(ref dest, src.Dimensions);
		SerializationWriteInt(ref dest, (int) src.RequiredImageHandleType);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc/>
	public static ExportableRenderSurfaceCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Dimensions = SerializationRead<XYPair<int>>(ref src),
			RequiredImageHandleType = (ExportableRenderSurfaceImageHandleType) SerializationReadInt(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
