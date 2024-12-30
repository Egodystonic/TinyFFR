// Created on 2024-10-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Factory.Local;

public sealed class LocalTinyFfrFactoryConfig {
	public const int MaxMaxCpuToGpuAssetTransferSizeBytes = 1 << 29;
	public const int DefaultMaxCpuToGpuAssetTransferSizeBytes = 1024 * 1024 * 100; // 100 MB 

	readonly int _maxCpuToGpuAssetTransferSizeBytes = DefaultMaxCpuToGpuAssetTransferSizeBytes;
	public int MaxCpuToGpuAssetTransferSizeBytes {
		get => _maxCpuToGpuAssetTransferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxCpuToGpuAssetTransferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset size must be between 1 and {MaxMaxCpuToGpuAssetTransferSizeBytes} bytes.");
			}
			_maxCpuToGpuAssetTransferSizeBytes = value;
		}
	}
}