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
	/// Creates a pattern of one uniform value, a single texel in size.
	/// </summary>
	/// <remarks>
	/// This is the cheapest way to supply a constant value where a whole pattern is expected.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="fillValue">The value every texel takes.</param>
	public static TexturePattern<T> PlainFill<T>(T fillValue) where T : unmanaged {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args.ReadFirstArg(out T result);
			return result;
		}

		var argData = new TexturePatternArgData();
		argData.WriteFirstArg(fillValue);
		return new TexturePattern<T>((1, 1), &GetTexel, argData, null);
	}
}