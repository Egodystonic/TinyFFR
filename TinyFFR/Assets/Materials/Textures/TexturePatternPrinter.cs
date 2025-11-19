// Created on 2025-11-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe class TexturePatternPrinter {
	#region Texel Conversions & Helper Funcs
	static void ThrowIfBufferCanNotFitPattern(XYPair<int> dimensions, int spanLength) {
		if (dimensions.Area <= spanLength) return;
		throw new ArgumentException($"Destination buffer length ({spanLength}) was too small to accomodate pattern ({dimensions.X}x{dimensions.Y}={dimensions.Area} texels).");
	}

	public static XYPair<int> GetCompositePatternDimensions<T1, T2>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2) where T1 : unmanaged where T2 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, pattern2.Dimensions.X),
			Math.Max(pattern1.Dimensions.Y, pattern2.Dimensions.Y)
		);
	}
	public static XYPair<int> GetCompositePatternDimensions<T1, T2, T3>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, Math.Max(pattern2.Dimensions.X, pattern3.Dimensions.X)),
			Math.Max(pattern1.Dimensions.Y, Math.Max(pattern2.Dimensions.Y, pattern3.Dimensions.Y))
		);
	}
	public static XYPair<int> GetCompositePatternDimensions<T1, T2, T3, T4>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, Math.Max(pattern2.Dimensions.X, Math.Max(pattern3.Dimensions.X, pattern4.Dimensions.X))),
			Math.Max(pattern1.Dimensions.Y, Math.Max(pattern2.Dimensions.Y, Math.Max(pattern3.Dimensions.Y, pattern4.Dimensions.Y)))
		);
	}

	public static TexelRgb24 ConvertSphericalCoordToNormalTexel(UnitSphericalCoordinate coord) {
		const float Multiplicand = Byte.MaxValue * 0.5f;

		var v = coord.ToDirection(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
					.ToVector3()
					+ Vector3.One;
		v *= Multiplicand;
		return new((byte) v.X, (byte) v.Y, (byte) v.Z);
	}

	public static byte ConvertNormalizedRealToTexelByteChannel(Real r) => (byte) (r * Byte.MaxValue);
	#endregion

	#region Fundamental Print Functions
	public static int PrintPattern<TTexel>(in TexturePattern<TTexel> pattern, Span<TTexel> destinationBuffer) where TTexel : unmanaged {
		var dimensions = pattern.Dimensions;
		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				destinationBuffer[texelIndex++] = pattern[x, y];
			}
		}

		return texelIndex;
	}
	public static int PrintPattern<T, TTexel>(in TexturePattern<T> pattern, Span<TTexel> destinationBuffer) where T : unmanaged where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, T> {
		var dimensions = pattern.Dimensions;
		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				destinationBuffer[texelIndex++] = TTexel.ConvertFrom(pattern[x, y]);
			}
		}
		return texelIndex;
	}
	public static int PrintPattern<T, TTexel>(in TexturePattern<T> pattern, delegate* managed<T, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T : unmanaged where TTexel : unmanaged {
		var dimensions = pattern.Dimensions;
		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				destinationBuffer[texelIndex++] = conversionMapFunc(pattern[x, y]);
			}
		}

		return texelIndex;
	}

	public static int PrintPattern<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, delegate* managed<T, byte> conversionMapFunc, Span<TexelRgb24> destinationBuffer) where T : unmanaged {
		var sameDimensions = xRedPattern.Dimensions == yGreenPattern.Dimensions;
		var dimensions = sameDimensions
			? xRedPattern.Dimensions
			: GetCompositePatternDimensions(in xRedPattern, in yGreenPattern);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x, y]),
						conversionMapFunc(yGreenPattern[x, y]),
						0
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x % xRedPattern.Dimensions.X, y % xRedPattern.Dimensions.Y]),
						conversionMapFunc(yGreenPattern[x % yGreenPattern.Dimensions.X, y % yGreenPattern.Dimensions.Y]),
						0
					);
				}
			}
			return texelIndex;
		}
	}
	public static int PrintPattern<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern, delegate* managed<T, byte> conversionMapFunc, Span<TexelRgb24> destinationBuffer) where T : unmanaged {
		var sameDimensions = xRedPattern.Dimensions == yGreenPattern.Dimensions && yGreenPattern.Dimensions == zBluePattern.Dimensions;
		var dimensions = sameDimensions
			? xRedPattern.Dimensions
			: GetCompositePatternDimensions(in xRedPattern, in yGreenPattern, in zBluePattern);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x, y]),
						conversionMapFunc(yGreenPattern[x, y]),
						conversionMapFunc(zBluePattern[x, y])
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x % xRedPattern.Dimensions.X, y % xRedPattern.Dimensions.Y]),
						conversionMapFunc(yGreenPattern[x % yGreenPattern.Dimensions.X, y % yGreenPattern.Dimensions.Y]),
						conversionMapFunc(zBluePattern[x % zBluePattern.Dimensions.X, y % zBluePattern.Dimensions.Y])
					);
				}
			}
			return texelIndex;
		}
	}
	public static int PrintPattern<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern, in TexturePattern<T> wAlphaPattern, delegate* managed<T, byte> conversionMapFunc, Span<TexelRgba32> destinationBuffer) where T : unmanaged {
		var sameDimensions = xRedPattern.Dimensions == yGreenPattern.Dimensions && yGreenPattern.Dimensions == zBluePattern.Dimensions && zBluePattern.Dimensions == wAlphaPattern.Dimensions;
		var dimensions = sameDimensions
			? xRedPattern.Dimensions
			: GetCompositePatternDimensions(in xRedPattern, in yGreenPattern, in zBluePattern, in wAlphaPattern);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x, y]),
						conversionMapFunc(yGreenPattern[x, y]),
						conversionMapFunc(zBluePattern[x, y]),
						conversionMapFunc(wAlphaPattern[x, y])
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x % xRedPattern.Dimensions.X, y % xRedPattern.Dimensions.Y]),
						conversionMapFunc(yGreenPattern[x % yGreenPattern.Dimensions.X, y % yGreenPattern.Dimensions.Y]),
						conversionMapFunc(zBluePattern[x % zBluePattern.Dimensions.X, y % zBluePattern.Dimensions.Y]),
						conversionMapFunc(wAlphaPattern[x % wAlphaPattern.Dimensions.X, y % wAlphaPattern.Dimensions.Y])
					);
				}
			}
			return texelIndex;
		}
	}

	// TODO Offer BMP functions below; add Func<> overloads; add default values to TexturePattern (e.g. TexturePattern.DefaultMapValues.Metallic)
	public static int PrintPattern<TXyz, TW>(in TexturePattern<TXyz> xyzRgbPattern, in TexturePattern<TW> wAlphaPattern, delegate* managed<TXyz, TexelRgb24> xyzConversionMapFunc, delegate* managed<TW, byte> wConversionMapFunc, Span<TexelRgba32> destinationBuffer) where TXyz : unmanaged where TW : unmanaged {
		var sameDimensions = xyzRgbPattern.Dimensions == wAlphaPattern.Dimensions;
		var dimensions = sameDimensions
			? xyzRgbPattern.Dimensions
			: GetCompositePatternDimensions(in xyzRgbPattern, in wAlphaPattern);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						xyzConversionMapFunc(xyzRgbPattern[x, y]),
						wConversionMapFunc(wAlphaPattern[x, y])
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = new(
						xyzConversionMapFunc(xyzRgbPattern[x % xyzRgbPattern.Dimensions.X, y % xyzRgbPattern.Dimensions.Y]),
						wConversionMapFunc(wAlphaPattern[x % wAlphaPattern.Dimensions.X, y % wAlphaPattern.Dimensions.Y])
					);
				}
			}
			return texelIndex;
		}
	}
	#endregion
}