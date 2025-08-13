// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;
using System;
using static System.Formats.Asn1.AsnWriter;

namespace Egodystonic.TinyFFR.Rendering;

sealed class LocalRendererBuilder : IRendererBuilder, IRendererImplProvider, IDisposable {
	[StructLayout(LayoutKind.Explicit)]
	readonly struct RenderTargetUnion : IRenderTarget, IEquatable<RenderTargetUnion> {
		const int UnionRenderTargetOffset = 8;
		[FieldOffset(0)]
		public readonly IntPtr TypeHandle;
		[FieldOffset(UnionRenderTargetOffset)]
		public readonly Window AsWindow;
		[FieldOffset(UnionRenderTargetOffset)]
		public readonly RenderOutputBuffer AsBuffer;

		public bool IsWindow => TypeHandle == ResourceHandle<Window>.TypeHandle;
		public bool IsBuffer => TypeHandle == ResourceHandle<RenderOutputBuffer>.TypeHandle;
		public ResourceHandle RawHandle {
			get {
				if (TypeHandle == ResourceHandle<Window>.TypeHandle) return AsWindow.Handle;
				if (TypeHandle == ResourceHandle<RenderOutputBuffer>.TypeHandle) return AsBuffer.Handle;
				throw new InvalidOperationException($"Unknown render target type ({TypeHandle}).");
			}
		}

		public XYPair<int> ViewportOffset {
			get {
				if (IsWindow) return ((IRenderTarget) AsWindow).ViewportOffset;
				if (IsBuffer) return ((IRenderTarget) AsBuffer).ViewportOffset;
				throw new InvalidOperationException();
			}
		}
		public XYPair<uint> ViewportDimensions {
			get {
				if (IsWindow) return ((IRenderTarget) AsWindow).ViewportDimensions;
				if (IsBuffer) return ((IRenderTarget) AsBuffer).ViewportDimensions;
				throw new InvalidOperationException();
			}
		}

		public RenderTargetUnion(Window window) {
			TypeHandle = ResourceHandle<Window>.TypeHandle;
			AsWindow = window;
		}
		public RenderTargetUnion(RenderOutputBuffer buffer) {
			TypeHandle = ResourceHandle<RenderOutputBuffer>.TypeHandle;
			AsBuffer = buffer;
		}

		public bool Equals(RenderTargetUnion other) => TypeHandle == other.TypeHandle && RawHandle == other.RawHandle;
		public override bool Equals(object? obj) => obj is RenderTargetUnion other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(TypeHandle, RawHandle);
		public static bool operator ==(RenderTargetUnion left, RenderTargetUnion right) => left.Equals(right);
		public static bool operator !=(RenderTargetUnion left, RenderTargetUnion right) => !left.Equals(right);
	}
	
	readonly record struct TargetSpecificData(UIntPtr RendererPtr, UIntPtr? SwapChainPtr);
	readonly record struct ViewportData(UIntPtr Handle, XYPair<uint> CurrentSize);
	readonly record struct RendererData(Scene Scene, Camera Camera, RenderTargetUnion RenderTarget, ViewportData Viewport, bool AutoUpdateCameraAspectRatio, bool EmitFences, RenderQualityConfig Quality);
	readonly unsafe struct OutputBufferCallbackData {
		public delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> OutputChangeHandler { get; }
		public Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? OutputChangeHandlerManaged { get; }

		public bool AnySet => OutputChangeHandler != null || OutputChangeHandlerManaged != null;

		public OutputBufferCallbackData(delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> outputChangeHandler, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>>? outputChangeHandlerManaged) {
			OutputChangeHandler = outputChangeHandler;
			OutputChangeHandlerManaged = outputChangeHandlerManaged;
		}
	}
	readonly record struct OutputBufferData(ResourceHandle<RenderOutputBuffer> Handle, XYPair<int> TextureDimensions, UIntPtr TextureHandle, UIntPtr RenderTargetHandle, OutputBufferCallbackData RenderCompletionHandlers, bool HandleOnlyNextChange);

	const string DefaultRendererName = "Unnamed Renderer";
	const string DefaultRenderOutputBufferName = "Unnamed Render Output Buffer";

	static readonly ArrayPoolBackedMap<nuint, (LocalRendererBuilder Builder, ResourceHandle<RenderOutputBuffer> BufferHandle, OutputBufferCallbackData Callbacks)> _pendingRenderTargetReadbacks = new();
	readonly ArrayPoolBackedMap<RenderTargetUnion, TargetSpecificData> _loadedTargets = new();
	readonly ArrayPoolBackedMap<ResourceHandle<RenderOutputBuffer>, OutputBufferData> _loadedBuffers = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Renderer>, RendererData> _loadedRenderers = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly RenderOutputBufferImplProvider _renderOutputBufferImplProvider;
	nuint _previousHandleId = 0U;
	bool _isDisposed = false;

	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IRenderOutputBufferImplProvider and IRendererBuilder. 
	sealed class RenderOutputBufferImplProvider : IRenderOutputBufferImplProvider {
		readonly LocalRendererBuilder _owner;

		public RenderOutputBufferImplProvider(LocalRendererBuilder owner) => _owner = owner;

		public string GetNameAsNewStringObject(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetNameAsNewStringObject(handle);
		public int GetNameLength(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetNameLength(handle);
		public void CopyName(ResourceHandle<RenderOutputBuffer> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
		public bool IsDisposed(ResourceHandle<RenderOutputBuffer> handle) => _owner.IsDisposed(handle);
		public void Dispose(ResourceHandle<RenderOutputBuffer> handle) => _owner.Dispose(handle);
		public XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle) => _owner.GetTextureDimensions(handle);
		public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, handleOnlyNextChange);
		public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, handleOnlyNextChange);
		public void ClearOutputChangeHandlers(ResourceHandle<RenderOutputBuffer> handle, bool cancelQueuedFrames) => _owner.ClearOutputChangeHandlers(handle, cancelQueuedFrames);
		public override string ToString() => _owner.ToString();
	}

	public LocalRendererBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_renderOutputBufferImplProvider = new(this);
	}

	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget, IResource<TRenderTarget> {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		static RenderTargetUnion SetUpWindowRenderer(LocalRendererBuilder @this, Window window) {
			var result = new RenderTargetUnion(window);

			if (@this._loadedTargets.ContainsKey(result)) return result;
			
			AllocateRendererAndSwapChain(
				window.Handle,
				out var rendererHandle,
				out var swapChainHandle
			).ThrowIfFailure();
			@this._loadedTargets.Add(result, new(rendererHandle, swapChainHandle));
			return result;
		}
		static RenderTargetUnion SetUpBufferRenderer(LocalRendererBuilder @this, RenderOutputBuffer buffer) {
			var result = new RenderTargetUnion(buffer);

			if (@this._loadedTargets.ContainsKey(result)) return result;

			AllocateRenderer(
				out var rendererHandle
			).ThrowIfFailure();
			@this._loadedTargets.Add(result, new(rendererHandle, null));
			return result;
		}

		var rtu = renderTarget switch {
			Window w => SetUpWindowRenderer(this, w),
			RenderOutputBuffer b => SetUpBufferRenderer(this, b),
			_ => throw new InvalidOperationException($"{this} does not support render targets of type {typeof(TRenderTarget).Name}.")
		};
		
		AllocateViewDescriptor(
			scene.Handle,
			camera.Handle,
			rtu.IsBuffer ? _loadedBuffers[rtu.AsBuffer.Handle].RenderTargetHandle : UIntPtr.Zero,
			out var viewDescriptorHandle
		).ThrowIfFailure();
		var viewportData = new ViewportData(viewDescriptorHandle, XYPair<uint>.Zero);

		_previousHandleId++;
		var handle = new ResourceHandle<Renderer>(_previousHandleId);
		_loadedRenderers.Add(handle, new(scene, camera, rtu, viewportData, config.AutoUpdateCameraAspectRatio, config.GpuSynchronizationFrameBufferCount >= 0, config.Quality));

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultRendererName);

		var result = HandleToInstance(handle);
		_globals.DependencyTracker.RegisterDependency(result, scene);
		_globals.DependencyTracker.RegisterDependency(result, camera);
		_globals.DependencyTracker.RegisterDependency(result, renderTarget);

		if (config.GpuSynchronizationFrameBufferCount >= 0) {
			LocalFrameSynchronizationManager.RegisterRenderer(handle, config.GpuSynchronizationFrameBufferCount); 
		}

		SetQualityConfig(handle, config.Quality);

		return result;
	}

	public unsafe RenderOutputBuffer CreateRenderOutputBuffer(in RenderOutputBufferCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocateRenderTarget(
			config.TextureDimensions.X,
			config.TextureDimensions.Y,
			out var textureHandle,
			out var renderTargetHandle
		).ThrowIfFailure();

		_previousHandleId++;
		var handle = new ResourceHandle<RenderOutputBuffer>(_previousHandleId);
		var bufferData = new OutputBufferData(handle, config.TextureDimensions, textureHandle, renderTargetHandle, new(null, null), false);
		_loadedBuffers.Add(handle, bufferData);

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultRenderOutputBufferName);
		return HandleToInstance(handle);
	}

	public unsafe void Render(ResourceHandle<Renderer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		SetUpSceneShadowQuality(handle);

		var rendererData = _loadedRenderers[handle];
		var viewportData = rendererData.Viewport;
		var curViewportSize = viewportData.CurrentSize;
		var curTargetSize = rendererData.RenderTarget.ViewportDimensions;
		if (curViewportSize != curTargetSize) {
			rendererData = rendererData with { Viewport = viewportData with { CurrentSize = curTargetSize } };
			SetViewDescriptorSize(viewportData.Handle, curTargetSize.X, curTargetSize.Y).ThrowIfFailure();
			if (rendererData.AutoUpdateCameraAspectRatio) {
				rendererData.Camera.SetAspectRatio(curTargetSize.Ratio ?? CameraCreationConfig.DefaultAspectRatio);
			}
		}

		var targetData = _loadedTargets[rendererData.RenderTarget];
		if (rendererData.RenderTarget.IsWindow) {
			RenderScene(
				targetData.RendererPtr,
				rendererData.Viewport.Handle,
				targetData.SwapChainPtr!.Value
			).ThrowIfFailure();
		}
		else if (rendererData.RenderTarget.IsBuffer) {
			var bufferData = _loadedBuffers[rendererData.RenderTarget.AsBuffer.Handle];
			if (!bufferData.RenderCompletionHandlers.AnySet) {
				RenderScene(
					targetData.RendererPtr,
					rendererData.Viewport.Handle,
					bufferData.RenderTargetHandle,
					null,
					0U,
					0U,
					0U,
					0U
				).ThrowIfFailure();
			}
			else {
				var requiredSize = bufferData.TextureDimensions.Area * sizeof(TexelRgba32);
				var buffer = _globals.CreateGpuHoldingBuffer(requiredSize, &HandleRenderTargetReadback);
				_pendingRenderTargetReadbacks.Add(
					buffer.BufferIdentity,
					(this, bufferData.Handle, bufferData.RenderCompletionHandlers)
				);
				if (bufferData.HandleOnlyNextChange) {
					_loadedBuffers[bufferData.Handle] = _loadedBuffers[bufferData.Handle] with {
						RenderCompletionHandlers = new(),
						HandleOnlyNextChange = false
					};
				}
				RenderScene(
					targetData.RendererPtr,
					rendererData.Viewport.Handle,
					bufferData.RenderTargetHandle,
					(byte*) buffer.DataPtr,
					(uint) buffer.DataLengthBytes,
					(uint) bufferData.TextureDimensions.X,
					(uint) bufferData.TextureDimensions.Y,
					buffer.BufferIdentity
				).ThrowIfFailure();
			}
		}

		if (rendererData.EmitFences) {
			LocalFrameSynchronizationManager.EmitFenceAndCycleBuffer(handle);
		}
	}

	public void WaitForGpu(ResourceHandle<Renderer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var rendererData = _loadedRenderers[handle];
		if (!rendererData.EmitFences) return;

		LocalFrameSynchronizationManager.FlushAllPendingFences(handle);
	}

	static unsafe void HandleRenderTargetReadback(nuint bufferIdentity, ReadOnlySpan<byte> data) {
		if (!_pendingRenderTargetReadbacks.Remove(bufferIdentity, out var tuple)) return; // Can happen if user has cancelled pending readbacks
		if (tuple.Builder.IsDisposed(tuple.BufferHandle)) return; // Can happen if user has disposed target output buffer

		var dimensions = tuple.Builder._loadedBuffers[tuple.BufferHandle].TextureDimensions;
		var requiredLength = dimensions.Area * sizeof(TexelRgba32);
		if (data.Length < requiredLength) {
			throw new InvalidOperationException($"Received render target readback for output buffer with dimensions {dimensions} " +
												$"(requiring buffer size {requiredLength}), " +
												$"but buffer size was {data.Length}.");
		}
		if (tuple.Callbacks.OutputChangeHandler != null) tuple.Callbacks.OutputChangeHandler(dimensions, MemoryMarshal.Cast<byte, TexelRgba32>(data[..requiredLength]));
		else tuple.Callbacks.OutputChangeHandlerManaged?.Invoke(dimensions, MemoryMarshal.Cast<byte, TexelRgba32>(data[..requiredLength]));
	}

	public void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedRenderers[handle] = _loadedRenderers[handle] with { Quality = newConfig };
		SetViewShadowFidelityLevel(_loadedRenderers[handle].Viewport.Handle, (int) newConfig.ShadowQuality).ThrowIfFailure();
	}

	public XYPair<int> GetTextureDimensions(ResourceHandle<RenderOutputBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedBuffers[handle].TextureDimensions;
	}
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, bool handleOnlyNextChange) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedBuffers[handle] = _loadedBuffers[handle] with {
			RenderCompletionHandlers = new(null, handler),
			HandleOnlyNextChange = handleOnlyNextChange
		};
	}
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, bool handleOnlyNextChange) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedBuffers[handle] = _loadedBuffers[handle] with {
			RenderCompletionHandlers = new(handler, null),
			HandleOnlyNextChange = handleOnlyNextChange
		};
	}
	public unsafe void ClearOutputChangeHandlers(ResourceHandle<RenderOutputBuffer> handle, bool cancelQueuedFrames) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedBuffers[handle] = _loadedBuffers[handle] with { RenderCompletionHandlers = new(null, null) };
		if (!cancelQueuedFrames) return;

		using var currentlyQueuedFrameIdentities = _globals.HeapPool.Borrow<nuint>(_pendingRenderTargetReadbacks.Count);
		var i = -1;
		foreach (var key in _pendingRenderTargetReadbacks.Keys) {
			currentlyQueuedFrameIdentities.Buffer[++i] = key;
		}

		for (; i >= 0; --i) {
			var bufferId = currentlyQueuedFrameIdentities.Buffer[i];
			var tuple = _pendingRenderTargetReadbacks[bufferId];
			if (tuple.Builder == this && tuple.BufferHandle == handle) _pendingRenderTargetReadbacks.Remove(bufferId);
		}
	}

	public string GetNameAsNewStringObject(ResourceHandle<Renderer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultRendererName));
	}
	public int GetNameLength(ResourceHandle<Renderer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultRendererName).Length;
	}
	public void CopyName(ResourceHandle<Renderer> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultRendererName, destinationBuffer);
	}
	public string GetNameAsNewStringObject(ResourceHandle<RenderOutputBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultRenderOutputBufferName));
	}
	public int GetNameLength(ResourceHandle<RenderOutputBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultRenderOutputBufferName).Length;
	}
	public void CopyName(ResourceHandle<RenderOutputBuffer> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultRenderOutputBufferName, destinationBuffer);
	}

	void SetUpSceneShadowQuality(ResourceHandle<Renderer> handle) {
		var scene = _loadedRenderers[handle].Scene;
		var quality = _loadedRenderers[handle].Quality.ShadowQuality;

		// Currently in filament the cascade count only really affects directional lights, but we set values anyway in case that changes one day
		switch (quality) {
			case Quality.VeryLow:
				scene.SetLightShadowFidelity(
					quality,
					pointLightFidelity:			new(256, 1),
					spotLightFidelity:			new(256, 1),
					directionalLightFidelity:	new(1024, 1)
				);
				break;
			case Quality.Low:
				scene.SetLightShadowFidelity(
					quality,
					pointLightFidelity:			new(512, 1),
					spotLightFidelity:			new(512, 1),
					directionalLightFidelity:	new(2048, 2)
				);
				break;
			case Quality.High:
				scene.SetLightShadowFidelity(
					quality,
					pointLightFidelity:			new(1024, 2),
					spotLightFidelity:			new(1024, 2),
					directionalLightFidelity:	new(2048, 4)
				);
				break;
			case Quality.VeryHigh:
				scene.SetLightShadowFidelity(
					quality,
					pointLightFidelity:			new(2048, 4),
					spotLightFidelity:			new(2048, 4),
					directionalLightFidelity:	new(4096, 4)
				);
				break;
			default:
				scene.SetLightShadowFidelity(
					quality,
					pointLightFidelity:			new(1024, 1),
					spotLightFidelity:			new(1024, 1),
					directionalLightFidelity:	new(2048, 3)
				);
				break;
		}
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_renderer_and_swap_chain")]
	static extern InteropResult AllocateRendererAndSwapChain(
		UIntPtr windowHandle,
		out UIntPtr rendererHandle,
		out UIntPtr swapChainHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_renderer")]
	static extern InteropResult AllocateRenderer(
		out UIntPtr rendererHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_view_descriptor")]
	static extern InteropResult AllocateViewDescriptor(
		UIntPtr sceneHandle,
		UIntPtr cameraHandle,
		UIntPtr optionalRenderTargetHandle,
		out UIntPtr outViewDescriptorHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_view_descriptor")]
	static extern InteropResult DisposeViewDescriptor(
		UIntPtr viewDescriptorHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_renderer")]
	static extern InteropResult DisposeRenderer(
		UIntPtr rendererHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_swap_chain")]
	static extern InteropResult DisposeSwapChain(
		UIntPtr swapChainHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_view_descriptor_size")]
	static extern InteropResult SetViewDescriptorSize(
		UIntPtr viewDescriptorHandle,
		uint width,
		uint height
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_view_shadow_fidelity_level")]
	static extern InteropResult SetViewShadowFidelityLevel(
		UIntPtr viewDescriptorHandle,
		int level
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "render_scene")]
	static extern InteropResult RenderScene(
		UIntPtr rendererHandle,
		UIntPtr viewDescriptorHandle,
		UIntPtr swapChainHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "render_scene_standalone")]
	static extern unsafe InteropResult RenderScene(
		UIntPtr rendererHandle,
		UIntPtr viewDescriptorHandle,
		UIntPtr renderTargetHandle,
		byte* optionalReadbackBufferPtr,
		uint readbackBufferLengthBytes,
		uint readbackBufferWidth,
		uint readbackBufferHeight,
		nuint readbackBufferIdentity
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_render_target")]
	static extern InteropResult AllocateRenderTarget(
		int width,
		int height,
		out UIntPtr textureHandle,
		out UIntPtr renderTargetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_render_target")]
	static extern InteropResult DisposeRenderTarget(
		UIntPtr renderTargetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_render_target_buffer")]
	static extern InteropResult DisposeRenderTargetBuffer(
		UIntPtr textureHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Renderer HandleToInstance(ResourceHandle<Renderer> h) => new(h, this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	RenderOutputBuffer HandleToInstance(ResourceHandle<RenderOutputBuffer> h) => new(h, _renderOutputBufferImplProvider);

	public override string ToString() => _isDisposed ? "TinyFFR Local Renderer Builder [Disposed]" : "TinyFFR Local Renderer Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Renderer> handle) => _isDisposed || !_loadedRenderers.ContainsKey(handle);
	public void Dispose(ResourceHandle<Renderer> handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));

		var data = _loadedRenderers[handle];
		if (data.EmitFences) LocalFrameSynchronizationManager.DeregisterRenderer(handle);

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Camera);
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Scene);
		if (data.RenderTarget.IsWindow) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.RenderTarget.AsWindow);
		if (data.RenderTarget.IsBuffer) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.RenderTarget.AsBuffer);
		_globals.DisposeResourceNameIfExists(handle.Ident);

		DisposeViewDescriptor(
			data.Viewport.Handle
		).ThrowIfFailure();

		_loadedRenderers.Remove(handle);
		foreach (var rendererData in _loadedRenderers.Values) {
			if (rendererData.RenderTarget == data.RenderTarget) return;
		}

		_loadedTargets.Remove(data.RenderTarget, out var swapChainData);
		DisposeRenderer(
			swapChainData.RendererPtr
		).ThrowIfFailure();
		if (swapChainData.SwapChainPtr == null) return;
		DisposeSwapChain(
			swapChainData.SwapChainPtr.Value
		).ThrowIfFailure();
	}

	public bool IsDisposed(ResourceHandle<RenderOutputBuffer> handle) => _isDisposed || !_loadedBuffers.ContainsKey(handle);
	public unsafe void Dispose(ResourceHandle<RenderOutputBuffer> handle) {
		if (IsDisposed(handle)) return;

		var data = _loadedBuffers[handle];
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.TextureHandle, &DisposeRenderTargetBuffer);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.RenderTargetHandle, &DisposeRenderTarget);
		_globals.DisposeResourceNameIfExists(handle.Ident);

		_loadedBuffers.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			while (_loadedRenderers.Count > 0) {
				Dispose(_loadedRenderers.GetPairAtIndex(0).Key);
			}
			while (_loadedBuffers.Count > 0) {
				Dispose(_loadedBuffers.GetPairAtIndex(0).Key);
			}

			_loadedRenderers.Dispose();
			_loadedTargets.Dispose();
			_loadedBuffers.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Renderer> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Renderer));
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<RenderOutputBuffer> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(RenderOutputBuffer));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}