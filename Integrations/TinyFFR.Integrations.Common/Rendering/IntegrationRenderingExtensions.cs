// Created on 2025-08-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Creates the renderers and compositors that TinyFFR's user interface framework integrations bind their scene view controls to.
/// </summary>
public static class IntegrationRenderingExtensions {
	/// <summary>
	/// Creates a renderer that a user interface framework's scene view control can bind to.
	/// </summary>
	/// <remarks>
	/// A scene view control can only be bound to a renderer created this way; binding an ordinary renderer fails. Each call to the
	/// returned renderer's <c>Render</c> updates the control it is bound to.
	/// </remarks>
	/// <param name="this">The renderer builder to create the renderer with.</param>
	/// <param name="scene">The scene to render.</param>
	/// <param name="camera">The camera to render it from.</param>
	/// <param name="allocator">The allocator used for the renderer's internal frame buffers. The factory's own <c>ResourceAllocator</c> is the usual choice.</param>
	/// <param name="name">The name to give the renderer. May be left empty.</param>
	public static Renderer CreateBindableRenderer(this IRendererBuilder @this, Scene scene, Camera camera, IResourceAllocator allocator, ReadOnlySpan<char> name = default) {
		return @this.CreateBindableRenderer(scene, camera, allocator, new BindableRendererCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a renderer that a user interface framework's scene view control can bind to, using the given config.
	/// </summary>
	/// <remarks>
	/// A scene view control can only be bound to a renderer created this way; binding an ordinary renderer fails. Each call to the
	/// returned renderer's <c>Render</c> updates the control it is bound to.
	/// </remarks>
	/// <param name="this">The renderer builder to create the renderer with.</param>
	/// <param name="scene">The scene to render.</param>
	/// <param name="camera">The camera to render it from.</param>
	/// <param name="allocator">The allocator used for the renderer's internal frame buffers. The factory's own <c>ResourceAllocator</c> is the usual choice.</param>
	/// <param name="config">Controls how the renderer is created.</param>
	public static Renderer CreateBindableRenderer(this IRendererBuilder @this, Scene scene, Camera camera, IResourceAllocator allocator, in BindableRendererCreationConfig config) {
		var impl = new BindableRendererImplProvider(@this, allocator, scene, camera, in config);
		return impl.BindableRendererInstance;
	}

	/// <summary>
	/// Creates a compositor that a user interface framework's scene view control can bind to.
	/// </summary>
	/// <remarks>
	/// A scene view control can only be bound to a compositor created this way; binding an ordinary compositor fails. Each call to the
	/// returned compositor's <c>RenderAll</c> updates the control it is bound to.
	/// </remarks>
	/// <param name="this">The renderer builder to create the compositor with.</param>
	/// <param name="name">The name to give the compositor. May be left empty.</param>
	public static RendererCompositor CreateBindableCompositor(this IRendererBuilder @this, ReadOnlySpan<char> name = default) {
		return @this.CreateBindableCompositor(new BindableRendererCompositorCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a compositor that a user interface framework's scene view control can bind to, using the given config.
	/// </summary>
	/// <remarks>
	/// A scene view control can only be bound to a compositor created this way; binding an ordinary compositor fails. Each call to the
	/// returned compositor's <c>RenderAll</c> updates the control it is bound to.
	/// </remarks>
	/// <param name="this">The renderer builder to create the compositor with.</param>
	/// <param name="config">Controls how the compositor is created.</param>
	public static RendererCompositor CreateBindableCompositor(this IRendererBuilder @this, in BindableRendererCompositorCreationConfig config) {
		var impl = new BindableRendererCompositorImplProvider(@this, in config);
		return impl.BindableCompositorInstance;
	}
}