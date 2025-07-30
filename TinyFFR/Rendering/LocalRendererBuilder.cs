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
	readonly struct RenderTargetUnion : IRenderTarget {
		const int UnionRenderTargetOffset = 8;
		[FieldOffset(0)]
		public readonly IntPtr TypeHandle;
		[FieldOffset(UnionRenderTargetOffset)]
		public readonly Window AsWindow;
		[FieldOffset(UnionRenderTargetOffset)]
		public readonly RenderOutputBuffer AsBuffer;

		public bool IsWindow => TypeHandle == ResourceHandle<Window>.TypeHandle;
		public bool IsBuffer => TypeHandle == ResourceHandle<RenderOutputBuffer>.TypeHandle;

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
	}
	
	readonly record struct WindowData(Window Window, UIntPtr RendererPtr, UIntPtr SwapChainPtr);
	readonly record struct ViewportData(UIntPtr Handle, XYPair<uint> CurrentSize);
	readonly record struct RendererData(ResourceHandle<Renderer> Handle, Scene Scene, Camera Camera, RenderTargetUnion RenderTarget, ViewportData Viewport, bool AutoUpdateCameraAspectRatio, bool EmitFences, RenderQualityConfig Quality);
	readonly unsafe struct OutputBufferData {
		public ResourceHandle<RenderOutputBuffer> Handle { get; }
		public UIntPtr TextureHandle { get; }
		public UIntPtr RenderTargetHandle { get; }
		public XYPair<int> TextureDimensions { get; }
		public delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> OutputChangeHandler { get; }
		public Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? OutputChangeHandlerManaged { get; }
		public bool HandleOnlyNextChange { get; }

		public OutputBufferData(ResourceHandle<RenderOutputBuffer> handle, UIntPtr textureHandle, UIntPtr renderTargetHandle, XYPair<int> textureDimensions, delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> outputChangeHandler, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>? outputChangeHandlerManaged, bool handleOnlyNextChange) {
			Handle = handle;
			TextureHandle = textureHandle;
			RenderTargetHandle = renderTargetHandle;
			TextureDimensions = textureDimensions;
			OutputChangeHandler = outputChangeHandler;
			OutputChangeHandlerManaged = outputChangeHandlerManaged;
			HandleOnlyNextChange = handleOnlyNextChange;
		}

		public OutputBufferData WithOutputChangeHandler(delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> newChangeHandler, bool handleOnlyNextChange) {
			return new(Handle, TextureHandle, RenderTargetHandle, TextureDimensions, newChangeHandler, OutputChangeHandlerManaged, handleOnlyNextChange);
		}
		public OutputBufferData WithOutputChangeHandler(Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> newChangeHandler, bool handleOnlyNextChange) {
			return new(Handle, TextureHandle, RenderTargetHandle, TextureDimensions, OutputChangeHandler, newChangeHandler, handleOnlyNextChange);
		}
	}

	const string DefaultRendererName = "Unnamed Renderer";
	const string DefaultRenderOutputBufferName = "Unnamed Render Output Buffer";

	readonly ArrayPoolBackedMap<Window, WindowData> _loadedWindows = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Renderer>, RendererData> _loadedRenderers = new();
	readonly ArrayPoolBackedMap<ResourceHandle<RenderOutputBuffer>, OutputBufferData> _loadedBuffers = new();
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
		public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, handleOnlyNextChange);
		public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, bool handleOnlyNextChange) => _owner.SetOutputChangeHandler(handle, handler, handleOnlyNextChange);
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
			if (@this._loadedWindows.ContainsKey(window)) return new RenderTargetUnion(window);
			
			AllocateRendererAndSwapChain(
				window.Handle,
				out var rendererHandle,
				out var swapChainHandle
			).ThrowIfFailure();
			@this._loadedWindows.Add(window, new(window, rendererHandle, swapChainHandle));
			
			return new RenderTargetUnion(window);
		}
		static RenderTargetUnion SetUpBufferRenderer(LocalRendererBuilder @this, RenderOutputBuffer buffer) {
			return new RenderTargetUnion(buffer);
		}

		var rtu = renderTarget switch {
			Window w => SetUpWindowRenderer(this, w),
			RenderOutputBuffer b => SetUpBufferRenderer(this, b),
			_ => throw new InvalidOperationException($"{this} does not support render targets of type {typeof(TRenderTarget).Name}.")
		};
		
		AllocateViewDescriptor(
			scene.Handle,
			camera.Handle,
			out var viewDescriptorHandle
		).ThrowIfFailure();
		var viewportData = new ViewportData(viewDescriptorHandle, XYPair<uint>.Zero);

		_previousHandleId++;
		var handle = new ResourceHandle<Renderer>(_previousHandleId);
		_loadedRenderers.Add(handle, new(handle, scene, camera, rtu, viewportData, config.AutoUpdateCameraAspectRatio, config.GpuSynchronizationFrameBufferCount >= 0, config.Quality));

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
		var bufferData = new OutputBufferData(handle, textureHandle, renderTargetHandle, config.TextureDimensions, null, null, false);
		_loadedBuffers.Add(handle, bufferData);

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultRenderOutputBufferName);
		return HandleToInstance(handle);
	}

	public void Render(ResourceHandle<Renderer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		SetUpSceneShadowQuality(handle);
		
		var viewportData = _loadedRenderers[handle].Viewport;
		var curViewportSize = viewportData.CurrentSize;
		var curTargetSize = _loadedRenderers[handle].RenderTarget.ViewportDimensions;
		if (curViewportSize != curTargetSize) {
			_loadedRenderers[handle] = _loadedRenderers[handle] with { Viewport = viewportData with { CurrentSize = curTargetSize } };
			SetViewDescriptorSize(viewportData.Handle, curTargetSize.X, curTargetSize.Y).ThrowIfFailure();
			if (_loadedRenderers[handle].AutoUpdateCameraAspectRatio) {
				_loadedRenderers[handle].Camera.SetAspectRatio(curTargetSize.Ratio ?? CameraCreationConfig.DefaultAspectRatio);
			}
		}

		if (!_loadedRenderers[handle].RenderTarget.IsWindow) return;
		var windowData = _loadedWindows[_loadedRenderers[handle].RenderTarget.AsWindow];
		RenderScene(
			windowData.RendererPtr,
			windowData.SwapChainPtr,
			_loadedRenderers[handle].Viewport.Handle
		).ThrowIfFailure();

		if (_loadedRenderers[handle].EmitFences) {
			LocalFrameSynchronizationManager.EmitFenceAndCycleBuffer(handle);
		}
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
	public void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, bool handleOnlyNextChange) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedBuffers[handle] = _loadedBuffers[handle].WithOutputChangeHandler(handler, handleOnlyNextChange);
	}
	public unsafe void SetOutputChangeHandler(ResourceHandle<RenderOutputBuffer> handle, delegate*<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, bool handleOnlyNextChange) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_loadedBuffers[handle] = _loadedBuffers[handle].WithOutputChangeHandler(handler, handleOnlyNextChange);
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
		out UIntPtr windowRendererHandle,
		out UIntPtr windowSwapChainHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_view_descriptor")]
	static extern InteropResult AllocateViewDescriptor(
		UIntPtr sceneHandle,
		UIntPtr cameraHandle,
		out UIntPtr outViewDescriptorHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_view_descriptor")]
	static extern InteropResult DisposeViewDescriptor(
		UIntPtr viewDescriptorHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_renderer_and_swap_chain")]
	static extern InteropResult DisposeRendererAndSwapChain(
		UIntPtr rendererHandle,
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
		UIntPtr swapChainHandle,
		UIntPtr viewDescriptorHandle
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
		UIntPtr textureHandle,
		UIntPtr renderTargetHandle
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
		var data = _loadedRenderers[handle];
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));

		static void DisposeWindowData(LocalRendererBuilder @this, ResourceHandle<Renderer> handle, Window window) {
			@this._globals.DependencyTracker.DeregisterDependency(@this.HandleToInstance(handle), window);
			foreach (var rendererData in @this._loadedRenderers.Values) {
				if (rendererData.RenderTarget.IsWindow && rendererData.RenderTarget.AsWindow == window) return;
			}

			var windowData = @this._loadedWindows[window];
			DisposeRendererAndSwapChain(
				windowData.RendererPtr,
				windowData.SwapChainPtr
			).ThrowIfFailure();
			@this._loadedWindows.Remove(window);
		}

		if (data.EmitFences) {
			LocalFrameSynchronizationManager.DeregisterRenderer(handle);
		}

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Camera);
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), data.Scene);
		
		DisposeViewDescriptor(
			data.Viewport.Handle
		).ThrowIfFailure();

		if (data.RenderTarget.IsWindow) DisposeWindowData(this, handle, data.RenderTarget.AsWindow);

		_loadedRenderers.Remove(handle);
	}

	public bool IsDisposed(ResourceHandle<RenderOutputBuffer> handle) => _isDisposed || !_loadedBuffers.ContainsKey(handle);
	public void Dispose(ResourceHandle<RenderOutputBuffer> handle) {
		if (IsDisposed(handle)) return;

		var data = _loadedBuffers[handle];
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));

		TODO //we might need to queue this for disposal
		DisposeRenderTarget(
			data.TextureHandle,
			data.RenderTargetHandle
		).ThrowIfFailure();

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
			_loadedWindows.Dispose();
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