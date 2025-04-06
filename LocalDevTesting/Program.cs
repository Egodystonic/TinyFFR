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
var assLoad = factory.AssetLoader;

using var mesh = assLoad.LoadMesh(@"C:\Users\ben\Documents\Temp\treasure_chest\treasure_chest_4k.gltf");
using var colorMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_diff_4k.jpg");
using var normalMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_nor_gl_4k.jpg");
using var ormMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_arm_4k.jpg");
using var material = assLoad.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap);
using var modelInstance = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: (0f, -0.3f, 0f));


using var hdr = assLoad.LoadEnvironmentCubemap(@"C:\Users\ben\Documents\Temp\treasure_chest\belfast_sunset_puresky_4k.hdr");
using var scene = factory.SceneBuilder.CreateScene();

scene.Add(modelInstance);
scene.SetBackdrop(hdr, 0.7f);

var cameraDistance = 1.3f;
var chestToCameraStartVect = Direction.Backward * cameraDistance;
using var camera = factory.CameraBuilder.CreateCamera(
	initialPosition: Location.Origin + chestToCameraStartVect, 
	initialViewDirection: Direction.Forward
);

using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);

var cameraRotationPerSec = (90f % Direction.Down);
while (!loop.Input.UserQuitRequested) {
	_ = loop.IterateOnce().TotalSeconds;

	var currentRotation = cameraRotationPerSec * (float) loop.TotalIteratedTime.TotalSeconds;
	camera.Position = Location.Origin + chestToCameraStartVect * currentRotation;
	camera.ViewDirection = (camera.Position >> Location.Origin).Direction;

	renderer.Render();
}