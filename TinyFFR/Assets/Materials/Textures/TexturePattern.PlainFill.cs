// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public static TexturePattern<T> PlainFill<T>(T fillValue) where T : unmanaged {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> xy) {
			args.ReadFirstArg(out T result);
			return result;
		}

		var argData = new TexturePatternArgData();
		argData.WriteFirstArg(fillValue);
		return new TexturePattern<T>((1, 1), &GetTexel, argData);
	}
}