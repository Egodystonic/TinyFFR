// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = TexelSizeBytes)]
public readonly record struct TexelRgba32(byte R, byte G, byte B, byte A) : ITexel<TexelRgba32> {
	const int TexelSizeBytes = 4;
	public static int SerializationByteSpanLength { get; } = TexelSizeBytes;
	public static TexelType Type { get; } = TexelType.Rgba32;

	public TexelRgb24 AsRgb24 => new(R, G, B);
	public ColorVect AsColorVect => ColorVect.FromRgba32(R, G, B, A);

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
}