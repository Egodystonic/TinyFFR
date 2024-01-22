// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Windowing;

namespace Egodystonic.TinyFFR.Factory;

#pragma warning disable CA2000 // "Dispose local variables of IDisposable type": Overzealous in this class
// Implementation note: Using GetXyz() instead of properties because it allows us to parameterize the getting of builders/loaders now or in future and keep everything consistent API-wise
public sealed class TffrFactory : ITffrFactory {
	readonly FactoryObjectStore<WindowBuilderCreationConfig, IWindowBuilder> _windowBuilders = new();

	public bool IsDisposed { get; private set; }

	public TffrFactory() : this(new()) { }
	public TffrFactory(FactoryCreationConfig config) { }

	public void Dispose() {
		if (IsDisposed) return;
		try {
			_windowBuilders.DisposeAll();
		}
		finally {
			IsDisposed = true;
		}
	}

	public IWindowBuilder GetWindowBuilder() => GetWindowBuilder(new());
	public IWindowBuilder GetWindowBuilder(WindowBuilderCreationConfig config) {
		ThrowIfThisIsDisposed();
		if (!_windowBuilders.ContainsObjectForConfig(config)) _windowBuilders.SetObjectForConfig(config, new NativeWindowBuilder(config));
		return _windowBuilders.GetObjectForConfig(config);
	}

	void ThrowIfThisIsDisposed() {
		if (IsDisposed) throw new InvalidOperationException("Factory has been disposed.");
	}

	//public (/* TODO tuple or dedicated struct of stuff handles */) BuildDefaultStuff() { } // TODO a better name, but I'd like to use this as a way to quickly create a window, camera, etc for quick "hello cube" and so on
	// TODO maybe instead of a tuple we can do the compositeresourcehandle again but allow ways of us trying to get certain handle types out of it
	// TODO or maybe this type can act as a global lookup of active resources? So we could auto-create things in the ctor (config-overridden) and then just look them up
}
#pragma warning restore CA2000