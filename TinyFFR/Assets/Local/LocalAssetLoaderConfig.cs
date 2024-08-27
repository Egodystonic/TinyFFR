// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxAssetSizeBytes = 1 << 29;
	public const int DefaultMaxAssetSizeBytes = 1024 * 1024;
	public const int MaxMaxAssetNameLength = 10_000;
	public const int DefaultMaxAssetNameLength = 300;

	readonly int _maxAssetSizeBytes = DefaultMaxAssetSizeBytes;
	public int MaxAssetSizeBytes {
		get => _maxAssetSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxAssetSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset size must be between 1 and {MaxMaxAssetSizeBytes} bytes.");
			}
			_maxAssetSizeBytes = value;
		}
	}

	readonly int _maxAssetNameLength = DefaultMaxAssetNameLength;
	public int MaxAssetNameLength {
		get => _maxAssetNameLength;
		init {
			if (value is <= 0 or > MaxMaxAssetNameLength) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset name length be between 1 and {MaxMaxAssetNameLength}.");
			}
			_maxAssetNameLength = value;
		}
	}
}