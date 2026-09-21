---
title: Windows Forms Integration
description: Tutorial on integrating TinyFFR with Windows Forms.
---

It's possible to render TinyFFR scenes as a Windows Forms control.

## Installation & Initialization

Install `Egodystonic.TinyFFR.WinForms` from Nuget in to your Windows Forms application project.(1)
{ .annotate }

1. This package contains a transitive reference to `Egodystonic.TinyFFR` so you won't need to explicitly install both, but if you do decide to reference both please make sure their version numbers match.

### Factory Setup

When building the `LocalTinyFfrFactory`, you should specify you want to use `HeadlessMode`. This lets TinyFFR know that the UI framework will be supplying the input, windowing, and OS system integration services. Interfaces such as the `IWindowBuilder` and `IDisplayDiscoverer` will not be usable because TinyFFR will defer to Windows Forms' control of these aspects of the system.

To enable headless mode:

```csharp
var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true }
);
```

### TinyFfrSceneView

TinyFFR integration is offered primarily as a `TinyFfrSceneView` control which can be used like any other Windows Forms control. A `TinyFfrSceneView` allows you to invoke `Render()` on a bound `Renderer`(1) (or `RenderAll()` on a `RendererCompositor`, see below), and the updated scene will be displayed on this control's client area:
{ .annotate }

1. You must bind a specific "Bindable" renderer; see bullet points below.

`TinyFfrSceneView` is a `UserControl`, so you can drop it on to a form in the designer and set it up there, or construct and configure it in code:

```csharp
var sceneView = new TinyFfrSceneView { // (1)!
	Dock = DockStyle.Fill,
	FallbackBrush = new SolidBrush(Color.LightGray),
	InternalRenderResolution = new Size(1280, 720),
	InternalRenderResolutionMax = new Size(1920, 1080)
};
Controls.Add(sceneView);

sceneView.Renderer = myBindableRenderer;
```

1. The control lives in the `Egodystonic.TinyFFR.WinForms` namespace.

* Windows Forms has no XAML-style markup or data binding, so unlike the [Avalonia](avalonia_integration.md) and [WPF](wpf_integration.md) integrations these are plain properties: assign to them directly (or via the designer) whenever you want to change them.

* The `FallbackBrush` property is optional and can be used to set the fill brush of the control when no renderer has been set and/or when no scene has been rendered. It is a `System.Drawing.Brush`.

* The `InternalRenderResolution` property is also optional and can be used to set the internal resolution scenes will be rendered at before being scaled to the size of the control.(1) It is a `System.Drawing.Size` and a literal count of pixels; it is not multiplied by the display's scaling factor. If left unset, scenes are rendered at the size of the control's client area in physical pixels, which keeps the image sharp on displays using a scaling factor other than 100%.
	{ .annotate }

	1. The height and width of the render resolution of a scene view must both be between `1` and `32768`; assigning anything else throws an `ArgumentException`.

* The `InternalRenderResolutionMax` property is also optional and sets an *upper bound* on the internal render resolution rather than a fixed value.(1) Like `InternalRenderResolution` it is a literal count of pixels and is not multiplied by the display's scaling factor. Whenever the resolution the control would otherwise render at exceeds this bound on either axis, it is scaled down to fit while preserving its aspect ratio: for example, a control that would render at 2000x1500 with an `InternalRenderResolutionMax` of 1000x1000 renders at 1000x750 instead. If both properties are set, both apply: `InternalRenderResolution` selects the resolution and `InternalRenderResolutionMax` then caps it.
	{ .annotate }

	1. The height and width of the maximum render resolution of a scene view must both be between `1` and `32768`; assigning anything else throws an `ArgumentException`.

* The `Renderer` must be created via the `CreateBindableRenderer(...)` extension method on an `IRendererBuilder` instance (or a `RendererCompositor` via `CreateBindableCompositor(...)`). Assigning a non-bindable renderer/compositor throws an `ArgumentException`. See next section:

!!! note "DPI awareness"
	The control derives its render resolution from its client area, which Windows Forms only reports in true physical pixels when your application is per-monitor DPI aware. This is the default for new projects created with `ApplicationConfiguration.Initialize()`; on an older or hand-written entry point you may need to opt in yourself, otherwise scenes will be rendered at the unscaled size and then stretched by the OS.

### The Renderer

The `Renderer` should be created via `CreateBindableRenderer` using a pre-created `Scene` and `Camera`; e.g:

```csharp 
sceneView.Renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator); // (1)!
```

1. This extension method is provided in the `Egodystonic.TinyFFR.WinForms` package under the `Egodystonic.TinyFFR.Rendering` namespace (the same namespace as the `IRendererBuilder` type).

Each time you invoke `Render()` on this `Renderer` it will invoke an update to the frame/image for the `TinyFfrSceneView` that has it assigned to the `Renderer` property.(1)
{ .annotate }

1. 	A bindable `Renderer` supplies frames to one `TinyFfrSceneView` at a time. Assigning the same `Renderer` to a second scene view silently takes it away from the first, which will simply freeze on whatever frame it last received. If you want to show the same `Scene` in two places at once, create a second bindable `Renderer` for it.

You should not assign a disposed `Renderer` or a non-bindable `Renderer` to a `TinyFfrSceneView`. Similarly, you should not dispose a bound `Renderer` and leave it bound. The `Renderer` property *can* be set to `null` (at which point the `FallbackBrush` will be used to fill the control's client area).

#### Alternative: The Compositor

Rather than binding a single `Renderer`, it is possible to assign a `RendererCompositor` (see [Compositing](compositing.md)) to the `Compositor` property. Only one of `Renderer` and `Compositor` can be bound at any given time.

The compositor must likewise be created via `CreateBindableCompositor(...)`; e.g:

```csharp 
sceneView.Compositor = factory.RendererBuilder.CreateBindableCompositor(); // (1)!
```

1. Like `CreateBindableRenderer`, this extension method is provided in the `Egodystonic.TinyFFR.WinForms` package under the `Egodystonic.TinyFFR.Rendering` namespace.

## Looping

???+ danger "Primary Thread Safety"
	All interaction with your UI and TinyFFR should be done on the UI thread (e.g. the thread running the message loop). Windows Forms and TinyFFR share the same primary thread, and you can treat all interaction with TinyFFR's API like you would interaction with Windows Forms (i.e. all interaction should be marshalled on to the primary/UI thread, for example with `Control.Invoke()`).
	
	[Asynchronous TinyFFR operations](asynchronous_loading.md) still work as usual. TinyFFR schedules any continuation back on the UI thread context by default.

If your application requires the scene view to be animated you can achieve this via the extension method "`StartWinFormsUiLoop()`" supplied on the `ILocalApplicationLoopBuilder` interface.(1)
{ .annotate }

1. This extension method is provided in the `Egodystonic.TinyFFR.WinForms` package under the `Egodystonic.TinyFFR.WinForms` namespace.

This method allows you to supply a callback delegate that will be invoked at a target frequency (e.g. 30Hz) on the UI thread. You can render your scene on each tick (like you would inside an `ApplicationLoop` in a standalone TinyFFR application):

```csharp
// You can start this loop like so...
var loopTerminationDisposable = factory.ApplicationLoopBuilder.StartWinFormsUiLoop(Tick);

// Example Tick function
void Tick(TimeSpan tickIterationTime) {
	var deltaTime = tickIterationTime.AsDeltaTime()

	// Render one frame each tick
	Renderer.Render();

	// Manipulate objects in the scene also
	_instance.RotateBy(deltaTime * 130f % Direction.Up);
	_instance.RotateBy(deltaTime * 80f % Direction.Right);

	// It's safe to touch UI data and TinyFFR objects here; 
	// this function is guaranteed to be executed on the UI thread
	myLabel.Text = GetSomeNewValue();
}

// Dispose "loopTerminationDisposable" to stop the loop...
loopTerminationDisposable.Dispose();
```

This function schedules your tick/render loop on the pre-existing Windows Forms message loop, integrating it in to the UI subsystem, and is preferred over creating an `ApplicationLoop` manually (see warning above). The function takes optional arguments as follows:

<span class="def-icon">:material-code-block-parentheses:</span> `StartWinFormsUiLoop(tickCallback, tickRateHz, name)`

:   * `tickCallback` is the `Action<TimeSpan>` that you wish to be invoked on the UI thread. The singular argument is a `TimeSpan` indicating the "delta time" since the last 'tick'.

	* `tickRateHz` is optional, this sets the target framerate for the animation loop. Please note that the maximum framerate may be limited by the UI framework compositor and/or message loop mechanism; in particular, this loop is driven by a `System.Windows.Forms.Timer`, whose resolution is tied to the system timer tick of roughly 15.6ms, which caps the achievable tick rate at around 64Hz. You should also note that the 'consistency' (e.g. jitter) of the framerate will likely be much more variable than with a typical standalone TinyFFR application loop.

	* `name` is optional, this will set the name of the underlying `ApplicationLoop` resource used internally by TinyFFR.

	This function returns an `IDisposable` that should be disposed when you wish to terminate the loop.

## Input

You may of course wish to use Windows Forms' built-in input layer; simultaneously however TinyFFR's own input abstraction (as detailed in [Keyboard / Mouse Input](keyboard_and_mouse_input.md)) is available when using Windows Forms integration. This can be useful when you want to create a control scheme that works the same way both within Windows Forms and in a standalone window.

??? info "Input Sourcing" 
	The sourced UI event data is taken from Windows Forms' reporting; not from direct OS system requests as is typical in a standalone application. 
	
	This can create some occasional discrepancies in the way some key / mouse events are handled, and care should be taken to test any input code on both systems thoroughly.

To use TinyFFR's input abstraction, use the `StartWinFormsUiLoop()` overload that supplies an `ILatestInputRetriever`:

```csharp
var loopTerminationDisposable = factory.ApplicationLoopBuilder.StartWinFormsUiLoop(
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

1. 	This parameter tells TinyFFR which Windows Forms [Control](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control) should be used as the source of all collected input events.

	In this case we're using our `TinyFfrSceneView` (which inherits from `UserControl` -> `Control`).

2.	Because we passed the `TinyFfrSceneView` to `StartWinFormsUiLoop()` above, we can now grab input events from that scene view just like we would in a standalone application.

	In this example we're using keyboard/mouse controls to drive a [Camera Controller](camera_controllers.md).


<span class="def-icon">:material-code-block-parentheses:</span> `StartWinFormsUiLoop(inputSource, tickCallback, tickRateHz, name)`

:   * `inputSource` is the [`Control`](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control) whose input events will be observed; usually your `TinyFfrSceneView`, but you may pass any control (e.g. the containing form) if you want a wider scope.

	* `tickCallback` is the `Action<TimeSpan, ILatestInputRetriever>` that you wish to be invoked on the UI thread. The second argument supplies the input state accumulated since the previous 'tick'.

	* `tickRateHz` behaves as described above.

	* `name` behaves as described above.

	This function returns an `IDisposable` that should be disposed when you wish to terminate the loop; doing so also unsubscribes from the `inputSource`'s events.

#### Scope and semantics

* **Keyboard** events are observed only whilst `inputSource` has focus. `TinyFfrSceneView` is selectable and takes focus when clicked, so a user must click in to the scene view before keyboard input reaches TinyFFR. This is deliberate: it means typing in a `TextBox` elsewhere in your application does not also drive your scene.
* `TinyFfrSceneView` also claims the arrow keys and `Tab` for itself rather than letting them move focus between controls on the form, so they remain usable for camera movement.
* **Mouse** buttons and the scroll wheel are observed whilst the pointer is over `inputSource`. Whilst a button is held, the mouse is captured, so `MouseCursorDelta` keeps accumulating even if the pointer leaves the control (which makes drag-to-look work as expected).
* `MouseCursorPosition` is relative to `inputSource` (`(0, 0)` being its top-left corner) and expressed in that control's own coordinate space. You can pass it straight to `Renderer.CreateRayFromRenderSurface()` and similar methods: Those methods convert it to the render buffer's pixel space for you, accounting both for the display's scaling factor and for any `InternalRenderResolution` you have set. Pass `disableDpiScalingAdjustment: true` if you are supplying a coordinate that is already in buffer space. For this to line up, `inputSource` must be the scene view itself.
* Keys and buttons are released automatically when focus or mouse capture is lost, so holding a key and then switching away from your application will not leave that key "stuck" down.
* Game controllers are not currently supported under UI framework integration. `GameControllers` is always empty and `GameControllersCombined` always reports a neutral state.
