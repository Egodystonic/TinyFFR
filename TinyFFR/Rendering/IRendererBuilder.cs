// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Builder interface that allows you create <see cref="Renderer"/>s, <see cref="RenderOutputBuffer"/>s, and <see cref="RendererCompositor"/>s.
/// </summary>
public interface IRendererBuilder {
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> with the given <paramref name="camera"/> in to the given <paramref name="window"/>.
	/// </summary>
	/// <param name="scene">The scene you'd like to render.</param>
	/// <param name="camera">The camera you'd like to render with.</param>
	/// <param name="window">The window you want to present the rendered scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer(Scene scene, Camera camera, Window window, ReadOnlySpan<char> name = default) => CreateRenderer<Window>(scene, camera, window, name);
	public Renderer CreateRenderer(Scene scene, Camera camera, RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateRenderer<RenderOutputBuffer>(scene, camera, buffer, name);
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		return CreateRenderer(scene, camera, renderTarget, new RendererCreationConfig { Name = name, Quality = RenderQualityConfig.Default });
	}
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget>;

	public Renderer CreateRenderer(CanvasScene scene, Window window, ReadOnlySpan<char> name = default) => CreateRenderer<Window>(scene, window, name);
	public Renderer CreateRenderer(CanvasScene scene, RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateRenderer<RenderOutputBuffer>(scene, buffer, name);
	public Renderer CreateRenderer<TRenderTarget>(CanvasScene scene, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		return CreateRenderer(scene, renderTarget, new RendererCreationConfig { Quality = new(BuiltInQualityConfiguration.Canvas), Name = name });
	}
	public Renderer CreateRenderer<TRenderTarget>(CanvasScene scene, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget>;

	/// <summary>
	/// Creates a new <see cref="RenderOutputBuffer"/> you can render scenes in to.
	/// </summary>
	/// <param name="textureDimensions">The dimensions of the buffer (<c>X</c> = width, <c>Y</c> = height).
	/// If <c>null</c> a default value will be used (<see cref="RenderOutputBufferCreationConfig.DefaultTextureDimensions"/>).</param>
	/// <param name="name">Optional name for this buffer.</param>
	public RenderOutputBuffer CreateRenderOutputBuffer(XYPair<int>? textureDimensions = null, ReadOnlySpan<char> name = default) {
		return CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig {
			TextureDimensions = textureDimensions ?? RenderOutputBufferCreationConfig.DefaultTextureDimensions,
			Name = name
		});
	}
	public RenderOutputBuffer CreateRenderOutputBuffer(in RenderOutputBufferCreationConfig config);

	/// <summary>
	/// Creates a new <see cref="RendererCompositor"/> that composites renderers in to the given <paramref name="window"/>.
	/// </summary>
	/// <param name="window">The window you want to target with composited rendering.</param>
	/// <param name="name">Optional name for this compositor.</param>
	public RendererCompositor CreateCompositor(Window window, ReadOnlySpan<char> name = default) => CreateCompositor<Window>(window, name);
	public RendererCompositor CreateCompositor(RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateCompositor<RenderOutputBuffer>(buffer, name);
	public RendererCompositor CreateCompositor<TRenderTarget>(TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget>;
}