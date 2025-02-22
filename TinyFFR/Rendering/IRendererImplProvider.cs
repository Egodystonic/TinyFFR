// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererImplProvider : IDisposableResourceImplProvider<Renderer> {
	void Render(ResourceHandle<Renderer> handle);
}