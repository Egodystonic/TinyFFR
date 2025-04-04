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
using var cubeMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
var materialBuilder = factory.AssetLoader.MaterialBuilder;

using var colorMap = materialBuilder.CreateColorMap(
	TexturePattern.ChequerboardBordered<ColorVect>(
		borderValue: StandardColor.Black,
		firstValue: StandardColor.Red,
		secondValue: StandardColor.Green,
		thirdValue: StandardColor.Blue,
		fourthValue: StandardColor.Purple,
		borderWidth: 8,
		transform: new Transform2D(translation: (100f, 10f))
	)
);

var roughnessPattern = TexturePattern.Lines<Real>(
	firstValue: 0f,
	secondValue: 0.7f,
	thirdValue: 0.3f,
	fourthValue: 1f,
	horizontal: false,
	numRepeats: 3,
	perturbationMagnitude: 0.1f,
	perturbationFrequency: 2f,
	transform: Transform2D.FromScalingOnly((1.3f, 0.2f))
);
var metallicPattern = TexturePattern.Lines<Real>(
	firstValue: 0f,
	secondValue: 1f,
	horizontal: true,
	numRepeats: 1,
	perturbationMagnitude: 2f,
	perturbationFrequency: -0.3f,
	transform: Transform2D.FromScalingOnly(0.6f)
);

using var ormMap = materialBuilder.CreateOrmMap(
	roughnessPattern: roughnessPattern,
	metallicPattern: metallicPattern
);

using var material = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap);

using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, material, initialPosition: (0f, 0f, 2f));
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);
using var scene = factory.SceneBuilder.CreateScene();

scene.Add(cube);
scene.Add(light);

using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var camera = factory.CameraBuilder.CreateCamera(initialPosition: Location.Origin, initialViewDirection: Direction.Forward);

using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
var input = loop.Input;
var kbm = input.KeyboardAndMouse;

while (!input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;
	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) cube.RotateBy(90f % Direction.Down * deltaTime);
	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.Return)) cube.RotateBy(90f % Direction.Right * deltaTime);
	renderer.Render();
}