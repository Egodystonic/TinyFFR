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
	int _importedGeneration = -1;
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

	public bool IsUsable => _interop is { IsLost: false } && !_hasFailed;
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
		if (interop == null || !SupportsOpaqueFileDescriptorSharing(interop)) return;

		_compositor = compositor;
		_interop = interop;
		_hasFailed = false;
		_onUsabilityChanged();
	}

	static bool SupportsOpaqueFileDescriptorSharing(ICompositionGpuInterop interop) {
		return interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor)
			&& interop.SupportedSemaphoreTypes.Contains(KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor);
	}

	int MaxUpdatesInFlight => _frameSource?.MaxFramesInFlight ?? DefaultMaxUpdatesInFlight;

	public bool CanAcceptFrame => _surface == null || _hasFailed || _updatesInFlight < MaxUpdatesInFlight;

	public void OnSurfaceAttached(ExportableRenderSurface surface, IBindableRendererGpuFrameSource source) {
		if (_surface == surface) return;
		ReleaseSession();
		if (_compositor == null || _interop == null || !SurfaceIsCompatible(surface, _interop)) {
			Fail();
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

	static bool SurfaceIsCompatible(ExportableRenderSurface surface, ICompositionGpuInterop interop) {
		var capabilities = surface.Capabilities;
		if (capabilities.HandleType != ExportableRenderSurfaceHandleType.OpaqueFileDescriptor) return false;
		if (capabilities.SyncKind != ExportableRenderSurfaceSyncKind.Semaphores) return false;
		if (surface.ImageCount > MaxImageCount) return false;

		var interopUuid = interop.DeviceUuid;
		if (interopUuid == null || interopUuid.Length != ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes) return false;
		Span<byte> surfaceUuid = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceUuidLengthBytes];
		surface.CopyDeviceUuid(surfaceUuid);
		return surfaceUuid.SequenceEqual(interopUuid);
	}

	void PumpPresentedFrames() {
		if (_updatesInFlight >= MaxUpdatesInFlight || _surface is not { } surface || _drawingSurface == null || _interop == null) return;
		if (_interop.IsLost) {
			Fail();
			return;
		}
		if (surface.CurrentImageSet is not { } imageSet) return;

		if (imageSet.Generation != _importedGeneration) {
			if (!_importInProgress && _updatesInFlight == 0) _ = ReimportAsync(surface, imageSet, _sessionVersion);
			return;
		}
		if (!_importsCompleted) return;

		if (!surface.TryTakeLatestPresentedImage(out var generation, out var index)) return;
		if (generation != _importedGeneration || index >= _importedImageCount) {
			surface.ReturnImage(generation, index, consumerSignalledRelease: false);
			return;
		}

		_ = PresentImageAsync(surface, _drawingSurface, generation, index, _sessionVersion);
	}

	async Task PresentImageAsync(ExportableRenderSurface surface, CompositionDrawingSurface drawingSurface, int generation, int index, int sessionVersion) {
		_updatesInFlight++;
		var consumerSignalledRelease = false;
		try {
			await drawingSurface.UpdateWithSemaphoresAsync(_images[index]!, _readySemaphores[index]!, _releasedSemaphores[index]!);
			consumerSignalledRelease = true;
			if (_compositor is { } compositor) {
				var batch = compositor.RequestCompositionBatchCommitAsync();
				await batch.Rendered;
				var isNewBatch = !ReferenceEquals(batch, _lastCountedBatch);
				if (sessionVersion == _sessionVersion && isNewBatch) {
					_lastCountedBatch = batch;
					_displayedFrameCount++;
				}
			}
		}
		catch { }

		try {
			surface.ReturnImage(generation, index, consumerSignalledRelease);
		}
		catch (ObjectDisposedException) { }

		if (sessionVersion != _sessionVersion) return;
		_updatesInFlight--;
		if (!consumerSignalledRelease && generation == _importedGeneration) {
			Fail();
			return;
		}
		_frameSource?.RenderDeferredFrameIfPending();
		PumpPresentedFrames();
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
			Fail();
			return;
		}
		await AwaitImportCompletionAsync(imageSet.Generation, sessionVersion);
		if (sessionVersion != _sessionVersion) return;
		_importInProgress = false;
		PumpPresentedFrames();
	}

	async Task AwaitImportCompletionAsync(int generation, int sessionVersion) {
		var pending = new Task[_importedImageCount * 3];
		for (var i = 0; i < _importedImageCount; ++i) {
			pending[i * 3] = _images[i]!.ImportCompleted;
			pending[i * 3 + 1] = _readySemaphores[i]!.ImportCompleted;
			pending[i * 3 + 2] = _releasedSemaphores[i]!.ImportCompleted;
		}
		var succeeded = true;
		try {
			await Task.WhenAll(pending);
		}
		catch {
			succeeded = false;
		}

		if (sessionVersion != _sessionVersion || generation != _importedGeneration) return;
		if (!succeeded) {
			Fail();
			return;
		}
		_importsCompleted = true;
	}

	static PlatformGraphicsExternalImageProperties CreateImageProperties(ExportableRenderSurfaceImageSet imageSet, long memorySizeBytes) {
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
		var imageCount = surface.ImageCount;
		try {

			for (var i = 0; i < imageCount; ++i) {
				var memoryHandle = surface.ExportImage(imageSet.Generation, i, out var memorySizeBytes);
				try {
					_images[i] = interop.ImportImage(
						new PlatformHandle(memoryHandle, KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor),
						CreateImageProperties(imageSet, memorySizeBytes)
					);
				}
				catch {
					surface.CloseExportedHandle(memoryHandle);
					throw;
				}

				surface.ExportSemaphores(imageSet.Generation, i, out var readyHandle, out var releasedHandle);
				try {
					_readySemaphores[i] = interop.ImportSemaphore(new PlatformHandle(readyHandle, KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor));
				}
				catch {
					surface.CloseExportedHandle(readyHandle);
					surface.CloseExportedHandle(releasedHandle);
					throw;
				}
				try {
					_releasedSemaphores[i] = interop.ImportSemaphore(new PlatformHandle(releasedHandle, KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor));
				}
				catch {
					surface.CloseExportedHandle(releasedHandle);
					throw;
				}
			}
		}
		catch {
			_ = ReleaseImports();
			return false;
		}

		_importedGeneration = imageSet.Generation;
		_importedImageCount = imageCount;
		return true;
	}

	Task ReleaseImports() {
		var disposals = new List<Task>();
		for (var i = 0; i < MaxImageCount; ++i) {
			DisposeImportedObject(ref _images[i], disposals);
			DisposeImportedObject(ref _readySemaphores[i], disposals);
			DisposeImportedObject(ref _releasedSemaphores[i], disposals);
		}
		_importedGeneration = -1;
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

	void Fail() {
		if (_hasFailed) return;
		_hasFailed = true;
		ReleaseSession();
		Dispatcher.UIThread.Post(_onUsabilityChanged);
	}
}
