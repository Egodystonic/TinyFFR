// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct MaterialCreationConfig : IConfigStruct<MaterialCreationConfig> {
	public ReadOnlySpan<char> Name { get; init; }

	public MaterialCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	public static int GetHeapStorageFormattedLength(in MaterialCreationConfig src) {
		return SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MaterialCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
	}
	public static MaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MaterialCreationConfig {
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}