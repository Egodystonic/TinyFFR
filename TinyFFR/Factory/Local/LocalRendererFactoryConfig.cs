// Created on 2024-10-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Factory.Local;

public sealed class LocalRendererFactoryConfig {
	public const int MaxMaxAssetSizeBytes = 1 << 29;
	public const int DefaultMaxAssetSizeBytes = 1024 * 1024;

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
}