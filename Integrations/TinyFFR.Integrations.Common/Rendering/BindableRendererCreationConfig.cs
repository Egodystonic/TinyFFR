// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Controls how a bindable renderer is created: one that draws in to an internal buffer so that a user interface framework can display the result.
/// </summary>
/// <remarks>
/// <para>
/// A bindable renderer is what a UI framework's scene view control binds to. Rather than drawing to a window of its own, it renders
/// to a buffer and hands each finished frame to whichever control it is bound to.
/// </para>
/// <para>
/// Everything an ordinary renderer accepts is available through <see cref="BaseConfig"/>, with the most commonly used settings
/// surfaced directly on this type as well.
/// </para>
/// </remarks>
public readonly ref struct BindableRendererCreationConfig : IConfigStruct<BindableRendererCreationConfig> {
	/// <summary>
	/// The default value for <see cref="AutoUpdateCameraAspectRatio"/>, taken from <see cref="RendererCreationConfig.DefaultAutoUpdateCameraAspectRatio"/>.
	/// </summary>
	public const bool DefaultAutoUpdateCameraAspectRatio = RendererCreationConfig.DefaultAutoUpdateCameraAspectRatio;
	/// <summary>
	/// The default value for <see cref="GpuSynchronizationFrameBufferCount"/>, taken from <see cref="RendererCreationConfig.DefaultGpuSynchronizationFrameBufferCount"/>.
	/// </summary>
	public const int DefaultGpuSynchronizationFrameBufferCount = RendererCreationConfig.DefaultGpuSynchronizationFrameBufferCount;
	/// <summary>
	/// The smallest permitted value for <see cref="GpuSynchronizationFrameBufferCount"/>, taken from <see cref="RendererCreationConfig.MinGpuSynchronizationFrameBufferCount"/>.
	/// </summary>
	public const int MinGpuSynchronizationFrameBufferCount = RendererCreationConfig.MinGpuSynchronizationFrameBufferCount;
	/// <summary>
	/// The largest permitted value for <see cref="GpuSynchronizationFrameBufferCount"/>, taken from <see cref="RendererCreationConfig.MaxGpuSynchronizationFrameBufferCount"/>.
	/// </summary>
	public const int MaxGpuSynchronizationFrameBufferCount = RendererCreationConfig.MaxGpuSynchronizationFrameBufferCount;
	/// <summary>
	/// The default value for <see cref="DefaultBufferSize"/>: <c>(960, 540)</c>.
	/// </summary>
	public static readonly XYPair<int> DefaultDefaultBufferSize = (960, 540);

	/// <summary>
	/// The size the renderer's internal buffer starts at, in pixels. Defaults to <see cref="DefaultDefaultBufferSize"/>: <c>(960, 540)</c>.
	/// </summary>
	/// <remarks>
	/// The buffer is resized to match whichever control the renderer is bound to as soon as that control reports its size, so this
	/// only governs the very first frame. Both components must be positive.
	/// </remarks>
	public XYPair<int> DefaultBufferSize { get; init; } = DefaultDefaultBufferSize;

	/// <summary>
	/// The settings common to every renderer, such as its name and quality configuration.
	/// </summary>
	public RendererCreationConfig BaseConfig { get; init; } = new();

	/// <summary>
	/// Whether the target camera's aspect ratio is kept in step with the size of the control the renderer is bound to. Defaults to <see cref="DefaultAutoUpdateCameraAspectRatio"/>.
	/// </summary>
	/// <remarks>
	/// Set this to <see langword="false"/> where one camera is shared between several views, since each would otherwise fight the
	/// others over the aspect ratio. This is <see cref="RendererCreationConfig.AutoUpdateCameraAspectRatio"/> on
	/// <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public bool AutoUpdateCameraAspectRatio {
		get => BaseConfig.AutoUpdateCameraAspectRatio;
		init => BaseConfig = BaseConfig with { AutoUpdateCameraAspectRatio = value };
	}
	/// <summary>
	/// How many frames the CPU may run ahead of the GPU before rendering blocks to let it catch up. Defaults to <see cref="DefaultGpuSynchronizationFrameBufferCount"/>.
	/// </summary>
	/// <remarks>
	/// This is <see cref="RendererCreationConfig.GpuSynchronizationFrameBufferCount"/> on <see cref="BaseConfig"/>, surfaced here
	/// for convenience; see that property for what the values mean and for the constraints that apply when setting <c>-1</c>.
	/// </remarks>
	public int GpuSynchronizationFrameBufferCount {
		get => BaseConfig.GpuSynchronizationFrameBufferCount;
		init => BaseConfig = BaseConfig with { GpuSynchronizationFrameBufferCount = value };
	}
	/// <summary>
	/// The quality settings the renderer starts with, or <see langword="null"/> to use whatever is recommended for the scene and target. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// This is <see cref="RendererCreationConfig.Quality"/> on <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public RenderQualityConfig? Quality {
		get => BaseConfig.Quality;
		init => BaseConfig = BaseConfig with { Quality = value };
	} 
	/// <summary>
	/// The name to give the renderer. May be left empty.
	/// </summary>
	/// <remarks>
	/// This is <see cref="RendererCreationConfig.Name"/> on <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Constructs a new <see cref="BindableRendererCreationConfig"/> with default values for every property.
	/// </summary>
	public BindableRendererCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="BindableRendererCreationConfig"/> from the given common renderer settings.
	/// </summary>
	/// <param name="baseConfig">The value for <see cref="BaseConfig"/>.</param>
	public BindableRendererCreationConfig(RendererCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (DefaultBufferSize.X <= 0 || DefaultBufferSize.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(DefaultBufferSize), DefaultBufferSize, "Both X and Y component must be positive.");
		}
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in BindableRendererCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>() // DefaultBufferSize
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in BindableRendererCreationConfig src) {
		SerializationWrite(ref dest, src.DefaultBufferSize);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc />
	public static BindableRendererCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var bufferSize = SerializationRead<XYPair<int>>(ref src);
		var subConfig = SerializationReadSubConfig<RendererCreationConfig>(ref src);
		return new() {
			DefaultBufferSize = bufferSize,
			BaseConfig = subConfig,
			Quality = subConfig.Quality
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}