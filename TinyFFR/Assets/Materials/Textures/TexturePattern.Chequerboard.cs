// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public static readonly XYPair<int> ChequerboardDefaultRepetitionCount = (8, 8);
	public const int ChequerboardDefaultCellResolution = 64;

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, firstValue, secondValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, thirdValue, secondValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return ChequerboardBordered(firstValue, 0, firstValue, secondValue, thirdValue, fourthValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, firstValue, firstValue, firstValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, secondValue, firstValue, secondValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, T thirdValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
		return ChequerboardBordered(borderValue, borderWidth, firstValue, secondValue, thirdValue, secondValue, repetitionCount, cellResolution);
	}

	public static TexturePattern<T> ChequerboardBordered<T>(T borderValue, int borderWidth, T firstValue, T secondValue, T thirdValue, T fourthValue, XYPair<int>? repetitionCount = null, int cellResolution = ChequerboardDefaultCellResolution) where T : unmanaged {
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
		return new TexturePattern<T>(textureSize, &GetTexel, argData);
	}
}