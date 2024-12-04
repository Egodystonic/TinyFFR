// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using static System.Formats.Asn1.AsnWriter;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalRendererBuilder : IRendererBuilder, IRendererImplProvider, IDisposable {
	readonly record struct RendererArgs(RendererHandle Handle, Scene Scene, Camera Camera, Window Window, RendererCreationConfig Conmfig);
	readonly record struct WindowArgs(Window Window, UIntPtr RendererPtr, UIntPtr SwapChainPtr);
	readonly record struct ViewArgs(Scene Scene, Camera Camera, UIntPtr View, XYPair<uint> CurrentSize);

	readonly ArrayPoolBackedMap<Window, WindowArgs> _loadedWindows = new();
	readonly ArrayPoolBackedMap<(Scene Scene, Camera Camera), ViewArgs> _loadedViews = new();
	readonly ArrayPoolBackedMap<RendererHandle, RendererArgs> _loadedRenderers = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalRendererBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget {
		if (renderTarget is not Window window) throw new NotImplementedException();
		if (!_loadedWindowData.TryGetValue(window, out var windowRendererTuple)) {
			AllocateRendererAndSwapChain(
				window.Handle,
				out var rendererHandle,
				out var swapChainHandle
			).ThrowIfFailure();
			windowRendererTuple = (rendererHandle, swapChainHandle);
			_loadedWindowData.Add(window, windowRendererTuple);
		}

		if (!_sceneCameraViewMap.TryGetValue(scene, out var cameraToViewMap)) {
			cameraToViewMap = _viewDescriptorCameraMapPool.Rent();
			_sceneCameraViewMap.Add(scene, cameraToViewMap);
		}
		if (!cameraToViewMap.TryGetValue(camera, out var viewDescriptorHandle)) {
			AllocateViewDescriptor(
				scene.Handle,
				camera.Handle,
				out viewDescriptorHandle
			).ThrowIfFailure();
			cameraToViewMap.Add(camera, viewDescriptorHandle);
		}

		if (!_viewSizeMap.TryGetValue(viewDescriptorHandle, out var curViewSize) || curViewSize != renderTarget.ViewportDimensions) {
			curViewSize = renderTarget.ViewportDimensions;
			_viewSizeMap[viewDescriptorHandle] = curViewSize;
			SetViewDescriptorSize(viewDescriptorHandle, curViewSize.X, curViewSize.Y).ThrowIfFailure();
		}

		
	}

	public void Render(RendererHandle handle) {
		RenderScene(
			windowRendererTuple.RendererPtr, 
			windowRendererTuple.SwapChainPtr, 
			viewDescriptorHandle
		).ThrowIfFailure();
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
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "render_scene")]
	static extern InteropResult RenderScene(
		UIntPtr rendererHandle,
		UIntPtr swapChainHandle,
		UIntPtr viewDescriptorHandle
	);
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_viewSizeMap.Dispose();

			foreach (var kvp in _sceneCameraViewMap) {
				foreach (var kvp2 in kvp.Value) {
					DisposeViewDescriptor(kvp2.Value).ThrowIfFailure();
				}
			}

			foreach (var kvp in _loadedWindowData) {
				DisposeRendererAndSwapChain(
					kvp.Value.RendererPtr, 
					kvp.Value.SwapChainPtr
				).ThrowIfFailure();
			}
			_loadedWindowData.Dispose();
			_viewDescriptorCameraMapPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}