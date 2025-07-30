// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Rendering;

public unsafe interface IRenderOutputBufferImplProvider : IDisposableResourceImplProvider<RenderOutputBuffer> {
	XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle);
	void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, bool handleOnlyNextChange);
	void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, bool handleOnlyNextChange);
}