using System.Reflection;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;

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
var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f), name: "Clive the Cuboid");
//using var tex = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.White, name: "Terry the Texture");
var colorPattern = TexturePattern.ChequerboardBordered(
	new ColorVect(1f, 1f, 1f), 
	4, 
	new ColorVect(1f, 0f, 0f), 
	new ColorVect(0f, 1f, 0f), 
	new ColorVect(0f, 0f, 1f), 
	new ColorVect(0.5f, 0.5f, 0.5f), 
	(4, 4),
	cellResolution: 256
);
//var colorPattern = TexturePattern.GradientRadial(new ColorVect(0.5f, 0.5f, 0.5f), new ColorVect(0f, 0f, 0f), innerOuterRatio: 0.4f);
//var colorPattern = TexturePattern.PlainFill(new ColorVect(1f, 1f, 1f));
// var colorPattern = TexturePattern.Rectangles(
// 	interiorSize:			(96, 96),
// 	borderSize:				(15, 15),
// 	paddingSize:				(30, 30),
// 	interiorValue:			new ColorVect(1f, 1f, 1f),
// 	borderRightValue:		new ColorVect(1f, 0f, 0f),
// 	borderTopValue:			new ColorVect(0f, 1f, 0f),
// 	borderLeftValue:			new ColorVect(0f, 0f, 1f),
// 	borderBottomValue:		new ColorVect(1f, 1f, 0f),
// 	paddingValue:			new ColorVect(0.5f, 0.5f, 0.5f),
// 	repetitions:				(4, 4)
// );
// var colorPattern = TexturePattern.Rectangles(
// 	new ColorVect(1f, 0f, 0f),
// 	new ColorVect(0f, 0f, 1f)
// );
// var colorPattern = TexturePattern.Lines(
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 0f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 1f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 2f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 3f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 4f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 5f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 6f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 7f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 8f), 
// 	new ColorVect(1f, 0f, 0f).WithHueAdjustedBy(36f * 9f), 
// 	true, 
// 	perturbationMagnitude: 0.1f
// );
// var colorPattern = TexturePattern.Circles(
// 	new ColorVect(0.6f, 0.6f, 0.6f),
// 	new ColorVect(1f, 0f, 0f),
// 	new ColorVect(0.3f, 0.3f, 0.3f)
// );
// var colorPattern = TexturePattern.Circles(
// 	new ColorVect(0.5f, 0f, 0f).WithHue(0f),
// 	new ColorVect(0.5f, 0f, 0f).WithHue(90f),
// 	new ColorVect(0.5f, 0f, 0f).WithHue(180f),
// 	new ColorVect(0.5f, 0f, 0f).WithHue(270f),
// 	new ColorVect(1f, 0f, 0f).WithHue(0f),
// 	new ColorVect(1f, 0f, 0f).WithHue(90f),
// 	new ColorVect(1f, 0f, 0f).WithHue(180f),
// 	new ColorVect(1f, 0f, 0f).WithHue(270f),
// 	new ColorVect(0.3f, 0.3f, 0.3f)
// );
var metallicPattern = TexturePattern.ChequerboardBordered<Real>(0f, 25, 1f, (4, 4));
// var normalPattern = TexturePattern.Rectangles(
// 	interiorSize:			(96, 96),
// 	borderSize:				(15, 15),
// 	paddingSize:				(30, 30),
// 	interiorValue:			Direction.Forward,
// 	borderRightValue:		new Direction(1f, 0f, 1f),
// 	borderTopValue:			new Direction(0f, 1f, 1f),
// 	borderLeftValue:			new Direction(-1f, 0f, 1f),
// 	borderBottomValue:		new Direction(0f, -1f, 1f),
// 	paddingValue:			Direction.Forward, 
// 	repetitions:				(4, 4)
// );
var normalPattern = TexturePattern.Circles(
	Direction.Forward,
	new Direction(1f, 0f, 1f),
	new Direction(0f, 1f, 1f),
	new Direction(-1f, 0f, 1f),
	new Direction(0f, -1f, 1f),
	Direction.Forward
);

using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(colorPattern, name: "Terry the Texture");
//using var colorMap = factory.AssetLoader.LoadTexture(@"C:\Users\ben\Pictures\ihavedep.png");
// using var colorMap = factory.AssetLoader.MaterialBuilder.CreateTexture(
// 	stackalloc TexelRgb24[] {
// 		TexelRgb24.ConvertFrom(StandardColor.Red),
// 		TexelRgb24.ConvertFrom(StandardColor.Yellow),
// 		TexelRgb24.ConvertFrom(StandardColor.Green),
// 		TexelRgb24.ConvertFrom(StandardColor.Blue),
// 		TexelRgb24.ConvertFrom(StandardColor.Purple),
// 		TexelRgb24.ConvertFrom(StandardColor.Olive),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.White),
// 		TexelRgb24.ConvertFrom(StandardColor.Red),
// 		TexelRgb24.ConvertFrom(StandardColor.Yellow),
// 		TexelRgb24.ConvertFrom(StandardColor.Green),
// 		TexelRgb24.ConvertFrom(StandardColor.Blue),
// 		TexelRgb24.ConvertFrom(StandardColor.Purple),
// 		TexelRgb24.ConvertFrom(StandardColor.Olive),
// 		TexelRgb24.ConvertFrom(StandardColor.Red),
// 		TexelRgb24.ConvertFrom(StandardColor.Yellow),
// 		TexelRgb24.ConvertFrom(StandardColor.Green),
// 		TexelRgb24.ConvertFrom(StandardColor.Blue),
// 		TexelRgb24.ConvertFrom(StandardColor.Purple),
// 		TexelRgb24.ConvertFrom(StandardColor.Olive),
// 		TexelRgb24.ConvertFrom(StandardColor.Red),
// 		TexelRgb24.ConvertFrom(StandardColor.Yellow),
// 		TexelRgb24.ConvertFrom(StandardColor.Green),
// 		TexelRgb24.ConvertFrom(StandardColor.Blue),
// 		TexelRgb24.ConvertFrom(StandardColor.Purple),
// 		TexelRgb24.ConvertFrom(StandardColor.Olive),
// 		TexelRgb24.ConvertFrom(StandardColor.Red),
// 		TexelRgb24.ConvertFrom(StandardColor.Yellow),
// 		TexelRgb24.ConvertFrom(StandardColor.Green),
// 		TexelRgb24.ConvertFrom(StandardColor.Blue),
// 		TexelRgb24.ConvertFrom(StandardColor.Purple),
// 		TexelRgb24.ConvertFrom(StandardColor.Olive),
// 	},
// 	new() {
// 		FlipX = true,
// 		FlipY = true,
// 		GenerateMipMaps = false,
// 		Width = 6,
// 		Height = 6,
// 		InvertZBlueChannel = true
// 	}
// );
//using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap(normalPattern);
using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap();
// using var normalMap = factory.AssetLoader.MaterialBuilder.CreateTexture(
// 	stackalloc TexelRgb24[] {
//  		TexelRgb24.ConvertFrom(Direction.Backward)
// 	},
// 	new() {
//  		Width = 1,
//  		Height = 1,
//  		InvertZBlueChannel = true
// 	}
// );
//using var normalMap = factory.AssetLoader.MaterialBuilder.DefaultNormalMap;
//using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap(metallicPattern: metallicPattern);
using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap(metallicPattern: TexturePattern.PlainFill<Real>(1f));
using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap: ormMap, name: "Matthew the Material");
using var instance = factory.ObjectBuilder.CreateModelInstance(factory.AssetLoader.LoadMesh(@"C:\Users\ben\Documents\Egodystonic\EscapeLizards\EscapeLizardsInst\Models\LizardCoin.obj"), mat, name: "Iain the Instance");
using var light = factory.LightBuilder.CreatePointLight(camera.Position + Direction.Forward * 1f, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f), name: "Lars the Light"); // TODO why so bright?
using var scene = factory.SceneBuilder.CreateScene(name: "Sean the Scene");
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window, name: "Ryan the Renderer");

scene.Add(instance);
scene.Add(light);

Console.WriteLine(display);
Console.WriteLine(window);
Console.WriteLine(loop);
Console.WriteLine(camera);
Console.WriteLine(mesh);
Console.WriteLine(colorMap);
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
	window.Title = (1000d / loop.IterateOnce().TotalMilliseconds).ToString("N0") + " FPS";
	renderer.Render();

	// var newMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f), new(rotation: (float) loop.TotalIteratedTime.TotalSeconds * -47f), true, name: "Clive the Cuboid");
	// instance.Mesh = newMesh;
	// mesh.Dispose();
	// mesh = newMesh;

	instance.RotateBy(0.5f * 1f % Direction.Up);
	instance.RotateBy(0.5f * 0.66f % Direction.Right);
	
	light.Color = light.Color.WithHueAdjustedBy(0.5f);
	light.Position = instance.Position + (((instance.Position >> camera.Position) * 1f) * ((MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 0.8f) * 45f) % Direction.Down));
	light.Position += Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 1f) * 3.5f;

	if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) window.Size += (100, 100);
}