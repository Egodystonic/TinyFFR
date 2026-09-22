---
title: WPF Integration
description: Tutorial on integrating TinyFFR with WPF (Windows Presentation Foundation).
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * You can render TinyFFR scenes in WPF using a `TinyFfrSceneView` control, provided as part of the `Egodystonic.TinyFFR.Wpf` NuGet package. :material-arrow-right: [Installation & Initialization](#installation-initialization)
    * All API interaction should be done on the primary/UI thread, just as you would with WPF itself. :material-arrow-right: [Looping](#looping)
    * You can use WPF's input event system as normal; simultaneously using TinyFFR's for specific controls only via opt-in. :material-arrow-right: [Input](#input)

</div>

## Installation & Initialization

Install `Egodystonic.TinyFFR.Wpf` from Nuget in to your WPF application project.(1)
{ .annotate }

1. This package contains a transitive reference to `Egodystonic.TinyFFR` so you won't need to explicitly install both, but if you do decide to reference both please make sure their version numbers match.

### Factory Setup

When building the `LocalTinyFfrFactory`, you should specify you want to use `HeadlessMode`. This lets TinyFFR know that the UI framework will be supplying the input, windowing, and OS system integration services. Interfaces such as the `IWindowBuilder` and `IDisplayDiscoverer` will not be usable because TinyFFR will defer to WPF's control of these aspects of the system.

To enable headless mode:

```csharp
var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true }
);
```

### TinyFfrSceneView

TinyFFR integration is offered primarily as a `TinyFfrSceneView` control which can be used like any other WPF control. A `TinyFfrSceneView` allows you to invoke `Render()` on a bound `Renderer`(1) (or `RenderAll()` on a `RendererCompositor`, see below), and the updated scene will be displayed on this control's client area:
{ .annotate }

1. You must bind a specific "Bindable" renderer; see bullet points below.

```xml
<tffr:TinyFfrSceneView
    FallbackBrush="LightGray"
    InternalRenderResolution="{Binding InternalRenderResolution}"
    InternalRenderResolutionMax="{Binding InternalRenderResolutionMax}"
    Renderer="{Binding Renderer}" />
```

* The `tffr:` prefix assumes you've declared the namespace somewhere (e.g. in the root declaration for your window/control), for example: `#!xml xmlns:tffr="clr-namespace:Egodystonic.TinyFFR.Wpf;assembly=TinyFFR.Wpf"`.(1)
	{ .annotate }

	1. 	The `assembly=` portion is required because the control lives in a different assembly to your application. Note that the assembly name is `TinyFFR.Wpf`, whereas the Nuget package ID is `Egodystonic.TinyFFR.Wpf`.

* Every property below is backed by a `DependencyProperty`, so they can all be databound (or set directly) in the usual WPF manner.

* The `FallbackBrush` property is optional and can be used to set the fill brush of the control when no renderer has been set and/or when no scene has been rendered.

* The `InternalRenderResolution` property is also optional and can be used to set the internal resolution scenes will be rendered at before being scaled to the size of the control.(1) It is a literal count of pixels and is not multiplied by the display's scaling factor. If left unset, scenes are rendered at the size of the control bounds in physical pixels, which keeps the image sharp on displays using a scaling factor other than 100%.
	{ .annotate }

	1. The height and width of the render resolution of a scene view must both be between `1` and `32768`.

* The `InternalRenderResolutionMax` property is also optional and sets an *upper bound* on the internal render resolution rather than a fixed value.(1) Like `InternalRenderResolution` it is a literal count of pixels and is not multiplied by the display's scaling factor. Whenever the resolution the control would otherwise render at exceeds this bound on either axis, it is scaled down to fit while preserving its aspect ratio: for example, a control that would render at 2000x1500 with an `InternalRenderResolutionMax` of 1000x1000 renders at 1000x750 instead. If both properties are set, both apply: `InternalRenderResolution` selects the resolution and `InternalRenderResolutionMax` then caps it.
	{ .annotate }

	1. The height and width of the maximum render resolution of a scene view must both be between `1` and `32768`.

* The `Renderer` must be created via the `CreateBindableRenderer(...)` extension method on an `IRendererBuilder` instance (or a `RendererCompositor` via `CreateBindableCompositor(...)`). Attempting to databind a non-bindable renderer/compositor will fail. See next section:

### The Renderer

The `Renderer` should be created via `CreateBindableRenderer` using a pre-created `Scene` and `Camera`; e.g:

```csharp 
Renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator); // (1)!
```

1. This extension method is provided in the `Egodystonic.TinyFFR.Wpf` package under the `Egodystonic.TinyFFR.Rendering` namespace (the same namespace as the `IRendererBuilder` type).

Each time you invoke `Render()` on this `Renderer` it will invoke an update to the frame/image for the `TinyFfrSceneView` that has it bound to the `Renderer` property.(1)
{ .annotate }

1. 	A bindable `Renderer` supplies frames to one `TinyFfrSceneView` at a time. Binding the same `Renderer` to a second scene view silently takes it away from the first, which will simply freeze on whatever frame it last received. If you want to show the same `Scene` in two places at once, create a second bindable `Renderer` for it.

You should not bind a disposed `Renderer` or a non-bindable `Renderer` to a `TinyFfrSceneView`. Similarly, you should not dispose a bound `Renderer` and leave it bound. The `Renderer` property *can* be set to `null` (at which point the `FallbackBrush` will be used to fill the control's client area).

#### Alternative: The Compositor

Rather than binding a single `Renderer`, it is possible to bind a `RendererCompositor` (see [Compositing](compositing.md)) to the `Compositor` property. Only one of `Renderer` and `Compositor` can be bound at any given time.

The compositor must likewise be created via `CreateBindableCompositor(...)`; e.g:

```csharp 
Compositor = factory.RendererBuilder.CreateBindableCompositor(); // (1)!
```

1. Like `CreateBindableRenderer`, this extension method is provided in the `Egodystonic.TinyFFR.Wpf` package under the `Egodystonic.TinyFFR.Rendering` namespace.

## Looping

???+ danger "Primary Thread Safety"
	All interaction with your UI and TinyFFR should be done in the UI Dispatcher context (e.g. on the UI thread). WPF and TinyFFR share the same primary thread, and you can treat all interaction with TinyFFR's API like you would interaction with WPF (i.e. all interaction should be marshalled on to the primary/UI context).
	
	[Asynchronous TinyFFR operations](asynchronous_loading.md) still work as usual. TinyFFR schedules any continuation back on the UI thread context by default.

If your application requires the scene view to be animated you can achieve this via the extension method "`StartWpfUiLoop()`" supplied on the `ILocalApplicationLoopBuilder` interface.(1)
{ .annotate }

1. This extension method is provided in the `Egodystonic.TinyFFR.Wpf` package under the `Egodystonic.TinyFFR.Wpf` namespace.

This method allows you to supply a callback delegate that will be invoked at a target frequency (e.g. 30Hz) on the UI context (e.g. the UI thread). You can render your scene on each tick (like you would inside an `ApplicationLoop` in a standalone TinyFFR application):

```csharp
// You can start this loop like so...
var loopTerminationDisposable = factory.ApplicationLoopBuilder.StartWpfUiLoop(Tick);

// Example Tick function
void Tick(TimeSpan tickIterationTime) {
	var deltaTime = tickIterationTime.AsDeltaTime()

	// Render one frame each tick
	Renderer.Render();

	// Manipulate objects in the scene also
	_instance.RotateBy(deltaTime * 130f % Direction.Up);
	_instance.RotateBy(deltaTime * 80f % Direction.Right);

	// It's safe to touch UI/viewmodel data and TinyFFR objects here; 
	// this function is guaranteed to be executed on the UI context
	MyBoundProperty = GetSomeNewValue();
}

// Dispose "loopTerminationDisposable" to stop the loop...
loopTerminationDisposable.Dispose();
```

This function schedules your tick/render loop on the pre-existing WPF dispatcher loop, integrating it in to the UI subsystem, and is preferred over creating an `ApplicationLoop` manually (see warning above). The function takes optional arguments as follows:

<span class="def-icon">:material-code-block-parentheses:</span> `StartWpfUiLoop(tickCallback, tickRateHz, priority, name)`

:   * `tickCallback` is the `Action<TimeSpan>` that you wish to be invoked on the UI thread/context. The singular argument is a `TimeSpan` indicating the "delta time" since the last 'tick'.

	* `tickRateHz` is optional, this sets the target framerate for the animation loop. Please note that the maximum framerate may be limited by the UI framework compositor and/or dispatcher loop mechanism. You should also note that the 'consistency' (e.g. jitter) of the framerate will likely be much more variable than with a typical standalone TinyFFR application loop.

	* `priority` is optional, this sets the [DispatcherPriority](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcherpriority) with which this loop will be instigated.

	* `name` is optional, this will set the name of the underlying `ApplicationLoop` resource used internally by TinyFFR.

	This function returns an `IDisposable` that should be disposed when you wish to terminate the loop.

!!! warning "Application.Current must be set"
	The loop is scheduled on `Application.Current.Dispatcher`, so a WPF `Application` must already exist when you call `StartWpfUiLoop()`. This is the case for any normal WPF application, but it is worth bearing in mind if you are hosting WPF content inside another framework.

## Input

You may of course wish to use WPF's built-in input layer; simultaneously however TinyFFR's own input abstraction (as detailed in [Keyboard / Mouse Input](keyboard_and_mouse_input.md)) is available when using WPF integration. This can be useful when you want to create a control scheme that works the same way both within WPF and in a standalone window.

??? info "Input Sourcing" 
	The sourced UI event data is taken from WPF's reporting; not from direct OS system requests as is typical in a standalone application. 
	
	This can create some occasional discrepancies in the way some key / mouse events are handled, and care should be taken to test any input code on both systems thoroughly.

To use TinyFFR's input abstraction, use the `StartWpfUiLoop()` overload that supplies an `ILatestInputRetriever`:

```csharp
var loopTerminationDisposable = factory.ApplicationLoopBuilder.StartWpfUiLoop(
	mySceneView, // (1)!
	Tick
);

void Tick(TimeSpan tickIterationTime, ILatestInputRetriever input) { // (2)!
	var deltaTime = tickIterationTime.AsDeltaTime();

	// Identical to the equivalent code in a standalone application
	_cameraController.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime);
	_cameraController.Progress(deltaTime);

	Renderer.Render();
}
```

1. 	This parameter tells TinyFFR which WPF [UIElement](https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement) should be used as the source of all collected input events.

	In this case we're using our `TinyFfrSceneView` (which inherits from `Control` -> `UIElement`).

2.	Because we passed the `TinyFfrSceneView` to `StartWpfUiLoop()` above, we can now grab input events from that scene view just like we would in a standalone application.

	In this example we're using keyboard/mouse controls to drive a [Camera Controller](camera_controllers.md).


<span class="def-icon">:material-code-block-parentheses:</span> `StartWpfUiLoop(inputSource, tickCallback, tickRateHz, priority, name)`

:   * `inputSource` is the [`UIElement`](https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement) whose input events will be observed; usually your `TinyFfrSceneView`, but you may pass any element (e.g. the containing window) if you want a wider scope.

	* `tickCallback` is the `Action<TimeSpan, ILatestInputRetriever>` that you wish to be invoked on the UI thread/context. The second argument supplies the input state accumulated since the previous 'tick'.

	* `tickRateHz` behaves as described above.

	* `priority` and `name` behave as described above.

	This function returns an `IDisposable` that should be disposed when you wish to terminate the loop; doing so also unsubscribes from the `inputSource`'s events.

#### Scope and semantics

* **Keyboard** events are observed only whilst `inputSource` has focus. `TinyFfrSceneView` is focusable and takes focus when clicked, so a user must click in to the scene view before keyboard input reaches TinyFFR. This is deliberate: it means typing in a `TextBox` elsewhere in your application does not also drive your scene.
* Keyboard events are observed via WPF's tunnelling *preview* events (e.g. `PreviewKeyDown`), so they reach TinyFFR before any nested control has a chance to mark them handled.
* **Mouse** buttons and the scroll wheel are observed whilst the pointer is over `inputSource`. Whilst a button is held, the mouse is captured, so `MouseCursorDelta` keeps accumulating even if the pointer leaves the element (which makes drag-to-look work as expected).
* `MouseCursorPosition` is relative to `inputSource` (`(0, 0)` being its top-left corner) and expressed in that element's own coordinate space. You can pass it straight to `Renderer.CreateRayFromRenderSurface()` and similar methods: Those methods convert it to the render buffer's pixel space for you, accounting both for the display's scaling factor and for any `InternalRenderResolution` you have set. Pass `disableDpiScalingAdjustment: true` if you are supplying a coordinate that is already in buffer space. For this to line up, `inputSource` must be the scene view itself.
* Keys and buttons are released automatically when focus or mouse capture is lost, so holding a key and then switching away from your application will not leave that key "stuck" down.
* Game controllers are not currently supported under UI framework integration. `GameControllers` is always empty and `GameControllersCombined` always reports a neutral state.
