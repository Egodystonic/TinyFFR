// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Rendering.Local.Sync;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct RendererCreationConfig {
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
}