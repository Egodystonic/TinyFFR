// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererBuilder {
	public Renderer CreateRenderer(Scene scene, Camera camera, Window window, ReadOnlySpan<char> name = default) => CreateRenderer<Window>(scene, camera, window, name);
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget {
		return CreateRenderer(scene, camera, renderTarget, new RendererCreationConfig { Name = name });
	}
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget;

	public RenderOutputBuffer CreateRenderOutputBuffer(ReadOnlySpan<char> name = default) => CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig { Name = name });
	public RenderOutputBuffer CreateRenderOutputBuffer(in RenderOutputBufferCreationConfig config);
}