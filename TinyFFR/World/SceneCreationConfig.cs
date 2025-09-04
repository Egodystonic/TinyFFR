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
		return	SerializationSizeOf(src.Name)
			+	SerializationSizeOf(src.InitialBackdropColor.HasValue)
			+	SerializationSizeOf(src.InitialBackdropColor ?? default);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in SceneCreationConfig src) {
		SerializationWrite(ref dest, src.Name);
		SerializationWrite(ref dest, src.InitialBackdropColor.HasValue);
		SerializationWrite(ref dest, src.InitialBackdropColor ?? default);
	}
	public static SceneCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new SceneCreationConfig {
			Name = SerializationReadString(ref src),
			InitialBackdropColor = SerializationReadBool(ref src) ? SerializationRead<ColorVect>(ref src) : null
		};
	}
}