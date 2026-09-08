// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

enum BenchmarkRenderTargetKind {
	OutputBuffer,
	Window
}

sealed class BenchmarkRenderTarget : IDisposable {
	public static readonly XYPair<int> DefaultDimensions = (480, 270);
	const string WindowTitle = "TinyFFR Benchmark Inspection";

	readonly ILocalTinyFfrFactory _factory;
	readonly Window _window;
	readonly RenderOutputBuffer _buffer;
	bool _isDisposed = false;

	public BenchmarkRenderTargetKind Kind { get; }
	public Window? Window => Kind == BenchmarkRenderTargetKind.Window ? _window : null;

	public BenchmarkRenderTarget(ILocalTinyFfrFactory factory, BenchmarkRenderTargetKind kind) {
		_factory = factory;
		Kind = kind;

		if (kind == BenchmarkRenderTargetKind.Window) {
			var display = factory.DisplayDiscoverer.Primary
				?? throw new InvalidOperationException("Can not run in inspection mode: no display was discovered.");
			_window = factory.WindowBuilder.CreateWindow(display, size: DefaultDimensions * 3, title: WindowTitle);
		}
		else {
			_buffer = factory.RendererBuilder.CreateRenderOutputBuffer(DefaultDimensions, "Benchmark Render Output Buffer");
		}
	}

	public Renderer CreateRenderer(Scene scene, Camera camera, in RendererCreationConfig config) {
		return Kind == BenchmarkRenderTargetKind.Window
			? _factory.RendererBuilder.CreateRenderer(scene, camera, _window, in config)
			: _factory.RendererBuilder.CreateRenderer(scene, camera, _buffer, in config);
	}

	public Renderer CreateRenderer(Scene scene, Camera camera) {
		return CreateRenderer(scene, camera, new RendererCreationConfig { Quality = RenderQualityConfig.Default });
	}

	public Renderer CreateRenderer(CanvasScene scene, in RendererCreationConfig config) {
		return Kind == BenchmarkRenderTargetKind.Window
			? _factory.RendererBuilder.CreateRenderer(scene, _window, in config)
			: _factory.RendererBuilder.CreateRenderer(scene, _buffer, in config);
	}

	public Renderer CreateRenderer(CanvasScene scene) {
		return CreateRenderer(scene, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Canvas) });
	}

	public RendererCompositor CreateCompositor(ReadOnlySpan<char> name) {
		return Kind == BenchmarkRenderTargetKind.Window
			? _factory.RendererBuilder.CreateCompositor(_window, name)
			: _factory.RendererBuilder.CreateCompositor(_buffer, name);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			if (Kind == BenchmarkRenderTargetKind.Window) _window.Dispose();
			else _buffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}
