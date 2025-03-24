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

using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f)); // 1m cube

// using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.Gradient(
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Right)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpRight)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Up)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Left)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Down)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownRight)!.Value, 1f, 0.5f),
// 	ColorVect.White
// ));
using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.ChequerboardBordered(
	ColorVect.RandomOpaque(), 5, ColorVect.RandomOpaque()
));
// using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.Lines(
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Right)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpRight)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Up)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Left)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.Down)!.Value, 1f, 0.5f),
// 	ColorVect.FromHueSaturationLightness(Angle.From2DPolarAngle(Orientation2D.DownRight)!.Value, 1f, 0.5f),
// 	horizontal: true,
// 	perturbationMagnitude: 0.2f,
// 	numRepeats: 1
// ));

using var material = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap);
var s = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: new Location(0f, 0.05f, 1.6f) + Vect.Random(new(-0.1f), new(0.1f)));
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);

using var light = factory.LightBuilder.CreatePointLight(new Location(0f, 0f, 0f), new ColorVect(1f, 1f, 1f));
using var rLight = factory.LightBuilder.CreatePointLight(new Location(1.6f, 2.6f, 1.6f), new ColorVect(0f, 1f, 0f));
using var lLight = factory.LightBuilder.CreatePointLight(new Location(-1.6f, 2.6f, 1.6f), new ColorVect(1f, 0f, 0f));
using var bLight = factory.LightBuilder.CreatePointLight(new Location(0f, 2.6f, 3.6f), new ColorVect(0f, 0f, 1f));

using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var scene = factory.SceneBuilder.CreateScene(includeBackdrop: true);
scene.Add(s);
s.SetPosition(instance.Position + Vect.Random());
using var camera = factory.CameraBuilder.CreateCamera();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

//scene.Add(instance);
scene.Add(light);
// scene.Add(rLight);
// scene.Add(lLight);
// scene.Add(bLight);

instance.SetPosition(new Location(0f, 0.05f, 1.6f));
instance.RotateBy(45f % Direction.Down);
instance.RotateBy(45f % Direction.Right);

for (var i = 0; i < 6000; ++i) {
	var q = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: instance.Position + Vect.Random());
	scene.Add(q);
	q.SetPosition(instance.Position + Vect.Random());
}

using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
while (!loop.Input.UserQuitRequested) {
	var dt = (float) loop.IterateOnce().TotalSeconds;

	if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
		var newColorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(TexturePattern.ChequerboardBordered(
			ColorVect.RandomOpaque(), 3, 
			ColorVect.RandomOpaque(),
			ColorVect.RandomOpaque(),
			ColorVect.RandomOpaque(),
			ColorVect.RandomOpaque(),
			repetitionCount: (4, 4)
		));
		var newMaterial = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(newColorMap);
		instance.Material = newMaterial;
	}

	camera.RotateBy((90f % Direction.Down) * dt);
	camera.RotateBy((90f % Direction.Right) * dt);

	renderer.Render();
}