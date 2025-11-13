// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgba32(byte R, byte G, byte B, byte A) : IFourByteChannelTexel<TexelRgba32>, IConversionSupplyingTexel<TexelRgba32, ColorVect> {
	public const int TexelSizeBytes = 4;

	public TexelRgb24 AsRgb24 => new(R, G, B);
	public ColorVect AsColorVect => ColorVect.FromRgba32(R, G, B, A);

	public byte this[int index] => index switch {
		0 => R,
		1 => G,
		2 => B,
		3 => A,
		_ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in range 0 - 3.")
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

	public static explicit operator TexelRgba32(ColorVect color) => new(color);
	public static explicit operator ColorVect(TexelRgba32 texel) => texel.AsColorVect;
	public static explicit operator TexelRgba32(TexelRgb24 texel) => texel.AsRgba32;
	
	public static TexelRgba32 ConvertFrom(ColorVect v) => new(v);
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
}