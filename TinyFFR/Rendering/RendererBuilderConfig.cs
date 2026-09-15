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

/// <summary>
/// Object used to configure a <see cref="Egodystonic.TinyFFR.Factory.Local.LocalTinyFfrFactory"/>'s <see cref="IRendererBuilder"/>.
/// </summary>
public sealed record RendererBuilderConfig {
	/// <summary>
	/// The rendering API to use. Defaults to <see cref="RenderingBackendApi.SystemRecommended"/>.
	/// </summary>
	/// <remarks>
	/// Only <see cref="RenderingBackendApi.Metal"/> is supported on MacOS.
	/// Only <see cref="RenderingBackendApi.OpenGl"/> and <see cref="RenderingBackendApi.Vulkan"/> are supported on Windows and Linux.
	/// </remarks>
	public RenderingBackendApi RenderingApi {
		get;
		init {
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, null);
			field = value;
		}
	} = RenderingBackendApi.SystemRecommended;

	/// <summary>
	/// "VSync", short for Vertical Synchronization, is a configuration setting that controls whether or not your rendered frames must wait for the monitor's refresh rate.
	/// By default, EnableVSync is true.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When VSync is enabled, each frame you render to a <see cref="TinyFFR.Environment.Local.Window">Window</see> will not actually be displayed until the parent <see cref="TinyFFR.Environment.Local.Display">Display</see>'s
	/// next screen update.  For most applications this is desirable for two reasons:
	/// <ul>
	/// <li>
	/// "Rendering" frames faster than the display can actually update is a waste of resources/energy. If your display has a 60Hz refresh rate but you're rendering 240 frames per second, 75% of those frames will never be seen.
	/// </li>
	/// <li>
	/// Updating the display's data buffer mid-refresh usually results in screen tearing. Keeping VSync enabled eliminates this problem.
	/// </li>
	/// </ul>
	/// </para>
	/// <para>
	/// Because VSync blocks the renderer until the monitor cycles, it can reduce throughput in your application.
	/// This also means your application loop's maximum frequency will be capped by the target monitor's refresh rate.
	/// Relatedly, VSync introduces additional delay between a frame being rendered and it actually being displayed (e.g. "frames" must wait for the next display update refresh cycle).
	/// In applications that demand minimal input latency, this can be problematic.
	/// </para>
	/// <para>
	/// If VSync is disabled, TinyFFR will write each rendered frame to the display's pixel buffer as soon as it's ready, with no delay.
	/// This will introduce screen tearing, but reduce input latency and increase throughput.
	/// </para>
	/// </remarks>
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