// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets;

public interface IBuiltInTexturePathLibrary {
	ReadOnlySpan<char> Gray100Percent { get; }
	ReadOnlySpan<char> Gray90Percent { get; }
	ReadOnlySpan<char> Gray80Percent { get; }
	ReadOnlySpan<char> Gray70Percent { get; }
	ReadOnlySpan<char> Gray60Percent { get; }
	ReadOnlySpan<char> Gray50Percent { get; }
	ReadOnlySpan<char> Gray40Percent { get; }
	ReadOnlySpan<char> Gray30Percent { get; }
	ReadOnlySpan<char> Gray20Percent { get; }
	ReadOnlySpan<char> Gray10Percent { get; }
	ReadOnlySpan<char> Gray0Percent { get; }

	ReadOnlySpan<char> White => Gray100Percent;
	ReadOnlySpan<char> Black => Gray0Percent;
}