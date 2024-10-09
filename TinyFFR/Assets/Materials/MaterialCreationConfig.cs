// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct MaterialCreationConfig {
	public bool IgnoresLighting { get; init; } = false;

	public ReadOnlySpan<char> NameAsSpan { get; init; }
	public string Name {
		get => new(NameAsSpan);
		init => NameAsSpan = value.AsSpan();
	}

	public MaterialCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}