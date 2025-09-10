// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct SceneCreationConfig : IConfigStruct<SceneCreationConfig> {
	public static readonly ColorVect DefaultInitialBackdropColor = ColorVect.FromRgb24(0x43A8D3);

	public ReadOnlySpan<char> Name { get; init; }
	public ColorVect? InitialBackdropColor { get; init; } = DefaultInitialBackdropColor;

	public SceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	public static int GetHeapStorageFormattedLength(in SceneCreationConfig src) {
		return	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfNullable<ColorVect>(); // InitialBackdropColor
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in SceneCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteNullable(ref dest, src.InitialBackdropColor);
	}
	public static SceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new SceneCreationConfig {
			Name = SerializationReadString(ref src),
			InitialBackdropColor = SerializationReadNullable<ColorVect>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}