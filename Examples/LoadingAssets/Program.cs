using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

var backdropEnabled = false;

using var factory = new LocalTinyFfrFactory();
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
using var camera = factory.CameraBuilder.CreateCamera();
using var cameraController = camera.CreateController<InspectorCameraController>();
var lights = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);

Console.WriteLine("Loading metro backdrop + chess set; this may take a few seconds...");
using var backdrop = factory.AssetLoader.LoadBakedBackdropTexture("metro_noord_4k.tffr");
using var chessBundle = factory.AssetLoader.LoadBundledAsset("ABeautifulGame.glb");
using var chessInstances = factory.ObjectBuilder.CreateModelInstances(chessBundle);

using var scene = factory.SceneBuilder.CreateScene();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop();

window.LockCursor = true;
cameraController.SetConstraints(chessBundle.CalculateCombinedBoundingBox());
scene.Add(chessInstances);

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	var kbm = loop.Input.KeyboardAndMouse;

	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) break;

	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.B)) {
		backdropEnabled = !backdropEnabled;
		if (backdropEnabled) scene.SetBackdrop(backdrop);
		else scene.SetBackdrop(SceneCreationConfig.DefaultInitialBackdropColor);
	}

	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.P)) {
		var pointLight = factory.LightBuilder.CreatePointLight(
			position: camera.Position,
			color: ColorVect.RandomOpaque().WithSaturation(0.35f),
			brightnessPreset: PointLightBrightnessPreset.BulbTypical,
			castsShadows: true
		);
		lights.Add(pointLight);
		scene.Add(pointLight);
		scene.AddPrimitivePoint(
			pointLight.Position, 
			new PrimitivePaintbrush(pointLight.Color.WithSaturation(1f))
		);
	}

	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.S)) {
		var spotLight = factory.LightBuilder.CreateSpotLight(
			position: camera.Position, 
			coneDirection: camera.ViewDirection, 
			brightnessPreset: SpotLightBrightnessPreset.DeskLamp,
			color: StandardColor.LightingIncandescentBulb,
			highQuality: true,
			castsShadows: true
		);
		lights.Add(spotLight);
		scene.Add(spotLight);
		scene.AddPrimitiveArrow(
			spotLight.Position, 
			spotLight.ConeDirection, 
			new PrimitivePaintbrush(spotLight.Color)
		);
	}
	
	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.D)) {
		scene.RemoveAll(includeModelInstances: false, includeLights: true, includePrimitives: true);
		lights.Dispose();
		lights = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
	}

	cameraController.AdjustAllViaDefaultControls(kbm, deltaTime);
	cameraController.Progress(deltaTime);

	renderer.Render();
}

scene.RemoveAll();
lights.Dispose();
