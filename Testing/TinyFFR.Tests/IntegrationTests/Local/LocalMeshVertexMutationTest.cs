// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMeshVertexMutationTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }
	
	[Test]
	public void Execute() {
		DoStandardCubeTest();
		DoMinefieldTest();
	}
	
	public void DoStandardCubeTest() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Mesh Vertex Mutation Test Part 1/2 (Standard Cube)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube, centreTextureOrigin: false, generationConfig: new(), config: new() { AllowsPerInstanceVertexMutation = true });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
		
		scene.Add(instance);
		
		using var cameraController = camera.CreateController<InspectorCameraController>();
		cameraController.MaxDistance = 4f;
		cameraController.MinDistance = 2f;
		cameraController.Distance = 4f;
		
		var numDefaultVertices = mesh.GetNonModifiedVertexCountIfAllowsMutation();
		var vertexBuffer = new MeshVertex[numDefaultVertices];
		mesh.CopyNonModifiedVerticesIfAllowsMutation(vertexBuffer);
		
		for (var i = 0; i < vertexBuffer.Length; ++i) Console.WriteLine(i + " : " + vertexBuffer[i].Location);
	
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			const float MutationSpeed = 0.2f;
			instance.CopyModifiedVerticesIfAllowsMutation(vertexBuffer);
			var modifiedVertex = vertexBuffer[0] with { Location = vertexBuffer[0].Location.AsVect().WithLengthIncreasedBy(dt * MutationSpeed).AsLocation() };
			instance.ModifyVertices(0, new ReadOnlySpan<MeshVertex>(in modifiedVertex));
			var extraVertices = new[] {
				vertexBuffer[15] with { Location = vertexBuffer[15].Location.AsVect().WithLengthIncreasedBy(dt * MutationSpeed).AsLocation() },
				vertexBuffer[16],
				vertexBuffer[17] with { Location = vertexBuffer[17].Location.AsVect().WithLengthIncreasedBy(dt * MutationSpeed).AsLocation() },
			};
			instance.ModifyVertices(15, extraVertices);
			
			if (loop.Input.KeyboardAndMouse.NewMouseClicks.Any(mc => mc.Key == MouseKey.MouseLeft)) window.LockCursor = !window.LockCursor;
			
			cameraController.AdjustAllViaDefaultControls(loop.Input.KeyboardAndMouse, dt);
			cameraController.AdjustAllViaDefaultControls(loop.Input.GameControllersCombined, dt);
			cameraController.Progress(dt);
			
			renderer.Render();
		}
		
		scene.Remove(instance);
	}

	public void DoMinefieldTest() {
		const int NumInstances = 5;
		const float TimeBetweenVertexSelectionsSecs = 0.05f;
		const float VertexAnimationTime = 1f;
		const float VertexDisplacementStep = 0.2f;
		
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Mesh Vertex Mutation Test Part 2/2 (Minefield)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere, subdivisionLevel: 6, generationConfig: new(), config: new() { AllowsPerInstanceVertexMutation = true });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		
		using var instances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, NumInstances)
				.Select(i => factory.ObjectBuilder.CreateModelInstance(mesh, mat, new Vect(0f, 0f, 1.4f).RotatedBy((360f / NumInstances) * i % Direction.Up).AsLocation()))
				.ToArray()
		);
		
		scene.Add(instances);
		
		using var cameraController = camera.CreateController<InspectorCameraController>();
		cameraController.MaxDistance = 6f;
		cameraController.MinDistance = 2f;
		cameraController.Distance = 4f;
		
		var timeUntilNextVerticesSelected = TimeBetweenVertexSelectionsSecs;
		var ongoingMutationIndices = new List<(int Index, float Time)>[NumInstances];
		for (var i = 0; i < NumInstances; ++i) ongoingMutationIndices[i] = new List<(int Index, float Time)>();
		var numDefaultVertices = mesh.GetNonModifiedVertexCountIfAllowsMutation();
		var defaultVertices = new MeshVertex[numDefaultVertices];
		mesh.CopyNonModifiedVerticesIfAllowsMutation(defaultVertices);
		var interpAlgo = InterpolationAlgorithm<Real>.DecelerateFromFastWithOvershoot();
	
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			timeUntilNextVerticesSelected -= dt;
			while (timeUntilNextVerticesSelected <= 0f) {
				for (var i = 0; i < NumInstances; ++i) {
					// Could technically double-add the same index but that's fine, it'll just look a bit odd for a bit
					ongoingMutationIndices[i].Add((Random.Shared.Next(numDefaultVertices), 0f));
				}
				timeUntilNextVerticesSelected += TimeBetweenVertexSelectionsSecs;
			}
			
			for (var i = 0; i < NumInstances; ++i) {
				var list = ongoingMutationIndices[i];
				for (var j = list.Count - 1; j >= 0; --j) {
					var tuple = list[j];
					var defaultVert = defaultVertices[tuple.Index];
					var newTime = tuple.Time += dt;
					var displacementTarget = 1f + VertexDisplacementStep * (i + 1);
					if (newTime >= VertexAnimationTime) {
						list.RemoveAt(j);
						var newVert = defaultVert with { Location = defaultVert.Location.AsVect().WithLength(displacementTarget).AsLocation() };
						instances[i].ModifyVertices(tuple.Index, new ReadOnlySpan<MeshVertex>(in newVert));
					}
					else {
						list[j] = (tuple.Index, newTime);
						var newVert = defaultVert with { Location = defaultVert.Location.AsVect().WithLength(displacementTarget * interpAlgo.GetValue(0f, 1f, newTime, VertexAnimationTime)).AsLocation() };
						instances[i].ModifyVertices(tuple.Index, new ReadOnlySpan<MeshVertex>(in newVert));
					}
				}
			}
		
			
			
			if (loop.Input.KeyboardAndMouse.NewMouseClicks.Any(mc => mc.Key == MouseKey.MouseLeft)) window.LockCursor = !window.LockCursor;
			
			cameraController.AdjustAllViaDefaultControls(loop.Input.KeyboardAndMouse, dt);
			cameraController.AdjustAllViaDefaultControls(loop.Input.GameControllersCombined, dt);
			cameraController.Progress(dt);
			
			renderer.Render();
		}
		
		scene.Remove(instances);
	}
}