// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalDependencyTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using (var factory = new LocalTinyFfrFactory()) {
			Assert.Catch<InvalidOperationException>(() => _ = new LocalTinyFfrFactory());

			var group = factory.ResourceAllocator.CreateResourceGroup(false);
			var loop = factory.ApplicationLoopBuilder.CreateLoop();
			group.AddResource(loop);
			AssertDependency(loop, group);

			var tex = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.White);
			var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(tex);
			AssertDependency(tex, mat);

			tex = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.White);
			mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(tex);
			var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor());
			var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
			AssertDependency(mesh, instance);
			
			mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor());
			instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
			AssertDependency(mat, instance);
			mesh.Dispose();
			tex.Dispose();

			var camera = factory.CameraBuilder.CreateCamera();
			var scene = factory.SceneBuilder.CreateScene();
			var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Recommended!.Value);
			var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(camera, renderer);

			camera = factory.CameraBuilder.CreateCamera();
			renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(scene, renderer);

			scene = factory.SceneBuilder.CreateScene();
			renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(window, renderer);
			scene.Dispose();
			camera.Dispose();
		}

		Assert.DoesNotThrow(() => new LocalTinyFfrFactory().Dispose());
	}

	void AssertDependency<TTarget, TDependent>(TTarget target, TDependent dependent) where TTarget : IDisposableResource<TTarget> where TDependent : IDisposableResource<TDependent> {
		Assert.Catch<ResourceDependencyException>(target.Dispose);
		Assert.DoesNotThrow(dependent.Dispose);
		Assert.DoesNotThrow(target.Dispose);
	}
} 