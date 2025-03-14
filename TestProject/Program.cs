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
using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60); // 60hz Loop
using var camera = factory.CameraBuilder.CreateCamera();
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f)); // 1m cube
var material = factory.AssetLoader.MaterialBuilder.TestMaterial;
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);
using var scene = factory.SceneBuilder.CreateScene();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

scene.Add(instance);
scene.Add(light);

instance.SetPosition(new Location(0f, 0f, 2f));

while (!loop.Input.UserQuitRequested) {
	var dt = (float) loop.IterateOnce().TotalSeconds;
	renderer.Render();

	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		instance.RotateBy(new Rotation(angle: 90f, axis: Direction.Down) * dt);
	}
}