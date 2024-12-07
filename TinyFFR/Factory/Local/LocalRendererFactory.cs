// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Factory.Local;

public sealed class LocalRendererFactory : ILocalRendererFactory {
	readonly ResourceDependencyTracker _dependencyTracker = new();
	readonly ManagedStringPool _stringPool = new();
	readonly LocalCombinedResourceGroupImplProvider _resourceGroupProvider;

	readonly IDisplayDiscoverer _displayDiscoverer;
	readonly IWindowBuilder _windowBuilder;
	readonly ILocalApplicationLoopBuilder _applicationLoopBuilder;
	readonly IAssetLoader _assetLoader;
	readonly ICameraBuilder _cameraBuilder;
	readonly IObjectBuilder _objectBuilder;
	readonly ISceneBuilder _sceneBuilder;
	readonly IRendererBuilder _rendererBuilder;

	public IDisplayDiscoverer DisplayDiscoverer => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _displayDiscoverer;
	public IWindowBuilder WindowBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _windowBuilder;
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _applicationLoopBuilder;
	public IAssetLoader AssetLoader => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _assetLoader;
	public ICameraBuilder CameraBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _cameraBuilder;
	public IObjectBuilder ObjectBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _objectBuilder;
	public ISceneBuilder SceneBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _sceneBuilder;
	public IRendererBuilder RendererBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalRendererFactory)) : _rendererBuilder;

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

		_displayDiscoverer = new LocalDisplayDiscoverer(globals);
		_windowBuilder = new LocalWindowBuilder(globals, windowBuilderConfig ?? new());
		_applicationLoopBuilder = new LocalApplicationLoopBuilder(globals, applicationLoopBuilderConfig ?? new());
		_assetLoader = new LocalAssetLoader(globals, assetLoaderConfig ?? new());
		_cameraBuilder = new LocalCameraBuilder(globals);
		_objectBuilder = new LocalObjectBuilder(globals);
		_sceneBuilder = new LocalSceneBuilder(globals);
		_rendererBuilder = new LocalRendererBuilder(globals);
	}

	#region Resource Group Creation
	public CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed) {
		if (IsDisposed) throw new ObjectDisposedException(nameof(ILocalRendererFactory));
		return _resourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed);
	}
	public CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name) {
		if (IsDisposed) throw new ObjectDisposedException(nameof(ILocalRendererFactory));
		return _resourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, name);
	}
	public CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, int initialCapacity) {
		if (IsDisposed) throw new ObjectDisposedException(nameof(ILocalRendererFactory));
		return _resourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, initialCapacity);
	}
	public CombinedResourceGroup CreateResourceGroup(bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name, int initialCapacity) {
		if (IsDisposed) throw new ObjectDisposedException(nameof(ILocalRendererFactory));
		return _resourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed, name, initialCapacity);
	}
	#endregion

	public override string ToString() => IsDisposed ? "TinyFFR Local Renderer Factory [Disposed]" : "TinyFFR Local Renderer Factory";

	#region Disposal
	internal bool IsDisposed { get; private set; }

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
			DisposeObjectIfDisposable(_rendererBuilder);
			DisposeObjectIfDisposable(_sceneBuilder);
			DisposeObjectIfDisposable(_objectBuilder);
			DisposeObjectIfDisposable(_cameraBuilder);
			DisposeObjectIfDisposable(_assetLoader);
			DisposeObjectIfDisposable(_applicationLoopBuilder);
			DisposeObjectIfDisposable(_windowBuilder);
			DisposeObjectIfDisposable(_displayDiscoverer);
			DisposeObjectIfDisposable(_resourceGroupProvider);
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