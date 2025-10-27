---
title: Displays & Windows
description: This page explains the concept of displays and windows in TinyFFR.
---

## Display Discovery

Displays connected to the host machine can be found using the `IDisplayDiscoverer` interface; accessed via the `DisplayDiscoverer` property on the factory.

The `Primary` property returns the primary display or `null` if there are no displays connected. This property is guaranteed to not be null as long as one display is detected on the system.

The `HighestResolution` and `HighestRefreshRate` properties return the display with the highest resolution or refresh rate respectively.(1)
{ .annotate }

1. 	In the case a of tie, the returned display will be the one that can support the highest resolution/refresh rate at the tied value.

	If this is still tied, if any display is the `Primary` display, that one will be returned.

	Otherwise, all else being equal, the display that will be returned is the one that first appears in the `All` span.

Otherwise, the `All` property returns a `ReadOnlySpan<Display>`. Use this property to iterate/discover all displays connected to the system. The span may be empty (0 length) if there are no displays connected, but can not be `null`. The convenience property `AtLeastOneDisplayConnected` can be used to determine if any displays are connected.

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

### Size and FullscreenStyle

#### FullscreenStyle

TinyFFR supports three `FullscreenStyle` options for `Window` objects: `NotFullscreen` for a standard window with a border and typical controls (e.g. minimize, restore, close) etc.; and `Fullscreen` or `FullscreenBorderless` for a window that should take the entire display's screen space with no border or typical window controls.

The difference between `Fullscreen` and `FullscreenBorderless` is ultimately in how the operating system and TinyFFR interact: 

* With standard `Fullscreen` TinyFFR takes control of the target display itself, setting the target display's resolution according to your requested window `Size`.
* With `FullscreenBorderless`, the window is instead drawn as an OS window that is set to perfectly match the resolution of the user's desktop, with no borders or controls. The target display's resolution is not adjusted.

In general, `FullscreenBorderless` is recommended over `Fullscreen` as it interacts more nicely with the user's existing desktop configuration (especially concerning multi-monitor setups). However, the size of the window resolution can not be set in this mode. In cases where you wish to set the fullscreen resolution, traditional `Fullscreen` is the only option. Where possible, offer your users the option to configure between the two. Some graphics drivers or desktop OSs may, in some cases, require one option over the other.

#### Size

Setting a window's `Size` property has a different effect depending on its `FullscreenStyle`:

* `NotFullscreen`: The `Size` property simply sets the size of the window.
* `Fullscreen`: The `Size` property sets the resolution of the display. This resolution must be one of the `SupportedDisplayModes`. If you specify a width/height that is not supported, TinyFFR will automatically pick the nearest one for you.
* `FullscreenBorderless`: The `Size` property has no effect, but will be remembered & applied if you change the `FullscreenStyle` again later. 

### Multiple Windows

It is possible to operate multiple windows in one application. You can also operate independent `Scene`s and `Renderer`s (or render the same scene twice from different camera angles, etc).

For more information, see [Scenes & Rendering](scenes_and_rendering.md).