// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxAssetSizeBytes = 1 << 29;
	public const int DefaultMaxAssetSizeBytes = 1024 * 1024;

	readonly int _maxAssetSizeBytes = DefaultMaxAssetSizeBytes;
	public int MaxAssetSizeBytes {
		get => _maxAssetSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxAssetSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset size must be between 0 and {MaxMaxAssetSizeBytes} bytes.");
			}
			_maxAssetSizeBytes = value;
		}
	}
}