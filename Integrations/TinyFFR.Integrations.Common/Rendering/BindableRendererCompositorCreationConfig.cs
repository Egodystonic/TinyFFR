// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Controls how a bindable compositor is created: one that combines several renderers' output in to a single image for a user interface framework to display.
/// </summary>
/// <remarks>
/// Use a compositor in place of a plain bindable renderer where a scene view must show several render passes stacked together, such
/// as a three-dimensional scene with a user interface drawn over it.
/// </remarks>
public readonly ref struct BindableRendererCompositorCreationConfig {
	/// <summary>
	/// The default value for <see cref="DefaultBufferSize"/>: <c>(960, 540)</c>.
	/// </summary>
	public static readonly XYPair<int> DefaultDefaultBufferSize = BindableRendererCreationConfig.DefaultDefaultBufferSize;

	/// <summary>
	/// The size the renderer's internal buffer starts at, in pixels. Defaults to <see cref="DefaultDefaultBufferSize"/>: <c>(960, 540)</c>.
	/// </summary>
	/// <remarks>
	/// The buffer is resized to match whichever control the renderer is bound to as soon as that control reports its size, so this
	/// only governs the very first frame. Both components must be positive.
	/// </remarks>
	public XYPair<int> DefaultBufferSize { get; init; } = DefaultDefaultBufferSize;

	/// <summary>
	/// If <see langword="true"/>, the compositor always hands frames to its bound control by copying them through the CPU, even where they could be shared
	/// directly on the GPU. Defaults to <see cref="BindableRendererCreationConfig.DefaultPreferCpuRenderBuffer"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// By default, where the platform and the UI framework support it, the bound control receives frames as GPU images shared directly with the
	/// framework's compositor ("GPU shared frames"), which avoids copying every frame back to the CPU and uploading it again. Where that is not possible
	/// (e.g. an unsupported platform, rendering backend or framework rendering mode, or the renderer and the framework running on different GPUs),
	/// the control falls back to CPU-copied frames automatically and a message explaining the cause is written to the console.
	/// </para>
	/// <para>
	/// When frames are shared on the GPU, the number that may be in flight to the UI framework is governed by the lowest non-negative
	/// <see cref="BindableRendererCreationConfig.GpuSynchronizationFrameBufferCount"/> among the compositor's added renderers.
	/// </para>
	/// <para>
	/// Set this to <see langword="true"/> to always use CPU-copied frames, e.g. to rule out a driver or interop problem, or where the frames
	/// must be readable on the CPU anyway.
	/// </para>
	/// </remarks>
	public bool PreferCpuRenderBuffer { get; init; } = BindableRendererCreationConfig.DefaultPreferCpuRenderBuffer;

	/// <summary>
	/// The name to give the compositor. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="BindableRendererCompositorCreationConfig"/> with default values for every property.
	/// </summary>
	public BindableRendererCompositorCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (DefaultBufferSize.X <= 0 || DefaultBufferSize.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(DefaultBufferSize), DefaultBufferSize, "Both X and Y component must be positive.");
		}
	}
}
