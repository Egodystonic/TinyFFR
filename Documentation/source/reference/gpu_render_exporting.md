---
title: GPU Render Exporting
description: Information on how to share TinyFFR's rendered frames directly with another graphics API or UI framework on the GPU, without copying them via the CPU.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * An `ExportableRenderSurface` is a render target whose images can be imported in to another graphics API (e.g. Vulkan, Direct3D, Metal, or a UI framework's compositor) without being copied. :material-arrow-right: [Exportable Render Surfaces](#exportable-render-surfaces)
    * You import the surface's images once, then repeatedly take the latest rendered image, display it, and give it back. :material-arrow-right: [Importing Images](#importing-images), [Taking & Returning Images](#taking-returning-images)
    * Reading frames back via a `RenderOutputBuffer` is much simpler, and works everywhere; use this API only when you need the extra performance. :material-arrow-right: [Capturing Render Output](capturing_render_output.md)

</div>

## Exportable Render Surfaces

Usually, getting a rendered frame out of TinyFFR and in to something else (e.g. a UI framework) means [reading it back](capturing_render_output.md#reading-frames) on the CPU, then uploading it again to wherever it is displayed. That round trip costs CPU time, memory bandwidth, and latency on every frame.

An *exportable render surface* (`ExportableRenderSurface`) avoids it: TinyFFR renders in to a small set of GPU images that are *shared* with another graphics API, so a consumer can display or process each frame directly where TinyFFR rendered it.

This is how TinyFFR's [Avalonia](avalonia_integration.md#gpu-shared-frames) and [WPF](wpf_integration.md#gpu-shared-frames) integrations display frames, and you only need this page if you're writing a consumer of your own; for example to integrate a UI framework TinyFFR doesn't support (see [Other Frameworks](other_frameworks.md)), to feed a custom compositor or game engine, or to hand frames to a hardware video encoder.

???+ warning "Native Interop Required"
	Exported images are handed over as raw operating-system/graphics-API handles (file descriptors, Direct3D shared handles, `IOSurfaceRef`s, etc.). Importing them requires using that graphics API directly, usually via P/Invoke or a binding library, and getting the synchronization described below right. 
	
	If you don't need the performance, render to a [`RenderOutputBuffer`](capturing_render_output.md) instead.

## Creating a Surface

```csharp
var surface = factory.RendererBuilder.TryCreateExportableRenderSurface((1920, 1080)); // (1)!
if (surface == null) {
	// Not supported on this platform/device: Fall back to a RenderOutputBuffer instead
}
```

1.	Asks for a 1920x1080 surface whose images can be exported with this operating system's default handle type.

<span class="def-icon">:material-code-block-parentheses:</span> `factory.RendererBuilder.TryCreateExportableRenderSurface(dimensions, name)`

:   Creates a surface of the given size in pixels (each dimension must be between `1` and `32,768`; defaulting to 960x540 if omitted). 

	Returns `null` if the current platform or GPU can not export images with the requested handle type. Invalid arguments (e.g. out-of-range dimensions) still throw an exception as usual.

<span class="def-icon">:material-code-block-parentheses:</span> `factory.RendererBuilder.TryCreateExportableRenderSurface(config)`

:   As above, but takes an `ExportableRenderSurfaceCreationConfig`:

	<span class="def-icon">:material-card-bulleted-outline:</span> `Dimensions`

	:   The initial size of the surface's images in pixels.

	<span class="def-icon">:material-card-bulleted-outline:</span> `RequiredImageHandleType`

	:   The kind of handle the images must be exportable as; i.e. the kind your consumer can import (see below). Defaults to `ExportableRenderSurfaceCreationConfig.DefaultImageHandleTypeForCurrentOperatingSystem`.

	<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

	:   An optional name for the surface.

### Handle Types

Each `ExportableRenderSurfaceImageHandleType` is specific to one platform, and determines both what kind of handle the images are exported as, and how access to them must be synchronized (the surface's `ExportableRenderSurfaceSyncKind`, see [Synchronization](#synchronization)):

| Handle Type                      | Platform | Exported As                                                        | Pixel Format | Sync Kind                              |
| :------------------------------- | :------- | :----------------------------------------------------------------- | :----------- | :------------------------------------- |
| `VulkanOpaqueFileDescriptor`     | Linux    | Vulkan opaque POSIX file descriptors (image memory and semaphores) | RGBA8        | `VulkanOpaqueFileDescriptorSemaphores` |
| `D3D11TextureNtHandle`           | Windows  | NT handle to a Direct3D 11 texture with a DXGI keyed mutex          | BGRA8        | `DxgiKeyedMutex`                       |
| `D3D11TextureLegacySharedHandle` | Windows  | Legacy (non-NT) shared handle to a Direct3D 11 texture, as required by Direct3D 9Ex | BGRA8 | `CompletedBeforeTake`         |
| `IOSurfaceRef`                   | macOS    | `IOSurfaceRef` (plus one `MTLSharedEvent` per image)               | BGRA8        | `MetalSharedEventTimeline`             |

The defaults are `D3D11TextureNtHandle` on Windows, `IOSurfaceRef` on macOS, and `VulkanOpaqueFileDescriptor` on Linux.

## Rendering to a Surface

A surface is a render target just like a window or a [render output buffer](capturing_render_output.md#render-output-buffers), so renderers and [compositors](compositing.md) can be created for it in the usual way:

```csharp
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, surface.Value);

// Per-frame:
renderer.Render();
```

Rendering to a surface is never paced by a display and never blocks on your consumer. If your consumer is holding on to every one of the surface's images when a frame is rendered, the frame is simply dropped(1).
{ .annotate }

1.	TinyFFR still renders the scene to an internal buffer in order to accomodate for effects that require continuous data (such as temporal antialiasing).

The surface has the following members:

<span class="def-icon">:material-card-bulleted-outline:</span> `Dimensions`

:   The size of the surface's images in pixels. A surface can be resized by setting this property; new images at the new size are created the next time the surface is rendered to (see [Image Sets & Generations](#image-sets-generations)).

<span class="def-icon">:material-card-bulleted-outline:</span> `Capabilities`

:   The surface's `ImageHandleType` and `SyncKind`.

<span class="def-icon">:material-card-bulleted-outline:</span> `ImageCount`

:   How many exportable images the surface cycles through.

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentImageSet`

:   The surface's current set of images (see below), or `null` if it has none yet (it hasn't been rendered to yet, or the renderer targeting it was disposed).

### Matching the GPU

On machines with more than one GPU (e.g. laptops with both an integrated and a dedicated GPU), your consumer must be using the same GPU as TinyFFR, or importing the images will fail (or misbehave). Compare TinyFFR's GPU against your own before importing anything:

<span class="def-icon">:material-code-block-parentheses:</span> `CopyDeviceUuid(destination)`

:   Writes the 16-byte UUID of the GPU TinyFFR renders with in to `destination`. Compare this with your Vulkan device's `VkPhysicalDeviceIDProperties.deviceUUID`. All zeroes are written where the platform provides no UUID (e.g. macOS).

<span class="def-icon">:material-code-block-parentheses:</span> `TryCopyDeviceLuid(destination)`

:   Writes the 8-byte LUID of the graphics adapter TinyFFR renders with in to `destination`, and returns `true`; or returns `false` if the platform provides no LUID (currently only Windows does). Compare this with your DXGI adapter's `DXGI_ADAPTER_DESC1.AdapterLuid`.

`IOSurfaceRef`s are shared system-wide on macOS, so there's nothing to match there.

## Importing Images

### Image Sets & Generations

A surface's images are created the first time it's rendered to, and recreated every time its `Dimensions` change. Each batch of images is represented by an `ExportableRenderSurfaceImageSet`, and has a `Generation` number that increases every time the images are recreated.

Your consumer should probably check the `CurrentImageSet`'s `Generation` before each frame, and (re-)import every image in the set whenever it changes. Regardless, a superseded set is harmless; its methods simply do nothing (`ExtractImage()` returns `null`, `TryTakeLatestPresentedImage()` returns `false`, and `ReturnImage()` is ignored), so it's always safe to keep using a stale set until your consumer notices the change.

<span class="def-icon">:material-card-bulleted-outline:</span> `Generation`

:   The set's generation number.

<span class="def-icon">:material-card-bulleted-outline:</span> `Dimensions`

:   The size of every image in the set in pixels.

<span class="def-icon">:material-card-bulleted-outline:</span> `ImageCount`

:   The number of images in the set. Valid image indices are `0` to `ImageCount - 1`.

<span class="def-icon">:material-code-block-parentheses:</span> `ExtractImage(index)`

:   Exports the handles for the image at `index` as an `ExportedRenderSurfaceImage`, or returns `null` if the set has been superseded:

	<span class="def-icon">:material-card-bulleted-outline:</span> `ImageHandle`

	:   The handle to the image's memory/texture/surface.

	<span class="def-icon">:material-card-bulleted-outline:</span> `ImageSizeBytes`

	:   The size of the image's underlying memory allocation in bytes (needed when importing Vulkan memory).

	<span class="def-icon">:material-card-bulleted-outline:</span> `ReadySemaphoreHandle` / `ReleasedSemaphoreHandle`

	:   The handles to the image's synchronization objects, for the sync kinds that use them (see [Synchronization](#synchronization)). Otherwise these are `default`.

	The handles are `ResourceHandle`s; use their `AsInteger` or `AsPointer` property to get the raw value to pass to your graphics API.

### Handle Ownership

Every call to `ExtractImage()` creates *new* handles, and your consumer owns them. What you must do with them once you've imported an image depends on how your importer treats them:

* **Importers that take ownership** of the handles they successfully import must be left to close them. Closing them yourself as well would close them twice. For example, Vulkan (`vkImportMemoryFdKHR`, `vkImportSemaphoreFdKHR`) and OpenGL (`glImportMemoryFdEXT`) take ownership of file descriptors they import successfully. If an import *fails*, ownership is not transferred, and you must close the handle yourself.
* **Importers that only open or reference the handle** leave you owning it, so you must close it once the import has finished. For example, Direct3D 11's `OpenSharedResource1()` opens a texture from an NT handle but does not close the handle, and macOS objects such as `IOSurfaceRef` are reference-counted, so an importer retains its own reference.

Check your importer's documentation for which applies. Handles that are neither handed over nor closed are leaked; and a leaked image handle can also keep the image's GPU memory allocated, so a consumer that re-imports on every resize can leak a whole set of images each time.

To close a handle:

<span class="def-icon">:material-code-block-parentheses:</span> `surface.CloseExportedHandle(exportedHandle)`

:   Closes/releases an exported handle in the way appropriate to the surface's handle type (closing the file descriptor or NT handle, or releasing the retained Core Foundation object). Note that `default` handles are ignored, so it's safe to pass every handle of an `ExportedRenderSurfaceImage` regardless of sync kind.

`D3D11TextureLegacySharedHandle` images are the one exception to all of the above: Legacy shared handles aren't owned by the caller, so there's nothing to close (and `CloseExportedHandle()` does nothing with them).

!!! warning "Deferred Imports"
	Some importers don't use the handle straight away. Avalonia, for example, queues the actual import to run later on its render thread, and reports when it has finished via `ICompositionImportedGpuImage.ImportCompleted`. If your importer doesn't take ownership of the handle, keep it open until that deferred import has finished, and only then close it; closing it earlier can make the import fail.

## Taking & Returning Images

Once your consumer has imported a set's images, each frame it takes the latest image TinyFFR has finished rendering, displays it, and gives back the image it was displaying previously:

<span class="def-icon">:material-code-block-parentheses:</span> `TryTakeLatestPresentedImage(out index)`

:   If a new image has been rendered since the last call, takes ownership of the newest one, writes its index to `index`, and returns `true`. Older rendered images that weren't taken are discarded (i.e. reused for future frames). Returns `false` if there is no new image yet (or the set has been superseded).

	TinyFFR won't render to a taken image again until it has been returned, so every image you take must eventually be returned.

<span class="def-icon">:material-code-block-parentheses:</span> `TryTakeLatestPresentedImage(out index, out timelineWaitValue, out timelineSignalValue)`

:   As above, but also outputs the timeline values needed for the `MetalSharedEventTimeline` sync kind (see below). For every other sync kind, both values are `0` and can be ignored.

<span class="def-icon">:material-code-block-parentheses:</span> `ReturnImage(index, consumerSignalledRelease)`

:   Gives back a taken image, allowing TinyFFR to render to it again. 

	Pass `consumerSignalledRelease: true` if your consumer performed its side of the synchronization (e.g. signalled the released semaphore). Pass `false` if it didn't (e.g. because the frame failed to display), so that TinyFFR doesn't wait for a signal that will never arrive. 

	Calls for a superseded set, or for an image that isn't currently taken, are ignored.

Holding on to the displayed image until the next one replaces it is what lets your consumer keep showing (or re-compositing) the last frame whilst TinyFFR renders the next. Holding more than one image at a time is allowed (e.g. whilst several frames are in flight in your graphics API), but every image held is one fewer that TinyFFR can render in to. If your consumer ever holds all of them, newly rendered frames are dropped until one is returned.

### Synchronization

TinyFFR and your consumer both run their GPU work run independently, so access to each image must be synchronized. How depends on the surface's `Capabilities.SyncKind`:

<span class="def-icon">:material-card-bulleted-outline:</span> `VulkanOpaqueFileDescriptorSemaphores`

:   Each image has a pair of binary Vulkan semaphores, exported as opaque file descriptors (`ReadySemaphoreHandle` and `ReleasedSemaphoreHandle`). 

	Before reading a taken image, your consumer's GPU work must wait on the *ready* semaphore. When it has finished reading the image, it must signal the *released* semaphore. You can then return the image straight away; TinyFFR waits for the released semaphore before rendering to the image again.

<span class="def-icon">:material-card-bulleted-outline:</span> `DxgiKeyedMutex`

:   The shared texture has a DXGI keyed mutex. Your consumer must acquire it with key `ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexAcquireKey` (`1`) before reading a taken image, and release it with key `ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexReleaseKey` (`0`) when finished, before returning the image.

<span class="def-icon">:material-card-bulleted-outline:</span> `CompletedBeforeTake`

:   There's no GPU synchronization object. Instead, TinyFFR only lets you take an image once the GPU has completely finished rendering it, and won't render to it again until it is returned. Your consumer therefore needs no synchronization before reading, but must not return an image until it has completely finished reading it.

<span class="def-icon">:material-card-bulleted-outline:</span> `MetalSharedEventTimeline`

:   Each image has an `MTLSharedEvent`, used as a timeline semaphore and exported as both `ReadySemaphoreHandle` and `ReleasedSemaphoreHandle` (each handle holds its own reference to the event, so close both). Take images with the overload that outputs timeline values.
	
	Before reading the image, your consumer's GPU work must wait for the event to reach `timelineWaitValue`. When it has finished, it must raise the event to `timelineSignalValue`. TinyFFR won't render to the image again until the event reaches `timelineSignalValue`, even once it has been returned.

## Example

The following example shows the shape of a complete consumer. `MyGraphicsApi` and `MyImportedImage` stand in for your own graphics API: Import, synchronization, and presentation calls depend entirely on what you're integrating with.

```csharp
sealed class MyExportedFrameConsumer : IDisposable {
	readonly ExportableRenderSurface _surface;
	readonly MyImportedImage?[] _importedImages;
	ExportableRenderSurfaceImageSet? _importedSet;
	int? _displayedImageIndex;

	public MyExportedFrameConsumer(ExportableRenderSurface surface) {
		Span<byte> deviceUuid = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes];
		surface.CopyDeviceUuid(deviceUuid);
		if (!deviceUuid.SequenceEqual(MyGraphicsApi.DeviceUuid)) { // (1)!
			throw new InvalidOperationException("TinyFFR is rendering on a different GPU.");
		}

		_surface = surface;
		_importedImages = new MyImportedImage?[surface.ImageCount];
	}

	public void Update() { // (2)!
		if (_surface.CurrentImageSet is not { } currentSet) return; // (3)!
		if (currentSet.Generation != _importedSet?.Generation) ImportImages(currentSet);
		if (_importedSet is not { } importedSet) return;

		if (!importedSet.TryTakeLatestPresentedImage(out var index)) return; // (4)!
		MyGraphicsApi.Display(_importedImages[index]!); // (5)!

		if (_displayedImageIndex is { } previousIndex) {
			importedSet.ReturnImage(previousIndex, consumerSignalledRelease: true); // (6)!
		}
		_displayedImageIndex = index;
	}

	void ImportImages(ExportableRenderSurfaceImageSet imageSet) {
		ReleaseImportedImages();

		for (var i = 0; i < imageSet.ImageCount; ++i) {
			if (imageSet.ExtractImage(i) is not { } exportedImage) return; // (7)!
			try {
				_importedImages[i] = MyGraphicsApi.ImportImage(
					exportedImage.ImageHandle.AsInteger,
					exportedImage.ImageSizeBytes,
					imageSet.Dimensions
				);
			}
			finally {
				_surface.CloseExportedHandle(exportedImage.ImageHandle); // (8)!
				_surface.CloseExportedHandle(exportedImage.ReadySemaphoreHandle);
				_surface.CloseExportedHandle(exportedImage.ReleasedSemaphoreHandle);
			}
		}
		_importedSet = imageSet;
	}

	void ReleaseImportedImages() {
		if (_displayedImageIndex is { } displayedIndex) {
			_importedSet?.ReturnImage(displayedIndex, consumerSignalledRelease: true); // (9)!
		}
		_displayedImageIndex = null;

		for (var i = 0; i < _importedImages.Length; ++i) {
			_importedImages[i]?.Dispose();
			_importedImages[i] = null;
		}
		_importedSet = null;
	}

	public void Dispose() => ReleaseImportedImages();
}
```

1.	Makes sure the consumer is on the same GPU as TinyFFR (see [Matching the GPU](#matching-the-gpu)). A real consumer would more likely fall back to CPU-copied frames here instead of throwing.

2.	Call this once per frame, after rendering (e.g. `renderer.Render(); consumer.Update();`). Like all TinyFFR API calls, it must be made on the primary thread.

3.	The surface has no images until it has been rendered to at least once.

4.	There may not be a new frame yet (e.g. if the GPU hasn't finished rendering the latest one). In that case, carry on displaying the previous image.

5.	This is where the synchronization described in [Synchronization](#synchronization) goes: e.g. waiting on the ready semaphore and then signalling the released semaphore, or acquiring and releasing the keyed mutex, around your graphics API's use of the image.

6.	The previous image is no longer being displayed, so it can be given back to TinyFFR to render in to again. With the `CompletedBeforeTake` sync kind, make sure your graphics API has completely finished reading the image before doing this.

7.	`null` means this set was superseded whilst it was being imported; the new set will be imported on the next `Update()`.

8.	Closes every handle exported for this image. This assumes `MyGraphicsApi.ImportImage()` imports synchronously and doesn't take ownership of the handle: If it takes ownership, only close the handles when the import fails; and if it imports later, close them once that import has finished (see [Handle Ownership](#handle-ownership)). Unused semaphore handles are `default`, which `CloseExportedHandle()` ignores.

9.	Gives back the image that was being displayed. If the set has been superseded, this is simply ignored.

## Resizing & Disposal

To resize the images, set `surface.Dimensions`. The next `Render()` creates a new image set at the new size, and the consumer above re-imports when it sees the new `Generation`. Until then, the old set keeps working, so your consumer can keep displaying its last frame.

When shutting down, release your consumer's imported images (and return any taken image) first, then dispose the renderer or compositor, and finally the surface:

```csharp
consumer.Dispose();
renderer.Dispose();
surface.Dispose();
```

??? info "Reference Implementations"
	TinyFFR's own integrations are complete, real-world consumers of this API, and may be useful as a reference when writing your own:

	* [`GpuSharedFramePresenter.cs`](https://github.com/Egodystonic/TinyFFR/blob/main/Integrations/TinyFFR.Avalonia/GpuSharedFramePresenter.cs) (Avalonia) imports `VulkanOpaqueFileDescriptor`, `D3D11TextureNtHandle`, and `IOSurfaceRef` images in to Avalonia's compositor, using each one's sync kind.
	* [`WpfGpuSharedFramePresenter.cs`](https://github.com/Egodystonic/TinyFFR/blob/main/Integrations/TinyFFR.Wpf/WpfGpuSharedFramePresenter.cs) (WPF) opens `D3D11TextureLegacySharedHandle` images in Direct3D 9Ex for display via a `D3DImage`.
