// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct BackdropTextureCreationConfig {
	public ReadOnlySpan<char> Name { get; init; }

	public BackdropTextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}