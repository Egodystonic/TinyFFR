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

public sealed class LocalTinyFfrFactory : ILocalTinyFfrFactory, ILocalGpuHoldingBufferAllocator {
	static LocalTinyFfrFactory? _instance = null;

	readonly ResourceDependencyTracker _dependencyTracker = new();
	readonly ManagedStringPool _stringPool = new();
	readonly HeapPool _heapPool = new();
	readonly ArrayPoolBackedMap<ResourceIdent, ManagedStringPool.RentedStringHandle> _resourceNameMap = new();
	readonly LocalResourceGroupImplProvider _resourceGroupProvider;
#pragma warning disable CA2213 // We don't dispose this pool here because it needs to be disposed only when the native lib is done with the memory. This might be after disposal of this factory.
	readonly FixedByteBufferPool _gpuHoldingBufferPool;
#pragma warning restore CA2213

	readonly IDisplayDiscoverer _displayDiscoverer;
	readonly IWindowBuilder _windowBuilder;
	readonly ILocalApplicationLoopBuilder _applicationLoopBuilder;
	readonly IAssetLoader _assetLoader;
	readonly ICameraBuilder _cameraBuilder;
	readonly ILightBuilder _lightBuilder;
	readonly IObjectBuilder _objectBuilder;
	readonly ISceneBuilder _sceneBuilder;
	readonly IRendererBuilder _rendererBuilder;
	readonly IResourceAllocator _resourceAllocator;

	public IDisplayDiscoverer DisplayDiscoverer => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _displayDiscoverer;
	public IWindowBuilder WindowBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _windowBuilder;
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _applicationLoopBuilder;
	public IAssetLoader AssetLoader => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _assetLoader;
	public ICameraBuilder CameraBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _cameraBuilder;
	public ILightBuilder LightBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _lightBuilder;
	public IObjectBuilder ObjectBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _objectBuilder;
	public ISceneBuilder SceneBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _sceneBuilder;
	public IRendererBuilder RendererBuilder => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _rendererBuilder;
	public IResourceAllocator ResourceAllocator => IsDisposed ? throw new ObjectDisposedException(nameof(ILocalTinyFfrFactory)) : _resourceAllocator;
	FixedByteBufferPool ILocalGpuHoldingBufferAllocator.GpuHoldingBufferPool => _gpuHoldingBufferPool;

	IApplicationLoopBuilder ITinyFfrFactory.ApplicationLoopBuilder => ApplicationLoopBuilder;

	public LocalTinyFfrFactory(LocalTinyFfrFactoryConfig? factoryConfig = null, WindowBuilderConfig? windowBuilderConfig = null, LocalAssetLoaderConfig? assetLoaderConfig = null) {
		if (_instance != null) throw new InvalidOperationException($"Only one {nameof(LocalTinyFfrFactory)} may be live at any given time. Dispose the previous instance before creating another one.");

		LocalNativeUtils.InitializeNativeLibIfNecessary();
		factoryConfig ??= new LocalTinyFfrFactoryConfig();

		var resourceGroupProviderRef = new DeferredRef<LocalResourceGroupImplProvider>();
		_gpuHoldingBufferPool = new FixedByteBufferPool(factoryConfig.MaxCpuToGpuAssetTransferSizeBytes);
		var globals = new LocalFactoryGlobalObjectGroup(
			this,
			_resourceNameMap,
			_dependencyTracker,
			_stringPool,
			_heapPool,
			resourceGroupProviderRef
		);
		_resourceGroupProvider = new(globals);
		resourceGroupProviderRef.Resolve(_resourceGroupProvider);

		_displayDiscoverer = new LocalDisplayDiscoverer(globals);
		_windowBuilder = new LocalWindowBuilder(globals, windowBuilderConfig ?? new());
		_applicationLoopBuilder = new LocalApplicationLoopBuilder(globals);
		_assetLoader = new LocalAssetLoader(globals, assetLoaderConfig ?? new());
		_cameraBuilder = new LocalCameraBuilder(globals);
		_lightBuilder = new LocalLightBuilder(globals);
		_objectBuilder = new LocalObjectBuilder(globals);
		_sceneBuilder = new LocalSceneBuilder(globals);
		_rendererBuilder = new LocalRendererBuilder(globals);
		_resourceAllocator = new LocalResourceAllocator(globals);

		_instance = this;
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
			_dependencyTracker.EraseAllDependencies();

			// Maintainer's note: These are disposed in reverse order (e.g. opposite order compared to the order they're constructed in the ctor above)
			// However, by erasing all dependencies (above) we also try to avoid nasty dependency-related exceptions getting thrown which are ultimately not that useful as we're disposing everything anyway.
			DisposeObjectIfDisposable(_resourceAllocator);
			DisposeObjectIfDisposable(_rendererBuilder);
			DisposeObjectIfDisposable(_sceneBuilder);
			DisposeObjectIfDisposable(_objectBuilder);
			DisposeObjectIfDisposable(_lightBuilder);
			DisposeObjectIfDisposable(_cameraBuilder);
			DisposeObjectIfDisposable(_assetLoader);
			DisposeObjectIfDisposable(_applicationLoopBuilder);
			DisposeObjectIfDisposable(_windowBuilder);
			DisposeObjectIfDisposable(_displayDiscoverer);
			DisposeObjectIfDisposable(_resourceGroupProvider);
			DisposeObjectIfDisposable(_resourceNameMap);
			DisposeObjectIfDisposable(_heapPool);
			DisposeObjectIfDisposable(_stringPool);
			DisposeObjectIfDisposable(_dependencyTracker);
			LocalNativeUtils.DisposeTemporaryCpuBufferPoolIfSafe(this);
		}
		finally {
			IsDisposed = true;
			_instance = null;
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