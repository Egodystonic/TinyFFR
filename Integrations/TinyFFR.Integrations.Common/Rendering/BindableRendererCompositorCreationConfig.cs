// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct BindableRendererCompositorCreationConfig {
	public static readonly XYPair<int> DefaultDefaultBufferSize = BindableRendererCreationConfig.DefaultDefaultBufferSize;

	public XYPair<int> DefaultBufferSize { get; init; } = DefaultDefaultBufferSize;

	public ReadOnlySpan<char> Name { get; init; }

	public BindableRendererCompositorCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (DefaultBufferSize.X <= 0 || DefaultBufferSize.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(DefaultBufferSize), DefaultBufferSize, "Both X and Y component must be positive.");
		}
	}
}
