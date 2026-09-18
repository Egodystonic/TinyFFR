// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IConfigStruct{TSelf}"/> passed to the <see cref="IRendererBuilder"/> when creating a <see cref="Renderer"/>.
/// </summary>
public readonly ref struct RendererCreationConfig : IConfigStruct<RendererCreationConfig> {
	/// <summary>
	/// The default value of <see cref="AutoUpdateCameraAspectRatio"/>: <c>true</c>.
	/// </summary>
	public const bool DefaultAutoUpdateCameraAspectRatio = true;
	/// <summary>
	/// The default value of <see cref="GpuSynchronizationFrameBufferCount"/>: <c>3</c>.
	/// </summary>
	public const int DefaultGpuSynchronizationFrameBufferCount = 3;
	/// <summary>
	/// The minimum permitted value of <see cref="GpuSynchronizationFrameBufferCount"/>: <c>-1</c>.
	/// </summary>
	public const int MinGpuSynchronizationFrameBufferCount = -1;
	/// <summary>
	/// The maximum permitted value of <see cref="GpuSynchronizationFrameBufferCount"/>: <c>5</c>.
	/// </summary>
	public const int MaxGpuSynchronizationFrameBufferCount = LocalFrameSynchronizationManager.MaxBufferSize;

	/// <summary>
	/// If <c>true</c>, when the created <see cref="Renderer"/>'s <see cref="Renderer.TargetWindow">TargetWindow</see> is resized,
	/// the <see cref="Camera.AspectRatio">AspectRatio</see> of the <see cref="Renderer.TargetCamera">TargetCamera</see> will automatically
	/// be updated to match. Defaults to <see cref="DefaultAutoUpdateCameraAspectRatio"/>.
	/// </summary>
	/// <remarks>
	/// Generally speaking this is a useful utility to have, but if you're sharing a camera across multiple target windows you may wish to
	/// set this to <c>false</c>.
	/// </remarks>
	public bool AutoUpdateCameraAspectRatio { get; init; } = DefaultAutoUpdateCameraAspectRatio;

	/// <summary>
	/// This property controls the balance between throughput (e.g. framerate) and input latency. Defaults to <see cref="DefaultGpuSynchronizationFrameBufferCount"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is an advanced option that controls how this renderer synchronizes the CPU with the GPU; it controls how many frames can be "in progress" on the
	/// GPU side before the CPU waits in order to not get too far ahead.
	/// </para>
	/// <para>
	/// Values between 1 and 5 set a maximum number of frames that can be "queued" or "in progress" before the call to Render() will block the calling thread.
	/// A higher value generally increases your average throughput/FPS, but can also increase input latency.
	/// </para>
	/// <para>
	/// A value of 0 completely stops all asynchronous rendering. This means every call to Render() will always block the calling thread until the frame is fully rendered and displayed on the target/Window.
	/// Setting this value can drastically lower average throughput/FPS; a value of at least 1 is recommended in most scenarios; but setting it to 0 can still be desirable in some applications (e.g. competitive
	/// video games). 
	/// </para>
	/// <para>
	/// A value of -1 disables synchronization entirely. This means Render() will never block the calling thread; but over time commands submitted to the GPU may exceed the GPU's capability to keep up,
	/// resulting in stuttering or even errors. Setting this value is only recommended when using a multi-renderer setup (set all renderers except your last/"primary" renderer to -1).
	/// </para>
	/// <para>
	/// <b>It should be noted that setting this property to -1 without fulfilling these constraints can result in crashes or exceptions being thrown seemingly at random.</b>
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to set a value outside the range
	/// <see cref="MinGpuSynchronizationFrameBufferCount"/> to <see cref="MaxGpuSynchronizationFrameBufferCount"/> inclusive.</exception>
	public int GpuSynchronizationFrameBufferCount {
		get;
		init {
			if (value is < MinGpuSynchronizationFrameBufferCount or > MaxGpuSynchronizationFrameBufferCount) {
				throw new ArgumentOutOfRangeException(
					nameof(GpuSynchronizationFrameBufferCount),
					value,
					$"Must be between {nameof(MinGpuSynchronizationFrameBufferCount)} ({MinGpuSynchronizationFrameBufferCount}) and " +
					$"{nameof(MaxGpuSynchronizationFrameBufferCount)} ({MaxGpuSynchronizationFrameBufferCount}) (inclusive)."
				);
			}

			field = value;
		}
	} = DefaultGpuSynchronizationFrameBufferCount;

	/// <summary>
	/// The initial quality configuration for the created <see cref="Renderer"/>, or <c>null</c> to use the recommended
	/// config for your scene &amp; render target.
	/// </summary>
	public RenderQualityConfig? Quality { get; init; } = null;

	/// <summary>
	/// The name given to the new <see cref="Renderer"/>.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Creates a new config object with all default values set.
	/// </summary>
	public RendererCreationConfig() { }

	internal void ThrowIfInvalid() {
		/* no-op */
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in RendererCreationConfig src) {
		return	SerializationSizeOfBool() // AutoUpdateCameraAspectRatio
			+	SerializationSizeOfInt() // GpuSynchronizationFrameBufferCount
			+	SerializationSizeOfBool() // Quality.HasValue
			+	SerializationSizeOfSubConfig(src.Quality ?? new()) // Quality
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RendererCreationConfig src) {
		SerializationWriteBool(ref dest, src.AutoUpdateCameraAspectRatio);
		SerializationWriteInt(ref dest, src.GpuSynchronizationFrameBufferCount);
		SerializationWriteBool(ref dest, src.Quality.HasValue);
		SerializationWriteSubConfig(ref dest, src.Quality ?? new());
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc />
	public static RendererCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var autoUpdateCameraAspectRatio = SerializationReadBool(ref src);
		var gpuSynchronizationFrameBufferCount = SerializationReadInt(ref src);
		var qualityHasValue = SerializationReadBool(ref src);
		var quality = SerializationReadSubConfig<RenderQualityConfig>(ref src);
		var name = SerializationReadString(ref src);
		return new() {
			AutoUpdateCameraAspectRatio = autoUpdateCameraAspectRatio,
			GpuSynchronizationFrameBufferCount = gpuSynchronizationFrameBufferCount,
			Quality = qualityHasValue ? quality : null,
			Name = name
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}