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
class LocalDisposalProtectionTest {
	const string SkyboxFile = "IntegrationTests\\kloofendal_48d_partly_cloudy_puresky_4k.hdr";

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		var factory = new LocalTinyFfrFactory();

		var displayDiscoverer = factory.DisplayDiscoverer;
		var recommendedDisplay = displayDiscoverer.Recommended!.Value;
		var windowBuilder = factory.WindowBuilder;
		var window = windowBuilder.CreateWindow(recommendedDisplay, size: XYPair<int>.One);
		var windowActions = new Action<Window>[] {
			v => _ = v.Handle,
			v => _ = v.Title,
			v => _ = v.Display,
			v => _ = v.FullscreenStyle,
			v => _ = v.LockCursor,
			v => _ = v.Position,
			v => _ = v.Size,
			v => v.Display = displayDiscoverer.Recommended!.Value,
			v => v.FullscreenStyle = WindowFullscreenStyle.NotFullscreen,
			v => v.LockCursor = false,
			v => v.Position = XYPair<int>.Zero,
			v => v.Size = XYPair<int>.One,
			v => v.Title = "test"
		};
		AssertUseAfterDisposalThrowsException(windowBuilder.CreateWindow(recommendedDisplay, size: XYPair<int>.One), objectIsAlreadyDisposed: false, windowActions);
		var loopBuilder = factory.ApplicationLoopBuilder;
		var loop = loopBuilder.CreateLoop();
		var loopActions = new Action<ApplicationLoop>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => _ = v.Input,
			v => _ = v.TimeUntilNextIteration,
			v => _ = v.TotalIteratedTime,
			v => _ = v.IterateOnce(),
			v => _ = v.TryIterateOnce(out _)
		};
		AssertUseAfterDisposalThrowsException(loopBuilder.CreateLoop(), objectIsAlreadyDisposed: false, loopActions);
		var cameraBuilder = factory.CameraBuilder;
		var camera = cameraBuilder.CreateCamera();
		var cameraActions = new Action<Camera>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => _ = v.FarPlaneDistance,
			v => _ = v.GetModelMatrix(),
			v => v.GetModelMatrix(out _),
			v => _ = v.GetProjectionMatrix(),
			v => v.GetProjectionMatrix(out _),
			v => _ = v.GetViewMatrix(),
			v => v.GetViewMatrix(out _),
			v => _ = v.HorizontalFieldOfView,
			v => v.MoveBy(Vect.One),
			v => _ = v.NearPlaneDistance,
			v => _ = v.Position,
			v => v.RotateBy(Rotation.None),
			v => v.SetModelMatrix(new Matrix4x4()),
			v => v.SetProjectionMatrix(new Matrix4x4()),
			v => v.SetViewMatrix(new Matrix4x4()),
			v => _ = v.UpDirection,
			v => _ = v.VerticalFieldOfView,
			v => _ = v.ViewDirection,
			v => v.FarPlaneDistance = 1f,
			v => v.HorizontalFieldOfView = 1f,
			v => v.NearPlaneDistance = 0.1f,
			v => v.Position = Location.Origin,
			v => v.UpDirection = Direction.Up,
			v => v.VerticalFieldOfView = 1f,
			v => v.ViewDirection = Direction.Forward
		};
		AssertUseAfterDisposalThrowsException(cameraBuilder.CreateCamera(), objectIsAlreadyDisposed: false, cameraActions);
		var assetLoader = factory.AssetLoader;
		var meshBuilder = assetLoader.MeshBuilder;
		var mesh = meshBuilder.CreateMesh(new Cuboid(1f));
		var meshActions = new Action<Mesh>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => _ = v.BufferData
		};
		AssertUseAfterDisposalThrowsException(meshBuilder.CreateMesh(new Cuboid(1f)), objectIsAlreadyDisposed: false, meshActions);
		var materialBuilder = assetLoader.MaterialBuilder;
		var texture = materialBuilder.CreateColorMap(StandardColor.RealWorldBrick);
		var textureActions = new Action<Texture>[] {
			v => _ = v.Handle,
			v => _ = v.Name
		};
		AssertUseAfterDisposalThrowsException(materialBuilder.CreateColorMap(StandardColor.RealWorldBrick), objectIsAlreadyDisposed: false, textureActions);
		var material = materialBuilder.CreateOpaqueMaterial(texture);
		var materialActions = new Action<Material>[] {
			v => _ = v.Handle,
			v => _ = v.Name
		};
		AssertUseAfterDisposalThrowsException(materialBuilder.CreateOpaqueMaterial(texture), objectIsAlreadyDisposed: false, materialActions);
		var objectBuilder = factory.ObjectBuilder;
		var modelInstance = objectBuilder.CreateModelInstance(mesh, material);
		var modelInstanceActions = new Action<ModelInstance>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => v.AdjustScaleBy(Vect.One),
			v => v.AdjustScaleBy(1f),
			v => _ = v.Material,
			v => _ = v.Mesh,
			v => v.MoveBy(Vect.One),
			v => _ = v.Position,
			v => v.RotateBy(Rotation.None),
			v => _ = v.Rotation,
			v => v.ScaleBy(Vect.One),
			v => v.ScaleBy(1f),
			v => _ = v.Scaling,
			v => _ = v.Transform,
			v => v.Material = material,
			v => v.Mesh = mesh,
			v => v.Position = Location.Origin,
			v => v.Rotation = Rotation.None,
			v => v.Scaling = Vect.One,
			v => v.Transform = Transform.None
		};
		AssertUseAfterDisposalThrowsException(objectBuilder.CreateModelInstance(mesh, material), objectIsAlreadyDisposed: false, modelInstanceActions);
		var sceneBuilder = factory.SceneBuilder;
		var scene = sceneBuilder.CreateScene();
		var sceneActions = new Action<Scene>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => v.Add(modelInstance),
			v => v.Remove(modelInstance)
		};
		AssertUseAfterDisposalThrowsException(sceneBuilder.CreateScene(), objectIsAlreadyDisposed: false, sceneActions);
		var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(SkyboxFile);
		var cubemapActions = new Action<EnvironmentCubemap>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => _ = v.SkyboxTextureHandle,
			v => _ = v.IndirectLightingTextureHandle,
		};
		AssertUseAfterDisposalThrowsException(factory.AssetLoader.LoadEnvironmentCubemap(SkyboxFile), objectIsAlreadyDisposed: false, cubemapActions);
		var rendererBuilder = factory.RendererBuilder;
		var renderer = rendererBuilder.CreateRenderer(scene, camera, window);
		var rendererActions = new Action<Renderer>[] {
			v => _ = v.Handle,
			v => _ = v.Name,
			v => v.Render()
		};
		AssertUseAfterDisposalThrowsException(rendererBuilder.CreateRenderer(scene, camera, window), objectIsAlreadyDisposed: false, rendererActions);



		// === Factory disposed here === 
		AssertUseAfterDisposalThrowsException(
			factory, objectIsAlreadyDisposed: false,
			v => _ = v.ApplicationLoopBuilder,
			v => _ = v.AssetLoader,
			v => _ = v.CameraBuilder,
			v => _ = v.DisplayDiscoverer,
			v => _ = v.ObjectBuilder,
			v => _ = v.RendererBuilder,
			v => _ = v.SceneBuilder,
			v => _ = v.WindowBuilder,
			v => _ = v.ResourceAllocator.CreateResourceGroup(true),
			v => _ = v.ResourceAllocator.CreateResourceGroup(true, "test"),
			v => _ = v.ResourceAllocator.CreateResourceGroup(true, "test", 3),
			v => _ = v.ResourceAllocator.CreateResourceGroup(true, 3)
		);

		// === Now check everything we created above has been auto-disposed
		AssertUseAfterDisposalThrowsException(window, objectIsAlreadyDisposed: true, windowActions);
		AssertUseAfterDisposalThrowsException(loop, objectIsAlreadyDisposed: true, loopActions);
		AssertUseAfterDisposalThrowsException(camera, objectIsAlreadyDisposed: true, cameraActions);
		AssertUseAfterDisposalThrowsException(mesh, objectIsAlreadyDisposed: true, meshActions);
		AssertUseAfterDisposalThrowsException(texture, objectIsAlreadyDisposed: true, textureActions);
		AssertUseAfterDisposalThrowsException(material, objectIsAlreadyDisposed: true, materialActions);
		AssertUseAfterDisposalThrowsException(modelInstance, objectIsAlreadyDisposed: true, modelInstanceActions);
		AssertUseAfterDisposalThrowsException(scene, objectIsAlreadyDisposed: true, sceneActions);
		AssertUseAfterDisposalThrowsException(cubemap, objectIsAlreadyDisposed: true, cubemapActions);
		AssertUseAfterDisposalThrowsException(renderer, objectIsAlreadyDisposed: true, rendererActions);

		AssertUseAfterDisposalThrowsException(
			displayDiscoverer, objectIsAlreadyDisposed: true,
			v => _ = v.All,
			v => _ = v.Primary,
			v => _ = v.Recommended
		);
		AssertUseAfterDisposalThrowsException(
			recommendedDisplay, objectIsAlreadyDisposed: true,
			v => _ = v.Handle,
			v => _ = v.Name,
			v => _ = v.CurrentResolution,
			v => _ = v.GlobalPositionOffset,
			v => _ = v.HighestSupportedRefreshRateMode,
			v => _ = v.HighestSupportedResolutionMode,
			v => _ = v.IsPrimary,
			v => _ = v.IsRecommended,
			v => _ = v.SupportedDisplayModes,
			v => _ = v.TranslateDisplayLocalWindowPositionToGlobal(XYPair<int>.Zero),
			v => _ = v.TranslateGlobalWindowPositionToDisplayLocal(XYPair<int>.Zero)
		);
		AssertUseAfterDisposalThrowsException(
			windowBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateWindow(new WindowConfig { Display = default })
		);
		AssertUseAfterDisposalThrowsException(
			loopBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateLoop()
		);
		AssertUseAfterDisposalThrowsException(
			cameraBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateCamera()
		);
		AssertUseAfterDisposalThrowsException(
			assetLoader, objectIsAlreadyDisposed: true,
			v => _ = v.MeshBuilder,
			v => _ = v.MaterialBuilder
		);
		AssertUseAfterDisposalThrowsException(
			meshBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateMesh(new Cuboid(1f))
		);
		AssertUseAfterDisposalThrowsException(
			materialBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateColorMap(StandardColor.RealWorldBrick)
		);
		AssertUseAfterDisposalThrowsException(
			objectBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateModelInstance(mesh, material),
			v => _ = v.CreateModelInstance(default(Mesh), default(Material))
		);
		AssertUseAfterDisposalThrowsException(
			sceneBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateScene()
		);
		AssertUseAfterDisposalThrowsException(
			rendererBuilder, objectIsAlreadyDisposed: true,
			v => _ = v.CreateRenderer(scene, camera, window),
			v => _ = v.CreateRenderer(default, default, default)
		);
	}

	void AssertUseAfterDisposalThrowsException<T>(T disposable, bool objectIsAlreadyDisposed, params Action<T>[] usageActions) {
		if (!objectIsAlreadyDisposed) {
			for (var i = 0; i < usageActions.Length; i++) {
				try {
					Assert.DoesNotThrow(() => usageActions[i](disposable));
				}
				catch {
					Console.WriteLine($"Exception thrown before disposal for {disposable}. Action index: {i}");
					throw;
				}
			}

			(disposable as IDisposable)?.Dispose();
		}
		
		if (disposable is IDisposable d) Assert.DoesNotThrow(d.Dispose);

		try {
#pragma warning disable CS8602 // Dereference of a possibly null reference -- seems like a compiler error?
			Assert.DoesNotThrow(() => _ = disposable.ToString());
			// ReSharper disable once EqualExpressionComparison
			Assert.IsTrue(disposable.Equals(disposable));
#pragma warning restore CS8602
		}
		catch {
			Console.WriteLine($"Unexpected exception when invoking ToString() or Equals() for disposed object of type {typeof(T).Name}.");
			throw;
		}

		for (var i = 0; i < usageActions.Length; i++) {
			try {
				Assert.Catch<ObjectDisposedException>(() => usageActions[i](disposable));
			}
			catch {
				Console.WriteLine($"Exception not thrown after disposal for {disposable}. Action index: {i}");
				throw;
			}
		}
	}
} 