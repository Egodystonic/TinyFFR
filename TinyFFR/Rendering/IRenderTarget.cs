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
	/// <summary>
	/// The dimensions of the target in the operating system's own (logical) units; i.e. the coordinate space that mouse cursor positions are reported in.
	/// </summary>
	/// <remarks>
	/// On displays with scaling enabled, this differs from <see cref="ViewportDimensions"/> by the display's scale factor (for example, a window on a display
	/// with 200% scaling has logical dimensions half the size of its viewport dimensions). For targets that aren't shown by the operating system (such as
	/// <see cref="RenderOutputBuffer"/>s), this is the same as <see cref="ViewportDimensions"/>.
	/// </remarks>
	XYPair<int> LogicalDimensions { get; }
}