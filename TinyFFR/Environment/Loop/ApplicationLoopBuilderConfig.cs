// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment.Loop;

public sealed record ApplicationLoopBuilderConfig {
	readonly InputTrackerConfig _inputTrackerConfig = new();

	public InputTrackerConfig InputTrackerConfig {
		get => _inputTrackerConfig;
		init {
			ArgumentNullException.ThrowIfNull(value);
			_inputTrackerConfig = value;
		}
	}
}