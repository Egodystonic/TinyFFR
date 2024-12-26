using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Meshes;

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
using var camera = factory.CameraBuilder.CreateCamera((0f, 0f, -30f), name: "Carl the Camera");
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(10f, 7f, 2f), name: "Clive the Cuboid");
using var mat = factory.AssetLoader.MaterialBuilder.CreateBasicSolidColorMat(0x00FF00FF, name: "Matthew the Material");
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, name: "Iain the Instance");
using var light = factory.LightBuilder.CreatePointLight((0f, 0f, -20f), StandardColor.Red, falloffRange: 100f, name: "Lars the Light");
using var scene = factory.SceneBuilder.CreateScene(name: "Sean the Scene");
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window, name: "Ryan the Renderer");

scene.Add(instance);

Console.WriteLine(display);
Console.WriteLine(window);
Console.WriteLine(loop);
Console.WriteLine(camera);
Console.WriteLine(mesh);
Console.WriteLine(mat);
Console.WriteLine(light);
Console.WriteLine(instance);
Console.WriteLine(scene);
Console.WriteLine(renderer);

while (!loop.Input.UserQuitRequested) {
	_ = loop.IterateOnce();
	renderer.Render();
	//instance.MoveBy(Direction.Left * 0.01f);
	camera.ViewDirection = Direction.Forward;
	camera.MoveBy(Direction.Right * 0.01f);
}