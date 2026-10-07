// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Specifies which underlying graphics API TinyFFR should use to render, via <see cref="RendererBuilderConfig.RenderingApi"/>.
/// </summary>
/// <remarks>
/// Platform availability is limited: see the remarks on <see cref="RendererBuilderConfig.RenderingApi"/> for exactly which values are supported on which operating system.
/// </remarks>
public enum RenderingBackendApi {
	/// <summary>
	/// Lets TinyFFR choose the most appropriate API for the current operating system.
	/// </summary>
	SystemRecommended = 0,
	/// <summary>
	/// OpenGL.
	/// </summary>
	OpenGl = 1,
	/// <summary>
	/// Vulkan.
	/// </summary>
	Vulkan = 2,
	/// <summary>
	/// Apple's Metal API.
	/// </summary>
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

	/// <summary>
	/// Whether rendering to a minimized (or zero-sized) <see cref="TinyFFR.Environment.Local.Window">Window</see> should wait as though the frame had been shown at
	/// the window's <see cref="TinyFFR.Environment.Local.Display">Display</see>'s refresh rate. By default, EnableMinimizedWindowFramePacing is true.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Nothing is rendered to a window that is minimized or has no area, so <see cref="Renderer.Render"/> (and <see cref="RendererCompositor.RenderAll"/>) return
	/// immediately for such windows. Without anything else pacing it, a loop with no frame rate cap would then run as fast as it possibly can whilst the window
	/// is minimized, needlessly consuming CPU time.
	/// </para>
	/// <para>
	/// When this is enabled, each skipped render instead blocks the caller for whatever remains of one refresh interval of the window's display
	/// (see <see cref="TinyFFR.Environment.Local.Display.CurrentRefreshRateHz"/>), much as <see cref="EnableVSync">vsync</see> would. A loop that is already capped
	/// at or below the display's refresh rate is not slowed down any further. This applies whether or not <see cref="EnableVSync"/> is enabled.
	/// </para>
	/// <para>
	/// Disable this if your application loop must keep running at full speed whilst its window is minimized.
	/// </para>
	/// </remarks>
	public bool EnableMinimizedWindowFramePacing { get; init; } = true;

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