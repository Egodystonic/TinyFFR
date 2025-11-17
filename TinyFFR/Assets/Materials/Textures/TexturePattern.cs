// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	internal const int MaxDimensionWidth = 16_384;
	internal const int MaxDimensionHeight = MaxDimensionWidth;

	internal static void AssertDimensions(XYPair<int> dimensions) {
		if (dimensions.X < 1 || dimensions.X > MaxDimensionWidth) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Given pattern's resultant dimensions would result in a width that is either non-positive or too large (max {MaxDimensionWidth}).");
		}
		if (dimensions.Y < 1 || dimensions.Y > MaxDimensionHeight) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Given pattern's resultant dimensions would result in a height that is either non-positive or too large (max {MaxDimensionHeight}).");
		}
	}

	static Span<byte> WriteFirstArg<T>(this ref TexturePatternArgData argData, T arg) where T : unmanaged {
		return ((Span<byte>) argData).AndThen(arg);
	}
	static Span<byte> AndThen<T>(this Span<byte> argData, T arg) where T : unmanaged {
		if (argData.Length < sizeof(T)) {
			throw new InvalidOperationException($"The given texture pattern can not be used with arguments of " +
												$"type '{typeof(T).Name}' because they are too large to fit in internal " +
												$"argument data on the stack.");
		}
		MemoryMarshal.Write(argData, arg);
		return argData[sizeof(T)..];
	}
	static ReadOnlySpan<byte> ReadFirstArg<T>(this ReadOnlySpan<byte> args, out T outValue) where T : unmanaged {
		return AndThen(args, out outValue);
	}
	static ReadOnlySpan<byte> AndThen<T>(this ReadOnlySpan<byte> args, out T outValue) where T : unmanaged {
		outValue = MemoryMarshal.Read<T>(args);
		return args[sizeof(T)..];
	}

	static void FlipX(XYPair<int> dimensions, ref XYPair<int> xy) => xy = xy with { X = dimensions.X - (xy.X + 1) };
	static void FlipY(XYPair<int> dimensions, ref XYPair<int> xy) => xy = xy with { Y = dimensions.Y - (xy.Y + 1) };
}

#pragma warning disable CA1815 // "TexturePattern<T> should implement equality members" -- There's no reasonable equality comparison for two instances, function pointers can not be compared
[InlineArray(ArgsLengthMax)]
struct TexturePatternArgData { public const int ArgsLengthMax = 256; byte _; }
public readonly unsafe struct TexturePattern<T> where T : unmanaged {
	readonly XYPair<int> _dimensions;
	readonly delegate* managed<ReadOnlySpan<byte>, XYPair<int>, XYPair<int>, T> _generationFunc;
	readonly TexturePatternArgData _argsBuffer;
	readonly Transform2D? _transform;

	public XYPair<int> Dimensions => _dimensions;

	public T this[int x, int y] {
		get {
			if (_generationFunc == null) throw InvalidObjectException.InvalidDefault<TexturePattern<T>>();
			if (x < 0 || x >= _dimensions.X) throw new ArgumentOutOfRangeException(nameof(x));
			if (y < 0 || y >= _dimensions.Y) throw new ArgumentOutOfRangeException(nameof(y));
			if (_transform == null) return _generationFunc(_argsBuffer, _dimensions, new(x, y));

			var transformedXy = _transform.Value.AppliedTo(new XYPair<int>(x, y));
			transformedXy = new(MathUtils.TrueModulus(transformedXy.X, _dimensions.X), MathUtils.TrueModulus(transformedXy.Y, _dimensions.Y));
			return _generationFunc(_argsBuffer, _dimensions, transformedXy);
		}
	}

	public TexturePattern() {
		this = TexturePattern.PlainFill<T>(default);
	}

	internal TexturePattern(XYPair<int> dimensions, delegate*<ReadOnlySpan<byte>, XYPair<int>, XYPair<int>, T> generationFunc, TexturePatternArgData argsBuffer, Transform2D? transform) {
		TexturePattern.AssertDimensions(dimensions);
		ArgumentNullException.ThrowIfNull(generationFunc);

		_dimensions = dimensions;
		_generationFunc = generationFunc;
		_argsBuffer = argsBuffer;

		if (transform.HasValue) {
			_transform = (transform.Value with { Translation = transform.Value.Translation * dimensions.Cast<float>() }).Inverse;
		}
		else _transform = null;
	}
}
#pragma warning restore CA1815