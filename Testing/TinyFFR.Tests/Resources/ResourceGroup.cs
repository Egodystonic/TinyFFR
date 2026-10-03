using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Resources;

[TestFixture]
class ResourceGroupTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void GroupsShouldDisposeContentsInDependencyOrder() {
		using var factory = new LocalTinyFfrFactory();
		var texture = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
		var material = factory.MaterialBuilder.CreateStandardMaterial(texture);
		var mesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
		var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(instance);
		group.Add(material);
		group.Add(texture);
		group.Add(mesh);

		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(group.IsDisposed);
		Assert.IsTrue(instance.IsDisposed);
		Assert.IsTrue(material.IsDisposed);
		Assert.IsTrue(texture.IsDisposed);
	}

	[Test]
	public void GroupDisposalShouldBeAtomicWhenAContainedResourceIsInUseElsewhere() {
		using var factory = new LocalTinyFfrFactory();
		var texture = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
		var mesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
		var material = factory.MaterialBuilder.CreateStandardMaterial(texture);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(mesh);
		group.Add(texture);

		Assert.Throws<ResourceDependencyException>(() => group.Dispose());
		Assert.IsFalse(group.IsDisposed);
		Assert.IsFalse(texture.IsDisposed);
		Assert.IsFalse(mesh.IsDisposed);
		Assert.AreEqual(2, group.ResourceCount);

		material.Dispose();
		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(texture.IsDisposed);
		Assert.IsTrue(mesh.IsDisposed);
	}

	[Test]
	public void GroupDisposalShouldTreatExcludedResourcesAsExternalDependents() {
		using var factory = new LocalTinyFfrFactory();
		var texture = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
		var material = factory.MaterialBuilder.CreateStandardMaterial(texture);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(texture);
		group.Add(material);
		group.ExcludeFromDisposal(material);

		Assert.Throws<ResourceDependencyException>(() => group.Dispose());
		Assert.IsFalse(group.IsDisposed);
		Assert.IsFalse(texture.IsDisposed);

		Assert.DoesNotThrow(() => group.Dispose(disposeContainedResources: false));
		material.Dispose();
		texture.Dispose();
	}

	[Test]
	public void FontsShouldOwnTheirPensAndStrings() {
		using var factory = new LocalTinyFfrFactory();
		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(StandardColor.White);
		var str = font.CreateString("Hello");
		var text = factory.ObjectBuilder.CreateTextInstance(pen, str);

		Assert.Throws<ResourceDependencyException>(() => font.Dispose());
		Assert.IsFalse(font.IsDisposed);
		Assert.DoesNotThrow(() => font.MeasureString("Hello"));

		text.Dispose();
		Assert.DoesNotThrow(() => font.Dispose());
		Assert.IsTrue(font.IsDisposed);
	}

	[Test]
	public void GroupsShouldOrderFontsAfterTheirUsers() {
		using var factory = new LocalTinyFfrFactory();
		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(StandardColor.White);
		var str = font.CreateString("Hello");
		var text = factory.ObjectBuilder.CreateTextInstance(pen, str);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(font);
		group.Add(str);
		group.Add(pen);
		group.Add(text);
		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(font.IsDisposed);
		Assert.IsTrue(text.UnderlyingModelInstance.IsDisposed);

		font = factory.AssetLoader.LoadFont();
		pen = font.CreatePen(StandardColor.White);
		str = font.CreateString("Hello");
		text = factory.ObjectBuilder.CreateTextInstance(pen, str);
		group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(text);
		group.Add(font);
		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(font.IsDisposed);
		Assert.IsTrue(text.UnderlyingModelInstance.IsDisposed);
	}

	[Test]
	public void GroupDisposalShouldBeAtomicWhenAFontIsInUseOutsideTheGroup() {
		using var factory = new LocalTinyFfrFactory();
		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(StandardColor.White);
		var str = font.CreateString("Hello");
		var text = factory.ObjectBuilder.CreateTextInstance(pen, str);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(font);
		group.Add(pen);

		Assert.Throws<ResourceDependencyException>(() => group.Dispose());
		Assert.IsFalse(group.IsDisposed);
		Assert.IsFalse(font.IsDisposed);
		Assert.DoesNotThrow(() => text.Pen = pen);

		text.Dispose();
		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(font.IsDisposed);
	}

	[Test]
	public void GroupsShouldProtectFontPensAndStringsFromIndependentDisposal() {
		using var factory = new LocalTinyFfrFactory();
		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(StandardColor.White);
		var str = font.CreateString("Hello");

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(font);
		group.Add(pen);
		group.Add(str);
		Assert.Throws<ResourceDependencyException>(() => pen.Dispose());
		Assert.Throws<ResourceDependencyException>(() => str.Dispose());
		Assert.Throws<ResourceDependencyException>(() => font.Dispose());
		Assert.IsFalse(pen.IsDisposed);
		Assert.IsFalse(str.IsDisposed);

		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(font.IsDisposed);
		Assert.IsTrue(pen.IsDisposed);
		Assert.IsTrue(str.IsDisposed);
	}

	[Test]
	public void PensAndStringsShouldBeBlockedByTheirUsersAndNameThemselvesInErrors() {
		using var factory = new LocalTinyFfrFactory();
		var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(StandardColor.White);
		var str = font.CreateString("Hello");
		var text = factory.ObjectBuilder.CreateTextInstance(pen, str);

		var penException = Assert.Throws<ResourceDependencyException>(() => pen.Dispose())!;
		StringAssert.Contains(nameof(FontPen), penException.Message);
		var stringException = Assert.Throws<ResourceDependencyException>(() => str.Dispose())!;
		StringAssert.Contains(nameof(FontString), stringException.Message);
		Assert.Throws<ResourceDependencyException>(() => font.Dispose());

		Assert.AreEqual(font, pen.Font);
		Assert.AreEqual(font, str.Font);
		text.Dispose();
		pen.Dispose();
		Assert.IsTrue(pen.IsDisposed);
		Assert.Throws<ObjectDisposedException>(() => _ = pen.Font);
		font.Dispose();
		Assert.IsTrue(str.IsDisposed);
	}

	[Test]
	public void RenderOutputBuffersShouldOwnTheirTextures() {
		using var factory = new LocalTinyFfrFactory();
		var buffer = factory.RendererBuilder.CreateRenderOutputBuffer();
		var material = factory.MaterialBuilder.CreateStandardMaterial(buffer.CreateDynamicTexture());

		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());
		Assert.IsFalse(buffer.IsDisposed);

		material.Dispose();
		Assert.DoesNotThrow(() => buffer.Dispose());
		Assert.IsTrue(buffer.IsDisposed);
	}

	[Test]
	public void GroupsShouldRejectCyclicalNesting() {
		using var factory = new LocalTinyFfrFactory();
		var a = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, name: "A");
		var b = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, name: "B");
		var c = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, name: "C");

		Assert.Throws<InvalidOperationException>(() => a.Add(a));
		Assert.Throws<InvalidOperationException>(() => a.Add<ResourceGroup>(a));

		a.Add(b);
		Assert.Throws<InvalidOperationException>(() => b.Add(a));

		b.Add(c);
		Assert.Throws<InvalidOperationException>(() => c.Add(a));
		Assert.Throws<InvalidOperationException>(() => c.Add<ResourceGroup>(b));
		Assert.DoesNotThrow(() => a.Add(c));

		Assert.AreEqual(2, a.ResourceCount);
		Assert.AreEqual(1, b.ResourceCount);
		Assert.AreEqual(0, c.ResourceCount);

		Assert.DoesNotThrow(() => a.Dispose());
		Assert.IsTrue(a.IsDisposed);
		Assert.IsTrue(b.IsDisposed);
		Assert.IsTrue(c.IsDisposed);
	}
	
	[Test]
	public void ViewsShouldOnlyBeVisibleViaTheTypeTheyWereAddedAs() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var quad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		using var camLockedQuad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material);
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(StandardColor.White);
		using var str = font.CreateString("View");
		using var text = factory.ObjectBuilder.CreateTextInstance(pen, str);
		using var camLockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, str);
		using var canvasSourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		var canvasTexture = canvas.Add(canvasSourceTexture);
		var canvasText = canvas.Add(str, pen);

		using var viewGroup = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		viewGroup.Add(quadMesh);
		viewGroup.Add(quad);
		viewGroup.Add(camLockedQuad);
		viewGroup.Add(text);
		viewGroup.Add(camLockedText);
		viewGroup.Add(canvas);
		viewGroup.Add(canvasTexture);
		viewGroup.Add(canvasText);

		CollectionAssert.AreEqual(new[] { quadMesh }, viewGroup.QuadMeshes.ToArray());
		CollectionAssert.AreEqual(new[] { quad }, viewGroup.QuadInstances.ToArray());
		CollectionAssert.AreEqual(new[] { camLockedQuad }, viewGroup.CameraLockedQuadInstances.ToArray());
		CollectionAssert.AreEqual(new[] { text }, viewGroup.TextInstances.ToArray());
		CollectionAssert.AreEqual(new[] { camLockedText }, viewGroup.CameraLockedTextInstances.ToArray());
		CollectionAssert.AreEqual(new[] { canvas }, viewGroup.CanvasScenes.ToArray());
		CollectionAssert.AreEqual(new[] { canvasTexture }, viewGroup.CanvasTextures.ToArray());
		CollectionAssert.AreEqual(new[] { canvasText }, viewGroup.CanvasTexts.ToArray());
		Assert.AreEqual(canvas, viewGroup.CanvasTexts[0].Canvas);
		Assert.AreEqual(canvas, viewGroup.CanvasTextures[0].Canvas);
		Assert.AreEqual(0, viewGroup.Meshes.Count);
		Assert.AreEqual(0, viewGroup.ModelInstances.Count);
		Assert.AreEqual(0, viewGroup.Scenes.Count);
		Assert.Throws<ArgumentOutOfRangeException>(() => _ = viewGroup.QuadInstances[1]);
		Assert.AreEqual(quad, viewGroup.GetNthResourceOfType<QuadInstance>(0));
		Assert.Throws<ArgumentOutOfRangeException>(() => viewGroup.GetNthResourceOfType<QuadInstance>(1));
		Assert.IsInstanceOf<QuadInstance>(viewGroup.GetAllResourcesBoxed().ToArray()[1]);

		using var baseGroup = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		baseGroup.Add(quadMesh.UnderlyingMesh);
		baseGroup.Add(quad.UnderlyingModelInstance);
		baseGroup.Add(camLockedQuad.UnderlyingQuadInstance.UnderlyingModelInstance);
		baseGroup.Add(text.UnderlyingModelInstance);
		baseGroup.Add(camLockedText.UnderlyingTextInstance.UnderlyingModelInstance);
		baseGroup.Add(canvas.UnderlyingScene);
		baseGroup.Add(canvasTexture.UnderlyingModelInstance);
		baseGroup.Add(canvasText.UnderlyingModelInstance);

		Assert.AreEqual(1, baseGroup.Meshes.Count);
		Assert.AreEqual(6, baseGroup.ModelInstances.Count);
		Assert.AreEqual(1, baseGroup.Scenes.Count);
		Assert.AreEqual(0, baseGroup.QuadMeshes.Count);
		Assert.AreEqual(0, baseGroup.QuadInstances.Count);
		Assert.AreEqual(0, baseGroup.CameraLockedQuadInstances.Count);
		Assert.AreEqual(0, baseGroup.TextInstances.Count);
		Assert.AreEqual(0, baseGroup.CameraLockedTextInstances.Count);
		Assert.AreEqual(0, baseGroup.CanvasScenes.Count);
		Assert.AreEqual(0, baseGroup.CanvasTextures.Count);
		Assert.AreEqual(0, baseGroup.CanvasTexts.Count);
		Assert.IsInstanceOf<ModelInstance>(baseGroup.GetAllResourcesBoxed().ToArray()[1]);
	}

	[Test]
	public void ViewsShouldBeFirstClassResources() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var quad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);

		Assert.AreEqual(ResourceUtils.ToStub(quad.UnderlyingModelInstance), ResourceUtils.ToStub(quad));
		Assert.AreEqual(ResourceUtils.ToStub(quadMesh.UnderlyingMesh), ResourceUtils.ToStub(quadMesh));
		Assert.AreEqual(quad.GetNameAsNewStringObject(), quad.UnderlyingModelInstance.GetNameAsNewStringObject());

		using var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		group.Add(quad);
		group.Add(quadMesh);
		Assert.AreEqual(0, group.ModelInstances.Count);
		Assert.AreEqual(0, group.Meshes.Count);
		Assert.AreEqual(quad, group.GetNthResourceOfType<QuadInstance>(0));
		Assert.AreEqual(quadMesh, group.GetNthResourceOfType<QuadMesh>(0));
		var boxed = group.GetAllResourcesBoxed().ToArray();
		Assert.IsInstanceOf<QuadInstance>(boxed[0]);
		Assert.IsInstanceOf<QuadMesh>(boxed[1]);

		Assert.Throws<ResourceDependencyException>(() => quad.UnderlyingModelInstance.Dispose());
		Assert.Throws<ResourceDependencyException>(() => quadMesh.UnderlyingMesh.Dispose());
		group.ExcludeFromDisposal(quad);
		group.ExcludeFromDisposal(quadMesh);
		group.Dispose(disposeContainedResources: true);
		Assert.IsFalse(quad.UnderlyingModelInstance.IsDisposed);
		Assert.IsFalse(quadMesh.UnderlyingMesh.IsDisposed);
	}

	[Test]
	public void PlainResourcesShouldNotBeRecognisedAsViews() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var mesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
		using var scene = factory.SceneBuilder.CreateScene();

		using var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		group.Add(mesh);
		group.Add(instance);
		group.Add(scene);

		Assert.AreEqual(0, group.QuadMeshes.Count);
		Assert.AreEqual(0, group.QuadInstances.Count);
		Assert.AreEqual(0, group.CameraLockedQuadInstances.Count);
		Assert.AreEqual(0, group.TextInstances.Count);
		Assert.AreEqual(0, group.CameraLockedTextInstances.Count);
		Assert.AreEqual(0, group.CanvasScenes.Count);
		Assert.AreEqual(0, group.CanvasTextures.Count);
		Assert.AreEqual(0, group.CanvasTexts.Count);
		Assert.IsFalse(instance.Implementation.IsTextInstance(instance.Handle));
		Assert.IsNull(instance.Implementation.GetCanvas(instance.Handle));
	}

	[Test]
	public void CameraLockConfigShouldBeOwnedByTheUnderlyingInstance() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(StandardColor.White);
		using var str = font.CreateString("Locked");
		using var camLockedQuad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(
			quadMesh,
			material,
			lockedUprightDirection: Direction.Up,
			positionAnchor: Orientation2D.UpLeft,
			scalingMode: CameraLockedScalingMode.ViewportFractionalFixedHeight,
			lockStyle: CameraLockStyle.FaceCameraPlane
		);
		using var camLockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(
			pen,
			str,
			lockedUprightDirection: Direction.Left,
			layout: new TextLayout(0.1f, Orientation2D.DownRight),
			scalingMode: CameraLockedScalingMode.ViewportFractionalFixedWidth,
			lockStyle: CameraLockStyle.FaceCameraPosition
		);

		void AssertQuadConfig(CameraLockedQuadInstance q) {
			Assert.AreEqual(Direction.Up, q.LockedUprightDirection);
			Assert.AreEqual(Orientation2D.UpLeft, q.PositionAnchor);
			Assert.AreEqual(CameraLockedScalingMode.ViewportFractionalFixedHeight, q.ScalingMode);
			Assert.AreEqual(CameraLockStyle.FaceCameraPlane, q.LockStyle);
		}
		void AssertTextConfig(CameraLockedTextInstance t) {
			Assert.AreEqual(Direction.Left, t.LockedUprightDirection);
			Assert.AreEqual(Orientation2D.DownRight, t.PositionAnchor);
			Assert.AreEqual(CameraLockedScalingMode.ViewportFractionalFixedWidth, t.ScalingMode);
			Assert.AreEqual(CameraLockStyle.FaceCameraPosition, t.LockStyle);
		}

		AssertQuadConfig(camLockedQuad);
		AssertTextConfig(camLockedText);
		AssertQuadConfig((CameraLockedQuadInstance) (SceneObject) camLockedQuad);
		AssertTextConfig((CameraLockedTextInstance) (SceneObject) camLockedText);

		using var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		group.Add(camLockedQuad);
		group.Add(camLockedText);
		AssertQuadConfig(group.CameraLockedQuadInstances[0]);
		AssertTextConfig(group.CameraLockedTextInstances[0]);
		Assert.AreEqual(0, group.QuadInstances.Count);
		Assert.AreEqual(0, group.TextInstances.Count);

		using var plainQuad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		var notActuallyLocked = new CameraLockedQuadInstance(plainQuad);
		Assert.AreEqual(Direction.None, notActuallyLocked.LockedUprightDirection);
		Assert.AreEqual(Orientation2D.None, notActuallyLocked.PositionAnchor);
		Assert.AreEqual(CameraLockedScalingMode.Standard, notActuallyLocked.ScalingMode);
		Assert.AreEqual(CameraLockStyle.FaceCameraPosition, notActuallyLocked.LockStyle);

		var rewrapped = CameraLockedQuadInstance.FromPreviouslyAllocatedUnderlyingQuadInstance(camLockedQuad.UnderlyingQuadInstance, Direction.Down, Orientation2D.Right, CameraLockedScalingMode.Standard, CameraLockStyle.FaceCameraPosition);
		Assert.AreEqual(camLockedQuad, rewrapped);
		Assert.AreEqual(Direction.Down, camLockedQuad.LockedUprightDirection);
		Assert.AreEqual(Orientation2D.Right, camLockedQuad.PositionAnchor);
	}

	[Test]
	public void GroupsShouldDisposeViewsViaTheirUnderlyingResources() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		var quadMesh = factory.MeshBuilder.CreateQuad();
		var quad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		var canvas = factory.SceneBuilder.CreateCanvasScene();
		var canvasTexture = canvas.Add(material);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(quad);
		group.Add(quadMesh);
		group.Add(canvasTexture);
		group.Add(canvas);

		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(quad.UnderlyingModelInstance.IsDisposed);
		Assert.IsTrue(quadMesh.UnderlyingMesh.IsDisposed);
		Assert.IsTrue(canvas.UnderlyingScene.IsDisposed);
		Assert.IsTrue(canvasTexture.UnderlyingModelInstance.IsDisposed);
	}

	[Test]
	public void CanvasScenesShouldOwnTheirCanvasObjects() {
		using var factory = new LocalTinyFfrFactory();
		using var sourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		var canvas = factory.SceneBuilder.CreateCanvasScene();
		var canvasTexture = canvas.Add(sourceTexture);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false);
		group.Add(canvasTexture);

		Assert.Throws<ResourceDependencyException>(() => canvas.Dispose());
		Assert.IsFalse(canvas.UnderlyingScene.IsDisposed);
		Assert.AreEqual(1, canvas.UnderlyingScene.ContainedModelInstances.Count);
		var secondTexture = canvas.Add(sourceTexture);
		Assert.AreEqual(2, canvas.UnderlyingScene.ContainedModelInstances.Count);

		group.Dispose();
		Assert.DoesNotThrow(() => canvasTexture.Dispose());
		Assert.AreEqual(1, canvas.UnderlyingScene.ContainedModelInstances.Count);
		Assert.DoesNotThrow(() => canvas.Dispose());
		Assert.IsTrue(secondTexture.UnderlyingModelInstance.IsDisposed);
	}

	[Test]
	public void DisposingACanvasObjectsUnderlyingInstanceShouldRemoveItFromItsCanvas() {
		using var factory = new LocalTinyFfrFactory();
		using var sourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		var canvasTexture = canvas.Add(sourceTexture);
		var canvasText = canvas.Add("Hello", factory.AssetLoader.LoadFont().CreatePen(BuiltInFontPenStyle.Default));
		Assert.AreEqual(2, canvas.UnderlyingScene.ContainedModelInstances.Count);

		canvasTexture.UnderlyingModelInstance.Dispose();
		Assert.IsTrue(canvasTexture.UnderlyingModelInstance.IsDisposed);
		Assert.AreEqual(1, canvas.UnderlyingScene.ContainedModelInstances.Count);

		canvasText.IsVisible = false;
		canvasText.IsVisible = true;
		canvasText.UnderlyingModelInstance.Dispose();
		Assert.AreEqual(0, canvas.UnderlyingScene.ContainedModelInstances.Count);
	}

	[Test]
	public void GroupsHoldingOnlyCanvasObjectsShouldDisposeThem() {
		using var factory = new LocalTinyFfrFactory();
		using var sourceTexture = factory.TextureBuilder.CreateCanvasTexture(ColorVect.WhiteOpaque, includeAlpha: false);
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		var canvasTexture = canvas.Add(sourceTexture);

		var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		group.Add(canvasTexture);
		Assert.DoesNotThrow(() => group.Dispose());
		Assert.IsTrue(canvasTexture.UnderlyingModelInstance.IsDisposed);
		Assert.AreEqual(0, canvas.UnderlyingScene.ContainedModelInstances.Count);
	}
}
