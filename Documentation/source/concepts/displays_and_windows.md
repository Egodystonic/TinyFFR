---
title: Displays & Windows
description: This page explains the concept of displays and windows in TinyFFR.
---

## Display Discovery

Displays connected to the host machine can be found using the `IDisplayDiscoverer` interface; accessed via the `DisplayDiscoverer` property on the factory.

The `Primary` property returns the primary display or `null` if there are no displays connected. This property is guaranteed to not be null as long as one display is detected on the system.

Otherwise, the `All` property returns a `ReadOnlySpan<Display>`. Use this property to iterate/discover all displays connected to the system. The span may be empty (0 length) if there are no displays connected, but can not be `null`.

### The Display Type

The `Display` type is considered a resource but does not need to be disposed (in fact, there is no `Dispose()` method); because it is an immutable part of the host environment.

In a similar vein, there are no settable/mutable properties on the `Display` type. The following properties are all read-only:

<span class="def-icon">:material-card-bulleted-outline:</span> `IsPrimary`

:   Indicates whether this is the primary display or not.

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   Returns the system name of the display as a `ReadOnlySpan<char>`.

<span class="def-icon">:material-card-bulleted-outline:</span> `SupportedDisplayModes`

:   This returns a `ReadOnlySpan<DisplayMode>`.

	Each supported `DisplayMode` has an `XYPair<int> Resolution` coupled with an `int RefreshRateHz`.

	If you just want the highest supported resolution or refresh rate; use one of the following two convenience properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `HighestSupportedResolutionMode`

:   Returns the `DisplayMode` of this display with the highest resolution.

	In the case where multiple display modes share the highest resolution, this property will return the one with the highest refresh rate.

<span class="def-icon">:material-card-bulleted-outline:</span> `HighestSupportedRefreshRateMode`

:   Returns the `DisplayMode` of this display with the highest refresh rate.

	In the case where multiple display modes share the highest refresh rate, this property will return the one with the highest resolution.

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentResolution`

:   Tells you the resolution of the display as it is currently set.

## Window Building

It is possible to create windows using an `IWindowBuilder`; accessed via the `WindowBuilder` property on the factory.

The `IWindowBuilder` only offers one method; `CreateWindow()`. The only required parameter for `CreateWindow()` is the `Display` on which to show the window.

### Positioning

When supplying a position for a `Window` (either via `CreateWindow()` or by setting the `Position` property on the `Window` itself):

* The position is relative to the window's `Display`.
* Each `Display`'s (0, 0) point is at its top left corner ([by convention](conventions.md)).

Therefore, setting the position to `(0, 0)` will always move the window to the top-left corner of the selected `Display`.

You can also set the `Display` after the `Window` is created (i.e. `#!csharp window.Display = display2;`) and the window will keep its relative position on the new display.

### Multiple Windows

It is possible to operate multiple windows in one application. You can also operate independent `Scene`s and `Renderer`s (or render the same scene twice from different camera angles, etc).

For more information, see [Scenes & Rendering](scenes_and_rendering.md).