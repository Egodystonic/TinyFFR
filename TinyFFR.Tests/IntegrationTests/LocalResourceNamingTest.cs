// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalResourceNamingTest {
	const string SkyboxFile = "IntegrationTests\\kloofendal_48d_partly_cloudy_puresky_4k.hdr";
	
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		// ReSharper disable AccessToDisposedClosure Factory will be disposed only after closure is no longer in use
		using var factory = new LocalTinyFfrFactory();
		TestNameStorageAndRetrieval(n => factory.AssetLoader.LoadEnvironmentCubemap(SkyboxFile, name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.MaterialBuilder.CreateColorMap(ColorVect.Black, name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f), name: n));
		TestNameStorageAndRetrieval(n => factory.ApplicationLoopBuilder.CreateLoop(name: n));
		if (factory.DisplayDiscoverer.Primary is { } primaryDisplay) {
			TestNameStorageAndRetrieval(n => factory.WindowBuilder.CreateWindow(primaryDisplay, title: n));
			using var scene = factory.SceneBuilder.CreateScene();
			using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
			using var camera = factory.CameraBuilder.CreateCamera();
			TestNameStorageAndRetrieval(n => factory.RendererBuilder.CreateRenderer(scene, camera, window, name: n));
		}
		TestNameStorageAndRetrieval(n => factory.ResourceAllocator.CreateResourceGroup(false, name: n));
		TestNameStorageAndRetrieval(n => factory.CameraBuilder.CreateCamera(name: n));
		TestNameStorageAndRetrieval(n => factory.LightBuilder.CreatePointLight(name: n));
		TestNameStorageAndRetrieval(n => factory.LightBuilder.CreateSpotLight(name: n));
		TestNameStorageAndRetrieval(n => factory.SceneBuilder.CreateScene(name: n));
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateModelInstance(mesh, factory.AssetLoader.MaterialBuilder.TestMaterial, name: n));
		// ReSharper restore AccessToDisposedClosure
	}

	void TestNameStorageAndRetrieval<T>(Func<string, T> creationFunc) where T : IResource<T> {
		const string TestName = "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
		var res = creationFunc(TestName);
		var charArr = new char[TestName.Length];
		res.CopyName(charArr);
		
		Assert.AreEqual(TestName, res.GetNameAsNewStringObject());
		Assert.AreEqual(TestName.Length, res.GetNameLength());
		Assert.AreEqual(TestName, new String(charArr));

		if (res is not IDisposableResource disposable) return;
		disposable.Dispose();

		Assert.Catch<ObjectDisposedException>(() => res.GetNameAsNewStringObject());
		Assert.Catch<ObjectDisposedException>(() => res.GetNameLength());
		Assert.Catch<ObjectDisposedException>(() => res.CopyName(charArr));
	}
} 