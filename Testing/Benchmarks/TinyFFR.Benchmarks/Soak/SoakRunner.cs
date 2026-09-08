// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Soak;

readonly record struct SoakSample(TimeSpan Elapsed, long Frames, long AllocatedBytes, int Gen0, int Gen1, int Gen2, long HeapBytes);

static class SoakRunner {
	public static void Run(double durationMinutes) {
		var duration = TimeSpan.FromMinutes(durationMinutes > 0d ? durationMinutes : SoakWorkload.DefaultDurationMinutes);

		Console.WriteLine($"Soak: steady-state render loop for {duration.TotalMinutes:N1} minutes, sampling every {SoakWorkload.SampleIntervalSeconds:N0}s.");
		Console.WriteLine("No GC is ever forced, so ArrayPool trimming behaves as it would in a real application.");
		Console.WriteLine("The question being answered: does allocation per frame climb over time, or stay flat?");
		Console.WriteLine();

		BenchmarkEnvironment.RetainAcrossBenchmarks = true;
		BenchmarkEnvironment.Initialize(BenchmarkRenderTargetKind.OutputBuffer);

		var samples = new List<SoakSample>();
		try {
			RunLoop(duration, samples);
		}
		finally {
			BenchmarkEnvironment.TearDown(force: true);
		}

		Report(samples);
	}

	static void RunLoop(TimeSpan duration, List<SoakSample> samples) {
		var factory = BenchmarkEnvironment.Factory;
		var target = BenchmarkEnvironment.RenderTarget;
		var loop = BenchmarkEnvironment.Loop;

		using var cuboidMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Soak Cuboid Mesh");
		using var sphereMesh = factory.MeshBuilder.CreateMesh(new Sphere(0.5f), subdivisionLevel: SoakWorkload.SphereSubdivisionLevel, name: "Soak Sphere Mesh");
		using var colorMap = factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Soak Color Map");
		using var normalMap = factory.TextureBuilder.CreateNormalMap(BenchmarkAssets.NormalPattern, "Soak Normal Map");
		using var ormMap = factory.TextureBuilder.CreateOcclusionRoughnessMetallicMap(
			BenchmarkAssets.OcclusionPattern, BenchmarkAssets.RoughnessPattern, BenchmarkAssets.MetallicPattern, "Soak ORM Map"
		);
		using var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap, normalMap, ormMap, name: "Soak Material");
		using var scene = factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Aqua, name: "Soak Scene");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -12f), Direction.Forward, name: "Soak Camera");
		using var renderer = target.CreateRenderer(scene, camera);

		var instances = factory.ResourceAllocator.GetSharedScratchList<ModelInstance>();
		var lights = factory.ResourceAllocator.GetSharedScratchList<PointLight>();
		var queryResults = factory.ResourceAllocator.CreatePooledMemoryBuffer<ModelInstance>(SoakWorkload.QueryResultCapacity);

		for (var i = 0; i < SoakWorkload.InstanceCount; ++i) {
			var instance = factory.ObjectBuilder.CreateModelInstance(
				(i & 1) == 0 ? cuboidMesh : sphereMesh,
				material,
				new Location(((i % 8) - 4) * 1.5f, ((i / 8) - 4) * 1.5f, 0f),
				name: "Soak Instance"
			);
			scene.Add(instance);
			instances.Add(instance);
		}

		for (var i = 0; i < SoakWorkload.LightCount; ++i) {
			var light = factory.LightBuilder.CreatePointLight(new Location((i - 4) * 2f, 2f, -4f), StandardColor.White, name: "Soak Light");
			scene.Add(light);
			lights.Add(light);
		}

		try {
			var timer = Stopwatch.StartNew();
			var sampleInterval = TimeSpan.FromSeconds(SoakWorkload.SampleIntervalSeconds);
			var nextSample = sampleInterval;

			var frames = 0L;
			var lastFrames = 0L;
			var lastAllocated = GC.GetTotalAllocatedBytes(precise: true);
			var lastGen0 = GC.CollectionCount(0);
			var lastGen1 = GC.CollectionCount(1);
			var lastGen2 = GC.CollectionCount(2);

			while (timer.Elapsed < duration) {
				Frame(factory, scene, renderer, loop, instances, lights, queryResults.Span, cuboidMesh, material, frames);
				++frames;

				if (timer.Elapsed < nextSample) continue;

				var allocated = GC.GetTotalAllocatedBytes(precise: true);
				var gen0 = GC.CollectionCount(0);
				var gen1 = GC.CollectionCount(1);
				var gen2 = GC.CollectionCount(2);

				samples.Add(new SoakSample(
					timer.Elapsed,
					frames - lastFrames,
					allocated - lastAllocated,
					gen0 - lastGen0,
					gen1 - lastGen1,
					gen2 - lastGen2,
					GC.GetTotalMemory(forceFullCollection: false)
				));

				lastFrames = frames;
				lastAllocated = allocated;
				lastGen0 = gen0;
				lastGen1 = gen1;
				lastGen2 = gen2;
				nextSample += sampleInterval;
			}
		}
		finally {
			factory.ResourceAllocator.ReturnPooledMemoryBuffer(queryResults);
			scene.RemoveAll();
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
			for (var i = 0; i < lights.Count; ++i) lights[i].Dispose();
		}
	}

	static void Frame(
		ILocalTinyFfrFactory factory,
		Scene scene,
		Renderer renderer,
		ApplicationLoop loop,
		IList<ModelInstance> instances,
		IList<PointLight> lights,
		Span<ModelInstance> queryResults,
		Mesh churnMesh,
		Material churnMaterial,
		long frameIndex
	) {
		_ = loop.IterateOnce();

		for (var i = 0; i < instances.Count; ++i) instances[i].RotateBy(1.5f % Direction.Up);
		for (var i = 0; i < lights.Count; ++i) {
			var light = lights[i];
			light.Color = light.Color.WithHueAdjustedBy(1f);
		}

		for (var i = 0; i < SoakWorkload.QueriesPerFrame; ++i) {
			var origin = new Location(-10f + i, 0f, 0f);
			_ = scene.QueryProvider.FindIntersections(new Ray(origin, Direction.Right), queryResults);
			_ = scene.QueryProvider.FindIntersections(new PositionedSphere(2f, origin), queryResults);
		}

		if (frameIndex % SoakWorkload.TransientChurnFrameInterval == 0L) {
			for (var i = 0; i < SoakWorkload.TransientChurnCount; ++i) {
				using var transient = factory.ObjectBuilder.CreateModelInstance(churnMesh, churnMaterial, Location.Origin, name: "Soak Transient Instance");
				scene.Add(transient);
				scene.Remove(transient);
			}
		}

		renderer.RenderAndWaitForGpu();
	}

	static void Report(List<SoakSample> samples) {
		if (samples.Count == 0) {
			Console.WriteLine("No samples were collected.");
			return;
		}

		Console.WriteLine($"{"elapsed",9} {"frames",9} {"bytes/frame",13} {"gen0",6} {"gen1",6} {"gen2",6} {"heap MB",9}");
		foreach (var sample in samples) {
			var perFrame = sample.Frames > 0L ? sample.AllocatedBytes / (double) sample.Frames : 0d;
			Console.WriteLine(
				$"{sample.Elapsed.TotalSeconds,8:N0}s {sample.Frames,9:N0} {perFrame,13:N1} " +
				$"{sample.Gen0,6:N0} {sample.Gen1,6:N0} {sample.Gen2,6:N0} {sample.HeapBytes / (1024d * 1024d),9:N1}"
			);
		}

		var quarter = Int32.Max(1, samples.Count / 4);
		var first = AveragePerFrame(samples, 0, quarter);
		var last = AveragePerFrame(samples, samples.Count - quarter, quarter);

		Console.WriteLine();
		Console.WriteLine($"First quarter: {first:N1} bytes/frame.   Last quarter: {last:N1} bytes/frame.");
		if (last < SteadyStateBytesPerFrameThreshold) {
			Console.WriteLine($"Verdict: steady state allocates {last:N1} bytes/frame - at or below the {SteadyStateBytesPerFrameThreshold:N0} byte threshold, so the loop is allocation-free.");
			return;
		}

		if (first <= 0d) {
			Console.WriteLine("Verdict: no allocation measured in the first quarter, so no trend can be computed.");
			return;
		}

		var changePercent = 100d * (last - first) / first;
		Console.WriteLine($"Verdict: allocation per frame changed by {changePercent:+0.0;-0.0;0.0}% over the run.");
		Console.WriteLine(
			Math.Abs(changePercent) < 10d
				? "Flat within noise - ArrayPool trimming is not causing runaway allocation in a steady-state loop."
				: "NOT flat - worth investigating whether pool trimming is dropping hot buffers."
		);
	}

	const double SteadyStateBytesPerFrameThreshold = 1d;

	static double AveragePerFrame(List<SoakSample> samples, int start, int count) {
		var bytes = 0L;
		var frames = 0L;
		for (var i = start; i < start + count && i < samples.Count; ++i) {
			bytes += samples[i].AllocatedBytes;
			frames += samples[i].Frames;
		}
		return frames > 0L ? bytes / (double) frames : 0d;
	}
}
