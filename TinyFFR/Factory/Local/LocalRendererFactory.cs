// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Factory.Local;

public sealed class LocalRendererFactory : ITinyFfrFactory {
	public IDisplayDiscoverer DisplayDiscoverer { get; }
	public IWindowBuilder WindowBuilder { get; }
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder { get; }
	public IAssetLoader AssetLoader { get; }
	public ICameraBuilder CameraBuilder { get; }

	IApplicationLoopBuilder ITinyFfrFactory.ApplicationLoopBuilder => ApplicationLoopBuilder;

	public LocalRendererFactory(WindowBuilderConfig? windowBuilderConfig = null, LocalApplicationLoopBuilderConfig? applicationLoopBuilderConfig = null, LocalAssetLoaderConfig? assetLoaderConfig = null) {
		NativeUtils.InitializeNativeLibIfNecessary();

		DisplayDiscoverer = new DisplayDiscoverer();
		WindowBuilder = new WindowBuilder(windowBuilderConfig ?? new());
		ApplicationLoopBuilder = new LocalApplicationLoopBuilder(applicationLoopBuilderConfig ?? new());
		AssetLoader = new LocalAssetLoader(assetLoaderConfig ?? new());
		CameraBuilder = new LocalCameraBuilder();
	}

	public override string ToString() => IsDisposed ? "TinyFFR Local Renderer Factory [Disposed]" : "TinyFFR Local Renderer Factory";

	#region Disposal
	bool IsDisposed { get; set; }

	public void Dispose() {
		// Maintainer's note: This is not simply accepting IDisposable because we want the flexibility
		// to make the factory objects disposable in future without forgetting to dispose them here.
		// In other words, even if 'o' isn't IDisposable today, it can be made IDisposable tomorrow and we
		// don't have to remember to add a dispose call in this function.
		static void DisposeFactoryObjectIfDisposable(object o) {
			(o as IDisposable)?.Dispose();
		}

		if (IsDisposed) return;
		try {
			// Maintainer's note: These are disposed in reverse order (e.g. opposite order compared to the order they're constructed in in the ctor above)
			DisposeFactoryObjectIfDisposable(CameraBuilder);
			DisposeFactoryObjectIfDisposable(AssetLoader);
			DisposeFactoryObjectIfDisposable(ApplicationLoopBuilder);
			DisposeFactoryObjectIfDisposable(WindowBuilder);
			DisposeFactoryObjectIfDisposable(DisplayDiscoverer);
		}
		finally {
			IsDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
	}
	#endregion

	//public (/* TODO tuple or dedicated struct of stuff handles */) BuildDefaultStuff() { } // TODO a better name, but I'd like to use this as a way to quickly create a window, camera, etc for quick "hello cube" and so on
	// TODO maybe instead of a tuple we can do the compositeresourcehandle again but allow ways of us trying to get certain handle types out of it
	// TODO or maybe this type can act as a global lookup of active resources? So we could auto-create things in the ctor (config-overridden) and then just look them up
}