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
var display = factory.DisplayDiscoverer.Recommended ?? throw new ApplicationException("This test requires at least one connected display.");
using var window = factory.WindowBuilder.CreateWindow(display, title: "William the Window");
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60, name: "Larry the Loop");
using var camera = factory.CameraBuilder.CreateCamera(Location.Origin, name: "Carl the Camera");
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, factory.AssetLoader.MaterialBuilder.TestMaterial, name: "Iain the Instance");
using var light = factory.LightBuilder.CreatePointLight(camera.Position + Direction.Forward * 1f, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f), name: "Lars the Light");
using var scene = factory.SceneBuilder.CreateScene(includeBackdrop: false, name: "Sean the Scene");
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window, name: "Ryan the Renderer");

scene.Add(instance);
scene.Add(light);

instance.SetPosition(camera.Position + Direction.Forward * 2.2f);

while (!loop.Input.UserQuitRequested) {
	window.Title = (1000d / loop.IterateOnce().TotalMilliseconds).ToString("N0") + " FPS";
	renderer.Render();

	// var newMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f), new(rotation: (float) loop.TotalIteratedTime.TotalSeconds * -47f), true, name: "Clive the Cuboid");
	// instance.Mesh = newMesh;
	// mesh.Dispose();
	// mesh = newMesh;

	instance.RotateBy(0.5f * 1f % Direction.Up);
	instance.RotateBy(0.5f * 0.66f % Direction.Right);
	
	light.AdjustColorHueBy(0.5f);
	light.Position = instance.Position + (((instance.Position >> camera.Position) * 2.2f) * ((MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 0.8f) * 45f) % Direction.Down));
	light.Position += Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 1f) * 3.5f;

	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		light.AdjustBrightnessBy(0.05f);
	}
	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Backspace)) {
		light.AdjustBrightnessBy(-0.05f);
	}
}