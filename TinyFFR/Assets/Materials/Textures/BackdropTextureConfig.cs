// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct BackdropTextureCreationConfig : IConfigStruct<BackdropTextureCreationConfig> {
	public ReadOnlySpan<char> Name { get; init; }

	public BackdropTextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	public static int GetHeapStorageFormattedLength(in BackdropTextureCreationConfig src) {
		return SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in BackdropTextureCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
	}
	public static BackdropTextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new BackdropTextureCreationConfig {
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
