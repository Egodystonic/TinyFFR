// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Input;

public sealed record InputTrackerConfig {
	readonly int _maxControllerNameLength = 200;

	public int MaxControllerNameLength {
		get => _maxControllerNameLength;
		init {
			if (value <= 0) throw new ArgumentOutOfRangeException(nameof(MaxControllerNameLength), value, $"Must be at least 1.");
			_maxControllerNameLength = value;
		}
	}
}