// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR;

// Maintainer's note: I mostly named this "ColorVect" rather than "Color" simply to differentiate it from all the other "Color" structs in various common libraries.
// But it does also make it clearer immediately that this is stored in 4-float format.
/// <summary>
/// A four-channel vector of floating point values representing a colour.
/// </summary>
/// <remarks>
/// <para>
/// The vector is laid out in RGBA format (i.e. <see cref="Red"/> = <c>X</c>, <see cref="Green"/> = <c>Y</c>, <see cref="Blue"/> = <c>Z</c>, <see cref="Alpha"/> = <c>W</c>).
/// </para>
/// <para>
/// Each component is expected in a normalized range (e.g. between 0 and 1) where 0 indicates a complete absense of intensity in that channel and 1 indicates a complete saturation.
/// </para>
/// <para>
/// Some examples:
/// <ul>
/// <li>Opaque white: <c>new ColorVect(1f, 1f, 1f, 1f)</c></li>
/// <li>Opaque red: <c>new ColorVect(1f, 0f, 0f, 1f)</c></li>
/// <li>Opaque green: <c>new ColorVect(0f, 1f, 0f, 1f)</c></li>
/// <li>Opaque blue: <c>new ColorVect(0f, 0f, 1f, 1f)</c></li>
/// <li>Semi-translucent pink: <c>new ColorVect(1f, 0f, 1f, 0.5f)</c></li>
/// </ul>
/// </para>
/// <para>
/// Also note that there is an implicit conversion from <see cref="StandardColor"/> to ColorVect.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)]
public readonly partial struct ColorVect : IVect<ColorVect> {
	/// <summary>
	/// When constructing a ColorVect via HSL representation (e.g. by invoking <see cref="FromHueSaturationLightness(Angle, float, float)"/>)
	/// this value represents the "pure red" (R=1, G=0, B=0) angle on the colour wheel.
	/// </summary>
	public static readonly Angle RedHueAngle = 0f;
	/// <summary>
	/// When constructing a ColorVect via HSL representation (e.g. by invoking <see cref="FromHueSaturationLightness(Angle, float, float)"/>)
	/// this value represents the "pure green" (R=0, G=1, B=0) angle on the colour wheel.
	/// </summary>
	public static readonly Angle GreenHueAngle = 120f;
	/// <summary>
	/// When constructing a ColorVect via HSL representation (e.g. by invoking <see cref="FromHueSaturationLightness(Angle, float, float)"/>)
	/// this value represents the "pure blue" (R=0, G=0, B=1) angle on the colour wheel.
	/// </summary>
	public static readonly Angle BlueHueAngle = 240f;
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque white (<c>(R=1, G=1, B=1, A=1)</c>).
	/// </summary>
	public static readonly ColorVect WhiteOpaque = new(1f, 1f, 1f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque black (<c>(R=0, G=0, B=0, A=1)</c>).
	/// </summary>
	public static readonly ColorVect BlackOpaque = new(0f, 0f, 0f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque red (<c>(R=1, G=0, B=0, A=1)</c>).
	/// </summary>
	public static readonly ColorVect RedOpaque = new(1f, 0f, 0f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque green (<c>(R=0, G=1, B=0, A=1)</c>).
	/// </summary>
	public static readonly ColorVect GreenOpaque = new(0f, 1f, 0f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque blue (<c>(R=0, G=0, B=1, A=1)</c>).
	/// </summary>
	public static readonly ColorVect BlueOpaque = new(0f, 0f, 1f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque yellow (<c>(R=1, G=1, B=0, A=1)</c>).
	/// </summary>
	public static readonly ColorVect YellowOpaque = new(1f, 1f, 0f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque cyan (<c>(R=0, G=1, B=1, A=1)</c>).
	/// </summary>
	public static readonly ColorVect CyanOpaque = new(0f, 1f, 1f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-opaque pink (<c>(R=1, G=0, B=1, A=1)</c>).
	/// </summary>
	public static readonly ColorVect PinkOpaque = new(1f, 0f, 1f, 1f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent white (<c>(R=1, G=1, B=1, A=0)</c>).
	/// </summary>
	public static readonly ColorVect WhiteTransparent = new(1f, 1f, 1f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent black (<c>(R=0, G=0, B=0, A=0)</c>).
	/// </summary>
	public static readonly ColorVect BlackTransparent = new(0f, 0f, 0f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent red (<c>(R=1, G=0, B=0, A=0)</c>).
	/// </summary>
	public static readonly ColorVect RedTransparent = new(1f, 0f, 0f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent green (<c>(R=0, G=1, B=0, A=0)</c>).
	/// </summary>
	public static readonly ColorVect GreenTransparent = new(0f, 1f, 0f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent blue (<c>(R=0, G=0, B=1, A=0)</c>).
	/// </summary>
	public static readonly ColorVect BlueTransparent = new(0f, 0f, 1f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent yellow (<c>(R=1, G=1, B=0, A=0)</c>).
	/// </summary>
	public static readonly ColorVect YellowTransparent = new(1f, 1f, 0f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent cyan (<c>(R=0, G=1, B=1, A=0)</c>).
	/// </summary>
	public static readonly ColorVect CyanTransparent = new(0f, 1f, 1f, 0f);
	/// <summary>
	/// Returns a ColorVect representing a fully-transparent pink (<c>(R=1, G=0, B=1, A=0)</c>).
	/// </summary>
	public static readonly ColorVect PinkTransparent = new(1f, 0f, 1f, 0f);

	internal readonly Vector4 AsVector4;

	/// <summary>
	/// The first component of this colour, typically a value between 0 and 1 indicating the intensity of the red channel.
	/// </summary>
	public float Red {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.X = value;
	}

	/// <summary>
	/// The second component of this colour, typically a value between 0 and 1 indicating the intensity of the green channel.
	/// </summary>
	public float Green {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Y = value;
	}

	/// <summary>
	/// The third component of this colour, typically a value between 0 and 1 indicating the intensity of the blue channel.
	/// </summary>
	public float Blue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Z = value;
	}

	/// <summary>
	/// The fourth component of this colour, typically a value between 0 and 1 indicating opacity (0 being fully transparent, 1 being fully opaque).
	/// </summary>
	public float Alpha {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.W;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.W = value;
	}

	/// <summary>
	/// Returns the hue angle after converting this colour vect from RGB to HSL representation.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="Hue"/>, <see cref="Saturation"/>, or <see cref="Lightness"/> requires a full RGB-&gt;HSL conversion.
	/// If you intend to extract all three properties, consider using <see cref="ToHueSaturationLightness"/> instead. 
	/// </remarks>
	public Angle Hue {
		get {
			ToHueSaturationLightness(out var result, out _, out _);
			return result;
		}
	}

	/// <summary>
	/// Returns the normalized saturation (0 to 1) after converting this colour vect from RGB to HSL representation.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="Hue"/>, <see cref="Saturation"/>, or <see cref="Lightness"/> requires a full RGB-&gt;HSL conversion.
	/// If you intend to extract all three properties, consider using <see cref="ToHueSaturationLightness"/> instead. 
	/// </remarks>
	public float Saturation {
		get {
			ToHueSaturationLightness(out _, out var result, out _);
			return result;
		}
	}

	/// <summary>
	/// Returns the normalized lightness (0 to 1) after converting this colour vect from RGB to HSL representation.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="Hue"/>, <see cref="Saturation"/>, or <see cref="Lightness"/> requires a full RGB-&gt;HSL conversion.
	/// If you intend to extract all three properties, consider using <see cref="ToHueSaturationLightness"/> instead. 
	/// </remarks>
	public float Lightness {
		get {
			ToHueSaturationLightness(out _, out _, out var result);
			return result;
		}
	}

	/// <summary>
	/// Gets the float component specified by the given <paramref name="channel"/>.
	/// </summary>
	/// <param name="channel">The channel whose component you wish to retrieve.</param>
	public float this[ColorChannel channel] => channel switch {
		ColorChannel.R => Red,
		ColorChannel.G => Green,
		ColorChannel.B => Blue,
		ColorChannel.A => Alpha,
		_ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unrecognised channel.")
	};
	/// <summary>
	/// Gets the two float components specified by <paramref name="first"/> and <paramref name="second"/>, as an <see cref="XYPair{T}"/>.
	/// </summary>
	/// <param name="first">The channel whose component you wish to retrieve as the first (<see cref="XYPair{T}.X"/>) value.</param>
	/// <param name="second">The channel whose component you wish to retrieve as the second (<see cref="XYPair{T}.Y"/>) value.</param>
	public XYPair<float> this[ColorChannel first, ColorChannel second] => new(this[first], this[second]);
	/// <summary>
	/// Gets the three float components specified by <paramref name="first"/>, <paramref name="second"/>, and <paramref name="third"/>, reassembled as a new <see cref="ColorVect"/> (with <see cref="Alpha"/> defaulting to <c>1f</c>).
	/// </summary>
	/// <param name="first">The channel whose component you wish to use as the resultant <see cref="Red"/> value.</param>
	/// <param name="second">The channel whose component you wish to use as the resultant <see cref="Green"/> value.</param>
	/// <param name="third">The channel whose component you wish to use as the resultant <see cref="Blue"/> value.</param>
	public ColorVect this[ColorChannel first, ColorChannel second, ColorChannel third] => new(this[first], this[second], this[third]);
	/// <summary>
	/// Gets the four float components specified by <paramref name="first"/>, <paramref name="second"/>, <paramref name="third"/>, and <paramref name="fourth"/>, reassembled as a new <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="first">The channel whose component you wish to use as the resultant <see cref="Red"/> value.</param>
	/// <param name="second">The channel whose component you wish to use as the resultant <see cref="Green"/> value.</param>
	/// <param name="third">The channel whose component you wish to use as the resultant <see cref="Blue"/> value.</param>
	/// <param name="fourth">The channel whose component you wish to use as the resultant <see cref="Alpha"/> value.</param>
	public ColorVect this[ColorChannel first, ColorChannel second, ColorChannel third, ColorChannel fourth] => new(this[first], this[second], this[third], this[fourth]);

	float IVect.X => Red;
	float IVect.Y => Green;
	float IVect.Z => Blue;
	float IVect.this[Axis axis] => this[(ColorChannel) axis];
	XYPair<float> IVect.this[Axis first, Axis second] => new(this[(ColorChannel) first], this[(ColorChannel) second]);
	ColorVect IVect<ColorVect>.this[Axis first, Axis second, Axis third] => new(this[(ColorChannel) first], this[(ColorChannel) second], this[(ColorChannel) third]);

	/// <summary>
	/// Constructs a new <see cref="ColorVect"/> equal to <see cref="BlackOpaque"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect() : this(0f, 0f, 0f) { }
	/// <summary>
	/// Constructs a new, fully-opaque <see cref="ColorVect"/> with the given <paramref name="red"/>, <paramref name="green"/>, and <paramref name="blue"/> components.
	/// </summary>
	/// <param name="red">The value for <see cref="Red"/>.</param>
	/// <param name="green">The value for <see cref="Green"/>.</param>
	/// <param name="blue">The value for <see cref="Blue"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue) : this(red, green, blue, 1f) { }
	/// <summary>
	/// Constructs a new <see cref="ColorVect"/> with the given <paramref name="red"/>, <paramref name="green"/>, <paramref name="blue"/>, and <paramref name="alpha"/> components.
	/// </summary>
	/// <param name="red">The value for <see cref="Red"/>.</param>
	/// <param name="green">The value for <see cref="Green"/>.</param>
	/// <param name="blue">The value for <see cref="Blue"/>.</param>
	/// <param name="alpha">The value for <see cref="Alpha"/>.</param>
	/// <param name="multiplyAlpha">If <see langword="true"/>, the constructed value has <see cref="WithPremultipliedAlpha"/> applied before being returned.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue, float alpha, bool multiplyAlpha = false) : this(new Vector4(red, green, blue, alpha)) {
		if (multiplyAlpha) this = WithPremultipliedAlpha();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ColorVect(Vector4 v) { AsVector4 = v; }
	/// <summary>
	/// Constructs a new <see cref="ColorVect"/> equivalent to <paramref name="c"/>; equivalent to <see cref="FromStandardColor"/>.
	/// </summary>
	/// <param name="c">The standard colour to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(StandardColor c) { this = FromStandardColor(c); }

	#region Factories and Conversions
	/// <summary>
	/// Returns this colour with its <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> channels multiplied by <see cref="Alpha"/>.
	/// Has no effect if <see cref="Alpha"/> is <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Premultiplied alpha representation is required by many (but not all) texture or material parameter types in TinyFFR.
	/// </remarks>
	public ColorVect WithPremultipliedAlpha() => PremultiplyAlpha(this);
	/// <summary>
	/// Returns <paramref name="nonpremultipliedInput"/> with its <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> channels multiplied by its <see cref="Alpha"/>.
	/// Has no effect if <see cref="Alpha"/> is <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Premultiplied alpha representation is required by many (but not all) texture or material parameter types in TinyFFR.
	/// </remarks>
	/// <param name="nonpremultipliedInput">The colour to premultiply.</param>
	public static ColorVect PremultiplyAlpha(ColorVect nonpremultipliedInput) {
		return new(
			nonpremultipliedInput.Red * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Green * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Blue * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Alpha
		);
	}

	/// <summary>
	/// Converts a packed 32-bit unsigned integer, in <c>0xRRGGBBAA</c> byte order, to a <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="rgba">The packed colour value, with red in the most-significant byte and alpha in the least-significant byte.</param>
	public static ColorVect FromRgba32(uint rgba) {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new(new Vector4(
			(0xFF000000 & rgba) >> 24,
			(0xFF0000 & rgba) >> 16,
			(0xFF00 & rgba) >> 8,
			0xFF & rgba
		) * Multiplicand);
	}

	/// <inheritdoc cref="FromRgba32(uint)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgba32(int rgba) => FromRgba32((uint) rgba);

	/// <summary>
	/// Constructs a <see cref="ColorVect"/> from four individual 8-bit channel values.
	/// </summary>
	/// <param name="r">The red channel, from <c>0</c> to <c>255</c>.</param>
	/// <param name="g">The green channel, from <c>0</c> to <c>255</c>.</param>
	/// <param name="b">The blue channel, from <c>0</c> to <c>255</c>.</param>
	/// <param name="a">The alpha channel, from <c>0</c> to <c>255</c>.</param>
	public static ColorVect FromRgba32(byte r, byte g, byte b, byte a) {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new(new Vector4(r, g, b, a) * Multiplicand);
	}

	/// <summary>
	/// Converts a packed 24-bit unsigned integer, in <c>0xRRGGBB</c> byte order, to a fully-opaque <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="rgb">The packed colour value, with red in the most-significant byte and blue in the least-significant byte. Only the lowest 24 bits are used.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgb24(uint rgb) => FromRgba32((rgb << 8) | Byte.MaxValue);

	/// <inheritdoc cref="FromRgb24(uint)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgb24(int rgb) => FromRgb24((uint) rgb);

	/// <summary>
	/// Constructs a fully-opaque <see cref="ColorVect"/> from three individual 8-bit channel values.
	/// </summary>
	/// <param name="r">The red channel, from <c>0</c> to <c>255</c>.</param>
	/// <param name="g">The green channel, from <c>0</c> to <c>255</c>.</param>
	/// <param name="b">The blue channel, from <c>0</c> to <c>255</c>.</param>
	public static ColorVect FromRgb24(byte r, byte g, byte b) => FromRgba32(r, g, b, Byte.MaxValue);

	/// <summary>
	/// Converts <paramref name="c"/> to an equivalent, fully-opaque <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="c">The standard colour to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromStandardColor(StandardColor c) => FromRgb24((uint) c);

	/// <summary>
	/// Constructs a fully-opaque <see cref="ColorVect"/> from a hue/saturation/lightness (HSL) representation.
	/// </summary>
	/// <param name="hue">The hue angle. Any value is accepted and wrapped to a full turn; see <see cref="RedHueAngle"/>, <see cref="GreenHueAngle"/>, and <see cref="BlueHueAngle"/> for reference points on the colour wheel.</param>
	/// <param name="saturation">The saturation, clamped to <c>[0, 1]</c>; <c>0</c> is a shade of grey, <c>1</c> is fully saturated.</param>
	/// <param name="lightness">The lightness, clamped to <c>[0, 1]</c>; <c>0</c> is black, <c>1</c> is white, with the most saturated colours at <c>0.5</c>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness) => FromHueSaturationLightness(hue, saturation, lightness, 1f);
	/// <inheritdoc cref="FromHueSaturationLightness(Angle,float,float)"/>
	/// <param name="alpha">The value for <see cref="Alpha"/>, clamped to <c>[0, 1]</c>.</param>
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness, float alpha) {
		const float SixthCircleRads = MathF.Tau / 6f;

		hue = hue.Normalized;
		var hueRads = hue.Radians;
		saturation = Single.Clamp(saturation, 0f, 1f);
		lightness = Single.Clamp(lightness, 0f, 1f);
		alpha = Single.Clamp(alpha, 0f, 1f);

		var c = (1f - MathF.Abs(2f * lightness - 1f)) * saturation;
		var x = c * (1f - MathF.Abs(MathUtils.TrueModulus(hueRads / SixthCircleRads, 2f) - 1f));
		var m = lightness - c * 0.5f;

		return hueRads switch {
			< 1f * SixthCircleRads => new(m + c, m + x, m, alpha),
			< 2f * SixthCircleRads => new(m + x, m + c, m, alpha),
			< 3f * SixthCircleRads => new(m, m + c, m + x, alpha),
			< 4f * SixthCircleRads => new(m, m + x, m + c, alpha),
			< 5f * SixthCircleRads => new(m + x, m, m + c, alpha),
			_ => new(m + c, m, m + x, alpha)
		};
	}

	/// <summary>
	/// Converts this colour from RGB to hue/saturation/lightness (HSL) representation.
	/// </summary>
	/// <remarks>
	/// Prefer this method over reading <see cref="Hue"/>, <see cref="Saturation"/>, and <see cref="Lightness"/> individually if you need all three, since each of those properties performs its own full RGB-&gt;HSL conversion.
	/// </remarks>
	/// <param name="outHue">Will be set to the equivalent of <see cref="Hue"/>.</param>
	/// <param name="outSaturation">Will be set to the equivalent of <see cref="Saturation"/>.</param>
	/// <param name="outLightness">Will be set to the equivalent of <see cref="Lightness"/>.</param>
	public void ToHueSaturationLightness(out Angle outHue, out float outSaturation, out float outLightness) {
		const float SixthCircleRads = MathF.Tau / 6f;

		var cMax = MathF.Max(Red, MathF.Max(Green, Blue));
		var cMin = MathF.Min(Red, MathF.Min(Green, Blue));
		var delta = cMax - cMin;

		outLightness = (cMax + cMin) * 0.5f;

		// ReSharper disable CompareOfFloatsByEqualityOperator Direct comparison is correct here as we're comparing with the returned value of MathF.Max which should return exactly one of its inputs
		if (delta == 0f) {
			outHue = Angle.Zero;
			outSaturation = 0f;
			return;
		}

		if (cMax == Red) {
			outHue = Angle.FromRadians(SixthCircleRads * MathUtils.TrueModulus((Green - Blue) / delta, 6f));
		}
		else if (cMax == Green) {
			outHue = Angle.FromRadians(SixthCircleRads * ((Blue - Red) / delta + 2f));
		}
		else {
			outHue = Angle.FromRadians(SixthCircleRads * ((Red - Green) / delta + 4f));
		}
		outSaturation = delta / (1f - MathF.Abs(cMax + cMin - 1f));
		// ReSharper restore CompareOfFloatsByEqualityOperator
	}

	/// <summary>
	/// Converts this colour to a packed 32-bit unsigned integer, in <c>0xRRGGBBAA</c> byte order; the inverse of <see cref="FromRgba32(uint)"/>.
	/// </summary>
	/// <remarks>
	/// Each channel is clamped to <c>[0, 1]</c> before conversion, so values outside that range do not wrap or overflow.
	/// </remarks>
	public uint ToRgba32() {
		ToRgba32(out var r, out var g, out var b, out var a);
		return (uint) ((r << 24) + (g << 16) + (b << 8) + a);
	}
	/// <summary>
	/// Converts this colour to four individual 8-bit channel values; the inverse of <see cref="FromRgba32(byte,byte,byte,byte)"/>.
	/// </summary>
	/// <remarks>
	/// Each channel is clamped to <c>[0, 1]</c> before conversion, so values outside that range do not wrap or overflow.
	/// </remarks>
	/// <param name="r">Will be set to the equivalent of <see cref="Red"/>.</param>
	/// <param name="g">Will be set to the equivalent of <see cref="Green"/>.</param>
	/// <param name="b">Will be set to the equivalent of <see cref="Blue"/>.</param>
	/// <param name="a">Will be set to the equivalent of <see cref="Alpha"/>.</param>
	public void ToRgba32(out byte r, out byte g, out byte b, out byte a) {
		const float Multiplicand = Byte.MaxValue;

		var v = AsVector4 * Multiplicand;
		r = (byte) Single.Clamp(v.X, Byte.MinValue, Byte.MaxValue);
		g = (byte) Single.Clamp(v.Y, Byte.MinValue, Byte.MaxValue);
		b = (byte) Single.Clamp(v.Z, Byte.MinValue, Byte.MaxValue);
		a = (byte) Single.Clamp(v.W, Byte.MinValue, Byte.MaxValue);
	}

	/// <summary>
	/// Converts this colour (discarding <see cref="Alpha"/>) to a packed 24-bit unsigned integer, in <c>0xRRGGBB</c> byte order; the inverse of <see cref="FromRgb24(uint)"/>.
	/// </summary>
	/// <remarks>
	/// Each channel is clamped to <c>[0, 1]</c> before conversion, so values outside that range do not wrap or overflow.
	/// </remarks>
	public uint ToRgb24() {
		ToRgb24(out var r, out var g, out var b);
		return (uint) ((r << 16) + (g << 8) + b);
	}
	/// <summary>
	/// Converts this colour (discarding <see cref="Alpha"/>) to three individual 8-bit channel values; the inverse of <see cref="FromRgb24(byte,byte,byte)"/>.
	/// </summary>
	/// <remarks>
	/// Each channel is clamped to <c>[0, 1]</c> before conversion, so values outside that range do not wrap or overflow.
	/// </remarks>
	/// <param name="r">Will be set to the equivalent of <see cref="Red"/>.</param>
	/// <param name="g">Will be set to the equivalent of <see cref="Green"/>.</param>
	/// <param name="b">Will be set to the equivalent of <see cref="Blue"/>.</param>
	public void ToRgb24(out byte r, out byte g, out byte b) {
		const float Multiplicand = Byte.MaxValue;

		var v = AsVector4 * Multiplicand;
		r = (byte) Single.Clamp(v.X, Byte.MinValue, Byte.MaxValue);
		g = (byte) Single.Clamp(v.Y, Byte.MinValue, Byte.MaxValue);
		b = (byte) Single.Clamp(v.Z, Byte.MinValue, Byte.MaxValue);
	}

	/// <summary>
	/// Converts a raw SIMD-ready <see cref="Vector3"/> to a fully-opaque <see cref="ColorVect"/>, treating its components as <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> respectively.
	/// </summary>
	/// <param name="v">The vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromVector3(Vector3 v) => new(v.X, v.Y, v.Z);

	/// <summary>
	/// Converts a raw SIMD-ready <see cref="Vector4"/> to a <see cref="ColorVect"/>, treating its components as <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> respectively.
	/// </summary>
	/// <param name="v">The vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromVector4(Vector4 v) => new(v);

	/// <summary>
	/// Converts this colour (discarding <see cref="Alpha"/>) to a raw SIMD-ready <see cref="Vector3"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	/// <summary>
	/// Converts this colour to a raw SIMD-ready <see cref="Vector4"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector4 ToVector4() => AsVector4;

	/// <summary>
	/// Deconstructs this colour (discarding <see cref="Alpha"/>) into its individual <paramref name="red"/>, <paramref name="green"/>, and <paramref name="blue"/> components.
	/// </summary>
	/// <param name="red">Will be set to the value of <see cref="Red"/>.</param>
	/// <param name="green">Will be set to the value of <see cref="Green"/>.</param>
	/// <param name="blue">Will be set to the value of <see cref="Blue"/>.</param>
	public void Deconstruct(out float red, out float green, out float blue) {
		red = Red;
		green = Green;
		blue = Blue;
	}
	/// <summary>
	/// Converts a tuple of three floats to a fully-opaque <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="tuple">The tuple to convert; its elements are mapped to <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> respectively.</param>
	public static implicit operator ColorVect((float Red, float Green, float Blue) tuple) => new(tuple.Red, tuple.Green, tuple.Blue);
	static implicit IVect<ColorVect>.operator ColorVect((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);
	/// <summary>
	/// Converts a tuple of four floats to a <see cref="ColorVect"/>.
	/// </summary>
	/// <param name="tuple">The tuple to convert; its elements are mapped to <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> respectively.</param>
	public static implicit operator ColorVect((float Red, float Green, float Blue, float Alpha) tuple) => new(tuple.Red, tuple.Green, tuple.Blue, tuple.Alpha);
	/// <summary>
	/// Deconstructs this colour into its individual <paramref name="red"/>, <paramref name="green"/>, <paramref name="blue"/>, and <paramref name="alpha"/> components.
	/// </summary>
	/// <param name="red">Will be set to the value of <see cref="Red"/>.</param>
	/// <param name="green">Will be set to the value of <see cref="Green"/>.</param>
	/// <param name="blue">Will be set to the value of <see cref="Blue"/>.</param>
	/// <param name="alpha">Will be set to the value of <see cref="Alpha"/>.</param>
	public void Deconstruct(out float red, out float green, out float blue, out float alpha) {
		red = Red;
		green = Green;
		blue = Blue;
		alpha = Alpha;
	}

	/// <summary>
	/// Converts <paramref name="c"/> to an equivalent, fully-opaque <see cref="ColorVect"/>; equivalent to <see cref="FromStandardColor"/>.
	/// </summary>
	/// <param name="c">The standard colour to convert.</param>
	public static implicit operator ColorVect(StandardColor c) => FromStandardColor(c);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Vect IVect.AsVect() => Vect.FromVector3(ToVector3());
	#endregion

	#region Random
	/// <summary>
	/// Produces a random colour, with each of <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> independently in the range <c>[0, 1]</c>.
	/// </summary>
	public static ColorVect Random() {
		return new(
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive()
		);
	}
	/// <summary>
	/// Produces a random, fully-opaque colour, with each of <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> independently in the range <c>[0, 1]</c>.
	/// </summary>
	public static ColorVect RandomOpaque() {
		return new(
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive()
		);
	}
	/// <summary>
	/// Produces a random colour, with each of <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> independently between the corresponding components of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The minimum value for each channel independently.</param>
	/// <param name="maxExclusive">The exclusive ceiling for each channel independently.</param>
	public static ColorVect Random(ColorVect minInclusive, ColorVect maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.Red, maxExclusive.Red),
			RandomUtils.NextSingle(minInclusive.Green, maxExclusive.Green),
			RandomUtils.NextSingle(minInclusive.Blue, maxExclusive.Blue),
			RandomUtils.NextSingle(minInclusive.Alpha, maxExclusive.Alpha)
		);
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc/>
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 4;

	/// <inheritdoc/>
	public static void SerializeToBytes(Span<byte> dest, ColorVect src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Red);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Green);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Blue);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 3)..], src.Alpha);
	}

	/// <inheritdoc/>
	public static ColorVect DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 3)..])
		);
	}
	#endregion

	#region String Conversion
	/// <summary>
	/// The character prefixing the red channel's value in this type's string representation (see <see cref="ToString()"/>).
	/// </summary>
	public const char RedChar = 'R';
	/// <summary>
	/// The character prefixing the green channel's value in this type's string representation (see <see cref="ToString()"/>).
	/// </summary>
	public const char GreenChar = 'G';
	/// <summary>
	/// The character prefixing the blue channel's value in this type's string representation (see <see cref="ToString()"/>).
	/// </summary>
	public const char BlueChar = 'B';
	/// <summary>
	/// The character prefixing the alpha channel's value in this type's string representation (see <see cref="ToString()"/>).
	/// </summary>
	public const char AlphaChar = 'A';

	/// <inheritdoc/>
	public override string ToString() => ToString(null, null);

	/// <inheritdoc/>
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return IVect.VectorStringPrefixChar +
			   $"{RedChar} {PercentageUtils.ConvertFractionToPercentageString(Red, format, formatProvider)}, " +
			   $"{GreenChar} {PercentageUtils.ConvertFractionToPercentageString(Green, format, formatProvider)}, " +
			   $"{BlueChar} {PercentageUtils.ConvertFractionToPercentageString(Blue, format, formatProvider)}, " +
			   $"{AlphaChar} {PercentageUtils.ConvertFractionToPercentageString(Alpha, format, formatProvider)}" +
			   IVect.VectorStringSuffixChar;
	}
	/// <inheritdoc/>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		// <
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];



		// R
		if (destination.Length < 2) return false;
		destination[0] = RedChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Red
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Red, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// G
		if (destination.Length < 2) return false;
		destination[0] = GreenChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Green
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Green, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// B
		if (destination.Length < 2) return false;
		destination[0] = BlueChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Blue
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Blue, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// A
		if (destination.Length < 2) return false;
		destination[0] = AlphaChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Alpha
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Alpha, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// >
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);

	/// <inheritdoc/>
	public static bool TryParse(string? s, IFormatProvider? provider, out ColorVect result) {
		if (s != null && TryParse(s.AsSpan(), provider, out result)) return true;
		result = default;
		return false;
	}

	/// <inheritdoc/>
	public static ColorVect Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var red = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var green = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var blue = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		// Assume ends with VectorStringSuffixChar
		var alpha = PercentageUtils.ParsePercentageStringToFraction(s[2..^1], provider); 

		return new(red, green, blue, alpha);
	}

	/// <inheritdoc/>
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ColorVect result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != RedChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var red)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != GreenChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var green)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != BlueChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var blue)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		if (s.Length < 4 || s[0] != AlphaChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..^1], provider, out var alpha)) return false;

		result = new(red, green, blue, alpha);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(ColorVect other) => AsVector4.Equals(other.AsVector4);
	/// <summary>
	/// Determines whether this colour is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(ColorVect other, float tolerance) {
		return MathF.Abs(Red - other.Red) <= tolerance
			&& MathF.Abs(Green - other.Green) <= tolerance
			&& MathF.Abs(Blue - other.Blue) <= tolerance
			&& MathF.Abs(Alpha - other.Alpha) <= tolerance;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(ColorVect left, ColorVect right) => left.Equals(right);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(ColorVect left, ColorVect right) => !left.Equals(right);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is ColorVect other && Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}