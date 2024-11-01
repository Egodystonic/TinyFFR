// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Scene;

public readonly ref struct SceneCreationConfig {
	public ReadOnlySpan<char> NameAsSpan { get; init; }
	public string Name {
		get => new(NameAsSpan);
		init => NameAsSpan = value.AsSpan();
	}

	public SceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}