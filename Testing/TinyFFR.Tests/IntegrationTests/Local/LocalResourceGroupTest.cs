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

		var meshes = new Mesh[6];
		for (var i = 0; i < meshes.Length; ++i) meshes[i] = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f + 1f * i, 1f + 2f * i, 1f + 3f * i));

		var cameras = new Camera[4];
		for (var i = 0; i < cameras.Length; ++i) cameras[i] = factory.CameraBuilder.CreateCamera(new Location(i, i, i));

		using var tex = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.RealWorldBrick), includeAlpha: false);
		var materials = new Material[2];
		for (var i = 0; i < materials.Length; ++i) materials[i] = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);

		using (var group = factory.ResourceAllocator.CreateResourceGroup(true)) {
			Assert.AreEqual(true, group.DisposesContainedResourcesByDefaultWhenDisposed);
			Assert.AreEqual(false, group.IsSealed);

			group.Add(meshes[0]);
			group.Add(meshes[1]);
			group.Add(cameras[0]);
			group.Add(cameras[1]);

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
			Assert.Catch(() => group.Add(cameras[2]));
		}

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[0].GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[1].GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(cameras[0].GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(cameras[1].GetNameAsNewStringObject()));

		using (var group = factory.ResourceAllocator.CreateResourceGroup(false)) {
			Assert.AreEqual(false, group.DisposesContainedResourcesByDefaultWhenDisposed);
			Assert.AreEqual(false, group.IsSealed);

			group.Add(meshes[2]);
			group.Add(meshes[3]);

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
			Assert.Catch(() => group.Add(cameras[2]));
		}

		Assert.DoesNotThrow(() => Console.WriteLine(meshes[2].GetNameAsNewStringObject()));
		Assert.DoesNotThrow(() => Console.WriteLine(meshes[3].GetNameAsNewStringObject()));

		var g = factory.ResourceAllocator.CreateResourceGroup(false);
		g.Add(meshes[2]);
		g.Add(meshes[3]);
		g.Dispose(disposeContainedResources: true);

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[2].GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(meshes[3].GetNameAsNewStringObject()));

		g = factory.ResourceAllocator.CreateResourceGroup(true);
		g.Add(cameras[2]);
		g.Add(cameras[3]);
		g.Dispose(disposeContainedResources: false);

		Assert.DoesNotThrow(() => Console.WriteLine(cameras[2].GetNameAsNewStringObject()));
		Assert.DoesNotThrow(() => Console.WriteLine(cameras[3].GetNameAsNewStringObject()));



		void AssertIteratorValid<T>(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, T> iterator) {
			Assert.DoesNotThrow(() => iterator.CopyTo(new T[100]));
			Assert.DoesNotThrow(() => _ = iterator.TryCopyTo(new T[1000]));
			Assert.DoesNotThrow(() => _ = iterator.Count);
			Assert.DoesNotThrow(() => iterator.ElementAt(0));
			Assert.DoesNotThrow(() => _ = iterator.Count());
			Assert.DoesNotThrow(() => _ = iterator[0]);
		}
		void AssertIteratorInvalid<T>(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, T> iterator) {
			Assert.Catch<InvalidOperationException>(() => iterator.CopyTo(new T[100]));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.TryCopyTo(new T[1000]));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.Count);
			Assert.Catch<InvalidOperationException>(() => iterator.ElementAt(0));
			Assert.Catch<InvalidOperationException>(() => _ = iterator.Count());
			Assert.Catch<InvalidOperationException>(() => _ = iterator[0]);
		}

		g = factory.ResourceAllocator.CreateResourceGroup(false);
		g.Add(meshes[4]);
		g.Add(materials[0]);
		var i1 = g.GetAllResourcesOfType<Mesh>();
		var i2 = g.Materials;
		var i3 = g.Meshes;
		AssertIteratorValid(i1);
		AssertIteratorValid(i2);
		AssertIteratorValid(i3);
		g.Add(meshes[5]);
		g.Add(materials[1]);
		AssertIteratorInvalid(i1);
		AssertIteratorInvalid(i2);
		AssertIteratorInvalid(i3);
		g.Dispose();

		foreach (var mesh in meshes) mesh.Dispose();
		foreach (var mat in materials) mat.Dispose();
		foreach (var cam in cameras) cam.Dispose();
	}

	ResourceStub ToStub<TResource>(TResource r) where TResource : IResource => r.AsStub;
} 