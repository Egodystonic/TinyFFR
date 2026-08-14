// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
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
			var boxedResources = group.GetAllResourcesBoxed().ToArray();
			Assert.AreEqual(4, boxedResources.Length);
			Assert.AreEqual(ToStub(meshes[0]), boxedResources[0]);
			Assert.AreEqual(ToStub(meshes[1]), boxedResources[1]);
			Assert.AreEqual(ToStub(cameras[0]), boxedResources[2]);
			Assert.AreEqual(ToStub(cameras[1]), boxedResources[3]);

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
			var boxedResources = group.GetAllResourcesBoxed().ToArray();
			Assert.AreEqual(2, boxedResources.Length);
			Assert.AreEqual(ToStub(meshes[2]), boxedResources[0]);
			Assert.AreEqual(ToStub(meshes[3]), boxedResources[1]);

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

	[Test]
	public void SpecializedResourcesShouldRoundTrip() {
		using var factory = new LocalTinyFfrFactory();

		using var tex = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.RealWorldBrick), includeAlpha: false);
		using var material = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);

		using var quadMesh = factory.MeshBuilder.CreateQuadMesh();
		using var quadInstance = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		using var cameraLockedQuadInstance = factory.ObjectBuilder.CreateCameraLockedQuadInstance(
			quadMesh,
			material,
			lockedUprightDirection: Direction.Up,
			positionAnchor: Orientation2D.UpLeft,
			scalingMode: CameraLockedScalingMode.ViewportFractionalFixedHeight,
			lockStyle: CameraLockStyle.FaceCameraPlane
		);

		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(BuiltInFontPenStyle.Default);
		using var fontString = font.CreateString("Test");
		using var textInstance = factory.ObjectBuilder.CreateTextInstance(pen, fontString);
		using var cameraLockedTextInstance = factory.ObjectBuilder.CreateCameraLockedTextInstance(
			pen,
			fontString,
			lockedUprightDirection: Direction.Left,
			layout: new TextLayout(0.1f, Orientation2D.DownRight),
			scalingMode: CameraLockedScalingMode.ViewportFractionalFixedWidth,
			lockStyle: CameraLockStyle.FaceCameraPosition
		);

		using var canvasScene = factory.SceneBuilder.CreateCanvasScene();
		using var canvasSourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		using var canvasTexture = canvasScene.Add(canvasSourceTexture);
		using var canvasText = canvasScene.Add("Test", pen);

		using var group = factory.ResourceAllocator.CreateResourceGroup(false);
		group.Add(quadMesh);
		group.Add(quadInstance);
		group.Add(cameraLockedQuadInstance);
		group.Add(pen);
		group.Add(fontString);
		group.Add(textInstance);
		group.Add(cameraLockedTextInstance);
		group.Add(canvasScene);
		group.Add(canvasTexture);
		group.Add(canvasText);

		Assert.AreEqual(10, group.ResourceCount);

		Assert.AreEqual(1, group.QuadMeshes.Count);
		Assert.AreEqual(quadMesh, group.QuadMeshes[0]);
		Assert.AreEqual(1, group.QuadInstances.Count);
		Assert.AreEqual(quadInstance, group.QuadInstances[0]);
		Assert.AreEqual(1, group.CameraLockedQuadInstances.Count);
		Assert.AreEqual(cameraLockedQuadInstance, group.CameraLockedQuadInstances[0]);
		Assert.AreEqual(1, group.FontPens.Count);
		Assert.AreEqual(pen, group.FontPens[0]);
		Assert.AreEqual(1, group.FontStrings.Count);
		Assert.AreEqual(fontString, group.FontStrings[0]);
		Assert.AreEqual(1, group.TextInstances.Count);
		Assert.AreEqual(textInstance, group.TextInstances[0]);
		Assert.AreEqual(1, group.CameraLockedTextInstances.Count);
		Assert.AreEqual(cameraLockedTextInstance, group.CameraLockedTextInstances[0]);
		Assert.AreEqual(1, group.CanvasScenes.Count);
		Assert.AreEqual(canvasScene, group.CanvasScenes[0]);
		Assert.AreEqual(1, group.CanvasTextures.Count);
		Assert.AreEqual(canvasTexture, group.CanvasTextures[0]);
		Assert.AreEqual(1, group.CanvasTexts.Count);
		Assert.AreEqual(canvasText, group.CanvasTexts[0]);

		Assert.AreEqual(quadMesh, group.GetNthResourceOfType<QuadMesh, Mesh>(0));
		Assert.AreEqual(pen, group.GetNthResourceOfType<FontPen, Font>(0));
		Assert.AreEqual(canvasText, group.GetNthResourceOfType<CanvasText, ModelInstance>(0));
		Assert.Catch(() => group.GetNthResourceOfType<QuadMesh, Mesh>(1));
		Assert.Catch(() => group.GetNthResourceOfType<CanvasText, ModelInstance>(1));

		var roundTrippedLockedQuad = group.CameraLockedQuadInstances[0];
		Assert.AreEqual(Direction.Up, roundTrippedLockedQuad.LockedUprightDirection);
		Assert.AreEqual(Orientation2D.UpLeft, roundTrippedLockedQuad.PositionAnchor);
		Assert.AreEqual(CameraLockedScalingMode.ViewportFractionalFixedHeight, roundTrippedLockedQuad.ScalingMode);
		Assert.AreEqual(CameraLockStyle.FaceCameraPlane, roundTrippedLockedQuad.LockStyle);

		var roundTrippedLockedText = group.CameraLockedTextInstances[0];
		Assert.AreEqual(Direction.Left, roundTrippedLockedText.LockedUprightDirection);
		Assert.AreEqual(Orientation2D.DownRight, roundTrippedLockedText.PositionAnchor);
		Assert.AreEqual(CameraLockedScalingMode.ViewportFractionalFixedWidth, roundTrippedLockedText.ScalingMode);
		Assert.AreEqual(CameraLockStyle.FaceCameraPosition, roundTrippedLockedText.LockStyle);

		Assert.AreEqual(pen.Font, group.FontPens[0].Font);
		Assert.AreEqual(fontString.Size, group.FontStrings[0].Size);
		Assert.AreEqual(canvasScene, group.CanvasTextures[0].Canvas);
		Assert.AreEqual(canvasScene, group.CanvasTexts[0].Canvas);

		Assert.AreEqual(1, group.Meshes.Count);
		Assert.AreEqual(quadMesh.UnderlyingMesh, group.Meshes[0]);
		Assert.AreEqual(6, group.ModelInstances.Count);
		Assert.AreEqual(2, group.Fonts.Count);
		Assert.AreEqual(1, group.Scenes.Count);
		Assert.AreEqual(canvasScene.UnderlyingScene, group.Scenes[0]);
		Assert.AreEqual(0, group.Cameras.Count);

		var boxed = group.GetAllResourcesBoxed().ToArray();
		Assert.AreEqual(10, boxed.Length);
		Assert.IsInstanceOf<QuadMesh>(boxed[0]);
		Assert.IsInstanceOf<QuadInstance>(boxed[1]);
		Assert.IsInstanceOf<CameraLockedQuadInstance>(boxed[2]);
		Assert.IsInstanceOf<FontPen>(boxed[3]);
		Assert.IsInstanceOf<FontString>(boxed[4]);
		Assert.IsInstanceOf<TextInstance>(boxed[5]);
		Assert.IsInstanceOf<CameraLockedTextInstance>(boxed[6]);
		Assert.IsInstanceOf<CanvasScene>(boxed[7]);
		Assert.IsInstanceOf<CanvasTexture>(boxed[8]);
		Assert.IsInstanceOf<CanvasText>(boxed[9]);
		Assert.AreEqual(quadMesh, (QuadMesh) boxed[0]);
		Assert.AreEqual(pen, (FontPen) boxed[3]);
		Assert.AreEqual(canvasText, (CanvasText) boxed[9]);

		var quadMeshIterator = group.QuadMeshes;
		var canvasTextIterator = group.CanvasTexts;
		Assert.DoesNotThrow(() => _ = quadMeshIterator.Count);
		Assert.DoesNotThrow(() => _ = canvasTextIterator.Count);
		group.Add(quadInstance);
		Assert.Catch<InvalidOperationException>(() => _ = quadMeshIterator.Count);
		Assert.Catch<InvalidOperationException>(() => _ = canvasTextIterator.Count);
	}

	[Test]
	public void SpecializedResourcesShouldBeDisposedWithGroup() {
		using var factory = new LocalTinyFfrFactory();

		using var tex = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.RealWorldBrick), includeAlpha: false);
		using var material = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);

		var quadMesh = factory.MeshBuilder.CreateQuadMesh();
		var quadInstance = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		var underlyingMesh = quadMesh.UnderlyingMesh;
		var underlyingQuadModelInstance = quadInstance.UnderlyingModelInstance;

		var meshGroup = factory.ResourceAllocator.CreateResourceGroup(true);
		meshGroup.Add(quadMesh);
		meshGroup.Add(quadInstance);
		meshGroup.Dispose();

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(underlyingQuadModelInstance.GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(underlyingMesh.GetNameAsNewStringObject()));

		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(BuiltInFontPenStyle.Default);
		var fontString = font.CreateString("Test");

		var fontGroup = factory.ResourceAllocator.CreateResourceGroup(true);
		fontGroup.Add(pen);
		fontGroup.Add(fontString);
		fontGroup.Dispose();

		Assert.DoesNotThrow(() => Console.WriteLine(font.GetNameAsNewStringObject()));

		var canvasPen = font.CreatePen(BuiltInFontPenStyle.Default);
		var canvasScene = factory.SceneBuilder.CreateCanvasScene();
		using var canvasSourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		var canvasTexture = canvasScene.Add(canvasSourceTexture);
		var canvasText = canvasScene.Add("Test", canvasPen);
		var underlyingScene = canvasScene.UnderlyingScene;
		var underlyingCanvasTextureInstance = canvasTexture.UnderlyingModelInstance;
		var underlyingCanvasTextInstance = canvasText.UnderlyingModelInstance;

		var canvasGroup = factory.ResourceAllocator.CreateResourceGroup(true);
		canvasGroup.Add(canvasScene);
		canvasGroup.Add(canvasTexture);
		canvasGroup.Add(canvasText);
		canvasGroup.Dispose();

		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(underlyingScene.GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(underlyingCanvasTextureInstance.GetNameAsNewStringObject()));
		Assert.Throws<ObjectDisposedException>(() => Console.WriteLine(underlyingCanvasTextInstance.GetNameAsNewStringObject()));

		canvasPen.Dispose();

		var survivingQuadMesh = factory.MeshBuilder.CreateQuadMesh();
		var survivingUnderlyingMesh = survivingQuadMesh.UnderlyingMesh;
		var nonDisposingGroup = factory.ResourceAllocator.CreateResourceGroup(true);
		nonDisposingGroup.Add(survivingQuadMesh);
		nonDisposingGroup.Dispose(disposeContainedResources: false);

		Assert.DoesNotThrow(() => Console.WriteLine(survivingUnderlyingMesh.GetNameAsNewStringObject()));
		survivingQuadMesh.Dispose();
		font.Dispose();
	}

	[Test]
	public void SealedGroupShouldRejectSpecializedAdds() {
		using var factory = new LocalTinyFfrFactory();

		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(BuiltInFontPenStyle.Default);
		using var fontString = font.CreateString("Test");
		using var quadMesh = factory.MeshBuilder.CreateQuadMesh();

		using var group = factory.ResourceAllocator.CreateResourceGroup(false);
		group.Add(pen);
		Assert.AreEqual(1, group.ResourceCount);

		group.Seal();
		Assert.AreEqual(true, group.IsSealed);

		Assert.Catch<ResourceGroupSealedException>(() => group.Add(fontString));
		Assert.Catch<ResourceGroupSealedException>(() => group.Add(quadMesh));

		Assert.AreEqual(1, group.ResourceCount);
		Assert.AreEqual(1, group.FontPens.Count);
		Assert.AreEqual(pen, group.FontPens[0]);
		Assert.AreEqual(0, group.FontStrings.Count);
		Assert.AreEqual(0, group.QuadMeshes.Count);
	}

	[Test]
	public void GroupShouldTrackDependencyOnSpecializationAdditionalResourceRef() {
		using var factory = new LocalTinyFfrFactory();

		var canvasScene = factory.SceneBuilder.CreateCanvasScene();
		using var canvasSourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		var canvasTexture = canvasScene.Add(canvasSourceTexture);

		var group = factory.ResourceAllocator.CreateResourceGroup(false);
		group.Add(canvasTexture);

		Assert.Catch<ResourceDependencyException>(() => canvasScene.Dispose());

		group.Dispose();

		Assert.DoesNotThrow(() => canvasTexture.Dispose());
		Assert.DoesNotThrow(() => canvasScene.Dispose());
	}
} 