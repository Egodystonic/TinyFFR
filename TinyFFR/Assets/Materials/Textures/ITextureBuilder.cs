// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets.Materials;

public unsafe interface ITextureBuilder {
	#region Texture Creation And Processing
	protected readonly ref struct PreallocatedBuffer<TTexel> where TTexel : unmanaged, ITexel<TTexel> {
		public nuint BufferId { get; }
		public Span<TTexel> Span { get; }
		public PreallocatedBuffer(UIntPtr bufferId, Span<TTexel> span) {
			BufferId = bufferId;
			Span = span;
		}
	}

	protected Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	protected PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel>;

	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, in generationConfig, in config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	void ProcessTexture<TTexel>(Span<TTexel> texels, XYPair<int> dimensions, in TextureProcessingConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Pattern Printing / Helpers
	private int PrintPattern<TTexel>(in TexturePattern<TTexel> pattern, Span<TTexel> destinationBuffer) where TTexel : unmanaged {
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
	private int PrintPattern<T, TTexel>(in TexturePattern<T> pattern, Span<TTexel> destinationBuffer) where T : unmanaged where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, T> {
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
	private int PrintPattern<T, TTexel>(in TexturePattern<T> pattern, delegate* managed<T, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T : unmanaged where TTexel : unmanaged, ITexel<TTexel> {
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

	XYPair<int> GetCompositePatternDimensions<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern) where T : unmanaged {
		return new XYPair<int>(
			Math.Max(xRedPattern.Dimensions.X, Math.Max(yGreenPattern.Dimensions.X, zBluePattern.Dimensions.X)),
			Math.Max(xRedPattern.Dimensions.Y, Math.Max(yGreenPattern.Dimensions.Y, zBluePattern.Dimensions.Y))
		);
	}
	XYPair<int> GetCompositePatternDimensions<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern, in TexturePattern<T> wAlphaPattern) where T : unmanaged {
		return new XYPair<int>(
			Math.Max(xRedPattern.Dimensions.X, Math.Max(yGreenPattern.Dimensions.X, Math.Max(zBluePattern.Dimensions.X, wAlphaPattern.Dimensions.X))),
			Math.Max(xRedPattern.Dimensions.Y, Math.Max(yGreenPattern.Dimensions.Y, Math.Max(zBluePattern.Dimensions.Y, wAlphaPattern.Dimensions.Y)))
		);
	}

	private int PrintPattern<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern, delegate* managed<T, byte> conversionMapFunc, Span<TexelRgb24> destinationBuffer) where T : unmanaged {
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
	private int PrintPattern<T>(in TexturePattern<T> xRedPattern, in TexturePattern<T> yGreenPattern, in TexturePattern<T> zBluePattern, in TexturePattern<T> wAlphaPattern, delegate* managed<T, byte> conversionMapFunc, Span<TexelRgba32> destinationBuffer) where T : unmanaged {
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

	private void ThrowIfBufferCanNotFitPattern(XYPair<int> dimensions, int spanLength) {
		if (dimensions.Area <= spanLength) return;
		throw new ArgumentException($"Destination buffer length ({spanLength}) was too small to accomodate pattern ({dimensions.X}x{dimensions.Y}={dimensions.Area} texels).");
	}
	#endregion

	#region Generic Patterns
	int CreateTexture<T>(in TexturePattern<T> pattern, Span<T> destinationBuffer) where T : unmanaged => PrintPattern(pattern, destinationBuffer);

	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			pattern,
			CreateCreationConfig(
				pattern.Dimensions.Area,
				isLinearColorspace,
				TTexel.BlitType,
				TextureProcessingConfig.None,
				name
			)
		);
	}
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = PreallocateBuffer<TTexel>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}
	#endregion

	#region Color Map Patterns
	int CreateTexture<TTexel>(in TexturePattern<ColorVect> pattern, Span<TTexel> destinationBuffer) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, ColorVect> => PrintPattern(pattern, destinationBuffer);

	Texture CreateTexture(in TexturePattern<ColorVect> pattern, bool includeAlpha, ReadOnlySpan<char> name = default) { 
		var creationConfig = CreateCreationConfig(
			pattern.Dimensions.Area,
			false,
			includeAlpha ? TexelType.Rgba32 : TexelType.Rgb24,
			TextureProcessingConfig.None,
			name
		);
		return CreateTexture(pattern, in creationConfig); 
	}
	Texture CreateTexture(in TexturePattern<ColorVect> pattern, in TextureCreationConfig config) {
		if (config.TexelType == TexelType.Rgb24) {
			var buffer = PreallocateBuffer<TexelRgb24>(pattern.Dimensions.Area);
			_ = PrintPattern(pattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
		}
		else if (config.TexelType == TexelType.Rgba32) {
			var buffer = PreallocateBuffer<TexelRgba32>(pattern.Dimensions.Area);
			_ = PrintPattern(pattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
		}

		throw new ArgumentException($"Unsupported texel type '{config.TexelType}'.");
	}
	#endregion

	#region Normal Map Patterns
	private static TexelRgb24 Convert(UnitSphericalCoordinate coord) {
		const float Multiplicand = Byte.MaxValue * 0.5f;

		var v = coord.ToDirection(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
					.ToVector3()
					+ Vector3.One;
		v *= Multiplicand;
		return new((byte) v.X, (byte) v.Y, (byte) v.Z);
	}

	int CreateTexture(in TexturePattern<UnitSphericalCoordinate> pattern, Span<TexelRgb24> destinationBuffer) => PrintPattern(pattern, &Convert, destinationBuffer);

	Texture CreateTexture(in TexturePattern<UnitSphericalCoordinate> pattern, ReadOnlySpan<char> name = default) {
		return CreateTexture(
			pattern,
			CreateCreationConfig(
				pattern.Dimensions.Area,
				isLinearColorspace: true,
				texelType: TexelType.Rgb24,
				processingConfig: TextureProcessingConfig.None,
				name
			)
		);
	}
	Texture CreateTexture(in TexturePattern<UnitSphericalCoordinate> pattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, &Convert, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}
	#endregion

	#region Orm/Ormr Map Patterns
	private static byte Convert(byte b) => b;
	private static byte Convert(Real r) => (byte) (r * Byte.MaxValue);

	int CreateTexture(in TexturePattern<byte> xRedPattern, in TexturePattern<byte> yGreenPattern, in TexturePattern<byte> zBluePattern, Span<TexelRgb24> destinationBuffer) {
		return PrintPattern(xRedPattern, yGreenPattern, zBluePattern, &Convert, destinationBuffer);
	}
	int CreateTexture(in TexturePattern<byte> xRedPattern, in TexturePattern<byte> yGreenPattern, in TexturePattern<byte> zBluePattern, in TexturePattern<byte> wAlphaPattern, Span<TexelRgba32> destinationBuffer) {
		return PrintPattern(xRedPattern, yGreenPattern, zBluePattern, wAlphaPattern, &Convert, destinationBuffer);
	}
	int CreateTexture(in TexturePattern<Real> xRedPattern, in TexturePattern<Real> yGreenPattern, in TexturePattern<Real> zBluePattern, Span<TexelRgb24> destinationBuffer) {
		return PrintPattern(xRedPattern, yGreenPattern, zBluePattern, &Convert, destinationBuffer);
	}
	int CreateTexture(in TexturePattern<Real> xRedPattern, in TexturePattern<Real> yGreenPattern, in TexturePattern<Real> zBluePattern, in TexturePattern<Real> wAlphaPattern, Span<TexelRgba32> destinationBuffer) {
		return PrintPattern(xRedPattern, yGreenPattern, zBluePattern, wAlphaPattern, &Convert, destinationBuffer);
	}

	Texture CreateTexture(in TexturePattern<byte> xRedPattern, in TexturePattern<byte> yGreenPattern, in TexturePattern<byte> zBluePattern, ReadOnlySpan<char> name = default) {
		return CreateTexture(
			xRedPattern,
			yGreenPattern,
			zBluePattern,
			CreateCreationConfig(
				GetCompositePatternDimensions(in xRedPattern, in yGreenPattern, in zBluePattern).Area,
				isLinearColorspace: true,
				texelType: TexelType.Rgb24,
				processingConfig: TextureProcessingConfig.None,
				name
			)
		);
	}

	Texture CreateTexture(in TexturePattern<byte> xRedPattern, in TexturePattern<byte> yGreenPattern, in TexturePattern<byte> zBluePattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, &Convert, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}
	#endregion
}