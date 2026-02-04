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
using Egodystonic.TinyFFR.Testing;
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
			group.Add(loop);
			AssertDependency(loop, group);

			var tex = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false);
			var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			AssertDependency(tex, mat);

			tex = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false);
			mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
			var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
			AssertDependency(mesh, instance);
			
			mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
			instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat);
			AssertDependency(mat, instance);
			
			mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			var model = factory.AssetLoader.CreateModel(mesh, mat);
			AssertDependency(mat, model);
			mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			model = factory.AssetLoader.CreateModel(mesh, mat);
			AssertDependency(mesh, model);
			mat.Dispose();
			mesh.Dispose();
			tex.Dispose();

			var camera = factory.CameraBuilder.CreateCamera();
			var scene = factory.SceneBuilder.CreateScene();
			var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
			var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(camera, renderer);

			var cubemap = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
			scene.SetBackdrop(cubemap);
			AssertDependency(cubemap, scene);
			cubemap.Dispose();

			scene = factory.SceneBuilder.CreateScene();
			camera = factory.CameraBuilder.CreateCamera();
			renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(scene, renderer);

			scene = factory.SceneBuilder.CreateScene();
			renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
			AssertDependency(window, renderer);
			scene.Dispose();
			camera.Dispose();

			var renderBuffer = factory.RendererBuilder.CreateRenderOutputBuffer();
			scene = factory.SceneBuilder.CreateScene();
			camera = factory.CameraBuilder.CreateCamera();
			renderer = factory.RendererBuilder.CreateRenderer(scene, camera, renderBuffer);
			AssertDependency(renderBuffer, renderer);
			scene.Dispose();
			camera.Dispose();

			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.ApplicationLoopBuilder.CreateLoop());
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false));
			var tempTex = factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false);
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, tempTex, factory.MaterialBuilder.CreateStandardMaterial(tempTex));
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.MeshBuilder.CreateMesh(Cuboid.UnitCube));
			tempTex = factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false);
			var tempMat = factory.MaterialBuilder.CreateStandardMaterial(tempTex);
			var tempMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, tempTex, tempMat, tempMesh, factory.ObjectBuilder.CreateModelInstance(tempMesh, tempMat));
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.CameraBuilder.CreateCamera());
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.SceneBuilder.CreateScene());
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value));
			var tempCamera = factory.CameraBuilder.CreateCamera();
			var tempScene = factory.SceneBuilder.CreateScene();
			var tempWindow = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, tempCamera, tempScene, tempWindow, factory.RendererBuilder.CreateRenderer(tempScene, tempCamera, tempWindow));
			AssertCheckForDependentsBeforeDisposal(factory.ResourceAllocator, factory.RendererBuilder.CreateRenderOutputBuffer());

			void CheckEffectsMaterial(Func<Texture, Material> matCreationFunc, Action<Texture, MaterialEffectController> assignBlendDestTexAction) {
				using var map = factory.TextureBuilder.CreateTexture(new TexelRgba32(), isLinearColorspace: true);
				using var m = matCreationFunc(map);
				using var cube = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
				var blendMap = factory.TextureBuilder.CreateTexture(new TexelRgba32(), isLinearColorspace: true);
				var obj = factory.ObjectBuilder.CreateModelInstance(cube, m);
				assignBlendDestTexAction(blendMap, obj.MaterialEffects!.Value);
				AssertDependency(blendMap, obj);
			}
			// ReSharper disable AccessToDisposedClosure We know it won't be disposed
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateSimpleMaterial(t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.Color, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateStandardMaterial(t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.Color, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateStandardMaterial(t, ormOrOrmrMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallic, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateStandardMaterial(t, emissiveMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.Emissive, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateTransmissiveMaterial(t, absorptionTransmissionMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.Color, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateTransmissiveMaterial(t, absorptionTransmissionMap: t, ormrMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.OcclusionRoughnessMetallicReflectance, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateTransmissiveMaterial(t, absorptionTransmissionMap: t, emissiveMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.Emissive, t));
			CheckEffectsMaterial(t => factory.MaterialBuilder.CreateTransmissiveMaterial(t, absorptionTransmissionMap: t, enablePerInstanceEffects: true), (t, c) => c.SetBlendTexture(MaterialEffectMapType.AbsorptionTransmission, t));
			// ReSharper restore AccessToDisposedClosure
		}

		Assert.DoesNotThrow(() => new LocalTinyFfrFactory().Dispose());
	}

	void AssertDependency<TTarget, TDependent>(TTarget target, TDependent dependent) where TTarget : IDisposableResource<TTarget> where TDependent : IDisposableResource<TDependent> {
		Assert.Catch<ResourceDependencyException>(target.Dispose);
		Assert.DoesNotThrow(dependent.Dispose);
		Assert.DoesNotThrow(target.Dispose);
	}

	void AssertCheckForDependentsBeforeDisposal(IResourceAllocator allocator, params IDisposableResource[] resources) {
		var group = allocator.CreateResourceGroup(true, resources.Length);
		foreach (var resource in resources) group.Add(resource);
		foreach (var resource in resources) Assert.Catch<ResourceDependencyException>(resource.Dispose);
		Assert.DoesNotThrow(group.Dispose);
		foreach (var resource in resources) Assert.DoesNotThrow(resource.Dispose); // target should already be disposed because group was disposed, but this checks that disposal is idempotent
	}
} 