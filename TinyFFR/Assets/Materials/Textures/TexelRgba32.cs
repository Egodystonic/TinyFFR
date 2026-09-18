// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// A single element of a texture's data, holding four one-byte channels.
/// </summary>
/// <remarks>
/// This is the texel type for textures that need a fourth channel (a colour map with transparency, or a map such as ORMR that
/// simply has four values to store). Its channels do not necessarily mean colour and alpha: in an ORMR map the fourth channel
/// holds reflectance, which merely happens to be stored where alpha would be.
/// </remarks>
/// <param name="R">The first channel's value.</param>
/// <param name="G">The second channel's value.</param>
/// <param name="B">The third channel's value.</param>
/// <param name="A">The fourth channel's value.</param>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgba32(byte R, byte G, byte B, byte A) : IFourByteChannelTexel<TexelRgba32>, IConversionSupplyingTexel<TexelRgba32, ColorVect>, IConversionSupplyingTexel<TexelRgba32, TexelRgb24>, IConversionSupplyingTexel<TexelRgba32, TexelRgba32> {
	/// <summary>
	/// How many bytes one of these occupies: <c>4</c>.
	/// </summary>
	public const int TexelSizeBytes = 4;

	/// <inheritdoc />
	public byte this[int index] => index switch {
		0 => R,
		1 => G,
		2 => B,
		3 => A,
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range 0 - 3.")
	};
	/// <inheritdoc />
	public byte this[ColorChannel channel] => channel switch {
		ColorChannel.R => R,
		ColorChannel.G => G,
		ColorChannel.B => B,
		ColorChannel.A => A,
		_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Only R, G, B, A channels included in this texel type.")
	};

	/// <summary>
	/// Constructs a new <see cref="TexelRgba32"/> from a three-channel texel plus a fourth channel value.
	/// </summary>
	/// <param name="rgb">The texel to take the first three channel values from.</param>
	/// <param name="a">The fourth channel's value.</param>
	public TexelRgba32(TexelRgb24 rgb, byte a) : this(rgb.R, rgb.G, rgb.B, a) { }
	/// <summary>
	/// Constructs a new <see cref="TexelRgba32"/> from the given colour, including its alpha.
	/// </summary>
	/// <param name="color">The colour to convert.</param>
	public TexelRgba32(ColorVect color) : this(0, 0, 0, 0) {
		color.ToRgba32(out var r, out var g, out var b, out var a);
		R = r;
		G = g;
		B = b;
		A = a;
	}

	// This is provided so we can use it in TexturePatterns
	/// <summary>
	/// Constructs a new <see cref="TexelRgba32"/> from four channel values given as bytes.
	/// </summary>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	/// <param name="a">The fourth channel's value.</param>
	public static TexelRgba32 FromByteComponents(byte r, byte g, byte b, byte a) => new(r, g, b, a);
	/// <summary>
	/// Constructs a new <see cref="TexelRgba32"/> from four channel values given in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// Values outside that range wrap rather than clamping, so pre-clamp anything that might exceed it.
	/// </remarks>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	/// <param name="a">The fourth channel's value.</param>
	public static TexelRgba32 FromNormalizedFloats(float r, float g, float b, float a) {
		var vec4 = new Vector4(r, g, b, a) * Byte.MaxValue;
		return FromByteComponents(
			(byte) vec4.X,
			(byte) vec4.Y,
			(byte) vec4.Z,
			(byte) vec4.W
		);
	}
	/// <summary>
	/// Constructs a new <see cref="TexelRgba32"/> from four channel values given in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// Values outside that range wrap rather than clamping, so pre-clamp anything that might exceed it.
	/// </remarks>
	/// <param name="r">The first channel's value.</param>
	/// <param name="g">The second channel's value.</param>
	/// <param name="b">The third channel's value.</param>
	/// <param name="a">The fourth channel's value.</param>
	public static TexelRgba32 FromNormalizedFloats(Real r, Real g, Real b, Real a) => FromNormalizedFloats((float) r, (float) g, (float) b, (float) a);
	static TexelRgba32 IFourChannelTexel<TexelRgba32, byte>.ConstructFrom(byte r, byte g, byte b, byte a) => FromByteComponents(r, g, b, a);

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, TexelRgba32 src) {
		dest[0] = src.R;
		dest[1] = src.G;
		dest[2] = src.B;
		dest[3] = src.A;
	}
	/// <inheritdoc />
	public static TexelRgba32 DeserializeFromBytes(ReadOnlySpan<byte> src) => new(src[0], src[1], src[2], src[3]);

	/// <inheritdoc />
	public TexelRgba32 WithPremultipliedAlpha() => new(ToColorVect().WithPremultipliedAlpha());

	/// <inheritdoc />
	public override string ToString() {
		return $"{nameof(TexelRgba32)} " +
			   $"{R}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{G}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{B}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{A}";
	}

	/// <summary>
	/// Converts this texel to a three-channel one, discarding its fourth channel.
	/// </summary>
	public TexelRgb24 ToRgb24() => new(R, G, B);
	/// <summary>
	/// Converts this texel's four channels to a colour, including its alpha.
	/// </summary>
	public ColorVect ToColorVect() => ColorVect.FromRgba32(R, G, B, A);
	/// <summary>
	/// Returns this texel's four channels as values in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// This is the form to read when the channels hold surface data rather than colour, as it gives the values back on the scale
	/// they were written on.
	/// </remarks>
	public Vector4 ToNormalizedFloats() {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new Vector4(R, G, B, A) * Multiplicand;
	}

	/// <summary>
	/// Converts the given colour to a <see cref="TexelRgba32"/>, including its alpha.
	/// </summary>
	/// <param name="color">The colour to convert.</param>
	public static explicit operator TexelRgba32(ColorVect color) => new(color);
	/// <summary>
	/// Converts the given texel's four channels to a colour, including its alpha.
	/// </summary>
	/// <param name="texel">The texel to convert.</param>
	public static explicit operator ColorVect(TexelRgba32 texel) => texel.ToColorVect();
	/// <summary>
	/// Converts the given three-channel texel to a four-channel one, with its alpha channel fully opaque.
	/// </summary>
	/// <param name="texel">The texel to convert.</param>
	public static explicit operator TexelRgba32(TexelRgb24 texel) => texel.ToRgba32();

	/// <summary>
	/// Converts the given colour to a <see cref="TexelRgba32"/>, including its alpha.
	/// </summary>
	/// <param name="v">The colour to convert.</param>
	public static TexelRgba32 ConvertFrom(ColorVect v) => new(v);
	static TexelRgba32 IConversionSupplyingTexel<TexelRgba32, TexelRgba32>.ConvertFrom(TexelRgba32 t) => t;
	ColorVect IConversionSupplyingTexel<TexelRgba32, ColorVect>.Convert() => ToColorVect();
	TexelRgb24 IConversionSupplyingTexel<TexelRgba32, TexelRgb24>.Convert() => ToRgb24();
	TexelRgba32 IConversionSupplyingTexel<TexelRgba32, TexelRgba32>.Convert() => this;
	/// <summary>
	/// Converts the given three-channel texel to a four-channel one, with its alpha channel fully opaque.
	/// </summary>
	/// <param name="t">The texel to convert.</param>
	public static TexelRgba32 ConvertFrom(TexelRgb24 t) => t.ToRgba32();
	/// <summary>
	/// Converts any four-byte-channel texel to a <see cref="TexelRgba32"/> by taking its four channels in order.
	/// </summary>
	/// <typeparam name="T">The texel type to convert from.</typeparam>
	/// <param name="v">The texel to convert.</param>
	public static TexelRgba32 ConvertFrom<T>(T v) where T : unmanaged, IFourByteChannelTexel<T> => new(v[0], v[1], v[2], v[3]);

	/// <inheritdoc />
	public static bool TryCoerceSpan<TOther>(ReadOnlySpan<TexelRgba32> src, Span<TOther> dest) where TOther : unmanaged, ITexel<TOther> {
		switch (TOther.BlitType) {
			case TexelType.Rgba32:
				src.CopyTo(MemoryMarshal.Cast<TOther, TexelRgba32>(dest));
				return true;
			case TexelType.Rgb24:
				var castDest = MemoryMarshal.Cast<TOther, TexelRgb24>(dest);
				for (var i = 0; i < src.Length; ++i) castDest[i] = src[i].ToRgb24();
				return true;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public static bool TryCoerceSpanFrom<TOther>(ReadOnlySpan<TOther> src, Span<TexelRgba32> dest, bool mergeWithExistingDestinationData = false) where TOther : unmanaged, ITexel<TOther> {
		switch (TOther.BlitType) {
			case TexelType.Rgba32:
				MemoryMarshal.Cast<TOther, TexelRgba32>(src).CopyTo(dest);
				return true;
			case TexelType.Rgb24:
				var castSrc = MemoryMarshal.Cast<TOther, TexelRgb24>(src);
				if (mergeWithExistingDestinationData) {
					for (var i = 0; i < castSrc.Length; ++i) dest[i] = castSrc[i].ToRgba32(dest[i].A);
				}
				else {
					for (var i = 0; i < castSrc.Length; ++i) dest[i] = castSrc[i].ToRgba32();
				}
				return true;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public TexelRgba32 WithInvertedChannelIfPresent(int channelIndex) {
		return channelIndex switch {
			0 => this with { R = (byte) (Byte.MaxValue - R) },
			1 => this with { G = (byte) (Byte.MaxValue - G) },
			2 => this with { B = (byte) (Byte.MaxValue - B) },
			3 => this with { A = (byte) (Byte.MaxValue - A) },
			_ => this
		};
	}

	/// <inheritdoc />
	public TexelRgba32 SwizzlePresentChannels(ColorChannel redSource, ColorChannel greenSource, ColorChannel blueSource, ColorChannel alphaSource) {
		static byte? GetColorChannel(TexelRgba32 @this, ColorChannel channel) {
			return channel switch {
				ColorChannel.R => @this[0],
				ColorChannel.G => @this[1],
				ColorChannel.B => @this[2],
				ColorChannel.A => @this[3],
				_ => null
			};
		}

		return new(
			GetColorChannel(this, redSource) ?? R,
			GetColorChannel(this, greenSource) ?? G,
			GetColorChannel(this, blueSource) ?? B,
			GetColorChannel(this, alphaSource) ?? A
		);
	}

	/// <inheritdoc />
	public static TexelRgba32 Blend(TexelRgba32 start, TexelRgba32 end, float distance) {
		return new TexelRgba32(
			(byte) Real.Interpolate(start.R, end.R, distance),
			(byte) Real.Interpolate(start.G, end.G, distance),
			(byte) Real.Interpolate(start.B, end.B, distance),
			(byte) Real.Interpolate(start.A, end.A, distance)
		);
	}
}
