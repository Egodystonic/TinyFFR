// Created on 2024-11-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using static System.Formats.Asn1.AsnWriter;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalRendererBuilder : IRendererBuilder, IRendererImplProvider, IDisposable {
	[StructLayout(LayoutKind.Explicit)]
	readonly struct RenderTargetUnion : IRenderTarget {
		[FieldOffset(0)]
		public readonly IntPtr TypeHandle;
		[FieldOffset(8)]
		public readonly Window AsWindow;

		public bool IsWindow => TypeHandle == WindowHandle.TypeHandle;

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
			TypeHandle = WindowHandle.TypeHandle;
			AsWindow = window;
		}
	}
	
	readonly record struct WindowData(Window Window, UIntPtr RendererPtr, UIntPtr SwapChainPtr);
	readonly record struct ViewportData(UIntPtr Handle, XYPair<uint> CurrentSize);
	readonly record struct RendererData(RendererHandle Handle, Scene Scene, Camera Camera, RenderTargetUnion RenderTarget, ViewportData Viewport);

	const string DefaultRendererName = "Unnamed Renderer";

	readonly ArrayPoolBackedMap<Window, WindowData> _loadedWindows = new();
	readonly ArrayPoolBackedMap<RendererHandle, RendererData> _loadedRenderers = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	nuint _previousHandleId = 0U;
	bool _isDisposed = false;

	public LocalRendererBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public Renderer CreateRenderer<TRenderTarget>(Scene scene, Camera camera, TRenderTarget renderTarget, in RendererCreationConfig config) where TRenderTarget : IRenderTarget {
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
		var handle = new RendererHandle(_previousHandleId);
		_loadedRenderers.Add(handle, new(handle, scene, camera, new(window), viewportData));

		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		return HandleToInstance(handle);
	}

	public void Render(RendererHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var viewportData = _loadedRenderers[handle].Viewport;
		var curViewportSize = viewportData.CurrentSize;
		var curTargetSize = _loadedRenderers[handle].RenderTarget.ViewportDimensions;
		if (curViewportSize != curTargetSize) {
			_loadedRenderers[handle] = _loadedRenderers[handle] with { Viewport = viewportData with { CurrentSize = curTargetSize } };
			SetViewDescriptorSize(viewportData.Handle, curTargetSize.X, curTargetSize.Y).ThrowIfFailure();

		}

		if (!_loadedRenderers[handle].RenderTarget.IsWindow) return;
		var windowData = _loadedWindows[_loadedRenderers[handle].RenderTarget.AsWindow];
		RenderScene(
			windowData.RendererPtr,
			windowData.SwapChainPtr,
			_loadedRenderers[handle].Viewport.Handle
		).ThrowIfFailure();
	}

	public ReadOnlySpan<char> GetName(RendererHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultRendererName);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Renderer HandleToInstance(RendererHandle h) => new(h, this);

	#region Disposal
	public bool IsDisposed(RendererHandle handle) => _isDisposed || !_loadedRenderers.ContainsKey(handle);
	public void Dispose(RendererHandle handle) {
		if (IsDisposed(handle)) return;

		var data = _loadedRenderers[handle];

		DisposeViewDescriptor(
			data.Viewport.Handle
		).ThrowIfFailure();

		_loadedRenderers.Remove(handle);

		if (!data.RenderTarget.IsWindow) return;
		var window = data.RenderTarget.AsWindow;
		foreach (var rendererData in _loadedRenderers.Values) {
			if (rendererData.RenderTarget.IsWindow && rendererData.RenderTarget.AsWindow == window) return;
		}

		var windowData = _loadedWindows[window];
		DisposeRendererAndSwapChain(
			windowData.RendererPtr,
			windowData.SwapChainPtr
		).ThrowIfFailure();
		_loadedWindows.Remove(window);
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

	void ThrowIfThisOrHandleIsDisposed(RendererHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Renderer));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}