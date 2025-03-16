// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public sealed record WindowBuilderConfig {
	public const int DefaultMaxWindowTitleLength = 200;
	public const int DefaultMaxIconFilePathLengthChars = 2048;

	readonly int _maxWindowTitleLength = DefaultMaxWindowTitleLength;
	public int MaxWindowTitleLength {
		get => _maxWindowTitleLength;
		init {
			if (value <= 0) throw new ArgumentOutOfRangeException(nameof(MaxWindowTitleLength), value, $"Must be at least 1.");
			_maxWindowTitleLength = value;
		}
	}

	readonly int _maxIconFilePathLengthChars = DefaultMaxIconFilePathLengthChars;
	public int MaxIconFilePathLengthChars {
		get => _maxIconFilePathLengthChars;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(MaxIconFilePathLengthChars), value, $"Must be at least 1.");
			}
			_maxIconFilePathLengthChars = value;
		}
	}
}