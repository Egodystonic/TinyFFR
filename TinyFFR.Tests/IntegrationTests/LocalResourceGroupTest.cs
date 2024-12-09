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
class LocalResourceGroupTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		var meshes = new Mesh[4];
		for (var i = 0; i < meshes.Length; ++i) meshes[i] = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(1f * i, 2f * i, 3f * i));

		var cameras = new Camera[4];
		for (var i = 0; i < cameras.Length; ++i) cameras[i] = factory.CameraBuilder.CreateCamera(new Location(i, i, i));

		using (var group = factory.CreateResourceGroup(true)) {
			Assert.AreEqual(true, group.DisposesContainedResourcesByDefaultWhenDisposed);
			Assert.AreEqual(false, group.IsSealed);

			group.AddResource(meshes[0]);
			group.AddResource(meshes[1]);
			group.AddResource(cameras[0]);
			group.AddResource(cameras[1]);

			Assert.AreEqual(4, group.ResourceCount);
			Assert.AreEqual(4, group.Resources.Length);
			Assert.AreEqual(ToStub(meshes[0]), group.Resources[0]);
			Assert.AreEqual(ToStub(meshes[1]), group.Resources[1]);
			Assert.AreEqual(ToStub(cameras[0]), group.Resources[2]);
			Assert.AreEqual(ToStub(cameras[1]), group.Resources[3]);

			Assert.AreEqual(2, group.GetAllResourcesOfType<Mesh>().Count);
			Assert.AreEqual(meshes[0], group.GetAllResourcesOfType<Mesh>()[0]);
			Assert.AreEqual(meshes[1], group.GetAllResourcesOfType<Mesh>()[1]);

			Assert.AreEqual(2, group.GetAllResourcesOfType<Camera>().Count);
			Assert.AreEqual(cameras[0], group.GetAllResourcesOfType<Camera>()[0]);
			Assert.AreEqual(cameras[1], group.GetAllResourcesOfType<Camera>()[1]);
			
			Assert.AreEqual(0, group.GetAllResourcesOfType<Material>().Count);

			Assert.AreEqual(meshes[0], group.GetNthResourceOfType<Mesh>(0));
			Assert.AreEqual(meshes[1], group.GetNthResourceOfType<Mesh>(1));
			Assert.AreEqual(cameras[0], group.GetNthResourceOfType<Camera>(0));
			Assert.AreEqual(cameras[1], group.GetNthResourceOfType<Camera>(1));

			Assert.Catch(() => group.GetNthResourceOfType<Mesh>(2)); 
			Assert.Catch(() => group.GetNthResourceOfType<Camera>(2)); 
			Assert.Catch(() => group.GetNthResourceOfType<Material>(0));

			group.Seal();
			Assert.AreEqual(true, group.IsSealed);
			Assert.Catch(() => group.AddResource(cameras[2]));
		}

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[0].Name.ToString()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[1].Name.ToString()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(cameras[0].Name.ToString()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(cameras[1].Name.ToString()));

		using (var group = factory.CreateResourceGroup(false)) {
			Assert.AreEqual(false, group.DisposesContainedResourcesByDefaultWhenDisposed);
			Assert.AreEqual(false, group.IsSealed);

			group.AddResource(meshes[2]);
			group.AddResource(meshes[3]);

			Assert.AreEqual(2, group.ResourceCount);
			Assert.AreEqual(2, group.Resources.Length);
			Assert.AreEqual(ToStub(meshes[2]), group.Resources[0]);
			Assert.AreEqual(ToStub(meshes[3]), group.Resources[1]);

			Assert.AreEqual(2, group.GetAllResourcesOfType<Mesh>().Count);
			Assert.AreEqual(meshes[2], group.GetAllResourcesOfType<Mesh>()[0]);
			Assert.AreEqual(meshes[3], group.GetAllResourcesOfType<Mesh>()[1]);

			Assert.AreEqual(0, group.GetAllResourcesOfType<Material>().Count);
			Assert.AreEqual(0, group.GetAllResourcesOfType<Camera>().Count);

			Assert.AreEqual(meshes[2], group.GetNthResourceOfType<Mesh>(0));
			Assert.AreEqual(meshes[3], group.GetNthResourceOfType<Mesh>(1));

			Assert.Catch(() => group.GetNthResourceOfType<Mesh>(2));
			Assert.Catch(() => group.GetNthResourceOfType<Camera>(0));
			Assert.Catch(() => group.GetNthResourceOfType<Material>(0));

			group.Seal();
			Assert.AreEqual(true, group.IsSealed);
			Assert.Catch(() => group.AddResource(cameras[2]));
		}

		Assert.DoesNotThrow(() => Console.WriteLine(meshes[2].Name.ToString()));
		Assert.DoesNotThrow(() => Console.WriteLine(meshes[3].Name.ToString()));

		var g = factory.CreateResourceGroup(false);
		g.AddResource(meshes[2]);
		g.AddResource(meshes[3]);
		g.Dispose(disposeContainedResources: true);

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[2].Name.ToString()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[3].Name.ToString()));

		g = factory.CreateResourceGroup(true);
		g.AddResource(cameras[2]);
		g.AddResource(cameras[3]);
		g.Dispose(disposeContainedResources: false);

		Assert.DoesNotThrow(() => Console.WriteLine(cameras[2].Name.ToString()));
		Assert.DoesNotThrow(() => Console.WriteLine(cameras[3].Name.ToString()));
	}

	ResourceStub ToStub<TResource>(TResource r) where TResource : IResource => r.AsStub;
} 