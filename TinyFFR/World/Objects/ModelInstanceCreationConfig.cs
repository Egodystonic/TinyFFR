// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Configuration for creating a new <see cref="ModelInstance"/>.
/// </summary>
public readonly ref struct ModelInstanceCreationConfig : IConfigStruct<ModelInstanceCreationConfig> {
	/// <summary>
	/// The default value for <see cref="InitialTransform"/>: <see cref="Transform.None"/>, i.e. at the origin, unrotated and unscaled.
	/// </summary>
	public static readonly Transform DefaultInitialTransform = Transform.None;

	/// <summary>
	/// Optional name for the new model instance.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Where the new instance should be placed, how it should be oriented, and how large it should be. Defaults to <see cref="DefaultInitialTransform"/>.
	/// </summary>
	public Transform InitialTransform { get; init; } = DefaultInitialTransform;

	/// <summary>
	/// Constructs a new <see cref="ModelInstanceCreationConfig"/> with default values for every setting.
	/// </summary>
	public ModelInstanceCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in ModelInstanceCreationConfig src) {
		return	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOf<Transform>(); // InitialTransform
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ModelInstanceCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWrite(ref dest, src.InitialTransform);
	}
	/// <inheritdoc/>
	public static ModelInstanceCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Name = SerializationReadString(ref src),
			InitialTransform = SerializationRead<Transform>(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}