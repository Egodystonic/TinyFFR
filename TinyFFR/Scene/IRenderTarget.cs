// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface IRenderTarget {
	XYPair<int> ViewportOffset { get; }
	XYPair<uint> ViewportDimensions { get; }
}