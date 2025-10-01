// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering.Local;

public enum RenderingBackendApi {
	SystemRecommended = 0,
	OpenGl = 1
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

	internal RenderingBackendApi GetActualRenderingApi() => RenderingBackendApi.OpenGl; // TODO once we add support for more APIs this will return the recommended API if SystemRecommended
}