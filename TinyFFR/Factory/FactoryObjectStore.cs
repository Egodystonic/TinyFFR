// Created on 2024-01-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory;

sealed class FactoryObjectStore<TConfig, T> : IDisposable where TConfig : notnull {
	readonly ArrayPoolBackedMap<TConfig, T> _store = new();

	public bool ContainsObjectForConfig(TConfig config) => _store.ContainsKey(config);
	public T GetObjectForConfig(TConfig config) => _store[config];
	public void SetObjectForConfig(TConfig config, T obj) => _store[config] = obj;

	public void Dispose() {
		foreach (var kvp in _store) {
			if (kvp.Value is IDisposable disposable) disposable.Dispose();
		}
		_store.Dispose();
	}
}

sealed class FactoryObjectStore<T> : IDisposable {
	readonly ArrayPoolBackedVector<T> _store = new();

	public bool ContainsObjectType<TDerived>() where TDerived : T {
		foreach (var item in _store) {
			if (item is TDerived) return true;
		}
		return false;
	}
	public TDerived GetObjectOfType<TDerived>() {
		foreach (var item in _store) {
			if (item is TDerived derived) return derived;
		}
		throw new InvalidOperationException($"Store does not contain object of type '{typeof(TDerived).Name}'.");
	}

	public void SetObjectOfType(T obj) => _store.Add(obj);

	public void Dispose() {
		foreach (var obj in _store) {
			if (obj is IDisposable disposable) disposable.Dispose();
		}
		_store.Dispose();
	}
}