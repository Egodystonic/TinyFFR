// Created on 2025-08-21 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

sealed class BindableRendererImplProvider : IRendererImplProvider {
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
	bool _isDisposed = false;

	public Renderer BindableRendererInstance => new(_handle, this);

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

	static BindableRendererImplProvider GetBindableImplementationOrThrow(Renderer r) {
		return r.Implementation as BindableRendererImplProvider ?? throw new InvalidOperationException($"Given {nameof(Renderer)} ({r}) is not a bindable renderer.");
	}

	public static void StartOrContinueHandlingFrames(Renderer r, XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler) {
		GetBindableImplementationOrThrow(r).RecreateTargetBuffer(size, handler);
	}

	public static void StopHandlingFrames(Renderer r) {
		GetBindableImplementationOrThrow(r)._actualRendererTarget.StopReadingFrames(cancelQueuedFrames: true);
	}

	void RecreateTargetBuffer(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? handler) {
		_actualRenderer.Dispose();
		_actualRendererTarget.Dispose();
		CreateTargetBuffer(size, handler);
	}
	
	void CreateTargetBuffer(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? handler) {
		_actualRendererTarget = _rendererBuilder.CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig {
			Name = $"{_name} output buffer",
			TextureDimensions = size
		});
		if (handler != null) _actualRendererTarget.StartReadingFrames(handler, presentFramesTopToBottom: true);

		var scene = _sceneAndCamera.GetNthResourceOfType<Scene>(0);
		var camera = _sceneAndCamera.GetNthResourceOfType<Camera>(0);
		_actualRenderer = _rendererBuilder.CreateRenderer(
			scene,
			camera,
			_actualRendererTarget,
			BindableRendererCreationConfig.ConvertFromAllocatedHeapStorage(_serializedConfig).BaseConfig
		);
		if (_autoUpdateCameraAspectRatio && size.Ratio is { } ratio) camera.SetAspectRatio(ratio);
	}

	public bool IsDisposed(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _isDisposed;
	}
	public void Dispose(ResourceHandle<Renderer> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		if (_isDisposed) return;
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
}