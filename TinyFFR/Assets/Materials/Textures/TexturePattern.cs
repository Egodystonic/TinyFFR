// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe class TexturePattern {
	internal const int MaxDimensionWidth = 4096;
	internal const int MaxDimensionHeight = 4096;

	internal static void AssertDimensions(XYPair<int> dimensions) {
		if (dimensions.X < 1 || dimensions.X > MaxDimensionWidth) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Width out of range (1 to {MaxDimensionWidth}).");
		}
		if (dimensions.Y < 1 || dimensions.Y > MaxDimensionHeight) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Height out of range (1 to {MaxDimensionHeight}).");
		}
	}

	public static TexturePattern<T> PlainFill<T>(T fillValue) where T : unmanaged {
		static T Generate(ReadOnlySpan<byte> args, XYPair<int> xy) {
			return MemoryMarshal.AsRef<T>(args);
		}

		var argData = new TexturePatternArgData();
		return new TexturePattern<T>((1, 1), &Generate, 
	}
}

[InlineArray(ArgsLengthMax)]
struct TexturePatternArgData { public const int ArgsLengthMax = 48; byte _; }
public readonly unsafe ref struct TexturePattern<T> where T : unmanaged {
	readonly XYPair<int> _dimensions;
	readonly delegate* managed<ReadOnlySpan<byte>, XYPair<int>, T> _generationFunc;
	readonly TexturePatternArgData _argsBuffer;

	internal XYPair<int> Dimensions => _dimensions;

	internal T this[int x, int y] {
		get {
			if (_generationFunc == null) throw InvalidObjectException.InvalidDefault(typeof(TexturePattern<T>));
			if (x < 0 || x >= _dimensions.X) throw new ArgumentOutOfRangeException(nameof(x));
			if (y < 0 || y >= _dimensions.Y) throw new ArgumentOutOfRangeException(nameof(y));
			return _generationFunc(_argsBuffer, new(x, y));
		}
	}

	public TexturePattern() {
		this = TexturePattern.PlainFill<T>(default);
	}

	internal TexturePattern(XYPair<int> dimensions, delegate*<ReadOnlySpan<byte>, XYPair<int>, T> generationFunc, TexturePatternArgData argsBuffer) {
		TexturePattern.AssertDimensions(dimensions);
		ArgumentNullException.ThrowIfNull(generationFunc);

		_dimensions = dimensions;
		_generationFunc = generationFunc;
		_argsBuffer = argsBuffer;
	}
}