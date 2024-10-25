// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Factory.Local;

public sealed class LocalRendererFactory : ILocalRendererFactory {
	readonly ResourceDependencyTracker _dependencyTracker = new();
	readonly ManagedStringPool _stringPool = new();
	readonly LocalCombinedResourceGroupImplProvider _resourceGroupProvider;

	public IDisplayDiscoverer DisplayDiscoverer { get; }
	public IWindowBuilder WindowBuilder { get; }
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder { get; }
	public IAssetLoader AssetLoader { get; }
	public ICameraBuilder CameraBuilder { get; }
	public IObjectBuilder ObjectBuilder { get; }

	internal FixedByteBufferPool TemporaryCpuBufferPool { get; }

	IApplicationLoopBuilder ITinyFfrFactory.ApplicationLoopBuilder => ApplicationLoopBuilder;

	public LocalRendererFactory(LocalRendererFactoryConfig? factoryConfig = null, WindowBuilderConfig? windowBuilderConfig = null, LocalApplicationLoopBuilderConfig? applicationLoopBuilderConfig = null, LocalAssetLoaderConfig? assetLoaderConfig = null) {
		LocalNativeUtils.InitializeNativeLibIfNecessary();
		factoryConfig ??= new LocalRendererFactoryConfig();

		var resourceGroupProviderRef = new DeferredRef<LocalCombinedResourceGroupImplProvider>();
		TemporaryCpuBufferPool = new FixedByteBufferPool(factoryConfig.MaxAssetSizeBytes);
		var globals = new LocalFactoryGlobalObjectGroup(
			this,
			_dependencyTracker,
			_stringPool,
			resourceGroupProviderRef
		);
		_resourceGroupProvider = new(globals);
		resourceGroupProviderRef.Resolve(_resourceGroupProvider);

		DisplayDiscoverer = new DisplayDiscoverer(globals);
		WindowBuilder = new WindowBuilder(globals, windowBuilderConfig ?? new());
		ApplicationLoopBuilder = new LocalApplicationLoopBuilder(globals, applicationLoopBuilderConfig ?? new());
		AssetLoader = new LocalAssetLoader(globals, assetLoaderConfig ?? new());
		CameraBuilder = new LocalCameraBuilder(globals);
		ObjectBuilder = new LocalObjectBuilder(globals);
	}

	public CombinedResourceGroup CreateResourceGroup(int capacity, bool disposeContainedResourcesWhenDisposed) {
		return _resourceGroupProvider.CreateGroup(capacity, disposeContainedResourcesWhenDisposed);
	}

	public CombinedResourceGroup CreateResourceGroup(int capacity, bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name) {
		return _resourceGroupProvider.CreateGroup(capacity, disposeContainedResourcesWhenDisposed, name);
	}

	public override string ToString() => IsDisposed ? "TinyFFR Local Renderer Factory [Disposed]" : "TinyFFR Local Renderer Factory";

	#region Disposal
	public bool IsDisposed { get; private set; }

	public void Dispose() {
		// Maintainer's note: This is not simply accepting IDisposable because we want the flexibility
		// to make the factory objects disposable in future without forgetting to dispose them here.
		// In other words, even if 'o' isn't IDisposable today, it can be made IDisposable tomorrow and we
		// don't have to remember to add a dispose call in this function.
		static void DisposeObjectIfDisposable(object o) {
			(o as IDisposable)?.Dispose();
		}

		if (IsDisposed) return;
		try {
			// Maintainer's note: These are disposed in reverse order (e.g. opposite order compared to the order they're constructed in in the ctor above)
			DisposeObjectIfDisposable(ObjectBuilder);
			DisposeObjectIfDisposable(CameraBuilder);
			DisposeObjectIfDisposable(AssetLoader);
			DisposeObjectIfDisposable(ApplicationLoopBuilder);
			DisposeObjectIfDisposable(WindowBuilder);
			DisposeObjectIfDisposable(DisplayDiscoverer);
			DisposeObjectIfDisposable(_stringPool);
			DisposeObjectIfDisposable(_dependencyTracker);
			LocalNativeUtils.DisposeTemporaryCpuBufferPoolIfSafe(this);
		}
		finally {
			IsDisposed = true;
		}
	}

	internal void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(IsDisposed, typeof(ITinyFfrFactory));
	}
	#endregion

	//public (/* TODO tuple or dedicated struct of stuff handles */) BuildDefaultStuff() { } // TODO a better name, but I'd like to use this as a way to quickly create a window, camera, etc for quick "hello cube" and so on
	// TODO maybe instead of a tuple we can do the compositeresourcehandle again but allow ways of us trying to get certain handle types out of it
	// TODO or maybe this type can act as a global lookup of active resources? So we could auto-create things in the ctor (config-overridden) and then just look them up
}