// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Controls how a backdrop texture is created.
/// </summary>
public readonly ref struct BackdropTextureCreationConfig : IConfigStruct<BackdropTextureCreationConfig> {
	/// <summary>
	/// The name to give the backdrop texture. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="BackdropTextureCreationConfig"/> with default values for every property.
	/// </summary>
	public BackdropTextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in BackdropTextureCreationConfig src) {
		return SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in BackdropTextureCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc />
	public static BackdropTextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new BackdropTextureCreationConfig {
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
