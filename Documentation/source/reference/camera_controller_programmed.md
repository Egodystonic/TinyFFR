---
title: Programmed Camera Controller
description: Information on the ProgrammedCameraController, which plays a camera along a pre-scripted path of keyframes for cinematics, cut-scenes, fly-throughs, and replays.
---

## Summary

The `ProgrammedCameraController` differs from the other camera controllers: It is not usually expected to react to user input. Instead, the camera's behavior is driven by sequences of pre-defined *keyframes* on three independent tracks:

* The **Position** track — what location the camera occupies over time;
* The **Orientation** track — what direction the camera looks in (and which direction is "up") over time;
* The **Field of View** track — what vertical field of view the camera uses over time.

The three tracks share a single clock, but each has its own keyframes and wraps (e.g. loops) independently. A track with no keyframes leaves that aspect of the camera alone entirely, so you can (for example) script only the camera's position and control its orientation by other means. This makes the `ProgrammedCameraController` suitable for cinematics, cutscenes, fly-throughs, replays, and any other situation where the camera's motion should be scripted ahead of time rather than driven by user input.

Unlike the other camera controllers, this controller has no smoothing settings and no default input controls (calling `SetGlobalSmoothing()` or `AdjustAllViaDefaultControls()` via the `ICameraController` interface does nothing); the keyframes' interpolation algorithms determine how the camera moves. For an overview of how camera controllers work in general, see [Camera Controllers](camera_controllers.md).

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<ProgrammedCameraController>(); // (1)!
camera.Position = startingPosition; // (2)!

controller.AddPositionKeyframe(new( // (3)!
	LengthSeconds: 2f,
	Algorithm: InterpolationAlgorithm<Location>.Linear(),
	TargetValue: new Location(0f, 0f, -3f)
));
controller.AddPositionKeyframe(new( // (4)!
	LengthSeconds: 3f,
	Algorithm: InterpolationAlgorithm<Location>.Linear(),
	TargetValue: new Location(0f, 2f, -3f)
));

controller.PositionTrackWrapping = AnimationWrapStyle.Loop; // (5)!

// Per-frame:
controller.Progress(deltaTime); // (6)!
```

1.	This creates the controller, attached to the given `camera`.

2.	This sets the camera's starting position.

	The programmed keyframe tracks work by adjusting the position/orientation/FoV of the previous camera value consecutively; the first keyframe works by adjusting the camera's initial properties.

3.	This adds the first keyframe to the position track. The camera will spend the first 2 seconds linearly interpolating from its starting position (captured on the first `Progress()` call) to `(0, 0, -3)`.

4.	This adds the second keyframe. The camera will then spend the next 3 seconds linearly interpolating from `(0, 0, -3)` to `(0, 2, -3)`.

5.	This sets the position track to loop indefinitely once it reaches the end. By default, tracks play once and then stop, holding at the final value.

6.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters. For this controller, `Progress()` advances `CurrentTimestampSeconds` by `deltaTime` and applies the appropriate interpolated values to the camera.
	
	On the very first `Progress()` call (when `CurrentTimestampSeconds` is still `0`), this controller captures the camera's current position, view direction, up direction, and FOV as the implicit "start" values for each track's first keyframe to interpolate *from*.

## Keyframes

The controller defines three nested record-struct types, one per track. Each is constructed positionally with the parameters listed below. The position and orientation keyframes also have a second constructor, for sweeping around a pivot and for looking at a target respectively.

Each keyframe interpolates from the camera's existing properties at the start of the keyframe to its own `TargetValue`. This means the first keyframe will interpolate from the camera's starting position/orientation/FoV.

<span class="def-icon">:material-card-bulleted-outline:</span> `ProgrammedCameraController.PositionKeyframe`

:   Represents a single keyframe on the position track.

	* `LengthSeconds` (`float`) — how long this keyframe takes to interpolate from the previous keyframe (or, for the first keyframe, from the camera's starting position) to its `TargetValue`. Must be finite and non-negative; a length of `0f` jumps straight to the `TargetValue`.
	* `Algorithm` (`InterpolationAlgorithm<Location>`) — the [interpolation algorithm](interpolation_algorithms.md) to use for this keyframe (e.g. linear, ease-in, ease-out, etc.).
	* `TargetValue` (`Location`) — the world location the camera should occupy at the end of this keyframe. Must be physically valid.
	* `Pivot` (`Location?`) — *optional*, passed as a fourth constructor argument (or set with an initializer). When supplied, the camera sweeps around this point in an arc instead of travelling in a straight line. Must be physically valid. Defaults to `null` (straight line).
	
	When a `Pivot` is given, the camera's direction from the pivot turns along the shortest arc from where it starts to `TargetValue`, while its distance from the pivot changes gradually from the starting distance to the ending distance. If both ends are equally far from the pivot, the path is part of a circle; if they aren't, it spirals inwards or outwards. The `Algorithm` still controls easing along the arc, and easings that overshoot carry on along the arc before settling.
	
	* Because the shortest arc is used, one keyframe can sweep at most 180°. Use several consecutive keyframes (each with the same pivot) to sweep further, e.g. four 90° keyframes for a full orbit.
	* If the start and end are on exactly opposite sides of the pivot, there is no single shortest arc, and the plane of the sweep is chosen arbitrarily. Move the pivot slightly off the line between them to choose the plane yourself.
	* If either end of the keyframe is exactly at the pivot, the camera travels in a straight line instead.
	* Every built-in `InterpolationAlgorithm` works with a pivot. A `Custom()` algorithm is sampled on a straight line from `(0, 0, 0)` to `(1, 0, 0)`, and the X component of the result is used as the fraction of the arc travelled.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ProgrammedCameraController.OrientationKeyframe`

:   Represents a single keyframe on the orientation track.
	
	* `LengthSeconds` (`float`) — how long this keyframe takes to interpolate from the previous keyframe (or, for the first keyframe, from the camera's starting view/up directions) to its target directions. Must be finite and non-negative.
	* `Algorithm` (`InterpolationAlgorithm<Direction>`) — the interpolation algorithm to use for this keyframe.
	* `TargetViewDirection` (`Direction`) — the camera's view direction at the end of this keyframe. Must be a valid non-zero direction, unless `LookAtTarget` is set (in which case it is ignored).
	* `TargetUpDirection` (`Direction`) — the camera's up direction at the end of this keyframe. Must be a valid non-zero direction.
	* `LookAtTarget` (`Location?`) — *optional*. A point in the world the camera should look at. To use it, pass it as the third constructor argument in place of `TargetViewDirection`, i.e. `new(LengthSeconds, Algorithm, LookAtTarget, TargetUpDirection)`; that constructor sets `TargetViewDirection` to `Direction.None`. Must be physically valid. Defaults to `null` (look in `TargetViewDirection` instead).
	
	When a `LookAtTarget` is given, the direction to the target is recalculated on every `Progress()` from wherever the camera is on that frame (after the position track has been applied, so this works with scripted movement as well as any movement you apply yourself). The camera eases from the previous keyframe's view direction towards the target over the keyframe's length:
	
	* If the previous keyframe also looks at the same target, the camera stays fixed on it for the whole keyframe.
	* To lock on to a target instantly rather than easing towards it, precede the keyframe with a zero-length keyframe looking at the same target.
	* Once the keyframe has finished (e.g. while the track holds its final keyframe under `AnimationWrapStyle.Once`), the camera keeps looking at the target wherever it moves.
	* On any frame where the camera is exactly at the target, it keeps its current view direction.
	
	The view and up directions are interpolated separately, and the up direction is then straightened against the view direction (as with `Camera.SetViewAndUpDirection()`). Avoid keyframes where the view and up directions would pass through being parallel to one another.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ProgrammedCameraController.FieldOfViewKeyframe`

:   Represents a single keyframe on the field-of-view track.
	
	* `LengthSeconds` (`float`) — how long this keyframe takes to interpolate from the previous keyframe (or, for the first keyframe, from the camera's starting FOV) to its `TargetValue`. Must be finite and non-negative.
	* `Algorithm` (`InterpolationAlgorithm<Angle>`) — the interpolation algorithm to use for this keyframe.
	* `TargetValue` (`Angle`) — the vertical field of view the camera should have at the end of this keyframe. Must be within the camera's permitted FOV range (`Camera.FieldOfViewMin` ≤ `TargetValue` ≤ `Camera.FieldOfViewMax`).

### Orbiting a Point

Combining position keyframes that have a `Pivot` with orientation keyframes that have a `LookAtTarget` makes it easy to script an orbit around a subject:

```csharp
var subject = new Location(0f, 1f, 0f);
camera.Position = new Location(0f, 1f, -4f);
camera.LookAt(subject, Direction.Up); // (1)!

var alg = InterpolationAlgorithm<Location>.Linear();
controller.AddPositionKeyframe(new(2f, alg, new Location(4f, 1f, 0f), subject)); // (2)!
controller.AddPositionKeyframe(new(2f, alg, new Location(0f, 1f, 4f), subject));
controller.AddPositionKeyframe(new(2f, alg, new Location(-4f, 1f, 0f), subject));
controller.AddPositionKeyframe(new(2f, alg, new Location(0f, 1f, -4f), subject));
controller.PositionTrackWrapping = AnimationWrapStyle.Loop;

controller.AddOrientationKeyframe(new(0f, InterpolationAlgorithm<Direction>.Linear(), subject, Direction.Up)); // (3)!
```

1.	The camera starts four units from the subject, already looking at it.

2.	Each keyframe sweeps the camera a quarter-turn around `subject`, so four of them make a full orbit. With the track looping, the camera circles the subject indefinitely.

3.	A single zero-length look-at keyframe is enough. It locks on to the subject straight away, and because the orientation track then holds its final keyframe, the camera keeps looking at the subject as it orbits.

## Adding & Clearing Keyframes

<span class="def-icon">:material-code-block-parentheses:</span> `AddPositionKeyframe(PositionKeyframe keyframe)`

:   Appends `keyframe` to the end of the position track. The keyframe is validated: an `ArgumentException` will be thrown if its length or target value(s) are invalid, and an `InvalidObjectException` if its `Algorithm` is a `default` (uninitialized) value.

<span class="def-icon">:material-code-block-parentheses:</span> `AddOrientationKeyframe(OrientationKeyframe keyframe)`

:   Appends `keyframe` to the end of the orientation track. The keyframe is validated: an `ArgumentException` will be thrown if its length or target value(s) are invalid, and an `InvalidObjectException` if its `Algorithm` is a `default` (uninitialized) value.

<span class="def-icon">:material-code-block-parentheses:</span> `AddFieldOfViewKeyframe(FieldOfViewKeyframe keyframe)`

:   Appends `keyframe` to the end of the field-of-view track. The keyframe is validated: an `ArgumentException` will be thrown if its length or target value(s) are invalid, and an `InvalidObjectException` if its `Algorithm` is a `default` (uninitialized) value.

<span class="def-icon">:material-code-block-parentheses:</span> `ClearAllKeyframes()`

:   Removes every keyframe from all three tracks and resets each track's length to `0`. Neither the current timestamp (see below) nor the tracks' wrap styles are reset.

<span class="def-icon">:material-code-block-parentheses:</span> `ResetParametersToDefault()`

:   Removes every keyframe, sets every track's wrap style back to `AnimationWrapStyle.Once`, and resets `CurrentTimestampSeconds` to `0`. Disposing the controller also removes its keyframes.

## Track Properties

#### Wrapping

Each track has its own `AnimationWrapStyle?` controlling what happens once playback reaches the end of the track. Options include:

* `AnimationWrapStyle.Once`: The default. Once the final keyframe has completed, the track holds its final value. Note that the controller *keeps applying* that value to the camera on every `Progress()`, so any changes you make to the camera yourself will be overwritten.
* `AnimationWrapStyle.OncePingPonged`: The controller will play the keyframes in the order specified, and then once again in reverse. Once the first keyframe has completed in reverse, the track holds (and keeps applying) its starting value.
* `AnimationWrapStyle.Loop`: The controller will loop back around to the first keyframe once the track completes indefinitely.
* `AnimationWrapStyle.LoopPingPonged`: The controller will loop back and forward from start to end to start etc indefinitely.
* `null`: The controller will not wrap animation at all. The final keyframe will continue being interpolated indefinitely (e.g. the last movement on each track will be extrapolated in to the future). Extrapolated fields of view are still clamped to the camera's permitted range.

If you want the controller to stop controlling an aspect of the camera altogether, remove that track's keyframes (or stop calling `Progress()`).

<span class="def-icon">:material-card-bulleted-outline:</span> `PositionTrackWrapping`

:   The wrap style applied to the position track. Defaults to `AnimationWrapStyle.Once`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `OrientationTrackWrapping`

:   The wrap style applied to the orientation track. Defaults to `AnimationWrapStyle.Once`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `FieldOfViewTrackWrapping`

:   The wrap style applied to the field-of-view track. Defaults to `AnimationWrapStyle.Once`.

#### Track Lengths (read-only)

<span class="def-icon">:material-card-bulleted-outline:</span> `PositionTrackLengthSeconds`

:   The total length of the position track in seconds.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `OrientationTrackLengthSeconds`

:   The total length of the orientation track in seconds.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `FieldOfViewTrackLengthSeconds`

:   The total length of the field-of-view track in seconds.

## Time Control

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentTimestampSeconds`

:   The current playback timestamp shared by all three tracks, in seconds. `Progress()` adds `deltaTime` to this value each frame.
	
	You can also write to this property directly to seek to a specific point in the animation (for example, to scrub backward or jump to a specific moment).
	
	!!! warning "Initial state capture"
		Whenever `CurrentTimestampSeconds` is exactly `0f`, invoking `Progress()` quietly sets the current camera state as the "starting" keyframe.
		
		Conversely, if you set `CurrentTimestampSeconds` to a non-zero value *before* the first `Progress()`, the camera's state is never captured, and the first keyframes interpolate from default values instead (the origin, looking forward with up pointing up, at a 60° field of view).
		
		If you intend to restart the entire programmed sequence, it's important that you also reset the controlled `Camera` to its starting position/orientation/FoV as well.
