// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering.Local;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IConfigStruct{TSelf}"/> passed to the <see cref="IRendererBuilder"/> when creating a <see cref="RenderOutputBuffer"/>.
/// </summary>
public readonly ref struct RenderOutputBufferCreationConfig : IConfigStruct<RenderOutputBufferCreationConfig> {
	/// <summary>
	/// The default value of <see cref="TextureDimensions"/>: <c>(2560, 1440)</c>.
	/// </summary>
	public static readonly XYPair<int> DefaultTextureDimensions = (2560, 1440);
	/// <summary>
	/// The maximum permitted value for either component of <see cref="TextureDimensions"/>: <c>32,768</c>.
	/// </summary>
	public const int MaxTextureDimensionXY = 32_768;
	/// <summary>
	/// The minimum permitted value for either component of <see cref="TextureDimensions"/>: <c>1</c>.
	/// </summary>
	public const int MinTextureDimensionXY = 1;

	/// <summary>
	/// The pixel dimensions of the new <see cref="RenderOutputBuffer"/>. Defaults to <see cref="DefaultTextureDimensions"/>.
	/// </summary>
	/// <remarks>
	/// Both <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> must be between <see cref="MinTextureDimensionXY"/> and <see cref="MaxTextureDimensionXY"/> (inclusive).
	/// </remarks>
	public XYPair<int> TextureDimensions { get; init; } = DefaultTextureDimensions;

	/// <summary>
	/// The name given to the new <see cref="RenderOutputBuffer"/>.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Creates a new config object with all default values set.
	/// </summary>
	public RenderOutputBufferCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(RenderOutputBufferCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (TextureDimensions.Clamp(new(MinTextureDimensionXY), new(MaxTextureDimensionXY)) != TextureDimensions) {
			ThrowArgException(TextureDimensions, $"must have both X and Y values between {MinTextureDimensionXY} and {MaxTextureDimensionXY}.");
		}
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in RenderOutputBufferCreationConfig src) {
		return	SerializationSizeOf<XYPair<int>>() // TextureDimensions
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderOutputBufferCreationConfig src) {
		SerializationWrite(ref dest, src.TextureDimensions);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc/>
	public static RenderOutputBufferCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			TextureDimensions = SerializationRead<XYPair<int>>(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}