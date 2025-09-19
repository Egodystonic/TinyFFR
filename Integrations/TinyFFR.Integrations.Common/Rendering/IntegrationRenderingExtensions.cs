// Created on 2025-08-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

public static class IntegrationRenderingExtensions {
	public static Renderer CreateBindableRenderer(this IRendererBuilder @this, Scene scene, Camera camera, IResourceAllocator allocator, ReadOnlySpan<char> name = default) {
		return @this.CreateBindableRenderer(scene, camera, allocator, new BindableRendererCreationConfig { Name = name });
	}
	public static Renderer CreateBindableRenderer(this IRendererBuilder @this, Scene scene, Camera camera, IResourceAllocator allocator, in BindableRendererCreationConfig config) {
		var impl = new BindableRendererImplProvider(@this, allocator, scene, camera, in config);
		return impl.BindableRendererInstance;
	}
}