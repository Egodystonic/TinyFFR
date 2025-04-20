using System.Diagnostics;
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














var location1 = Location.Origin;
var location2 = location1 with { X = 0f };

Console.WriteLine(new Direction(1f, 2f, 3f) * MathF.Sqrt(14f));















using var factory = new LocalTinyFfrFactory();
var assLoad = factory.AssetLoader;
var meshBuilder = factory.AssetLoader.MeshBuilder;

var verticesMemory = factory.ResourceAllocator.CreatePooledMemoryBuffer<MeshVertex>(4);
var trianglesMemory = factory.ResourceAllocator.CreatePooledMemoryBuffer<VertexTriangle>(2);
var vertices = verticesMemory.Span;
var triangles = trianglesMemory.Span;

vertices[0] = new MeshVertex(
	location: (0.5f, -0.5f, 0f),
	textureCoords: (0f, 0f),
	tangent: Direction.Right,
	bitangent: Direction.Up,
	normal: Direction.Backward
);
vertices[1] = new MeshVertex(
	location: (-0.5f, -0.5f, 0f),
	textureCoords: (1f, 0f),
	tangent: Direction.Right,
	bitangent: Direction.Up,
	normal: Direction.Backward
);
vertices[2] = new MeshVertex(
	location: (-0.5f, 0.5f, 0f),
	textureCoords: (1f, 1f),
	tangent: Direction.Right,
	bitangent: Direction.Up,
	normal: Direction.Backward
);
vertices[3] = new MeshVertex(
	location: (0.5f, 0.5f, 0f),
	textureCoords: (0f, 1f),
	tangent: Direction.Right,
	bitangent: Direction.Up,
	normal: Direction.Backward
);

triangles[0] = new(0, 1, 2);
triangles[1] = new(2, 3, 0);

using var mesh = meshBuilder.CreateMesh(
	vertices,
	triangles,
	new MeshCreationConfig { /* specify creation options here */ }
);

factory.ResourceAllocator.ReturnPooledMemoryBuffer(trianglesMemory);
factory.ResourceAllocator.ReturnPooledMemoryBuffer(verticesMemory);


//using var mesh = assLoad.LoadMesh(@"C:\Users\ben\Documents\Temp\treasure_chest\treasure_chest_4k.gltf");
using var colorMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_diff_4k.jpg");
using var normalMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_nor_gl_4k.jpg");
using var ormMap = assLoad.LoadTexture(@"C:\Users\ben\Documents\Temp\treasure_chest\textures\treasure_chest_arm_4k.jpg");
using var material = assLoad.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap);
using var modelInstance = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: (0f, -0.3f, 0f));

using var hdr = assLoad.LoadEnvironmentCubemap(@"C:\Users\ben\Documents\Temp\treasure_chest\belfast_sunset_puresky_4k.hdr");
using var scene = factory.SceneBuilder.CreateScene();

scene.Add(modelInstance);
scene.SetBackdrop(hdr, 0.7f);

var cameraDistance = 3f;
var chestToCameraStartVect = Direction.Backward * cameraDistance;
using var camera = factory.CameraBuilder.CreateCamera(
	initialPosition: Location.Origin + chestToCameraStartVect, 
	initialViewDirection: Direction.Forward
);

using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);

window.LockCursor = true;

while (!loop.Input.UserQuitRequested) {
	var sw = Stopwatch.StartNew();
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;

	CameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime);
	CameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);

	renderer.Render();
	if (sw.ElapsedMilliseconds > 20) Console.WriteLine("EM: " + sw.ElapsedMilliseconds + "; " + "Dt: " + deltaTime);
}

static class CameraInputHandler {
	const float CameraMovementSpeed = 1f;
	static Angle _currentHorizontalAngle = Angle.Zero;
	static Angle _currentVerticalAngle = Angle.Zero;
	static Direction _currentHorizontalPlaneDir = Direction.Forward;

	public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
		AdjustCameraViewDirectionKbm(input, camera, deltaTime);
		AdjustCameraPositionKbm(input, camera, deltaTime);
	}

	public static void TickGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
		AdjustCameraViewDirectionGamepad(input, camera, deltaTime);
		AdjustCameraPositionGamepad(input, camera, deltaTime);
	}

	static void AdjustCameraViewDirectionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
		const float MouseSensitivity = 0.05f;

		var cursorDelta = input.MouseCursorDelta;
		_currentHorizontalAngle += cursorDelta.X * MouseSensitivity;
		_currentVerticalAngle += cursorDelta.Y * MouseSensitivity;

		_currentHorizontalAngle = _currentHorizontalAngle.Normalized;
		_currentVerticalAngle = _currentVerticalAngle.Clamp(-Angle.QuarterCircle, Angle.QuarterCircle);

		_currentHorizontalPlaneDir = Direction.Forward * (_currentHorizontalAngle % Direction.Down);
		var verticalTiltRot = _currentVerticalAngle % Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);

		camera.SetViewAndUpDirection(_currentHorizontalPlaneDir * verticalTiltRot, Direction.Up * verticalTiltRot);
	}

	static void AdjustCameraPositionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
		var positiveHorizontalYDir = camera.ViewDirection;
		var positiveHorizontalXDir = Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);

		var horizontalMovement = XYPair<float>.Zero;
		var verticalMovement = 0f;
		foreach (var currentKey in input.CurrentlyPressedKeys) {
			switch (currentKey) {
				case KeyboardOrMouseKey.ArrowLeft:
					horizontalMovement += (1f, 0f);
					break;
				case KeyboardOrMouseKey.ArrowRight:
					horizontalMovement += (-1f, 0f);
					break;
				case KeyboardOrMouseKey.ArrowUp:
					horizontalMovement += (0f, 1f);
					break;
				case KeyboardOrMouseKey.ArrowDown:
					horizontalMovement += (0f, -1f);
					break;
				case KeyboardOrMouseKey.RightControl:
					verticalMovement -= 1f;
					break;
				case KeyboardOrMouseKey.RightShift:
					verticalMovement += 1f;
					break;
			}
		}

		var horizontalMovementVect = (positiveHorizontalXDir * horizontalMovement.X) + (positiveHorizontalYDir * horizontalMovement.Y);
		var verticalMovementVect = Direction.Up * verticalMovement;
		var sumMovementVect = (horizontalMovementVect + verticalMovementVect).WithLength(CameraMovementSpeed * deltaTime);
		camera.MoveBy(sumMovementVect);
	}

	static void AdjustCameraViewDirectionGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
		const float StickSensitivity = 100f;

		var horizontalRotationStrength = input.RightStickPosition.GetDisplacementHorizontalWithDeadzone();
		var verticalRotationStrength = input.RightStickPosition.GetDisplacementVerticalWithDeadzone();

		_currentHorizontalAngle += StickSensitivity * horizontalRotationStrength * deltaTime;
		_currentHorizontalAngle = _currentHorizontalAngle.Normalized;

		_currentVerticalAngle -= StickSensitivity * verticalRotationStrength * deltaTime;
		_currentVerticalAngle = _currentVerticalAngle.Clamp(-Angle.QuarterCircle, Angle.QuarterCircle);

		_currentHorizontalPlaneDir = Direction.Forward * (_currentHorizontalAngle % Direction.Down);
		var verticalTiltRot = _currentVerticalAngle % Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);

		camera.SetViewAndUpDirection(_currentHorizontalPlaneDir * verticalTiltRot, Direction.Up * verticalTiltRot);
	}

	static void AdjustCameraPositionGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
		var verticalMovementMultiplier = input.RightTriggerPosition.GetDisplacementWithDeadzone() - input.LeftTriggerPosition.GetDisplacementWithDeadzone();
		var verticalMovementVect = verticalMovementMultiplier * Direction.Up;

		var horizontalMovementVect = Vect.Zero;
		var stickDisplacement = input.LeftStickPosition.GetDisplacementWithDeadzone();
		var stickAngle = input.LeftStickPosition.GetPolarAngle();

		if (stickAngle is { } horizontalMovementAngle) {
			var horizontalMovementDir = _currentHorizontalPlaneDir * (Direction.Up % (horizontalMovementAngle - Angle.QuarterCircle));
			horizontalMovementVect = horizontalMovementDir * stickDisplacement;
		}


		var sumMovementVect = (horizontalMovementVect + verticalMovementVect).WithMaxLength(1f) * CameraMovementSpeed * deltaTime;
		camera.MoveBy(sumMovementVect);
	}
}