// Created on 2024-10-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Factory.Local;

public enum MemoryUsageRubric {
	Standard = 0,
	UseLessMemory = 1,
	UseSignificantlyLessMemory = 2
}

public sealed class LocalTinyFfrFactoryConfig {
	public const int MaxMaxCpuToGpuAssetTransferSizeBytes = 1 << 29;
	public const int DefaultMaxCpuToGpuAssetTransferSizeBytes = 1024 * 1024 * 100; // 100 MB 
	public static readonly MemoryUsageRubric DefaultMemoryUsageRubric = MemoryUsageRubric.Standard;

	public int MaxCpuToGpuAssetTransferSizeBytes {
		get;
		init {
			if (value is <= 0 or > MaxMaxCpuToGpuAssetTransferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset size must be between 1 and {MaxMaxCpuToGpuAssetTransferSizeBytes} bytes.");
			}

			field = value;
		}
	} = DefaultMaxCpuToGpuAssetTransferSizeBytes;

	public MemoryUsageRubric MemoryUsageRubric {
		get;
		init {
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, null);
			field = value;
		}
	} = DefaultMemoryUsageRubric;
}