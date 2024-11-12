// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Scene;

public readonly ref struct ModelInstanceCreationConfig {
	public static readonly Transform DefaultInitialTransform = Transform.None;

	public ReadOnlySpan<char> Name { get; init; }

	public Transform InitialTransform { get; init; } = DefaultInitialTransform;

	public ModelInstanceCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}