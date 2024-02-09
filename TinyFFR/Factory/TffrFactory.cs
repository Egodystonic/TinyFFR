// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Factory;

#pragma warning disable CA2000 // "Dispose local variables of IDisposable type": Overzealous in this class
public sealed class TffrFactory : ITffrFactory {
	readonly FactoryObjectStore<IDisplayDiscoverer> _displayDiscoverers = new();
	readonly FactoryObjectStore<WindowBuilderConfig, IWindowBuilder> _windowBuilders = new();
	readonly FactoryObjectStore<ApplicationLoopBuilderConfig, IApplicationLoopBuilder> _loopBuilders = new();

	public bool IsDisposed { get; private set; }

	public TffrFactory() : this(new()) { }
	public TffrFactory(FactoryConfig config) {
		NativeUtils.InitializeNativeLibIfNecessary();
	}

	public IDisplayDiscoverer GetDisplayDiscoverer() {
		ThrowIfThisIsDisposed();
		if (!_displayDiscoverers.ContainsObjectType<NativeDisplayDiscoverer>()) _displayDiscoverers.SetObjectOfType(new NativeDisplayDiscoverer());
		return _displayDiscoverers.GetObjectOfType<NativeDisplayDiscoverer>();
	}

	public IWindowBuilder GetWindowBuilder() => GetWindowBuilder(new());
	public IWindowBuilder GetWindowBuilder(WindowBuilderConfig config) {
		ThrowIfThisIsDisposed();
		if (!_windowBuilders.ContainsObjectForConfig(config)) _windowBuilders.SetObjectForConfig(config, new NativeWindowBuilder(config));
		return _windowBuilders.GetObjectForConfig(config);
	}

	public IApplicationLoopBuilder GetApplicationLoopBuilder() => GetApplicationLoopBuilder(new());
	public IApplicationLoopBuilder GetApplicationLoopBuilder(ApplicationLoopBuilderConfig config) {
		ThrowIfThisIsDisposed();
		if (!_loopBuilders.ContainsObjectForConfig(config)) _loopBuilders.SetObjectForConfig(config, new NativeApplicationLoopBuilder(config));
		return _loopBuilders.GetObjectForConfig(config);
	}

	public void Dispose() {
		if (IsDisposed) return;
		try {
			_displayDiscoverers.Dispose();
			_windowBuilders.Dispose();
			_loopBuilders.Dispose();
		}
		finally {
			IsDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
	}

	//public (/* TODO tuple or dedicated struct of stuff handles */) BuildDefaultStuff() { } // TODO a better name, but I'd like to use this as a way to quickly create a window, camera, etc for quick "hello cube" and so on
	// TODO maybe instead of a tuple we can do the compositeresourcehandle again but allow ways of us trying to get certain handle types out of it
	// TODO or maybe this type can act as a global lookup of active resources? So we could auto-create things in the ctor (config-overridden) and then just look them up
}
#pragma warning restore CA2000