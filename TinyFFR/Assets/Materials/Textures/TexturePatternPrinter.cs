// Created on 2025-11-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe class TexturePatternPrinter {
	#region Helper Funcs
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
	#endregion

	#region Print Pattern (Delegate Pointer Overloads)
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

	public static int PrintPattern<T1, TTexel>(in TexturePattern<T1> pattern, delegate* managed<T1, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged {
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

	public static int PrintPattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, delegate* managed<T1, T2, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged {
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y], 
						pattern2[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}

	public static int PrintPattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, delegate* managed<T1, T2, T3, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions && pattern2.Dimensions == pattern3.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2, in pattern3);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y],
						pattern2[x, y],
						pattern3[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y],
						pattern3[x % pattern3.Dimensions.X, y % pattern3.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}

	public static int PrintPattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, delegate* managed<T1, T2, T3, T4, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions && pattern2.Dimensions == pattern3.Dimensions && pattern3.Dimensions == pattern4.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2, in pattern3, in pattern4);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y],
						pattern2[x, y],
						pattern3[x, y],
						pattern4[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y],
						pattern3[x % pattern3.Dimensions.X, y % pattern3.Dimensions.Y],
						pattern4[x % pattern4.Dimensions.X, y % pattern4.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}
	#endregion

	#region Print Pattern (Func Overloads)
	public static int PrintPattern<T1, TTexel>(in TexturePattern<T1> pattern, Func<T1, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged {
		ArgumentNullException.ThrowIfNull(conversionMapFunc);
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

	public static int PrintPattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, Func<T1, T2, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged {
		ArgumentNullException.ThrowIfNull(conversionMapFunc);
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y],
						pattern2[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}

	public static int PrintPattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, Func<T1, T2, T3, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
		ArgumentNullException.ThrowIfNull(conversionMapFunc);
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions && pattern2.Dimensions == pattern3.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2, in pattern3);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y],
						pattern2[x, y],
						pattern3[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y],
						pattern3[x % pattern3.Dimensions.X, y % pattern3.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}

	public static int PrintPattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, Func<T1, T2, T3, T4, TTexel> conversionMapFunc, Span<TTexel> destinationBuffer) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
		ArgumentNullException.ThrowIfNull(conversionMapFunc);
		var sameDimensions = pattern1.Dimensions == pattern2.Dimensions && pattern2.Dimensions == pattern3.Dimensions && pattern3.Dimensions == pattern4.Dimensions;
		var dimensions = sameDimensions
			? pattern1.Dimensions
			: GetCompositePatternDimensions(in pattern1, in pattern2, in pattern3, in pattern4);

		ThrowIfBufferCanNotFitPattern(dimensions, destinationBuffer.Length);

		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x, y],
						pattern2[x, y],
						pattern3[x, y],
						pattern4[x, y]
					);
				}
			}
			return texelIndex;
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					destinationBuffer[texelIndex++] = conversionMapFunc(
						pattern1[x % pattern1.Dimensions.X, y % pattern1.Dimensions.Y],
						pattern2[x % pattern2.Dimensions.X, y % pattern2.Dimensions.Y],
						pattern3[x % pattern3.Dimensions.X, y % pattern3.Dimensions.Y],
						pattern4[x % pattern4.Dimensions.X, y % pattern4.Dimensions.Y]
					);
				}
			}
			return texelIndex;
		}
	}
	#endregion

	#region Save Pattern (Delegate Pointer Overloads)
	static readonly HeapPool _bitmapHeapPool = new();
	static readonly Lock _bitmapHeapPoolMutationLock = new();

	public static void SavePattern<TTexel>(in TexturePattern<TTexel> pattern, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, TTexel>(in TexturePattern<T1> pattern, delegate* managed<T1, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, delegate* managed<T1, T2, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, delegate* managed<T1, T2, T3, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, delegate* managed<T1, T2, T3, T4, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3, pattern4);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, pattern4, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}
	#endregion

	#region Save Pattern (Func Overloads)
	public static void SavePattern<T1, TTexel>(in TexturePattern<T1> pattern, Func<T1, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, Func<T1, T2, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, Func<T1, T2, T3, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	public static void SavePattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, Func<T1, T2, T3, T4, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3, pattern4);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, pattern4, conversionMapFunc, pooledMemory.Buffer);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Buffer, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}
	#endregion
}