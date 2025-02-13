// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public const int DefaultLineThickness = 16;

	static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, T eighthValue, T ninthValue, T tenthValue, int numValues, bool horizontal, int lineThickness, int numRepeats) where T : unmanaged {
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

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(numValues)
			.AndThen(numRepeats)
			.AndThen(horizontal)
			.AndThen(lineThickness)
			.AndThen(firstValue)
			.AndThen(secondValue)
			.AndThen(thirdValue)
			.AndThen(fourthValue)
			.AndThen(fifthValue)
			.AndThen(sixthValue)
			.AndThen(seventhValue)
			.AndThen(eighthValue)
			.AndThen(ninthValue)
			.AndThen(tenthValue);
		return new TexturePattern<T>(textureSize, &GetTexel, argData);
	}
}