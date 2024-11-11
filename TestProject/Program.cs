using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;


NativeLibrary.SetDllImportResolver( // Yeah this is ugly af but it'll do for v1
		typeof(LocalRendererFactory).Assembly,
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


using var factory = (ILocalRendererFactory) new LocalRendererFactory();

var display = factory.DisplayDiscoverer.Recommended ?? throw new ApplicationException("This test requires at least one connected display.");
using var window = factory.WindowBuilder.Build(display, WindowFullscreenStyle.NotFullscreen);
using var loop = factory.ApplicationLoopBuilder.BuildLoop(new LocalApplicationLoopConfig { FrameRateCapHz = 60, Name = "Larry the Loop" });

using var camera = factory.CameraBuilder.CreateCamera(new() {
	Position = (Direction.Backward * 100f).AsLocation(),
	ViewDirection = Direction.Forward,
	UpDirection = Direction.Up,
	Name = "Carl the Camera"
});

using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(10f, 7f, 2f), new() { Name = "Clive the Cuboid" });
using var mat = factory.AssetLoader.MaterialBuilder.CreateBasicSolidColorMat(0x00FF00, new() { Name = "Matthew the Material" });
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, new() { Name = "Iain the Instance" });

using var scene = factory.SceneBuilder.CreateScene(new() { Name = "Sean the Scene" });
scene.Add(instance);

Console.WriteLine(display);
Console.WriteLine(window);
Console.WriteLine(loop);
Console.WriteLine(camera);
Console.WriteLine(mesh);
Console.WriteLine(mat);
Console.WriteLine(instance);
Console.WriteLine(scene);

while (!loop.Input.UserQuitRequested) {
	_ = loop.IterateOnce();
	scene.Render(camera, window);
	// var m = camera.GetProjectionMatrix();
	// Console.WriteLine($"====================================");
	// Console.WriteLine($"{m.M11,5}{m.M12,5}{m.M13,5}{m.M14,5}");
	// Console.WriteLine($"{m.M21,5}{m.M22,5}{m.M23,5}{m.M24,5}");
	// Console.WriteLine($"{m.M31,5}{m.M32,5}{m.M33,5}{m.M34,5}");
	// Console.WriteLine($"{m.M41,5}{m.M42,5}{m.M43,5}{m.M44,5}");
	camera.Rotate(new Rotation(3f, Direction.Down));
	// camera.Move((0f, 0f, 1f));
	// Console.WriteLine(camera.Position);
	// Console.WriteLine(camera.ViewDirection);
	//instance.Scale(1.05f);
}