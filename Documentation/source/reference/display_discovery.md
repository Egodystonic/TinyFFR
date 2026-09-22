---
title: Display Discovery
description: Information on how to discover and enumerate connected displays (monitors) with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Connected displays (monitors) can be discovered via `factory.DisplayDiscoverer`. :material-arrow-right: [The DisplayDiscoverer](#the-displaydiscoverer)
    * Each discovered `Display` can be inspected to determine supported resolutions + refresh rates. :material-arrow-right: [The Display Type](#the-display-type)

</div>

## The DisplayDiscoverer

```csharp
var primaryDisplay = factory.DisplayDiscoverer.Primary;
Console.WriteLine($"Primary display: {primaryDisplay?.ToString() ?? "<no display connected>"}");
foreach (var display in factory.DisplayDiscoverer.All) {
	Console.WriteLine($"{display.GetNameAsNewStringObject()}, max res mode = {display.HighestSupportedResolutionMode}");
	foreach (var displayMode in display.SupportedDisplayModes) {
		Console.WriteLine($"\t{displayMode.Resolution} @ {displayMode.RefreshRateHz}Hz");
	}
}
```


Displays connected to the host machine can be found using the `IDisplayDiscoverer` interface; accessed via the `DisplayDiscoverer` property on the factory.

The `Primary` property returns the primary display or `null` if there are no displays connected. This property is guaranteed to not be null as long as one display is detected on the system.

The `HighestResolution` and `HighestRefreshRate` properties return the display with the highest resolution or refresh rate respectively.(1)
{ .annotate }

1. 	In the case a of tie, the returned display will be the one that can support the highest resolution/refresh rate at the tied value.

	If this is still tied, if any display is the `Primary` display, that one will be returned.

	Otherwise, all else being equal, the display that will be returned is the one that first appears in the `All` span.

Otherwise, the `All` property returns a `ReadOnlySpan<Display>`. Use this property to iterate/discover all displays connected to the system. The span may be empty (0 length) if there are no displays connected, but can not be `null`. The convenience property `AtLeastOneDisplayConnected` can be used to determine if any displays are connected.

??? failure "Not Supported in Headless Mode"
	The display discoverer will report no connected displays when the factory is created in "headless mode", e.g.:
	
	```csharp
	var factory = new LocalTinyFfrFactory(
		// ⚠️ Enabling "HeadlessMode" like this means no displays will be discovered
		factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true }
	);
	```
	
	By default, the factory is *not* created in headless mode, but headless mode *is* suggested for UI framework integrations (e.g. Avalonia, WPF, WinForms, etc).

## The Display Type

The `Display` type is considered a resource but does not need to be disposed (in fact, there is no `Dispose()` method); because it is an immutable part of the host environment.

In a similar vein, there are no settable/mutable properties on the `Display` type. The following properties are all read-only:

<span class="def-icon">:material-card-bulleted-outline:</span> `IsPrimary`

:   Indicates whether this is the primary display or not.

<span class="def-icon">:material-code-block-parentheses:</span> `GetNameAsNewStringObject()` / `GetNameLength()` / `CopyName(Span<char>)`

:   These methods return the system name of the display.

	`GetNameAsNewStringObject()` allocates a new `string`; alternatively use `GetNameLength()` and `CopyName()` to copy the name into a buffer of your own without allocating.

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
