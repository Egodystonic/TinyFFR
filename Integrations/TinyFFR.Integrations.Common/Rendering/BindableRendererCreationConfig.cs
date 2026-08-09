// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct BindableRendererCreationConfig : IConfigStruct<BindableRendererCreationConfig> {
	public const bool DefaultAutoUpdateCameraAspectRatio = RendererCreationConfig.DefaultAutoUpdateCameraAspectRatio;
	public const int DefaultGpuSynchronizationFrameBufferCount = RendererCreationConfig.DefaultGpuSynchronizationFrameBufferCount;
	public const int MinGpuSynchronizationFrameBufferCount = RendererCreationConfig.MinGpuSynchronizationFrameBufferCount;
	public const int MaxGpuSynchronizationFrameBufferCount = RendererCreationConfig.MaxGpuSynchronizationFrameBufferCount;
	public static readonly XYPair<int> DefaultDefaultBufferSize = (960, 540);

	public XYPair<int> DefaultBufferSize { get; init; } = DefaultDefaultBufferSize;

	public RendererCreationConfig BaseConfig { get; init; } = new() { Quality = RenderQualityConfig.Default };

	public bool AutoUpdateCameraAspectRatio {
		get => BaseConfig.AutoUpdateCameraAspectRatio;
		init => BaseConfig = BaseConfig with { AutoUpdateCameraAspectRatio = value };
	}
	public int GpuSynchronizationFrameBufferCount {
		get => BaseConfig.GpuSynchronizationFrameBufferCount;
		init => BaseConfig = BaseConfig with { GpuSynchronizationFrameBufferCount = value };
	}
	public required RenderQualityConfig Quality {
		get => BaseConfig.Quality;
		init => BaseConfig = BaseConfig with { Quality = value };
	} 
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public BindableRendererCreationConfig() { }
	public BindableRendererCreationConfig(RendererCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (DefaultBufferSize.X <= 0 || DefaultBufferSize.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(DefaultBufferSize), DefaultBufferSize, "Both X and Y component must be positive.");
		}
	}

	public static int GetHeapStorageFormattedLength(in BindableRendererCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>() // DefaultBufferSize
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in BindableRendererCreationConfig src) {
		SerializationWrite(ref dest, src.DefaultBufferSize);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	public static BindableRendererCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var bufferSize = SerializationRead<XYPair<int>>(ref src);
		var subConfig = SerializationReadSubConfig<RendererCreationConfig>(ref src);
		return new() {
			DefaultBufferSize = bufferSize,
			BaseConfig = subConfig,
			Quality = subConfig.Quality
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}