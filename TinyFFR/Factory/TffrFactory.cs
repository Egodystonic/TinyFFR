// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Windowing;

namespace Egodystonic.TinyFFR.Factory;

// Implementation note: Using GetXyz() instead of properties because it allows us to parameterize the getting of builders/loaders now or in future and keep everything consistent API-wise
public sealed class TffrFactory {
	readonly FactoryObjectStore<IWindowBuilder> _windowBuilders = new();

	public TffrFactory() : this(new()) { }
	public TffrFactory(FactoryCreationConfig config) { }

	public IWindowBuilder GetWindowBuilder() {
		if (!_windowBuilders.ContainsObject<NativeWindowBuilder>()) _windowBuilders.SetObject(new NativeWindowBuilder());
		return _windowBuilders.GetObject<NativeWindowBuilder>();
	}

	//public (/* TODO tuple or dedicated struct of stuff handles */) BuildDefaultStuff() { } // TODO a better name, but I'd like to use this as a way to quickly create a window, camera, etc for quick "hello cube" and so on
	// TODO maybe instead of a tuple we can do the compositeresourcehandle again but allow ways of us trying to get certain handle types out of it
	// TODO or maybe this type can act as a global lookup of active resources? So we could auto-create things in the ctor (config-overridden) and then just look them up
}