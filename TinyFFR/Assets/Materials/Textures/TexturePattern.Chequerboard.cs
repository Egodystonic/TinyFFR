// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternDefaultValues;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	/// <summary>
	/// Creates a chequerboard pattern alternating between two values.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, firstValue, secondValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern cycling through three values.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="thirdValue">The value of the third cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, thirdValue, secondValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern cycling through four values.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="thirdValue">The value of the third cell.</param>
	/// <param name="fourthValue">The value of the fourth cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return ChequerboardBordered(firstValue, 0, firstValue, secondValue, thirdValue, fourthValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern of one value, with a border drawn around each cell.
	/// </summary>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="borderValue">The value of the border around each cell.</param>
	/// <param name="borderWidth">The thickness of the border drawn around each cell, in texels.</param>
	/// <param name="firstValue">The value of the cells' interiors.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, firstValue, firstValue, firstValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern alternating between two values, with a border drawn around each cell.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="borderValue">The value of the border around each cell.</param>
	/// <param name="borderWidth">The thickness of the border drawn around each cell, in texels.</param>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, secondValue, firstValue, secondValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern cycling through three values, with a border drawn around each cell.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="borderValue">The value of the border around each cell.</param>
	/// <param name="borderWidth">The thickness of the border drawn around each cell, in texels.</param>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="thirdValue">The value of the third cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, T thirdValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, secondValue, thirdValue, secondValue, repetitionCount, cellResolution, transform);
	}

	/// <summary>
	/// Creates a chequerboard pattern cycling through four values, with a border drawn around each cell.
	/// </summary>
	/// <remarks>
	/// Cells take each of the given values in turn, so a two-value chequerboard alternates in the familiar way and a
	/// four-value one cycles through all four.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="borderValue">The value of the border around each cell.</param>
	/// <param name="borderWidth">The thickness of the border drawn around each cell, in texels.</param>
	/// <param name="firstValue">The value of the first cell.</param>
	/// <param name="secondValue">The value of the second cell.</param>
	/// <param name="thirdValue">The value of the third cell.</param>
	/// <param name="fourthValue">The value of the fourth cell.</param>
	/// <param name="repetitionCount">How many cells the pattern is across and down, or <see langword="null"/> for <c>(8, 8)</c>.</param>
	/// <param name="cellResolution">The width and height of one cell, in texels. Defaults to <c>64</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, T thirdValue, T fourthValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution, Transform2D? transform = null) where T : unmanaged {
		static XYPair<int> GetTextureSize(XYPair<int> repetitionCount, int cellResolution) => cellResolution * repetitionCount;

		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out int cellResolution)
				.AndThen(out T firstValue)
				.AndThen(out T secondValue)
				.AndThen(out T thirdValue)
				.AndThen(out T fourthValue)
				.AndThen(out T borderValue)
				.AndThen(out int borderWidth);

			FlipY(dimensions, ref xy);
			var xyModCellRes = new XYPair<int>(xy.X % cellResolution, xy.Y % cellResolution);
			var distanceToSquareEdge = Int32.Min(Int32.Min(xyModCellRes.X, cellResolution - xyModCellRes.X), Int32.Min(xyModCellRes.Y, cellResolution - xyModCellRes.Y));
			if (distanceToSquareEdge < borderWidth) return borderValue;
			
			var rowColumnIndices = xy / cellResolution;
			return ((rowColumnIndices.X + rowColumnIndices.Y) & 0b11) switch {
				3 => fourthValue,
				2 => thirdValue,
				1 => secondValue,
				_ => firstValue
			};
		}

		var textureSize = GetTextureSize(repetitionCount ?? ChequerboardDefaultRepetitionCount, cellResolution);

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(cellResolution)
			.AndThen(firstValue)
			.AndThen(secondValue)
			.AndThen(thirdValue)
			.AndThen(fourthValue)
			.AndThen(borderValue)
			.AndThen(borderWidth);
		return new TexturePattern<T>(textureSize, &GetTexel, argData, transform);
	}
}