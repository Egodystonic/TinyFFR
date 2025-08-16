// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRenderTarget {
	XYPair<int> ViewportOffset { get; }
	XYPair<int> ViewportDimensions { get; }
}