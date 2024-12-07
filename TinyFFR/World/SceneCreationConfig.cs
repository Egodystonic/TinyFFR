// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct SceneCreationConfig {
	public ReadOnlySpan<char> Name { get; init; }

	public SceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}