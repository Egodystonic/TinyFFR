using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;
using System.Reflection;
using System.Runtime.InteropServices;

#pragma warning disable IDE0160 // ReSharper disable once CheckNamespace
namespace Egodystonic.TinyFFR.Testing.Local;
#pragma warning restore IDE0160

static partial class TestMain {
	static ILocalTinyFfrFactory Factory => TestScaffold.TestObjects.Factory;
	static IWindowBuilder WindowBuilder => Factory.WindowBuilder;
	static ILocalAssetLoader AssetLoader => Factory.AssetLoader;
	static IApplicationLoopBuilder ApplicationLoopBuilder => Factory.ApplicationLoopBuilder;
	static IDisplayDiscoverer DisplayDiscoverer => Factory.DisplayDiscoverer;
	static IMeshBuilder MeshBuilder => Factory.MeshBuilder;
	static IMaterialBuilder MaterialBuilder => Factory.MaterialBuilder;
	static ICameraBuilder CameraBuilder => Factory.CameraBuilder;
	static ILightBuilder LightBuilder => Factory.LightBuilder;
	static IObjectBuilder ObjectBuilder => Factory.ObjectBuilder;
	static ISceneBuilder SceneBuilder => Factory.SceneBuilder;
	static IRendererBuilder RendererBuilder => Factory.RendererBuilder;
	static IResourceAllocator ResourceAllocator => Factory.ResourceAllocator;

	static Window Window => TestScaffold.TestObjects.Window ?? throw new InvalidOperationException("No window created for this test.");
	static Scene Scene => TestScaffold.TestObjects.Scene ?? throw new InvalidOperationException("No scene created for this test.");
	static EnvironmentCubemap SkyBackdrop => TestScaffold.TestObjects.SkyBackdrop ?? throw new InvalidOperationException("No sky backdrop added for this test.");
	static ModelInstance Cube => TestScaffold.TestObjects.Cube ?? throw new InvalidOperationException("No cube added for this test.");
	static DirectionalLight Sunlight => TestScaffold.TestObjects.Sunlight ?? throw new InvalidOperationException("No sunlight added for this test.");
	static Camera Camera => TestScaffold.TestObjects.Camera ?? throw new InvalidOperationException("No camera added for this test.");
	static Renderer Renderer => TestScaffold.TestObjects.Renderer ?? throw new InvalidOperationException("No renderer added for this test.");
	static ApplicationLoop Loop => TestScaffold.TestObjects.Loop ?? throw new InvalidOperationException("No loop added for this test.");

	static TDisposable DisposeAtTestEnd<TDisposable>(this TDisposable @this) where TDisposable : IDisposable {
		TestScaffold.TestObjects.Disposables.Add(@this);
		return @this;
	}

	static void ExitTest() => TestScaffold.ExitTest();
}