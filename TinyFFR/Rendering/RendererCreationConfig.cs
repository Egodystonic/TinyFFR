// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering;

public readonly ref struct RendererCreationConfig {
	public ReadOnlySpan<char> Name { get; init; }

	public RendererCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}