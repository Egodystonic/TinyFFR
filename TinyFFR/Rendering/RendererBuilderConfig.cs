// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering;

public enum RenderingBackendApi {
	SystemRecommended = 0,
	OpenGl = 1,
	Vulkan = 2,
	Metal = 3
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
		if (OperatingSystem.IsMacOS()) {
			if (RenderingApi is not (RenderingBackendApi.SystemRecommended or RenderingBackendApi.Metal)) {
				throw new InvalidOperationException($"Rendering API '{RenderingApi}' is not supported on MacOS; only '{nameof(RenderingBackendApi.Metal)}' is available.");
			}
			return RenderingBackendApi.Metal;
		}

		if (RenderingApi == RenderingBackendApi.Metal) {
			throw new InvalidOperationException($"Rendering API '{nameof(RenderingBackendApi.Metal)}' is only supported on MacOS.");
		}

		if (RenderingApi != RenderingBackendApi.SystemRecommended) return RenderingApi;
		
		return RenderingBackendApi.Vulkan;
	}
}