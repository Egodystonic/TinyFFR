// Created on 2025-08-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

sealed class BindableRendererImplProvider : IRendererImplProvider {
	enum ViewportOverrideKind { None, Fraction, Pixel }

	const string DefaultRendererName = "Unnamed Bindable Renderer";
	static nuint _previousHandleId = 0;
	readonly IRendererBuilder _rendererBuilder;
	readonly ResourceHandle<Renderer> _handle;
	readonly string _name;
	readonly ResourceGroup _sceneAndCamera;
	readonly bool _autoUpdateCameraAspectRatio;
	byte[] _serializedConfig;
	Renderer _actualRenderer;
	RenderOutputBuffer _actualRendererTarget;
	XYPair<int> _bufferSizePixels;
	XYPair<int> _cursorCoordinateSpaceSize = XYPair<int>.Zero;
	bool? _frustumCullingEnabled;
	ViewportOverrideKind _viewportOverrideKind = ViewportOverrideKind.None;
	Orientation2D _viewportAnchor;
	XYPair<float> _viewportFractionalOffset;
	XYPair<float> _viewportFractionalDimensions;
	XYPair<int> _viewportPixelOffset;
	XYPair<int> _viewportPixelDimensions;
	Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? _currentFrameHandler;
	BindableRendererCompositorImplProvider? _attachedCompositor;
	bool _isDisposed = false;

	public Renderer BindableRendererInstance => new(_handle, this);
	internal Renderer ActualRenderer => _actualRenderer;

	XYPair<float> CursorCoordinateScaling {
		get {
			if (_cursorCoordinateSpaceSize.X <= 0 || _cursorCoordinateSpaceSize.Y <= 0) return new XYPair<float>(1f, 1f);
			return _bufferSizePixels.Cast<float>() / _cursorCoordinateSpaceSize.Cast<float>();
		}
	}

	public BindableRendererImplProvider(IRendererBuilder rendererBuilder, IResourceAllocator allocator, Scene scene, Camera camera, in BindableRendererCreationConfig config) {
		ArgumentNullException.ThrowIfNull(rendererBuilder);
		ArgumentNullException.ThrowIfNull(allocator);
		config.ThrowIfInvalid();

		_rendererBuilder = rendererBuilder;

		_handle = ++_previousHandleId;

		_name = config.Name.IsEmpty ? $"{DefaultRendererName} {_handle.AsInteger:X}" : config.Name.ToString();

		// Adding these to a group adds a dependency meaning users can't dispose the camera or group before this renderer is disposed
		_sceneAndCamera = allocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: false, name: config.Name, initialCapacity: 2);
		_sceneAndCamera.Add(scene);
		_sceneAndCamera.Add(camera);
		_sceneAndCamera.Seal();

		_autoUpdateCameraAspectRatio = config.AutoUpdateCameraAspectRatio;
		_serializedConfig = new byte[BindableRendererCreationConfig.GetHeapStorageFormattedLength(config)];
		BindableRendererCreationConfig.AllocateAndConvertToHeapStorage(_serializedConfig, config);

		CreateTargetBuffer(config.DefaultBufferSize, null);
	}

	public static bool IsBindableRenderer(Renderer r) => r.Implementation is BindableRendererImplProvider;

	internal static BindableRendererImplProvider GetBindableImplementationOrThrow(Renderer r) {
		return r.Implementation as BindableRendererImplProvider ?? throw new InvalidOperationException($"Given {nameof(Renderer)} ({r}) is not a bindable renderer.");
	}

	public static void StartOrContinueHandlingFrames(Renderer r, XYPair<int> bufferSizePixels, XYPair<int> cursorCoordinateSpaceSize, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler) {
		var impl = GetBindableImplementationOrThrow(r);
		impl.ThrowIfAttachedToCompositor();
		impl._cursorCoordinateSpaceSize = cursorCoordinateSpaceSize;

		if (bufferSizePixels == impl._bufferSizePixels) {
			impl._actualRendererTarget.StartReadingFrames(handler, presentFramesTopToBottom: true);
			impl._currentFrameHandler = handler;
			return;
		}

		impl.RecreateTargetBuffer(bufferSizePixels, handler);
	}

	public static void StopHandlingFrames(Renderer r) {
		var impl = GetBindableImplementationOrThrow(r);
		impl.ThrowIfAttachedToCompositor();
		impl._actualRendererTarget.StopReadingFrames(cancelQueuedFrames: true);
		impl._currentFrameHandler = null;
	}

	void RecreateTargetBuffer(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? handler) {
		_actualRenderer.Dispose();
		_actualRendererTarget.Dispose();
		CreateTargetBuffer(size, handler);
	}

	void CreateTargetBuffer(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? handler) {
		_bufferSizePixels = size;
		_actualRendererTarget = _rendererBuilder.CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig {
			Name = $"{_name} output buffer",
			TextureDimensions = size
		});
		if (handler != null) _actualRendererTarget.StartReadingFrames(handler, presentFramesTopToBottom: true);
		_currentFrameHandler = handler;
		CreateActualRenderer(_actualRendererTarget);
	}

	void CreateActualRenderer(RenderOutputBuffer target) {
		var scene = _sceneAndCamera.GetNthResourceOfType<Scene>(0);
		var camera = _sceneAndCamera.GetNthResourceOfType<Camera>(0);
		_actualRenderer = _rendererBuilder.CreateRenderer(
			scene,
			camera,
			target,
			BindableRendererCreationConfig.ConvertFromAllocatedHeapStorage(_serializedConfig).BaseConfig with { Name = _name }
		);
		if (_frustumCullingEnabled is { } frustumCullingEnabled) _actualRenderer.SetFrustumCullingEnabled(frustumCullingEnabled);
		switch (_viewportOverrideKind) {
			case ViewportOverrideKind.Fraction:
				_actualRenderer.SetRenderSubAreaFraction(_viewportAnchor, _viewportFractionalOffset, _viewportFractionalDimensions);
				break;
			case ViewportOverrideKind.Pixel:
				_actualRenderer.SetRenderSubAreaPixels(_viewportAnchor, _viewportPixelOffset, _viewportPixelDimensions);
				break;
		}
		if (_autoUpdateCameraAspectRatio && target.TextureDimensions.Ratio is { } ratio) camera.SetAspectRatio(ratio);
	}

	internal Renderer AttachToCompositor(BindableRendererCompositorImplProvider compositor, RenderOutputBuffer sharedBuffer, XYPair<int> sharedBufferSizePixels, XYPair<int> cursorCoordinateSpaceSize) {
		if (_isDisposed) throw new ObjectDisposedException(nameof(Renderer));
		if (_attachedCompositor != null) {
			throw new InvalidOperationException($"{BindableRendererInstance} has already been added to a bindable {nameof(RendererCompositor)}.");
		}
		if (_currentFrameHandler != null) {
			throw new InvalidOperationException($"{BindableRendererInstance} is currently supplying frames directly (e.g. to a scene view control). Unbind it before adding it to a {nameof(RendererCompositor)}.");
		}

		_actualRenderer.Dispose();
		_actualRendererTarget.Dispose();
		_attachedCompositor = compositor;
		_bufferSizePixels = sharedBufferSizePixels;
		_cursorCoordinateSpaceSize = cursorCoordinateSpaceSize;
		CreateActualRenderer(sharedBuffer);
		return _actualRenderer;
	}

	internal void DisposeActualRendererForCompositorRecreation() => _actualRenderer.Dispose();

	internal Renderer RecreateActualRendererOnSharedBuffer(RenderOutputBuffer sharedBuffer, XYPair<int> sharedBufferSizePixels, XYPair<int> cursorCoordinateSpaceSize) {
		_bufferSizePixels = sharedBufferSizePixels;
		_cursorCoordinateSpaceSize = cursorCoordinateSpaceSize;
		CreateActualRenderer(sharedBuffer);
		return _actualRenderer;
	}

	internal void SetCursorCoordinateSpaceSizeFromCompositor(XYPair<int> sharedBufferSizePixels, XYPair<int> cursorCoordinateSpaceSize) {
		_bufferSizePixels = sharedBufferSizePixels;
		_cursorCoordinateSpaceSize = cursorCoordinateSpaceSize;
	}

	internal void DetachFromCompositor() {
		_actualRenderer.Dispose();
		_attachedCompositor = null;
		_cursorCoordinateSpaceSize = XYPair<int>.Zero;
		CreateTargetBuffer(BindableRendererCreationConfig.ConvertFromAllocatedHeapStorage(_serializedConfig).DefaultBufferSize, null);
	}

	internal void DisposeDueToOwningCompositorDisposal() {
		if (_isDisposed) return;
		try {
			_actualRenderer.Dispose();
			_sceneAndCamera.Dispose();
			BindableRendererCreationConfig.DisposeAllocatedHeapStorage(_serializedConfig);
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfAttachedToCompositor() {
		if (_attachedCompositor != null) {
			throw new InvalidOperationException($"{BindableRendererInstance} has been added to a bindable {nameof(RendererCompositor)}; frame handling is controlled by that compositor.");
		}
	}

	public bool IsDisposed(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _isDisposed;
	}
	public void Dispose(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		if (_isDisposed) return;
		if (_attachedCompositor != null) {
			throw new InvalidOperationException(
				$"{BindableRendererInstance} has been added to a bindable {nameof(RendererCompositor)} and can not be disposed directly. " +
				$"Dispose the compositor instead (with 'disposeContainedRenderers' set to true to also dispose this renderer)."
			);
		}
		try {
			_actualRenderer.Dispose();
			_actualRendererTarget.Dispose();
			_sceneAndCamera.Dispose();
			BindableRendererCreationConfig.DisposeAllocatedHeapStorage(_serializedConfig);
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfHandleDoesNotBelongToThisInstance(ResourceHandle<Renderer> handle) {
		if (handle != _handle) {
			throw new InvalidOperationException($"This {nameof(Renderer)} was not constructed or deserialized correctly (implementation object does not match handle).");
		}
	}


	public string GetNameAsNewStringObject(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _name;
	}
	public int GetNameLength(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _name.Length;
	}
	public void CopyName(ResourceHandle<Renderer> handle, Span<char> destinationBuffer) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_name.CopyTo(destinationBuffer);
	}
	public void Render(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_actualRenderer.Render();
	}
	public void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);

		var config = BindableRendererCreationConfig.ConvertFromAllocatedHeapStorage(_serializedConfig);
		config = config with { Quality = newConfig };
		BindableRendererCreationConfig.DisposeAllocatedHeapStorage(_serializedConfig);
		var newConfigSize = BindableRendererCreationConfig.GetHeapStorageFormattedLength(config);
		if (newConfigSize != _serializedConfig.Length) Array.Resize(ref _serializedConfig, newConfigSize);
		BindableRendererCreationConfig.AllocateAndConvertToHeapStorage(_serializedConfig, config);

		_actualRenderer.SetQuality(newConfig);
	}
	public void SetFrustumCullingEnabled(ResourceHandle<Renderer> handle, bool enabled) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_frustumCullingEnabled = enabled;
		_actualRenderer.SetFrustumCullingEnabled(enabled);
	}

	public void SetTargetViewportDimensionsByFraction(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalDimensions) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_viewportOverrideKind = ViewportOverrideKind.Fraction;
		_viewportAnchor = anchor;
		_viewportFractionalOffset = fractionalOffset;
		_viewportFractionalDimensions = fractionalDimensions;
		_actualRenderer.SetRenderSubAreaFraction(anchor, fractionalOffset, fractionalDimensions);
	}
	public void SetTargetViewportDimensionsByPixel(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<int> fractionalLocation, XYPair<int> pixelDimensions) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_viewportOverrideKind = ViewportOverrideKind.Pixel;
		_viewportAnchor = anchor;
		_viewportPixelOffset = fractionalLocation;
		_viewportPixelDimensions = pixelDimensions;
		_actualRenderer.SetRenderSubAreaPixels(anchor, fractionalLocation, pixelDimensions);
	}
	public void WaitForGpu(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_actualRenderer.WaitForGpu();
	}

	public void CaptureScreenshot(ResourceHandle<Renderer> handle, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig, XYPair<int>? captureResolution) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_actualRenderer.CaptureScreenshot(bitmapFilePath, saveConfig, captureResolution);
	}
	public void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_actualRenderer.CaptureScreenshot(handler, captureResolution, lowestAddressesRepresentFrameTop);
	}
	public unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_actualRenderer.CaptureScreenshot(handler, captureResolution, lowestAddressesRepresentFrameTop);
	}

	public Ray CastRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool yZeroOriginAtBottom, bool disableDpiScalingAdjustment) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		if (!disableDpiScalingAdjustment) pixelCoord = pixelCoord.ScaledByReal(CursorCoordinateScaling);
		// The inner renderer targets a buffer, whose viewport dimensions are its texture dimensions by definition, so it can not
		// make this adjustment itself; we pass 'true' downstream for disableDpiScalingAdjustment regardless to indicate the coordinate has already been converted.
		return _actualRenderer.CastRayFromRenderSurface(pixelCoord, yZeroOriginAtBottom, true);
	}
	public Ray CastRayFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool yZeroOriginAtBottom, bool disableDpiScalingAdjustment) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		if (!disableDpiScalingAdjustment) pixelCoord = pixelCoord.ScaledByReal(CursorCoordinateScaling);
		// The inner renderer targets a buffer, whose viewport dimensions are its texture dimensions by definition, so it can not
		// make this adjustment itself; we pass 'true' downstream for disableDpiScalingAdjustment regardless to indicate the coordinate has already been converted.
		return _actualRenderer.CastRayFromRenderSubAreaSurface(pixelCoord, yZeroOriginAtBottom, true);
	}

	public Scene GetScene(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _actualRenderer.TargetScene;
	}
	public Camera GetCamera(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _actualRenderer.TargetCamera;
	}
	public Window? GetWindow(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _actualRenderer.TargetWindow;
	}
	public RenderOutputBuffer? GetBuffer(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _actualRenderer.TargetBuffer;
	}
}
