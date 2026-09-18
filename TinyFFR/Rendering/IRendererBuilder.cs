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
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> with the given <paramref name="camera"/> in to the given <paramref name="buffer"/>.
	/// </summary>
	/// <param name="scene">The scene you'd like to render.</param>
	/// <param name="camera">The camera you'd like to render with.</param>
	/// <param name="buffer">The buffer you want to render the scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer(Scene scene, Camera camera, RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateRenderer<RenderOutputBuffer>(scene, camera, buffer, name);
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> with the given <paramref name="camera"/> in to the given <paramref name="renderTarget"/>.
	/// </summary>
	/// <remarks>
	/// Equivalent to calling the <see cref="RendererCreationConfig"/> overload with a config using the given <paramref name="name"/> and <see cref="RenderQualityConfig.SceneDefault"/> for every other setting.
	/// </remarks>
	/// <typeparam name="TRenderTarget">The type of render target; either <see cref="Window"/> or <see cref="RenderOutputBuffer"/>.</typeparam>
	/// <param name="scene">The scene you'd like to render.</param>
	/// <param name="camera">The camera you'd like to render with.</param>
	/// <param name="renderTarget">The window or buffer you want to present the rendered scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		return CreateRenderer(scene, camera, renderTarget, new RendererCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> with the given <paramref name="camera"/> in to the given <paramref name="renderTarget"/>.
	/// </summary>
	/// <typeparam name="TRenderTarget">The type of render target; either <see cref="Window"/> or <see cref="RenderOutputBuffer"/>.</typeparam>
	/// <param name="scene">The scene you'd like to render.</param>
	/// <param name="camera">The camera you'd like to render with.</param>
	/// <param name="renderTarget">The window or buffer you want to present the rendered scene in to.</param>
	/// <param name="config">Configuration for the new renderer, including its name and initial <see cref="RenderQualityConfig">render quality</see>.</param>
	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget>;

	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> in to the given <paramref name="window"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasScene"/> renderers are used for pure 2D/UI-style rendering (e.g. canvas objects) and don't need a <see cref="Camera"/>.
	/// </remarks>
	/// <param name="scene">The canvas scene you'd like to render.</param>
	/// <param name="window">The window you want to present the rendered scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer(CanvasScene scene, Window window, ReadOnlySpan<char> name = default) => CreateRenderer<Window>(scene, window, name);
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> in to the given <paramref name="buffer"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasScene"/> renderers are used for pure 2D/UI-style rendering (e.g. canvas objects) and don't need a <see cref="Camera"/>.
	/// </remarks>
	/// <param name="scene">The canvas scene you'd like to render.</param>
	/// <param name="buffer">The buffer you want to render the scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer(CanvasScene scene, RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateRenderer<RenderOutputBuffer>(scene, buffer, name);
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> in to the given <paramref name="renderTarget"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasScene"/> renderers are used for pure 2D/UI-style rendering (e.g. canvas objects) and don't need a <see cref="Camera"/>.
	/// </remarks>
	/// <typeparam name="TRenderTarget">The type of render target; either <see cref="Window"/> or <see cref="RenderOutputBuffer"/>.</typeparam>
	/// <param name="scene">The canvas scene you'd like to render.</param>
	/// <param name="renderTarget">The window or buffer you want to present the rendered scene in to.</param>
	/// <param name="name">Optional name for this renderer.</param>
	public Renderer CreateRenderer<TRenderTarget>(CanvasScene scene, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		return CreateRenderer(scene, renderTarget, new RendererCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a new <see cref="Renderer"/> that renders the given <paramref name="scene"/> in to the given <paramref name="renderTarget"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasScene"/> renderers are used for pure 2D/UI-style rendering (e.g. canvas objects) and don't need a <see cref="Camera"/>.
	/// </remarks>
	/// <typeparam name="TRenderTarget">The type of render target; either <see cref="Window"/> or <see cref="RenderOutputBuffer"/>.</typeparam>
	/// <param name="scene">The canvas scene you'd like to render.</param>
	/// <param name="renderTarget">The window or buffer you want to present the rendered scene in to.</param>
	/// <param name="config">Configuration for the new renderer, including its name and initial <see cref="RenderQualityConfig">render quality</see>.</param>
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
	/// <inheritdoc cref="CreateRenderOutputBuffer(XYPair{int}?,ReadOnlySpan{char})"/>
	/// <param name="config">Configuration for the new buffer, including its dimensions and name.</param>
	public RenderOutputBuffer CreateRenderOutputBuffer(in RenderOutputBufferCreationConfig config);

	/// <summary>
	/// Creates a new <see cref="RendererCompositor"/> that composites renderers in to the given <paramref name="window"/>.
	/// </summary>
	/// <param name="window">The window you want to target with composited rendering.</param>
	/// <param name="name">Optional name for this compositor.</param>
	public RendererCompositor CreateCompositor(Window window, ReadOnlySpan<char> name = default) => CreateCompositor<Window>(window, name);
	/// <summary>
	/// Creates a new <see cref="RendererCompositor"/> that composites renderers in to the given <paramref name="buffer"/>.
	/// </summary>
	/// <param name="buffer">The buffer you want to target with composited rendering.</param>
	/// <param name="name">Optional name for this compositor.</param>
	public RendererCompositor CreateCompositor(RenderOutputBuffer buffer, ReadOnlySpan<char> name = default) => CreateCompositor<RenderOutputBuffer>(buffer, name);
	/// <summary>
	/// Creates a new <see cref="RendererCompositor"/> that composites renderers in to the given <paramref name="renderTarget"/>.
	/// </summary>
	/// <typeparam name="TRenderTarget">The type of render target; either <see cref="Window"/> or <see cref="RenderOutputBuffer"/>.</typeparam>
	/// <param name="renderTarget">The window or buffer you want to target with composited rendering.</param>
	/// <param name="name">Optional name for this compositor.</param>
	public RendererCompositor CreateCompositor<TRenderTarget>(TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget>;
}