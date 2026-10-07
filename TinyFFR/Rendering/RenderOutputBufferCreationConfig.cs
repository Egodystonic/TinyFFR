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
	/// If <see langword="true"/>, the new buffer stores its rows top-to-bottom, so that frames read with <c>presentFrameTopToBottom: true</c>
	/// (see <see cref="RenderOutputBuffer.ReadNextFrame(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},bool)"/>) can be delivered without first copying/inverting every row.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Set this when you'll mostly read this buffer's frames top-to-bottom, e.g. to copy them in to a UI framework's bitmap. With the default (<see langword="false"/>),
	/// frames are instead delivered most cheaply bottom-to-top, matching TinyFFR's texture convention (and therefore <see cref="ImageUtils.SaveBitmap{TTexel}(ReadOnlySpan{char},XYPair{int},ReadOnlySpan{TTexel})"/>).
	/// Frames can still be read in either order regardless of this setting; this only changes which order is cheaper.
	/// </para>
	/// <para>
	/// On the OpenGL rendering backend, frames read top-to-bottom always require the rows to be inverted when this is <see langword="true"/>
	/// (and frames read bottom-to-top always require it when this is <see langword="false"/>). On every other backend, the matching order is delivered without an inversion.
	/// </para>
	/// <para>
	/// Because the rows are stored top-to-bottom, a texture created with <see cref="RenderOutputBuffer.CreateDynamicTexture"/> from a buffer with this setting
	/// appears vertically flipped when used in a material or on a canvas (though not in an ImGui image).
	/// </para>
	/// </remarks>
	public bool OptimizeForTopToBottomReadback { get; init; } = false;

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
			+	SerializationSizeOfBool() // OptimizeForTopToBottomReadback
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderOutputBufferCreationConfig src) {
		SerializationWrite(ref dest, src.TextureDimensions);
		SerializationWriteBool(ref dest, src.OptimizeForTopToBottomReadback);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc/>
	public static RenderOutputBufferCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			TextureDimensions = SerializationRead<XYPair<int>>(ref src),
			OptimizeForTopToBottomReadback = SerializationReadBool(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}