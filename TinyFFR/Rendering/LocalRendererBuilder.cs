// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;
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

		public bool IsWindow => TypeHandle == ResourceHandle<Window>.TypeHandle;

		public XYPair<int> ViewportOffset {
			get {
				if (IsWindow) return ((IRenderTarget) AsWindow).ViewportOffset;
				throw new InvalidOperationException();
			}
		}
		public XYPair<uint> ViewportDimensions {
			get {
				if (IsWindow) return ((IRenderTarget) AsWindow).ViewportDimensions;
				throw new InvalidOperationException();
			}
		}

		public RenderTargetUnion(Window window) {
			TypeHandle = ResourceHandle<Window>.TypeHandle;
			AsWindow = window;
		}
	}
	
	readonly record struct WindowData(Window Window, UIntPtr RendererPtr, UIntPtr SwapChainPtr);
	readonly record struct ViewportData(UIntPtr Handle, XYPair<uint> CurrentSize);
	readonly record struct RendererData(ResourceHandle<Renderer> Handle, Scene Scene, Camera Camera, RenderTargetUnion RenderTarget, ViewportData Viewport, bool AutoUpdateCameraAspectRatio, bool EmitFences, RenderQualityConfig Quality);

	const string DefaultRendererName = "Unnamed Renderer";

	readonly ArrayPoolBackedMap<Window, WindowData> _loadedWindows = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Renderer>, RendererData> _loadedRenderers = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	nuint _previousHandleId = 0U;
	bool _isDisposed = false;

	public LocalRendererBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget {
		ThrowIfThisIsDisposed();
		if (renderTarget is not Window window) throw new NotImplementedException();

		if (!_loadedWindows.ContainsKey(window)) {
			AllocateRendererAndSwapChain(
				window.Handle,
				out var rendererHandle,
				out var swapChainHandle
			).ThrowIfFailure();
			_loadedWindows.Add(window, new(window, rendererHandle, swapChainHandle));
		}

		AllocateViewDescriptor(
			scene.Handle,
			camera.Handle,
			out var viewDescriptorHandle
		).ThrowIfFailure();
		var viewportData = new ViewportData(viewDescriptorHandle, XYPair<uint>.Zero);

		_previousHandleId++;
		var handle = new ResourceHandle<Renderer>(_previousHandleId);
		_loadedRenderers.Add(handle, new(handle, scene, camera, new(window), viewportData, config.AutoUpdateCameraAspectRatio, config.GpuSynchronizationFrameBufferCount >= 0, config.Quality));

		_globals.StoreResourceNameIfNotEmpty(handle.Ident, config.Name);

		var result = HandleToInstance(handle);
		_globals.DependencyTracker.RegisterDependency(result, scene);
		_globals.DependencyTracker.RegisterDependency(result, camera);
		_globals.DependencyTracker.RegisterDependency(result, window);

		if (config.GpuSynchronizationFrameBufferCount >= 0) {
			LocalFrameSynchronizationManager.RegisterRenderer(handle, config.GpuSynchronizationFrameBufferCount); 
		}

		SetQualityConfig(handle, config.Quality);

		return result;
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
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Renderer HandleToInstance(ResourceHandle<Renderer> h) => new(h, this);

	#region Disposal
	public bool IsDisposed(ResourceHandle<Renderer> handle) => _isDisposed || !_loadedRenderers.ContainsKey(handle);
	public void Dispose(ResourceHandle<Renderer> handle) {
		if (IsDisposed(handle)) return;

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

		var data = _loadedRenderers[handle];

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

	public void Dispose() {
		if (_isDisposed) return;
		try {
			while (_loadedRenderers.Count > 0) {
				Dispose(_loadedRenderers.GetPairAtIndex(0).Key);
			}

			_loadedRenderers.Dispose();
			_loadedWindows.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Renderer> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Renderer));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}