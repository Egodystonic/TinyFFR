// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Environment;

public readonly ref struct ApplicationLoopCreationConfig : IConfigStruct<ApplicationLoopCreationConfig> {
	internal readonly TimeSpan FrameInterval = TimeSpan.Zero;

	public int? FrameRateCapHz {
		get {
			return FrameInterval <= TimeSpan.Zero ? null : (int) Math.Round(TimeSpan.FromSeconds(1d) / FrameInterval, 0, MidpointRounding.AwayFromZero);
		}
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateCapHz), value, $"Frame rate cap must be a positive value, or 'null' for no cap.");
			}
			FrameInterval = value != null ? (TimeSpan.FromSeconds(1d) / value.Value) : TimeSpan.Zero;
		}
	}

	public ReadOnlySpan<char> Name { get; init; }

	public ApplicationLoopCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" - Yes, for now. Keeping this as a placeholder for future.
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	public static int GetHeapStorageFormattedLength(in ApplicationLoopCreationConfig src) {
		return	SerializationSizeOfBool(src.FrameRateCapHz.HasValue)
			+	SerializationSizeOfInt(src.FrameRateCapHz ?? 0)
			+	SerializationSizeOfString(src.Name);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ApplicationLoopCreationConfig src) {
		SerializationWriteBool(ref dest, src.FrameRateCapHz.HasValue);
		SerializationWriteInt(ref dest, src.FrameRateCapHz ?? 0);
		SerializationWriteString(ref dest, src.Name);
	}
	public static ApplicationLoopCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ApplicationLoopCreationConfig {
			FrameRateCapHz = SerializationReadNullableInt(ref src),
			Name = SerializationReadString(ref src)
		};
	}
}