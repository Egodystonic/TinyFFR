// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// A single element of a texture's data, holding three one-byte channels and no alpha.
/// </summary>
/// <remarks>
/// This is the compact texel type, used wherever a texture has no transparency to record — most normal maps, ORM maps and
/// opaque colour maps. Its three channels do not necessarily mean colour: in an ORM map they hold three unrelated surface
/// properties that merely happen to be stored where red, green and blue would be.
/// </remarks>
/// <param name="R">The first channel's value.</param>
/// <param name="G">The second channel's value.</param>
/// <param name="B">The third channel's value.</param>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgb24(byte R, byte G, byte B) : IThreeByteChannelTexel<TexelRgb24>, IConversionSupplyingTexel<TexelRgb24, ColorVect>, IConversionSupplyingTexel<TexelRgb24, TexelRgb24>, IConversionSupplyingTexel<TexelRgb24, TexelRgba32> {
	/// <summary>
	/// How many bytes one of these occupies: <c>3</c>.
	/// </summary>
	public const int TexelSizeBytes = 3;

	/// <inheritdoc />
	public byte this[int index] => index switch {
		0 => R,
		1 => G,
		2 => B,
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range 0 - 2.")
	};
	/// <inheritdoc />
	public byte this[ColorChannel channel] => channel switch {
		ColorChannel.R => R,
		ColorChannel.G => G,
		ColorChannel.B => B,
		_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Only R, G, B channels included in this texel type.")
	};

	/// <summary>
	/// Constructs a new <see cref="TexelRgb24"/> from the first three channels of the given texel, discarding its alpha.
	/// </summary>
	/// <param name="rgba">The texel to take the three channel values from.</param>
	public TexelRgb24(TexelRgba32 rgba) : this(rgba.R, rgba.G, rgba.B) { }
	/// <summary>
	/// Constructs a new <see cref="TexelRgb24"/> from the given colour, discarding its alpha.
	/// </summary>
	/// <param name="color">The colour to convert.</param>
	public TexelRgb24(ColorVect color) : this(0, 0, 0) {
		color.ToRgb24(out var r, out var g, out var b);
		R = r;
		G = g;
		B = b;
	}

	// This is provided so we can use it in TexturePatterns
	/// <summary>
	/// Constructs a new <see cref="TexelRgb24"/> from three channel values given as bytes.
	/// </summary>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	public static TexelRgb24 FromByteComponents(byte r, byte g, byte b) => new(r, g, b);
	/// <summary>
	/// Constructs a new <see cref="TexelRgb24"/> from three channel values given in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// Values outside that range wrap rather than clamping, so pre-clamp anything that might exceed it.
	/// </remarks>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	public static TexelRgb24 FromNormalizedFloats(float r, float g, float b) {
		var vec3 = new Vector3(r, g, b) * Byte.MaxValue;
		return FromByteComponents(
			(byte) vec3.X,
			(byte) vec3.Y,
			(byte) vec3.Z
		);
	}
	/// <summary>
	/// Constructs a new <see cref="TexelRgb24"/> from three channel values given in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// Values outside that range wrap rather than clamping, so pre-clamp anything that might exceed it.
	/// </remarks>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	public static TexelRgb24 FromNormalizedFloats(Real r, Real g, Real b) => FromNormalizedFloats((float) r, (float) g, (float) b);
	static TexelRgb24 IThreeChannelTexel<TexelRgb24, byte>.ConstructFrom(byte r, byte g, byte b) => FromByteComponents(r, g, b);

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, TexelRgb24 src) {
		dest[0] = src.R;
		dest[1] = src.G;
		dest[2] = src.B;
	}
	/// <inheritdoc />
	public static TexelRgb24 DeserializeFromBytes(ReadOnlySpan<byte> src) => new(src[0], src[1], src[2]);

	TexelRgb24 ITexel<TexelRgb24>.WithPremultipliedAlpha() => this;

	/// <inheritdoc />
	public override string ToString() {
		return $"{nameof(TexelRgb24)} " +
			   $"{R}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{G}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{B}";
	}

	/// <summary>
	/// Converts this texel to a four-channel one, with its alpha channel fully opaque.
	/// </summary>
	public TexelRgba32 ToRgba32() => ToRgba32(Byte.MaxValue);
	/// <summary>
	/// Converts this texel to a four-channel one, with the given alpha value.
	/// </summary>
	/// <param name="alphaValue">The value the resulting texel's alpha channel takes.</param>
	public TexelRgba32 ToRgba32(byte alphaValue) => new(R, G, B, alphaValue);
	/// <summary>
	/// Converts this texel's three channels to a colour, which is fully opaque.
	/// </summary>
	public ColorVect ToColorVect() => ColorVect.FromRgb24(R, G, B);
	/// <summary>
	/// Returns this texel's three channels as values in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// This is the form to read when the channels hold surface data rather than colour, as it gives the values back on the scale
	/// they were written on.
	/// </remarks>
	public Vector3 ToNormalizedFloats() {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new Vector3(R, G, B) * Multiplicand;
	}

	/// <summary>
	/// Converts the given colour to a <see cref="TexelRgb24"/>, discarding its alpha.
	/// </summary>
	/// <param name="color">The colour to convert.</param>
	public static explicit operator TexelRgb24(ColorVect color) => new(color);
	/// <summary>
	/// Converts the given texel's three channels to a colour, which is fully opaque.
	/// </summary>
	/// <param name="texel">The texel to convert.</param>
	public static explicit operator ColorVect(TexelRgb24 texel) => texel.ToColorVect();
	/// <summary>
	/// Converts the given four-channel texel to a three-channel one, discarding its alpha.
	/// </summary>
	/// <param name="texel">The texel to convert.</param>
	public static explicit operator TexelRgb24(TexelRgba32 texel) => texel.ToRgb24();

	/// <summary>
	/// Converts the given colour to a <see cref="TexelRgb24"/>, discarding its alpha.
	/// </summary>
	/// <param name="v">The colour to convert.</param>
	public static TexelRgb24 ConvertFrom(ColorVect v) => new(v);
	static TexelRgb24 IConversionSupplyingTexel<TexelRgb24, TexelRgb24>.ConvertFrom(TexelRgb24 t) => t;
	ColorVect IConversionSupplyingTexel<TexelRgb24, ColorVect>.Convert() => ToColorVect();
	TexelRgb24 IConversionSupplyingTexel<TexelRgb24, TexelRgb24>.Convert() => this;
	TexelRgba32 IConversionSupplyingTexel<TexelRgb24, TexelRgba32>.Convert() => ToRgba32();
	/// <summary>
	/// Converts the given four-channel texel to a three-channel one, discarding its alpha.
	/// </summary>
	/// <param name="t">The texel to convert.</param>
	public static TexelRgb24 ConvertFrom(TexelRgba32 t) => t.ToRgb24();
	/// <summary>
	/// Converts any three-byte-channel texel to a <see cref="TexelRgb24"/> by taking its three channels in order.
	/// </summary>
	/// <typeparam name="T">The texel type to convert from.</typeparam>
	/// <param name="v">The texel to convert.</param>
	public static TexelRgb24 ConvertFrom<T>(T v) where T : unmanaged, IThreeByteChannelTexel<T> => new(v[0], v[1], v[2]);

	/// <inheritdoc />
	public static bool TryCoerceSpan<TOther>(ReadOnlySpan<TexelRgb24> src, Span<TOther> dest) where TOther : unmanaged, ITexel<TOther> {
		switch (TOther.BlitType) {
			case TexelType.Rgb24:
				src.CopyTo(MemoryMarshal.Cast<TOther, TexelRgb24>(dest));
				return true;
			case TexelType.Rgba32:
				var castDest = MemoryMarshal.Cast<TOther, TexelRgba32>(dest);
				for (var i = 0; i < src.Length; ++i) castDest[i] = src[i].ToRgba32();
				return true;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public static bool TryCoerceSpanFrom<TOther>(ReadOnlySpan<TOther> src, Span<TexelRgb24> dest, bool mergeWithExistingDestinationData = false) where TOther : unmanaged, ITexel<TOther> {
		switch (TOther.BlitType) {
			case TexelType.Rgb24:
				MemoryMarshal.Cast<TOther, TexelRgb24>(src).CopyTo(dest);
				return true;
			case TexelType.Rgba32:
				var castSrc = MemoryMarshal.Cast<TOther, TexelRgba32>(src);
				for (var i = 0; i < castSrc.Length; ++i) dest[i] = castSrc[i].ToRgb24();
				return true;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public TexelRgb24 WithInvertedChannelIfPresent(int channelIndex) {
		return channelIndex switch {
			0 => this with { R = (byte) (Byte.MaxValue - R) },
			1 => this with { G = (byte) (Byte.MaxValue - G) },
			2 => this with { B = (byte) (Byte.MaxValue - B) },
			_ => this
		};
	}

	/// <inheritdoc />
	public TexelRgb24 SwizzlePresentChannels(ColorChannel redSource, ColorChannel greenSource, ColorChannel blueSource, ColorChannel alphaSource) {
		static byte? GetColorChannel(TexelRgb24 @this, ColorChannel channel) {
			return channel switch {
				ColorChannel.R => @this[0],
				ColorChannel.G => @this[1],
				ColorChannel.B => @this[2],
				_ => null
			};
		}

		return new(
			GetColorChannel(this, redSource) ?? R,
			GetColorChannel(this, greenSource) ?? G,
			GetColorChannel(this, blueSource) ?? B
		);
	}

	/// <inheritdoc />
	public static TexelRgb24 Blend(TexelRgb24 start, TexelRgb24 end, float distance) {
		return new TexelRgb24(
			(byte) Real.Interpolate(start.R, end.R, distance),
			(byte) Real.Interpolate(start.G, end.G, distance),
			(byte) Real.Interpolate(start.B, end.B, distance)
		);
	}
}
