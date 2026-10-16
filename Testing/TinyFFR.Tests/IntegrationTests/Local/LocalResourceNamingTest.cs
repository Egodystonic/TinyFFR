// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalResourceNamingTest {
	const string TestName = "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
	HashSet<Type> _resourceTypes = default!;
	
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		_resourceTypes = GetAllTypesInMainLibImplementingInterface(typeof(IResource), includeOtherInterfaces: false)
			.Where(t => t != typeof(Display) && t != typeof(ResourceStub) && t != typeof(VertexBuffer) && t != typeof(IndexBuffer))
			.ToHashSet();
		
		// ReSharper disable AccessToDisposedClosure Factory will be disposed only after closure is no longer in use
		using var factory = new LocalTinyFfrFactory();
		using var tex = factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill(ColorVect.WhiteOpaque), includeAlpha: false);
		using var mat = factory.MaterialBuilder.CreateStandardMaterial(tex);
		
		TestNameStorageAndRetrieval(n => factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr), name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.LoadFont(name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex, name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill(ColorVect.WhiteOpaque), includeAlpha: false, name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.MeshBuilder.CreateCuboid(new Cuboid(1f), name: n));
		TestNameStorageAndRetrieval(n => factory.MeshBuilder.CreateDynamicVertexBuffer(3, 3, name: n));
		TestNameStorageAndRetrieval(n => factory.ApplicationLoopBuilder.CreateLoop(name: n));
		if (factory.DisplayDiscoverer.Primary is { } primaryDisplay) {
			TestNameStorageAndRetrieval(n => factory.WindowBuilder.CreateWindow(primaryDisplay, title: n));
			using var scene = factory.SceneBuilder.CreateScene();
			using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
			using var camera = factory.CameraBuilder.CreateCamera();
			TestNameStorageAndRetrieval(n => factory.RendererBuilder.CreateRenderer(scene, camera, window, name: n));
			TestNameStorageAndRetrieval(n => factory.RendererBuilder.CreateCompositor(window, name: n));
		}
		TestNameStorageAndRetrieval(n => factory.ResourceAllocator.CreateResourceGroup(false, name: n));
		TestNameStorageAndRetrieval(n => factory.CameraBuilder.CreateCamera(name: n));
		TestNameStorageAndRetrieval(n => factory.LightBuilder.CreatePointLight(name: n));
		TestNameStorageAndRetrieval(n => factory.LightBuilder.CreateSpotLight(name: n));
		TestNameStorageAndRetrieval(n => factory.LightBuilder.CreateDirectionalLight(name: n));
		TestNameStorageAndRetrieval(n => factory.SceneBuilder.CreateScene(name: n));
		using var mesh = factory.AssetLoader.MeshBuilder.CreateCuboid(new Cuboid(1f));
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateModelInstance(mesh, mat, name: n));
		TestNameStorageAndRetrieval(n => factory.RendererBuilder.CreateRenderOutputBuffer(name: n));
		TestNameStorageAndRetrieval(n => factory.AssetLoader.CreateModel(mesh, mat, name: n));
		var boneIdx = MeshVertexSkeletal.BoneIndexArray.Create(0, 0, 0, 0);
		var boneWgt = MeshVertexSkeletal.BoneWeightArray.Create(1f, 0f, 0f, 0f);
		var skeletalVertices = new MeshVertexSkeletal[] {
			new(new Location(0f, 0f, 0f), default, Direction.Up, Direction.Right, Direction.Forward, boneIdx, boneWgt),
			new(new Location(1f, 0f, 0f), default, Direction.Up, Direction.Right, Direction.Forward, boneIdx, boneWgt),
			new(new Location(0f, 1f, 0f), default, Direction.Up, Direction.Right, Direction.Forward, boneIdx, boneWgt),
		};
		var skeletalNodes = new SkeletalAnimationNode[] {
			new(Matrix4x4.Identity, Matrix4x4.Identity, null, 0),
			new(Matrix4x4.Identity, Matrix4x4.Identity, 0, null),
			new(Matrix4x4.Identity, Matrix4x4.Identity, 0, null),
		};
		using var skeletalMesh = factory.MeshBuilder.CreateFromVertices(skeletalVertices, new VertexTriangle[] { new(0, 1, 2) }, skeletalNodes);
		TestNameStorageAndRetrieval(n => factory.MeshBuilder.AttachAnimation(
			skeletalMesh,
			ReadOnlySpan<SkeletalAnimationScalingKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationRotationKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationTranslationKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor>.Empty,
			1f,
			n
		));
		var bundleTableProvider = ((LocalMeshBuilder) factory.MeshBuilder).MeshGroupAnimationTableImplProvider;
		var bundleTableNodes = new SkeletalAnimationNode[] { new(Matrix4x4.Identity, Matrix4x4.Identity, null, null) };
		TestNameStorageAndRetrieval(n => bundleTableProvider.Create(
			bundleTableNodes,
			Matrix4x4.Identity,
			new[] { skeletalMesh },
			new[] { 1 },
			new[] { Matrix4x4.Identity },
			new[] { 0 },
			n
		));
		var meshNodeIndex = 0;
		TestNameStorageAndRetrieval(n => {
			factory.MeshBuilder.SetSkeletonNodeName(skeletalMesh, meshNodeIndex, n);
			var node = skeletalMesh.Skeleton.Nodes[meshNodeIndex];
			meshNodeIndex++;
			return node;
		});
		using var font = factory.AssetLoader.LoadFont(name: TestName);
		TestNameStorageAndRetrieval(_ => font.CreatePen(ColorVect.WhiteOpaque), TestName + " pen");
		TestNameStorageAndRetrieval(_ => font.CreateString("abc"), TestName + " string \"abc\"");
		using var pen = font.CreatePen(ColorVect.WhiteOpaque);
		using var str = font.CreateString("abc");
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateTextInstance(pen, str, name: n));
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, str, name: n));
		TestNameStorageAndRetrieval(n => factory.MeshBuilder.CreateQuad(name: n));
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateQuadInstance(quadMesh, mat, name: n));
		TestNameStorageAndRetrieval(n => factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, mat, name: n));
		TestNameStorageAndRetrieval(n => factory.SceneBuilder.CreateCanvasScene(name: n));
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		TestNameStorageAndRetrieval(n => canvas.Add(tex, name: n));
		TestNameStorageAndRetrieval(_ => canvas.Add("abc", pen), "Instance of " + TestName + " string \"abc\"");
		// ReSharper restore AccessToDisposedClosure
		
		Assert.IsFalse(_resourceTypes.Any(), "Following resource types are untested: " + String.Join(", ", _resourceTypes.Select(t => t.Name)));
	}

	void TestNameStorageAndRetrieval<T>(Func<string, T> creationFunc, string? expectedName = null) where T : IResource<T> {
		expectedName ??= TestName;

		Assert.IsTrue(_resourceTypes.Remove(typeof(T)), $"Resource type '{typeof(T).Name}' not found in resource type set (types = {String.Join(", ", _resourceTypes.Select(t => t.Name))})");

		var res = creationFunc(TestName);
		var charArr = new char[expectedName.Length];
		res.CopyName(charArr);

		Assert.AreEqual(expectedName, res.GetNameAsNewStringObject());
		Assert.AreEqual(expectedName.Length, res.GetNameLength());
		Assert.AreEqual(expectedName, new String(charArr));

		if (res is not IDisposableResource disposable) return;
		disposable.Dispose();

		Assert.Catch<ObjectDisposedException>(() => res.GetNameAsNewStringObject());
		Assert.Catch<ObjectDisposedException>(() => res.GetNameLength());
		Assert.Catch<ObjectDisposedException>(() => res.CopyName(charArr));
	}
} 