using System.Diagnostics;
using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using Egodystonic.TinyFFR.Environment;

// TODO make this a little better. Maybe make it a little framework and ignore the actual "meat" file
NativeLibrary.SetDllImportResolver( // Yeah this is ugly af but it'll do for v1
	typeof(LocalTinyFfrFactory).Assembly,
	(libName, assy, searchPath) => {
		var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		while (curDir != null) {
			var containedDirectories = Directory.GetDirectories(curDir);
			if (containedDirectories.Any(d => Path.GetFileName(d)?.Equals("build_output", StringComparison.OrdinalIgnoreCase) ?? false)) {
#if DEBUG
				const string BuildConfig = "Debug";
#else
				const string BuildConfig = "Release";
#endif
				var expectedFilePath = Path.Combine(curDir, "build_output", BuildConfig, libName);
				foreach (var possibleFilePath in new string[] { ".dll", ".lib", ".so" }.Concat([""]).Select(ext => expectedFilePath + ext)) {
					if (File.Exists(possibleFilePath)) return NativeLibrary.Load(possibleFilePath, assy, searchPath);
				}

				return IntPtr.Zero;
			}

			curDir = Directory.GetParent(curDir)?.FullName;
		}
		return IntPtr.Zero;
	}
);

























// using var factory = new LocalTinyFfrFactory();
// var assLoad = factory.AssetLoader;
// var meshBuilder = factory.AssetLoader.MeshBuilder;
//
// // var verticesMemory = factory.ResourceAllocator.CreatePooledMemoryBuffer<MeshVertex>(4);
// // var trianglesMemory = factory.ResourceAllocator.CreatePooledMemoryBuffer<VertexTriangle>(2);
// // var vertices = verticesMemory.Span;
// // var triangles = trianglesMemory.Span;
// //
// // vertices[0] = new MeshVertex(
// // 	location: (0.5f, -0.5f, 0f),
// // 	textureCoords: (0f, 0f),
// // 	tangent: Direction.Right,
// // 	bitangent: Direction.Up,
// // 	normal: Direction.Backward
// // );
// // vertices[1] = new MeshVertex(
// // 	location: (-0.5f, -0.5f, 0f),
// // 	textureCoords: (1f, 0f),
// // 	tangent: Direction.Right,
// // 	bitangent: Direction.Up,
// // 	normal: Direction.Backward
// // );
// // vertices[2] = new MeshVertex(
// // 	location: (-0.5f, 0.5f, 0f),
// // 	textureCoords: (1f, 1f),
// // 	tangent: Direction.Right,
// // 	bitangent: Direction.Up,
// // 	normal: Direction.Backward
// // );
// // vertices[3] = new MeshVertex(
// // 	location: (0.5f, 0.5f, 0f),
// // 	textureCoords: (0f, 1f),
// // 	tangent: Direction.Right,
// // 	bitangent: Direction.Up,
// // 	normal: Direction.Backward
// // );
// //
// // triangles[0] = new(0, 1, 2);
// // triangles[1] = new(2, 3, 0);
// //
// // using var mesh = meshBuilder.CreateMesh(
// // 	vertices,
// // 	triangles,
// // 	new MeshCreationConfig { /* specify creation options here */ }
// // );
// //
// // factory.ResourceAllocator.ReturnPooledMemoryBuffer(trianglesMemory);
// // factory.ResourceAllocator.ReturnPooledMemoryBuffer(verticesMemory);
//
//
// using var mesh = assLoad.LoadMesh(@"C:\Users\ben\Documents\Temp\treasure_chest\treasure_chest_4k.gltf");
// using var colorMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_diff_4k.jpg");
// using var normalMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_nor_gl_4k.jpg");
// using var ormMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_arm_4k.jpg");
// using var material = assLoad.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap);
// using var modelInstance = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: (0f, -0.3f, 0f));
//
// using var hdr = assLoad.LoadEnvironmentCubemap(@"C:\Users\ben\Documents\Temp\treasure_chest\belfast_sunset_puresky_4k.hdr");
// using var scene = factory.SceneBuilder.CreateScene();
//
// scene.Add(modelInstance);
// scene.Add(modelInstance);
// scene.Remove(modelInstance);
// scene.Remove(modelInstance);
// scene.Add(modelInstance);
// scene.SetBackdrop(hdr);
//
// var cameraDistance = 3f;
// var chestToCameraStartVect = Direction.Backward * cameraDistance;
// using var camera = factory.CameraBuilder.CreateCamera(
// 	initialPosition: Location.Origin + chestToCameraStartVect, 
// 	initialViewDirection: Direction.Forward
// );
//
// using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
// using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
// using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
//
// window.LockCursor = true;
//
// //using var window2 = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value, position: (0, 0));
// //using var renderer2 = factory.RendererBuilder.CreateRenderer(scene, camera, window2, new RendererCreationConfig { GpuSynchronizationFrameBufferCount = -1 });
//
// var spotlight = factory.LightBuilder.CreateSpotLight(new SpotLightCreationConfig {
// 	InitialColor = ColorVect.FromHueSaturationLightness(0f, 1f, 0.8f),
// 	InitialBrightness = 0.3f,
// 	IsHighQuality = false
// });
// scene.Add(spotlight);
// scene.Add(spotlight);
// scene.Remove(spotlight);
// scene.Remove(spotlight);
// scene.Add(spotlight);
// var parameter = 0;
//
// var sunlight = factory.LightBuilder.CreateDirectionalLight(color: StandardColor.LightingSunRiseSet, showSunDisc: true);
// scene.Add(sunlight);
// var sunlightDiscConfig = new SunDiscConfig();
//
// var frameCount = 0;
// var startTime = Stopwatch.StartNew();
// while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
// 	var sw = Stopwatch.StartNew();
// 	var deltaTime = (float) loop.IterateOnce().TotalSeconds;
//
// 	CameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime);
// 	CameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);
//
// 	// using var testCubeMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
// 	// using var testCubeMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.White);
// 	// using var testCubeMat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(testCubeMap);
// 	// using var testCube = factory.ObjectBuilder.CreateModelInstance(testCubeMesh, testCubeMat, initialPosition: camera.Position + camera.ViewDirection * 2f);
// 	// scene.Add(testCube);
//
// 	//renderer2.Render();
// 	spotlight.Position = camera.Position;
// 	spotlight.ConeDirection = camera.ViewDirection;
// 	renderer.Render();
//
// 	if (loop.Input.GameControllersCombined.ButtonIsCurrentlyDown(GameControllerButton.A)) {
// 		spotlight.AdjustColorHueBy(30f * deltaTime);
// 	}
// 	if (loop.Input.GameControllersCombined.ButtonIsCurrentlyDown(GameControllerButton.LeftBumper)) {
// 		spotlight.ConeAngle += 10f * deltaTime;
// 	}
// 	if (loop.Input.GameControllersCombined.ButtonIsCurrentlyDown(GameControllerButton.RightBumper)) {
// 		spotlight.ConeAngle -= 10f * deltaTime;
// 	}
// 	if (loop.Input.GameControllersCombined.ButtonIsCurrentlyDown(GameControllerButton.Y)) {
// 		spotlight.IntenseBeamAngle = spotlight.ConeAngle * 0.5f;
// 	}
//
//
// 	var parameterBefore = parameter;
// 	var delta = 0;
// 	foreach (var key in loop.Input.KeyboardAndMouse.NewKeyDownEvents) {
// 		parameter = key.GetNumericValue() ?? parameterBefore;
// 		if (parameter < 0 || parameter > 4) parameter = parameterBefore;
// 		if (key == KeyboardOrMouseKey.U) delta += 1;
// 		if (key == KeyboardOrMouseKey.D) delta -= 1;
//
// 		if (key == KeyboardOrMouseKey.Space) sunlight.Direction = camera.ViewDirection;
// 		if (key == KeyboardOrMouseKey.N) {
// 			sunlight.Brightness -= 0.25f;
// 			Console.WriteLine(sunlight.Brightness);
// 		}
// 		if (key == KeyboardOrMouseKey.M) {
// 			sunlight.Brightness += 0.25f;
// 			Console.WriteLine(sunlight.Brightness);
// 		}
//
// 		if (key == KeyboardOrMouseKey.F2) {
// 			sunlightDiscConfig = sunlightDiscConfig with { Scaling = sunlightDiscConfig.Scaling + 1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 		if (key == KeyboardOrMouseKey.F1) {
// 			sunlightDiscConfig = sunlightDiscConfig with { Scaling = sunlightDiscConfig.Scaling - 1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 		if (key == KeyboardOrMouseKey.F4) {
// 			sunlightDiscConfig = sunlightDiscConfig with { FringingScaling = sunlightDiscConfig.FringingScaling + 0.1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 		if (key == KeyboardOrMouseKey.F3) {
// 			sunlightDiscConfig = sunlightDiscConfig with { FringingScaling = sunlightDiscConfig.FringingScaling - 0.1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 		if (key == KeyboardOrMouseKey.F6) {
// 			sunlightDiscConfig = sunlightDiscConfig with { FringingOuterRadiusScaling = sunlightDiscConfig.FringingOuterRadiusScaling + 0.1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 		if (key == KeyboardOrMouseKey.F5) {
// 			sunlightDiscConfig = sunlightDiscConfig with { FringingOuterRadiusScaling = sunlightDiscConfig.FringingOuterRadiusScaling - 0.1f };
// 			Console.WriteLine(sunlightDiscConfig);
// 			sunlight.SetSunDiscParameters(sunlightDiscConfig);
// 		}
// 	}
// 	var parameterName = parameter switch {
// 		0 => "Max Illumination Distance",
// 		1 => "Cone Angle",
// 		2 => "Beam Angle",
// 		3 => "Color Hue",
// 		4 => "Brightness"
// 	};
// 	if (parameter != parameterBefore) {
// 		Console.WriteLine("Switch parameter to: " + parameterName);
// 	}
// 	if (delta != 0) {
// 		var adjustmentValueStr = "";
//
// 		switch (parameter) {
// 			case 0:
// 				spotlight.MaxIlluminationDistance += delta * 1f;
// 				adjustmentValueStr = "1m = " + spotlight.MaxIlluminationDistance.ToString("N1") + "m";
// 				break;
// 			case 1:
// 				spotlight.ConeAngle += delta * 10f;
// 				adjustmentValueStr = "10° = " + spotlight.ConeAngle;
// 				break;
// 			case 2:
// 				spotlight.IntenseBeamAngle += delta * 10f;
// 				adjustmentValueStr = "10° = " + spotlight.IntenseBeamAngle;
// 				break;
// 			case 3:
// 				spotlight.AdjustColorHueBy(delta * 30f);
// 				adjustmentValueStr = "30° = " + spotlight.ColorHue;
// 				break;
// 			case 4:
// 				spotlight.Brightness += delta * 0.5f;
// 				adjustmentValueStr = "0.5 = " + spotlight.Brightness.ToString("N1");
// 				break;
// 		}
//
// 		Console.WriteLine(parameterName + " " + (delta > 0 ? "+" : "-") + adjustmentValueStr);
// 		Console.WriteLine(spotlight.ConeAngle + " > " + spotlight.IntenseBeamAngle);
// 	}
//
//
//
//
//
// 	//scene.Remove(testCube);
// 	if (sw.ElapsedMilliseconds > 20) Console.WriteLine("EM: " + sw.ElapsedMilliseconds + "; " + "Dt: " + deltaTime);
// 	++frameCount;
// }
// Console.WriteLine("Avg FPS: " + (frameCount / startTime.Elapsed.TotalSeconds).ToString("N0") + " (" + frameCount + " frames over " + startTime.Elapsed.TotalSeconds.ToString("N1") + " seconds)");
//
// static class CameraInputHandler {
// 	const float CameraMovementSpeed = 1f;
// 	static Angle _currentHorizontalAngle = Angle.Zero;
// 	static Angle _currentVerticalAngle = Angle.Zero;
// 	static Direction _currentHorizontalPlaneDir = Direction.Forward;
//
// 	public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
// 		AdjustCameraViewDirectionKbm(input, camera, deltaTime);
// 		AdjustCameraPositionKbm(input, camera, deltaTime);
// 	}
//
// 	public static void TickGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
// 		AdjustCameraViewDirectionGamepad(input, camera, deltaTime);
// 		AdjustCameraPositionGamepad(input, camera, deltaTime);
// 	}
//
// 	static void AdjustCameraViewDirectionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
// 		const float MouseSensitivity = 0.05f;
//
// 		var cursorDelta = input.MouseCursorDelta;
// 		_currentHorizontalAngle += cursorDelta.X * MouseSensitivity;
// 		_currentVerticalAngle += cursorDelta.Y * MouseSensitivity;
//
// 		_currentHorizontalAngle = _currentHorizontalAngle.Normalized;
// 		_currentVerticalAngle = _currentVerticalAngle.Clamp(-Angle.QuarterCircle, Angle.QuarterCircle);
//
// 		_currentHorizontalPlaneDir = Direction.Forward * (_currentHorizontalAngle % Direction.Down);
// 		var verticalTiltRot = _currentVerticalAngle % Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);
//
// 		camera.SetViewAndUpDirection(_currentHorizontalPlaneDir * verticalTiltRot, Direction.Up * verticalTiltRot);
// 	}
//
// 	static void AdjustCameraPositionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
// 		var positiveHorizontalYDir = camera.ViewDirection;
// 		var positiveHorizontalXDir = Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);
//
// 		var horizontalMovement = XYPair<float>.Zero;
// 		var verticalMovement = 0f;
// 		foreach (var currentKey in input.CurrentlyPressedKeys) {
// 			switch (currentKey) {
// 				case KeyboardOrMouseKey.ArrowLeft:
// 					horizontalMovement += (1f, 0f);
// 					break;
// 				case KeyboardOrMouseKey.ArrowRight:
// 					horizontalMovement += (-1f, 0f);
// 					break;
// 				case KeyboardOrMouseKey.ArrowUp:
// 					horizontalMovement += (0f, 1f);
// 					break;
// 				case KeyboardOrMouseKey.ArrowDown:
// 					horizontalMovement += (0f, -1f);
// 					break;
// 				case KeyboardOrMouseKey.RightControl:
// 					verticalMovement -= 1f;
// 					break;
// 				case KeyboardOrMouseKey.RightShift:
// 					verticalMovement += 1f;
// 					break;
// 			}
// 		}
//
// 		var horizontalMovementVect = (positiveHorizontalXDir * horizontalMovement.X) + (positiveHorizontalYDir * horizontalMovement.Y);
// 		var verticalMovementVect = Direction.Up * verticalMovement;
// 		var sumMovementVect = (horizontalMovementVect + verticalMovementVect).WithLength(CameraMovementSpeed * deltaTime);
// 		camera.MoveBy(sumMovementVect);
// 	}
//
// 	static void AdjustCameraViewDirectionGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
// 		const float StickSensitivity = 100f;
//
// 		var horizontalRotationStrength = input.RightStickPosition.GetDisplacementHorizontalWithDeadzone();
// 		var verticalRotationStrength = input.RightStickPosition.GetDisplacementVerticalWithDeadzone();
//
// 		_currentHorizontalAngle += StickSensitivity * horizontalRotationStrength * deltaTime;
// 		_currentHorizontalAngle = _currentHorizontalAngle.Normalized;
//
// 		_currentVerticalAngle -= StickSensitivity * verticalRotationStrength * deltaTime;
// 		_currentVerticalAngle = _currentVerticalAngle.Clamp(-Angle.QuarterCircle, Angle.QuarterCircle);
//
// 		_currentHorizontalPlaneDir = Direction.Forward * (_currentHorizontalAngle % Direction.Down);
// 		var verticalTiltRot = _currentVerticalAngle % Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);
//
// 		camera.SetViewAndUpDirection(_currentHorizontalPlaneDir * verticalTiltRot, Direction.Up * verticalTiltRot);
// 	}
//
// 	static void AdjustCameraPositionGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
// 		var verticalMovementMultiplier = input.RightTriggerPosition.GetDisplacementWithDeadzone() - input.LeftTriggerPosition.GetDisplacementWithDeadzone();
// 		var verticalMovementVect = verticalMovementMultiplier * Direction.Up;
//
// 		var horizontalMovementVect = Vect.Zero;
// 		var stickDisplacement = input.LeftStickPosition.GetDisplacementWithDeadzone();
// 		var stickAngle = input.LeftStickPosition.GetPolarAngle();
//
// 		if (stickAngle is { } horizontalMovementAngle) {
// 			var horizontalMovementDir = _currentHorizontalPlaneDir * (Direction.Up % (horizontalMovementAngle - Angle.QuarterCircle));
// 			horizontalMovementVect = horizontalMovementDir * stickDisplacement;
// 		}
//
//
// 		var sumMovementVect = (horizontalMovementVect + verticalMovementVect).WithMaxLength(1f) * CameraMovementSpeed * deltaTime;
// 		camera.MoveBy(sumMovementVect);
// 	}
// }


const int GridSize = 3;
const int HalfGridSize = GridSize / 2;
const float CubeSize = 0.35f;
Location FloorPosition = new(0f, -(HalfGridSize + CubeSize), 0f);


using var factory = new LocalTinyFfrFactory();
var display = factory.DisplayDiscoverer.Recommended!.Value;
using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Shadows Test", size: (1920, 1080), position: (100, 100));
using var camera = factory.CameraBuilder.CreateCamera();
using var scene = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.Black);
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(CubeSize));
using var cubeMat = factory.MaterialBuilder.CreateOpaqueMaterial(
	colorMap: factory.MaterialBuilder.CreateColorMap(StandardColor.White)
);

var cubeList = factory.ResourceAllocator.CreateNewArrayPoolBackedList<ModelInstance>();

for (var x = -HalfGridSize; x <= HalfGridSize; ++x) {
	for (var y = -HalfGridSize; y <= HalfGridSize; ++y) {
		for (var z = -HalfGridSize; z <= HalfGridSize; ++z) {
			var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMat, initialPosition: new(x, y, z));
			scene.Add(cube);
			cubeList.Add(cube);
		}
	}
}

using var floor = factory.ObjectBuilder.CreateModelInstance(
	cubeMesh,
	cubeMat,
	initialPosition: new(0f, -(HalfGridSize + CubeSize), 0f),
	initialScaling: new Vect((GridSize / CubeSize) * 2f, 1f, (GridSize / CubeSize) * 2f)
);
scene.Add(floor);


using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);

void ExecSubTest(Action<ILightBuilder, Scene, ApplicationLoop, Renderer, Camera> subTest) {
	camera.Position = new Location(HalfGridSize, HalfGridSize * 1.5f, -(GridSize + 1));
	camera.HorizontalFieldOfView = 120f;
	camera.LookAt(FloorPosition);
	loop.ResetTotalIteratedTime();
	subTest(factory.LightBuilder, scene, loop, renderer, camera);
}

ExecSubTest(ShadowOnOffTest);
ExecSubTest(PointsInBoxes);
ExecSubTest(SpotlightRotating);
ExecSubTest(SpotlightsMoving);
ExecSubTest(DirectionalLongShadows);
ExecSubTest(DirectionalDynamic);

scene.Remove(floor);
foreach (var cube in cubeList) {
	scene.Remove(cube);
	cube.Dispose();
}
cubeList.Dispose();


bool PassedTimeFence(float deltaTime, TimeSpan totalTime, TimeSpan timeFence) {
	return totalTime.TotalSeconds >= timeFence.TotalSeconds && totalTime.TotalSeconds - deltaTime < timeFence.TotalSeconds;
}

void ShadowOnOffTest(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var spotlight = lightBuilder.CreateSpotLight(
		color: StandardColor.LightingSunRiseSet,
		castsShadows: true,
		position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
		coneDirection: new(0.3f, -1f, 0.3f),
		coneAngle: 90f
	);
	using var overhead = lightBuilder.CreateSpotLight(
		color: StandardColor.White,
		castsShadows: true,
		position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
		coneDirection: Direction.Down,
		coneAngle: 160f
	);

	scene.Add(spotlight);
	scene.Add(overhead);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });

	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
		camera.LookAt(FloorPosition, Direction.Up);

		spotlight.MoveBy((0f, 0.01f * dt, 0f));
		overhead.MoveBy((0f, 0.01f * dt, 0f));

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
			spotlight.CastsShadows = false;
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
			spotlight.CastsShadows = true;
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
			spotlight.CastsShadows = false;
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			spotlight.CastsShadows = true;
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
			spotlight.CastsShadows = false;
		}

		renderer.Render();
	}

	scene.Remove(overhead);
	scene.Remove(spotlight);
}


















void DirectionalLongShadows(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var directionalLight = lightBuilder.CreateDirectionalLight(
		direction: new Location(0f, CubeSize, GridSize).DirectionTo(Location.Origin),
		color: StandardColor.LightingSunRiseSet,
		showSunDisc: true,
		castsShadows: true
	);
	directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

	scene.Add(directionalLight);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(9d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		directionalLight.RotateBy(4f % Direction.Down * dt);
		directionalLight.Direction = (Location.Origin + directionalLight.Direction * -3f + Direction.Up * dt * 0.02f).DirectionTo(Location.Origin);

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Low });
			directionalLight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
			directionalLight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.High });
			directionalLight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
			directionalLight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
			directionalLight.CastsShadows = false;
		}

		renderer.Render();
	}

	scene.Remove(directionalLight);
}

void DirectionalDynamic(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var directionalLight = lightBuilder.CreateDirectionalLight(
		direction: new Direction(0.3f, -1f, 0.3f),
		color: StandardColor.Maroon,
		showSunDisc: true,
		castsShadows: true
	);
	directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

	scene.Add(directionalLight);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

	camera.Position = new Location(-HalfGridSize, HalfGridSize, -HalfGridSize);
	camera.LookAt(Location.Origin);
	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(13d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		directionalLight.RotateBy(50f % Direction.Forward * dt * (directionalLight.Direction.Y > 0f ? 10f : 1f));
		camera.Position = camera.Position.RotatedAroundOriginBy(-42f % Direction.Down * dt);
		camera.LookAt(Location.Origin, Direction.Up);

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(2d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Low });
			directionalLight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
			directionalLight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.High });
			directionalLight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(8d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
			directionalLight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(10d))) {
			directionalLight.CastsShadows = false;
		}

		renderer.Render();
	}

	scene.Remove(directionalLight);
}

void PointsInBoxes(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var pointLightUpper = lightBuilder.CreatePointLight(
		color: StandardColor.LightingSunRiseSet,
		castsShadows: true,
		position: new Location(0f, (HalfGridSize + 2) * CubeSize, HalfGridSize / 2f)
	);
	using var pointLightLower = lightBuilder.CreatePointLight(
		color: StandardColor.White,
		castsShadows: true,
		position: new Location(0f, -(HalfGridSize + 2) * CubeSize, HalfGridSize / -2f)
	);

	scene.Add(pointLightUpper);
	scene.Add(pointLightLower);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		pointLightUpper.Position = pointLightUpper.Position.RotatedAroundOriginBy(90f % Direction.Down * dt);

		camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
		camera.LookAt(FloorPosition, Direction.Up);

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Low });
			pointLightUpper.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
			pointLightUpper.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.High });
			pointLightUpper.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
			pointLightUpper.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
			pointLightUpper.CastsShadows = false; // TODO clue here: Changing this affects the next test...
		}

		renderer.Render();
	}

	scene.Remove(pointLightUpper);
	scene.Remove(pointLightLower);
}

void SpotlightRotating(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var spotlight = lightBuilder.CreateSpotLight(
		color: StandardColor.LightingSunRiseSet,
		castsShadows: true,
		position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
		coneDirection: new(0.3f, -1f, 0.3f),
		coneAngle: 90f
	);
	using var overhead = lightBuilder.CreateSpotLight(
		color: StandardColor.White,
		castsShadows: false,
		position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
		coneDirection: Direction.Down,
		coneAngle: 160f
	);

	scene.Add(spotlight);
	scene.Add(overhead);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		spotlight.RotateBy(90f % Direction.Down * dt);

		camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
		camera.LookAt(FloorPosition, Direction.Up);

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Low });
			spotlight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
			spotlight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.High });
			spotlight.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
			spotlight.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
			spotlight.CastsShadows = false;
		}

		renderer.Render();
	}

	scene.Remove(overhead);
	scene.Remove(spotlight);
}

void SpotlightsMoving(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
	using var spotlightA = lightBuilder.CreateSpotLight(
		color: StandardColor.LightingSunRiseSet,
		castsShadows: true,
		position: new Location(HalfGridSize * CubeSize, (GridSize + 2) * CubeSize, 0f),
		coneDirection: Direction.Down,
		coneAngle: 110f
	);
	using var spotlightB = lightBuilder.CreateSpotLight(
		color: StandardColor.LightingSunRiseSet,
		castsShadows: true,
		position: new Location(HalfGridSize * -CubeSize, (GridSize + 2) * CubeSize, 0f),
		coneDirection: Direction.Down,
		coneAngle: 110f
	);

	scene.Add(spotlightA);
	scene.Add(spotlightB);
	renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

	while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
		var dt = (float) loop.IterateOnce().TotalSeconds;

		spotlightA.Position = spotlightA.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
		spotlightB.Position = spotlightB.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);

		camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
		camera.LookAt(FloorPosition, Direction.Up);

		if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Low });
			spotlightA.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
			spotlightA.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.High });
			spotlightA.AdjustColorHueBy(30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
			renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
			spotlightA.AdjustColorHueBy(-30f);
		}
		else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
			spotlightA.CastsShadows = false;
		}

		renderer.Render();
	}

	scene.Remove(spotlightA);
	scene.Remove(spotlightB);
}