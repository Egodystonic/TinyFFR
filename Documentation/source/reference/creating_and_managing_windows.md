---
title: Creating / Managing Windows
description: Information on how to create and manage windows with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Windows can be built via `factory.WindowBuilder`. :material-arrow-right: [The WindowBuilder](#the-windowbuilder)
    * Each active `Window` can have its size, fullscreen state, icon, and more set. :material-arrow-right: [The Window Type](#the-window-type)

</div>

## The WindowBuilder

```csharp
var primaryDisplay = factory.DisplayDiscoverer.Primary
	?? throw new InvalidOperationException("No displays connected to this machine.");
var newWindow = factory.WindowBuilder.CreateWindow(primaryDisplay);
newWindow.SetTitle("My Application");
newWindow.SetIcon(pathToIconFile);
newWindow.Size = (1920, 1080);
newWindow.FullscreenStyle = WindowFullscreenStyle.NotFullscreen;
```


It is possible to create windows using an `IWindowBuilder`; accessed via the `WindowBuilder` property on the factory.

The `IWindowBuilder` only offers one method; `CreateWindow()`. The only required parameter for `CreateWindow()` is the `Display` on which to initially show the window.

??? failure "Not Supported in Headless Mode"
	The window builder only works when the factory is __not__ created in "headless mode", e.g.:
	
	```csharp
	var factory = new LocalTinyFfrFactory(
		// ⚠️ Enabling "HeadlessMode" like this disables window creation
		factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true }
	);
	```
	
	By default, the factory is *not* created in headless mode, but headless mode *is* suggested for UI framework integrations (e.g. Avalonia, WPF, WinForms, etc).
	
## The Window Type

### Positioning

When supplying a position for a `Window` (either via `CreateWindow()` or by setting the `Position` property on the `Window` itself):

* The position is relative to the window's `Display`.
* Each `Display`'s (0, 0) point is at its top left corner.

Therefore, setting the position to `(0, 0)` will always move the window to the top-left corner of the selected `Display`(1).
{ .annotate }

1.	Some display protocols (e.g. Wayland on Linux) prohibit allowing applications setting their own windows' position and target display. In these cases, setting `Display` and `Position` may have no effect.

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
