// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static class BenchmarkEnvironment {
	const int PumpFrameCount = 6;

	static ILocalTinyFfrFactory? _factory;
	static BenchmarkRenderTarget? _renderTarget;
	static ApplicationLoop? _loop;
	static Scene? _pumpScene;
	static Camera? _pumpCamera;
	static Renderer? _pumpRenderer;

	public static bool IsInitialized => _factory != null;
	public static bool RetainAcrossBenchmarks { get; set; } = false;

	public static ILocalTinyFfrFactory Factory => _factory ?? throw NotInitialized();
	public static BenchmarkRenderTarget RenderTarget => _renderTarget ?? throw NotInitialized();
	public static ApplicationLoop Loop => _loop ?? throw NotInitialized();
	public static IResourceAllocator Allocator => Factory.ResourceAllocator;

	public static void Initialize(BenchmarkRenderTargetKind kind) {
		if (_factory != null) return;

		BenchmarkAssets.Initialize();
		_factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		_renderTarget = new BenchmarkRenderTarget(_factory, kind);
		_loop = _factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: null, name: "Benchmark Loop");
		_pumpScene = _factory.SceneBuilder.CreateScene(name: "Benchmark Reclamation Pump Scene");
		_pumpCamera = _factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Reclamation Pump Camera");
		_pumpRenderer = _renderTarget.CreateRenderer(_pumpScene.Value, _pumpCamera.Value, new RendererCreationConfig {
			Name = "Benchmark Reclamation Pump Renderer",
			Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Lowest)
		});
	}

	public static void PumpGpuResourceReclamation() {
		if (_pumpRenderer is not { } renderer) return;
		for (var i = 0; i < PumpFrameCount; ++i) renderer.RenderAndWaitForGpu();
	}

	public static void TearDown(bool force = false) {
		if (RetainAcrossBenchmarks && !force) return;
		if (_factory == null) return;

		try {
			try {
				_pumpRenderer?.Dispose();
				_pumpCamera?.Dispose();
				_pumpScene?.Dispose();
				_loop?.Dispose();
			}
			finally {
				try {
					_renderTarget?.Dispose();
				}
				finally {
					_factory.Dispose();
				}
			}
		}
		finally {
			_pumpRenderer = null;
			_pumpCamera = null;
			_pumpScene = null;
			_loop = null;
			_renderTarget = null;
			_factory = null;
		}
	}

	static InvalidOperationException NotInitialized() => new($"{nameof(BenchmarkEnvironment)} has not been initialized.");
}
