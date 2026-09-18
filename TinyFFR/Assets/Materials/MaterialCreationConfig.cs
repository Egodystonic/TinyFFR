// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Threading;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// The settings shared by every kind of material, nested inside each of the more specific material configs.
/// </summary>
/// <remarks>
/// You rarely construct one of these directly; the properties it holds are surfaced on each specific material config too.
/// </remarks>
public readonly ref struct MaterialCreationConfig : IConfigStruct<MaterialCreationConfig> {
	static readonly Lock _parameterStringMutationLock = new();

	/// <summary>
	/// The name to give the material. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }
	/// <summary>
	/// Whether to allow objects using this material to alter it individually at runtime. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Enabling this lets each object using the material transform its textures or blend them towards a second set. It makes the
	/// material markedly more expensive to render whether or not any object actually uses those effects, so leave it off unless it
	/// is needed.
	/// </remarks>
	public bool EnablePerInstanceEffects { get; init; } = false;

	/// <summary>
	/// Constructs a new <see cref="MaterialCreationConfig"/> with default values for every property.
	/// </summary>
	public MaterialCreationConfig() { }

	internal static ReadOnlySpan<char> GetOrCreateParameterString(ref char[]? storage, ReadOnlySpan<byte> utf8Param) {
		lock (_parameterStringMutationLock) {
			if (storage == null) {
				storage = new char[SpanUtils.GetUtf16Length(utf8Param)];
				var result = SpanUtils.ConvertUtf8ToUtf16(storage, utf8Param);
				if (result.Length != storage.Length) storage = result.ToArray();
			}
			return storage;
		}
	}

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

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in MaterialCreationConfig src) {
		return SerializationSizeOfString(src.Name) // Name
			 + SerializationSizeOfBool(); // EnablePerInstanceEffects
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MaterialCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteBool(ref dest, src.EnablePerInstanceEffects);
	}
	/// <inheritdoc />
	public static MaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MaterialCreationConfig {
			Name = SerializationReadString(ref src),
			EnablePerInstanceEffects = SerializationReadBool(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}