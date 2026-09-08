// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	static readonly PrimitivePaintbrush QueryPaintbrush = new(new ColorVect(0.1f, 0.9f, 0.4f, 0.6f));

	public static void Lights() {
		using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Light Scene");
		var pointLights = Allocator.GetSharedScratchList<PointLight>();
		var spotLights = Allocator.GetSharedScratchList<SpotLight>();
		var directionalLights = Allocator.GetSharedScratchList<DirectionalLight>();

		try {
			for (var i = 0; i < SmokeWorkload.LightCount; ++i) {
				var offset = i * 0.05f;

				var point = Factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Up * offset, StandardColor.Red, name: "Benchmark Point Light");
				var spot = Factory.LightBuilder.CreateSpotLight(Location.Origin + Direction.Up * offset, Direction.Forward, name: "Benchmark Spot Light");
				var directional = Factory.LightBuilder.CreateDirectionalLight(new Direction(0f, -1f, -0.3f), showSunDisc: (i & 1) == 0, name: "Benchmark Directional Light");

				scene.Add(point);
				scene.Add(spot);
				scene.Add(directional);

				pointLights.Add(point);
				spotLights.Add(spot);
				directionalLights.Add(directional);
			}

			for (var pass = 0; pass < SmokeWorkload.LightPassCount; ++pass)
			for (var i = 0; i < pointLights.Count; ++i) {
				var point = pointLights[i];
				var spot = spotLights[i];
				var directional = directionalLights[i];
				point.Color = point.Color.WithHueAdjustedBy(1f);
				point.Position = Location.Origin + Direction.Up * (i * 0.02f);
				spot.ConeAngle = 30f + (i % 30);
				directional.Direction = new Direction(0.2f, -1f, -0.2f);
			}

			var containedLightCount = 0;
			foreach (var light in scene.ContainedLights) {
				if (light != default) ++containedLightCount;
			}
		}
		finally {
			scene.RemoveAll();
			for (var i = 0; i < pointLights.Count; ++i) {
				pointLights[i].Dispose();
				spotLights[i].Dispose();
				directionalLights[i].Dispose();
			}
		}
	}

	public static void ShadowsAndFog() {
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Aqua, name: "Benchmark Shadow Scene");
		var lights = Allocator.GetSharedScratchList<Light>();

		try {
			for (var i = 0; i < SmokeWorkload.ShadowLightCount; ++i) {
				var directional = Factory.LightBuilder.CreateDirectionalLight(castsShadows: true, name: "Benchmark Shadow Directional Light");
				var point = Factory.LightBuilder.CreatePointLight(Location.Origin + Direction.Up * (2f + i), castsShadows: true, name: "Benchmark Shadow Point Light");
				var spot = Factory.LightBuilder.CreateSpotLight(Location.Origin + Direction.Up * (2f + i), Direction.Down, castsShadows: true, highQuality: true, name: "Benchmark Shadow Spot Light");

				scene.Add(directional);
				scene.Add(point);
				scene.Add(spot);

				lights.Add(directional);
				lights.Add(point);
				lights.Add(spot);

				scene.AddFog(FogDensity.Moderate);
				scene.AddFog(FogDensity.Thick, StandardColor.White);
				scene.AddFog(new FogDescriptor { Color = StandardColor.White, DensityMultiplier = 0.5f, StartDistance = 2f });
				scene.RemoveFog();
			}

			using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Shadow Mesh");
			using var material = Factory.MaterialBuilder.CreateTestMaterial();
			using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Shadow Camera");
			var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Shadow Instance");

			try {
				scene.Add(instance);
				scene.AddFog(FogDensity.Moderate);

				using var renderer = Target.CreateRenderer(scene, camera);
				for (var frame = 0; frame < SmokeWorkload.ShadowFrameCount; ++frame) {
					instance.RotateBy(2f % Direction.Up);
					renderer.RenderAndWaitForGpu();
				}
			}
			finally {
				scene.Remove(instance);
				instance.Dispose();
			}
		}
		finally {
			scene.RemoveAll();
			for (var i = 0; i < lights.Count; ++i) lights[i].Dispose();
		}
	}

	public static void Backdrops() {
		for (var repeat = 0; repeat < SmokeWorkload.BackdropRepeatCount; ++repeat) ApplyOneBackdropSet();
	}

	static void ApplyOneBackdropSet() {
		using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Backdrop Scene");

		scene.SetBackdrop(StandardColor.Black);
		scene.SetBackdropWithoutIndirectLighting(StandardColor.White);
		scene.SetBackdrop(BuiltInSceneBackdrop.Clouds);

		using var backdropTexture = Factory.AssetLoader.LoadPreprocessedBackdropTexture(BenchmarkAssets.MetroSkyKtx, BenchmarkAssets.MetroIblKtx, "Benchmark Backdrop Texture");
		scene.SetBackdrop(backdropTexture, 0.8f, 45f % Direction.Up);
		scene.RemoveBackdrop();
	}

	public static void ModelInstances() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Instance Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Instance Scene");
		using var model = Factory.AssetLoader.CreateModel(mesh, material, "Benchmark Model");
		var instances = Allocator.GetSharedScratchList<ModelInstance>();

		try {
			for (var i = 0; i < SmokeWorkload.ModelInstanceCount; ++i) {
				var instance = (i & 1) == 0
					? Factory.ObjectBuilder.CreateModelInstance(mesh, material, new Location(i * 0.1f, 0f, 0f), name: "Benchmark Instance")
					: Factory.ObjectBuilder.CreateModelInstance(model, name: "Benchmark Model Instance");
				scene.Add(instance);
				instances.Add(instance);
			}

			for (var pass = 0; pass < SmokeWorkload.ModelInstancePassCount; ++pass)
			for (var i = 0; i < instances.Count; ++i) {
				var instance = instances[i];
				instance.SetPosition(Location.Origin + Direction.Forward * (i * 0.05f));
				instance.RotateBy(15f % Direction.Up);
				instance.SetScaling(Vect.One * 1.5f);
				instance.SetTransform(new Transform(Direction.Right * i, 30f % Direction.Up, Vect.One * 0.5f));

				_ = instance.GetWorldSpaceBoundingBox();
				_ = instance.GetWorldSpaceAxisAlignedBoundingBox();
				_ = instance.GetWorldSpaceBoundingSphere();
				_ = instance.GetModelSpaceBoundingBox();
			}

			var groupBuffer = Allocator.CreatePooledMemoryBuffer<ModelInstance>(2);
			try {
				groupBuffer.Span[0] = instances[0];
				groupBuffer.Span[1] = instances[1];
				using var group = Factory.ObjectBuilder.GroupModelInstances(groupBuffer.Span, disposingGroupDisposesInstances: false, "Benchmark Instance Group");
				group.SetPosition(Location.Origin + Direction.Up * 1f);
			}
			finally {
				Allocator.ReturnPooledMemoryBuffer(groupBuffer);
			}
		}
		finally {
			scene.RemoveAll();
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}
	}

	public static void ScenePrimitives() {
		using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Primitive Scene");

		for (var repeat = 0; repeat < SmokeWorkload.PrimitiveRepeatCount; ++repeat) {
			using var point = scene.AddPrimitivePoint(Location.Origin, in QueryPaintbrush, 0.05f, constantScreenSize: false);
			using var cuboid = scene.AddPrimitiveShape(new PositionedCuboid(1f, 1f, 1f, Location.Origin).WithRotation(Rotation.None), in QueryPaintbrush, wireframe: true);
			using var sphere = scene.AddPrimitiveShape(new PositionedSphere(0.5f, Location.Origin + Direction.Right * 1f), in QueryPaintbrush);
			using var boundedRay = scene.AddPrimitiveShape(new BoundedRay(Location.Origin, Direction.Forward * 2f));
			using var line = scene.AddPrimitiveShape(new Line(Location.Origin, Direction.Up));
			using var plane = scene.AddPrimitiveShape(new Plane(Direction.Up, Location.Origin));
			using var freeform = scene.AddPrimitive();

			freeform.SetPaintbrush(in QueryPaintbrush);
			freeform.SetGeometryPoint(Location.Origin + Direction.Left * 1f, 0.02f, constantScreenSize: true);
			freeform.SetGeometryGrid(Location.Origin, Direction.Up, Direction.Right, 4f, 1f, 0.25f);
		}
	}

	public static void SceneQueries() {
		using var cuboidMesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Query Cuboid Mesh");
		using var sphereMesh = Factory.MeshBuilder.CreateMesh(new Sphere(0.5f), subdivisionLevel: 2, name: "Benchmark Query Sphere Mesh");
		using var scene = Factory.SceneBuilder.CreateScene(name: "Benchmark Query Scene");
		var instances = Allocator.GetSharedScratchList<ModelInstance>();
		var results = Allocator.CreatePooledMemoryBuffer<ModelInstance>(SmokeWorkload.QueryResultCapacity);

		for (var i = 0; i < SmokeWorkload.QueryInstanceCount; ++i) {
			var instance = Factory.ObjectBuilder.CreateModelInstance(
				(i & 1) == 0 ? cuboidMesh : sphereMesh,
				initialPosition: new Location((i % 32) * 1.5f, ((i / 32) % 8) * 1.5f, (i / 256) * 1.5f),
				name: "Benchmark Query Instance"
			);
			scene.Add(instance);
			instances.Add(instance);
		}

		try {
			var resultsSpan = results.Span;
			for (var repeat = 0; repeat < SmokeWorkload.QueryRepeatCount; ++repeat) {
				var origin = Location.Origin + Direction.Left * (5f + repeat);
				_ = scene.QueryProvider.FindIntersections(new Ray(origin, Direction.Right), resultsSpan);
				_ = scene.QueryProvider.FindIntersections(new BoundedRay(origin, Direction.Right * 40f), resultsSpan, rayThickness: 0.1f);
				_ = scene.QueryProvider.FindIntersections(new PositionedSphere(3f, origin), resultsSpan);
				_ = scene.QueryProvider.FindIntersections(new PositionedCuboid(4f, 4f, 4f, origin), resultsSpan);
				_ = scene.QueryProvider.FindIntersections(new PositionedRotatedCuboid(new Cuboid(4f, 2f, 2f), origin, 30f % Direction.Up), resultsSpan);
			}
		}
		finally {
			Allocator.ReturnPooledMemoryBuffer(results);
			scene.RemoveAll();
			for (var i = 0; i < instances.Count; ++i) instances[i].Dispose();
		}
	}

	public static void CameraAndControllers() {
		var cameras = Allocator.GetSharedScratchList<Camera>();

		try {
			for (var i = 0; i < SmokeWorkload.CameraCount; ++i) {
				cameras.Add(Factory.CameraBuilder.CreateCamera(Location.Origin, Direction.Forward, name: "Benchmark Camera"));
			}

			for (var i = 0; i < cameras.Count; ++i) {
				var camera = cameras[i];
				camera.Position = Location.Origin + Direction.Up * 1f;
				camera.SetViewAndUpDirection(Direction.Forward, Direction.Up);
				camera.HorizontalFieldOfView = 75f;
				camera.SetExposure(16f, 1f / 125f, 100f);
				camera.NearPlaneDistance = 0.05f;
				camera.FarPlaneDistance = 500f;
			}

			using var freeFlying = cameras[0].CreateController<FreeFlyingCameraController>();
			using var inspector = cameras[1].CreateController<InspectorCameraController>();
			freeFlying.Position = cameras[0].Position;
			freeFlying.Pitch = 15f;

			for (var step = 0; step < SmokeWorkload.CameraStepCount; ++step) {
				freeFlying.Progress(0.016f);
				inspector.Progress(0.016f);
			}
		}
		finally {
			for (var i = 0; i < cameras.Count; ++i) cameras[i].Dispose();
		}
	}
}
