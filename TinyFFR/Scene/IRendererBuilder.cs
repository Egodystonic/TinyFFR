// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Scene;

public interface IRendererBuilder {
	public Renderer CreateRenderer(Scene scene, Camera camera, Window window, ReadOnlySpan<char> name = default) => CreateRenderer<Window>(scene, camera, window, name);
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget {
		return CreateRenderer(scene, camera, renderTarget, new RendererCreationConfig { Name = name });
	}
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget;
}