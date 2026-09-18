// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Numerics;
using Egodystonic.TinyFFR.Factory;
using Hexa.NET.ImGui;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.DearImGui;

/// <summary>
/// Extension types specifically for integrating TinyFFR and Dear ImGui.
/// </summary>
public static class TinyFfrImGuiExtensions {
	/// <summary>
	/// Creates a scene that hosts a Dear ImGui interface.
	/// </summary>
	/// <remarks>
	/// Dispose the returned scene when it is no longer needed, after disposing any renderer that targets it.
	/// </remarks>
	/// <param name="this">The scene builder to create the underlying scene with.</param>
	/// <param name="factory">The factory whose builders and allocator the interface uses for its own resources.</param>
	public static ImGuiScene CreateImGuiScene(this ISceneBuilder @this, ITinyFfrFactory factory) {
		return CreateImGuiScene(@this, factory, new ImGuiSceneCreationConfig());
	}

	/// <summary>
	/// Creates a scene that hosts a Dear ImGui interface, using the given config.
	/// </summary>
	/// <remarks>
	/// Dispose the returned scene when it is no longer needed, after disposing any renderer that targets it.
	/// </remarks>
	/// <param name="this">The scene builder to create the underlying scene with.</param>
	/// <param name="factory">The factory whose builders and allocator the interface uses for its own resources.</param>
	/// <param name="config">Controls which ImGui features are enabled.</param>
	public static ImGuiScene CreateImGuiScene(this ISceneBuilder @this, ITinyFfrFactory factory, in ImGuiSceneCreationConfig config) {
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(factory);
		return new ImGuiScene(factory, in config);
	}

	/// <summary>
	/// Creates a renderer for an ImGui interface, already configured for drawing a user interface.
	/// </summary>
	/// <remarks>
	/// This saves supplying the scene, camera and quality settings yourself: the interface's own scene and camera are used, its
	/// aspect ratio is left alone (the camera manages it), and the quality configuration suited to flat interface content is
	/// applied.
	/// </remarks>
	/// <typeparam name="TRenderTarget">The kind of target being rendered to, such as a window or a render output buffer.</typeparam>
	/// <param name="this">The renderer builder to create the renderer with.</param>
	/// <param name="scene">The ImGui scene to render.</param>
	/// <param name="renderTarget">Where the interface should be drawn.</param>
	/// <param name="name">The name to give the renderer. May be left empty.</param>
	public static Renderer CreateRenderer<TRenderTarget>(this IRendererBuilder @this, ImGuiScene scene, TRenderTarget renderTarget, ReadOnlySpan<char> name = default) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(scene);
		return @this.CreateRenderer(
			scene.UnderlyingScene,
			scene.Camera,
			renderTarget,
			new RendererCreationConfig {
				AutoUpdateCameraAspectRatio = false,
				Quality = new(BuiltInQualityConfiguration.Canvas),
				Name = name
			}
		);
	}
}
