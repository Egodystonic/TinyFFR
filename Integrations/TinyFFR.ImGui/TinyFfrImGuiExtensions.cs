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

public static class TinyFfrImGuiExtensions {
	public static ImGuiScene CreateImGuiScene(this ISceneBuilder @this, ITinyFfrFactory factory) {
		return CreateImGuiScene(@this, factory, new ImGuiSceneCreationConfig());
	}

	public static ImGuiScene CreateImGuiScene(this ISceneBuilder @this, ITinyFfrFactory factory, in ImGuiSceneCreationConfig config) {
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(factory);
		return new ImGuiScene(factory, in config);
	}

	public static unsafe void Image(ImTextureID textureId, Vector2 size) {
		Hexa.NET.ImGui.ImGui.Image(new ImTextureRef(null, textureId), size);
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
