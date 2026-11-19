// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Avalonia;

sealed class GpuSharedFramePresenter : IBindableRendererGpuFrameSink {
	const int VkImageLayoutTransferSrcOptimal = 6;
	const int MaxImageCount = 8;
	readonly Visual _owner;
	readonly Action _onUsabilityChanged;
	readonly ICompositionImportedGpuImage?[] _images = new ICompositionImportedGpuImage?[MaxImageCount];
	readonly ICompositionImportedGpuSemaphore?[] _readySemaphores = new ICompositionImportedGpuSemaphore?[MaxImageCount];
	readonly ICompositionImportedGpuSemaphore?[] _releasedSemaphores = new ICompositionImportedGpuSemaphore?[MaxImageCount];
	Compositor? _compositor;
	ICompositionGpuInterop? _interop;
	CompositionSurfaceVisual? _visual;
	CompositionDrawingSurface? _drawingSurface;
	ExportableRenderSurface? _surface;
	ExportableRenderSurfaceImageSet? _importedImageSet;
	int _importedImageCount = 0;
	int _attachmentVersion = 0;
	int _sessionVersion = 0;
	const int DefaultMaxUpdatesInFlight = 2;
	int _updatesInFlight = 0;
	long _displayedFrameCount = 0L;
	IBindableRendererGpuFrameSource? _frameSource;
	global::Avalonia.Rendering.Composition.Transport.CompositionBatch? _lastCountedBatch;
	bool _importsCompleted = false;
	bool _importInProgress = false;
	bool _hasFailed = false;
	Size _visualSize;
	ExportableRenderSurfaceHandleType _requiredHandleType = ExportableRenderSurfaceHandleType.VulkanOpaqueFileDescriptor;

	public bool IsUsable => _interop is { IsLost: false } && !_hasFailed;
	public string? UnavailableReason { get; private set; } = "GPU interop not discovered yet";
	public bool IsPresenting => _surface != null;
	public long DisplayedFrameCount => _displayedFrameCount;

	public GpuSharedFramePresenter(Visual owner, Action onUsabilityChanged) {
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(onUsabilityChanged);
		_owner = owner;
		_onUsabilityChanged = onUsabilityChanged;
	}

	public void OnAttachedToVisualTree() {
		var attachmentVersion = ++_attachmentVersion;
		_ = DiscoverInteropAsync(attachmentVersion);
	}

	public void OnDetachedFromVisualTree() {
		++_attachmentVersion;
		ReleaseSession();
		_interop = null;
		_compositor = null;
	}

	public void SetVisualSize(Size size) {
		_visualSize = size;
		if (_visual != null) _visual.Size = new Vector(size.Width, size.Height);
	}

	async Task DiscoverInteropAsync(int attachmentVersion) {
		var elementVisual = ElementComposition.GetElementVisual(_owner);
		if (elementVisual == null) return;
		var compositor = elementVisual.Compositor;
		ICompositionGpuInterop? interop;
		try {
			interop = await compositor.TryGetCompositionGpuInterop();
		}
		catch (Exception) {
			interop = null;
		}
		if (attachmentVersion != _attachmentVersion) return;
		if (interop == null) {
			UnavailableReason = "Avalonia offers no GPU interop for this window";
			_onUsabilityChanged();
			return;
		}
		if (SelectHandleType(interop) is not { } handleType) {
			UnavailableReason = $"Avalonia's GPU interop offers no supported handle type (images: {String.Join(", ", interop.SupportedImageHandleTypes)})";
			_onUsabilityChanged();
			return;
		}

		_compositor = compositor;
		_interop = interop;
		_requiredHandleType = handleType;
		_hasFailed = false;
		UnavailableReason = null;
		_onUsabilityChanged();
	}

	static ExportableRenderSurfaceHandleType? SelectHandleType(ICompositionGpuInterop interop) {
		if (interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor)
			&& interop.SupportedSemaphoreTypes.Contains(KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor)) {
			return ExportableRenderSurfaceHandleType.VulkanOpaqueFileDescriptor;
		}
		if (OperatingSystem.IsWindows() && interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle)) {
			return ExportableRenderSurfaceHandleType.D3D11TextureNtHandle;
		}
		return null;
	}

	bool UsesKeyedMutex => _requiredHandleType == ExportableRenderSurfaceHandleType.D3D11TextureNtHandle;

	int MaxUpdatesInFlight => _frameSource?.MaxFramesInFlight ?? DefaultMaxUpdatesInFlight;

	public ExportableRenderSurfaceHandleType RequiredHandleType => _requiredHandleType;

	public bool CanAcceptFrame => _surface == null || _hasFailed || _updatesInFlight < MaxUpdatesInFlight;

	public void OnSurfaceAttached(ExportableRenderSurface surface, IBindableRendererGpuFrameSource source) {
		if (_surface == surface) return;
		ReleaseSession();
		if (_compositor == null || _interop == null) {
			Fail("GPU interop was lost before the surface was attached");
			return;
		}
		if (DescribeIncompatibility(surface, _interop, _requiredHandleType) is { } incompatibility) {
			Fail(incompatibility);
			return;
		}

		_surface = surface;
		_frameSource = source;
		_drawingSurface = _compositor.CreateDrawingSurface();
		_visual = _compositor.CreateSurfaceVisual();
		_visual.Surface = _drawingSurface;
		_visual.Size = new Vector(_visualSize.Width, _visualSize.Height);
		ElementComposition.SetElementChildVisual(_owner, _visual);
	}

	public void OnSurfaceDetached(ExportableRenderSurface surface) {
		if (_surface != surface) return;
		ReleaseSession();
	}

	public void OnFrameRendered(ExportableRenderSurface surface) {
		if (_surface != surface || _hasFailed) return;
		PumpPresentedFrames();
	}

	static string? DescribeIncompatibility(ExportableRenderSurface surface, ICompositionGpuInterop interop, ExportableRenderSurfaceHandleType requiredHandleType) {
		var capabilities = surface.Capabilities;
		if (capabilities.HandleType != requiredHandleType) return $"surface handle type {capabilities.HandleType} does not match {requiredHandleType}";
		if (surface.ImageCount > MaxImageCount) return $"surface has {surface.ImageCount} images (max {MaxImageCount})";

		if (requiredHandleType == ExportableRenderSurfaceHandleType.VulkanOpaqueFileDescriptor) {
			var interopUuid = interop.DeviceUuid;
			if (interopUuid == null || interopUuid.Length != ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes) return "Avalonia did not report a device UUID";
			Span<byte> surfaceUuid = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes];
			surface.CopyDeviceUuid(surfaceUuid);
			return surfaceUuid.SequenceEqual(interopUuid) ? null : $"GPU mismatch (TinyFFR {Convert.ToHexString(surfaceUuid)}, Avalonia {Convert.ToHexString(interopUuid)})";
		}

		var interopLuid = interop.DeviceLuid;
		if (interopLuid == null || interopLuid.Length != ExportableRenderSurfaceCapabilities.DeviceLuidLengthBytes) return "Avalonia did not report an adapter LUID";
		Span<byte> surfaceLuid = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceLuidLengthBytes];
		if (!surface.TryCopyDeviceLuid(surfaceLuid)) return "TinyFFR's GPU has no adapter LUID";
		return surfaceLuid.SequenceEqual(interopLuid) ? null : $"adapter mismatch (TinyFFR {Convert.ToHexString(surfaceLuid)}, Avalonia {Convert.ToHexString(interopLuid)})";
	}

	void PumpPresentedFrames() {
		if (_updatesInFlight >= MaxUpdatesInFlight || _surface is not { } surface || _drawingSurface == null || _interop == null) return;
		if (_interop.IsLost) {
			Fail("Avalonia's GPU interop was lost");
			return;
		}
		if (surface.CurrentImageSet is not { } imageSet) return;

		if (imageSet.Generation != _importedImageSet?.Generation) {
			if (!_importInProgress && _updatesInFlight == 0) _ = ReimportAsync(surface, imageSet, _sessionVersion);
			return;
		}
		if (!_importsCompleted || _importedImageSet is not { } importedImageSet) return;

		if (!importedImageSet.TryTakeLatestPresentedImage(out var index)) return;
		_ = PresentImageAsync(importedImageSet, _drawingSurface, index, _sessionVersion);
	}

	async Task PresentImageAsync(ExportableRenderSurfaceImageSet imageSet, CompositionDrawingSurface drawingSurface, int index, int sessionVersion) {
		_updatesInFlight++;
		var consumerSignalledRelease = false;
		try {
			if (UsesKeyedMutex) {
				await drawingSurface.UpdateWithKeyedMutexAsync(_images[index]!, (uint) ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexAcquireKey, (uint) ExportableRenderSurfaceCapabilities.ConsumerKeyedMutexReleaseKey);
			}
			else {
				await drawingSurface.UpdateWithSemaphoresAsync(_images[index]!, _readySemaphores[index]!, _releasedSemaphores[index]!);
			}
			consumerSignalledRelease = true;
		}
		catch { }

		try {
			imageSet.ReturnImage(index, consumerSignalledRelease);
		}
		catch (ObjectDisposedException) { }

		if (sessionVersion != _sessionVersion) return;
		_updatesInFlight--;
		if (!consumerSignalledRelease && imageSet.Generation == _importedImageSet?.Generation) {
			Fail("a frame update failed");
			return;
		}
		_frameSource?.RenderDeferredFrameIfPending();
		PumpPresentedFrames();
		if (_compositor is { } compositor) await CountDisplayedFrameAsync(compositor, sessionVersion);
	}

	async Task CountDisplayedFrameAsync(Compositor compositor, int sessionVersion) {
		try {
			var batch = compositor.RequestCompositionBatchCommitAsync();
			await batch.Rendered;
			if (sessionVersion != _sessionVersion || ReferenceEquals(batch, _lastCountedBatch)) return;
			_lastCountedBatch = batch;
			_displayedFrameCount++;
		}
		catch { }
	}

	async Task ReimportAsync(ExportableRenderSurface surface, ExportableRenderSurfaceImageSet imageSet, int sessionVersion) {
		_importInProgress = true;
		try {
			await ReleaseImports();
		}
		catch { }
		if (sessionVersion != _sessionVersion) return;
		if (surface.CurrentImageSet is not { } latestImageSet) {
			_importInProgress = false;
			return;
		}
		imageSet = latestImageSet;

		if (!TryImportImageSet(surface, imageSet)) {
			_importInProgress = false;
			if (surface.CurrentImageSet?.Generation != imageSet.Generation) {
				PumpPresentedFrames();
				return;
			}
			Fail("importing the surface's images failed");
			return;
		}
		await AwaitImportCompletionAsync(imageSet.Generation, sessionVersion);
		if (sessionVersion != _sessionVersion) return;
		_importInProgress = false;
		PumpPresentedFrames();
	}

	async Task AwaitImportCompletionAsync(int generation, int sessionVersion) {
		var pending = new List<Task>(_importedImageCount * 3);
		for (var i = 0; i < _importedImageCount; ++i) {
			pending.Add(_images[i]!.ImportCompleted);
			if (_readySemaphores[i] is { } readySemaphore) pending.Add(readySemaphore.ImportCompleted);
			if (_releasedSemaphores[i] is { } releasedSemaphore) pending.Add(releasedSemaphore.ImportCompleted);
		}
		var succeeded = true;
		try {
			await Task.WhenAll(pending);
		}
		catch {
			succeeded = false;
		}

		if (sessionVersion != _sessionVersion || generation != _importedImageSet?.Generation) return;
		if (!succeeded) {
			Fail("the GPU import did not complete");
			return;
		}
		_importsCompleted = true;
	}

	PlatformGraphicsExternalImageProperties CreateImageProperties(ExportableRenderSurfaceImageSet imageSet, long memorySizeBytes) {
		if (UsesKeyedMutex) {
			return new PlatformGraphicsExternalImageProperties {
				Width = imageSet.Dimensions.X,
				Height = imageSet.Dimensions.Y,
				Format = PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm,
				MemorySize = (ulong) memorySizeBytes,
				MemoryOffset = 0UL,
				TopLeftOrigin = true
			};
		}
		return new PlatformGraphicsExternalImageProperties {
			Width = imageSet.Dimensions.X,
			Height = imageSet.Dimensions.Y,
			Format = PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
			MemorySize = (ulong) memorySizeBytes,
			MemoryOffset = 0UL,
			TopLeftOrigin = true,
			VulkanProperties = new PlatformGraphicsExternalImageVulkanProperties { Layout = VkImageLayoutTransferSrcOptimal }
		};
	}

	bool TryImportImageSet(ExportableRenderSurface surface, ExportableRenderSurfaceImageSet imageSet) {
		var interop = _interop!;
		var imageCount = imageSet.ImageCount;
		try {
			for (var i = 0; i < imageCount; ++i) {
				if (imageSet.ExtractImage(i) is not { } handles) {
					_ = ReleaseImports();
					return false;
				}
				if (UsesKeyedMutex) {
					try {
						_images[i] = interop.ImportImage(
							CreatePlatformHandle(handles.ImageHandle, KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle),
							CreateImageProperties(imageSet, handles.ImageSizeBytes)
						);
					}
					catch {
						surface.CloseExportedHandle(handles.ImageHandle);
						throw;
					}
					continue;
				}
				try {
					_images[i] = interop.ImportImage(
						CreatePlatformHandle(handles.ImageHandle, KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor),
						CreateImageProperties(imageSet, handles.ImageSizeBytes)
					);
				}
				catch {
					surface.CloseExportedHandle(handles.ImageHandle);
					surface.CloseExportedHandle(handles.ReadySemaphoreHandle);
					surface.CloseExportedHandle(handles.ReleasedSemaphoreHandle);
					throw;
				}
				try {
					_readySemaphores[i] = interop.ImportSemaphore(CreatePlatformHandle(handles.ReadySemaphoreHandle, KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor));
				}
				catch {
					surface.CloseExportedHandle(handles.ReadySemaphoreHandle);
					surface.CloseExportedHandle(handles.ReleasedSemaphoreHandle);
					throw;
				}
				try {
					_releasedSemaphores[i] = interop.ImportSemaphore(CreatePlatformHandle(handles.ReleasedSemaphoreHandle, KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor));
				}
				catch {
					surface.CloseExportedHandle(handles.ReleasedSemaphoreHandle);
					throw;
				}
			}
		}
		catch {
			_ = ReleaseImports();
			return false;
		}

		_importedImageSet = imageSet;
		_importedImageCount = imageCount;
		return true;
	}

	static PlatformHandle CreatePlatformHandle(ResourceHandle exportedHandle, string handleType) => new((nint) exportedHandle.AsInteger, handleType);

	Task ReleaseImports() {
		var disposals = new List<Task>();
		for (var i = 0; i < MaxImageCount; ++i) {
			DisposeImportedObject(ref _images[i], disposals);
			DisposeImportedObject(ref _readySemaphores[i], disposals);
			DisposeImportedObject(ref _releasedSemaphores[i], disposals);
		}
		_importedImageSet = null;
		_importedImageCount = 0;
		_importsCompleted = false;
		return Task.WhenAll(disposals);
	}

	static void DisposeImportedObject<T>(ref T? importedObject, List<Task> disposals) where T : class, ICompositionGpuImportedObject {
		if (importedObject == null) return;
		disposals.Add(importedObject.DisposeAsync().AsTask());
		importedObject = null;
	}

	void ReleaseSession() {
		++_sessionVersion;
		_updatesInFlight = 0;
		_importInProgress = false;
		_ = ReleaseImports();
		if (_visual != null) {
			ElementComposition.SetElementChildVisual(_owner, null);
			_visual = null;
		}
		_drawingSurface?.Dispose();
		_drawingSurface = null;
		_surface = null;
		_frameSource = null;
	}

	void Fail(string reason) {
		if (_hasFailed) return;
		_hasFailed = true;
		UnavailableReason = reason;
		ReleaseSession();
		Dispatcher.UIThread.Post(_onUsabilityChanged);
	}
}
