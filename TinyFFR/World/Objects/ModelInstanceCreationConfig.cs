// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct ModelInstanceCreationConfig : IConfigStruct<ModelInstanceCreationConfig> {
	public static readonly Transform DefaultInitialTransform = Transform.None;

	public ReadOnlySpan<char> Name { get; init; }

	public Transform InitialTransform { get; init; } = DefaultInitialTransform;

	public ModelInstanceCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	public static int GetHeapStorageFormattedLength(in ModelInstanceCreationConfig src) {
		return	SerializationSizeOf(src.Name)
			+	SerializationSizeOf(src.InitialTransform);
	}
	public static void ConvertToHeapStorageFormat(Span<byte> dest, in ModelInstanceCreationConfig src) {
		SerializationWrite(ref dest, src.Name);
		SerializationWrite(ref dest, src.InitialTransform);
	}
	public static ModelInstanceCreationConfig ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src) {
		return new() {
			Name = SerializationReadString(ref src),
			InitialTransform = SerializationRead<Transform>(ref src)
		};
	}
}