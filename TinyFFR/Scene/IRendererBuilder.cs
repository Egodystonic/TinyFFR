// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Scene;

public interface IRendererBuilder {
	public Renderer CreateRenderer(Camera camera, Window window) => CreateRenderer<Window>(camera, window);
	public Renderer CreateRenderer<TRenderTarget>(Camera camera, TRenderTarget renderTarget) where TRenderTarget : IRenderTarget;
}