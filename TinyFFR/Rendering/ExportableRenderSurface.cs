// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Identifies the kind of operating system / graphics API handle through which an <see cref="ExportableRenderSurface"/> shares its images with another API.
/// </summary>
/// <remarks>
/// Each value is specific to one platform. Request the type your consumer can import via <see cref="ExportableRenderSurfaceCreationConfig.RequiredImageHandleType"/>;
/// <see cref="IRendererBuilder.TryCreateExportableRenderSurface(in ExportableRenderSurfaceCreationConfig)"/> returns <see langword="null"/> if it is unavailable on the current platform or device.
/// </remarks>
public enum ExportableRenderSurfaceImageHandleType {
	/// <summary>
	/// No handle type; images can not be exported.
	/// </summary>
	None = 0,
	/// <summary>
	/// <b>Linux.</b> A Vulkan opaque POSIX file descriptor (<c>VK_EXTERNAL_MEMORY_HANDLE_TYPE_OPAQUE_FD_BIT</c>) referring to image memory in RGBA8 format.
	/// Synchronized with <see cref="ExportableRenderSurfaceSyncKind.VulkanOpaqueFileDescriptorSemaphores"/>.
	/// </summary>
	VulkanOpaqueFileDescriptor = 1,
	/// <summary>
	/// <b>Windows.</b> An NT handle to a shared Direct3D 11 BGRA8 texture created with a DXGI keyed mutex (<c>D3D11_RESOURCE_MISC_SHARED_NTHANDLE | D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX</c>).
	/// Synchronized with <see cref="ExportableRenderSurfaceSyncKind.DxgiKeyedMutex"/>.
	/// </summary>
	D3D11TextureNtHandle = 2,
	/// <summary>
	/// <b>Windows.</b> A legacy (non-NT) shared handle to a Direct3D 11 BGRA8 texture (<c>D3D11_RESOURCE_MISC_SHARED</c>), as required to open the texture in Direct3D 9Ex.
	/// Synchronized with <see cref="ExportableRenderSurfaceSyncKind.CompletedBeforeTake"/>.
	/// </summary>
	D3D11TextureLegacySharedHandle = 3,
	/// <summary>
	/// <b>macOS.</b> An <c>IOSurfaceRef</c> referring to a BGRA8 surface.
	/// Synchronized with <see cref="ExportableRenderSurfaceSyncKind.MetalSharedEventTimeline"/>.
	/// </summary>
	IOSurfaceRef = 4
}

/// <summary>
/// Identifies how a consumer of an <see cref="ExportableRenderSurface"/> must synchronize its access to an exported image with TinyFFR's rendering backend.
/// </summary>
/// <remarks>
/// The sync kind is determined by the surface's <see cref="ExportableRenderSurfaceImageHandleType"/>; see <see cref="ExportableRenderSurface.Capabilities"/>.
/// </remarks>
public enum ExportableRenderSurfaceSyncKind {
	/// <summary>
	/// No synchronization; images can not be exported.
	/// </summary>
	None = 0,
	/// <summary>
	/// A pair of binary Vulkan semaphores per image, exported as opaque POSIX file descriptors.
	/// The consumer waits on <see cref="ExportedRenderSurfaceImage.ReadySemaphoreHandle"/> before reading a taken image,
	/// and signals <see cref="ExportedRenderSurfaceImage.ReleasedSemaphoreHandle"/> when it has finished reading it.
	/// </summary>
	VulkanOpaqueFileDescriptorSemaphores = 1,
	/// <summary>
	/// A DXGI keyed mutex on the shared texture itself. The consumer acquires key <see cref="ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexAcquireKey"/>
	/// before reading a taken image and releases it with key <see cref="ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexReleaseKey"/> when finished.
	/// </summary>
	DxgiKeyedMutex = 2,
	/// <summary>
	/// No GPU synchronization object. TinyFFR only hands out an image (via <see cref="ExportableRenderSurfaceImageSet.TryTakeLatestPresentedImage(out int)"/>)
	/// once the GPU has finished rendering to it, and does not render to it again until it is returned via <see cref="ExportableRenderSurfaceImageSet.ReturnImage"/>.
	/// The consumer must therefore not return an image until it has finished reading it.
	/// </summary>
	CompletedBeforeTake = 3,
	/// <summary>
	/// A Metal <c>MTLSharedEvent</c> per image used as a timeline semaphore (exported as both <see cref="ExportedRenderSurfaceImage.ReadySemaphoreHandle"/> and
	/// <see cref="ExportedRenderSurfaceImage.ReleasedSemaphoreHandle"/>). Take images with
	/// <see cref="ExportableRenderSurfaceImageSet.TryTakeLatestPresentedImage(out int, out ulong, out ulong)"/>: the consumer waits for the event to reach
	/// the returned wait value before reading, and raises it to the returned signal value when it has finished reading.
	/// </summary>
	MetalSharedEventTimeline = 4
}

/// <summary>
/// Represents one generation of the images belonging to an <see cref="ExportableRenderSurface"/>.
/// </summary>
/// <remarks>
/// <para>
/// A surface's images are recreated (and the <see cref="Generation"/> incremented) whenever the surface is first rendered to and after every change to its
/// <see cref="ExportableRenderSurface.Dimensions"/>. Obtain the current set from <see cref="ExportableRenderSurface.CurrentImageSet"/>, export and import every image
/// in it, and re-import whenever the current set's <see cref="Generation"/> changes.
/// </para>
/// <para>
/// Once a set has been superseded, its methods do nothing: <see cref="ExtractImage"/> returns <see langword="null"/>, the <c>TryTakeLatestPresentedImage</c> overloads return
/// <see langword="false"/>, and <see cref="ReturnImage"/> is ignored. It is therefore always safe to keep using a stale set until the consumer notices the change.
/// </para>
/// </remarks>
public readonly record struct ExportableRenderSurfaceImageSet {
	readonly ExportableRenderSurface _surface;

	/// <summary>
	/// The generation number of this set. Increases every time the owning surface's images are recreated.
	/// </summary>
	public int Generation { get; }
	/// <summary>
	/// The pixel dimensions of every image in this set.
	/// </summary>
	public XYPair<int> Dimensions { get; }
	/// <summary>
	/// The number of images in this set; valid image indices are <c>0</c> to <c>ImageCount - 1</c>.
	/// </summary>
	public int ImageCount => _surface.ImageCount;

	internal ExportableRenderSurfaceImageSet(ExportableRenderSurface surface, int generation, XYPair<int> dimensions) {
		_surface = surface;
		Generation = generation;
		Dimensions = dimensions;
	}

	/// <summary>
	/// Exports the OS/API handles for the image at <paramref name="index"/> in this set, so that it can be imported by another graphics API.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every call creates new handles. Unless stated otherwise below, the caller owns them and must either pass them to an importer that takes ownership,
	/// or close them with <see cref="ExportableRenderSurface.CloseExportedHandle"/>:
	/// <list type="bullet">
	/// <item><description><see cref="ExportableRenderSurfaceImageHandleType.VulkanOpaqueFileDescriptor"/>: new file descriptors for the image and both semaphores.</description></item>
	/// <item><description><see cref="ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle"/>: a duplicated NT handle for the texture; there are no semaphores.</description></item>
	/// <item><description><see cref="ExportableRenderSurfaceImageHandleType.D3D11TextureLegacySharedHandle"/>: the texture's legacy shared handle, which is not owned by the caller (closing it is a no-op); there are no semaphores.</description></item>
	/// <item><description><see cref="ExportableRenderSurfaceImageHandleType.IOSurfaceRef"/>: a retained <c>IOSurfaceRef</c>, and the image's <c>MTLSharedEvent</c> retained once for each semaphore handle.</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Handles that do not apply to the surface's sync kind are <see langword="default"/>.
	/// </para>
	/// </remarks>
	/// <param name="index">The index of the image to export, from <c>0</c> to <see cref="ImageCount"/> <c>- 1</c>.</param>
	/// <returns>The exported handles, or <see langword="null"/> if this set has been superseded (see <see cref="Generation"/>).</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ExportedRenderSurfaceImage? ExtractImage(int index) => _surface.ExtractImage(Generation, index);
	/// <summary>
	/// Takes ownership of the most recently presented (i.e. fully rendered) image in this set, if there is one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// TinyFFR will not render to a taken image again until it is given back via <see cref="ReturnImage"/>, so every successfully-taken image must eventually be returned.
	/// Older presented images that have not been taken are discarded (they are re-used for future frames) when a newer one is taken.
	/// </para>
	/// <para>
	/// For surfaces with sync kind <see cref="ExportableRenderSurfaceSyncKind.MetalSharedEventTimeline"/>, use
	/// <see cref="TryTakeLatestPresentedImage(out int, out ulong, out ulong)"/> instead, as the timeline values it returns are required to synchronize access.
	/// </para>
	/// </remarks>
	/// <param name="index">When this method returns <see langword="true"/>, the index of the taken image.</param>
	/// <returns><see langword="true"/> if an image was taken; <see langword="false"/> if there is no newly-presented image yet or this set has been superseded.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryTakeLatestPresentedImage(out int index) => _surface.TryTakeLatestPresentedImage(Generation, out index, out _, out _);
	/// <inheritdoc cref="TryTakeLatestPresentedImage(out int)"/>
	/// <remarks>
	/// <para>
	/// For surfaces with sync kind <see cref="ExportableRenderSurfaceSyncKind.MetalSharedEventTimeline"/>, the image's timeline semaphore
	/// (<see cref="ExportedRenderSurfaceImage.ReadySemaphoreHandle"/>/<see cref="ExportedRenderSurfaceImage.ReleasedSemaphoreHandle"/>) must be waited for until it reaches
	/// <paramref name="timelineWaitValue"/> before reading the image, and raised to <paramref name="timelineSignalValue"/> once the consumer has finished reading it.
	/// TinyFFR does not render to the image again until the semaphore reaches <paramref name="timelineSignalValue"/>.
	/// </para>
	/// <para>
	/// For every other sync kind, both values are <c>0</c> and should be ignored. In all cases, every successfully-taken image must eventually be given back via <see cref="ReturnImage"/>.
	/// </para>
	/// </remarks>
	/// <param name="index">When this method returns <see langword="true"/>, the index of the taken image.</param>
	/// <param name="timelineWaitValue">When this method returns <see langword="true"/>, the timeline value to wait for before reading the image.</param>
	/// <param name="timelineSignalValue">When this method returns <see langword="true"/>, the timeline value to signal after reading the image.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryTakeLatestPresentedImage(out int index, out ulong timelineWaitValue, out ulong timelineSignalValue) => _surface.TryTakeLatestPresentedImage(Generation, out index, out timelineWaitValue, out timelineSignalValue);
	/// <summary>
	/// Gives back an image previously taken via one of the <c>TryTakeLatestPresentedImage</c> overloads, allowing TinyFFR to render to it again.
	/// </summary>
	/// <remarks>
	/// Pass <paramref name="consumerSignalledRelease"/> as <see langword="true"/> only if the consumer actually performed its side of the synchronization
	/// (e.g. signalled the released semaphore, released the keyed mutex, or signalled the timeline value). Pass <see langword="false"/> if it did not (for example
	/// because the consumer's update failed), so that TinyFFR does not wait for a signal that will never arrive.
	/// Calls for a superseded set, or for an image that is not currently taken, are ignored.
	/// </remarks>
	/// <param name="index">The index of the image being returned.</param>
	/// <param name="consumerSignalledRelease">Whether the consumer signalled the image's release through its sync mechanism.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReturnImage(int index, bool consumerSignalledRelease) => _surface.ReturnImage(Generation, index, consumerSignalledRelease);
}

/// <summary>
/// The OS/API handles exported for a single image of an <see cref="ExportableRenderSurfaceImageSet"/>; see <see cref="ExportableRenderSurfaceImageSet.ExtractImage"/>.
/// </summary>
/// <remarks>
/// The meaning and ownership of each handle depends on the owning surface's <see cref="ExportableRenderSurfaceImageHandleType"/> and <see cref="ExportableRenderSurfaceSyncKind"/>.
/// Handles that do not apply are <see langword="default"/>.
/// </remarks>
/// <param name="ImageHandle">The handle to the image's memory/texture/surface.</param>
/// <param name="ImageSizeBytes">The size of the image's underlying memory allocation, in bytes.</param>
/// <param name="ReadySemaphoreHandle">The handle to the semaphore the consumer waits on before reading the image, if the sync kind uses one.</param>
/// <param name="ReleasedSemaphoreHandle">The handle to the semaphore the consumer signals after reading the image, if the sync kind uses one.</param>
public readonly record struct ExportedRenderSurfaceImage(ResourceHandle ImageHandle, long ImageSizeBytes, ResourceHandle ReadySemaphoreHandle, ResourceHandle ReleasedSemaphoreHandle);

/// <summary>
/// Describes how an <see cref="ExportableRenderSurface"/> shares its images: the handle type and the matching synchronization mechanism.
/// </summary>
/// <param name="ImageHandleType">The kind of handle images are exported as.</param>
/// <param name="SyncKind">The synchronization mechanism consumers must use.</param>
public readonly record struct ExportableRenderSurfaceCapabilities(ExportableRenderSurfaceImageHandleType ImageHandleType, ExportableRenderSurfaceSyncKind SyncKind) {
	/// <summary>
	/// The length, in bytes, of a device UUID as written by <see cref="ExportableRenderSurface.CopyDeviceUuid"/>: <c>16</c>.
	/// </summary>
	public const int DeviceUuidLengthBytes = 16;
	/// <summary>
	/// The length, in bytes, of an adapter LUID as written by <see cref="ExportableRenderSurface.TryCopyDeviceLuid"/>: <c>8</c>.
	/// </summary>
	public const int DeviceLuidLengthBytes = 8;
	/// <summary>
	/// For <see cref="ExportableRenderSurfaceSyncKind.DxgiKeyedMutex"/>, the key the consumer acquires before reading a taken image: <c>1</c>.
	/// </summary>
	public const ulong ConsumerKeyedMutexAcquireKey = 1UL;
	/// <summary>
	/// For <see cref="ExportableRenderSurfaceSyncKind.DxgiKeyedMutex"/>, the key the consumer releases a taken image with once it has finished reading it: <c>0</c>.
	/// </summary>
	public const ulong ConsumerKeyedMutexReleaseKey = 0UL;
	/// <summary>
	/// A value representing no support for exporting images.
	/// </summary>
	public static ExportableRenderSurfaceCapabilities None { get; } = new(ExportableRenderSurfaceImageHandleType.None, ExportableRenderSurfaceSyncKind.None);

	/// <summary>
	/// <see langword="true"/> if both <see cref="ImageHandleType"/> and <see cref="SyncKind"/> are something other than <c>None</c>.
	/// </summary>
	public bool IsSupported => ImageHandleType != ExportableRenderSurfaceImageHandleType.None && SyncKind != ExportableRenderSurfaceSyncKind.None;
}

/// <summary>
/// Represents a render target whose images can be shared directly with another graphics API (e.g. a UI framework's compositor) without being copied via the CPU.
/// Created via the factory's <see cref="IRendererBuilder"/> (see <see cref="IRendererBuilder.TryCreateExportableRenderSurface(in ExportableRenderSurfaceCreationConfig)"/>).
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Renderer"/> or <see cref="RendererCompositor"/> renders to an exportable surface much like it renders to a <see cref="TinyFFR.Environment.Local.Window">Window</see>, cycling through a small set of
/// GPU images (see <see cref="ImageCount"/>). A consumer imports every image of the <see cref="CurrentImageSet"/>, then repeatedly takes the latest finished image,
/// displays it, and returns it; see <see cref="ExportableRenderSurfaceImageSet"/> for the details of that protocol.
/// </para>
/// <para>
/// Rendering never blocks on the consumer: if the consumer is holding every image, frames are rendered to an internal image that is never exported, and are dropped.
/// </para>
/// <para>
/// Most users do not need this type directly: the UI framework integrations (e.g. TinyFFR's Avalonia and WPF scene views) use it automatically where possible.
/// </para>
/// </remarks>
public readonly struct ExportableRenderSurface : IDisposableResource<ExportableRenderSurface, IExportableRenderSurfaceImplProvider>, IRenderTarget {
	readonly ResourceHandle<ExportableRenderSurface> _handle;
	readonly IExportableRenderSurfaceImplProvider _impl;

	internal ResourceHandle<ExportableRenderSurface> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ExportableRenderSurface)) : _handle;
	internal IExportableRenderSurfaceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ExportableRenderSurface>();

	IExportableRenderSurfaceImplProvider IResource<ExportableRenderSurface, IExportableRenderSurfaceImplProvider>.Implementation => Implementation;
	ResourceHandle<ExportableRenderSurface> IResource<ExportableRenderSurface>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The pixel dimensions of this surface's images.
	/// </summary>
	/// <remarks>
	/// Setting this does not resize the existing images; a new <see cref="CurrentImageSet"/> (with a new <see cref="ExportableRenderSurfaceImageSet.Generation"/>)
	/// at the new size is created when the surface is next rendered to.
	/// Both <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> must be between <see cref="ExportableRenderSurfaceCreationConfig.DimensionsMinComponent"/> and
	/// <see cref="ExportableRenderSurfaceCreationConfig.DimensionsMaxComponent"/> (inclusive).
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is out of range.</exception>
	public XYPair<int> Dimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDimensions(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDimensions(_handle, value);
	}

	/// <summary>
	/// The handle type and synchronization mechanism this surface's images are shared with.
	/// </summary>
	public ExportableRenderSurfaceCapabilities Capabilities {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCapabilities(_handle);
	}

	/// <summary>
	/// The number of exportable images in each of this surface's image sets.
	/// </summary>
	public int ImageCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetImageCount(_handle);
	}

	/// <summary>
	/// The surface's current set of images, or <see langword="null"/> if it has no live images (i.e. it has not been rendered to yet, or the last renderer targeting it was disposed).
	/// </summary>
	/// <remarks>
	/// The returned set is superseded whenever the images are recreated, e.g. after a change to <see cref="Dimensions"/>; see <see cref="ExportableRenderSurfaceImageSet"/>.
	/// </remarks>
	public ExportableRenderSurfaceImageSet? CurrentImageSet {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.TryGetCurrentImageSet(_handle, out var generation, out var dimensions) ? new ExportableRenderSurfaceImageSet(this, generation, dimensions) : null;
	}

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<int> IRenderTarget.ViewportDimensions => Dimensions;
	XYPair<int> IRenderTarget.LogicalDimensions => Dimensions;

	internal ExportableRenderSurface(ResourceHandle<ExportableRenderSurface> handle, IExportableRenderSurfaceImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <summary>
	/// Writes the UUID of the GPU that renders to this surface in to <paramref name="destination"/>.
	/// </summary>
	/// <remarks>
	/// Consumers can compare this against their own device's UUID (e.g. Vulkan's <c>VkPhysicalDeviceIDProperties.deviceUUID</c>) to make sure they are importing
	/// images from the same GPU. All zeroes are written when the platform does not provide a UUID.
	/// </remarks>
	/// <param name="destination">The span to write to. Must be at least <see cref="ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes"/> long.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> is too short.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyDeviceUuid(Span<byte> destination) => Implementation.CopyDeviceUuid(_handle, destination);
	/// <summary>
	/// Writes the LUID of the graphics adapter that renders to this surface in to <paramref name="destination"/>, if the platform provides one (currently only Windows).
	/// </summary>
	/// <remarks>
	/// Consumers can compare this against their own adapter's LUID (e.g. DXGI's <c>DXGI_ADAPTER_DESC1.AdapterLuid</c>) to make sure they are importing
	/// images from the same adapter.
	/// </remarks>
	/// <param name="destination">The span to write to. Must be at least <see cref="ExportableRenderSurfaceCapabilities.DeviceLuidLengthBytes"/> long.</param>
	/// <returns><see langword="true"/> if a LUID was written; <see langword="false"/> if none is available.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> is too short.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryCopyDeviceLuid(Span<byte> destination) => Implementation.TryCopyDeviceLuid(_handle, destination);

	/// <summary>
	/// Closes/releases a handle previously exported via <see cref="ExportableRenderSurfaceImageSet.ExtractImage"/> that was not handed to an importer that takes ownership of it.
	/// </summary>
	/// <remarks>
	/// The handle is released in the way appropriate to this surface's <see cref="ExportableRenderSurfaceImageHandleType"/> (closing the file descriptor or NT handle,
	/// or releasing the retained Core Foundation object). For <see cref="ExportableRenderSurfaceImageHandleType.D3D11TextureLegacySharedHandle"/> this is a no-op,
	/// as legacy shared handles are not owned by the caller. <see langword="default"/> handles are ignored.
	/// </remarks>
	/// <param name="exportedHandle">The handle to close.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CloseExportedHandle(ResourceHandle exportedHandle) => Implementation.CloseExportedHandle(_handle, exportedHandle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ExportedRenderSurfaceImage? ExtractImage(int generation, int index) => Implementation.ExtractImage(_handle, generation, index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryTakeLatestPresentedImage(int generation, out int index, out ulong timelineWaitValue, out ulong timelineSignalValue) => Implementation.TryTakeLatestPresentedImage(_handle, generation, out index, out timelineWaitValue, out timelineSignalValue);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ReturnImage(int generation, int index, bool consumerSignalledRelease) => Implementation.ReturnImage(_handle, generation, index, consumerSignalledRelease);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ExportableRenderSurface IResource<ExportableRenderSurface>.CreateFromHandleAndImpl(ResourceHandle<ExportableRenderSurface> handle, IResourceImplProvider impl) {
		return new ExportableRenderSurface(handle, impl as IExportableRenderSurfaceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<ExportableRenderSurface> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<ExportableRenderSurface> IResource<ExportableRenderSurface>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc/>
	public override string ToString() => $"Exportable Render Surface {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc/>
	public bool Equals(ExportableRenderSurface other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is ExportableRenderSurface other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <see cref="Equals(ExportableRenderSurface)"/>
	public static bool operator ==(ExportableRenderSurface left, ExportableRenderSurface right) => left.Equals(right);
	/// <see cref="Equals(ExportableRenderSurface)"/>
	public static bool operator !=(ExportableRenderSurface left, ExportableRenderSurface right) => !left.Equals(right);
	#endregion
}
