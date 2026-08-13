// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.DearImGui;

public static class TinyFfrImGuiExtensions {
	public static ImGuiScene CreateImGuiScene(this ISceneBuilder @this, ITinyFfrFactory factory) {
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(factory);
		return new ImGuiScene(factory);
	}

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
