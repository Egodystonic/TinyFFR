using System.Text;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

// Variables & constants
const string PlaceholderText = "Awaiting Text Input...";
var inProgressTextPrimitive = (ScenePrimitive?) null;
var inProgressTextLocation = new Location();
var inProgressTextChars = (INonDisposableArrayPoolBackedList<char>?) null;
var mostRecentlyAddedCubePrimitive = (ScenePrimitive?) null;
var mostRecentlyAddedCubeShape = PositionedRotatedCuboid.UnitCubeAtOriginUnrotated;
var groundPlaneShape = new Plane(Direction.Up, Location.Origin);

// Setup
using var factory = new LocalTinyFfrFactory();
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
using var scene = factory.SceneBuilder.CreateScene();
using var camera = factory.CameraBuilder.CreateCamera();
using var cameraController = camera.CreateController<FreeFlyingCameraController>();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop();

window.LockCursor = true;
cameraController.Position = (0f, 1f, -3f);
scene.AddPrimitiveShape(groundPlaneShape);

// Loop
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	var kbm = loop.Input.KeyboardAndMouse;

	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) break;

	var spawnLocation = camera.Position + camera.GetRelativeOrientationDirection(Orientation.Forward) * 2f;

	if (inProgressTextChars is not { } list) {
		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Backspace)) {
			scene.RemoveAll();
			scene.AddPrimitiveShape(groundPlaneShape);
			mostRecentlyAddedCubePrimitive = null;
		}
		
		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
			mostRecentlyAddedCubeShape = new PositionedRotatedCuboid(new Cuboid(0.5f), spawnLocation, Rotation.None);
			mostRecentlyAddedCubePrimitive = scene.AddPrimitiveShape(
				mostRecentlyAddedCubeShape, 
				new PrimitivePaintbrush(ColorVect.RandomOpaque())
			);
		}

		if (kbm.MouseScrollWheelDelta != 0 && mostRecentlyAddedCubePrimitive is { } primitive) {
			var adjustedShape = mostRecentlyAddedCubeShape.WithAllExtentsAdjustedBy(-kbm.MouseScrollWheelDelta * 0.05f);
			if (adjustedShape.SmallestExtent >= 0.05f) {
				mostRecentlyAddedCubeShape = adjustedShape;
				primitive.SetGeometryShape(mostRecentlyAddedCubeShape);
			}
		}

		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
			inProgressTextChars = factory.ResourceAllocator.GetSharedScratchList<char>();
			inProgressTextLocation = spawnLocation;
			inProgressTextPrimitive = scene.AddPrimitiveString(inProgressTextLocation, PlaceholderText, ScenePrimitiveSize.VeryLarge);
			loop.EnableInputTextTranscription = true;
		}
		
		cameraController.AdjustAllViaDefaultControls(kbm, deltaTime);
		cameraController.Progress(deltaTime);
	}
	else {
		list.AddRange(kbm.TranscribedText);
		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Backspace) && list.Count > 0) list.RemoveAt(list.Count - 1);
		inProgressTextPrimitive!.Value.SetGeometryString(
			inProgressTextLocation, 
			list.Count > 0 ? list.BackingSpan : PlaceholderText, 
			ScenePrimitiveSize.VeryLarge
		);

		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
			inProgressTextPrimitive!.Value.SetPaintbrush(new PrimitivePaintbrush(StandardColor.Green, StandardColor.Black));
			loop.EnableInputTextTranscription = false;
			inProgressTextPrimitive = null;
			inProgressTextChars = null;
		}
	}

	renderer.Render();
}
