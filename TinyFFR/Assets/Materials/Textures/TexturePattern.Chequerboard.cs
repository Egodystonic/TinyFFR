// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	internal static readonly XYPair<int> ChequerboardDefaultRepetitionCount = (8, 8);
	internal static readonly XYPair<int> ChequerboardDefaultSquareSize = (64, 64);

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, XYPair<int>? repetitionCount = null, XYPair<int>? squareSize = null) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, firstValue, secondValue, repetitionCount, squareSize);
	}

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, XYPair<int>? repetitionCount = null, XYPair<int>? squareSize = null) where T : unmanaged {
		return Chequerboard(firstValue, secondValue, thirdValue, secondValue, repetitionCount, squareSize);
	}

	public static TexturePattern<T> Chequerboard<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, XYPair<int>? repetitionCount = null, XYPair<int>? squareSize = null) where T : unmanaged {
		static XYPair<int> GetTextureSize(XYPair<int> repetitionCount, XYPair<int> squareSize) => squareSize * repetitionCount;

		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> xy) {
			args
				.ReadFirstArg(out XYPair<int> repetitionCount)
				.AndThen(out XYPair<int> squareSize)
				.AndThen(out T firstValue)
				.AndThen(out T secondValue)
				.AndThen(out T thirdValue)
				.AndThen(out T fourthValue);

			var rowColumnIndices = xy / squareSize;

			return ((rowColumnIndices.X + rowColumnIndices.Y) & 0b11) switch {
				3 => fourthValue,
				2 => thirdValue,
				1 => secondValue,
				_ => firstValue
			};
		}

		var textureSize = GetTextureSize(repetitionCount ?? ChequerboardDefaultRepetitionCount, squareSize ?? ChequerboardDefaultSquareSize);
		if (textureSize.X < 1 || textureSize.Y < 1) throw new ArgumentException("Repetition count and square size must have positive components.");
		
		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(repetitionCount ?? ChequerboardDefaultRepetitionCount)
			.AndThen(squareSize ?? ChequerboardDefaultSquareSize)
			.AndThen(firstValue)
			.AndThen(secondValue)
			.AndThen(thirdValue)
			.AndThen(fourthValue);
		return new TexturePattern<T>(textureSize, &GetTexel, argData);
	}
}