// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct MaterialCreationConfig : IConfigStruct<MaterialCreationConfig> {
	public ReadOnlySpan<char> Name { get; init; }
	public bool EnablePerInstanceEffects { get; init; } = false;

	public MaterialCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	internal static void ThrowIfTextureIsNotCorrectTexelType(Texture? texture, TexelType expectedType, [CallerArgumentExpression(nameof(texture))] string? textureName = null) {
		if (texture.HasValue && texture.Value.TexelType != expectedType) {
			throw new ArgumentException($"Texture is required to be of texel type '{expectedType}'; but was '{texture.Value.TexelType}'.", textureName);
		}
	}

	public static int GetHeapStorageFormattedLength(in MaterialCreationConfig src) {
		return SerializationSizeOfString(src.Name) // Name
			 + SerializationSizeOfBool(); // EnablePerInstanceEffects
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MaterialCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteBool(ref dest, src.EnablePerInstanceEffects);
	}
	public static MaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MaterialCreationConfig {
			Name = SerializationReadString(ref src),
			EnablePerInstanceEffects = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}