---
title: Handling Input
description: Examples of how to manage user input.
---

TinyFFR comes with a built-in API for reacting to user input via keyboard, mouse, and gamepad. This page will demonstrate how to use those devices to control a free-flying camera.

??? example "Continuing "Hello Cube""
	This tutorial will mostly be concerned with showing you how to move the `camera` according to input captured via keyboard & mouse and/or gamepad.

	If you wish you can integrate these camera controls directly with the hello cube example and/or the treasure chest example from the previous page, just replace/remove any pre-existing camera manipulation code.

???+ warning "Math Ahead"
	The examples on this page necessitate a little more usage of the in-built math API than previous pages. It might be worth checking out the [Math & Geometry](/concepts/math_and_geometry.md) page first for a primer, depending on how confident you are already with 3D math.

	If you learn best by example however go ahead and jump right in: Every line of math below is annotated with explanations.

	Also, don't forget you can use the debugger to inspect what's going on in each frame, or even print things out to console! Try commenting out certain lines or just experimenting with changing values to get a feel for what's going on.

## Initial Setup

For all of the code below we will handle our input in a dedicated class "`CameraInputHandler`". We will invoke two static methods each frame from inside our application loop:

* `TickKbm()` to handle keyboard/mouse input, and,
* `TickGamepad()` to handle game controller input.

We will also define some static fields that will track the camera state across frames.

```csharp
static class CameraInputHandler {
	const float CameraMovementSpeed = 1f; // (1)!
	static Angle _currentHorizontalAngle = Angle.Zero; // (2)!
	static Angle _currentVerticalAngle = Angle.Zero; // (3)!
	static Direction _currentHorizontalPlaneDir = Direction.Forward; // (4)!

	public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) { // (5)!
		// TODO
	}

	public static void TickGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) { // (6)!
		// TODO
	}
}
```

1. 	This is just a constant that sets the camera's speed.

	You can modify it if you wish to slow down or speed up the camera!

2. `_currentHorizontalAngle` will be used to keep track of the current angle the camera is pointing at on the horizontal plane (i.e. forward/left/backward/right etc.).

	Arbitrarily, we will define `Direction.Forward` as being 0°; otherwise any non-zero value is the rotation in a clockwise direction around the `Down` axis.

	For example, 0° means our camera is looking forward, 90° means our camera is looking right, 180° is looking behind, and 270° is looking to the left.

	Remember, this is just the horizontal plane angle. We will combine it with the vertical (up/down) angle to create the final look direction for the camera.

3. `_currentVerticalAngle` will be used to keep track of the current up/down angle for the camera.

	Arbitrarily, we will define no up/down tilt as being 0°; otherwise any non-zero value is the rotation around the axis that is currently pointing leftward from where the camera is looking.
	
	(If this isn't clear: Pick something up on your desk and imagine it's the camera. "Point" it in various directions, and add a pen or pencil always sticking out of its left side no matter which way it's pointing. That's the axis we're rotating around with `_currentVerticalAngle`. Rotate your "camera" around this axis and you'll understand how this creates an up/down tilt.)

	For example, 0° is facing straight forward, 90° is facing fully downward at our feet, -90° is facing fully up in to the sky.

	And again, remember, we will combine this angle with the horizontal to create the final look direction for the camera.

4. 	Finally, we'll also store the actual horizontal view direction as well as the angle. 

	Although we can always get this value easily by using `_currentHorizontalAngle` to calculate it, storing the calculated value is a performance optimisation as we'll use it repeatedly each frame.

	We set it to `Direction.Forward` initially as that matches our `Angle.Zero` value for `_currentHorizontalAngle`.

5.	We will pass three things to `TickKbm()`; 

	1. An `ILatestKeyboardAndMouseInputRetriever` instance that we will use to get the latest keyboard + mouse input state;
	2. A reference to our `camera`;
	3. The amount of time passed this frame (in seconds).

	You'll see how to get the `ILatestKeyboardAndMouseInputRetriever` in the next code snippet below.

6.	We will pass the same three things to `TickGamepad()` as we did to `TickKbm()` excepting the first parameter, which is now an `ILatestGameControllerInputStateRetriever` instance instead of an `ILatestKeyboardAndMouseInputRetriever`.

	As you might have guessed, this interface lets us get game controller state rather than keyboard/mouse state.

We'll invoke these methods inside our application loop. 

We also need to lock our cursor inside the window while running to make sure it can't escape outside when moving our mouse to turn the camera; so we set `window.LockCursor` to `true` before entering the loop:

```csharp
window.LockCursor = true; // (1)!

while (!loop.Input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;

	CameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime);
	CameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);

	renderer.Render();
}
```

1. 	When set to `true`, the mouse cursor will be "locked inside" our window. This is useful when you want to use the mouse to control a camera as it stops the cursor from escaping the application frame.

	If you're still not sure what the purpose of this is, set it to `false` (the default) and observe the difference.

???+ warning "Double Input"
	In this example, we call both `TickKbm()` and `TickGamepad()` every frame. This does mean that we're technically manipulating the camera *twice* per frame: Once for the keyboard/mouse input and once for the gamepad.

	This may or may not matter for your application, but one implication is that if we move the camera with the gamepad and the keyboard at the same time, it will move at double the speed.

	Improving this will depend on how exactly you wish to handle various input sources for your application.

??? question "What is `loop.Input`?"
	The `Input` property on the `loop` returns an `ILatestInputRetriever`. This interface provides an API for capturing the __latest__ user input events & state.

	You might be wondering what exactly "latest" means: The answer is that the state and events retrieved by this interface are updated every time `loop.IterateOnce()` is invoked (hence why it's a property of the `ApplicationLoop`). In actuality, the instance returned by `loop.Input` is the same one every time.

	This means you can hold on to the same `ILatestInputRetriever` reference indefinitely and as long as the `ApplicationLoop` it came from is not disposed, the instance will remain valid and can be used to always access input data for the current frame.

	The same lifetime and usage pattern applies to all members of the `ILatestInputRetriever`, including the `ILatestKeyboardAndMouseInputRetriever` returned via the `KeyboardAndMouse` property and the `ILatestGameControllerInputStateRetriever` returned by the `GameControllers`/`GameControllersCombined` properties.

	That being said, simply accessing `loop.Input` every time like we're doing above is absolutely fine too.

??? question "What is `GameControllersCombined`?"
	`GameControllersCombined` returns an `ILatestGameControllerInputStateRetriever` that represents every game controller connected to the system, combined. For example, you can press the "A" button on one controller, and the "B" button on a second controller, and both button press events will be reflected in the `GameControllersCombined` retriever.

	Also, `GameControllersCombined` will always be valid, will never be null, and will never throw any exceptions; even if there are no controllers connected to the system or if the user adds or removes controllers.

	The main purpose of `GameControllersCombined` is to allow you to simply support anyone using your application to connect any controller and begin using it, without having to worry about configuring the "correct" controller.

	If you prefer to work with specific controllers however, you can enumerate them with `loop.Input.GameControllers`-- this is a `ReadOnlySpan<>` containing every controller currently connected to the system.

## Mouse Camera Panning

Now we've got our "scaffolding" out of the way, let's add camera panning with the mouse. We're going to define a single method to handle this called `AdjustCameraViewDirectionKbm()` inside our `CameraInputHandler` class:

```csharp
static void AdjustCameraViewDirectionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
	const float MouseSensitivity = 0.05f; // (1)!

	var cursorDelta = input.MouseCursorDelta; // (2)!
	_currentHorizontalAngle += cursorDelta.X * MouseSensitivity; // (3)!
	_currentVerticalAngle += cursorDelta.Y * MouseSensitivity; // (4)!

	_currentHorizontalAngle = _currentHorizontalAngle.Normalized; // (5)!
	_currentVerticalAngle = _currentVerticalAngle.Clamp( // (6)!
		-Angle.QuarterCircle, 
		Angle.QuarterCircle
	); 

	_currentHorizontalPlaneDir = 
		Direction.Forward * (_currentHorizontalAngle % Direction.Down); // (7)!

	var cameraLeft = Direction.FromDualOrthogonalization( // (8)!
		Direction.Up, 
		_currentHorizontalPlaneDir
	);
	var verticalTiltRot = _currentVerticalAngle % cameraLeft;
	
	camera.SetViewAndUpDirection( // (9)!
		_currentHorizontalPlaneDir * verticalTiltRot, 
		Direction.Up * verticalTiltRot
	);
}
```

1.	This const just sets how fast the camera should pan around, and will depend a bit on your mouse's DPI setting. Feel free to adjust this value to taste.
2.	`input.MouseCursorDelta` returns an `XYPair<int>` which indicates how many pixels the mouse cursor moved this frame. 

	You don't need to know everything about the `XYPair<T>` type right now, all you need to know is that it has an `X` property and a `Y` property (as its name implies). In this case the `X` property tells us how many pixels the mouse moved in the left/right direction, and the `Y` property tells us how many pixels it moved in the up/down direction.

	The window's "origin" point is its top-left corner, so:

	* A positive `X` value means the cursor moved right. A negative `X` value means the cursor moved left.
	* A positive `Y` value means the cursor moved down. A negative `Y` value means the cursor moved up.

3.	Here we're adding `cursorDelta.X * MouseSensitivity` degrees to `_currentHorizontalAngle`.

	* If the user has not moved the mouse left or right this frame, `_currentHorizontalAngle` will not change.
	* If the user has moved the mouse to the right, `_currentHorizontalAngle` will be increased. 
	* If the user has moved the mouse to the left, `_currentHorizontalAngle` will be decreased.

4.	Here we're adding `cursorDelta.Y * MouseSensitivity` degrees to `_currentVerticalAngle`.

	* If the user has not moved the mouse up or down this frame, `_currentVerticalAngle` will not change
	* If the user has moved the mouse down, `_currentVerticalAngle` will be increased. 
	* If the user has moved the mouse up, `_currentVerticalAngle` will be decreased.

5.	On this step we're *normalizing* our horizontal angle. Normalizing just means we're making sure it stays within the range 0° to 360°.

	For example, if `_currentHorizontalAngle` is 370°, after normalization it will be 10°. If it was -30°, after normalization it will be 330°.

	Although this doesn't actually affect the math in any way, normalizing means we don't accrue floating-point error over time. Imagine if a user keeps panning around to the right for minutes on end; eventually they'll make `_currentHorizontalAngle` really high, at which point a 32-bit float may become too inaccurate and the camera will start "skipping" as it pans.

	Normalizing the value every frame gets rid of this issue.

6.	Here we clamp our vertical angle between -90° and 90° (`Angle.QuarterCircle` is just a static readonly for 90°).

	The point of this is to make sure that as we pan the camera up and down we never "flip over backwards" or "somersault forwards" and end up upside-down. By clamping this value to only ever be 90° up or 90° down, we make sure the viewer can only ever look directly up or down but no further.

	As a side effect, like normalizing the horizontal angle above, this also helps prevent floating-point errors.

7.	Now we set our `_currentHorizontalPlaneDir` according to the newly-calculated `_currentHorizontalAngle`.

	This line is simply rotating `Direction.Forward` by `_currentHorizontalAngle` around the `Down` axis (clockwise):

	* `(_currentHorizontalAngle % Direction.Down)` creates a rotation: The current horizontal angle *around* `Down`.
	* `Direction.Forward * (_currentHorizontalAngle % Direction.Down)` is rotating `Direction.Forward` by that rotation. The multiply-operator is defined between a `Direction` and a `Rotation` and produces another `Direction` which is the rotated input direction.

	To help visualize this, stick a pencil "forward" towards your monitor. Now imagine rotating it by a number of degrees around the up/down axis. The new direction it's facing is what we're storing on this line in `_currentHorizontalPlaneDir`.

8.	In the previous line we calculated the horizontal view direction for the camera. However, we also need to know how to tilt that direction up or down according to the *vertical* angle.

	`verticalTiltRot` is a rotation we're calculating on the next line that we will use to tilt our horizontal view direction up or down by rotating it. To create that rotation, we first need to find the camera's "left-hand" axis (i.e. the direction that points to the left of the camera).

	`Direction.FromDualOrthogonalization()` is a static method on the `Direction` type that finds a direction that is orthogonal to two other directions. For example, `Direction.FromDualOrthogonalization(Direction.Left, Direction.Up)` will return `Direction.Forward`. 
	
	In this case we want to find a direction that is orthogonal to both the `Up` direction and our horizontal camera direction. This will return our "left-hand" axis that points out to the left of our look direction.

	We then define a rotation as the `_currentVerticalAngle` around this left-hand axis.

	??? question "`FromDualOrthogonalization()`: Left or right?"
		You might be wondering how we know that `Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir)` gives us the left-hand camera direction; after all the right-hand direction is also an equally valid answer to the question of finding an orthogonal direction (it's orthogonal to both `Up` and `_currentHorizontalPlaneDir` also, just like the left-hand direction). 
		
		The answer is that `FromDualOrthogonalization()` follows the right-hand-rule:

		* Using your right hand, point your index finger towards the direction of the first argument (in this case `Up`).
		* Using the same hand, now point your middle finger towards the direction of the second argument (in this case `_currentHorizontalPlaneDir`).
		* Finally, on that hand, extend your thumb out so it's orthogonal to both your index and middle fingers: This is the direction `FromDualOrthogonalization()` will return.

		If you ever want to find the "opposite" answer, just swap the arguments around.

9.	Finally, we invoke a method on our `camera` called `SetViewAndUpDirection()` to set the view direction of the camera and its "up" direction at the same time.

	* `camera.ViewDirection` is the direction in which the camera is looking.
	* `camera.UpDirection` is the direction that is pointing "up" from the camera, i.e. this property determines which way up you're "holding" the camera.

	For example, we could have a camera whose `ViewDirection` is `Forward` but whose `UpDirection` is `Down`: This would be a camera looking forward but viewing the world "upside-down".

	To calculate the view direction, we specify `_currentHorizontalPlaneDir * verticalTiltRot`: That's basically rotating our horizontal view direction around the camera's left-axis by the up/down tilt calculated previously.

	To calculate the up direction, we specify `Direction.Up * verticalTiltRot`, which is just rotating the `Up` direction by the same tilt.

Now just call this method inside `TickKbm()` and you'll be able to move the camera using the mouse:

```csharp
public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
	AdjustCameraViewDirectionKbm(input, camera, deltaTime);
}
```

## Keyboard: Camera Movement

Now let's make it so we can use the keyboard to move the camera around in our scene. Add another method, `AdjustCameraPositionKbm()`:

```csharp
static void AdjustCameraPositionKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
	var positiveHorizontalYDir = camera.ViewDirection; // (1)!
	var positiveHorizontalXDir = Direction.FromDualOrthogonalization( // (2)!
		Direction.Up, 
		_currentHorizontalPlaneDir
	);

	var horizontalMovement = XYPair<float>.Zero; // (3)!
	var verticalMovement = 0f; // (4)!
	foreach (var currentKey in input.CurrentlyPressedKeys) { // (5)!
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

	var horizontalMovementVect = // (6)!
		(positiveHorizontalXDir * horizontalMovement.X) 
		+ (positiveHorizontalYDir * horizontalMovement.Y);

	var verticalMovementVect = Direction.Up * verticalMovement; // (7)!

	var sumMovementVect = // (8)!
		(horizontalMovementVect + verticalMovementVect)
		.WithLength(CameraMovementSpeed * deltaTime);

	camera.MoveBy(sumMovementVect); // (9)!
}
```

1. 	Overall, we're setting up controls for three directions, `positiveHorizontalYDir`, `positiveHorizontalXDir`, and `Direction.Up`. The horizontal directions are the two directions we will move the camera around when the user is holding any of the arrow keys. The vertical direction is just `Up`.

	On this line we're setting which way we want the camera to move when we're holding the forward/up arrow key. When the user holds the up arrow key we want the camera to move in the direction it's looking, so we simply set `positiveHorizontalYDir` to `camera.ViewDirection`.

2.	On this line we set the other horizontal direction, which we want to be to the camera's left side.

	We calculate that left-side direction using our friend `Direction.FromDualOrthogonalization()` again, to find the direction that is orthogonal to both `Up` and our `positiveHorizontalYDir` that we set in the previous line to the camera's view direction.

	Incidentally: We don't create a `positiveVerticalDir` anywhere because it's just `Direction.Up`.

3. 	Here we define an `XYPair<float>` called `horizontalMovement` and initialize it to zero. 

	Further below we will set `X` and `Y` to one of `-1f`, `0f`, or `1f` depending on which arrow keys are currently held down.

4.	And here we set a `verticalMovement` value as just a `float` and also initialize it to zero.

	Much like the `horizontalMovement` properties we will set this value to one of `-1f`, `0f`, or `1f` depending on which keys are held down.

5.	This foreach loop is iterating through every keyboard and mouse key that the user is currently holding down in this frame.

	We then switch over each key (`switch (currentKey) { ... }`) and add or remove `1f` to/from `horizontalMovement.X`, `horizontalMovement.Y`, or `verticalMovement` depending on which key is being held down.

	For example, if the user is holding the `ArrowUp` key, we add `1f` to `horizontalMovement.Y`. Conversely, if the user is holding the `ArrowDown` key, we subtract `1f` from that same property. When the loop finishes we will know which directions through space the user wishes to move the camera.

	One nice thing about this approach also is that "opposing" movement keys automatically cancel each other out. If the user is holding both `ArrowUp` and `ArrowDown` the resultant value for `horizontalMovement.Y` will be `0f`.

6.	Here we create a `Vect` that is just multiplying `X` and `Y` of `horizontalMovement` by `positiveHorizontalXDir` and `positiveHorizontalYDir` respectively. 

	Because we know that `X`/`Y` will only ever be `-1f,` `0f`, or `1f`, we know that this will only ever be either adding or removing 1 meter of `positiveHorizontalXDir` and `positiveHorizontalYDir` (or nothing at all).

	In other words, `horizontalMovementVect` will end up being a vect pointing in the direction we want the camera to move in its horizontal plane.

7.	Here we create a `Vect` indicating which way we want the camera to move in the `Up`/`Down` axis by simply multiplying `verticalMovement` by `Direction.Up`.

	Because `verticalMovement` is going to be either `-1f`, `0f`, or `1f`, 

And like before, don't forget to actually call this method from inside `TickKbm()`:

```csharp
public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, Camera camera, float deltaTime) {
	AdjustCameraViewDirectionKbm(input, camera, deltaTime);
	AdjustCameraPositionKbm(input, camera, deltaTime); // (1)!
}
```

1. 	Note that we invoke this *after* `AdjustCameraViewDirectionKbm()`.

	This is important if you don't want your left/right/forward/back camera movement to always be one frame "out of sync" with which way the camera is looking.

## Complete Example

Here's the complete example that puts everything above together in one snippet:

```csharp
window.LockCursor = true;

while (!loop.Input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;

	CameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime);
	CameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);

	renderer.Render();
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

		var horizontalRotationStrength = input.RightStickPosition.DisplacementHorizontalWithDeadzone;
		var verticalRotationStrength = input.RightStickPosition.DisplacementVerticalWithDeadzone;

		_currentHorizontalAngle += StickSensitivity * horizontalRotationStrength * deltaTime;
		_currentHorizontalAngle = _currentHorizontalAngle.Normalized;

		_currentVerticalAngle -= StickSensitivity * verticalRotationStrength * deltaTime;
		_currentVerticalAngle = _currentVerticalAngle.Clamp(-Angle.QuarterCircle, Angle.QuarterCircle);

		_currentHorizontalPlaneDir = Direction.Forward * (_currentHorizontalAngle % Direction.Down);
		var verticalTiltRot = _currentVerticalAngle % Direction.FromDualOrthogonalization(Direction.Up, _currentHorizontalPlaneDir);

		camera.SetViewAndUpDirection(_currentHorizontalPlaneDir * verticalTiltRot, Direction.Up * verticalTiltRot);
	}

	static void AdjustCameraPositionGamepad(ILatestGameControllerInputStateRetriever input, Camera camera, float deltaTime) {
		var verticalMovementMultiplier = input.RightTriggerPosition.DisplacementWithDeadzone - input.LeftTriggerPosition.DisplacementWithDeadzone;
		var verticalMovementVect = verticalMovementMultiplier * Direction.Up;

		var horizontalMovementVect = Vect.Zero;
		var stickDisplacement = input.LeftStickPosition.Displacement;
		var stickAngle = input.LeftStickPosition.GetPolarAngle();

		if (stickAngle is { } horizontalMovementAngle) {
			var horizontalMovementDir = _currentHorizontalPlaneDir * (Direction.Up % (horizontalMovementAngle - Angle.QuarterCircle));
			horizontalMovementVect = horizontalMovementDir * stickDisplacement;
		}


		var sumMovementVect = (horizontalMovementVect + verticalMovementVect).WithLength(CameraMovementSpeed * deltaTime);
		camera.MoveBy(sumMovementVect);
	}
}
```