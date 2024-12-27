// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials.Textures;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct TextureCreationConfig {
	public required int Width { get; init; }
	public required int Height { get; init; }
	public bool GenerateMipMaps { get; init; } = true;

	public ReadOnlySpan<char> Name { get; init; }

	public TextureCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}