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
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalResourceDirectoryTest {
	IResourceDirectory _resourceDirectory = default!;
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
		_resourceDirectory = factory.ResourceDirectory;

		
		TestDirectoryRetrieval(n => factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr), name: n));
		using (var tex = factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill(ColorVect.White), includeAlpha: false, name: "bbbbb")) {
			TestDirectoryRetrieval(n => factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex, name: n));
			using (var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f))) {
				using (var mat = factory.MaterialBuilder.CreateStandardMaterial(tex, name: "bbbbb")) {
					TestDirectoryRetrieval(n => factory.ObjectBuilder.CreateModelInstance(mesh, mat, name: n));
					TestDirectoryRetrieval(n => factory.AssetLoader.CreateModel(mesh, mat, name: n));
				}
			}
		}
		TestDirectoryRetrieval(n => factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill(ColorVect.White), includeAlpha: false, name: n));
		TestDirectoryRetrieval(n => factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f), name: n));
		TestDirectoryRetrieval(n => factory.ApplicationLoopBuilder.CreateLoop(name: n));
		if (factory.DisplayDiscoverer.Primary is { } primaryDisplay) {
			var displayName = primaryDisplay.GetNameAsNewStringObject();
			var foundDisplay = _resourceDirectory.FindByName<Display>(displayName);
			Assert.AreEqual(primaryDisplay, foundDisplay);
			TestDirectoryRetrieval(n => factory.WindowBuilder.CreateWindow(primaryDisplay, title: n));
			using var scene = factory.SceneBuilder.CreateScene();
			using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
			using var camera = factory.CameraBuilder.CreateCamera();
			TestDirectoryRetrieval(n => factory.RendererBuilder.CreateRenderer(scene, camera, window, name: n));
		}
		TestDirectoryRetrieval(n => factory.ResourceAllocator.CreateResourceGroup(false, name: n));
		TestDirectoryRetrieval(n => factory.CameraBuilder.CreateCamera(name: n));
		TestDirectoryRetrieval(n => factory.LightBuilder.CreatePointLight(name: n));
		TestDirectoryRetrieval(n => factory.LightBuilder.CreateSpotLight(name: n));
		TestDirectoryRetrieval(n => factory.LightBuilder.CreateDirectionalLight(name: n));
		TestDirectoryRetrieval(n => factory.SceneBuilder.CreateScene(name: n));
		TestDirectoryRetrieval(n => factory.RendererBuilder.CreateRenderOutputBuffer(name: n));
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
		using var skeletalMesh = factory.MeshBuilder.CreateMesh(skeletalVertices, new VertexTriangle[] { new(0, 1, 2) }, skeletalNodes);
		TestDirectoryRetrieval(n => factory.MeshBuilder.AttachAnimation(
			skeletalMesh,
			ReadOnlySpan<SkeletalAnimationScalingKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationRotationKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationTranslationKeyframe>.Empty,
			ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor>.Empty,
			1f,
			n
		));
		var meshNodeIndex = 0;
		TestDirectoryRetrieval(n => {
			factory.MeshBuilder.SetSkeletonNodeName(skeletalMesh, meshNodeIndex, n);
			var node = skeletalMesh.Skeleton.Nodes[meshNodeIndex];
			meshNodeIndex++;
			return node;
		}, expectThreeAlways: true);
		// ReSharper restore AccessToDisposedClosure
		
		Assert.IsFalse(_resourceTypes.Any(), "Following resource types are untested: " + String.Join(", ", _resourceTypes.Select(t => t.Name))); 
	}

	void TestDirectoryRetrieval<T>(Func<string, T> creationFunc, bool expectThreeAlways = false) where T : struct, IResource<T> {
		const string AppleName = "Apple";
		const string BananaName = "Banana";
		const string CarrotName = "Carrot";
		
		Assert.IsTrue(_resourceTypes.Remove(typeof(T)), $"Resource type '{typeof(T).Name}' not found in resource type set (types = {String.Join(", ", _resourceTypes.Select(t => t.Name))})"); 

		var apple = creationFunc(AppleName);
		var banana = creationFunc(BananaName);
		Assert.AreEqual(expectThreeAlways ? 3 : 2, _resourceDirectory.GetAllActiveInstances<T>().Count, String.Join(", ", _resourceDirectory.GetAllActiveInstances<T>().ToArray()));
		Assert.Contains(apple, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.Contains(banana, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		
		var dest = new T[3];

		Assert.AreEqual(apple, _resourceDirectory.FindByName<T>(AppleName));
		Assert.AreEqual(banana, _resourceDirectory.FindByName<T>(BananaName));
		Assert.AreEqual(null, _resourceDirectory.FindByName<T>(CarrotName));
		
		Assert.AreEqual(1, _resourceDirectory.FindByName(dest, AppleName));
		Assert.AreEqual(apple, dest[0]);
		Assert.AreEqual(default(T), dest[1]);
		Assert.AreEqual(default(T), dest[2]);
		Assert.AreEqual(1, _resourceDirectory.FindByName(dest, BananaName));
		Assert.AreEqual(banana, dest[0]);
		Assert.AreEqual(default(T), dest[1]);
		Assert.AreEqual(default(T), dest[2]);
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, CarrotName));
		
		var carrot = creationFunc(CarrotName);
		Assert.AreEqual(3, _resourceDirectory.GetAllActiveInstances<T>().Count);
		Assert.Contains(apple, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.Contains(banana, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.Contains(carrot, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.AreEqual(carrot, _resourceDirectory.FindByName<T>(CarrotName));
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: false, StringComparison.OrdinalIgnoreCase));
		Assert.AreEqual(3, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: true, StringComparison.OrdinalIgnoreCase));
		Assert.IsTrue(dest.Contains(apple));
		Assert.IsTrue(dest.Contains(banana));
		Assert.IsTrue(dest.Contains(carrot));
		Array.Clear(dest);
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: false, StringComparison.Ordinal));
		Assert.AreEqual(2, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: true, StringComparison.Ordinal));
		Assert.IsFalse(dest.Contains(apple));
		Assert.IsTrue(dest.Contains(banana));
		Assert.IsTrue(dest.Contains(carrot));
		Assert.AreEqual(default(T), dest[2]);
		Array.Clear(dest);
		
		if (apple is not IDisposable disposableApple || banana is not IDisposable disposableBanana || carrot is not IDisposable disposableCarrot) return;
		disposableCarrot.Dispose();
		Assert.AreEqual(2, _resourceDirectory.GetAllActiveInstances<T>().Count);
		Assert.Contains(apple, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.Contains(banana, _resourceDirectory.GetAllActiveInstances<T>().ToArray());
		Assert.AreEqual(1, _resourceDirectory.FindByName(dest, AppleName));
		Assert.AreEqual(apple, dest[0]);
		Assert.AreEqual(default(T), dest[1]);
		Assert.AreEqual(default(T), dest[2]);
		Assert.AreEqual(1, _resourceDirectory.FindByName(dest, BananaName));
		Assert.AreEqual(banana, dest[0]);
		Assert.AreEqual(default(T), dest[1]);
		Assert.AreEqual(default(T), dest[2]);
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, CarrotName));
		
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: false, StringComparison.OrdinalIgnoreCase));
		Assert.AreEqual(2, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: true, StringComparison.OrdinalIgnoreCase));
		Assert.IsTrue(dest.Contains(apple));
		Assert.IsTrue(dest.Contains(banana));
		Assert.IsFalse(dest.Contains(carrot));
		Assert.AreEqual(default(T), dest[2]);
		Array.Clear(dest);
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: false, StringComparison.Ordinal));
		Assert.AreEqual(1, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: true, StringComparison.Ordinal));
		Assert.IsFalse(dest.Contains(apple));
		Assert.IsTrue(dest.Contains(banana));
		Assert.IsFalse(dest.Contains(carrot));
		Assert.AreEqual(default(T), dest[1]);
		Assert.AreEqual(default(T), dest[2]);
		Array.Clear(dest);
		
		disposableBanana.Dispose();
		disposableApple.Dispose();
		Assert.AreEqual(0, _resourceDirectory.GetAllActiveInstances<T>().Count);
		Assert.AreEqual(0, _resourceDirectory.FindByName(dest, "a", allowPartialMatch: true, StringComparison.OrdinalIgnoreCase));
		Assert.AreEqual(null, _resourceDirectory.FindByName<T>(AppleName));
		Assert.AreEqual(null, _resourceDirectory.FindByName<T>(BananaName));
		Assert.AreEqual(null, _resourceDirectory.FindByName<T>(CarrotName));
	}
} 