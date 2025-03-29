using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.World;

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

using var factory = new LocalTinyFfrFactory();

using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.Gradient(
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Right)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpRight)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Up)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Left)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Down)!.Value, 1f, 0.5f),
	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownRight)!.Value, 1f, 0.5f),
	ColorVect.White
));
// using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.ChequerboardBordered(
// 	ColorVect.RandomOpaque(), 5, ColorVect.RandomOpaque()
// ));
// using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.ChequerboardBordered(
// 	ColorVect.RandomOpaque().WithLightness(0.5f),
// 	16,
// 	ColorVect.RandomOpaque().WithSaturation(0.85f),
// 	ColorVect.RandomOpaque().WithSaturation(0.85f),
// 	ColorVect.RandomOpaque().WithSaturation(0.85f),
// 	ColorVect.RandomOpaque().WithSaturation(0.85f),
// 	cellResolution: 256
// ));
using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap(TexturePattern.Rectangles(
	(256, 256),
	(16, 16),
	(0, 0),
	Direction.Forward,
	new Direction(-1f, 0f, 1f),
	new Direction(0f, 1f, 1f),
	new Direction(1f, 0f, 1f),
	new Direction(0f, -1f, 1f),
	Direction.Forward, 
	(8, 8)
));
var ormPattern = TexturePattern.Rectangles<Real>(
	(256, 256),
	(16, 16),
	(0, 0),
	0f,
	1f,
	0.4f,
	0f,
	0.4f,
	1f,
	(8, 8)
);
using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap(ormPattern, ormPattern, ormPattern);

// Create a cuboid mesh and load an instance of it in to the world with a test material
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f)); // 1m cube
using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(
	colorMap,
	normalMap,
	ormMap
);
using var instance = factory.ObjectBuilder.CreateModelInstance(
  mesh,
  mat
);

// Create a light to illuminate the cube
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);

// Create a window to render to, 
// a scene to render, 
// a camera to capture the scene, 
// and a renderer to render it all
using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value, size: (800, 800));
using var scene = factory.SceneBuilder.CreateScene();
using var camera = factory.CameraBuilder.CreateCamera();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

using var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(@"C:\Users\ben\Documents\Egodystonic\TinyFFR\Repository\TinyFFR.Tests\IntegrationTests\kloofendal_48d_partly_cloudy_puresky_4k.hdr");
scene.SetBackdrop(cubemap);

// Add the cube instance and light to the scene
scene.Add(instance);
scene.Add(light);

// Put the cube 2m in front of the camera
instance.SetPosition(new Location(0f, 0f, 2f));

// Keep rendering at 60Hz until the user closes the window
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
while (!loop.Input.UserQuitRequested) {
	var dt = (float) loop.IterateOnce().TotalSeconds;

	// If we're holding space down, rotate the cube
	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		camera.RotateBy(new Rotation(angle: 90f, axis: Direction.Down) * dt);
		camera.Position = instance.Position - camera.ViewDirection * 2f;
		light.Position = camera.Position + (Direction.Up * MathF.Sin(3f * (float) loop.TotalIteratedTime.TotalSeconds));
		//instance.RotateBy(new Rotation(angle: 90f, axis: Direction.Down) * dt);
	}

	renderer.Render();
}