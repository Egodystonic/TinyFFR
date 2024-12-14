// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct LightCreationConfig {
	public static readonly Location DefaultInitialPosition = Location.Origin;
	public static readonly ColorVect DefaultInitialColor = StandardColor.White;

	public ReadOnlySpan<char> Name { get; init; }

	public Location InitialPosition { get; init; } = DefaultInitialPosition;

	public ColorVect InitialColor { get; init; } = DefaultInitialColor;

	public LightCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}