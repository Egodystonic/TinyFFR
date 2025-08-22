// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct RendererCreationConfig : IConfigStruct<RendererCreationConfig> {
	public const bool DefaultAutoUpdateCameraAspectRatio = true;
	public const int DefaultGpuSynchronizationFrameBufferCount = 3;
	public const int MinGpuSynchronizationFrameBufferCount = -1;
	public const int MaxGpuSynchronizationFrameBufferCount = LocalFrameSynchronizationManager.MaxBufferSize;
	public static readonly RenderQualityConfig DefaultQuality = new();

	readonly int _gpuSynchronizationFrameBufferCount = DefaultGpuSynchronizationFrameBufferCount;

	public bool AutoUpdateCameraAspectRatio { get; init; } = DefaultAutoUpdateCameraAspectRatio;
	
	public int GpuSynchronizationFrameBufferCount {
		get => _gpuSynchronizationFrameBufferCount;
		init {
			if (value is < MinGpuSynchronizationFrameBufferCount or > MaxGpuSynchronizationFrameBufferCount) {
				throw new ArgumentOutOfRangeException(
					nameof(GpuSynchronizationFrameBufferCount), 
					value, 
					$"Must be between {nameof(MinGpuSynchronizationFrameBufferCount)} ({MinGpuSynchronizationFrameBufferCount}) and " +
					$"{nameof(MaxGpuSynchronizationFrameBufferCount)} ({MaxGpuSynchronizationFrameBufferCount}) (inclusive)."
				);
			}
			_gpuSynchronizationFrameBufferCount = value;
		}
	}

	public RenderQualityConfig Quality { get; init; } = DefaultQuality;

	public ReadOnlySpan<char> Name { get; init; }

	public RendererCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	public static int GetHeapStorableLength(in RendererCreationConfig src) {
		return	SerializationSizeOf(src.AutoUpdateCameraAspectRatio)
			+	SerializationSizeOf(src.GpuSynchronizationFrameBufferCount)
			+	SerializationSizeOf(src.Quality)
			+	SerializationSizeOf(src.Name);
	}
	public static void ConvertToHeapStorable(Span<byte> dest, in RendererCreationConfig src) {
		SerializationWrite(ref dest, src.AutoUpdateCameraAspectRatio);
		SerializationWrite(ref dest, src.GpuSynchronizationFrameBufferCount);
		SerializationWrite(ref dest, src.Quality);
		SerializationWrite(ref dest, src.Name);
	}
	public static RendererCreationConfig DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new() {
			AutoUpdateCameraAspectRatio = SerializationReadBool(ref src),
			GpuSynchronizationFrameBufferCount = SerializationReadInt(ref src),
			Quality = SerializationReadSubConfig<RenderQualityConfig>(ref src),
			Name = SerializationReadString(ref src)
		};
	}
}