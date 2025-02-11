using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
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
using var camera = factory.CameraBuilder.CreateCamera(Location.Origin, name: "Carl the Camera");
var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(2f, 2f, 2f), name: "Clive the Cuboid");
//using var tex = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.White, name: "Terry the Texture");
var texPattern = TexturePattern.Chequerboard(new ColorVect(1f, 0f, 0f), new ColorVect(0f, 1f, 0f), new ColorVect(0f, 0f, 1f), new ColorVect(0.5f, 0.5f, 0.5f));
using var tex = factory.AssetLoader.MaterialBuilder.CreateColorMap(texPattern, name: "Terry the Texture");
using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(tex, name: "Matthew the Material");
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, name: "Iain the Instance");
using var light = factory.LightBuilder.CreatePointLight(camera.Position, StandardColor.Red, brightness: 5000000f, name: "Lars the Light"); // TODO why so bright?
using var scene = factory.SceneBuilder.CreateScene(name: "Sean the Scene");
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window, name: "Ryan the Renderer");

scene.Add(instance);
scene.Add(light);

Console.WriteLine(display);
Console.WriteLine(window);
Console.WriteLine(loop);
Console.WriteLine(camera);
Console.WriteLine(mesh);
Console.WriteLine(tex);
Console.WriteLine(mat);
Console.WriteLine(light);
Console.WriteLine(instance);
Console.WriteLine(scene);
Console.WriteLine(renderer);

instance.SetPosition(camera.Position + Direction.Forward * 2.2f);
Console.WriteLine(camera.Position);
Console.WriteLine(light.Position);
Console.WriteLine(instance.Position);
while (!loop.Input.UserQuitRequested) {
	_ = loop.IterateOnce();
	renderer.Render();
	var newMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(2f, 2f, 2f), Transform2D.FromRotationOnly((float) loop.TotalIteratedTime.TotalSeconds * 30f), name: "Clive the Cuboid");
	instance.Mesh = newMesh;
	mesh.Dispose();
	mesh = newMesh;
	instance.RotateBy(0.5f * 1f % Direction.Up);
	instance.RotateBy(0.5f * 0.66f % Direction.Right);
	//light.MoveBy(Direction.Backward * 0.1f);
	//Console.WriteLine(instance.Position >> light.Position);
	light.Color = light.Color.WithHueAdjustedBy(1f);
}