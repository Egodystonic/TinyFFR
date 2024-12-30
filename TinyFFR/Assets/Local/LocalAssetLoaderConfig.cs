// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxShaderBufferSizeBytes = 1 << 29;
	public const int DefaultMaxShaderBufferSizeBytes = 1024 * 1024 * 1; // 1 MB

	readonly int _maxShaderBufferSizeBytes = DefaultMaxShaderBufferSizeBytes;
	public int MaxShaderBufferSizeBytes {
		get => _maxShaderBufferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxShaderBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max shader buffer size must be between 1 and {MaxMaxShaderBufferSizeBytes} bytes.");
			}
			_maxShaderBufferSizeBytes = value;
		}
	}
}