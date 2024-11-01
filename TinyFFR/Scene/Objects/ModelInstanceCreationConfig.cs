// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Scene;

public readonly ref struct ModelInstanceCreationConfig {
	public ReadOnlySpan<char> NameAsSpan { get; init; }
	public string Name {
		get => new(NameAsSpan);
		init => NameAsSpan = value.AsSpan();
	}

	public Transform InitialTransform { get; init; } = Transform.None;

	public ModelInstanceCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}