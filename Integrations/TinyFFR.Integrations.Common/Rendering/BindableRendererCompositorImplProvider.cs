// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Rendering;

sealed class BindableRendererCompositorImplProvider : IRendererCompositorImplProvider {
	readonly record struct AddedRendererData(Renderer BindableRenderer, RenderCompositionType CompositionType, bool IsEnabled) {
		public int? FrameRateCap { get; init; } = null;
		public int FrameRateRatio { get; init; } = 1;
	}

	const string DefaultCompositorName = "Unnamed Bindable Renderer Compositor";
	static nuint _previousHandleId = 0;
	readonly IRendererBuilder _rendererBuilder;
	readonly ResourceHandle<RendererCompositor> _handle;
	readonly string _name;
	readonly ArrayPoolBackedVector<AddedRendererData> _addedRenderers = new();
	RenderOutputBuffer _sharedBuffer;
	RendererCompositor _actualCompositor;
	XYPair<int> _sharedBufferSizePixels;
	XYPair<int> _cursorCoordinateSpaceSize = XYPair<int>.Zero;
	Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? _currentFrameHandler;
	bool _isDisposed = false;

	public RendererCompositor BindableCompositorInstance => new(_handle, this);

	public BindableRendererCompositorImplProvider(IRendererBuilder rendererBuilder, in BindableRendererCompositorCreationConfig config) {
		ArgumentNullException.ThrowIfNull(rendererBuilder);
		config.ThrowIfInvalid();

		_rendererBuilder = rendererBuilder;

		_handle = ++_previousHandleId;

		_name = config.Name.IsEmpty ? $"{DefaultCompositorName} {_handle.AsInteger:X}" : config.Name.ToString();

		CreateSharedBufferAndCompositor(config.DefaultBufferSize, null);
	}

	public static bool IsBindableCompositor(RendererCompositor c) => c.Implementation is BindableRendererCompositorImplProvider;

	static BindableRendererCompositorImplProvider GetBindableImplementationOrThrow(RendererCompositor c) {
		return c.Implementation as BindableRendererCompositorImplProvider ?? throw new InvalidOperationException($"Given {nameof(RendererCompositor)} ({c}) is not a bindable compositor.");
	}

	public static void StartOrContinueHandlingFrames(RendererCompositor c, XYPair<int> bufferSizePixels, XYPair<int> cursorCoordinateSpaceSize, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler) {
		var impl = GetBindableImplementationOrThrow(c);
		impl._cursorCoordinateSpaceSize = cursorCoordinateSpaceSize;

		// Recreating the shared buffer tears down and rebuilds every added renderer, so don't do it just because the control told us the same size again
		if (bufferSizePixels == impl._sharedBufferSizePixels) {
			impl._sharedBuffer.StartReadingFrames(handler, presentFramesTopToBottom: true);
			impl._currentFrameHandler = handler;
			impl.PropagateCursorCoordinateSpaceSizeToAddedRenderers();
			return;
		}

		impl.RecreateSharedBufferAndCompositor(bufferSizePixels, handler);
	}

	void PropagateCursorCoordinateSpaceSizeToAddedRenderers() {
		for (var i = 0; i < _addedRenderers.Count; ++i) {
			BindableRendererImplProvider.GetBindableImplementationOrThrow(_addedRenderers[i].BindableRenderer)
				.SetCursorCoordinateSpaceSizeFromCompositor(_sharedBufferSizePixels, _cursorCoordinateSpaceSize);
		}
	}

	public static void StopHandlingFrames(RendererCompositor c) {
		var impl = GetBindableImplementationOrThrow(c);
		impl._sharedBuffer.StopReadingFrames(cancelQueuedFrames: true);
		impl._currentFrameHandler = null;
	}

	void RecreateSharedBufferAndCompositor(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? handler) {
		_actualCompositor.Dispose();
		for (var i = 0; i < _addedRenderers.Count; ++i) {
			BindableRendererImplProvider.GetBindableImplementationOrThrow(_addedRenderers[i].BindableRenderer).DisposeActualRendererForCompositorRecreation();
		}
		_sharedBuffer.Dispose();
		CreateSharedBufferAndCompositor(size, handler);
	}

	void CreateSharedBufferAndCompositor(XYPair<int> size, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? handler) {
		_sharedBufferSizePixels = size;
		_sharedBuffer = _rendererBuilder.CreateRenderOutputBuffer(new RenderOutputBufferCreationConfig {
			Name = $"{_name} output buffer",
			TextureDimensions = size
		});
		if (handler != null) _sharedBuffer.StartReadingFrames(handler, presentFramesTopToBottom: true);
		_currentFrameHandler = handler;
		_actualCompositor = _rendererBuilder.CreateCompositor(_sharedBuffer, _name);
		for (var i = 0; i < _addedRenderers.Count; ++i) {
			var entry = _addedRenderers[i];
			var actualRenderer = BindableRendererImplProvider.GetBindableImplementationOrThrow(entry.BindableRenderer).RecreateActualRendererOnSharedBuffer(_sharedBuffer, _sharedBufferSizePixels, _cursorCoordinateSpaceSize);
			_actualCompositor.Add(actualRenderer, entry.CompositionType);
			if (!entry.IsEnabled) _actualCompositor.SetEnabledState(actualRenderer, false);
			if (entry.FrameRateCap != null) _actualCompositor.SetRendererFrameRateCap(actualRenderer, entry.FrameRateCap);
			if (entry.FrameRateRatio != 1) _actualCompositor.SetRendererFrameRateRatio(actualRenderer, entry.FrameRateRatio);
		}
	}

	public void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();

		if (renderer.Implementation is not BindableRendererImplProvider bindableImpl) {
			throw new ArgumentException(
				$"{renderer} is not a bindable renderer. Only renderers created via '{nameof(IntegrationRenderingExtensions.CreateBindableRenderer)}' can be added to a bindable {nameof(RendererCompositor)}.",
				nameof(renderer)
			);
		}
		for (var i = 0; i < _addedRenderers.Count; ++i) {
			if (_addedRenderers[i].BindableRenderer == renderer) {
				throw new ArgumentException($"{renderer} has already been added to {BindableCompositorInstance}.", nameof(renderer));
			}
		}

		var actualRenderer = bindableImpl.AttachToCompositor(this, _sharedBuffer, _sharedBufferSizePixels, _cursorCoordinateSpaceSize);
		_actualCompositor.Add(actualRenderer, compositionType);
		_addedRenderers.Add(new(renderer, compositionType, true));
	}

	public void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();

		for (var i = 0; i < _addedRenderers.Count; ++i) {
			if (_addedRenderers[i].BindableRenderer != renderer) continue;
			_addedRenderers[i] = _addedRenderers[i] with { IsEnabled = newEnabledState };
			_actualCompositor.SetEnabledState(BindableRendererImplProvider.GetBindableImplementationOrThrow(renderer).ActualRenderer, newEnabledState);
			return;
		}

		throw new ArgumentException($"{renderer} has not been added to {BindableCompositorInstance}.", nameof(renderer));
	}

	int GetAddedRendererIndexOrThrow(Renderer renderer) {
		for (var i = 0; i < _addedRenderers.Count; ++i) {
			if (_addedRenderers[i].BindableRenderer == renderer) return i;
		}
		throw new ArgumentException($"{renderer} has not been added to {BindableCompositorInstance}.", nameof(renderer));
	}

	public void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();

		var index = GetAddedRendererIndexOrThrow(renderer);
		_actualCompositor.SetRendererFrameRateCap(BindableRendererImplProvider.GetBindableImplementationOrThrow(renderer).ActualRenderer, maxFramesPerSecond);
		_addedRenderers[index] = _addedRenderers[index] with { FrameRateCap = maxFramesPerSecond };
	}

	public int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();
		return _addedRenderers[GetAddedRendererIndexOrThrow(renderer)].FrameRateCap;
	}

	public void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int ratioDenominator) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();

		var index = GetAddedRendererIndexOrThrow(renderer);
		_actualCompositor.SetRendererFrameRateRatio(BindableRendererImplProvider.GetBindableImplementationOrThrow(renderer).ActualRenderer, ratioDenominator);
		_addedRenderers[index] = _addedRenderers[index] with { FrameRateRatio = ratioDenominator };
	}

	public int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();
		return _addedRenderers[GetAddedRendererIndexOrThrow(renderer)].FrameRateRatio;
	}

	public unsafe IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();

		static int GetCount(RendererCompositor c) => ((BindableRendererCompositorImplProvider) c.Implementation)._addedRenderers.Count;
		static int GetVersion(RendererCompositor c) => ((BindableRendererCompositorImplProvider) c.Implementation)._addedRenderers.Version;
		static Renderer GetItem(RendererCompositor c, int index) => ((BindableRendererCompositorImplProvider) c.Implementation)._addedRenderers[index].BindableRenderer;

		return new IndirectEnumerable<RendererCompositor, Renderer>(
			BindableCompositorInstance,
			_addedRenderers.Version,
			&GetCount,
			&GetVersion,
			&GetItem
		);
	}

	public void RenderAll(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();
		_actualCompositor.RenderAll();
	}

	public void WaitForGpu(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		ThrowIfDisposed();
		_actualCompositor.WaitForGpu();
	}

	public string GetNameAsNewStringObject(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _name;
	}
	public int GetNameLength(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _name.Length;
	}
	public void CopyName(ResourceHandle<RendererCompositor> handle, Span<char> destinationBuffer) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		_name.CopyTo(destinationBuffer);
	}

	public bool IsDisposed(ResourceHandle<RendererCompositor> handle) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		return _isDisposed;
	}
	public void Dispose(ResourceHandle<RendererCompositor> handle) => Dispose(handle, disposeContainedRenderers: false);
	public void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers) {
		ThrowIfHandleDoesNotBelongToThisInstance(handle);
		if (_isDisposed) return;
		try {
			_actualCompositor.Dispose();
			for (var i = _addedRenderers.Count - 1; i >= 0; --i) {
				var bindableImpl = BindableRendererImplProvider.GetBindableImplementationOrThrow(_addedRenderers[i].BindableRenderer);
				if (disposeContainedRenderers) bindableImpl.DisposeDueToOwningCompositorDisposal();
				else bindableImpl.DetachFromCompositor();
			}
			_sharedBuffer.Dispose();
			_addedRenderers.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfDisposed() {
		if (_isDisposed) throw new ObjectDisposedException(nameof(RendererCompositor));
	}

	void ThrowIfHandleDoesNotBelongToThisInstance(ResourceHandle<RendererCompositor> handle) {
		if (handle != _handle) {
			throw new InvalidOperationException($"This {nameof(RendererCompositor)} was not constructed or deserialized correctly (implementation object does not match handle).");
		}
	}
}
