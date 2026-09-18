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
