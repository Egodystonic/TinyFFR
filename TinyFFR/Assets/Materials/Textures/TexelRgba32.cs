// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgba32(byte R, byte G, byte B, byte A) : IFourByteChannelTexel<TexelRgba32>, IConversionSupplyingTexel<TexelRgba32, ColorVect>, IConversionSupplyingTexel<TexelRgba32, TexelRgb24>, IConversionSupplyingTexel<TexelRgba32, TexelRgba32> {
	public const int TexelSizeBytes = 4;

	public byte this[int index] => index switch {
		0 => R,
		1 => G,
		2 => B,
		3 => A,
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range 0 - 3.")
	};
	public byte this[ColorChannel channel] => channel switch {
		ColorChannel.R => R,
		ColorChannel.G => G,
		ColorChannel.B => B,
		ColorChannel.A => A,
		_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Only R, G, B, A channels included in this texel type.")
	};

	public TexelRgba32(TexelRgb24 rgb, byte a) : this(rgb.R, rgb.G, rgb.B, a) { }
	public TexelRgba32(ColorVect color) : this(0, 0, 0, 0) {
		color.ToRgba32(out var r, out var g, out var b, out var a);
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public static void SerializeToBytes(Span<byte> dest, TexelRgba32 src) {
		dest[0] = src.R;
		dest[1] = src.G;
		dest[2] = src.B;
		dest[3] = src.A;
	}
	public static TexelRgba32 DeserializeFromBytes(ReadOnlySpan<byte> src) => new(src[0], src[1], src[2], src[3]);

	public override string ToString() {
		return $"{nameof(TexelRgba32)} " +
			   $"{R}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{G}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{B}{NumberFormatInfo.CurrentInfo.NumberGroupSeparator} " +
			   $"{A}";
	}

	public TexelRgb24 ToRgb24() => new(R, G, B);
	public ColorVect ToColorVect() => ColorVect.FromRgba32(R, G, B, A);

	public static explicit operator TexelRgba32(ColorVect color) => new(color);
	public static explicit operator ColorVect(TexelRgba32 texel) => texel.ToColorVect();
	public static explicit operator TexelRgba32(TexelRgb24 texel) => texel.ToRgba32();
	
	public static TexelRgba32 ConvertFrom(ColorVect v) => new(v);
	static TexelRgba32 IConversionSupplyingTexel<TexelRgba32, TexelRgba32>.ConvertFrom(TexelRgba32 t) => t;
	public static TexelRgba32 ConvertFrom(TexelRgb24 t) => t.ToRgba32();
	public static TexelRgba32 ConvertFrom<T>(T v) where T : unmanaged, IFourByteChannelTexel<T> => new(v[0], v[1], v[2], v[3]);

	public TexelRgba32 WithInvertedChannelIfPresent(int channelIndex) {
		return channelIndex switch {
			0 => this with { R = (byte) (Byte.MaxValue - R) },
			1 => this with { G = (byte) (Byte.MaxValue - G) },
			2 => this with { B = (byte) (Byte.MaxValue - B) },
			3 => this with { A = (byte) (Byte.MaxValue - A) },
			_ => this
		};
	}

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
}