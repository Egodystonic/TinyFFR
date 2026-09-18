// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Interface representing anything a <see cref="Renderer"/> can render in to.
/// </summary>
public interface IRenderTarget {
	/// <summary>
	/// The offset in to the target (back) buffer to begin rendering in to.
	/// </summary>
	XYPair<int> ViewportOffset { get; }
	/// <summary>
	/// The actual pixel dimensions of the target (back) buffer.
	/// </summary>
	XYPair<int> ViewportDimensions { get; }
}