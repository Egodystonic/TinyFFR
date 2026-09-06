// Created on 2026-09-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalSceneQueryingTest {
	const int GridWidth = 5;
	const int GridHeight = 3;
	const int GridDepth = 5;
	const float GridSpacing = 1f;
	const int MaxResultCount = 8;
	const float ShapeQueryDistance = 1.5f;
	const float BoundedRayLength = 2.25f;
	const float ShapeDragSharpness = 6f;
	const float RayThicknessStep = 0.05f;
	const float MinRayVisualThickness = 0.025f;
	const float RayForwardOffset = 0.4f;
	const float RayDownwardOffset = 0.2f;
	const float BoundingBoxAlpha = 0.2f;

	static readonly ColorVect UnhitColor = new(0.35f, 0.37f, 0.45f);
	static readonly ColorVect NearestHitColor = new(1f, 0.08f, 0.08f);
	static readonly ColorVect FarthestHitColor = new(0.08f, 0.3f, 1f);
	static readonly PrimitivePaintbrush QueryShapePaintbrush = new(new ColorVect(0.1f, 0.9f, 0.4f, 0.6f));
	static readonly InterpolationAlgorithm<Location> ShapeDragAlgorithm = InterpolationAlgorithm<Location>.Linear();
	static readonly InterpolationAlgorithm<ColorVect> HitOrderGradient = InterpolationAlgorithm<ColorVect>.Linear();

	enum QueryMode {
		Ray,
		BoundedRay,
		Sphere,
		AxisAlignedCuboid,
		RotatedCuboid
	}

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Scene Querying");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -15f), initialViewDirection: Direction.Forward);
		using var cameraController = camera.CreateController<FreeFlyingCameraController>();
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: false);
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		scene.Add(sunlight);

		using var cuboidMesh = factory.MeshBuilder.CreateMesh(
			new Cuboid(0.8f),
			centreTextureOrigin: false,
			new MeshGenerationConfig { TextureTransform = Transform2D.None },
			new MeshCreationConfig { Name = "Query Test Cuboid", GenerateWireframeData = true }
		);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(
			new Sphere(0.45f),
			subdivisionLevel: 4,
			new MeshGenerationConfig { TextureTransform = Transform2D.None },
			new MeshCreationConfig { Name = "Query Test Sphere", GenerateWireframeData = true }
		);

		var instances = new List<ModelInstance>();
		for (var x = 0; x < GridWidth; ++x) {
			for (var y = 0; y < GridHeight; ++y) {
				for (var z = 0; z < GridDepth; ++z) {
					var index = (x * GridHeight * GridDepth) + (y * GridDepth) + z;
					var instance = factory.ObjectBuilder.CreateModelInstance(
						(index & 0b1) == 0 ? cuboidMesh : sphereMesh,
						initialPosition: new Location(
							(x - (GridWidth - 1) * 0.5f) * GridSpacing,
							(y - (GridHeight - 1) * 0.5f) * GridSpacing,
							(z - (GridDepth - 1) * 0.5f) * GridSpacing
						),
						initialRotation: Rotation.Random(),
						initialScaling: Vect.Random(Vect.One * 0.3f, Vect.One * 0.9f),
						name: "Query Test Instance " + index
					);
					instance.SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle.Wireframe);
					scene.Add(instance);
					instances.Add(instance);
				}
			}
		}

		var boundingBoxPrimitives = new List<ScenePrimitive>();
		foreach (var instance in instances) {
			boundingBoxPrimitives.Add(scene.AddPrimitiveShape(instance.GetWorldSpaceBoundingBox(), new PrimitivePaintbrush(UnhitColor with { Alpha = BoundingBoxAlpha })));
		}
		using var queryShapePrimitive = scene.AddPrimitive();
		queryShapePrimitive.SetPaintbrush(in QueryShapePaintbrush);

		var results = new ModelInstance[MaxResultCount];
		var queryMode = QueryMode.Ray;
		var resultCap = 1;
		var rotatedCuboidAngle = Angle.Zero;
		var frozen = false;
		var rayThickness = 0f;

		Ray BuildQueryRay() => new(
			camera.Position + camera.ViewDirection * RayForwardOffset + camera.UpDirection * -RayDownwardOffset,
			camera.ViewDirection
		);

		var frozenRay = BuildQueryRay();
		var draggedShapeCentre = BuildQueryRay().StartPoint + BuildQueryRay().Direction * ShapeQueryDistance;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = loop.IterateOnce().AsDeltaTime();

			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				queryMode = queryMode == QueryMode.RotatedCuboid ? QueryMode.Ray : queryMode + 1;
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.F)) {
				frozen = !frozen;
				if (frozen) frozenRay = BuildQueryRay();
			}
			for (var i = 1; i <= MaxResultCount; ++i) {
				if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0 + i)) resultCap = i;
			}

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, cameraController, deltaTime, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, cameraController, deltaTime);
			DefaultCameraInputHandler.Progress(cameraController, deltaTime);

			rotatedCuboidAngle += deltaTime * 45f;
			rayThickness = MathF.Max(0f, rayThickness - loop.Input.KeyboardAndMouse.MouseScrollWheelDelta * RayThicknessStep);
			var rayVisualThickness = MathF.Max(rayThickness * 2f, MinRayVisualThickness);

			var queryOrigin = frozen ? frozenRay : BuildQueryRay();
			draggedShapeCentre = ShapeDragAlgorithm.GetValue(
				draggedShapeCentre,
				queryOrigin.StartPoint + queryOrigin.Direction * ShapeQueryDistance,
				1f - MathF.Exp(-ShapeDragSharpness * deltaTime)
			);
			var shapeCentre = draggedShapeCentre;
			var resultsDest = results.AsSpan(0, resultCap);

			int hitCount;
			switch (queryMode) {
				case QueryMode.Ray:
					hitCount = scene.QueryProvider.FindIntersections(queryOrigin, resultsDest, rayThickness);
					queryShapePrimitive.SetGeometryShape(queryOrigin, size: rayVisualThickness, includeStartPoint: true, constantScreenSize: false);
					break;
				case QueryMode.BoundedRay:
					var boundedRay = new BoundedRay(queryOrigin.StartPoint, queryOrigin.Direction * BoundedRayLength);
					hitCount = scene.QueryProvider.FindIntersections(boundedRay, resultsDest, rayThickness);
					queryShapePrimitive.SetGeometryShape(boundedRay, size: rayVisualThickness, includeEndpoints: true, constantScreenSize: false);
					break;
				case QueryMode.Sphere:
					var sphere = new PositionedSphere(0.5f, shapeCentre);
					hitCount = scene.QueryProvider.FindIntersections(sphere, resultsDest);
					queryShapePrimitive.SetGeometryShape(sphere);
					break;
				case QueryMode.AxisAlignedCuboid:
					var aaCuboid = new PositionedCuboid(0.875f, 0.875f, 0.875f, shapeCentre);
					hitCount = scene.QueryProvider.FindIntersections(aaCuboid, resultsDest);
					queryShapePrimitive.SetGeometryShape(aaCuboid.WithRotation(Rotation.None));
					break;
				default:
					var rotatedCuboid = new PositionedRotatedCuboid(new Cuboid(1.125f, 0.375f, 0.375f), shapeCentre, rotatedCuboidAngle % Direction.Up);
					hitCount = scene.QueryProvider.FindIntersections(rotatedCuboid, resultsDest);
					queryShapePrimitive.SetGeometryShape(rotatedCuboid);
					break;
			}

			for (var i = 0; i < instances.Count; ++i) {
				var hitIndex = -1;
				for (var r = 0; r < hitCount; ++r) {
					if (resultsDest[r] != instances[i]) continue;
					hitIndex = r;
					break;
				}
				var color = hitIndex < 0
					? UnhitColor
					: HitOrderGradient.GetValue(NearestHitColor, FarthestHitColor, hitCount > 1 ? hitIndex / (float) (hitCount - 1) : 0f);
				instances[i].SetDefaultMaterialBaseColor(color);
				boundingBoxPrimitives[i].SetPaintbrush(new PrimitivePaintbrush(color with { Alpha = BoundingBoxAlpha }));
			}

			window.SetTitle(
				$"Scene Querying | {queryMode} | {hitCount}/{resultCap} hit(s) of {instances.Count} instances | " +
				$"{(frozen ? "FROZEN" : "live")} | ray thickness {rayThickness:N2} | red -> blue = hit order | " +
				$"Space = mode, F = freeze, wheel = thickness, 1-{MaxResultCount} = result cap"
			);

			renderer.Render();
		}

		foreach (var primitive in boundingBoxPrimitives) primitive.Dispose();
		foreach (var instance in instances) {
			scene.Remove(instance);
			instance.Dispose();
		}
	}
}
