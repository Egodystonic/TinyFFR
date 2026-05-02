---
title: Pan-Tilt-Zoom Camera Controller
---

## Summary

The `PanTiltZoomCameraController` allows you to place a camera in a specific `Position` and then control where it's looking at via the `Pan`, `Tilt`, and `Zoom` properties.

* Adjusting `Pan` turns the camera left or right;
* Adjusting `Tilt` tilts the camera up or down;
* Adjusting `Zoom` zooms the camera in or out.

Created via `#!csharp camera.CreateController<PanTiltZoomCameraController>()`. Like all controllers, you must invoke `Progress()` every frame in order to make the controller actually manipulate the camera.

## Properties

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   The location of the camera in the world.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   The local "up" direction of this camera. This is the direction the camera will tilt towards when increasing `Tilt`.

	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ZeroPanTiltDirection`

:   This is the direction the camera should point when `Pan` and `Tilt` are at zero.

	Defaults to `Direction.Forward`.
	
----

<span class="def-icon">:material-card-bulleted-outline:</span> `Pan`

:   Sets the left/right view angle of the camera.

	Increasing this value turns the camera to the left; decreasing to the right.

	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Tilt`

:   Sets the up/down view angle of the camera.

	Increasing this value tilts the camera upward; decreasing downward.

	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Zoom`

:   A normalized value between `0f` and `1f` (representing 0% to 100% zoom). Sets how zoomed-in the camera is.

	A value of `0f` is 0% zoomed in (e.g. fully zoomed out); a value of `1f` is 100% zoomed in.

	Defaults to `0.5f` (i.e. 50%).

----

<span class="def-icon">:material-card-bulleted-outline:</span> `PanRange`

:   Can be set to any value between `0°` and `360°` to set the maximum amount the camera is permitted to pan left/right from its `ZeroPanTiltDirection`.

	This value is applied 50% in either direction equally (i.e. a value of `160°` results in the camera being able to pan 80° left and 80° right).

	Can be set to `null` to remove any limit.
	
	Defaults to `160°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxTiltUp`

:   Can be set to any value between `0°` and `180°` to set the maximum amount the camera is permitted to tilt upward from its `ZeroPanTiltDirection`.
	
	Defaults to `35°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxTiltDown`

:   Can be set to any value between `0°` and `180°` to set the maximum amount the camera is permitted to tilt upward from its `ZeroPanTiltDirection`.
	
	Defaults to `55°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxZoomInFov`

:   Determines the vertical camera FOV at max zoom in. This should be smaller than or equal to `MaxZoomOutFov`.

	Defaults to `15°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxZoomOutFov`

:   Determines the vertical camera FOV at max zoom in. This should be smaller than or equal to `MaxZoomOutFov`.

	Defaults to `90°`.

## Reacting to Input

### Adjusting Pan

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPan(Angle adjustmentPerSec, float deltaTime)`

:   Adjusts `Pan` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaMouseCursor(XYPair<int> cursorDelta, Angle adjustmentPerPixel, Axis2D axis = Axis2D.X, bool invertMouseControl = false)`

:   Adjusts `Pan` according to the the captured mouse cursor movement for this frame.

	`cursorDelta` is expected to be an `XYPair<int>` representing the number of pixels the mouse has moved along the X and Y axis. Most commonly you will pass [MouseCursorDelta from the keyboard/mouse input retriever](/tutorials/input.md#ilatestkeyboardandmouseinputretriever) as this parameter.
	
	`adjustmentPerPixel` 

## Smoothing
