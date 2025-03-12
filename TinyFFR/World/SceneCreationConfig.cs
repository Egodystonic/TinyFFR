// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct SceneCreationConfig {
	public static readonly ColorVect DefaultInitialBackdropColor = ColorVect.FromRgb24(0x43A8D3);

	public ReadOnlySpan<char> Name { get; init; }
	public ColorVect? InitialBackdropColor { get; init; } = DefaultInitialBackdropColor;

	public SceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}