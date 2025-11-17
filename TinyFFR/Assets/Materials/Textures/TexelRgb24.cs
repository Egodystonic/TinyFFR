// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR.Assets.Materials;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgb24(byte R, byte G, byte B) : IThreeByteChannelTexel<TexelRgb24>, IConversionSupplyingTexel<TexelRgb24, ColorVect>, IConversionSupplyingTexel<TexelRgb24, TexelRgb24>, IConversionSupplyingTexel<TexelRgb24, TexelRgba32> {
	public const int TexelSizeBytes = 3;

	public byte this[int index] => index switch {
		0 => R,
		1 => G,
		2 => B,
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range 0 - 2.")
	};
	public byte this[ColorChannel channel] => channel switch {
		ColorChannel.R => R,
		ColorChannel.G => G,
		ColorChannel.B => B,
		_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Only R, G, B channels included in this texel type.")
	};

	public TexelRgb24(TexelRgba32 rgba) : this(rgba.R, rgba.G, rgba.B) { }
	public TexelRgb24(ColorVect color) : this(0, 0, 0) {
		color.ToRgb24(out var r, out var g, out var b);
		R = r;
		G = g;
		B = b;
	}

	public static void SerializeToBytes(Span<byte> dest, TexelRgb24 src) {
		dest[0] = src.R;
		dest[1] = src.G;
		dest[2] = src.B;
	}
	public static TexelRgb24 DeserializeFromBytes(ReadOnlySpan<byte> src) => new(src[0], src[1], src[2]);

	public override string ToString() {
		return $"{nameof(TexelRgb24)} " +
			   $"{R}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{G}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{B}";
	}

	public TexelRgba32 ToRgba32() => new(R, G, B, Byte.MaxValue);
	public ColorVect ToColorVect() => ColorVect.FromRgb24(R, G, B);

	public static explicit operator TexelRgb24(ColorVect color) => new(color);
	public static explicit operator ColorVect(TexelRgb24 texel) => texel.ToColorVect();
	public static explicit operator TexelRgb24(TexelRgba32 texel) => texel.ToRgb24();

	public static TexelRgb24 ConvertFrom(ColorVect v) => new(v);
	static TexelRgb24 IConversionSupplyingTexel<TexelRgb24, TexelRgb24>.ConvertFrom(TexelRgb24 t) => t;
	public static TexelRgb24 ConvertFrom(TexelRgba32 t) => t.ToRgb24();
	public static TexelRgb24 ConvertFrom<T>(T v) where T : unmanaged, IThreeByteChannelTexel<T> => new(v[0], v[1], v[2]);

	public TexelRgb24 WithInvertedChannelIfPresent(int channelIndex) {
		return channelIndex switch {
			0 => this with { R = (byte) (Byte.MaxValue - R) },
			1 => this with { G = (byte) (Byte.MaxValue - G) },
			2 => this with { B = (byte) (Byte.MaxValue - B) },
			_ => this
		};
	}

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
}