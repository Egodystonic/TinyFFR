// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public const int LineDefaultRepeatCount = 4;
	public const int LineDefaultTextureSize = 1024;
	public const float LineDefaultPerturbationMagnitude = 0f;
	public const float LineDefaultPerturbationFrequency = 1f;

	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 2;
		return Lines(
			firstValue,
			secondValue,
			default, default, default, default, default, default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 3;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			default, default, default, default, default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 4;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue, 
			default, default, default, default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 5;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue, 
			default, default, default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 6;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue,
			sixthValue, 
			default, default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 7;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue,
			sixthValue,
			seventhValue,
			default, default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, T eighthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 8;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue,
			sixthValue,
			seventhValue,
			eighthValue, 
			default, default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, T eighthValue, T ninthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 9;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue,
			sixthValue,
			seventhValue,
			eighthValue,
			ninthValue, 
			default,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}
	public static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, T eighthValue, T ninthValue, T tenthValue, bool horizontal, int numRepeats = LineDefaultRepeatCount, float perturbationMagnitude = LineDefaultPerturbationMagnitude, float perturbationFrequency = LineDefaultPerturbationFrequency, int? lineThickness = null, int? colinearSize = null, Transform2D? transform = null) where T : unmanaged {
		const int NumValues = 10;
		return Lines(
			firstValue,
			secondValue,
			thirdValue,
			fourthValue,
			fifthValue,
			sixthValue,
			seventhValue,
			eighthValue,
			ninthValue,
			tenthValue,
			NumValues,
			horizontal,
			lineThickness ?? LineDefaultTextureSize / (NumValues * numRepeats),
			numRepeats,
			perturbationMagnitude,
			perturbationFrequency,
			colinearSize,
			transform
		);
	}

	static TexturePattern<T> Lines<T>(T firstValue, T secondValue, T thirdValue, T fourthValue, T fifthValue, T sixthValue, T seventhValue, T eighthValue, T ninthValue, T tenthValue, int numValues, bool horizontal, int lineThickness, int numRepeats, float perturbationMagnitude, float perturbationFrequency, int? colinearSize, Transform2D? transform) where T : unmanaged {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out int numValues)
				.AndThen(out int numRepeats)
				.AndThen(out bool horizontal)
				.AndThen(out int lineThickness)
				.AndThen(out float perturbationMagnitude)
				.AndThen(out float perturbationFrequency)
				.AndThen(out T firstValue)
				.AndThen(out T secondValue)
				.AndThen(out T thirdValue)
				.AndThen(out T fourthValue)
				.AndThen(out T fifthValue)
				.AndThen(out T sixthValue)
				.AndThen(out T seventhValue)
				.AndThen(out T eighthValue)
				.AndThen(out T ninthValue)
				.AndThen(out T tenthValue);

			FlipY(dimensions, ref xy);

			int colinearCoord, orthogonalCoord, colinearDimension, orthogonalDimension;
			if (horizontal) {
				colinearCoord = xy.X;
				colinearDimension = dimensions.X;
				orthogonalCoord = xy.Y;
				orthogonalDimension = dimensions.Y;
			}
			else {
				colinearCoord = xy.Y;
				colinearDimension = dimensions.Y;
				orthogonalCoord = xy.X;
				orthogonalDimension = dimensions.X;
			}

			var colinearDistance = (float) colinearCoord / colinearDimension;
			var orthogonalDistance = MathUtils.TrueModulus(((float) orthogonalCoord / orthogonalDimension) + (MathF.Sin(Angle.FullCircle.Radians * colinearDistance * perturbationFrequency) * perturbationMagnitude), 1f);

			var lineThicknessNormalized = 1f / (numValues * numRepeats);
			var lineIndex = (int) (orthogonalDistance / lineThicknessNormalized);

			return (lineIndex % numValues) switch {
				0 => firstValue,
				1 => secondValue,
				2 => thirdValue,
				3 => fourthValue,
				4 => fifthValue,
				5 => sixthValue,
				6 => seventhValue,
				7 => eighthValue,
				8 => ninthValue,
				_ => tenthValue
			};
		}

		var orthogonalExtent = lineThickness * numValues * numRepeats;
		var colinearExtent = colinearSize ?? (perturbationMagnitude != 0f ? LineDefaultTextureSize : 1);

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(numValues)
			.AndThen(numRepeats)
			.AndThen(horizontal)
			.AndThen(lineThickness)
			.AndThen(perturbationMagnitude)
			.AndThen(perturbationFrequency)
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
		return new TexturePattern<T>(horizontal ? (colinearExtent, orthogonalExtent) : (orthogonalExtent, colinearExtent), &GetTexel, argData, transform);
	}
}