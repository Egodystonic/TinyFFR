// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering.Local;

public enum RenderingBackendApi {
	SystemRecommended = 0,
	OpenGl = 1,
	Vulkan = 2
}

public sealed record RendererBuilderConfig {
	readonly RenderingBackendApi _renderingApi = RenderingBackendApi.SystemRecommended;
	public RenderingBackendApi RenderingApi {
		get => _renderingApi;
		init {
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, null);
			_renderingApi = value;
		}
	}

	public bool EnableVSync { get; init; } = true;

	internal RenderingBackendApi GetActualRenderingApi() {
		if (RenderingApi != RenderingBackendApi.SystemRecommended) return RenderingApi;
		
		if (OperatingSystem.IsWindows()) return RenderingBackendApi.Vulkan;
		else if (OperatingSystem.IsLinux()) return RenderingBackendApi.Vulkan;
		else return RenderingBackendApi.OpenGl;
	}
}