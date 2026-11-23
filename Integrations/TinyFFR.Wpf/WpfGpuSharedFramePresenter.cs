// Created on 2026-11-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.Wpf;

sealed class WpfGpuSharedFramePresenter : IBindableRendererGpuFrameSink {
	const int MaxImageCount = 8;
	readonly Action _onUsabilityChanged;
	readonly Action _onFirstFrameShown;
	readonly EventHandler _onRendering;
	readonly DependencyPropertyChangedEventHandler _onFrontBufferAvailableChanged;
	readonly nint[] _textures = new nint[MaxImageCount];
	readonly nint[] _surfaces = new nint[MaxImageCount];
	readonly byte[] _deviceLuid = new byte[ExportableRenderSurfaceCapabilities.DeviceLuidLengthBytes];
	D3D9ExDevice? _device;
	D3DImage? _image;
	ExportableRenderSurface? _surface;
	ExportableRenderSurfaceImageSet? _importedImageSet;
	int _importedImageCount;
	int? _shownIndex;
	bool _hasShownFrame;
	bool _isHookedToRendering;
	bool _hasFailed;
	long _displayedFrameCount;

	public bool IsUsable => OperatingSystem.IsWindows() && !_hasFailed;
	public bool IsPresenting => _surface != null;
	public long DisplayedFrameCount => _displayedFrameCount;
	public string? UnavailableReason { get; private set; }
	public D3DImage? ImageToDraw => _surface != null && _hasShownFrame ? _image : null;

	public ExportableRenderSurfaceImageHandleType RequiredImageHandleType => ExportableRenderSurfaceImageHandleType.D3D11TextureLegacySharedHandle;
	public bool CanAcceptFrame => true;

	public WpfGpuSharedFramePresenter(Action onUsabilityChanged, Action onFirstFrameShown) {
		ArgumentNullException.ThrowIfNull(onUsabilityChanged);
		ArgumentNullException.ThrowIfNull(onFirstFrameShown);
		_onUsabilityChanged = onUsabilityChanged;
		_onFirstFrameShown = onFirstFrameShown;
		_onRendering = OnCompositionTargetRendering;
		_onFrontBufferAvailableChanged = OnFrontBufferAvailableChanged;
	}

	public void OnSurfaceAttached(ExportableRenderSurface surface, IBindableRendererGpuFrameSource source) {
		if (_surface == surface) return;
		ReleaseSession();

		var capabilities = surface.Capabilities;
		if (capabilities.ImageHandleType != RequiredImageHandleType) {
			Fail($"surface handle type {capabilities.ImageHandleType} does not match {RequiredImageHandleType}");
			return;
		}
		if (surface.ImageCount > MaxImageCount) {
			Fail($"surface has {surface.ImageCount} images (max {MaxImageCount})");
			return;
		}
		Span<byte> surfaceLuid = stackalloc byte[ExportableRenderSurfaceCapabilities.DeviceLuidLengthBytes];
		if (!surface.TryCopyDeviceLuid(surfaceLuid)) {
			Fail("TinyFFR's GPU has no adapter LUID");
			return;
		}
		if (_device == null || !surfaceLuid.SequenceEqual(_deviceLuid)) {
			_device?.Dispose();
			_device = D3D9ExDevice.TryCreate(surfaceLuid, out var failureReason);
			if (_device == null) {
				Fail(failureReason ?? "creating the Direct3D 9Ex device failed");
				return;
			}
			surfaceLuid.CopyTo(_deviceLuid);
		}

		_surface = surface;
		_image = new D3DImage();
		_image.IsFrontBufferAvailableChanged += _onFrontBufferAvailableChanged;
		CompositionTarget.Rendering += _onRendering;
		_isHookedToRendering = true;
	}

	public void OnSurfaceDetached(ExportableRenderSurface surface) {
		if (_surface != surface) return;
		ReleaseSession();
	}

	public void OnFrameRendered(ExportableRenderSurface surface) { }

	void OnCompositionTargetRendering(object? sender, EventArgs e) {
		if (_surface is not { } surface || _image is not { } image || _device is not { } device) return;
		if (!image.IsFrontBufferAvailable) return;
		if (surface.CurrentImageSet is not { } imageSet) return;

		if (imageSet.Generation != _importedImageSet?.Generation) {
			ReleaseImportedSurfaces();
			if (!TryOpenImageSet(device, imageSet)) return;
		}
		if (_importedImageSet is not { } importedImageSet) return;
		if (!importedImageSet.TryTakeLatestPresentedImage(out var index)) return;

		image.Lock();
		try {
			image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surfaces[index], enableSoftwareFallback: true);
			image.AddDirtyRect(new Int32Rect(0, 0, importedImageSet.Dimensions.X, importedImageSet.Dimensions.Y));
		}
		finally {
			image.Unlock();
		}

		if (_shownIndex is { } previousIndex) importedImageSet.ReturnImage(previousIndex, consumerSignalledRelease: true);
		_shownIndex = index;
		_displayedFrameCount++;
		if (!_hasShownFrame) {
			_hasShownFrame = true;
			_onFirstFrameShown();
		}
	}

	bool TryOpenImageSet(D3D9ExDevice device, ExportableRenderSurfaceImageSet imageSet) {
		var imageCount = imageSet.ImageCount;
		for (var i = 0; i < imageCount; ++i) {
			if (imageSet.ExtractImage(i) is not { } handles) {
				ReleaseImportedSurfaces();
				return false;
			}
			if (!device.TryOpenSharedSurface(imageSet.Dimensions.X, imageSet.Dimensions.Y, (nint) handles.ImageHandle.AsInteger, out _textures[i], out _surfaces[i])) {
				ReleaseImportedSurfaces();
				Fail("opening a shared texture in Direct3D 9Ex failed");
				return false;
			}
			_importedImageCount = i + 1;
		}
		_importedImageSet = imageSet;
		return true;
	}

	void ReleaseImportedSurfaces() {
		if (_image is { } image && _hasShownFrame) {
			image.Lock();
			image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
			image.Unlock();
			_hasShownFrame = false;
		}
		if (_shownIndex is { } shownIndex && _importedImageSet is { } importedImageSet) {
			try {
				importedImageSet.ReturnImage(shownIndex, consumerSignalledRelease: true);
			}
			catch (ObjectDisposedException) { }
		}
		_shownIndex = null;
		for (var i = 0; i < _importedImageCount; ++i) {
			D3D9ExDevice.ReleaseSharedSurface(_textures[i], _surfaces[i]);
			_textures[i] = 0;
			_surfaces[i] = 0;
		}
		_importedImageCount = 0;
		_importedImageSet = null;
	}

	void OnFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e) {
		if (e.NewValue is true) return;
		ReleaseImportedSurfaces();
	}

	void ReleaseSession() {
		if (_isHookedToRendering) {
			CompositionTarget.Rendering -= _onRendering;
			_isHookedToRendering = false;
		}
		ReleaseImportedSurfaces();
		if (_image != null) {
			_image.IsFrontBufferAvailableChanged -= _onFrontBufferAvailableChanged;
			_image = null;
		}
		_surface = null;
	}

	void Fail(string reason) {
		if (_hasFailed) return;
		_hasFailed = true;
		UnavailableReason = reason;
		Console.WriteLine($"GPU shared frames are unavailable: {UnavailableReason}. Falling back to CPU-copied frames.");
		ReleaseSession();
		Application.Current?.Dispatcher.BeginInvoke(_onUsabilityChanged);
	}

	public void Dispose() {
		ReleaseSession();
		_device?.Dispose();
		_device = null;
	}
}
