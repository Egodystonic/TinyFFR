// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalRenderer : IRendererImplProvider, IDisposable {
	readonly ArrayPoolBackedMap<Window, (UIntPtr RendererPtr, UIntPtr SwapChainPtr)> _windowRendererMap = new();
	readonly ArrayPoolBackedMap<Scene, ArrayPoolBackedMap<Camera, UIntPtr>> _sceneViewMap = new();
	readonly ArrayPoolBackedMap<UIntPtr, XYPair<uint>> _viewSizeMap = new();
	readonly ObjectPool<ArrayPoolBackedMap<Camera, UIntPtr>> _viewDescriptorCameraMapPool;
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalRenderer(LocalFactoryGlobalObjectGroup globals) {
		static ArrayPoolBackedMap<Camera, UIntPtr> AllocateNewViewDescriptorCameraMapPool() => new();

		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_viewDescriptorCameraMapPool = new(&AllocateNewViewDescriptorCameraMapPool);
	}

	public void Render<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget) where TRenderTarget : IRenderTarget {
		if (renderTarget is not Window window) throw new NotImplementedException();
		if (!_windowRendererMap.TryGetValue(window, out var windowRendererTuple)) {
			AllocateRendererAndSwapChain(
				window.Handle,
				out var rendererHandle,
				out var swapChainHandle
			).ThrowIfFailure();
			windowRendererTuple = (rendererHandle, swapChainHandle);
			_windowRendererMap.Add(window, windowRendererTuple);
		}

		if (!_sceneViewMap.TryGetValue(scene, out var cameraToViewMap)) {
			cameraToViewMap = _viewDescriptorCameraMapPool.Rent();
			_sceneViewMap.Add(scene, cameraToViewMap);
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

		RenderScene(
			windowRendererTuple.RendererPtr, 
			windowRendererTuple.SwapChainPtr, 
			viewDescriptorHandle
		).ThrowIfFailure();

		// TODO track disposal of scene/camera, maybe via the dependency tracker. And I guess remove IsDisposed at that time
		// TODO I guess window too?
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

			foreach (var kvp in _sceneViewMap) {
				foreach (var kvp2 in kvp.Value) {
					DisposeViewDescriptor(kvp2.Value).ThrowIfFailure();
				}
			}

			foreach (var kvp in _windowRendererMap) {
				DisposeRendererAndSwapChain(
					kvp.Value.RendererPtr, 
					kvp.Value.SwapChainPtr
				).ThrowIfFailure();
			}
			_windowRendererMap.Dispose();
			_viewDescriptorCameraMapPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}