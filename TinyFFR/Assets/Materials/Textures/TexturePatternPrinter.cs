// Created on 2025-11-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Evaluates texture patterns in to texel buffers, or writes them to bitmap files for inspection.
/// </summary>
/// <remarks>
/// A pattern is a description rather than any actual data; this class is what turns one in to texels.
/// </remarks>
public static unsafe class TexturePatternPrinter {
	#region Helper Funcs
	static void ThrowIfBufferCanNotFitPattern(XYPair<int> dimensions, int spanLength) {
		if (dimensions.Area <= spanLength) return;
		throw new ArgumentException($"Destination buffer length ({spanLength}) was too small to accomodate pattern ({dimensions.X}x{dimensions.Y}={dimensions.Area} texels).");
	}

	/// <summary>
	/// Returns the width and height that combining the given patterns would produce.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	public static XYPair<int> GetCompositePatternDimensions<T1, T2>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2) where T1 : unmanaged where T2 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, pattern2.Dimensions.X),
			Math.Max(pattern1.Dimensions.Y, pattern2.Dimensions.Y)
		);
	}
	/// <summary>
	/// Returns the width and height that combining the given patterns would produce.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	public static XYPair<int> GetCompositePatternDimensions<T1, T2, T3>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, Math.Max(pattern2.Dimensions.X, pattern3.Dimensions.X)),
			Math.Max(pattern1.Dimensions.Y, Math.Max(pattern2.Dimensions.Y, pattern3.Dimensions.Y))
		);
	}
	/// <summary>
	/// Returns the width and height that combining the given patterns would produce.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="T4">The value type the fourth pattern produces.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="pattern4">The fourth pattern to evaluate.</param>
	public static XYPair<int> GetCompositePatternDimensions<T1, T2, T3, T4>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged {
		return new XYPair<int>(
			Math.Max(pattern1.Dimensions.X, Math.Max(pattern2.Dimensions.X, Math.Max(pattern3.Dimensions.X, pattern4.Dimensions.X))),
			Math.Max(pattern1.Dimensions.Y, Math.Max(pattern2.Dimensions.Y, Math.Max(pattern3.Dimensions.Y, pattern4.Dimensions.Y)))
		);
	}
	#endregion

	#region Print Pattern (Delegate Pointer Overloads)
	/// <summary>
	/// Evaluates a pattern in to the given buffer.
	/// </summary>
	/// <typeparam name="TTexel">The texel type the pattern produces, which is also what is written.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates a pattern in to the given buffer, converting each value in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// The overloads taking a function pointer avoid the allocation a delegate would incur, and are preferable in code
	/// that runs every frame.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the value from the pattern in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates two patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <remarks>
	/// The overloads taking a function pointer avoid the allocation a delegate would incur, and are preferable in code
	/// that runs every frame.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates three patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <remarks>
	/// The overloads taking a function pointer avoid the allocation a delegate would incur, and are preferable in code
	/// that runs every frame.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates four patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// Where several patterns are combined, the result takes the largest width and height of any of them; a smaller
	/// pattern simply repeats to fill the difference.
	/// </remarks>
	/// <remarks>
	/// The overloads taking a function pointer avoid the allocation a delegate would incur, and are preferable in code
	/// that runs every frame.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="T4">The value type the fourth pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="pattern4">The fourth pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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
	/// <summary>
	/// Evaluates a pattern in to the given buffer, converting each value in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// This overload takes a delegate for convenience; prefer the function-pointer overload in code that runs every frame,
	/// as a delegate allocates.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the value from the pattern in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates two patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// This overload takes a delegate for convenience; prefer the function-pointer overload in code that runs every frame,
	/// as a delegate allocates.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates three patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// This overload takes a delegate for convenience; prefer the function-pointer overload in code that runs every frame,
	/// as a delegate allocates.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates four patterns in to the given buffer, converting each set of values in to texels as it goes.
	/// </summary>
	/// <remarks>
	/// This overload takes a delegate for convenience; prefer the function-pointer overload in code that runs every frame,
	/// as a delegate allocates.
	/// </remarks>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="T4">The value type the fourth pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="pattern4">The fourth pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="destinationBuffer">The buffer to write the texels in to, laid out row by row (bottom to top). Must be large enough to contain the pattern's area (i.e. <c>pattern.Dimensions.Area</c>).</param>
	/// <returns>The number of texels written, which is never more than the length of <paramref name="destinationBuffer"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if the destination buffer is too small for the pattern.</exception>
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

	/// <summary>
	/// Evaluates a pattern and writes the result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="TTexel">The texel type the pattern produces.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<TTexel>(in TexturePattern<TTexel> pattern, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates a pattern and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the value from the pattern in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, TTexel>(in TexturePattern<T1> pattern, delegate* managed<T1, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, delegate* managed<T1, T2, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, delegate* managed<T1, T2, T3, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="T4">The value type the fourth pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="pattern4">The fourth pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, delegate* managed<T1, T2, T3, T4, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3, pattern4);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, pattern4, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}
	#endregion

	#region Save Pattern (Func Overloads)
	/// <summary>
	/// Evaluates a pattern and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the value from the pattern in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, TTexel>(in TexturePattern<T1> pattern, Func<T1, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = pattern.Dimensions;
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, Func<T1, T2, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, T3, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, Func<T1, T2, T3, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}

	/// <summary>
	/// Evaluates several patterns and writes the converted result to a bitmap file on disc.
	/// </summary>
	/// <typeparam name="T1">The value type the first pattern produces.</typeparam>
	/// <typeparam name="T2">The value type the second pattern produces.</typeparam>
	/// <typeparam name="T3">The value type the third pattern produces.</typeparam>
	/// <typeparam name="T4">The value type the fourth pattern produces.</typeparam>
	/// <typeparam name="TTexel">The texel type to write.</typeparam>
	/// <param name="pattern1">The first pattern to evaluate.</param>
	/// <param name="pattern2">The second pattern to evaluate.</param>
	/// <param name="pattern3">The third pattern to evaluate.</param>
	/// <param name="pattern4">The fourth pattern to evaluate.</param>
	/// <param name="conversionMapFunc">Combines the values from the patterns in to one texel.</param>
	/// <param name="bitmapFilePath">The path of the bitmap file to write. Any existing file at that path is overwritten.</param>
	/// <param name="bitmapConfig">Options for how the bitmap is written, or <see langword="null"/> for the defaults.</param>
	public static void SavePattern<T1, T2, T3, T4, TTexel>(in TexturePattern<T1> pattern1, in TexturePattern<T2> pattern2, in TexturePattern<T3> pattern3, in TexturePattern<T4> pattern4, Func<T1, T2, T3, T4, TTexel> conversionMapFunc, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? bitmapConfig = null) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where TTexel : unmanaged, ITexel<TTexel, byte> {
		var dimensions = GetCompositePatternDimensions(pattern1, pattern2, pattern3, pattern4);
		PooledHeapMemory<TTexel> pooledMemory;
		lock (_bitmapHeapPoolMutationLock) {
			pooledMemory = _bitmapHeapPool.Borrow<TTexel>(dimensions.Area);
		}
		try {
			_ = PrintPattern(pattern1, pattern2, pattern3, pattern4, conversionMapFunc, pooledMemory.Span);
			ImageUtils.SaveBitmap(bitmapFilePath, dimensions, pooledMemory.Span, bitmapConfig ?? new() { IncludeAlphaChannel = TTexel.ChannelCount > 3 });
		}
		finally {
			lock (_bitmapHeapPoolMutationLock) {
				pooledMemory.Dispose();
			}
		}
	}
	#endregion
}