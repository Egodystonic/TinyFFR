// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public sealed record WindowBuilderCreationConfig {
	readonly int _maxWindowTitleLength = 200;

	public int MaxWindowTitleLength {
		get => _maxWindowTitleLength;
		init {
			if (value <= 0) throw new ArgumentOutOfRangeException(nameof(MaxWindowTitleLength), value, $"Must be at least 1.");
			_maxWindowTitleLength = value;
		}
	}
}